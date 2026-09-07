using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LethalDungeon.Domain.Dungeons;
using NUnit.Framework;

namespace LethalDungeon.Tests
{
    public class DungeonLoopTests
    {
        private static PlacedRoom R(string id,int x,int z) => new PlacedRoom(id,"junction",new GridPoint(x,0,z));
        private static DoorConnection C(string id,string a,string sa,string b,string sb) => new DoorConnection(id,a,sa,b,sb,"door_"+id);
        private static DungeonManifest Square(RoomCatalog c,int lastZ=16) => new DungeonManifest(c.Version,
            new[] {R("a",0,0),R("b",16,0),R("c",16,16),R("d",0,lastZ)},new[] {
                C("1","a","east","b","west"),C("2","b","north","c","south"),C("3","c","west","d","east")});

        // MAP-011: a real four-room cycle closes a spatially aligned doorway.
        [Test]
        public void SquareClosesExactlyOnePhysicalLoop()
        {
            var c=PrototypeCatalog.CreateLoopReady();var map=Square(c);
            var links=LoopConnections.Find(c,map);
            Assert.That(links.Count,Is.EqualTo(1));
            var closed=new DungeonManifest(c.Version,map.Rooms,map.Connections.Concat(links));
            Assert.That(LayoutValidator.Validate(c,closed).IsValid,Is.True);
            Assert.That(closed.Connections.Count-closed.Rooms.Count+1,Is.EqualTo(1));
            Assert.That(LoopConnections.Find(c,closed),Is.Empty);
        }

        [Test]
        public void RemoteOrInvalidLayoutCannotCreateVirtualLoop()
        {
            var c=PrototypeCatalog.CreateLoopReady();
            Assert.Throws<ArgumentException>(new Action(()=>LoopConnections.Find(c,Square(c,20))));
        }

        [Test]
        public void ParallelDoorsBetweenTwoRoomsDoNotCountAsAUsefulLoop()
        {
            var c=PrototypeCatalog.Create();
            var a=new PlacedRoom("a","hall",new GridPoint(0,0,0));
            var b=new PlacedRoom("b","hall",new GridPoint(0,0,20));
            var map=new DungeonManifest(c.Version,new[]{a,b},new[]{C("1","a","north_01","b","south_01")});
            Assert.That(LayoutValidator.Validate(c,map).IsValid,Is.True);
            Assert.That(LoopConnections.Find(c,map),Is.Empty);
        }

        [TestCase(-1)] [TestCase(9)]
        public void InvalidCycleCountIsRejected(int count)
        { Assert.Throws<ArgumentOutOfRangeException>(new Action(()=>DungeonGenerator.Generate(PrototypeCatalog.Create(),"entry",18,new XorShiftRandom(1),minimumCycles:count))); }

        [Test]
        public void CycleRequestRequiresExplicitConnectorModule()
        { Assert.Throws<ArgumentException>(new Action(()=>DungeonGenerator.Generate(PrototypeCatalog.Create(),"entry",18,new XorShiftRandom(1),minimumCycles:2))); }

        // MAP-012: no partial tree is returned when loops cannot be made.
        [Test]
        public void ImpossibleRequestDoesNotSilentlyReturnTree()
        {
            var c=PrototypeCatalog.CreateLoopReady();
            var r=DungeonGenerator.Generate(c,"entry",3,new XorShiftRandom(1),100,false,1,"junction");
            Assert.That(r.Succeeded,Is.False);Assert.That(r.Manifest,Is.Null);Assert.That(r.Attempts,Is.LessThanOrEqualTo(100));
        }

        [Test]
        public void PlacementAndClosureShareBudget()
        {
            var r=DungeonGenerator.Generate(PrototypeCatalog.CreateLoopReady(),"entry",18,new XorShiftRandom(1),1,true,2,"junction");
            Assert.That(r.Succeeded,Is.False);Assert.That(r.Manifest,Is.Null);
            Assert.That(r.Attempts,Is.EqualTo(1));Assert.That(r.Failure,Is.EqualTo("BudgetExceeded"));
        }

