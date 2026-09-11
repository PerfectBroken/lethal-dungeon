using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace LethalDungeon.Domain.Dungeons
{
    public enum Direction { North, East, South, West }

    public readonly struct GridPoint
    {
        public int X { get; }
        public int Y { get; }
        public int Z { get; }
        public GridPoint(int x, int y, int z) { X = x; Y = y; Z = z; }
        public override string ToString() => $"({X},{Y},{Z})";
    }

    public readonly struct GridBox
    {
        public GridPoint Min { get; }
        public GridPoint Max { get; }
        public GridBox(GridPoint min, GridPoint max)
        {
            if (min.X >= max.X || min.Y >= max.Y || min.Z >= max.Z)
                throw new ArgumentException("An occupancy box must have positive volume.");
            Min = min; Max = max;
        }
    }

    public sealed class DoorSocket
    {
        public string Id { get; }
        public GridPoint Position { get; }
        public Direction Facing { get; }
        public int Width { get; }
        public int Height { get; }
        public string Kind { get; }
        public ReadOnlyCollection<int> TangentOffsets { get; }
        public DoorSocket(string id, GridPoint position, Direction facing, int width = 4, int height = 6, string kind = "standard", IEnumerable<int>? tangentOffsets = null)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(kind) ||
                (int)facing < 0 || (int)facing > 3 || width <= 0 || width > 128 || width % 2 != 0 || height <= 0 || height > 128)
                throw new ArgumentException("Invalid socket ID, direction, aperture or kind.");
            var offsets=(tangentOffsets??new[]{0}).ToArray();
            if(offsets.Length<1||offsets.Length>9||offsets.Distinct().Count()!=offsets.Length||!offsets.Contains(0)||offsets.Any(v=>Math.Abs((long)v)>6))throw new ArgumentException("Invalid tangent offsets.");
            TangentOffsets=Array.AsReadOnly(offsets);
            Id = id; Position = position; Facing = facing; Width = width; Height = height; Kind = kind;
        }
    }

    public sealed class RoomDefinition
    {
        public string Id { get; }
        public string Name { get; }
        public ReadOnlyCollection<GridBox> Boxes { get; }
        public ReadOnlyCollection<DoorSocket> Sockets { get; }
        public bool IsRootOnly { get; }
        public RoomDefinition(string id, string name, IEnumerable<GridBox> boxes, IEnumerable<DoorSocket> sockets, bool isRootOnly = false)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Module ID and name are required.");
            Id = id; Name = name; Boxes = boxes.ToList().AsReadOnly(); Sockets = sockets.ToList().AsReadOnly(); IsRootOnly = isRootOnly;
        }
    }

    public sealed class RoomCatalog
    {
        public string Version { get; }
        public ReadOnlyCollection<RoomDefinition> Rooms { get; }
        public RoomCatalog(string version, IEnumerable<RoomDefinition> rooms)
        {
            if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("A content version is required.");
            Version = version; Rooms = rooms.ToList().AsReadOnly();
            if (Rooms.Count == 0 || Rooms.Any(r => r == null) || Rooms.Select(r => r.Id).Distinct(StringComparer.Ordinal).Count() != Rooms.Count)
                throw new ArgumentException("Catalog must contain uniquely named modules.");
            foreach (var room in Rooms)
            {
                if (room.Boxes.Count == 0 || room.Boxes.Any(b => !Local(b.Min) || !Local(b.Max) || b.Min.X >= b.Max.X || b.Min.Y >= b.Max.Y || b.Min.Z >= b.Max.Z))
                    throw new ArgumentException("Module requires bounded occupancy boxes.");
                if (room.Sockets.Any(s => s == null) || room.Sockets.Select(s => s.Id).Distinct(StringComparer.Ordinal).Count() != room.Sockets.Count)
                    throw new ArgumentException("Socket IDs must be unique within a module.");
                foreach (var source in room.Sockets)
                foreach(var shift in source.TangentOffsets)
                {
                    var socket=new DoorSocket(source.Id,LayoutGeometry.OffsetSocket(source,shift),source.Facing,source.Width,source.Height,source.Kind);
                    if (!Local(socket.Position)) throw new ArgumentException("Socket is outside the local coordinate limit.");
                    var normal = LayoutGeometry.Normal(socket.Facing);
                    bool alongX = socket.Facing == Direction.North || socket.Facing == Direction.South;
                    for (int w = 0; w < socket.Width; w++)
                    for (int h = 0; h < socket.Height; h++)
                    {
                        // Half-cell probes are exact integers in doubled coordinates.
                        int tangent = 2*w-socket.Width+1;
                        var centre = new GridPoint(2*socket.Position.X+(alongX ? tangent : 0),
                            2*socket.Position.Y+2*h+1, 2*socket.Position.Z+(alongX ? 0 : tangent));
                        var inside = LayoutGeometry.Subtract(centre,normal);
                        var outside = LayoutGeometry.Add(centre,normal);
                        if (!room.Boxes.Any(b => ContainsDoubled(b,inside)) || room.Boxes.Any(b => ContainsDoubled(b,outside)))
                            throw new ArgumentException("Socket aperture must lie on an exterior wall with inward clearance.");
                    }
                }
            }
        }
        private static bool Local(GridPoint p) => Math.Abs((long)p.X) <= 256 && Math.Abs((long)p.Y) <= 256 && Math.Abs((long)p.Z) <= 256;
        private static bool ContainsDoubled(GridBox b, GridPoint p) =>
            p.X > 2*b.Min.X && p.X < 2*b.Max.X && p.Y > 2*b.Min.Y && p.Y < 2*b.Max.Y && p.Z > 2*b.Min.Z && p.Z < 2*b.Max.Z;
        public RoomDefinition Get(string id) => Rooms.SingleOrDefault(r => r.Id == id) ?? throw new ArgumentException("Unknown module: "+id);
    }

    public sealed class PlacedRoom
    {
        public string InstanceId { get; }
        public string ModuleId { get; }
        public GridPoint Position { get; }
        public int QuarterTurns { get; }
        public IReadOnlyDictionary<string,int> SocketOffsets { get; }
        public PlacedRoom(string instanceId, string moduleId, GridPoint position, int quarterTurns = 0, IDictionary<string,int>? socketOffsets = null)
        {
            if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(moduleId)) throw new ArgumentException("Instance and module IDs are required.");
            if (quarterTurns < 0 || quarterTurns > 3) throw new ArgumentOutOfRangeException(nameof(quarterTurns));
            if (Math.Abs((long)position.X)>1000000 || Math.Abs((long)position.Y)>1000000 || Math.Abs((long)position.Z)>1000000)
                throw new ArgumentOutOfRangeException(nameof(position));
            SocketOffsets=new ReadOnlyDictionary<string,int>(new Dictionary<string,int>(socketOffsets??new Dictionary<string,int>()));
            InstanceId = instanceId; ModuleId = moduleId; Position = position; QuarterTurns = quarterTurns;
        }
    }

    public sealed class DoorConnection
    {
        public string Id { get; }
        public string FromRoom { get; }
        public string FromSocket { get; }
        public string ToRoom { get; }
        public string ToSocket { get; }
        public string DoorId { get; }
        public DoorConnection(string id, string fromRoom, string fromSocket, string toRoom, string toSocket, string doorId)
        {
            if (new[] {id,fromRoom,fromSocket,toRoom,toSocket,doorId}.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Connection IDs are required.");
            Id = id; FromRoom = fromRoom; FromSocket = fromSocket; ToRoom = toRoom; ToSocket = toSocket; DoorId = doorId;
        }
    }

    public sealed class DungeonManifest
    {
        public string GeneratorVersion { get; }
        public string DoorSelectionVersion => "socket-offsets-v1";
        public string ContentVersion { get; }
        public decimal MetresPerUnit => 0.5m;
        public ReadOnlyCollection<PlacedRoom> Rooms { get; }
        public ReadOnlyCollection<DoorConnection> Connections { get; }
        public DungeonManifest(string contentVersion, IEnumerable<PlacedRoom> rooms, IEnumerable<DoorConnection> connections, string generatorVersion = "branch-routing-v0.3")
        { GeneratorVersion = generatorVersion; ContentVersion = contentVersion; Rooms = rooms.ToList().AsReadOnly(); Connections = connections.ToList().AsReadOnly(); }
    }

    public sealed class ValidationResult
    {
        public ReadOnlyCollection<string> Errors { get; }
        public bool IsValid => Errors.Count == 0;
        public ValidationResult(IEnumerable<string> errors) { Errors = errors.ToList().AsReadOnly(); }
    }

    public sealed class GenerationResult
    {
        public DungeonManifest? Manifest { get; }
        public string Failure { get; }
        public int Attempts { get; }
        public int Backtracks { get; }
        public bool Succeeded => Manifest != null;
        public GenerationResult(DungeonManifest? manifest, string failure, int attempts, int backtracks)
        { Manifest = manifest; Failure = failure; Attempts = attempts; Backtracks = backtracks; }
    }

    public interface IRandomSource { int Next(int exclusiveMax); }
    public sealed class XorShiftRandom : IRandomSource
    {
        private uint state;
        public XorShiftRandom(uint seed) { state = seed == 0 ? 0x9E3779B9u : seed; }
        public int Next(int exclusiveMax)
        {
            if (exclusiveMax <= 0) throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (int)(state % (uint)exclusiveMax);
        }
    }
}
