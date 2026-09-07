# 团结/微信验证工程

## 用途与边界

用户授权的第三阶段准备工程。先验证已完成的Domain DLL能在编辑器中导入和执行，再添加运行场景与微信导出。当前只含验证配置和先行测试；不含可玩的场景或新的业务实现。

## 必须遵守的规范

| ID | 规范 |
|---|---|
| ENGINE-001 | 候选验证版本固定团结1.10.2，内部版本2022.3.62t14、revision 1f04f7aba499；未经实际验证不称为正式选型完成 |
| ENGINE-002 | Test Framework固定1.1.33；微信SDK Git固定提交d288776c50578926c732496882bd6ab6684c778c，不跟随main浮动；真实解析锁文件由编辑器生成 |
| ENGINE-003 | 仅导入本仓库构建的Domain DLL，不复制规则源码、不携带桌面NUnit4测试宿主；引擎测试使用引擎自己的NUnit |
| ENGINE-004 | 编辑器测试、微信开发者工具运行、真机运行、双客户端联网是四项独立验收；未执行必须明确标注 |

## 接口与依赖

输入：src/Domain Release DLL；输出：引擎Test Runner报告与后续微信构建产物。现阶段测试程序集只在Editor启用，不发布进游戏。

## 测试与执行

工程配置由Architecture.Tests/EngineProjectTests.cs先行校验；校验前缺少配置文件应失败。Assets/Tests是引擎导入集成测试，验证已经实现的规则，不新造业务桩。

从仓库根目录先构建并复制插件：

```sh
dotnet build src/Domain/Domain.csproj --configuration Release --no-restore
mkdir -p engine/Validation/Assets/Plugins/Domain
cp src/Domain/bin/Release/netstandard2.1/LethalDungeon.Domain.dll engine/Validation/Assets/Plugins/Domain/
```

安装、登录并激活目标编辑器后，打开本目录。等待包解析，运行Window → General → Test Runner中的EditMode测试。命令行方式：

```sh
"$TUANJIE_EDITOR" -batchmode -nographics -projectPath "$PWD/engine/Validation" -runTests -testPlatform EditMode -testResults "$PWD/evidence/latest/engine-editmode.xml" -logFile "$PWD/evidence/latest/engine-editmode.log"
```

TUANJIE_EDITOR需指向真实编辑器可执行文件；本命令尚未在本机执行。不要添加-quit提前结束Test Runner。包解析后提交Packages/packages-lock.json；不得手写假的解析记录。

## AI修改约束

先完成导入测试，再给运行探针、场景构建等新增模块写规范和测试。财宝待用户设计。不得将本地测试时钟冒充联网权威时钟；不得把编辑器脚本的成功当作微信IL2CPP成功。

## 验证与已知限制

本机尚无编辑器、微信开发者工具和已验证许可证。安装包下载中；不进行许可证绕过。当前没有已完成的引擎Red/Green或真机记录，配置检查不能替代它们。

版本来源：[团结官方下载页](https://unity.cn/tuanjie/releases)、[Test Framework 1.1文档](https://docs.unity3d.com/Packages/com.unity.test-framework@1.1/manual/index.html)、[微信SDK](https://github.com/wechat-miniprogram/minigame-tuanjie-transform-sdk)。候选版本兼容性以实际运行结果为准。
