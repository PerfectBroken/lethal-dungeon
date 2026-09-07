# 执行证据

记录实际命令、SDK/依赖版本、退出码、用例总数和失败原因。未执行项目明确列出，不将环境错误当作Red。

`public-red/`保留本阶段原始TRX和日志；`latest/`用于后续重跑，避免覆盖Red基线。早期编译诊断和原始日志仅保留本地，不公开。

本目录只保存报告，不是运行时模块。不得把预期失败改写成测试通过；不得记录未发生的Green、引擎或真机验证。

## 2026-09-07 实际结果

| 检查 | 结果 | 证据 |
|---|---|---|
| 本地依赖还原 | 成功，锁定模式成功 | public-red/restore.log、public-red/restore-locked.log |
| 规则测试 | 总数42，失败42，通过0，跳过0；退出码1 | public-red/domain.trx、public-red/domain.log、public-red/domain-exit-code.txt |
| 规范检查 | 总数6，通过6，失败0，跳过0；退出码0 | public-red/architecture.trx、public-red/architecture.log、public-red/architecture-exit-code.txt |

规则测试中18个背包用例、24个时钟用例。41个失败消息包含NotImplementedException（包括异常类型不符合契约的断言）；另1个为空时间源没有抛出应有异常。程序集已编译，测试已发现并执行，没有不可运行用例。业务实现尚未开始，这些失败是有效Red，不是功能完成。

规范检查验证README存在与关键内容、规则ID映射、Domain项目目标与依赖、程序集引用及常见禁止API。它们不能独立证明所有TDD历史或动态依赖安全，仍需规范评审。

## 工具与复跑

macOS arm64；SDK 10.0.400；测试宿主net10.0；Domain为netstandard2.1/C#9；NUnit 4.6.1、NUnit3TestAdapter 6.3.0、Microsoft.NET.Test.Sdk 18.9.0。传递依赖以各项目packages.lock.json为准。

实际运行的命令与根README相同，将`--results-directory`设为`evidence/red`并重定向到对应.log。还原源为`../../work/tooling/nuget-feed`；测试使用`--no-restore`。不要覆盖既有Red日志，后续使用latest目录。

首次在线restore因本机.NET证书读取失败；使用curl正常HTTPS下载官方包后从本地源成功还原。首次编译发现NUnit新版本的Action/TestDelegate重载在C#9中歧义，测试改为显式Action委托，未改变预期断言；该次编译失败不计作Red。

