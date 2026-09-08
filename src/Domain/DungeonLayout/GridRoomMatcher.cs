using System;
using System.Collections.Generic;
using System.Linq;
namespace LethalDungeon.Domain.Dungeons
{
 internal static class GridRoomMatcher
 {
  sealed class Candidate
  {
   internal RoomPattern Pattern=null!;
   internal int Rotation;
   internal GridPoint Offset;
   internal string[] Cells=Array.Empty<string>();
   internal Dictionary<(string Inside,string Outside),string> Ports=new Dictionary<(string,string),string>();
  }
  internal static ConfiguredDungeonResult Generate(ConfiguredRoomCatalog catalog,uint seed,int maximum)
  {
   if(catalog==null)throw new ArgumentNullException(nameof(catalog));
   if(maximum<1||maximum>100000)throw new ArgumentOutOfRangeException(nameof(maximum));
   string lastFailure="Unknown";int attempts=0;bool Spend(){if(attempts>=maximum)return false;attempts++;return true;}
   ConfiguredDungeonResult Fail(string reason)=>new ConfiguredDungeonResult(new GenerationResult(null,reason,attempts,0));
   // Coverage of all nonempty horizontal masks is checked by capabilities, never by ID.
   var masks=new HashSet<int>();
   foreach(var p in catalog.Patterns.Where(p=>p.Cells.Count==1))foreach(var q in p.Rotations){int mask=0;foreach(var s in p.Room.Sockets)mask|=1<<(((int)s.Facing+q)%4);masks.Add(mask);}
   if(Enumerable.Range(1,15).Any(mask=>!masks.Contains(mask)))return Fail("MissingFallback");
   var transitions=catalog.Patterns.Where(p=>p.Cells.Select(c=>c.Y).Distinct().Count()>1).ToArray();
   if(transitions.Length==0)return Fail("MissingTransition");
   var plan=SeededDungeon.Resolve(seed);var random=new XorShiftRandom(plan.LayoutSeed);
   var ordered=catalog.Patterns.OrderBy(p=>p.Room.Id,StringComparer.Ordinal).ToArray();
   for(int variant=0;variant<10&&attempts<maximum;variant++)
   {
    var raw=BranchLoopGenerator.Generate(random,plan.TargetRooms,plan.LoopCount,Math.Min(20000,maximum-attempts),false,new ExplorationBranchOptions(),false,true);
    attempts+=raw.Layout.Attempts;if(!raw.Layout.Succeeded){lastFailure="Grid:"+raw.Layout.Failure;continue;}
    var original=raw.Layout.Manifest!;var nodes=original.Rooms.ToDictionary(r=>r.InstanceId,r=>r.Position);
    var edges=original.Connections;var adjacent=nodes.Keys.ToDictionary(id=>id,id=>edges.Where(e=>e.FromRoom==id||e.ToRoom==id).Select(e=>e.FromRoom==id?e.ToRoom:e.FromRoom).ToArray());
    var xz=nodes.ToDictionary(p=>(p.Value.X,p.Value.Z),p=>p.Key);
    List<Candidate> Candidates(IEnumerable<RoomPattern> patterns,bool flatten,HashSet<string>? allowed=null)
    {
     var output=new List<Candidate>();
     foreach(var p in patterns)foreach(var q in p.Rotations)foreach(var anchor in original.Rooms.Where(r=>allowed==null||(allowed.Contains(r.InstanceId)&&adjacent[r.InstanceId].Length==2)))
     {
      if(!Spend())return output;
      var first=LayoutGeometry.Rotate(p.Cells[0],q);var offset=LayoutGeometry.Subtract(nodes[anchor.InstanceId],first);
      var ids=new string[p.Cells.Count];bool valid=true;
      for(int i=0;i<p.Cells.Count;i++){
       var v=LayoutGeometry.Add(LayoutGeometry.Rotate(p.Cells[i],q),offset);
       if(!xz.TryGetValue((v.X,v.Z),out var id)||(!flatten&&nodes[id].Y!=v.Y)){valid=false;break;}ids[i]=id;
      }
      if(!valid||(allowed!=null&&ids.Any(id=>!allowed.Contains(id))))continue;var set=new HashSet<string>(ids);
      var inside=edges.Where(e=>set.Contains(e.FromRoom)&&set.Contains(e.ToRoom)).ToArray();
      if(inside.Length!=p.Edges.Count||p.Edges.Any(e=>!adjacent[ids[e.A]].Contains(ids[e.B])))continue;
      var boundary=ids.SelectMany(id=>adjacent[id].Where(n=>!set.Contains(n)).Select(n=>(Inside:id,Outside:n))).ToArray();
      if(boundary.Length!=p.Room.Sockets.Count)continue;
      var ports=new Dictionary<(string,string),string>();
      foreach(var s in p.Room.Sockets){
       var id=ids[p.SocketCells[s.Id]];var normal=LayoutGeometry.Rotate(LayoutGeometry.Normal(s.Facing),q);var pos=nodes[id];
       var match=boundary.Where(e=>e.Inside==id&&nodes[e.Outside].X==pos.X+normal.X*16&&nodes[e.Outside].Z==pos.Z+normal.Z*16).ToArray();
       if(match.Length!=1||(!flatten&&nodes[match[0].Outside].Y!=pos.Y)||ports.ContainsKey(match[0])){valid=false;break;}ports.Add(match[0],s.Id);
      }
      if(valid)output.Add(new Candidate{Pattern=p,Rotation=q,Offset=offset,Cells=ids,Ports=ports});
     }
     return output;
    }
    bool elevated=true;
    for(int li=0;li<raw.Loops.Count;li++)
    {
     var loop=raw.Loops[li];var ring=new HashSet<string>(loop.BranchA.Concat(loop.BranchB).Concat(loop.Bridge).Append(loop.ForkRoom));
     var candidates=Candidates(ordered.Where(p=>transitions.Contains(p)),true,ring).Where(c=>c.Cells.All(id=>ring.Contains(id)&&adjacent[id].Length==2)).ToList();
     // Seeded ties; complex footprints have priority over straight two-cell transitions.
     candidates=candidates.Select(c=>(Candidate:c,Tie:random.Next(int.MaxValue))).OrderByDescending(p=>p.Candidate.Cells.Length).ThenBy(p=>p.Tie).Select(p=>p.Candidate).ToList();
     bool found=false;
     foreach(var a in candidates){if(found)break;foreach(var b in candidates){
      if(!Spend())break;
      if(a.Cells.Intersect(b.Cells).Any())continue;var cut=new HashSet<string>(a.Cells.Concat(b.Cells));
      if(cut.Contains(loop.ForkRoom))continue;
      var low=new HashSet<string>{loop.ForkRoom};var queue=new Queue<string>();queue.Enqueue(loop.ForkRoom);
      while(queue.Count>0){foreach(var n in adjacent[queue.Dequeue()])if(!cut.Contains(n)&&low.Add(n))queue.Enqueue(n);}
      var high=new HashSet<string>(nodes.Keys.Where(id=>!low.Contains(id)&&!cut.Contains(id)));
      if(ring.Count(id=>high.Contains(id))<2||ring.Count(id=>low.Contains(id))<2)continue;
      if(!raw.ExplorationBranches.Where(t=>t.LoopIndex==li).Any(t=>high.Contains(t.AnchorRoom)))continue;
      bool Good(Candidate c){var lower=c.Pattern.Room.Sockets.OrderBy(s=>s.Position.Y).First().Id;var upper=c.Pattern.Room.Sockets.OrderBy(s=>s.Position.Y).Last().Id;return low.Contains(c.Ports.Single(p=>p.Value==lower).Key.Outside)&&high.Contains(c.Ports.Single(p=>p.Value==upper).Key.Outside);}
      if(!Good(a)||!Good(b))continue;
      foreach(var id in high){var p=nodes[id];nodes[id]=new GridPoint(p.X,p.Y+8,p.Z);}
      foreach(var c in new[]{a,b}){
       var lower=c.Pattern.Room.Sockets.OrderBy(s=>s.Position.Y).First();int floor=nodes[c.Ports.Single(p=>p.Value==lower.Id).Key.Outside].Y-lower.Position.Y;
       for(int i=0;i<c.Cells.Length;i++){var id=c.Cells[i];var p=nodes[id];nodes[id]=new GridPoint(p.X,floor+c.Pattern.Cells[i].Y,p.Z);}
      }
      found=true;break;
     }}
     if(!found){lastFailure="Elevation:"+li+":"+candidates.Count;elevated=false;break;}
    }
    if(!elevated||attempts>=maximum)continue;
    var choices=Candidates(ordered,false).Select(c=>(Candidate:c,Tie:random.Next(int.MaxValue)))
     .OrderByDescending(p=>p.Candidate.Pattern.Cells.Select(c=>c.Y).Distinct().Count()>1)
     .ThenByDescending(p=>p.Candidate.Cells.Length).ThenBy(p=>p.Tie).Select(p=>p.Candidate).ToArray();
    if(attempts>=maximum)continue;
    var occupied=new HashSet<string>();var selected=new List<Candidate>();
    foreach(var c in choices){if(c.Cells.Any(id=>occupied.Contains(id)))continue;selected.Add(c);foreach(var id in c.Cells)occupied.Add(id);}
    if(occupied.Count!=nodes.Count){lastFailure="Cover:"+occupied.Count+"/"+nodes.Count;continue;}
    // Keep the entry-containing instance first for the established manifest root convention.
    selected=selected.OrderByDescending(c=>c.Cells.Contains("room_000")).ToList();
    var placements=selected.Select((c,i)=>new PlacedRoom("module_"+i.ToString("000",System.Globalization.CultureInfo.InvariantCulture),c.Pattern.Room.Id,c.Offset,c.Rotation)).ToArray();
    var owner=selected.SelectMany((c,i)=>c.Cells.Select(id=>(Id:id,Index:i))).ToDictionary(p=>p.Id,p=>p.Index);
    var connections=new List<DoorConnection>();
    foreach(var edge in edges){int a=owner[edge.FromRoom],b=owner[edge.ToRoom];if(a==b)continue;connections.Add(new DoorConnection(edge.Id,placements[a].InstanceId,selected[a].Ports[(edge.FromRoom,edge.ToRoom)],placements[b].InstanceId,selected[b].Ports[(edge.ToRoom,edge.FromRoom)],edge.DoorId));}
    if(!Spend())continue;
    var map=new DungeonManifest(catalog.Geometry.Version,placements,connections,"configured-grid-v0.6");
    var validation=LayoutValidator.Validate(catalog.Geometry,map);if(!validation.IsValid){lastFailure=string.Join(",",validation.Errors.Take(3));continue;}
    if(map.Connections.Count-map.Rooms.Count+1!=plan.LoopCount)throw new InvalidOperationException("Template contraction changed cycle rank.");
    var grid=new DungeonManifest("abstract-grid-v1",original.Rooms.Select(r=>new PlacedRoom(r.InstanceId,"cell",nodes[r.InstanceId])),edges,"configured-grid-v0.6");
    return new ConfiguredDungeonResult(new GenerationResult(map,string.Empty,attempts,variant),grid,selected.Select((c,i)=>new RoomCoverage(placements[i].InstanceId,c.Cells)),raw.Loops,raw.ExplorationBranches);
   }
   return Fail(attempts>=maximum?"BudgetExceeded":"NoTemplateLayout:"+lastFailure);
  }
 }
}
