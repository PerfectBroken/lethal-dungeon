using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalDungeon.Domain.Dungeons
{
    public static class DungeonGenerator
    {
        public static GenerationResult Generate(RoomCatalog catalog, string rootModuleId, int targetRooms,
            IRandomSource random, int maxAttempts = 3000, bool requireHeightChange = false, int minimumCycles = 0, string? cycleModuleId = null, DungeonManifest? initialLayout = null)
        {
            if (minimumCycles < 0 || minimumCycles > 8) throw new ArgumentOutOfRangeException(nameof(minimumCycles));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (random == null) throw new ArgumentNullException(nameof(random));
            if (targetRooms < 1 || targetRooms > 64) throw new ArgumentOutOfRangeException(nameof(targetRooms));
            if (maxAttempts < 1 || maxAttempts > 100000) throw new ArgumentOutOfRangeException(nameof(maxAttempts));
            catalog.Get(rootModuleId);
            if (minimumCycles > 0)
            {
                if (string.IsNullOrWhiteSpace(cycleModuleId)) throw new ArgumentException("A connector module is required for loop growth.");
                if (catalog.Get(cycleModuleId!).IsRootOnly) throw new ArgumentException("Connector module cannot be root-only.");
            }
            if (initialLayout != null && (!LayoutValidator.Validate(catalog,initialLayout).IsValid ||
                initialLayout.Rooms.Count > targetRooms || initialLayout.Rooms[0].ModuleId != rootModuleId))
                throw new ArgumentException("Invalid initial layout, root or target count.");
            var rooms = initialLayout == null ? new List<PlacedRoom> { new PlacedRoom("room_000",rootModuleId,new GridPoint(0,0,0)) } : initialLayout.Rooms.ToList();
            var links = initialLayout == null ? new List<DoorConnection>() : initialLayout.Connections.ToList();
            int attempts = 0, backtracks = 0;
            DungeonManifest Snapshot() => new DungeonManifest(catalog.Version,rooms,links);
            bool Search()
            {
                if (rooms.Count == targetRooms)
                    return links.Count-rooms.Count+1 >= minimumCycles && (!requireHeightChange || links.Select(c => {
                        var p = rooms.Single(r => r.InstanceId == c.FromRoom);
                        return LayoutGeometry.WorldSocket(catalog.Get(p.ModuleId),p,c.FromSocket).Position.Y;
                    }).Distinct().Take(2).Count() == 2);
                bool needsLoops = links.Count-rooms.Count+1 < minimumCycles;
                var used = new HashSet<(string,string)>(links.SelectMany(c => new[] { (c.FromRoom,c.FromSocket),(c.ToRoom,c.ToSocket) }));
                var candidates = new List<(PlacedRoom Parent,DoorSocket From,RoomDefinition Child,DoorSocket To)>();
                foreach (var parent in rooms)
                foreach (var from in catalog.Get(parent.ModuleId).Sockets.OrderBy(s => s.Id,StringComparer.Ordinal))
                {
                    if (used.Contains((parent.InstanceId,from.Id))) continue;
                    foreach (var child in catalog.Rooms.Where(r => !r.IsRootOnly && (!needsLoops || r.Id == cycleModuleId)).OrderBy(r => r.Id,StringComparer.Ordinal))
                    foreach (var to in child.Sockets.OrderBy(s => s.Id,StringComparer.Ordinal))
                        if (from.Width == to.Width && from.Height == to.Height && from.Kind == to.Kind) candidates.Add((parent,from,child,to));
                }
                for (int i = candidates.Count-1; i > 0; i--)
                {
                    int j = random.Next(i+1);
                    if (j < 0 || j > i) throw new InvalidOperationException("Random source returned an out-of-range index.");
                    var swap = candidates[i]; candidates[i] = candidates[j]; candidates[j] = swap;
                }
                if (needsLoops)
                {
                    // Stable sort retains randomized ties. Reward geometric opportunities to close a ring.
                    var free = rooms.SelectMany(r => catalog.Get(r.ModuleId).Sockets.Where(s => !used.Contains((r.InstanceId,s.Id)))
                        .Select(s => (Room:r.InstanceId,Socket:LayoutGeometry.WorldSocket(catalog.Get(r.ModuleId),r,s.Id)))).ToList();
                    candidates = candidates.OrderByDescending(c => {
                        var placed = LayoutGeometry.Attach(catalog.Get(c.Parent.ModuleId),c.Parent,c.From.Id,c.Child,c.To.Id,"candidate");
                        return c.Child.Sockets.Where(s => s.Id != c.To.Id).Sum(s => {
                            var socket=LayoutGeometry.WorldSocket(c.Child,placed,s.Id);
                            return free.Count(f => f.Room != c.Parent.InstanceId && LoopConnections.Matches(socket,f.Socket));
                        });
                    }).ToList();
                }
                if (minimumCycles > 0 && requireHeightChange && rooms.Count >= targetRooms-2)
                {
                    var heights = new HashSet<int>(links.Select(c => {
                        var r=rooms.Single(r => r.InstanceId==c.FromRoom);
                        return LayoutGeometry.WorldSocket(catalog.Get(r.ModuleId),r,c.FromSocket).Position.Y;
                    }));
                    if (heights.Count == 1)
                    {
                        bool NewHeight(PlacedRoom r,string socket) => !heights.Contains(LayoutGeometry.WorldSocket(catalog.Get(r.ModuleId),r,socket).Position.Y);
                        if (rooms.Count == targetRooms-1) candidates=candidates.Where(c => NewHeight(c.Parent,c.From.Id)).ToList();
                        else candidates=candidates.OrderByDescending(c => NewHeight(c.Parent,c.From.Id) ? 2 : c.Child.Sockets.Any(s => s.Position.Y != c.To.Position.Y) ? 1 : 0).ToList();
                    }
                }
                foreach (var candidate in candidates)
                {
                    if (attempts >= maxAttempts) return false;
                    attempts++;
                    int nextId = rooms.Count;
                    string suffix;
                    do { suffix = nextId.ToString("000",System.Globalization.CultureInfo.InvariantCulture); nextId++; }
                    while (rooms.Any(r => r.InstanceId == "room_"+suffix) ||
                        links.Any(e => e.Id == "connection_"+suffix || e.DoorId == "door_"+suffix));
                    var child = LayoutGeometry.Attach(catalog.Get(candidate.Parent.ModuleId),candidate.Parent,candidate.From.Id,
                        candidate.Child,candidate.To.Id,"room_"+suffix);
                    int oldLinkCount = links.Count;
                    rooms.Add(child);
                    links.Add(new DoorConnection("connection_"+suffix,candidate.Parent.InstanceId,candidate.From.Id,child.InstanceId,candidate.To.Id,"door_"+suffix));
                    if (LayoutValidator.Validate(catalog,Snapshot()).IsValid)
                    {
                        // Apply one at a time: each new edge changes socket availability and graph distances.
                        while (links.Count-rooms.Count+1 < minimumCycles && attempts < maxAttempts)
                        {
                            bool closed = false;
                            foreach (var closure in LoopConnections.Find(catalog,Snapshot()))
                            {
                                if (attempts >= maxAttempts) break;
                                attempts++;
                                links.Add(closure);
                                if (LayoutValidator.Validate(catalog,Snapshot()).IsValid) { closed=true;break; }
                                links.RemoveAt(links.Count-1);
                            }
                            if (!closed) break;
                        }
                        if (Search()) return true;
                        backtracks++;
                    }
                    links.RemoveRange(oldLinkCount,links.Count-oldLinkCount); rooms.RemoveAt(rooms.Count-1);
                }
                return false;
            }
            if (!Search()) return new GenerationResult(null,attempts >= maxAttempts ? "BudgetExceeded" : "NoLayout",attempts,backtracks);
            var manifest = Snapshot();
            if (!LayoutValidator.Validate(catalog,manifest).IsValid) throw new InvalidOperationException("Generator produced an invalid manifest.");
            return new GenerationResult(manifest,string.Empty,attempts,backtracks);
        }
    }

    public static class PrototypeCatalog
    {
        public static RoomCatalog CreateLoopReady()
        {
            var junction = new RoomDefinition("junction","四向连接房",new[] {new GridBox(new GridPoint(-8,0,-8),new GridPoint(8,8,8))},new[] {
                new DoorSocket("north",new GridPoint(0,0,8),Direction.North),new DoorSocket("east",new GridPoint(8,0,0),Direction.East),
                new DoorSocket("south",new GridPoint(0,0,-8),Direction.South),new DoorSocket("west",new GridPoint(-8,0,0),Direction.West)});
            return new RoomCatalog("prototype-rooms-v0.2",Create().Rooms.Concat(new[] {junction}));
        }
        public static RoomCatalog Create()
        {
            GridPoint P(int x,int y,int z) => new GridPoint(x,y,z);
            GridBox B(int x0,int y0,int z0,int x1,int y1,int z1) => new GridBox(P(x0,y0,z0),P(x1,y1,z1));
            DoorSocket S(string id,int x,int y,int z,Direction facing) => new DoorSocket(id,P(x,y,z),facing);
            return new RoomCatalog("prototype-rooms-v0.1",new[] {
                new RoomDefinition("entry","入口房",new[] {B(-8,0,-8,8,8,8)},new[] {
                    S("north_01",-4,0,8,Direction.North),S("north_02",4,0,8,Direction.North),
                    S("east",8,0,0,Direction.East),S("south",0,0,-8,Direction.South),S("west",-8,0,0,Direction.West)},true),
                new RoomDefinition("hall","大厅",new[] {B(-12,0,-10,12,8,10)},new[] {
                    S("north_01",-6,0,10,Direction.North),S("north_02",6,0,10,Direction.North),
                    S("east_01",12,0,-4,Direction.East),S("east_02",12,0,4,Direction.East),
                    S("south_01",-6,0,-10,Direction.South),S("south_02",6,0,-10,Direction.South),
                    S("west_01",-12,0,-4,Direction.West),S("west_02",-12,0,4,Direction.West)}),
                new RoomDefinition("corridor","直走廊",new[] {B(-3,0,-8,3,8,8)},new[] {
                    S("north",0,0,8,Direction.North),S("south",0,0,-8,Direction.South)}),
                new RoomDefinition("elbow","L形房",new[] {B(-8,0,-8,0,8,8),B(0,0,-8,8,8,0)},new[] {
                    S("north",-4,0,8,Direction.North),S("east",8,0,-4,Direction.East),S("south",0,0,-8,Direction.South),S("west",-8,0,0,Direction.West)}),
                new RoomDefinition("stairs","楼梯间",new[] {B(-4,0,-10,4,16,10)},new[] {
                    S("lower",0,0,-10,Direction.South),S("upper",0,8,10,Direction.North)}),
                new RoomDefinition("ramp","坡道间",new[] {B(-4,0,-16,4,16,16)},new[] {
                    S("lower",0,0,-16,Direction.South),S("upper",0,8,16,Direction.North)})
            });
        }
    }
}
