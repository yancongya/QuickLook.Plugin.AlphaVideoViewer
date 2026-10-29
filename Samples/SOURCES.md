# 真实世界 Alpha 视频参考素材

本目录只放**各开源官方仓库直接提供的原生 demo 素材**（仅用于本插件回归测试）。
早先用 ffmpeg 生成的纯色/普通视频占位样本、以及手工重拼的 `*_2to1` 对照素材已全部删除——
检测改为宽松逐帧像素自检后，原生素材即可直接命中，不再需要人造样本。

检测采用**宽松识别**：`tkhd` 尺寸只做初筛，真正判定靠前端逐帧像素自检。像素判非拼接、
或合成近乎全透明的，自动退回原始画面预览（`mode=video`），不会显示一片空白。

| 文件 | 来源 | 尺寸 | 本插件当前行为 |
|---|---|---|---|
| `alphaplayer_demo_video.mp4` | [bytedance/AlphaPlayer](https://github.com/bytedance/AlphaPlayer) `android/gift/demoRes/demo_video.mp4`（Apache-2.0） | 1440×2000 | ✅ 原生接管，半幅（右 RGB / 左 Alpha），透明播放（沙漏礼物动效） |
| `alphaplayer_heartbeats.mp4` | 同上 `iOS/Example/.../TestResource/heartbeats/heartbeats.mp4` | 1440×1280 | ✅ 原生接管，半幅（右 RGB / 左 Alpha），心跳 + 心电图 |
| `vap_demo.mp4` | [Tencent/vap](https://github.com/Tencent/vap) `Android/PlayerProj/app/src/main/assets/demo.mp4`（MIT） | 752×880 | ✅ 接管，VAP 布局（主区占上半、遮罩降采样塞进下半左象限） |
| `yyeva_out.mp4` | [yylive/YYEVA](https://github.com/yylive/YYEVA) `img/out.mp4`（Apache-2.0） | 672×704 | ⚠️ 合成近乎全透明 → 自动退回原始画面（`mode=video`，compositedVisibility 兜底） |
| `yyeva_demo_ufo.mov` | 同上 `img/demo_ufo.mov` | 672×704 | ⚠️ 依赖元数据声明的遮罩区域，像素推断不稳定 → 退回原始画面 |

关于「内嵌 JSON」：VAP 的遮罩区域/动态元素写在 **`.vap` 容器**的 JSON 头里，`.mp4` 裸视频里
**并没有内嵌 JSON**。本插件不解析 `.vap` 容器，但对仓库里已导出成 `.mp4` 的 VAP 素材
（如 `vap_demo.mp4`），会用像素自检推断「主区半幅 + 遮罩象限」的几何并直接合成；
其矢量/位图动态元素仍不还原。

> 说明：腾讯 VAP 仓库里多数示例是 `.vap` 容器而非散装的 alpha 拼接 `.mp4`，能直接拿来当原生
> 测试的只有 `vap_demo.mp4` 这类少数散文件；因此 VAP 象限遮罩的回归以 `vap_demo.mp4` 为准。
