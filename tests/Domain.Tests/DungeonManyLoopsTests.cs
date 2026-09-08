using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using LethalDungeon.Domain.Dungeons;
using LethalDungeon.CatalogJson;
using NUnit.Framework;
namespace LethalDungeon.Tests
{
 public class DungeonManyLoopsTests
 {
  static ConfiguredRoomCatalog Catalog()=>RoomCatalogJson.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"base-rooms.json")));
  // MAP-033, MAP-034
  [TestCase(0,12,300000)] [TestCase(1,13,300000)] [TestCase(8,3,300000)] [TestCase(1,12,0)] [TestCase(1,12,1000001)]
  public void InvalidOptionsRejected(int min,int max,int budget){Assert.Throws<ArgumentOutOfRangeException>(new Action(()=>new DungeonGenerationOptions(min,max,budget)));}
  [Test] public void DefaultSeedRangeIncludesEveryLoopCount(){var counts=new HashSet<int>();for(uint s=0;s<100;s++){var p=ConfiguredDungeon.ResolvePlan(s);Assert.That(p.MinLoops,Is.EqualTo(1));Assert.That(p.MaxLoops,Is.EqualTo(12));Assert.That(p.LoopCount,Is.InRange(1,12));Assert.That(p.TargetCells,Is.EqualTo(8+32*p.LoopCount));counts.Add(p.LoopCount);}Assert.That(counts.OrderBy(n=>n),Is.EqualTo(Enumerable.Range(1,12)));}
  [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)] [TestCase(8)] [TestCase(9)] [TestCase(10)] [TestCase(11)] [TestCase(12)]
  public void ExplicitLoopCountIsExact(int count){var c=Catalog();var g=ConfiguredDungeon.Generate(c,5,new DungeonGenerationOptions(count,count));Check(c,g,count);}
  // MAP-035, MAP-036
  static void Check(ConfiguredRoomCatalog c,ConfiguredDungeonResult g,int count){
   ConfiguredDungeonTests.Check(c,g,count);Assert.That(g.Plan,Is.Not.Null);Assert.That(g.Plan!.LoopCount,Is.EqualTo(count));Assert.That(g.Plan.RuleVersion,Is.EqualTo("configured-grid-v0.7"));Assert.That(g.Layout.Manifest!.GeneratorVersion,Is.EqualTo(g.Plan.RuleVersion));
   Assert.That(g.Grid!.Rooms.Count,Is.EqualTo(g.Plan.TargetCells));Assert.That(g.GridLoops.Count,Is.EqualTo(count));Assert.That(g.GridBranches.Count,Is.EqualTo(count*2));Assert.That(g.Layout.Attempts,Is.LessThanOrEqualTo(g.Plan.MaxAttempts));
   foreach(var b in g.GridBranches)Assert.That(b.Rooms.Count,Is.InRange(2,4));
   for(int i=0;i<count;i++)Assert.That(g.GridBranches.Count(b=>b.LoopIndex==i),Is.EqualTo(2));
  }
  [Test] public void TwelveLoopFailureDoesNotDowngrade(){var g=ConfiguredDungeon.Generate(Catalog(),5,new DungeonGenerationOptions(12,12,1));Assert.That(g.Layout.Manifest,Is.Null);Assert.That(g.Grid,Is.Null);Assert.That(g.Coverage,Is.Empty);Assert.That(g.GridLoops,Is.Empty);Assert.That(g.GridBranches,Is.Empty);Assert.That(g.Plan!.LoopCount,Is.EqualTo(12));Assert.That(g.Layout.Attempts,Is.LessThanOrEqualTo(1));}
  [Test] public void TwelveLoopCrowdedSeedCompletes(){var c=Catalog();Check(c,ConfiguredDungeon.Generate(c,4,new DungeonGenerationOptions(12,12)),12);}
  [Test] public void TwelveLoopSeedRepeats(){var c=Catalog();var o=new DungeonGenerationOptions(12,12);var a=ConfiguredDungeon.Generate(c,5,o);Check(c,a,12);Assert.That(JsonSerializer.Serialize(a),Is.EqualTo(JsonSerializer.Serialize(ConfiguredDungeon.Generate(c,5,o))));}
  [Test] public void EachScalePassesTenSeeds(){var c=Catalog();var examples=new List<object>();int max=0;for(int count=1;count<=12;count++)for(uint seed=0;seed<10;seed++){var g=ConfiguredDungeon.Generate(c,seed,new DungeonGenerationOptions(count,count));Assert.That(g.Layout.Succeeded,Is.True,$"count={count}, seed={seed}, {g.Layout.Failure}, attempts={g.Layout.Attempts}");Check(c,g,count);max=Math.Max(max,g.Layout.Attempts);if(seed==5&&(count==6||count==8||count==12))examples.Add(new{Seed=seed,Manifest=g.Layout.Manifest,g.Grid,g.Coverage,g.GridLoops,g.GridBranches,g.Plan,g.Layout.Attempts});}TestContext.Out.WriteLine("120 layouts; maximum attempts: "+max);var file=Path.Combine(TestContext.CurrentContext.TestDirectory,"dungeon-many-loops.json");File.WriteAllText(file,JsonSerializer.Serialize(new{Catalog=c.Geometry,Examples=examples},new JsonSerializerOptions{WriteIndented=true}));TestContext.AddTestAttachment(file);}
 }
}