        [Test]
        public void LoopSeedIsReproducible()
        {
            var c=PrototypeCatalog.CreateLoopReady();
            var a=DungeonGenerator.Generate(c,"entry",18,new XorShiftRandom(37),10000,true,2,"junction");
            var b=DungeonGenerator.Generate(c,"entry",18,new XorShiftRandom(37),10000,true,2,"junction");
            Assert.That(a.Succeeded&&b.Succeeded,Is.True);
            Assert.That(JsonSerializer.Serialize(a.Manifest),Is.EqualTo(JsonSerializer.Serialize(b.Manifest)));
            Assert.That(a.Attempts,Is.EqualTo(b.Attempts));
        }

        [Test]
        public void OneHundredSeedsContainRealLoopsAndHeightChanges()
        {
            var catalog=PrototypeCatalog.CreateLoopReady();var examples=new List<object>();var signatures=new HashSet<string>();
            int maximumAttempts=0;
            for(uint seed=1;seed<=100;seed++)
            {
                var result=DungeonGenerator.Generate(catalog,"entry",18,new XorShiftRandom(seed),10000,true,2,"junction");
                Assert.That(result.Succeeded,Is.True,$"seed {seed}: {result.Failure}, {result.Attempts}");
                var map=result.Manifest!;
                Assert.That(map.Rooms.Count,Is.EqualTo(18));
                Assert.That(map.Connections.Count-map.Rooms.Count+1,Is.GreaterThanOrEqualTo(2));
                Assert.That(LayoutValidator.Validate(catalog,map).IsValid,Is.True);
                Assert.That(result.Attempts,Is.LessThanOrEqualTo(10000));
                var heights=map.Connections.Select(c=>{var r=map.Rooms.Single(r=>r.InstanceId==c.FromRoom);return LayoutGeometry.WorldSocket(catalog.Get(r.ModuleId),r,c.FromSocket).Position.Y;}).Distinct();
                Assert.That(heights.Count(),Is.GreaterThanOrEqualTo(2));
                // Every emitted loop edge must still have an alternate path of >=3 edges without itself.
                foreach(var edge in map.Connections.Where(e=>e.Id.StartsWith("loop_",StringComparison.Ordinal)))
                {
                    var distance=new Dictionary<string,int>{{edge.FromRoom,0}};var queue=new Queue<string>();queue.Enqueue(edge.FromRoom);
                    while(queue.Count>0){var id=queue.Dequeue();foreach(var e in map.Connections.Where(e=>e.Id!=edge.Id)){
                        string? next=e.FromRoom==id?e.ToRoom:e.ToRoom==id?e.FromRoom:null;
                        if(next!=null&&!distance.ContainsKey(next)){distance[next]=distance[id]+1;queue.Enqueue(next);}
                    }}
                    Assert.That(distance.ContainsKey(edge.ToRoom),Is.True);
                    Assert.That(distance[edge.ToRoom],Is.GreaterThanOrEqualTo(3));
                }
                signatures.Add(JsonSerializer.Serialize(map));maximumAttempts=Math.Max(maximumAttempts,result.Attempts);
                if(seed<=6)examples.Add(new{Seed=seed,result.Attempts,result.Backtracks,Manifest=map});
            }
            Assert.That(signatures.Count,Is.GreaterThan(90));
            TestContext.Out.WriteLine("Maximum attempts across 100 seeds: "+maximumAttempts);
            var path=Path.Combine(TestContext.CurrentContext.TestDirectory,"dungeon-loops.json");
            File.WriteAllText(path,JsonSerializer.Serialize(new{Catalog=catalog,Examples=examples},new JsonSerializerOptions{WriteIndented=true}));
            TestContext.AddTestAttachment(path);
        }
    }
}
