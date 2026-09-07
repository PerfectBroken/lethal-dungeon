using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LethalDungeon.Domain.Dungeons;
using NUnit.Framework;

namespace LethalDungeon.Tests
{
    public class DungeonLayoutTests
    {
        private static GridPoint P(int x, int y, int z) => new GridPoint(x, y, z);
        private static GridBox B(int x0, int y0, int z0, int x1, int y1, int z1) => new GridBox(P(x0,y0,z0),P(x1,y1,z1));
        private static PlacedRoom Place(string id, string module, int x = 0, int y = 0, int z = 0, int turn = 0) => new PlacedRoom(id,module,P(x,y,z),turn);
        private static DoorConnection Link(string a, string sa, string b, string sb, string id = "c1", string door = "d1") => new DoorConnection(id,a,sa,b,sb,door);
        private static RoomDefinition Small(string id, bool rootOnly = false, params Direction[] directions)
        {
            return new RoomDefinition(id,id,new[] { B(-2,0,-2,2,4,2) },directions.Select(d =>
                new DoorSocket(d.ToString(), d == Direction.North ? P(0,0,2) : d == Direction.South ? P(0,0,-2) :
                    d == Direction.East ? P(2,0,0) : P(-2,0,0), d,2,2)),rootOnly);
        }
        private static DungeonManifest Map(RoomCatalog c, PlacedRoom[] rooms, params DoorConnection[] links) => new DungeonManifest(c.Version,rooms,links);
        private static void HasError(ValidationResult result, string prefix)
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Any(e => e.StartsWith(prefix,StringComparison.Ordinal)), Is.True, string.Join("; ",result.Errors));
        }
        private sealed class KeepOrder : IRandomSource { public int Next(int exclusiveMax) => exclusiveMax-1; }
        private sealed class BadRandom : IRandomSource { public int Next(int exclusiveMax) => exclusiveMax; }

        // MAP-002/006: hostile or malformed metadata must not bypass validation.
        [Test]
        public void DefaultBoxCannotBypassPositiveVolumeContract()
        {
            Assert.Throws<ArgumentException>(new Action(() => new RoomCatalog("v",new[] {
                new RoomDefinition("r","r",new[] {default(GridBox)},Array.Empty<DoorSocket>()) })));
        }

        [Test]
        public void MismatchedDoorDimensionsAreRejected()
        {
            var a = Small("a",false,Direction.East);
            var b = new RoomDefinition("b","b",a.Boxes,new[] {new DoorSocket("West",P(-2,0,0),Direction.West,4,2)});
            var c = new RoomCatalog("v",new[] {a,b});
            HasError(LayoutValidator.Validate(c,Map(c,new[] {Place("a","a"),Place("b","b",4)},Link("a","East","b","West"))),"DoorCompatibility");
        }

        [Test]
        public void DuplicateDoorEntitiesAreRejected()
        {
            var c = new RoomCatalog("v",new[] {Small("r",false,Direction.East,Direction.West)});
            HasError(LayoutValidator.Validate(c,Map(c,new[] {Place("a","r"),Place("b","r",4),Place("c","r",8)},
                Link("a","East","b","West"),Link("b","East","c","West","c2","d1"))),"DuplicateDoor");
        }

        // MAP-001: Golden coordinates follow Unity positive Y rotation, not a second algorithm.
        [TestCase(0,2,3)]
        [TestCase(1,3,-2)]
        [TestCase(2,-2,-3)]
        [TestCase(3,-3,2)]
        public void QuarterTurnsHaveExactCoordinates(int turn,int x,int z)
        { Assert.That(LayoutGeometry.Rotate(P(2,6,3),turn), Is.EqualTo(P(x,6,z))); }

        [TestCase(-1)]
        [TestCase(4)]
        public void InvalidRotationIsRejected(int turn)
        { Assert.Throws<ArgumentOutOfRangeException>(new Action(() => Place("a","box",turn:turn))); }

        [Test]
        public void RotatedBoxHasCorrectExtents()
        {
            var room = new RoomDefinition("r","r",new[] { B(-2,0,-4,2,6,4) },Array.Empty<DoorSocket>());
            var box = LayoutGeometry.WorldBoxes(room,Place("r","r",10,8,20,1)).Single();
            Assert.That(box.Min,Is.EqualTo(P(6,8,18)));
            Assert.That(box.Max,Is.EqualTo(P(14,14,22)));
        }

        // MAP-002
        [Test]
        public void PrototypeHasSixSpecifiedModulesAndVerticalSockets()
        {
            var c = PrototypeCatalog.Create();
            Assert.That(c.Rooms.Select(r => r.Id),Is.EquivalentTo(new[] {"entry","hall","corridor","elbow","stairs","ramp"}));
            Assert.That(c.Get("entry").Sockets.Count,Is.EqualTo(5));
            Assert.That(c.Get("hall").Sockets.Count,Is.EqualTo(8));
            Assert.That(c.Get("elbow").Boxes.Count,Is.EqualTo(2));
            Assert.That(c.Get("stairs").Sockets.Select(s => s.Position.Y).OrderBy(v => v),Is.EqualTo(new[] {0,8}));
            Assert.That(c.Get("ramp").Boxes.Single().Max.Z-c.Get("ramp").Boxes.Single().Min.Z,Is.EqualTo(32));
            Assert.That(c.Get("entry").IsRootOnly,Is.True);
        }

        [Test]
        public void ZeroVolumeBoxIsRejected()
        { Assert.Throws<ArgumentException>(new Action(() => B(0,0,0,0,8,8))); }

        [Test]
        public void DuplicateModuleIdsAreRejected()
        {
            var r = Small("same");
            Assert.Throws<ArgumentException>(new Action(() => new RoomCatalog("v",new[] {r,r})));
        }

        [Test]
        public void DuplicateSocketIdsAreRejected()
        {
            var s = new DoorSocket("same",P(0,0,2),Direction.North,2,2);
            var r = new RoomDefinition("r","r",new[] {B(-2,0,-2,2,4,2)},new[] {s,s});
            Assert.Throws<ArgumentException>(new Action(() => new RoomCatalog("v",new[] {r})));
        }

        [TestCase(0,0,0,2,2)] // Inside the room
        [TestCase(2,0,2,2,2)] // Straddles a corner
        [TestCase(0,3,2,2,2)] // Above the envelope
        [TestCase(0,0,2,3,2)] // Half-grid aperture
        [TestCase(0,0,2,2,0)]
        public void InvalidApertureIsRejected(int x,int y,int z,int width,int height)
        {
            Assert.Throws<ArgumentException>(new Action(() => new RoomCatalog("v",new[] {
                new RoomDefinition("r","r",new[] {B(-2,0,-2,2,4,2)},new[] {new DoorSocket("s",P(x,y,z),Direction.North,width,height)})
            })));
        }

        [Test]
        public void DoorFacingIntoTheRoomIsRejected()
        {
            var r = new RoomDefinition("r","r",new[] {B(-2,0,-2,2,4,2)},new[] {new DoorSocket("s",P(0,0,2),Direction.South,2,2)});
            Assert.Throws<ArgumentException>(new Action(() => new RoomCatalog("v",new[] {r})));
        }

        [Test]
        public void MissingEnvelopeIsRejected()
        { Assert.Throws<ArgumentException>(new Action(() => new RoomCatalog("v",new[] {new RoomDefinition("r","r",Array.Empty<GridBox>(),Array.Empty<DoorSocket>())}))); }

        // MAP-003
        [Test]
        public void AttachmentSolvesTranslationAndRotationFromDoorFrames()
        {
            var c=PrototypeCatalog.Create();
            var parent=Place("a","entry",40,12,60,1);
            var child=LayoutGeometry.Attach(c.Get("entry"),parent,"north_01",c.Get("corridor"),"south","b");
            Assert.That(child.Position,Is.EqualTo(P(56,12,64)));
            Assert.That(child.QuarterTurns,Is.EqualTo(1));
            var a=LayoutGeometry.WorldSocket(c.Get("entry"),parent,"north_01");
            var b=LayoutGeometry.WorldSocket(c.Get("corridor"),child,"south");
            Assert.That(a.Position,Is.EqualTo(P(48,12,64)));
            Assert.That(a.Position,Is.EqualTo(b.Position));
            Assert.That(a.Facing,Is.EqualTo(Direction.East));
            Assert.That(b.Facing,Is.EqualTo(Direction.West));
        }

        [Test]
        public void UpperStairDoorPlacesTheNextRoomOnTheUpperFloor()
        {
            var c=PrototypeCatalog.Create();
            var child=LayoutGeometry.Attach(c.Get("stairs"),Place("s","stairs"),"upper",c.Get("corridor"),"south","b");
            Assert.That(child.Position,Is.EqualTo(P(0,8,18)));
            Assert.That(LayoutGeometry.RoomsOverlap(c.Get("stairs"),Place("s","stairs"),c.Get("corridor"),child),Is.False);
        }

        // MAP-004
        [TestCase(3,0,0,true)]
        [TestCase(4,0,0,false)]
        [TestCase(0,4,0,false)]
        [TestCase(0,3,0,true)]
        [TestCase(4,4,4,false)]
        public void PositiveIntersectionIsRejectedButTouchingAndStackingAreAllowed(int x,int y,int z,bool overlap)
        { Assert.That(LayoutGeometry.Overlaps(B(0,0,0,4,4,4),B(x,y,z,x+4,y+4,z+4)),Is.EqualTo(overlap)); }

        [Test]
        public void LShapedNotchRemainsAvailableButItsLegIsSolid()
        {
            var c=PrototypeCatalog.Create();
            var tiny=Small("tiny");
            Assert.That(LayoutGeometry.RoomsOverlap(c.Get("elbow"),Place("l","elbow"),tiny,Place("t","tiny",4,0,4)),Is.False);
            Assert.That(LayoutGeometry.RoomsOverlap(c.Get("elbow"),Place("l","elbow"),tiny,Place("t","tiny",-4,0,4)),Is.True);
        }

        // MAP-005 / MAP-006
        [Test]
        public void ValidatorAcceptsAConnectedNonOverlappingPair()
        {
            var c=new RoomCatalog("v",new[] {Small("r",false,Direction.East,Direction.West)});
            Assert.That(LayoutValidator.Validate(c,Map(c,new[] {Place("a","r"),Place("b","r",4)},Link("a","East","b","West"))).IsValid,Is.True);
        }

        [Test]
        public void ConnectionDoesNotExemptOverlappingRooms()
        {
            var c=new RoomCatalog("v",new[] {Small("r",false,Direction.East,Direction.West)});
            HasError(LayoutValidator.Validate(c,Map(c,new[] {Place("a","r"),Place("b","r",3)},Link("a","East","b","West"))),"Overlap");
        }

        [Test]
        public void RemoteDoorReferenceDoesNotCreateATeleportConnection()
        {
            var c=new RoomCatalog("v",new[] {Small("r",false,Direction.East,Direction.West)});
            HasError(LayoutValidator.Validate(c,Map(c,new[] {Place("a","r"),Place("b","r",20)},Link("a","East","b","West"))),"DoorAlignment");
        }

        [Test]
        public void DisconnectedRoomsAreRejected()
        {
            var c=new RoomCatalog("v",new[] {Small("r")});
            HasError(LayoutValidator.Validate(c,Map(c,new[] {Place("a","r"),Place("b","r",20)})),"Disconnected");
        }

        [Test]
        public void SocketCannotBeUsedTwice()
        {
            var c=new RoomCatalog("v",new[] {Small("r",false,Direction.East,Direction.West)});
            var connection=Link("a","East","b","West");
            HasError(LayoutValidator.Validate(c,Map(c,new[] {Place("a","r"),Place("b","r",4)},connection,Link("a","East","b","West","c2","d2"))),"SocketReused");
        }

        [Test]
        public void UnknownModuleAndSocketReferencesAreRejected()
        {
            var c=new RoomCatalog("v",new[] {Small("r",false,Direction.East,Direction.West)});
            HasError(LayoutValidator.Validate(c,Map(c,new[] {Place("a","missing")})),"UnknownModule");
            HasError(LayoutValidator.Validate(c,Map(c,new[] {Place("a","r"),Place("b","r",4)},Link("a","absent","b","West"))),"UnknownSocket");
        }

        [Test]
        public void DuplicateInstancesAndContentVersionMismatchAreRejected()
        {
            var c=new RoomCatalog("v",new[] {Small("r")});
            HasError(LayoutValidator.Validate(c,Map(c,new[] {Place("a","r"),Place("a","r",8)})),"DuplicateInstance");
            HasError(LayoutValidator.Validate(c,new DungeonManifest("other",new[] {Place("a","r")},Array.Empty<DoorConnection>())),"ContentVersion");
        }

        [Test]
        public void ThirdRoomCannotBlockDoorApproachEvenWithoutEnvelopeOverlap()
        {
            var a=new RoomDefinition("a","a",new[] {B(-2,0,0,2,4,1)},new[] {new DoorSocket("n",P(0,0,1),Direction.North,2,2)});
            var b=new RoomDefinition("b","b",new[] {B(-2,0,0,2,4,1)},new[] {new DoorSocket("s",P(0,0,0),Direction.South,2,2)});
            var c=new RoomDefinition("c","c",new[] {B(-2,0,0,2,4,1)},Array.Empty<DoorSocket>());
            var catalog=new RoomCatalog("v",new[] {a,b,c});
            var result=LayoutValidator.Validate(catalog,Map(catalog,new[] {Place("a","a"),Place("b","b",z:1),Place("c","c",z:2)},Link("a","n","b","s")));
            HasError(result,"BlockedDoorway");
            Assert.That(result.Errors.Any(e => e.StartsWith("Overlap",StringComparison.Ordinal)),Is.False);
        }

        // MAP-007 / MAP-009
        [Test]
        public void DeadEndIsBacktrackedBeforeSuccessfulCompletion()
        {
            var c=new RoomCatalog("v",new[] {Small("root",true,Direction.East),Small("a_dead",false,Direction.West),Small("b_corridor",false,Direction.East,Direction.West)});
            var result=DungeonGenerator.Generate(c,"root",3,new KeepOrder(),100);
            Assert.That(result.Succeeded,Is.True);
            Assert.That(result.Backtracks,Is.GreaterThan(0));
            Assert.That(result.Manifest!.Rooms.Count,Is.EqualTo(3));
            Assert.That(LayoutValidator.Validate(c,result.Manifest).IsValid,Is.True);
        }

        [Test]
        public void AttemptBudgetFailureNeverReturnsAPartialMap()
        {
            var c=PrototypeCatalog.Create();
            var result=DungeonGenerator.Generate(c,"entry",10,new XorShiftRandom(1),1,true);
            Assert.That(result.Succeeded,Is.False);
            Assert.That(result.Manifest,Is.Null);
            Assert.That(result.Attempts,Is.EqualTo(1));
            Assert.That(result.Failure,Is.EqualTo("BudgetExceeded"));
        }

        [Test]
        public void ImpossibleCatalogTerminatesWithoutClaimingSuccess()
        {
            var c=new RoomCatalog("v",new[] {Small("root",true)});
            var result=DungeonGenerator.Generate(c,"root",2,new XorShiftRandom(1),10);
            Assert.That(result.Succeeded,Is.False);
            Assert.That(result.Manifest,Is.Null);
            Assert.That(result.Attempts,Is.Zero);
            Assert.That(result.Failure,Is.EqualTo("NoLayout"));
        }

        [Test]
        public void HeightRequirementMeansConnectedFloorsNotJustTallRooms()
        {
            var c=new RoomCatalog("v",new[] {Small("root",true,Direction.East),Small("flat",false,Direction.East,Direction.West)});
            var result=DungeonGenerator.Generate(c,"root",3,new KeepOrder(),100,true);
            Assert.That(result.Succeeded,Is.False);
            Assert.That(result.Manifest,Is.Null);
        }

        [TestCase(0,100)]
        [TestCase(65,100)]
        [TestCase(2,0)]
        [TestCase(2,100001)]
        public void InvalidGenerationRequestsAreRejected(int count,int budget)
        { Assert.Throws<ArgumentOutOfRangeException>(new Action(() => DungeonGenerator.Generate(PrototypeCatalog.Create(),"entry",count,new XorShiftRandom(1),budget))); }

        // MAP-008
        [Test]
        public void RandomAlgorithmHasStableGoldenSequence()
        {
            var rng=new XorShiftRandom(1);
            Assert.That(Enumerable.Range(0,5).Select(_ => rng.Next(1000)).ToArray(),Is.EqualTo(new[] {369,689,461,695,233}));
            Assert.Throws<ArgumentOutOfRangeException>(new Action(() => rng.Next(0)));
        }

        [Test]
        public void InvalidInjectedRandomSourceIsRejected()
        { Assert.Throws<InvalidOperationException>(new Action(() => DungeonGenerator.Generate(PrototypeCatalog.Create(),"entry",3,new BadRandom()))); }

        [Test]
        public void SameSeedProducesIdenticalManifest()
        {
            var c=PrototypeCatalog.Create();
            var a=DungeonGenerator.Generate(c,"entry",12,new XorShiftRandom(17),3000,true);
            var b=DungeonGenerator.Generate(c,"entry",12,new XorShiftRandom(17),3000,true);
            Assert.That(a.Succeeded && b.Succeeded,Is.True);
            Assert.That(JsonSerializer.Serialize(a.Manifest),Is.EqualTo(JsonSerializer.Serialize(b.Manifest)));
            Assert.That(a.Attempts,Is.EqualTo(b.Attempts));
        }

        // MAP-010: DTO collections cannot change after a successful validation.
        [Test]
        public void ManifestCopiesCollectionsAndDoesNotExposeMutableLists()
        {
            var source=new List<PlacedRoom> {Place("r","box")};
            var map=new DungeonManifest("v",source,Array.Empty<DoorConnection>());
            source.Clear();
            Assert.That(map.Rooms.Count,Is.EqualTo(1));
            Assert.Throws<NotSupportedException>(new Action(() => ((IList<PlacedRoom>)map.Rooms).Clear()));
        }

        [Test]
        public void OneHundredSeedsProduceValidMultiLevelLayoutsAndPreviewFixtures()
        {
            var c=PrototypeCatalog.Create();
            var examples=new List<object>();
            var signatures=new HashSet<string>();
            for(uint seed=1;seed<=100;seed++)
            {
                var result=DungeonGenerator.Generate(c,"entry",12,new XorShiftRandom(seed),3000,true);
                Assert.That(result.Succeeded,Is.True,$"Seed {seed}: {result.Failure}");
                var map=result.Manifest!;
                Assert.That(map.Rooms.Count,Is.EqualTo(12));
                Assert.That(map.Connections.Count,Is.EqualTo(11));
                var validation=LayoutValidator.Validate(c,map);
                Assert.That(validation.IsValid,Is.True,$"Seed {seed}: "+string.Join("; ",validation.Errors));
                var lookup=map.Rooms.ToDictionary(r=>r.InstanceId);
                var heights=map.Connections.Select(link => {
                    var r=lookup[link.FromRoom];
                    return LayoutGeometry.WorldSocket(c.Get(r.ModuleId),r,link.FromSocket).Position.Y;
                }).Distinct().Count();
                Assert.That(heights,Is.GreaterThanOrEqualTo(2));
                signatures.Add(JsonSerializer.Serialize(map));
                if(seed<=6)examples.Add(new {Seed=seed,result.Attempts,result.Backtracks,Manifest=map});
            }
            Assert.That(signatures.Count,Is.GreaterThan(90));
            var path=Path.Combine(TestContext.CurrentContext.TestDirectory,"dungeon-preview.json");
            File.WriteAllText(path,JsonSerializer.Serialize(new {Catalog=c,Examples=examples},new JsonSerializerOptions {WriteIndented=true}));
            TestContext.AddTestAttachment(path,"Actual C# generator results for the room preview");
        }
    }
}
