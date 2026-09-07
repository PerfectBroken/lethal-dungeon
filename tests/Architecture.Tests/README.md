# Architecture.Tests

## 用途与规范

HARNESS-001：根规范文件与每个生产/测试项目、生产子模块README存在，披露规则和验证边界。
HARNESS-002：Domain没有包引用和项目引用；目标netstandard2.1、C# 9。
HARNESS-003：Domain程序集只依赖标准程序集；源代码不直接调用墙上时钟、IO、网络或引擎API。
HARNESS-004：每个子模块规范ID在对应测试类文本中有映射；这只能验证映射存在，仍需人工审查断言意义。

CI规范见 `.github/workflows/README.md`；CiWorkflowTests先验证workflow的触发、命令、权限和失败处理，再由GitHub实际运行验证平台兼容性。

## 接口与依赖

测试读取工程内文档和项目文件并反射Domain程序集。只允许测试层依赖文件系统；生产层禁止。
根目录通过向上寻找AGENTS.md和src/Domain定位，找不到应失败，不能跳过。

## 测试与执行

用例位于ArchitectureTests.cs，命令见根README。先写校验，再创建被校验的工程配置。此测试基础设施不包含游戏业务逻辑。

## AI修改约束及限制

不得删除保护规则来接受业务跨层依赖；发现漏检时先补反例。文本规则只是基础护栏，不证明任意动态代码都符合边界，也不能证明TDD历史顺序；Red日志单独保留。
当前阶段规范检查和业务测试都应通过；实际结果见evidence。
