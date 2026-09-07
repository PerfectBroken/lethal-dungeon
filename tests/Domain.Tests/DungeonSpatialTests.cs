using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LethalDungeon.Domain.Dungeons;
using NUnit.Framework;
namespace LethalDungeon.Tests
{
    public class DungeonSpatialTests
    {
        // MAP-024, MAP-025, MAP-026, MAP-027
        [TestCase(3u)] [TestCase(0u)] [TestCase(1u)] [TestCase(4u)] [TestCase(5u)]
        public void EveryMainRingCrossesFloors(uint seed) { Check(SeededDungeon.Generate(seed)); }
        private static void Check(SeededDungeonResult g)
        {
            Assert.That(g.Result.Layout.Succeeded,Is.True,$"seed {g.Plan.WorldSeed}: {g.Result.Layout.Failure}");
            Assert.That(g.Plan.RuleVersion,Is.EqualTo("seed-rules-v3"));
            var map=g.Result.Layout.Manifest!;var catalog=PrototypeCatalog.CreateSpatial();
            DungeonBranchLoopTests.Check(g.Result,catalog);
            Assert.That(map.GeneratorVersion,Is.EqualTo("spatial-main-paths-v0.5"));
            Assert.That(map.Rooms.Count,Is.EqualTo(g.Plan.TargetRooms));
            Assert.That(g.Result.Layout.Attempts,Is.LessThanOrEqualTo(100000));
            var rooms=map.Rooms.ToDictionary(r=>r.InstanceId);
            for(int i=0;i<g.Result.Loops.Count;i++) {
                var t=g.Result.Loops[i];var ids=t.BranchA.Concat(t.BranchB).Concat(t.Bridge).Append(t.ForkRoom).ToHashSet();
                var main=ids.Select(id=>rooms[id]).ToArray();
                var heights=main.Where(r=>r.ModuleId=="junction").GroupBy(r=>r.Position.Y).ToArray();
                Assert.That(heights.Count(h=>h.Count()>=2),Is.GreaterThanOrEqualTo(2),$"flat ring {i}");
                foreach(var type in new[]{"routing_stairs","routing_ramp"}) {
                    var transition=main.Single(r=>r.ModuleId==type);
                    var edges=map.Connections.Where(e=>(e.FromRoom==transition.InstanceId||e.ToRoom==transition.InstanceId)&&ids.Contains(e.FromRoom)&&ids.Contains(e.ToRoom)).ToArray();
                    Assert.That(edges.Length,Is.EqualTo(2));
                    var ys=edges.Select(e=>LayoutGeometry.WorldSocket(catalog.Get(transition.ModuleId),transition,e.FromRoom==transition.InstanceId?e.FromSocket:e.ToSocket).Position.Y).OrderBy(y=>y).ToArray();
                    Assert.That(ys[1]-ys[0],Is.EqualTo(8));
                }
                var branches=g.Result.ExplorationBranches.Where(b=>b.LoopIndex==i).ToArray();Assert.That(branches.Length,Is.EqualTo(2));
                Assert.That(branches.Any(b=>rooms[b.AnchorRoom].Position.Y>heights.Min(h=>h.Key)),Is.True,"missing elevated exploration branch");
                foreach(var b in branches){
                    Assert.That(b.Rooms.Count,Is.InRange(2,4));string previous=b.AnchorRoom;
                    for(int j=0;j<b.Rooms.Count;j++){
                        var id=b.Rooms[j];Assert.That(map.Connections.Count(e=>e.FromRoom==id||e.ToRoom==id),Is.EqualTo(j==b.Rooms.Count-1?1:2));
                        Assert.That(map.Connections.Any(e=>(e.FromRoom==id&&e.ToRoom==previous)||(e.ToRoom==id&&e.FromRoom==previous)),Is.True);previous=id;
                    }
                }
            }
        }
        [Test] public void SpatialSeedRepeatsExactly()
        {var a=SeededDungeon.Generate(5);Check(a);Assert.That(JsonSerializer.Serialize(a),Is.EqualTo(JsonSerializer.Serialize(SeededDungeon.Generate(5))));}
        [Test] public void BudgetFailureHasNoMapOrTraces()
        {var g=SeededDungeon.Generate(5,1);Assert.That(g.Plan.RuleVersion,Is.EqualTo("seed-rules-v3"));Assert.That(g.Result.Layout.Attempts,Is.EqualTo(1));Assert.That(g.Result.Layout.Manifest,Is.Null);Assert.That(g.Result.Loops,Is.Empty);Assert.That(g.Result.ExplorationBranches,Is.Empty);}
        [Test] public void HundredSpatialSeeds()
        {
            var examples=new List<object>();int maximum=0;
            for(uint seed=0;seed<100;seed++){
                var g=SeededDungeon.Generate(seed);Check(g);maximum=Math.Max(maximum,g.Result.Layout.Attempts);
                if(seed==4||seed==5)examples.Add(new{Seed=seed,Manifest=g.Result.Layout.Manifest,g.Result.Loops,g.Result.ExplorationBranches,g.Plan,Attempts=g.Result.Layout.Attempts});
            }
            TestContext.Out.WriteLine("Spatial maximum attempts: "+maximum);
            var path=Path.Combine(TestContext.CurrentContext.TestDirectory,"dungeon-spatial.json");
            File.WriteAllText(path,JsonSerializer.Serialize(new{Catalog=PrototypeCatalog.CreateSpatial(),Examples=examples},new JsonSerializerOptions{WriteIndented=true}));TestContext.AddTestAttachment(path);
        }
    }
}
