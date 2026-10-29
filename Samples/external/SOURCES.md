# 真实世界 Alpha 视频参考素材

拉取日期：2026-10-29。全部来自各开源官方仓库的 demo 素材，仅用于本插件回归测试。

| 文件 | 来源 | 尺寸 | 本插件预期行为 |
|---|---|---|---|
| `alphaplayer_demo_video.mp4` | [bytedance/AlphaPlayer](https://github.com/bytedance/AlphaPlayer) `android/gift/demoRes/demo_video.mp4`（Apache-2.0） | 1440×2000 | **不接管**（非 2:1，走内置 VideoViewer） |
| `alphaplayer_heartbeats.mp4` | 同上 `iOS/Example/.../TestResource/heartbeats/heartbeats.mp4` | 1440×1280 | **不接管** |
| `alphaplayer_demo_video_2to1.mp4` | 由 demo_video 重拼：左右半各裁出、等比缩放后 pad 成正方形，左 Alpha / 右 RGB | 2000×1000 | ✅ 接管，透明播放（沙漏礼物动效） |
| `alphaplayer_heartbeats_2to1.mp4` | 同上做法 | 2002×1000 | ✅ 接管，透明播放（心跳 + 心电图） |
| `vap_demo.mp4` | [Tencent/vap](https://github.com/Tencent/vap) `Android/PlayerProj/app/src/main/assets/demo.mp4` | 752×880 | **不接管**（VAP 用内嵌 JSON 元数据声明遮罩区域，本插件按约定不解析） |
| `yyeva_demo_pk.mp4` | [yylive/YYEVA](https://github.com/yylive/YYEVA) `img/demo_pk.mp4`（Apache-2.0） | 828×1792 | **不接管** |
| `yyeva_out.mp4` | 同上 `img/out.mp4` | 672×704 | **不接管** |
| `yyeva_demo_ufo.mov` | 同上 `img/demo_ufo.mov` | 672×704 | **不接管** |

结论：业界真实素材大多**不是**严格 2:1 / 1:2（半区常被缩放或以元数据声明），
「宁漏勿抢」策略下这些文件全部安全地留在内置播放器；`*_2to1.mp4` 两个是把 AlphaPlayer
素材重拼成我们约定的布局，用于验证真实内容的透明合成。

重拼命令（demo_video 为例）：

```bash
ffmpeg -i alphaplayer_demo_video.mp4 -filter_complex \
  "[0:v]crop=720:2000:0:0,scale=360:1000,pad=1000:1000:(ow-iw)/2:(oh-ih)/2:black[m];\
   [0:v]crop=720:2000:720:0,scale=360:1000,pad=1000:1000:(ow-iw)/2:(oh-ih)/2:black[r];\
   [m][r]hstack=inputs=2,format=yuv420p[out]" -map "[out]" -c:v libx264 -crf 22 \
  alphaplayer_demo_video_2to1.mp4
```
