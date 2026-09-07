# 《致命地下城》规范与测试工程

当前进度：背包负重倍率、探险时钟与三维模块布局核心已完成Green实现。110个规则测试和14个规范/配置检查全部通过，0跳过；原始Red记录保留。CI已上传，云端运行被GitHub账号账单锁阻止；团结验证工程已准备，编辑器与真机尚未运行。

## 规范与模块

- [研发要求](docs/02-技术架构.md)第3.1–3.5节与12.0节为强制约束。
- [Domain](src/Domain/README.md)：纯规则程序集。
- [背包倍率](src/Domain/BackpackLoad/README.md)：已确认0～10格线性减速。
- [探险时钟](src/Domain/WorldClock/README.md)：已确认时间换算；房间加载屏障另属后续模块。
- [三维模块布局](src/Domain/DungeonLayout/README.md)：7种原型（含四向连接房）、长支线末端寻路连接、门位对接、三维占用检查、有限回退生成与独立校验；尚未接入引擎。
- [探险启动编排](src/Server/ExpeditionStartup/README.md)：地图生成、客户端搭建、全员到达及统一开局的流程归属；当前为模块规范，未实现。
- [规则测试](tests/Domain.Tests/README.md)：规则与边界测试。
- [规范检查](tests/Architecture.Tests/README.md)：README与依赖约束检查。
- [执行证据](evidence/README.md)：真实运行结果及限制。
- [引擎验证工程](engine/Validation/README.md)：候选版本配置和3个待执行的编辑器导入测试。
- [执行状态与后续验收](docs/04-执行状态与后续验收.md)：阻塞原因和接下来的操作。

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

当前规则测试与规范检查都应通过（退出码0）。原始Red记录在evidence/public-red，Green结果在evidence/public-green；运行环境见[evidence](evidence/README.md)。

原开发机器的SDK位于工程外 `../../work/tooling/dotnet/dotnet`，本地包源为 `../../work/tooling/nuget-feed`。由于该机器.NET证书读取异常，实际还原使用此本地源；公开工程保留官方NuGet源。SDK、包缓存及构建产物不纳入仓库。

设计文档统一位于[docs](docs/README.md)。开发机旁边的旧文档目录保留作历史副本，后续以本仓库docs为准。

## AI修改边界与待定事项

用户已授权按顺序推进基础规则、CI、团结/微信验证和灰盒闭环。财宝规则等待用户设计；世界绝对移动速度、人数屏障异常、撤离读条、经济结算、怪物及战斗细节仍需在对应模块前明确。12分钟到18:00，18分钟到午夜；不得改成12分钟强制结束。

已准备团结验证工程与微信SDK配置，尚未验证引擎导入或真机。目标仓库为 https://github.com/PerfectBroken/lethal-dungeon 。CI已配置并触发，但GitHub账号账单锁阻止job启动，尚无远程测试通过结果。
