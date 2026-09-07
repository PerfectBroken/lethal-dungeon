using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalDungeon.Domain.Dungeons
{
    public static class LoopConnections
    {
        public static IReadOnlyList<DoorConnection> Find(RoomCatalog catalog, DungeonManifest manifest)
        {
            if (!LayoutValidator.Validate(catalog,manifest).IsValid) throw new ArgumentException("Loop discovery requires a valid connected layout.");
            var used = new HashSet<(string,string)>(manifest.Connections.SelectMany(c => new[] {(c.FromRoom,c.FromSocket),(c.ToRoom,c.ToSocket)}));
            var free = manifest.Rooms.SelectMany(r => catalog.Get(r.ModuleId).Sockets
                .Where(s => !used.Contains((r.InstanceId,s.Id)))
                .Select(s => (Room:r.InstanceId,Socket:LayoutGeometry.WorldSocket(catalog.Get(r.ModuleId),r,s.Id)))).ToList();
            var adjacent = manifest.Rooms.ToDictionary(r => r.InstanceId,r => new List<string>());
            foreach (var c in manifest.Connections) { adjacent[c.FromRoom].Add(c.ToRoom); adjacent[c.ToRoom].Add(c.FromRoom); }
            var result = new List<DoorConnection>();
            for (int i = 0; i < free.Count; i++)
            for (int j = i+1; j < free.Count; j++)
            {
                var a=free[i]; var b=free[j];
                if (a.Room == b.Room || !Matches(a.Socket,b.Socket)) continue;
                var distance=new Dictionary<string,int>{{a.Room,0}};var queue=new Queue<string>();queue.Enqueue(a.Room);
                while(queue.Count>0 && !distance.ContainsKey(b.Room))
                {
                    var current=queue.Dequeue();
                    foreach(var next in adjacent[current]) if(!distance.ContainsKey(next))
                    { distance[next]=distance[current]+1;queue.Enqueue(next); }
                }
                if (!distance.TryGetValue(b.Room,out int length) || length < 3) continue;
                string id="loop_"+a.Room+"_"+a.Socket.Id+"__"+b.Room+"_"+b.Socket.Id;
                result.Add(new DoorConnection(id,a.Room,a.Socket.Id,b.Room,b.Socket.Id,"door_"+id));
            }
            return result.AsReadOnly();
        }
        internal static bool Matches(DoorSocket a,DoorSocket b) => a.Position.Equals(b.Position) &&
            ((int)a.Facing+2)%4==(int)b.Facing && a.Width==b.Width && a.Height==b.Height && a.Kind==b.Kind;
    }
}
