# 模型验证
覆盖 ART001–ART009 的尺寸、几何类别、确定性、设计通路以及GLB导出回读。
有效 Red 保存在 evidence/model-red.txt。正式通行需引擎角色碰撞体验证，测试不代替视觉审阅。

ART051：`node --test tests/identity.test.mjs` 校验房型身份与相邻游戏仓库配置、GLB、模型信息及压缩分片一致；需要两个项目保持相邻目录。
