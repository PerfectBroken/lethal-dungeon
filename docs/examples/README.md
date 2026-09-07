# 布局样例

prototype-layouts.json由DungeonLayoutTests.OneHundredSeedsProduceValidMultiLevelLayoutsAndPreviewFixtures实际运行导出，包含PrototypeCatalog目录及种子1～6的12房间跨层清单。单位0.5米，轴为X/Y/Z，Y向上；方向North/East/South/West序列化为0/1/2/3，旋转QuarterTurns为0～3。

目录的Boxes为占用包络，Sockets为候选门位；Rooms指定实例变换，Connections指定启用的成对门位及唯一门实体ID。未被连接引用的候选门位封墙。楼梯与坡道的上下门高度相差4米。

重新生成：运行Domain.Tests，将测试输出目录的dungeon-preview.json复制到此处。样例用于审阅；不是正式网络序列化协议，也不包含真实模型或导航。参数与契约见src/Domain/DungeonLayout/README.md。

loop-layouts.json为v0.2环路目录和6份18房间清单；来自DungeonLoopTests批量用例，使用minimumCycles=2、cycleModuleId=junction、requireHeightChange=true、预算10000。每张为19条连接，独立环路数E−V+1=2。loop_前缀标记生成时用于闭环的连接，其物理表示仍与普通门相同，不是单向门或传送。旧prototype-layouts.json保留旧版本结果作为历史样例。
