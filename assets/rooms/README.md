# 房间资产与逻辑房型关联规范

本目录是房间资产的唯一正式来源。registry.json 是关联配置；逻辑房型来自其中 catalogPath 指定的 JSON，不能按中文名称猜测关联。

目录固定为 `<roomId>/variants/<variantId>/`。同一个 roomId 可以包含多个美术变体；variantId 在该房型内唯一，resourceKey 全局唯一。新增直通房美术只增加 straight 下的变体目录和 registry 记录，不复制逻辑房型。

每个变体包含 identity.json（逻辑身份与资产版本）、concept（参考图）、source（生成源代码和输入）、textures（纹理）、model（交付 GLB）、tests、review（审阅记录）、README.md、THIRD_PARTY.md（素材来源与授权）。公共素材未来集中 assets/materials，引用必须明确。

模型内 roomIdentity 必须与 identity.json 一致；catalogVersion、roomRevision、prefabKey 必须与指定目录版本一致。resourceKey 标识美术变体，prefabKey 保留逻辑插槽含义，两者不可混用。已验收模型迁移时校验 SHA256，修改模型后同步资产版本、记录和测试。

前两个房间仍绑定 base-rooms-v1；左转房绑定 v2。不得仅修改标签宣称已适配新门位。当前预览不等于团结运行时随机开门已经集成；服务器选择美术变体和客户端装配尚待实现。

从仓库根目录 npm ci，再运行 npm run preview -- corner_left。具体入口见 tools/room-preview/README.md。构建产物只允许放仓库内 .work，禁止继续向 outputs 下的独立房间目录写入正式资产。历史副本仅供回退。
