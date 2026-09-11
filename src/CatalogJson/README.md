# CatalogJson

JSON-001：RoomCatalogJson.Parse接收字符串，不读取文件；严格检查格式版本、单位、坐标、字段及房间引用，返回Domain配置对象。未知字段、重复JSON键和不支持的配置必须拒绝，不能静默忽略。
JSON-002：采用.NET 10标准库System.Text.Json，只依赖Domain；不把JSON或IO依赖引入Domain。该适配器用于服务端/本地工具，尚未声称能导入团结；客户端解析适配另行验证。
JSON-003：输入最大1MB、128模块、每模块最多16模板格子，防止无界输入。只接受一个双向连通组、全部列出接口必接、标准门2×3米和净空1米、0.5米坐标单位、8米水平格子/4米层高。匹配模板为连通树，socketCells明确接口所属格子。

规范与接口先行，ConfiguredDungeonTests验证有效导入、非法字段/版本/旋转/模板/资源标识/门位、以及JSON改名和删减会实际影响生成结果。完整结果在evidence。尚未接入网络和编辑器资源加载。

正式配置位于docs/examples/base-rooms.json，13个模块均含matching。用法：

```csharp
var catalog = RoomCatalogJson.Parse(jsonText);
var result = ConfiguredDungeon.Generate(catalog, 5u);
if (!result.Layout.Succeeded) { /* 根据Failure处理，不能开始探险 */ }
// result.Layout.Manifest为实际搭建清单；catalog.Patterns提供PrefabKey和Revision。
```

17个新增用例及完整回归通过，证据evidence/public-configured。生成器不读取文件，调用方负责提供JSON文本；测试宿主负责实际文件读取和样例导出。适配器仍是服务端/工具.NET 10项目，非已验证的团结客户端组件。

## 房间模型关联入口

房型 ID、名称、资源键与美术变体的完整约定及文件位置统一记录在 [DungeonLayout 核心 README：房间配置与美术模型关联规则](../Domain/DungeonLayout/README.md#房间配置与美术模型关联规则)。正式配置为 `docs/examples/base-rooms.json`；首个模型身份配置位于仓库相邻目录 `../cave-room-preview/model/room-identity.json`。身份配置属于美术工具层，不向本模块的严格 JSON 格式追加变体字段。

JSON-004 room-catalog-v2的socket必须包含tangentOffsets，旧v1保持旧字段规则。一个socket仍代表一条必连拓扑接口，offset是互斥候选位置，不能把每个候选误作一扇必开的门。配置坐标单位0.5m。
