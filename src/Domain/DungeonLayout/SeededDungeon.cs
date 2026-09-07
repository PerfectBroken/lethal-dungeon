using System;
namespace LethalDungeon.Domain.Dungeons
{
    public sealed class DungeonSeedPlan
    {
        public uint WorldSeed { get; }
        public string RuleVersion => "seed-rules-v1";
        public int LoopCount { get; }
        public int TargetRooms { get; }
        public uint LayoutSeed { get; }
        internal DungeonSeedPlan(uint worldSeed,int loops,int rooms,uint layoutSeed)
        {WorldSeed=worldSeed;LoopCount=loops;TargetRooms=rooms;LayoutSeed=layoutSeed;}
    }
    public sealed class SeededDungeonResult
    {
        public DungeonSeedPlan Plan { get; }
        public BranchLoopResult Result { get; }
        public int AttemptIndex { get; }
        public uint ActualLayoutSeed { get; }
        internal SeededDungeonResult(DungeonSeedPlan plan,BranchLoopResult result,int attemptIndex,uint actualLayoutSeed)
        {Plan=plan;Result=result;AttemptIndex=attemptIndex;ActualLayoutSeed=actualLayoutSeed;}
    }
    public static class SeededDungeon
    {
        private static uint Mix(uint value)
        {
            unchecked {
                value ^= value >> 16; value *= 0x7FEB352Du;
                value ^= value >> 15; value *= 0x846CA68Bu;
                return value ^ (value >> 16);
            }
        }
        public static DungeonSeedPlan Resolve(uint worldSeed)
        {
            int loops=1+(int)(Mix(worldSeed^0x4C4F4F50u)&3u);
            int rooms=loops==1?24:loops==2?40:loops==3?56:64;
            return new DungeonSeedPlan(worldSeed,loops,rooms,Mix(worldSeed^0x4C41594Fu));
        }
        public static SeededDungeonResult Generate(uint worldSeed,int maxAttempts=100000)
        {
            if(maxAttempts<1||maxAttempts>100000)throw new ArgumentOutOfRangeException(nameof(maxAttempts));
            var plan=Resolve(worldSeed);int used=0,backtracks=0,index=0;uint actual=plan.LayoutSeed;
            for(index=0;index<5;index++)
            {
                actual=index==0?plan.LayoutSeed:Mix(plan.LayoutSeed^unchecked((uint)index*0x9E3779B9u));
                var result=BranchLoopGenerator.Generate(new XorShiftRandom(actual),plan.TargetRooms,plan.LoopCount,Math.Min(20000,maxAttempts-used),true);
                used+=result.Layout.Attempts;backtracks+=result.Layout.Backtracks;
                if(result.Layout.Succeeded)
                {
                    var aggregate=new BranchLoopResult(new GenerationResult(result.Layout.Manifest,string.Empty,used,backtracks),result.Loops);
                    return new SeededDungeonResult(plan,aggregate,index,actual);
                }
                if(used>=maxAttempts||index==4)break;
            }
            string failure=used>=maxAttempts?"BudgetExceeded":"LayoutVariantsExhausted";
            var failed=new BranchLoopResult(new GenerationResult(null,failure,used,backtracks),Array.Empty<BranchLoopTrace>());
            return new SeededDungeonResult(plan,failed,index,actual);
        }
    }
}
