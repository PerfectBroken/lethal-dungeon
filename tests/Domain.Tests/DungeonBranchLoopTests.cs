using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LethalDungeon.Domain.Dungeons;
using NUnit.Framework;
namespace LethalDungeon.Tests
{
    public class DungeonBranchLoopTests
    {
        // MAP-013, MAP-015: inspect the actual graph, not a reported cycle counter.
        private static void Check(BranchLoopResult result)
        {
            Assert.That(result.Layout.Succeeded,Is.True,result.Layout.Failure);
            var map=result.Layout.Manifest!;
            Assert.That(LayoutValidator.Validate(PrototypeCatalog.CreateLoopReady(),map).IsValid,Is.True);
            Assert.That(map.Connections.Count-map.Rooms.Count+1,Is.EqualTo(result.Loops.Count));
            foreach(var trace in result.Loops)
            {
                Assert.That(trace.BranchA.Count,Is.GreaterThanOrEqualTo(4));
                Assert.That(trace.BranchB.Count,Is.GreaterThanOrEqualTo(4));
                Assert.That(trace.Bridge.Count,Is.GreaterThanOrEqualTo(2));
                var members=trace.BranchA.Concat(trace.BranchB).Concat(trace.Bridge).Append(trace.ForkRoom).ToArray();
                Assert.That(members.Distinct().Count(),Is.EqualTo(members.Length));
                var closing=map.Connections.Single(e=>e.Id==trace.ClosingConnection);
                var tree=map.Connections.Where(e=>e.Id!=trace.ClosingConnection).ToArray();
                Assert.That(Distance(tree,closing.FromRoom,closing.ToRoom),Is.GreaterThanOrEqualTo(10));
                // A traced branch must be a real chain from the fork.
                foreach(var branch in new[]{trace.BranchA,trace.BranchB})
                {
                    string previous=trace.ForkRoom;
                    foreach(var id in branch){Assert.That(map.Connections.Any(e=>Joins(e,previous,id)),Is.True);previous=id;}
                }
                // Reconstruct the pre-bridge graph within this fork's two branches.
                var pre=new HashSet<string>(trace.BranchA.Concat(trace.BranchB).Append(trace.ForkRoom));
                var edges=map.Connections.Where(e=>pre.Contains(e.FromRoom)&&pre.Contains(e.ToRoom)).ToArray();
                foreach(var end in new[]{trace.BranchA.Last(),trace.BranchB.Last()})
                    Assert.That(edges.Count(e=>e.FromRoom==end||e.ToRoom==end),Is.EqualTo(1));
                Assert.That(Distance(edges,trace.BranchA.Last(),trace.BranchB.Last()),Is.GreaterThanOrEqualTo(8));
                var route=trace.Bridge.Prepend(trace.BranchA.Last()).Append(trace.BranchB.Last()).ToArray();
                for(int i=1;i<route.Length;i++)Assert.That(map.Connections.Any(e=>Joins(e,route[i-1],route[i])),Is.True);
            }
            // No hidden 4-room shortcuts anywhere: remove each edge and inspect its alternate path.
            foreach(var edge in map.Connections)
            {
                int alternate=Distance(map.Connections.Where(e=>e.Id!=edge.Id),edge.FromRoom,edge.ToRoom);
                Assert.That(alternate==-1||alternate>=10,Is.True,$"short cycle at {edge.Id}: {alternate+1}");
            }
        }
        private static bool Joins(DoorConnection e,string a,string b)=>(e.FromRoom==a&&e.ToRoom==b)||(e.FromRoom==b&&e.ToRoom==a);
        private static int Distance(IEnumerable<DoorConnection> input,string start,string end)
        {
            var edges=input.ToArray();var dist=new Dictionary<string,int>{{start,0}};var queue=new Queue<string>();queue.Enqueue(start);
            while(queue.Count>0){var current=queue.Dequeue();foreach(var edge in edges){
                string? next=edge.FromRoom==current?edge.ToRoom:edge.ToRoom==current?edge.FromRoom:null;
                if(next==null||dist.ContainsKey(next))continue;dist[next]=dist[current]+1;if(next==end)return dist[next];queue.Enqueue(next);
            }}return -1;
        }
        [Test] public void ExtensionPreservesExistingIdsWithoutNamingCollisions()
        {
            var c=PrototypeCatalog.CreateLoopReady();
            var initial=new DungeonManifest(c.Version,new[]{new PlacedRoom("room_001","entry",new GridPoint(0,0,0))},Array.Empty<DoorConnection>());
            var result=DungeonGenerator.Generate(c,"entry",2,new XorShiftRandom(1),100,initialLayout:initial);
            Assert.That(result.Succeeded,Is.True);Assert.That(result.Manifest!.Rooms[0].InstanceId,Is.EqualTo("room_001"));
            Assert.That(result.Manifest.Rooms.Select(r=>r.InstanceId).Distinct().Count(),Is.EqualTo(2));
        }
        [Test] public void ExtensionRejectsInvalidSeedLayout()
        {
            var c=PrototypeCatalog.CreateLoopReady();var invalid=new DungeonManifest(c.Version,new[]{new PlacedRoom("a","junction",new GridPoint(0,0,0))},Array.Empty<DoorConnection>());
            Assert.Throws<ArgumentException>(new Action(()=>DungeonGenerator.Generate(c,"entry",40,new XorShiftRandom(1),initialLayout:invalid)));
        }
        [Test] public void TwoLongBranchEndsAreJoinedThroughARealBridge(){Check(BranchLoopGenerator.Generate(new XorShiftRandom(1)));}
        // MAP-016
        [Test] public void FailureHasNoPartialMapOrTrace()
        {
            var r=BranchLoopGenerator.Generate(new XorShiftRandom(1),maxAttempts:1);
            Assert.That(r.Layout.Manifest,Is.Null);Assert.That(r.Loops,Is.Empty);Assert.That(r.Layout.Attempts,Is.EqualTo(1));
        }
        [TestCase(0)] [TestCase(100001)] public void InvalidBudget(int budget)
        {Assert.Throws<ArgumentOutOfRangeException>(new Action(()=>BranchLoopGenerator.Generate(new XorShiftRandom(1),maxAttempts:budget)));}
        [Test] public void SameSeedRepeatsBranchAndBridgeLayout()
        {
            var a=BranchLoopGenerator.Generate(new XorShiftRandom(17));var b=BranchLoopGenerator.Generate(new XorShiftRandom(17));Check(a);Check(b);
            Assert.That(JsonSerializer.Serialize(a),Is.EqualTo(JsonSerializer.Serialize(b)));
        }
        // MAP-014, MAP-015, MAP-016
        [Test] public void HundredSeedsHaveLongCyclesAndPreviewTraces()
        {
            var examples=new List<object>();var signatures=new HashSet<string>();int maximum=0;
            for(uint seed=1;seed<=100;seed++){
                var result=BranchLoopGenerator.Generate(new XorShiftRandom(seed));
                Assert.That(result.Layout.Succeeded,Is.True,$"seed {seed}: {result.Layout.Failure}, attempts {result.Layout.Attempts}");Check(result);
                var map=result.Layout.Manifest!;Assert.That(map.Rooms.Count,Is.EqualTo(40));Assert.That(result.Loops.Count,Is.EqualTo(2));
                var catalog=PrototypeCatalog.CreateLoopReady();
                var heights=map.Connections.Select(e=>{var r=map.Rooms.Single(r=>r.InstanceId==e.FromRoom);return LayoutGeometry.WorldSocket(catalog.Get(r.ModuleId),r,e.FromSocket).Position.Y;}).Distinct();
                Assert.That(heights.Count(),Is.GreaterThanOrEqualTo(2));
                Assert.That(result.Layout.Attempts,Is.LessThanOrEqualTo(10000));maximum=Math.Max(maximum,result.Layout.Attempts);
                signatures.Add(JsonSerializer.Serialize(map));if(seed<=6)examples.Add(new{Seed=seed,Manifest=map,result.Loops,result.Layout.Attempts});
            }
            Assert.That(signatures.Count,Is.GreaterThan(90));TestContext.Out.WriteLine("Maximum attempts: "+maximum);
            var path=Path.Combine(TestContext.CurrentContext.TestDirectory,"dungeon-branch-loops.json");
            File.WriteAllText(path,JsonSerializer.Serialize(new{Catalog=PrototypeCatalog.CreateLoopReady(),Examples=examples},new JsonSerializerOptions{WriteIndented=true}));TestContext.AddTestAttachment(path);
        }
    }
}
