# corner_left / round_cave

本目录是该美术变体的唯一正式资产位置。identity.json为既有身份唯一来源；source为可重建源，model为已验收导出，textures为材质，concept为参考，review保存验收证据。验收版本R28。

运行仓库根目录 `node tools/room-preview/cli.mjs preview corner_left`；重建用 `node tools/room-preview/cli.mjs model corner_left`，测试用 `node tools/room-preview/cli.mjs test corner_left`。不得修改未授权物体。配置兼容性见registry；仅美术验收，团结运行时尚未集成。旧README中的外部目录引用属于迁移前历史。

---

# R28 最终美术验收：用户95分（2026-09-11）

用户明确“这个房间就这样吧”，左转角房corner_left就此定稿。地面碎石观感仍有不足，作为已知美术备注保留，不继续主动调整。最终源代码及GLB快照approved/r28；R24旧85分基准仍保留。当前预览模型R28，22项回归通过。验收仅指整体美术，运行时门墙装配、碰撞导航与真机性能另行验证。下方为历史记录。

---

# 当前R27：石堆与钟乳石组合修订

移除新增泥土，大石收拢至墙脚，小石减至149块并检查全底面承托。三根加长保持沉积肩、四根原长平滑。R24已验收快照保持，当前细化待用户审阅。详见evidence/REVIEW.md。

# 当前R25：碎石与钟乳石细化，待审阅

R24用户85分已验收版本保存在approved/r24，当前尝试新增小碎石与沉积层钟乳石。当前待用户评价；下方R24验收仅对应历史基准。

# R24 / 0.6 已获用户美术验收（2026-09-11）

用户评价：“这次整体非常好，这个我能给到85分。”房型corner_left，资源键dungeon/base/corner_left，概念V04。保留两道宽偏心弧圈、自然岩肩、统一门墙岩体及既定钟乳石构图。当前模型public/models/corner-left-round-cave.glb，源model/room.mjs，身份model/room-identity.json；20项测试通过记录evidence/r24-green.txt。预览http://localhost:3004/?view=inside，页面R24。

本次验收范围为房间整体美术。GLB仍在导出时按示例清单开门，团结运行时门墙装配、碰撞导航与真机性能尚待工程验证。下方内容为历史记录，以本节及model/review-status.json为准。

---

# 当前R18 / 0.6：V04岩肩过渡
V04概念已获用户认可，模型按西北斜向岩肩、西南不对称圆弧、东南接顶过渡调整。保留配置v2、南西±1m候选门位与清单选择。最新审阅见evidence/REVIEW.md，预览localhost:3004。下方R17为历史。

# 当前 R17 / 0.5：配置驱动随机门位
正式输入：../lethal-dungeon/docs/examples/base-rooms.json（base-rooms-v2，corner_left revision2）。每个南/西接口候选tangentOffsets为[-2,0,2]，单位0.5m，即±1m和中央。其余圆腔保留，南西改为可覆盖候选区的内墙结构。
模型buildRoom({doors:{south:2,west:-2}})接受配置单位的偏移；null为封闭检查。九组合均验证完整门内通路，装饰不得堵住候选净空。
导出入口：npm run model -- --manifest model/server-example.json --instance module_058。默认请求model/preview-request.json。server-example.json来自C#种子5真实结果；不是手工猜测门位。GLB extras与model-info保存doorOffsets/sourceInstance。服务器选一次，客户端不得重新随机。当前预览南门+1m、西门-1m；http://localhost:3004/。
检查规范CL017，测试tests/doors.test.mjs。当前为网页建模/导出链路；团结客户端加载、碰撞导航和动态资源接入尚未验证。首两房模型仍保留v1美术基准，不冒充已适配v2。

---
以下为历史说明，以本节为准。

# 左转角房 · 圆腔石洞

当前资产0.4，模型迭代R16。用户对0.1评60分，旧自评85已撤回；本次调整钟乳石构图、北墙嵌入巨石与墙角碎石，等待复核，不自动宣称85分。

产物 `public/models/corner-left-round-cave.glb`，实际统计见model-info.json；7根钟乳石，左一长三短、右三短。唯一身份来源 `model/room-identity.json`，自动写入GLB根extras和model-info。运行npm run model可重建完整资产。

预览只在显示层镜像X，使北+Z、东+X与图纸的北上东右一致；导出坐标不镜像。俯视使用正交相机。剖切封面与审阅灯光是显示辅助，不进入GLB。后处理和未压缩贴图只用于本地审阅；尚未制作LOD、团结碰撞/导航、真机性能测试或随机门适配。GLB大小不代表运行内存。
独立保存第三房，不修改此前两房。配置身份corner_left，资源键dungeon/base/corner_left，8×8×4米，北+Z，米制。参考public/reference.png，来自概念V03。规范model/README.md，测试tests/README.md；审阅evidence/REVIEW.md。npm run model导出模型；npm run dev预览3003；npm test；npm run build。
当前逐物体内部门槛85分，用户认可与自评分分开记录；未通过内部审阅不交付。模型不是团结引擎运行完成证明。随机门仍为后续适配，本模型是南/西示例接口。

六根短钟乳石现为0.43–0.54米，长石0.78米，竖直主轴保留。随机门位复核见evidence/door-compatibility.md。
