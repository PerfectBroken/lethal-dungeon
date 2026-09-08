using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace LethalDungeon.Domain.Dungeons
{
    public sealed class RoomPattern
    {
        public RoomDefinition Room {get;}
        public string PrefabKey {get;}
        public int Revision {get;}
        public ReadOnlyCollection<int> Rotations {get;}
        public ReadOnlyCollection<GridPoint> Cells {get;}
        public ReadOnlyCollection<(int A,int B)> Edges {get;}
        public IReadOnlyDictionary<string,int> SocketCells {get;}
        public RoomPattern(RoomDefinition room,string prefabKey,int revision,IEnumerable<int> rotations,IEnumerable<GridPoint> cells,IEnumerable<(int A,int B)> edges,IDictionary<string,int> socketCells)
        {Room=room;PrefabKey=prefabKey;Revision=revision;Rotations=rotations.ToList().AsReadOnly();Cells=cells.ToList().AsReadOnly();Edges=edges.ToList().AsReadOnly();SocketCells=new ReadOnlyDictionary<string,int>(new Dictionary<string,int>(socketCells));}
    }
    public sealed class ConfiguredRoomCatalog
    {
        public RoomCatalog Geometry {get;}
        public ReadOnlyCollection<RoomPattern> Patterns {get;}
        public ConfiguredRoomCatalog(string version,IEnumerable<RoomPattern> patterns)
        {Patterns=patterns.ToList().AsReadOnly();Geometry=new RoomCatalog(version,Patterns.Select(p=>p.Room));foreach(var p in Patterns)PatternValidation.Validate(p);}
    }
    public sealed class RoomCoverage
    {
        public string InstanceId {get;}
        public ReadOnlyCollection<string> CellIds {get;}
        public RoomCoverage(string instanceId,IEnumerable<string> ids){InstanceId=instanceId;CellIds=ids.ToList().AsReadOnly();}
    }
    public sealed class ConfiguredDungeonResult
    {
        public ConfiguredGenerationPlan? Plan {get;}
        public GenerationResult Layout {get;}
        public DungeonManifest? Grid {get;}
        public ReadOnlyCollection<RoomCoverage> Coverage {get;}
        public ReadOnlyCollection<BranchLoopTrace> GridLoops {get;}
        public ReadOnlyCollection<ExplorationBranchTrace> GridBranches {get;}
        public ConfiguredDungeonResult(GenerationResult layout,DungeonManifest? grid=null,IEnumerable<RoomCoverage>? coverage=null,IEnumerable<BranchLoopTrace>? loops=null,IEnumerable<ExplorationBranchTrace>? branches=null,ConfiguredGenerationPlan? plan=null)
        {Plan=plan;Layout=layout;Grid=grid;Coverage=(coverage??Array.Empty<RoomCoverage>()).ToList().AsReadOnly();GridLoops=(loops??Array.Empty<BranchLoopTrace>()).ToList().AsReadOnly();GridBranches=(branches??Array.Empty<ExplorationBranchTrace>()).ToList().AsReadOnly();}
    }
    public static class ConfiguredDungeon
    {
        public static ConfiguredGenerationPlan ResolvePlan(uint seed,DungeonGenerationOptions? options=null)
        {
            options=options??new DungeonGenerationOptions();uint value=seed^0x4C4F4F50u;
            unchecked {value^=value>>16;value*=0x7FEB352Du;value^=value>>15;value*=0x846CA68Bu;value^=value>>16;}
            return new ConfiguredGenerationPlan(seed,options,options.MinLoops+(int)(value%(uint)(options.MaxLoops-options.MinLoops+1)));
        }
        public static ConfiguredDungeonResult Generate(ConfiguredRoomCatalog catalog,uint seed,DungeonGenerationOptions? options=null)
        {
            var plan=ResolvePlan(seed,options);var result=GridRoomMatcher.Generate(catalog,seed,plan.MaxAttempts,plan);
            return new ConfiguredDungeonResult(result.Layout,result.Grid,result.Coverage,result.GridLoops,result.GridBranches,plan);
        }
        public static ConfiguredDungeonResult Generate(ConfiguredRoomCatalog catalog,uint seed,int maxAttempts)=>GridRoomMatcher.Generate(catalog,seed,maxAttempts);
    }
}
