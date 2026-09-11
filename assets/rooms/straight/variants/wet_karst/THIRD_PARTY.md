# 素材来源
- Rock Boulder Dry：Dimitrios Savva（摄影）、Rico Cilliers（处理），Poly Haven，CC0。https://polyhaven.com/a/rock_boulder_dry 。2K Diffuse、OpenGL Normal、ARM，用于母岩/沉积矿物，结合顶点泥色和粗糙度表达潮湿。原素材为户外干岩纹理，不声称扫描本洞穴。
- Brown Mud 02：Rob Tuytel，Poly Haven，CC0。https://polyhaven.com/a/brown_mud_02 。2K Diffuse、OpenGL Normal、ARM，用于泥地和潭底。
- 许可：https://polyhaven.com/license 。API文件清单存 source/*-source.json；素材存 textures，GLB嵌入6张图。
- 残骨：Artec 3D Human skeleton HD，CC BY 4.0，https://www.artec3d.com/3d-models/human-skeleton-hd ，https://creativecommons.org/licenses/by/4.0/ 。沿用第一房已提取减面的骨片，重新旋转放置并改为棕黄色。source/bone-scans.json 保存原来源。为人体扫描，不是动物骨。
- 概念：本项目 ImageGen V02。洞穴几何为本项目生成，不是下载的完整洞穴。

- Rock Face 01：Dario Barresi，Poly Haven，CC0。https://polyhaven.com/a/rock_face_01 。实际下载2K glTF扫描模型、BIN及三张贴图。提取几何与UV，对坐标做缩放/倾斜用于左侧岩体，模型封装内嵌纹理共9张。
- 水面微波法线：本项目数学生成，source/export.mjs 内可复现。不是下载的水面扫描。

R18新增 unified-rock-{diffuse,normal,arm}.jpg：基于现有Poly Haven Rock Boulder Dry（CC0）贴图，进行三向投影、棕色/积土乘色及法线坐标转换后烘焙。主岩体仍使用原扫描几何；不把程序烘焙声称为新的真实扫描素材。
