using System;
namespace LethalDungeon.Domain.Dungeons
{
 public sealed class DungeonGenerationOptions
 {
  public int MinLoops {get;}
  public int MaxLoops {get;}
  public int MaxAttempts {get;}
  public DungeonGenerationOptions(int minLoops=1,int maxLoops=12,int maxAttempts=300000)
  {if(minLoops<1||maxLoops>12||minLoops>maxLoops)throw new ArgumentOutOfRangeException(nameof(minLoops));if(maxAttempts<1||maxAttempts>1000000)throw new ArgumentOutOfRangeException(nameof(maxAttempts));MinLoops=minLoops;MaxLoops=maxLoops;MaxAttempts=maxAttempts;}
 }
 public sealed class ConfiguredGenerationPlan
 {
  public uint WorldSeed {get;}
  public int MinLoops {get;}
  public int MaxLoops {get;}
  public int LoopCount {get;}
  public int TargetCells {get;}
  public int MaxAttempts {get;}
  public string RuleVersion => "configured-grid-v0.7";
  internal ConfiguredGenerationPlan(uint seed,DungeonGenerationOptions options,int loops)
  {WorldSeed=seed;MinLoops=options.MinLoops;MaxLoops=options.MaxLoops;LoopCount=loops;TargetCells=8+32*loops;MaxAttempts=options.MaxAttempts;}
 }
}
