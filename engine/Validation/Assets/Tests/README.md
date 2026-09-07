# 编辑器导入测试

ENGINE-003/004：使用编辑器自己的测试框架加载已经实现的Domain DLL，并检查代表性的倍率、午夜边界和异常行为；无新业务实现。

SharedRulesImportTests.cs只依赖Domain与NUnit，不使用真实等待。测试程序集限定Editor，避免测试框架进入微信包。DLL需从当前仓库Release构建复制，不能从下载站获取。

执行方法见Validation/README.md。当前未运行；未编译、未运行或许可证错误都不能记为有效Red。将来Runtime探针需另设模块并测试先行。
