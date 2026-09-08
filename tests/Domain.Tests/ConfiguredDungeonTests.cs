using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using LethalDungeon.Domain.Dungeons;
using LethalDungeon.CatalogJson;
using NUnit.Framework;
namespace LethalDungeon.Tests
{
 public class ConfiguredDungeonTests
 {
  static string Json=>File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory,"base-rooms.json"));
  // JSON-001, JSON-002, JSON-003; MAP-028, MAP-029, MAP-030, MAP-031, MAP-032
  [Test] public void ImportsThirteenConfiguredModules(){var c=RoomCatalogJson.Parse(Json);Assert.That(c.Patterns.Count,Is.EqualTo(13));Assert.That(c.Patterns.Single(p=>p.Room.Id=="ramp_l_left").Cells.Count,Is.EqualTo(3));}
  [TestCase("version")] [TestCase("rotation")] [TestCase("unknown")] [TestCase("template")] [TestCase("resource")] [TestCase("required")] [TestCase("groups")] [TestCase("unit")]
  public void RejectsUnsupportedConfiguration(string kind){var d=JsonNode.Parse(Json)!;var r=d["rooms"]![0]!;
   switch(kind){case "version":d["schemaVersion"]="bad";break;case "rotation":r["allowedRotations"]=new JsonArray(45);break;case "unknown":r["magic"]=true;break;case "template":r["matching"]!["socketCells"]!["south"]=99;break;case "resource":r["prefabKey"]="";break;case "required":r["connectionPolicy"]!["requiredSockets"]=new JsonArray();break;case "groups":r["traversalGroups"]=new JsonArray();break;case "unit":d["metresPerUnit"]=1;break;}
   Assert.Throws<ArgumentException>(new Action(()=>RoomCatalogJson.Parse(d.ToJsonString())));
  }
  [Test] public void RejectsDuplicateJsonKeys(){Assert.Throws<ArgumentException>(new Action(()=>RoomCatalogJson.Parse(Json.Replace("\"revision\": 1","\"revision\": 1, \"revision\": 2"))));}
  internal static void Check(ConfiguredRoomCatalog c,ConfiguredDungeonResult g,int loops){
   Assert.That(g.Layout.Succeeded,Is.True,g.Layout.Failure);var map=g.Layout.Manifest!;var grid=g.Grid!;
   Assert.That(LayoutValidator.Validate(c.Geometry,map).IsValid,Is.True);
   Assert.That(map.Connections.Count-map.Rooms.Count+1,Is.EqualTo(loops));
   Assert.That(g.Coverage.SelectMany(x=>x.CellIds).Distinct().Count(),Is.EqualTo(grid.Rooms.Count));
   Assert.That(g.Coverage.Sum(x=>x.CellIds.Count),Is.EqualTo(grid.Rooms.Count));
   foreach(var room in map.Rooms){var p=c.Patterns.Single(p=>p.Room.Id==room.ModuleId);Assert.That(p.Rotations,Does.Contain(room.QuarterTurns));Assert.That(map.Connections.Count(e=>e.FromRoom==room.InstanceId||e.ToRoom==room.InstanceId),Is.EqualTo(p.Room.Sockets.Count));}
   var owner=g.Coverage.SelectMany(x=>x.CellIds.Select(id=>(id,x.InstanceId))).ToDictionary(x=>x.id,x=>x.InstanceId);
   var boundary=grid.Connections.Where(e=>owner[e.FromRoom]!=owner[e.ToRoom]).ToArray();Assert.That(boundary.Length,Is.EqualTo(map.Connections.Count));
   foreach(var edge in boundary)Assert.That(map.Connections.Any(e=>(e.FromRoom==owner[edge.FromRoom]&&e.ToRoom==owner[edge.ToRoom])||(e.ToRoom==owner[edge.FromRoom]&&e.FromRoom==owner[edge.ToRoom])),Is.True);
   foreach(var loop in g.GridLoops){var ids=loop.BranchA.Concat(loop.BranchB).Concat(loop.Bridge).Append(loop.ForkRoom).ToHashSet();Assert.That(grid.Rooms.Where(r=>ids.Contains(r.InstanceId)).Select(r=>r.Position.Y).Distinct().Count(),Is.GreaterThanOrEqualTo(2));}
   foreach(var branch in g.GridBranches){var end=branch.Rooms.Last();Assert.That(grid.Connections.Count(e=>e.FromRoom==end||e.ToRoom==end),Is.EqualTo(1));}
  }
  [Test] public void JsonNamesDriveActualOutput(){var d=JsonNode.Parse(Json)!;foreach(var r in d["rooms"]!.AsArray()){r!["id"]="custom_"+r["id"]!.GetValue<string>();}var c=RoomCatalogJson.Parse(d.ToJsonString());var g=ConfiguredDungeon.Generate(c,5,maxAttempts:100000);Check(c,g,4);Assert.That(g.Layout.Manifest!.Rooms.All(r=>r.ModuleId.StartsWith("custom_")),Is.True);}
  [Test] public void ComplexTemplatesAndLSlopesAreUsed(){var c=RoomCatalogJson.Parse(Json);var g=ConfiguredDungeon.Generate(c,5,maxAttempts:100000);Check(c,g,4);Assert.That(g.Coverage.Any(x=>x.CellIds.Count==3),Is.True);Assert.That(g.Layout.Manifest!.Rooms.Any(r=>r.ModuleId.StartsWith("ramp_l_")),Is.True);}
  [Test] public void RemovingOptionalLargeRoomsUsesFallback(){var d=JsonNode.Parse(Json)!;var rooms=d["rooms"]!.AsArray();foreach(var r in rooms.Where(r=>r!["id"]!.GetValue<string>().StartsWith("room_l_")).ToArray())rooms.Remove(r);var c=RoomCatalogJson.Parse(d.ToJsonString());Check(c,ConfiguredDungeon.Generate(c,5,maxAttempts:100000),4);}
  [Test] public void MissingFallbackFailsWithoutHiddenRooms(){var d=JsonNode.Parse(Json)!;var rooms=d["rooms"]!.AsArray();rooms.Remove(rooms.First(r=>r!["id"]!.GetValue<string>()=="dead_end"));var g=ConfiguredDungeon.Generate(RoomCatalogJson.Parse(d.ToJsonString()),5,maxAttempts:100000);Assert.That(g.Layout.Succeeded,Is.False);Assert.That(g.Layout.Failure,Is.EqualTo("MissingFallback"));Assert.That(g.Coverage,Is.Empty);Assert.That(g.Grid,Is.Null);}
  [Test] public void BudgetCannotReturnPartialMap(){var g=ConfiguredDungeon.Generate(RoomCatalogJson.Parse(Json),5,1);Assert.That(g.Layout.Succeeded,Is.False);Assert.That(g.Layout.Attempts,Is.LessThanOrEqualTo(1));Assert.That(g.Grid,Is.Null);Assert.That(g.Coverage,Is.Empty);}
  [Test] public void SameJsonAndSeedReproduce(){var c=RoomCatalogJson.Parse(Json);var a=ConfiguredDungeon.Generate(c,5,maxAttempts:100000);Check(c,a,4);Assert.That(JsonSerializer.Serialize(a),Is.EqualTo(JsonSerializer.Serialize(ConfiguredDungeon.Generate(c,5,maxAttempts:100000))));}
  [Test] public void HundredConfiguredSeeds(){var c=RoomCatalogJson.Parse(Json);var examples=new List<object>();int max=0;for(uint seed=0;seed<100;seed++){var g=ConfiguredDungeon.Generate(c,seed,maxAttempts:100000);Assert.That(g.Layout.Succeeded,Is.True,$"seed {seed}: {g.Layout.Failure}, attempts {g.Layout.Attempts}");Check(c,g,SeededDungeon.Resolve(seed).LoopCount);max=Math.Max(max,g.Layout.Attempts);if(seed==5)examples.Add(new{Seed=seed,Manifest=g.Layout.Manifest,g.Grid,g.Coverage,g.GridLoops,g.GridBranches,g.Layout.Attempts});}TestContext.Out.WriteLine("Configured maximum attempts: "+max);var path=Path.Combine(TestContext.CurrentContext.TestDirectory,"dungeon-configured.json");File.WriteAllText(path,JsonSerializer.Serialize(new{Catalog=c.Geometry,Examples=examples},new JsonSerializerOptions{WriteIndented=true}));TestContext.AddTestAttachment(path);}
 }
}
