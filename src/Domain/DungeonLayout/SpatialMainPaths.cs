using System;
using System.Collections.Generic;
using System.Linq;
namespace LethalDungeon.Domain.Dungeons
{
    internal static class SpatialMainPaths
    {
        // Plan elevations on the ring skeleton before exploration paths are attached.
        // The conservative XZ footprint stays fixed; move whole connected components,
        // otherwise attachments to later rings would become disconnected in world space.
        internal static bool Elevate(RoomCatalog catalog,List<PlacedRoom> rooms,List<DoorConnection> links,
            IEnumerable<BranchLoopTrace> traces,Func<bool> spend,Func<int,int> next)
        {
            foreach(var trace in traces)
            {
                var ring=trace.BranchA.Concat(trace.BranchB).Concat(trace.Bridge).Append(trace.ForkRoom).ToHashSet();
                var byId=rooms.ToDictionary(r=>r.InstanceId);
                var adjacency=rooms.ToDictionary(r=>r.InstanceId,r=>links.Where(e=>e.FromRoom==r.InstanceId||e.ToRoom==r.InstanceId).Select(e=>e.FromRoom==r.InstanceId?e.ToRoom:e.FromRoom).ToArray());
                bool Straight(string id) {
                    var ns=adjacency[id];if(ns.Length!=2||byId[id].ModuleId!="junction")return false;
                    var a=byId[ns[0]].Position;var b=byId[ns[1]].Position;var p=byId[id].Position;
                    return a.Y==p.Y&&b.Y==p.Y&&a.X+b.X==2*p.X&&a.Z+b.Z==2*p.Z;
                }
                var candidates=ring.Where(Straight).OrderBy(id=>id,StringComparer.Ordinal).ToArray();
                var pairs=(from a in candidates from b in candidates where string.CompareOrdinal(a,b)<0 select (A:a,B:b)).ToList();
                for(int i=pairs.Count-1;i>0;i--){var j=next(i+1);var p=pairs[i];pairs[i]=pairs[j];pairs[j]=p;}
                bool completed=false;
                foreach(var pair in pairs)
                {
                    if(!spend())return false;
                    var cut=new HashSet<string>{pair.A,pair.B};
                    var lower=new HashSet<string>{trace.ForkRoom};var queue=new Queue<string>();queue.Enqueue(trace.ForkRoom);
                    if(cut.Contains(trace.ForkRoom))continue;
                    while(queue.Count>0){var id=queue.Dequeue();foreach(var neighbor in adjacency[id])if(!cut.Contains(neighbor)&&lower.Add(neighbor))queue.Enqueue(neighbor);}
                    var raised=new HashSet<string>(rooms.Select(r=>r.InstanceId).Where(id=>!lower.Contains(id)&&!cut.Contains(id)));
                    if(ring.Count(id=>raised.Contains(id))<2||ring.Count(id=>lower.Contains(id))<2)continue;
                    if(cut.Any(id=>adjacency[id].Count(n=>raised.Contains(n))!=1))continue;
                    // Count all affected transforms and changed transition doors against the same budget.
                    for(int n=0;n<raised.Count+6;n++)if(!spend())return false;
                    var proposed=rooms.Select(r=>raised.Contains(r.InstanceId)?new PlacedRoom(r.InstanceId,r.ModuleId,new GridPoint(r.Position.X,r.Position.Y+8,r.Position.Z),r.QuarterTurns):r).ToList();
                    var replacements=new Dictionary<(string,string),string>();
                    foreach(var id in new[]{pair.A,pair.B})
                    {
                        var low=adjacency[id].Single(n=>lower.Contains(n));
                        var edge=links.Single(e=>(e.FromRoom==id&&e.ToRoom==low)||(e.ToRoom==id&&e.FromRoom==low));
                        var parent=proposed.Single(r=>r.InstanceId==low);var port=edge.FromRoom==low?edge.FromSocket:edge.ToSocket;
                        var module=id==pair.A?"routing_stairs":"routing_ramp";
                        var placed=LayoutGeometry.Attach(catalog.Get(parent.ModuleId),parent,port,catalog.Get(module),"lower",id);
                        proposed[proposed.FindIndex(r=>r.InstanceId==id)]=placed;
                        foreach(var e in links.Where(e=>e.FromRoom==id||e.ToRoom==id)) {
                            var neighbor=e.FromRoom==id?e.ToRoom:e.FromRoom;var old=e.FromRoom==id?e.FromSocket:e.ToSocket;
                            replacements[(id,old)]=raised.Contains(neighbor)?"upper":"lower";
                        }
                    }
                    string Port(string id,string old)=>replacements.TryGetValue((id,old),out var port)?port:old;
                    var proposedLinks=links.Select(e=>new DoorConnection(e.Id,e.FromRoom,Port(e.FromRoom,e.FromSocket),e.ToRoom,Port(e.ToRoom,e.ToSocket),e.DoorId)).ToList();
                    var map=new DungeonManifest(catalog.Version,proposed,proposedLinks,"spatial-main-paths-v0.5");
                    if(!LayoutValidator.Validate(catalog,map).IsValid)continue;
                    rooms.Clear();rooms.AddRange(proposed);links.Clear();links.AddRange(proposedLinks);completed=true;break;
                }
                if(!completed)return false;
            }
            return true;
        }
    }
}
