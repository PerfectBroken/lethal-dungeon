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
