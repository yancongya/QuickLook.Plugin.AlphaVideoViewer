# 真实世界 Alpha 视频参考素材

拉取日期：2026-10-29。全部来自各开源官方仓库的 demo 素材，仅用于本插件回归测试。

检测采用**宽松识别**：`tkhd` 尺寸只做初筛，真正判定靠前端逐帧像素自检。因此下表多数素材现在都能被
原生接管（旧版「严格 2:1 才接管、其余不接管」的行为已作废），像素判非拼接或合成近乎全透明的才退回原始画面。

| 文件 | 来源 | 尺寸 | 本插件当前行为 |
|---|---|---|---|
| `alphaplayer_demo_video.mp4` | [bytedance/AlphaPlayer](https://github.com/bytedance/AlphaPlayer) `android/gift/demoRes/demo_video.mp4`（Apache-2.0） | 1440×2000 | ✅ 原生接管，半幅（右 RGB / 左 Alpha），透明播放 |
| `alphaplayer_heartbeats.mp4` | 同上 `iOS/Example/.../TestResource/heartbeats/heartbeats.mp4` | 1440×1280 | ✅ 原生接管，半幅（右 RGB / 左 Alpha） |
| `alphaplayer_demo_video_2to1.mp4` | 由 `demo_video` 手拼：左右半各裁出、等比缩放后 pad 成正方形（旧版严格 2:1 时代的产物，现已被原生接管取代，保留作对照） | 2000×1000 | ✅ 接管，透明播放 |
| `alphaplayer_heartbeats_2to1.mp4` | 同上做法（旧版产物，保留作对照） | 2002×1000 | ✅ 接管，透明播放 |
| `vap_demo.mp4` | [Tencent/vap](https://github.com/Tencent/vap) `Android/PlayerProj/app/src/main/assets/demo.mp4`（MIT） | 752×880 | ✅ 接管，VAP 布局（主区占上半、遮罩降采样塞进下半左象限） |
| `vap_1504x1024.mp4` | Tencent/vap 示例素材 | 1504×1024 | ✅ 接管，VAP 布局（主区上半 / 遮罩下-左象限） |
| `vap_1024x1504.mp4` | Tencent/vap 示例素材 | 1024×1504 | ✅ 接管，VAP 布局（主区左半 / 遮罩右-上象限） |
| `vap_1104x1504.mp4` | Tencent/vap 示例素材 | 1104×1504 | ✅ 接管，VAP 布局（主区左半 / 遮罩右-上象限） |
| `vap_750x814.mp4` | Tencent/vap 示例素材 | 750×814 | ✅ 接管，半幅（左 RGB / 右 Alpha）——该素材实为左右半 + 黑边，非长轴切分 |
| `yyeva_demo_pk.mp4` | [yylive/YYEVA](https://github.com/yylive/YYEVA) `img/demo_pk.mp4`（Apache-2.0） | 828×1792 | ⚠️ 像素自检判非拼接 → 以原始画面预览（`mode=video`） |
| `yyeva_out.mp4` | 同上 `img/out.mp4` | 672×704 | ⚠️ 合成近乎全透明 → 自动退回原始画面（`mode=video`，见 compositedVisibility 兜底） |
| `yyeva_demo_ufo.mov` | 同上 `img/demo_ufo.mov` | 672×704 | ⚠️ 依赖元数据声明的遮罩区域，像素推断不稳定 → 退回原始画面 |

关于「内嵌 JSON」的更正：VAP 的遮罩区域/动态元素写在 **`.vap` 容器**的 JSON 头里，`.mp4` 裸视频里
**并没有内嵌 JSON**。本插件不解析 `.vap` 容器，但对已经导出成 `.mp4` 的 VAP 素材，会用像素自检推断
「主区半幅 + 遮罩象限」的几何并直接合成；其矢量/位图动态元素仍不还原。

重拼命令（仅用于生成 `*_2to1.mp4` 旧对照素材，demo_video 为例）：

```bash
ffmpeg -i alphaplayer_demo_video.mp4 -filter_complex \
  "[0:v]crop=720:2000:0:0,scale=360:1000,pad=1000:1000:(ow-iw)/2:(oh-ih)/2:black[m];\
   [0:v]crop=720:2000:720:0,scale=360:1000,pad=1000:1000:(ow-iw)/2:(oh-ih)/2:black[r];\
   [m][r]hstack=inputs=2,format=yuv420p[out]" -map "[out]" -c:v libx264 -crf 22 \
  alphaplayer_demo_video_2to1.mp4
```
