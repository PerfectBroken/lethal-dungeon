# Domain.Tests

## 用途、规范与依赖

TEST-001：通过公开接口验证LOAD、CLOCK与MAP规则；显式边界预期，不复制实现公式作为唯一判断。
TEST-002：不Skip、不等待、不联网；只依赖Domain、NUnit及测试适配器。
TEST-003：有效Red要求成功还原、编译、发现并执行测试，因缺少行为失败。

BackpackLoadTests、WorldClockTests、DungeonLayoutTests与DungeonLoopTests分别映射子模块README。手动时钟作为本测试模块内的替身，不是生产时钟实现。

## 接口与执行

由dotnet test发现测试；运行命令见根README。测试输入独立，不依赖顺序。依赖版本固定在项目文件与锁文件。

## AI修改边界与验证限制

只能按已披露规范新增或修改预期；不得为了通过NotImplemented桩而断言它抛异常。桩异常是Red失败原因而非最终正确行为。
此层不是引擎、网络、资产事务或真机测试。实际结果见evidence。

地图批量用例在所有断言通过后，将目录及6份实际布局导出为测试附件dungeon-preview.json，供预览使用；文件写入仅在测试宿主，不进入Domain。

环路用例对应MAP-011/012，输出dungeon-loops.json；目录包含新增四向连接房，前6份布局供预览。新增10个用例先运行有效Red，再实现闭环与共享预算；最终结果见evidence/public-loops。

DungeonBranchLoopTests对应MAP-013～016，重建支线、连接段及完整图，逐边检查替代路径，防止统计大环却夹带小环捷径。批量用例输出dungeon-branch-loops.json与生成轨迹；此文件只由测试层写入。
