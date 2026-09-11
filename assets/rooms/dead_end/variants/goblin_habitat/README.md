# dead_end / goblin_habitat

本目录是该美术变体的唯一正式资产位置。identity.json为既有身份唯一来源；source为可重建源，model为已验收导出，textures为材质，concept为参考，review保存验收证据。验收版本V0.26。

运行仓库根目录 `node tools/room-preview/cli.mjs preview dead_end`；重建用 `node tools/room-preview/cli.mjs model dead_end`，测试用 `node tools/room-preview/cli.mjs test dead_end`。不得修改未授权物体。配置兼容性见registry；仅美术验收，团结运行时尚未集成。旧README中的外部目录引用属于迁移前历史。

---

# 致命地下城：山洞尽头房三维审阅

精细化可编辑程序化模型 v0.3及实时网页，非概念图投影。

- `model/README.md`：模型规范、约束与验证边界。
- `model/room.mjs`：可复现建模源文件，米制，Y向上。
- `model/export.mjs`：GLB导出。执行 `node model/export.mjs && node model/embed-textures.mjs`。
- `public/models/dead-end-cave.glb`：完整房间，12个按类别合并的网格，包含顶、前墙和内部装饰。可导入支持 glTF 的建模软件；团结需使用适配的导入流程。
- `app/`：室内、总览、俯视、洞顶显示、线框、展示补光、通路与概念图对照。
- `tests/`：运行 `node --test tests/*.test.mjs`。
- `evidence/`：有效Red与通过后的测试记录。

预览以30FPS上限节省功耗；不等于微信端性能认证。
岩体、柱子和棚屋的设计包络保存在模型extras中，供后续碰撞体设置参考；尚未创建团结物理组件。
仅静态模型，网页滴水为审阅动画。未实现随机门位或怪物逻辑。门洞示例位于南侧偏东，宽2m、高3m。
制作依据用户确认的概念图 public/reference.png，程序化模型与图有外观细节差异；以网页三维审阅为准。

## 网页规范
VIEW001：主界面直接显示真实三维模型，不以参考图片作为3D画面。
VIEW002：明确显示加载、错误、WebGL失效状态；支持鼠标与触屏旋转缩放。
VIEW003：组件卸载时释放模型、材质、渲染器与事件；后台暂停绘制。
VIEW004：展示灯光不属于导出资产；下载始终提供完整GLB，不受隐藏洞顶影响。
网页契约以TypeScript编译和构建验证；没有声称浏览器交互或手机测试已通过。

精细化版本加入两张生成的基础色纹理和两张由亮度梯度推导的微表面法线纹理（不是扫描测量的真实法线）。纹理已内嵌 GLB，无外部下载依赖。

V0.3：墙脚泥土渐变和碎石、倒三角接顶岩体、分层草束与绑绳、开孔头骨。完整模型无损压缩分片存放在 model/packed，prebuild自动还原GLB；此安排仅用于可靠传输。

常用美术偏好与色值见 [项目记忆](MEMORY.md)，模型直接读取 [配色配置](model/palette.json)。

## 房型关联（当前资产）

配置主来源：`../lethal-dungeon/docs/examples/base-rooms.json`。
- 房型 ID：`dead_end`；配置名：**尽头房**。
- 美术变体 ID：`goblin_habitat`；变体名：**哥布林栖居洞穴**；版本：`0.26`。
- 目标 prefabKey：`dungeon/base/dead_end`（尚未制作团结 Prefab）。
- 机器可读关联：`model/room-identity.json`，同时内嵌模型和模型信息。
后续所有房间均先匹配配置 ID 与名称，再添加美术变体；禁止仅用展示名区分房型。此资产仍是示例门位，坐标/单位/门位需适配后才能接入配置生成器。
