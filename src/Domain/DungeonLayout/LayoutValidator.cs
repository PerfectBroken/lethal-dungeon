using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalDungeon.Domain.Dungeons
{
    public static class LayoutValidator
    {
        public static ValidationResult Validate(RoomCatalog catalog, DungeonManifest manifest)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            var errors = new List<string>();
            if (manifest.ContentVersion != catalog.Version) errors.Add("ContentVersion");
            if (manifest.Rooms.Count == 0) errors.Add("EmptyLayout");
            if (manifest.Rooms.Any(r => r == null) || manifest.Connections.Any(c => c == null))
                return new ValidationResult(new[] { "NullMember" });
            if (manifest.Rooms.Select(r => r.InstanceId).Distinct().Count() != manifest.Rooms.Count) errors.Add("DuplicateInstance");
            if (manifest.Rooms.Any(r => !catalog.Rooms.Any(d => d.Id == r.ModuleId))) errors.Add("UnknownModule");
            if (errors.Contains("DuplicateInstance") || errors.Contains("UnknownModule")) return new ValidationResult(errors);
            var rooms = manifest.Rooms.ToDictionary(r => r.InstanceId);
            var boxes = rooms.ToDictionary(p => p.Key, p => LayoutGeometry.WorldBoxes(catalog.Get(p.Value.ModuleId), p.Value));
            for (int i = 0; i < manifest.Rooms.Count; i++)
            for (int j = i+1; j < manifest.Rooms.Count; j++)
                if (boxes[manifest.Rooms[i].InstanceId].Any(a => boxes[manifest.Rooms[j].InstanceId].Any(b => LayoutGeometry.Overlaps(a,b))))
                    errors.Add("Overlap:"+manifest.Rooms[i].InstanceId+":"+manifest.Rooms[j].InstanceId);
            var ids = new HashSet<string>();
            var doors = new HashSet<string>();
            var ports = new HashSet<(string,string)>();
            var adjacent = rooms.ToDictionary(p => p.Key, p => new List<string>());
            foreach (var link in manifest.Connections)
            {
                if (!ids.Add(link.Id)) errors.Add("DuplicateConnection:"+link.Id);
                if (!doors.Add(link.DoorId)) errors.Add("DuplicateDoor:"+link.DoorId);
                if (!ports.Add((link.FromRoom,link.FromSocket)) || !ports.Add((link.ToRoom,link.ToSocket))) errors.Add("SocketReused:"+link.Id);
                if (!rooms.TryGetValue(link.FromRoom,out var a) || !rooms.TryGetValue(link.ToRoom,out var b)) { errors.Add("UnknownRoom:"+link.Id); continue; }
                if (a.InstanceId == b.InstanceId) { errors.Add("SelfConnection:"+link.Id); continue; }
                var ad = catalog.Get(a.ModuleId); var bd = catalog.Get(b.ModuleId);
                if (!ad.Sockets.Any(s => s.Id == link.FromSocket) || !bd.Sockets.Any(s => s.Id == link.ToSocket)) { errors.Add("UnknownSocket:"+link.Id); continue; }
                var sa = LayoutGeometry.WorldSocket(ad,a,link.FromSocket);
                var sb = LayoutGeometry.WorldSocket(bd,b,link.ToSocket);
                if (sa.Width != sb.Width || sa.Height != sb.Height || sa.Kind != sb.Kind) { errors.Add("DoorCompatibility:"+link.Id); continue; }
                if (!sa.Position.Equals(sb.Position) || ((int)sa.Facing+2)%4 != (int)sb.Facing) { errors.Add("DoorAlignment:"+link.Id); continue; }
                adjacent[a.InstanceId].Add(b.InstanceId); adjacent[b.InstanceId].Add(a.InstanceId);
                var clearance = LayoutGeometry.DoorwayBounds(sa);
                foreach (var other in rooms.Keys.Where(id => id != a.InstanceId && id != b.InstanceId))
                    if (boxes[other].Any(box => LayoutGeometry.Overlaps(clearance,box))) errors.Add("BlockedDoorway:"+link.Id+":"+other);
            }
            if (rooms.Count > 0)
            {
                var visited = new HashSet<string>(); var pending = new Stack<string>();
                pending.Push(manifest.Rooms[0].InstanceId);
                while (pending.Count > 0)
                {
                    string id = pending.Pop();
                    if (!visited.Add(id)) continue;
                    foreach (var next in adjacent[id]) pending.Push(next);
                }
                if (visited.Count != rooms.Count) errors.Add("Disconnected");
            }
            return new ValidationResult(errors);
        }
    }
}
