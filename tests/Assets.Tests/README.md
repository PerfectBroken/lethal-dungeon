# 资产归档测试
ASSET-001 registry中的(roomId, variantId)唯一且位于仓库内；identity与GLB及model-info一致。
ASSET-002 每变体有概念、源代码、正式模型、材质来源与README；迁移前后正式GLB SHA256相同。
ASSET-003 兼容目录版本按资产声明校验，不能把v1模型冒充v2。运行 node --test tests/Assets.Tests/*.test.mjs。
