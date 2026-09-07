using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using LethalDungeon.Domain.Dungeons;
using NUnit.Framework;
namespace LethalDungeon.Tests
{
    public class DungeonSeedTests
    {
        // MAP-017: fixed golden vectors, including seed 0 and uint upper boundary.
        [TestCase(0u,2,521625886u)]
        [TestCase(1u,3,2679961916u)]
        [TestCase(2u,2,3397078639u)]
        [TestCase(3u,1,543629138u)]
        [TestCase(4u,4,488595076u)]
        [TestCase(uint.MaxValue,1,306079645u)]
        public void SeedRuleHasStableGoldenMapping(uint seed,int loops,uint layout)
        {
            var plan=SeededDungeon.Resolve(seed);
            Assert.That(plan.WorldSeed,Is.EqualTo(seed));Assert.That(plan.RuleVersion,Is.EqualTo("seed-rules-v1"));
            Assert.That(plan.LoopCount,Is.EqualTo(loops));Assert.That(plan.LayoutSeed,Is.EqualTo(layout));
        }
        // MAP-018
        [TestCase(3u,24)] [TestCase(0u,40)] [TestCase(1u,56)] [TestCase(4u,64)]
        public void RoomScaleMatchesSelectedLoopCount(uint seed,int rooms)
        {Assert.That(SeededDungeon.Resolve(seed).TargetRooms,Is.EqualTo(rooms));}
        // MAP-019
        [TestCase(0)] [TestCase(100001)] public void InvalidBudgetRejected(int budget)
        {Assert.Throws<ArgumentOutOfRangeException>(new Action(()=>SeededDungeon.Generate(1,budget)));}
        [Test] public void ExhaustionKeepsSeedPlanWithoutPartialMap()
        {
            var result=SeededDungeon.Generate(4,1);
            Assert.That(result.Plan.LoopCount,Is.EqualTo(4));Assert.That(result.Result.Layout.Manifest,Is.Null);
            Assert.That(result.Result.Loops,Is.Empty);Assert.That(result.Result.Layout.Attempts,Is.EqualTo(1));
        }
        [Test] public void FullResultIsReproducible()
        {
            var a=SeededDungeon.Generate(4);var b=SeededDungeon.Generate(4);
            Assert.That(a.Result.Layout.Succeeded,Is.True);
            Assert.That(JsonSerializer.Serialize(a),Is.EqualTo(JsonSerializer.Serialize(b)));
        }
        [Test] public void HundredSeedsGenerateTheirSelectedLoopCounts()
        {
            var counts=new Dictionary<int,int>();int retries=0,maxWork=0;
            for(uint seed=0;seed<100;seed++){
                var result=SeededDungeon.Generate(seed);var plan=result.Plan;var generated=result.Result;
                Assert.That(generated.Layout.Succeeded,Is.True,$"seed {seed}, loops {plan.LoopCount}, {generated.Layout.Failure}");
                Assert.That(generated.Loops.Count,Is.EqualTo(plan.LoopCount));
                DungeonBranchLoopTests.Check(generated);
                var map=generated.Layout.Manifest!;
                Assert.That(map.Rooms.Count,Is.EqualTo(plan.TargetRooms));
                Assert.That(map.Connections.Count-map.Rooms.Count+1,Is.EqualTo(plan.LoopCount));
                Assert.That(LayoutValidator.Validate(PrototypeCatalog.CreateLoopReady(),map).IsValid,Is.True);
                Assert.That(generated.Layout.Attempts,Is.LessThanOrEqualTo(100000));
                Assert.That(result.AttemptIndex,Is.InRange(0,4));
                foreach(var loop in generated.Loops){Assert.That(loop.BranchA.Count,Is.GreaterThanOrEqualTo(4));Assert.That(loop.BranchB.Count,Is.GreaterThanOrEqualTo(4));Assert.That(loop.Bridge.Count,Is.GreaterThanOrEqualTo(2));}
                counts[plan.LoopCount]=counts.TryGetValue(plan.LoopCount,out int count)?count+1:1;
                if(result.AttemptIndex>0){retries++;TestContext.Out.WriteLine($"Retry seed: {seed}, loops: {plan.LoopCount}, variant: {result.AttemptIndex}");}maxWork=Math.Max(maxWork,generated.Layout.Attempts);
            }
            Assert.That(counts.Keys,Is.EquivalentTo(new[]{1,2,3,4}));
            TestContext.Out.WriteLine("Distribution: "+JsonSerializer.Serialize(counts)+"; retried seeds: "+retries+"; max attempts: "+maxWork);
        }
    }
}