官方工具说明：[Microsoft本地SDK安装脚本](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script)、[NUnit安装说明](https://docs.nunit.org/articles/nunit/getting-started/installation.html)。包下载来自api.nuget.org官方源，未修改包内容。

## 尚未执行

团结引擎导入、真实服务端时钟适配、房间到达屏障、联机与微信真机测试、远程CI实际执行。以上都不能从本次纯规则测试推导为完成。

## 公开仓库处理

public-red是实际Red报告的脱敏副本：本机工程绝对路径替换为`/workspace/lethal-dungeon`，工具目录替换为`/workspace/tooling`，计算机名统一为`local-machine`。测试结果、预期、异常和退出码保持不变。本地red/setup原始文件由.gitignore排除。

## 首批Green实现（2026-09-07）

先重新运行原42个用例，确认全部Red；未修改原测试期望。实现背包公式与输入校验后18个用例通过，再实现时钟状态与边界。完整回归42个规则测试与6个规范检查全部通过，0跳过，退出码0。

命令：`dotnet test LethalDungeon.sln --no-restore --logger "trx;LogFilePrefix=green" --results-directory <本地报告目录>`，使用前述工作目录SDK及包缓存。发布副本为[domain.trx](public-green/domain.trx)和[architecture.trx](public-green/architecture.trx)。脱敏方式与Red一致。

实现保持公开接口和所有原断言不变；简单逻辑未作额外结构重构。真实服务端时间源、加载屏障和引擎运行仍未验证。

## CI与引擎配置验证（2026-09-07）

- CI配置：先运行4个检查，均因工作流缺失而失败；再新增ci.yml，4个检查通过。actionlint 1.7.12语法检查通过，退出码0。
- 引擎配置：先运行3个检查，均因必需配置文件缺失而失败；再新增候选编辑器版本、包manifest和测试asmdef，3个检查通过。
- 最终Release回归：42个Domain测试、13个Architecture测试全部通过，0跳过。报告在public-validation；其中不含3个尚未运行的引擎导入测试。
- CI在提交18027e1首次触发：[Actions运行](https://github.com/PerfectBroken/lethal-dungeon/actions/runs/34087217947)。结论failure，job没有steps、runner未分配。GitHub check-run 101633448040的annotation原文：`The job was not started because your account is locked due to a billing issue.` 此结果不是测试失败或测试通过，需账号问题解除后重新执行。

新的配置代码未修改42个原规则用例。引擎工程仅完成静态配置检查和插件准备；未启动编辑器，未产生引擎包解析锁文件，也未执行微信构建。

## 三维模块布局核心（2026-09-07）

- 规范与47个地图用例先于实现。编译桩运行：46失败、1通过、0跳过，退出码1；只读集合容器检查通过，其余因行为未实现失败。记录为public-dungeon/dungeon-red.trx。
- 几何、目录、生成与独立校验实现后47个用例通过。随后补充门洞尺寸不匹配、重复门实体及默认结构体盒验证；默认盒测试有效Red后修复目录入口校验，记录为default-box-red.trx。
- 最终Release：92个规则用例、14个规范/配置用例通过，0跳过，退出码0；报告为public-dungeon/domain-green.trx与architecture-green.trx。未改变已有背包和时钟断言。
- 一个批量用例实际生成并检查100个种子的12房间跨层地图；前6份结果及目录导出到docs/examples/prototype-layouts.json。预览读取这些结果，不另写随机布局算法。
- 命令：dotnet test LethalDungeon.sln --no-restore -c Release --logger 'trx;LogFilePrefix=final' --results-directory <本地报告目录>。本机SDK与缓存位置同前。公开TRX替换本机路径、机器名及运行用户名，断言与结果未改。
- 这些结果只证明当前元数据几何与连接规则；真实预制件通行、服务端传输、客户端搭建、引擎导航、微信性能尚未验证。CI账号账单阻塞未解决。

## 环路扩展（2026-09-07）

- 规范新增MAP-011/012，10个环路用例及编译桩先行；实际Red为10失败、0通过、0跳过，退出码1，失败来自未实现行为。报告public-loops/loops-red.trx。
- 初次实现后9通过、1失败：批量种子33未在10000次内完成环路和跨层要求。保持全部断言与预算不变，增加最后两房间优先建立高差、最后一房间剔除不能完成高差的候选后通过。
- 批量100种子，全部18房间且具有2个独立环路、跨层连接；最少4房间成环、完整几何与图校验通过。最高尝试39次。前6份真实输出为docs/examples/loop-layouts.json。
- 最终Release：102个规则测试和14个规范检查通过，0跳过，退出码0；报告为public-loops/domain-green.trx、architecture-green.trx。命令dotnet test LethalDungeon.sln --no-restore -c Release --logger 'trx;LogFilePrefix=loops-final' --results-directory <本地报告目录>。
- 公开报告按此前方式脱敏。预览脚本语法检查通过；未重复尝试此前被浏览器安全策略拒绝的本地预览动作，未声称完成浏览器视觉实测。引擎、导航、网络、微信真机和CI云端执行状态不变。

## 长支线末端连接（2026-09-07）

- 用户否定局部四房间环，规范新增MAP-013～016，BranchLoopGenerator改为先生成两条长支线、再在空闲格中BFS路由连接段。原局部环算法仅保留兼容和历史样例。
- 先行6个新用例全部因NotImplementedException有效失败（branches-red.trx）；初始清单扩展参数另有1个有效Red（extension-red.trx）。随后发现非默认初始ID与自动命名冲突，补1个测试确认失败后修复（extension-ids-red.trx）。8个新增用例通过。
- 100种子均通过：40房间、两个长环、跨层连接、支线叶端与深度、连接段、完整几何及逐边替代路径检查。全图最短环不少于11房间；最高8374次尝试，预算10000。前6份实际输出与轨迹为docs/examples/branch-loop-layouts.json。
- Release回归110个规则测试通过，0跳过（domain-green.trx）。规范检查首次发现测试注释缩写MAP-013/015没有完整列出MAP-015；补全文字映射后14个规范检查通过（architecture-green.trx），没有放宽检查。
- 执行命令：dotnet test LethalDungeon.sln --no-restore -c Release --logger 'trx;LogFilePrefix=branches-final' --results-directory <本地报告目录>；修正文档映射后单独重跑Architecture.Tests。SDK与脱敏策略同前。
- 新预览脚本通过node --check；未绕过此前浏览器URL安全策略，未声称完成浏览器视觉实测。真实素材、引擎导航、跨层长环、微信及云端CI没有新增验收结果。

## 种子派生环数（2026-09-07）

- 规范MAP-017～019先行，15个DungeonSeedTests在编译桩上全部有效Red，退出码1；证据public-seeds/seed-red.trx。
- 实现独立加盐派生环数及布局种子，环数1～4，房间规模24/40/56/64。最多5种确定性布局变体共享预算，重试不会改变环数。未修改原地图算法或既有预期。
- 种子0～99全部成功：1/2/3/4环分别24/24/26/26次；种子86重试一次后仍为3环，最大累计20235步。新批量用例复用既有长支线图结构检查，覆盖无小环捷径与真实空间连接。
- 最终Release：125规则+14规范检查全部通过，0跳过，退出码0。报告public-seeds/domain-green.trx、architecture-green.trx；命令dotnet test LethalDungeon.sln --no-restore -c Release --logger 'trx;LogFilePrefix=seed-final' --results-directory <本地报告目录>。SDK及脱敏方式同前。
- 房间规模映射是原型默认，不代表正式关卡难度、探索时长或微信性能定稿。未新增引擎、网络、真机或云端CI验收。

## 探索支路预算（2026-09-07）

- MAP-020～023规范先行，14个用例在编译桩上全部因未实现有效Red（public-exploration/exploration-red.trx），之后实现并通过；另补3个保护ID与显式配置用例。
- 默认升级seed-rules-v2；先抽取探索支路深度并预留房间，环生成后建立独立链，后续填充保护这些链。旧种子测试显式选择v1，原断言未放宽。
- 100种子均满足环数、完整长环、无短环、几何、跨层和每环2条2～4房间独立死路。最大20690次尝试，预算100000。真实样例docs/examples/exploration-layouts.json。
- Release全量142规则+14规范检查通过，0跳过；public-exploration/domain-green.trx、architecture-green.trx。命令dotnet test LethalDungeon.sln --no-restore -c Release --logger trx --results-directory <本地报告目录>。公开报告仅脱敏路径、用户名、主机名，不修改断言或结果。
- 预览40种离线脚本状态检查通过，覆盖两种宽度、全图/单环、空间/俯视及支路显隐；这不是浏览器视觉验证。未绕过此前浏览器安全策略。引擎、导航、网络、微信性能和云端CI状态不变。
