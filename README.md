# 《致命地下城》规范与测试工程

当前范围：背包负重倍率与探险时钟的规范、可执行测试和最小编译桩。已完成首批Red验证：42个规则用例按预期失败，6个规范检查通过，0跳过。业务实现留到下一阶段。

## 规范与模块

- [研发要求](docs/02-技术架构.md)第3.1–3.5节与12.0节为强制约束。
- [Domain](src/Domain/README.md)：纯规则程序集。
- [背包倍率](src/Domain/BackpackLoad/README.md)：已确认0～10格线性减速。
- [探险时钟](src/Domain/WorldClock/README.md)：已确认时间换算；房间加载屏障另属后续模块。
- [规则测试](tests/Domain.Tests/README.md)：规则与边界测试。
- [规范检查](tests/Architecture.Tests/README.md)：README与依赖约束检查。
- [执行证据](evidence/README.md)：真实运行结果及限制。

## 工程边界

Domain目标框架为netstandard2.1，C#语言固定为9.0；独立测试宿主使用.NET 10。这是本阶段的隔离测试选择，不表示已经验证团结引擎的导入兼容性，也不决定游戏服务器运行时。

采用NUnit测试；SDK和依赖版本在global.json、项目文件及packages.lock.json中固定。禁止引入浮动包版本。使用虚拟时钟，测试不睡眠、不联网、不读真实系统时间。

工程级配置属于本工程；依赖校验测试先于项目配置创建。src下独立模块需各自README；tests下每个测试项目有自己的README。

## 运行

安装 `global.json` 指定的 .NET SDK 后，在仓库根目录运行：

```sh
dotnet restore LethalDungeon.sln --locked-mode
dotnet test tests/Architecture.Tests/Architecture.Tests.csproj --no-restore --logger 'trx;LogFileName=architecture.trx' --results-directory evidence/latest
dotnet test tests/Domain.Tests/Domain.Tests.csproj --no-restore --logger 'trx;LogFileName=domain.trx' --results-directory evidence/latest
```

Red阶段规则测试应失败（退出码1）；规范检查应通过（退出码0）。不要将预期Red伪装成全部通过。原始运行环境及结果见[evidence](evidence/README.md)，公开日志在evidence/public-red。

原开发机器的SDK位于工程外 `../../work/tooling/dotnet/dotnet`，本地包源为 `../../work/tooling/nuget-feed`。由于该机器.NET证书读取异常，实际还原使用此本地源；公开工程保留官方NuGet源。SDK、包缓存及构建产物不纳入仓库。

设计文档统一位于[docs](docs/README.md)。开发机旁边的旧文档目录保留作历史副本，后续以本仓库docs为准。

## AI修改边界与待定事项

仅处理首批模块。物品占格策略、世界绝对移动速度、人数屏障异常、撤离读条、经济结算、怪物及战斗规则不属于本次范围。12分钟到18:00，18分钟到午夜；不得改成12分钟强制结束。

尚未创建团结工程、安装微信SDK或验证引擎/真机。目标仓库为 https://github.com/PerfectBroken/lethal-dungeon 。自动检查目前通过本地命令执行，尚无远程CI运行记录。
