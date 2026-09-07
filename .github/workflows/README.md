# 持续集成

## 用途与规范

用户已授权建立GitHub自动测试。本模块只定义CI，不发布游戏或操作城镇资产。

| ID | 必须遵守的规范 |
|---|---|
| CI-001 | push、pull_request和手动触发；只需要contents:read权限，不读取私有凭据 |
| CI-002 | 使用global.json的SDK与锁文件还原；Release构建，分别运行规范检查和全部规则测试 |
| CI-003 | 失败必须令job失败，禁止continue-on-error或吞掉退出码；即使失败也上传TRX，保留14天 |
| CI-004 | 官方Actions固定完整提交SHA；不持久化checkout凭据；作业限时15分钟 |

## 接口与依赖

输入为当前提交中的源码、规范与锁文件；输出为作业结论和TRX artifact。使用GitHub托管Ubuntu runner、官方checkout/setup-dotnet/upload-artifact。版本SHA通过官方仓库API获取。无自有运行脚本。

## 测试与执行

先编写Architecture.Tests/CiWorkflowTests.cs；缺少workflow时应得到明确失败，再添加ci.yml。用例覆盖触发、权限、必需命令、失败处理、结果保留和版本固定。文本检查不能替代GitHub实际解析与运行，远程作业完成后记录链接与结论。

本地：`dotnet test tests/Architecture.Tests/Architecture.Tests.csproj --filter FullyQualifiedName~CiWorkflowTests`。远程：push或Actions页面手动触发。

## AI修改约束与验证限制

不因Red历史记录而忽略当前失败；不申请写权限或仓库秘密。自动测试不是分支保护，本阶段不改变分支合并策略。4个先行配置用例已Red→Green，actionlint 1.7.12检查通过。GitHub已接收工作流，但账号账单锁阻止runner启动，远程运行尚未验证。
