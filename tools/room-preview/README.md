# 房间预览工具

规范：只从 assets/rooms/registry.json 解析房间，所有素材读取仓库内对应变体；.work 中仅有可删除的预览包装和链接。保留原有 Three.js 灯光、镜头与交互，哥布林房页面从原 React 页面挂载，去除原网站部署依赖。

安装：仓库根目录运行 npm ci（Node 22.13+）。

预览：`npm run preview -- dead_end`、`npm run preview -- straight` 或 `npm run preview -- corner_left`。默认端口3010，可用 PORT 指定。第二个参数可指定 variantId。新增变体若有不同交互需求，应扩展适配器选择规则。

测试：`npm run test:assets`。生成模型在变体目录运行 `node source/export.mjs`；尽头房之后还需 `node source/embed-textures.mjs`。生成会修改交付文件，完成视觉验收后才更新registry哈希与版本；本次归档没有重新生成已验收GLB。

适配器只保留预览UI，几何生成源码唯一来源在变体source目录。registry测试验证迁移身份、路径、版本和二进制一致性；各变体测试验证几何与导出规则。视觉验收仍需运行页面检查。
