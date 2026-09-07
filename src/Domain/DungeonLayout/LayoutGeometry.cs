using System;
using System.Collections.Generic;
using System.Linq;

namespace LethalDungeon.Domain.Dungeons
{
    public static class LayoutGeometry
    {
        internal static GridPoint Add(GridPoint a, GridPoint b) => new GridPoint(checked(a.X+b.X),checked(a.Y+b.Y),checked(a.Z+b.Z));
        internal static GridPoint Subtract(GridPoint a, GridPoint b) => new GridPoint(checked(a.X-b.X),checked(a.Y-b.Y),checked(a.Z-b.Z));
        internal static GridPoint Normal(Direction facing) => facing == Direction.North ? new GridPoint(0,0,1) :
            facing == Direction.East ? new GridPoint(1,0,0) : facing == Direction.South ? new GridPoint(0,0,-1) : new GridPoint(-1,0,0);
        public static GridPoint Rotate(GridPoint p, int quarterTurns)
        {
            switch (quarterTurns)
            {
                case 0: return p;
                case 1: return new GridPoint(p.Z,p.Y,checked(-p.X));
                case 2: return new GridPoint(checked(-p.X),p.Y,checked(-p.Z));
                case 3: return new GridPoint(checked(-p.Z),p.Y,p.X);
                default: throw new ArgumentOutOfRangeException(nameof(quarterTurns));
            }
        }
        public static IReadOnlyList<GridBox> WorldBoxes(RoomDefinition definition, PlacedRoom room)
        {
            return definition.Boxes.Select(box => {
                var a = Add(Rotate(box.Min,room.QuarterTurns),room.Position);
                var b = Add(Rotate(box.Max,room.QuarterTurns),room.Position);
                return new GridBox(new GridPoint(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Min(a.Z,b.Z)),
                    new GridPoint(Math.Max(a.X,b.X),Math.Max(a.Y,b.Y),Math.Max(a.Z,b.Z)));
            }).ToList().AsReadOnly();
        }
        public static DoorSocket WorldSocket(RoomDefinition definition, PlacedRoom room, string socketId)
        {
            var s = definition.Sockets.SingleOrDefault(p => p.Id == socketId) ?? throw new ArgumentException("Unknown socket: "+socketId);
            return new DoorSocket(s.Id,Add(Rotate(s.Position,room.QuarterTurns),room.Position),
                (Direction)(((int)s.Facing+room.QuarterTurns)%4),s.Width,s.Height,s.Kind);
        }
        public static PlacedRoom Attach(RoomDefinition parentDefinition, PlacedRoom parent, string parentSocket,
            RoomDefinition childDefinition, string childSocket, string childInstanceId)
        {
            var a = WorldSocket(parentDefinition,parent,parentSocket);
            var b = childDefinition.Sockets.SingleOrDefault(s => s.Id == childSocket) ?? throw new ArgumentException("Unknown child socket.");
            if (a.Width != b.Width || a.Height != b.Height || a.Kind != b.Kind) throw new ArgumentException("Incompatible door apertures.");
            int turn = ((int)a.Facing+2-(int)b.Facing+4)%4;
            return new PlacedRoom(childInstanceId,childDefinition.Id,Subtract(a.Position,Rotate(b.Position,turn)),turn);
        }
        public static bool Overlaps(GridBox a, GridBox b) =>
            a.Min.X < b.Max.X && b.Min.X < a.Max.X &&
            a.Min.Y < b.Max.Y && b.Min.Y < a.Max.Y &&
            a.Min.Z < b.Max.Z && b.Min.Z < a.Max.Z;
        public static bool RoomsOverlap(RoomDefinition a, PlacedRoom pa, RoomDefinition b, PlacedRoom pb) =>
            WorldBoxes(a,pa).Any(ba => WorldBoxes(b,pb).Any(bb => Overlaps(ba,bb)));
        public static GridBox DoorwayBounds(DoorSocket s)
        {
            bool alongX = s.Facing == Direction.North || s.Facing == Direction.South;
            int halfX = alongX ? s.Width/2 : 2;
            int halfZ = alongX ? 2 : s.Width/2;
            return new GridBox(new GridPoint(s.Position.X-halfX,s.Position.Y,s.Position.Z-halfZ),
                new GridPoint(s.Position.X+halfX,s.Position.Y+s.Height,s.Position.Z+halfZ));
        }
    }
}
