using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace LethalDungeon.Domain.Dungeons
{
    public sealed class BranchLoopTrace
    {
        public string ForkRoom { get; }
        public ReadOnlyCollection<string> BranchA { get; }
        public ReadOnlyCollection<string> BranchB { get; }
        public ReadOnlyCollection<string> Bridge { get; }
        public string ClosingConnection { get; }
        public BranchLoopTrace(string fork,IEnumerable<string> a,IEnumerable<string> b,IEnumerable<string> bridge,string closing)
        { ForkRoom=fork;BranchA=a.ToList().AsReadOnly();BranchB=b.ToList().AsReadOnly();Bridge=bridge.ToList().AsReadOnly();ClosingConnection=closing; }
    }
    public sealed class BranchLoopResult
    {
        public GenerationResult Layout { get; }
        public ReadOnlyCollection<BranchLoopTrace> Loops { get; }
        public ReadOnlyCollection<ExplorationBranchTrace> ExplorationBranches { get; }
        public BranchLoopResult(GenerationResult layout,IEnumerable<BranchLoopTrace> loops,IEnumerable<ExplorationBranchTrace>? explorationBranches=null)
        { Layout=layout;Loops=loops.ToList().AsReadOnly();ExplorationBranches=(explorationBranches??Array.Empty<ExplorationBranchTrace>()).ToList().AsReadOnly(); }
    }
    public static class BranchLoopGenerator
    {
        private static readonly GridPoint[] Steps = {new GridPoint(0,0,1),new GridPoint(1,0,0),new GridPoint(0,0,-1),new GridPoint(-1,0,0)};
        private static GridPoint Add(GridPoint a,GridPoint b) => new GridPoint(a.X+b.X,0,a.Z+b.Z);
        public static BranchLoopResult Generate(IRandomSource random,int targetRooms=40,int loopCount=2,int maxAttempts=10000,bool requireHeightChange=true,ExplorationBranchOptions? exploration=null,bool spatialMainPaths=false)
        {
            if(random==null)throw new ArgumentNullException(nameof(random));
            if(targetRooms<1||targetRooms>(exploration==null?64:128))throw new ArgumentOutOfRangeException(nameof(targetRooms));
            if(loopCount<1||loopCount>4)throw new ArgumentOutOfRangeException(nameof(loopCount));
            if(maxAttempts<1||maxAttempts>100000)throw new ArgumentOutOfRangeException(nameof(maxAttempts));
            var catalog=spatialMainPaths?PrototypeCatalog.CreateSpatial():PrototypeCatalog.CreateLoopReady();int attempts=0,retries=0;
            var rooms=new List<PlacedRoom>{new PlacedRoom("room_000","entry",new GridPoint(0,0,0))};
            var links=new List<DoorConnection>();var traces=new List<BranchLoopTrace>();var branches=new List<ExplorationBranchTrace>();
            var cells=new Dictionary<GridPoint,string>{{new GridPoint(0,0,0),"room_000"}};
            bool Spend(){if(attempts>=maxAttempts)return false;attempts++;return true;}
            int Next(int count){int n=random.Next(count);if(n<0||n>=count)throw new InvalidOperationException("Invalid random index.");return n;}
            List<T> Shuffle<T>(IEnumerable<T> values){var list=values.ToList();for(int i=list.Count-1;i>0;i--){int j=Next(i+1);var v=list[i];list[i]=list[j];list[j]=v;}return list;}
            var plannedDepths=new List<int[]>();
            if(exploration!=null)for(int i=0;i<loopCount;i++)
                plannedDepths.Add(Enumerable.Range(0,exploration.BranchesPerLoop).Select(_=>exploration.MinDepth+Next(exploration.MaxDepth-exploration.MinDepth+1)).ToArray());
            int branchReserve=plannedDepths.Sum(depths=>depths.Sum());
            DungeonManifest Snapshot()=>new DungeonManifest(catalog.Version,rooms,links,spatialMainPaths?"spatial-main-paths-v0.5":exploration==null?"branch-routing-v0.3":"exploration-branches-v0.4");
            BranchLoopResult Fail()=>new BranchLoopResult(new GenerationResult(null,attempts>=maxAttempts?"BudgetExceeded":"NoLayout",attempts,retries),Array.Empty<BranchLoopTrace>());
            List<GridPoint>? Walk(GridPoint fork,int length,HashSet<GridPoint> occupied)
            {
                var path=new List<GridPoint>();var current=fork;
                for(int i=0;i<length;i++){
                    bool found=false;
                    foreach(var step in Shuffle(Steps)){
                        if(!Spend())return null;var next=Add(current,step);
                        if(occupied.Contains(next)||Steps.Count(d=>occupied.Contains(Add(next,d)))>1)continue;
                        occupied.Add(next);path.Add(next);current=next;found=true;break;
                    }
                    if(!found)return null;
                }return path;
            }
            List<GridPoint>? Route(GridPoint start,GridPoint goal,HashSet<GridPoint> occupied)
            {
                int minX=occupied.Min(c=>c.X)-4,maxX=occupied.Max(c=>c.X)+4,minZ=occupied.Min(c=>c.Z)-4,maxZ=occupied.Max(c=>c.Z)+4;
                var parent=new Dictionary<GridPoint,GridPoint>{{start,start}};var queue=new Queue<GridPoint>();queue.Enqueue(start);var directions=Shuffle(Steps);
                while(queue.Count>0&&!parent.ContainsKey(goal)){
                    if(!Spend())return null;var current=queue.Dequeue();
                    foreach(var step in directions){var next=Add(current,step);
                        if(next.X<minX||next.X>maxX||next.Z<minZ||next.Z>maxZ||parent.ContainsKey(next)||(occupied.Contains(next)&&!next.Equals(goal)))continue;
                        parent[next]=current;queue.Enqueue(next);
                    }
                }
                if(!parent.ContainsKey(goal))return null;
                var reverse=new List<GridPoint>();var at=goal;
                while(!at.Equals(start)){reverse.Add(at);at=parent[at];}reverse.Reverse();reverse.RemoveAt(reverse.Count-1);return reverse;
            }
            string Append(GridPoint cell,string parentId)
            {
                var parent=rooms.Single(r=>r.InstanceId==parentId);
                var old=cells.Single(p=>p.Value==parentId).Key;
                int direction=Array.FindIndex(Steps,d=>Add(old,d).Equals(cell));
                var socket=catalog.Get(parent.ModuleId).Sockets.Single(s=>(int)s.Facing==direction);
                string opposite=((Direction)((direction+2)%4)).ToString().ToLowerInvariant();
                string suffix=rooms.Count.ToString("000",System.Globalization.CultureInfo.InvariantCulture),id="room_"+suffix;
                var placed=LayoutGeometry.Attach(catalog.Get(parent.ModuleId),parent,socket.Id,catalog.Get("junction"),opposite,id);
                rooms.Add(placed);cells.Add(cell,id);
                links.Add(new DoorConnection("connection_"+suffix,parentId,socket.Id,id,opposite,"door_"+suffix));return id;
            }
            if(exploration!=null && 1+loopCount*12+branchReserve+(requireHeightChange?2:0)>targetRooms)return Fail();
            while(traces.Count<loopCount){
                bool built=false;
                // Each attempt plans two independent leaf branches and a bridge before mutating the layout.
                while(!built && attempts<maxAttempts){
                    if(!Spend())return Fail();
                    var occupied=new HashSet<GridPoint>(cells.Keys);
                    var used=new HashSet<(string,string)>(links.SelectMany(e=>new[]{(e.FromRoom,e.FromSocket),(e.ToRoom,e.ToSocket)}));
                    var forks=new List<(GridPoint Cell,string Parent)>();
                    foreach(var r in rooms)
                    foreach(var s in catalog.Get(r.ModuleId).Sockets){
                        if(used.Contains((r.InstanceId,s.Id)))continue;
                        // Only the root's centred ports enter the 8m routing lattice.
                        var ws=LayoutGeometry.WorldSocket(catalog.Get(r.ModuleId),r,s.Id);var normal=Steps[(int)ws.Facing];
                        int x=ws.Position.X+normal.X*8,z=ws.Position.Z+normal.Z*8;
                        if(x%16!=0||z%16!=0)continue;var c=new GridPoint(x/16,0,z/16);
                        if(!occupied.Contains(c)&&Steps.Count(d=>occupied.Contains(Add(c,d)))==1)forks.Add((c,r.InstanceId));
                    }
                    if(forks.Count==0)return Fail();
                    var fork=forks[Next(forks.Count)];occupied.Add(fork.Cell);
                    var a=Walk(fork.Cell,4+Next(3),occupied);if(a==null){retries++;continue;}
                    var b=Walk(fork.Cell,4+Next(3),occupied);if(b==null){retries++;continue;}
                    var bridge=Route(a.Last(),b.Last(),occupied);
                    int remainingLoops=loopCount-traces.Count-1;
                    int reserve=branchReserve+(requireHeightChange?2:0)+remainingLoops*12;
                    if(bridge==null||bridge.Count<2||rooms.Count+1+a.Count+b.Count+bridge.Count>targetRooms-reserve){retries++;continue;}
                    int placements=1+a.Count+b.Count+bridge.Count;
                    if(maxAttempts-attempts<placements+1){while(Spend()){}return Fail();}
                    Spend();string forkId=Append(fork.Cell,fork.Parent);
                    List<string> Build(IEnumerable<GridPoint> path,string parentId){var ids=new List<string>();foreach(var c in path){Spend();parentId=Append(c,parentId);ids.Add(parentId);}return ids;}
                    var aIds=Build(a,forkId);var bIds=Build(b,forkId);var bridgeIds=Build(bridge,aIds.Last());
                    var sourceRoom=rooms.Single(r=>r.InstanceId==bridgeIds.Last());var to=rooms.Single(r=>r.InstanceId==bIds.Last());
                    var fromDef=catalog.Get(sourceRoom.ModuleId);var toDef=catalog.Get(to.ModuleId);
                    var matches=(from fs in fromDef.Sockets from ts in toDef.Sockets
                        where LoopConnections.Matches(LayoutGeometry.WorldSocket(fromDef,sourceRoom,fs.Id),LayoutGeometry.WorldSocket(toDef,to,ts.Id)) select (From:fs.Id,To:ts.Id)).Single();
                    Spend();string closing="loop_branch_"+traces.Count;
                    links.Add(new DoorConnection(closing,sourceRoom.InstanceId,matches.From,to.InstanceId,matches.To,"door_"+closing));
                    if(!LayoutValidator.Validate(catalog,Snapshot()).IsValid)throw new InvalidOperationException("Routed skeleton failed geometric validation.");
                    traces.Add(new BranchLoopTrace(forkId,aIds,bIds,bridgeIds,closing));built=true;
                }
                if(!built)return Fail();
            }
            if(spatialMainPaths && !SpatialMainPaths.Elevate(catalog,rooms,links,traces,Spend,Next))return Fail();
            var usedAnchors=new HashSet<string>();
            for(int loopIndex=0;loopIndex<plannedDepths.Count;loopIndex++)
            foreach(int depth in plannedDepths[loopIndex])
            {
                var trace=traces[loopIndex];
                var ring=trace.BranchA.Concat(trace.BranchB).Concat(trace.Bridge).Append(trace.ForkRoom).ToArray();
                bool built=false;
                while(!built && attempts<maxAttempts)
                {
                    if(!Spend())return Fail();
                    var possible=ring.Where(id=>rooms.Single(r=>r.InstanceId==id).ModuleId=="junction"&&!usedAnchors.Contains(id)).Where(id=>{
                        var cell=cells.Single(p=>p.Value==id).Key;
                        return Steps.Any(d=>!cells.ContainsKey(Add(cell,d))&&Steps.Count(other=>cells.ContainsKey(Add(Add(cell,d),other)))==1);
                    }).ToArray();
                    if(spatialMainPaths && !branches.Any(b=>b.LoopIndex==loopIndex)) {
                        int bottom=rooms.Where(r=>ring.Contains(r.InstanceId)&&r.ModuleId=="junction").Min(r=>r.Position.Y);
                        possible=possible.Where(id=>rooms.Single(r=>r.InstanceId==id).Position.Y>bottom).ToArray();
                    }
                    if(possible.Length==0)return Fail();
                    var anchor=possible[Next(possible.Length)];var at=cells.Single(p=>p.Value==anchor).Key;
                    var path=Walk(at,depth,new HashSet<GridPoint>(cells.Keys));
                    if(path==null){retries++;continue;}
                    if(maxAttempts-attempts<depth){while(Spend()){}return Fail();}
                    var ids=new List<string>();string parent=anchor;
                    foreach(var cell in path){Spend();parent=Append(cell,parent);ids.Add(parent);}
                    branches.Add(new ExplorationBranchTrace(loopIndex,anchor,ids));usedAnchors.Add(anchor);built=true;
                }
                if(!built)return Fail();
            }
            if(attempts>=maxAttempts)return Fail();
            var expanded=DungeonGenerator.Generate(catalog,"entry",targetRooms,random,maxAttempts-attempts,requireHeightChange,loopCount,"junction",Snapshot(),branches.SelectMany(b=>b.Rooms),exploration==null?64:128);
            attempts+=expanded.Attempts;retries+=expanded.Backtracks;
            if(!expanded.Succeeded)return Fail();
            return new BranchLoopResult(new GenerationResult(expanded.Manifest,string.Empty,attempts,retries),traces,branches);
        }
    }
}
