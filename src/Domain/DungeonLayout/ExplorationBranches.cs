using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
namespace LethalDungeon.Domain.Dungeons
{
    public sealed class ExplorationBranchOptions
    {
        public int BranchesPerLoop { get; }
        public int MinDepth { get; }
        public int MaxDepth { get; }
        public ExplorationBranchOptions(int branchesPerLoop=2,int minDepth=2,int maxDepth=4)
        {
            if(branchesPerLoop<1||branchesPerLoop>4)throw new ArgumentOutOfRangeException(nameof(branchesPerLoop));
            if(minDepth<1||maxDepth>8||minDepth>maxDepth)throw new ArgumentOutOfRangeException(nameof(minDepth));
            BranchesPerLoop=branchesPerLoop;MinDepth=minDepth;MaxDepth=maxDepth;
        }
    }
    public sealed class ExplorationBranchTrace
    {
        public int LoopIndex { get; }
        public string AnchorRoom { get; }
        public ReadOnlyCollection<string> Rooms { get; }
        public ExplorationBranchTrace(int loopIndex,string anchor,IEnumerable<string> rooms)
        {LoopIndex=loopIndex;AnchorRoom=anchor;Rooms=rooms.ToList().AsReadOnly();}
    }
}
