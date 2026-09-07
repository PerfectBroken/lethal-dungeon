using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using LethalDungeon.Domain.Dungeons;
using NUnit.Framework;
namespace LethalDungeon.Tests
{
    public class DungeonExplorationBranchTests
    {
        // MAP-023: preserve ring derivation while explicitly versioning the larger room budgets.
        [TestCase(3u,1,32)] [TestCase(0u,2,56)] [TestCase(1u,3,80)] [TestCase(4u,4,104)]
        public void DefaultPlanReservesExplorationScale(uint seed,int loops,int rooms)
        {
            var p=SeededDungeon.Resolve(seed);var old=SeededDungeon.Resolve(seed,"seed-rules-v1");
            Assert.That(p.RuleVersion,Is.EqualTo("seed-rules-v2"));Assert.That(p.LoopCount,Is.EqualTo(loops));Assert.That(p.TargetRooms,Is.EqualTo(rooms));
            Assert.That(p.LayoutSeed,Is.EqualTo(old.LayoutSeed));Assert.That(p.BranchesPerLoop,Is.EqualTo(2));
            Assert.That(p.MinBranchDepth,Is.EqualTo(2));Assert.That(p.MaxBranchDepth,Is.EqualTo(4));
        }
        [Test] public void UnknownRuleVersionRejected()
        {Assert.Throws<ArgumentException>(new Action(()=>SeededDungeon.Resolve(4,"unknown")));}
        [TestCase(0,2,4)] [TestCase(5,2,4)] [TestCase(2,0,4)] [TestCase(2,4,2)] [TestCase(2,2,9)]
        public void InvalidBranchConfigurationRejected(int count,int min,int max)
        {Assert.Throws<ArgumentOutOfRangeException>(new Action(()=>new ExplorationBranchOptions(count,min,max)));}
        // MAP-021
        [Test] public void InsufficientRoomBudgetCannotSilentlyOmitBranches()
        {
            var r=BranchLoopGenerator.Generate(new XorShiftRandom(1),14,1,500,true,new ExplorationBranchOptions(2,4,4));
            Assert.That(r.Layout.Succeeded,Is.False);Assert.That(r.Layout.Manifest,Is.Null);
            Assert.That(r.Loops,Is.Empty);Assert.That(r.ExplorationBranches,Is.Empty);Assert.That(r.Layout.Attempts,Is.LessThanOrEqualTo(500));
        }
        [Test] public void ExhaustionReturnsNoPartialExplorationTrace()
        {
            var r=SeededDungeon.Generate(4,1);Assert.That(r.Result.Layout.Manifest,Is.Null);Assert.That(r.Result.ExplorationBranches,Is.Empty);
            Assert.That(r.Result.Layout.Attempts,Is.EqualTo(1));Assert.That(r.Plan.BranchesPerLoop,Is.EqualTo(2));
        }
        [Test] public void NewModeIsReproducible()
        {
            var a=SeededDungeon.Generate(5);var b=SeededDungeon.Generate(5);Assert.That(a.Result.Layout.Succeeded,Is.True);
            Assert.That(JsonSerializer.Serialize(a),Is.EqualTo(JsonSerializer.Serialize(b)));
        }
        [Test] public void UnknownProtectedRoomIsRejected()
        {
            Assert.Throws<ArgumentException>(new Action(()=>DungeonGenerator.Generate(PrototypeCatalog.CreateLoopReady(),"entry",3,new XorShiftRandom(1),protectedRoomIds:new[]{"missing"})));
        }
        [TestCase(1,2,2)] [TestCase(3,2,3)]
        public void ExplicitBranchConfigurationIsHonoured(int count,int min,int max)
        {
            var r=BranchLoopGenerator.Generate(new XorShiftRandom(7),40,1,10000,true,new ExplorationBranchOptions(count,min,max));
            Assert.That(r.Layout.Succeeded,Is.True);Assert.That(r.ExplorationBranches.Count,Is.EqualTo(count));
            foreach(var branch in r.ExplorationBranches){
                Assert.That(branch.Rooms.Count,Is.InRange(min,max));var end=branch.Rooms.Last();
                Assert.That(r.Layout.Manifest!.Connections.Count(e=>e.FromRoom==end||e.ToRoom==end),Is.EqualTo(1));
            }
        }
        // MAP-020, MAP-022: inspect final edges and degrees, not merely returned counts.
        private static void Check(SeededDungeonResult generated)
        {
            var r=generated.Result;var map=r.Layout.Manifest!;DungeonBranchLoopTests.Check(r);
            Assert.That(map.GeneratorVersion,Is.EqualTo("exploration-branches-v0.4"));
            Assert.That(r.ExplorationBranches.Count,Is.EqualTo(generated.Plan.LoopCount*2));
            var degree=map.Rooms.ToDictionary(p=>p.InstanceId,p=>map.Connections.Count(e=>e.FromRoom==p.InstanceId||e.ToRoom==p.InstanceId));
            var ringRooms=new HashSet<string>(r.Loops.SelectMany(t=>t.BranchA.Concat(t.BranchB).Concat(t.Bridge).Append(t.ForkRoom)));
            var claimed=new HashSet<string>();var anchors=new HashSet<string>();
            for(int i=0;i<r.Loops.Count;i++)Assert.That(r.ExplorationBranches.Count(b=>b.LoopIndex==i),Is.EqualTo(2));
            foreach(var branch in r.ExplorationBranches){
                Assert.That(branch.Rooms.Count,Is.InRange(2,4));Assert.That(anchors.Add(branch.AnchorRoom),Is.True);
                var t=r.Loops[branch.LoopIndex];Assert.That(t.BranchA.Concat(t.BranchB).Concat(t.Bridge).Append(t.ForkRoom),Does.Contain(branch.AnchorRoom));
                Assert.That(degree[branch.AnchorRoom],Is.GreaterThanOrEqualTo(3));string previous=branch.AnchorRoom;
                for(int i=0;i<branch.Rooms.Count;i++){
                    string id=branch.Rooms[i];Assert.That(ringRooms.Contains(id),Is.False);Assert.That(claimed.Add(id),Is.True);
                    Assert.That(map.Connections.Count(e=>(e.FromRoom==previous&&e.ToRoom==id)||(e.FromRoom==id&&e.ToRoom==previous)),Is.EqualTo(1));
                    Assert.That(degree[id],Is.EqualTo(i==branch.Rooms.Count-1?1:2));previous=id;
                }
            }
            Assert.That(degree.Count(p=>p.Key!=map.Rooms[0].InstanceId&&p.Value==1),Is.GreaterThanOrEqualTo(r.ExplorationBranches.Count));
        }
        [Test] public void HundredSeedsHaveReservedBranchesAndRealDeadEnds()
        {
            var examples=new List<object>();int max=0;
            for(uint seed=0;seed<100;seed++){
                var g=SeededDungeon.Generate(seed);Assert.That(g.Result.Layout.Succeeded,Is.True,$"seed {seed}: {g.Result.Layout.Failure}, attempts {g.Result.Layout.Attempts}");Check(g);
                Assert.That(g.Result.Layout.Manifest!.Rooms.Count,Is.EqualTo(g.Plan.TargetRooms));
                var map=g.Result.Layout.Manifest!;var catalog=PrototypeCatalog.CreateLoopReady();
                Assert.That(map.Connections.Select(e=>{var room=map.Rooms.Single(r=>r.InstanceId==e.FromRoom);return LayoutGeometry.WorldSocket(catalog.Get(room.ModuleId),room,e.FromSocket).Position.Y;}).Distinct().Count(),Is.GreaterThanOrEqualTo(2));
                Assert.That(g.Result.Layout.Attempts,Is.LessThanOrEqualTo(100000));max=Math.Max(max,g.Result.Layout.Attempts);
                if(seed==4||seed==5)examples.Add(new{Seed=seed,Manifest=g.Result.Layout.Manifest,Loops=g.Result.Loops,ExplorationBranches=g.Result.ExplorationBranches,Plan=g.Plan,Attempts=g.Result.Layout.Attempts});
            }
            TestContext.Out.WriteLine("Maximum attempts: "+max);
            var path=Path.Combine(TestContext.CurrentContext.TestDirectory,"dungeon-exploration.json");
            File.WriteAllText(path,JsonSerializer.Serialize(new{Catalog=PrototypeCatalog.CreateLoopReady(),Examples=examples},new JsonSerializerOptions{WriteIndented=true}));TestContext.AddTestAttachment(path);
        }
    }
}
