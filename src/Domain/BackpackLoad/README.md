# BackpackLoad

## 用途与边界

将已占财宝格数转换成移动速度倍率。来源：需求规格第7节，10格与线性公式均已确认。
不处理拾取、堆叠、重量、背包状态、装备、移动或世界单位速度。

## 必须遵守的规范

| ID | 规则及状态 |
|---|---|
| LOAD-001 | 已确认：整数占格0～10，倍率1.2−0.03×占格 |
| LOAD-002 | 已确认公式推导：每格下降0.03；输出范围0.9～1.2；重复调用无状态 |
| LOAD-003 | 本阶段工程契约：负数或大于10，抛ArgumentOutOfRangeException，参数名occupiedSlots；不截断非法输入 |

## 接口与依赖

`BackpackLoad.SpeedMultiplier(int occupiedSlots) -> decimal`，静态纯函数；十进制精确倍率，客户端适配层以后再转换成引擎浮点数。整数输入避免小数格的隐式取整。输出无米/秒单位。
只有标准库依赖，不读取任何角色、网络或时钟。合法调用在非法调用前后均不受影响。

## 测试与执行

| ID | Domain.Tests中的用例 |
|---|---|
| LOAD-001 | EverySlotHasSpecifiedMultiplier：显式列出全部11个结果 |
| LOAD-002 | EachAdditionalSlotReducesSpeed；RepeatedAndInterleavedCallsAreStable |
| LOAD-003 | InvalidSlotsAreRejected：-1、11及int极值；InvalidCallDoesNotAffectLaterValidCall |

用例位于 [BackpackLoadTests.cs](../../../tests/Domain.Tests/BackpackLoadTests.cs)。执行命令见根README；Green时所有用例必须通过。

## AI修改约束

修改限本模块及对应测试。不得引入装备减速、冲刺、物品占格规则；需要改变容量时先修改需求和规范。

## 验证与已知限制

已完成Green实现，18个背包测试通过。速度倍率通过不等于角色移动已验证；结果见evidence。
