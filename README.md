# QuickLook.Plugin.AlphaVideoViewer

[中文](#中文) | [English](#english)

---

## 中文

### 简介

一个用于预览 **Alpha/RGB 拼接透明视频**（`.mp4` / `.mov` / `.m4v`）的 [QuickLook](https://github.com/QL-Win/QuickLook) 插件。

很多动效素材（字节系特效、直播贴纸、H5 模板等）会把 Alpha 遮罩和 RGB 画面拼接在同一个视频里（左右或上下各占一半，腾讯 VAP 还会把遮罩降采样塞进某个象限）。这类文件用普通播放器预览会出现"半边黑白遮罩"，本插件将其合成为真正的透明画面预览。

### 功能特性

- 两级自动识别：MP4 `tkhd` 尺寸做几何初筛，再用 Media Foundation 真解几帧像素自检确认拼接特征（左右 / 上下半幅、VAP 象限遮罩都支持）；普通 MP4/MOV/M4V 一旦确认为两半皆彩色，直接交回内置 VideoViewer（宁漏勿抢）
- 判据是「遮罩有多纯灰 × 画面有多彩 × 两块包围盒是否对齐」，纯色/降采样遮罩都能命中；合成结果几乎全透明时自动退回普通预览，不会显示一片空白
- 工具栏一键在候选方案间翻转（含切回不处理的原始视频预览），带遮罩区域可视化
- 自写极简 WebGL 合成器（单帧纹理取两个源矩形，`rgb × alpha` 预乘输出），可原生表达主区与遮罩不同长宽比的 VAP 素材
- 棋盘格 / 白色 / 黑色 / 自定义背景，循环、进度条拖拽、缩放平移、音量
- 自动隐藏控制栏，适配 QuickLook 暗色/亮色主题
- 中英双语支持

### 安装

1. 从 [Releases](https://github.com/yancongya/QuickLook.Plugin.AlphaVideoViewer/releases) 下载 `.qlplugin` 文件
2. 确保 QuickLook 正在运行
3. 选中 `.qlplugin` 文件，按空格键预览
4. 点击"安装"按钮
5. 重启 QuickLook

### 系统要求

- Windows 10/11
- [QuickLook](https://github.com/QL-Win/QuickLook) 4.x
- [WebView2 Runtime](https://developer.microsoft.com/zh-cn/microsoft-edge/webview2/)（Windows 10/11 通常已预装）

### 从源码构建

```bash
git clone https://github.com/yancongya/QuickLook.Plugin.AlphaVideoViewer.git
cd QuickLook.Plugin.AlphaVideoViewer
dotnet build -c Release
```

打包为 `.qlplugin`：

```powershell
powershell -ExecutionPolicy Bypass -File Scripts/build.ps1
```

### 操作

| 操作 | 方式 |
|------|------|
| 播放/暂停 | 点击按钮 |
| 跳转 | 点击/拖拽进度条 |
| 音量 | 鼠标滚轮 / 滑块 |
| 翻转 Alpha 位置 | 右上角翻转按钮 |
| 背景 | 右上角背景按钮（右键选择颜色） |
| 缩放/平移 | Ctrl+滚轮 / 拖拽 / 双击复位 |

### 项目结构

```
├── Plugin.cs                          # IViewer 入口（Priority 100）
├── Probing/Mp4Probe.cs                # MP4 tkhd 尺寸 + mvhd 时长 + 拼接布局几何初筛
├── Probing/FrameProbe.cs              # Media Foundation 解码像素自检（普通视频交回原生）
├── AlphaVideoViewerPanel.xaml(.cs)    # WebView2 播放面板
├── Resources/Web/
│   ├── alpha-compositor.js           # 自写 WebGL Alpha 合成器（ES module）
│   └── alpha-player.html             # 播放器 UI + 拼接自检
├── Samples/                           # 回归测试素材（仅各官方仓库直接提供的原生文件）
├── Translations.config                # 中英文翻译
├── Scripts/
└── *.csproj / *.sln
```

### 回归测试

`Samples/` 只保留各官方仓库**直接提供**的原生 demo（不再放 ffmpeg 生成或手工重拼的对照素材——宽松像素自检后原生文件即可直接命中）。每个文件的来源、尺寸与预期行为见 [Samples/SOURCES.md](Samples/SOURCES.md)。

| 素材 | 覆盖场景 |
|------|----------|
| `alphaplayer_demo_video.mp4` / `alphaplayer_heartbeats.mp4` | 左右半幅（右 RGB / 左 Alpha）透明合成 |
| `vap_demo.mp4` | 腾讯 VAP「主区半幅 + 遮罩降采样象限」几何 |
| `yyeva_out.mp4` / `yyeva_demo_ufo.mov` | 像素判非拼接 / 合成近乎全透明 → 退回原始画面 |

浏览器侧可用 `?autotest=<url-encoded 路径>[|WxH]` 直接加载某个样本跑检测（仅 harness 用，插件运行不依赖）。

### 已知限制

- 几何初筛（切半后内容宽高比 1:1～2.5:1）无法区分真实拼接与普通视频；Media Foundation 解码像素自检负责确认，命中"两半皆彩色"的普通 MP4/MOV/M4V 会直接交回内置 VideoViewer，无需手动翻转。若机器缺少所需解码器、单色/暗场帧占比过大等无法定论的情况，仍沿用几何初筛接管并交给前端逐帧自检——这是有意的"宁漏勿抢"降级
- 腾讯 VAP 的"主区占半幅、遮罩降采样塞进相邻象限"能自动识别，但不解析 `.vap` / ZIP / JSON / YYEVA 元数据，也不还原其动态元素，仅按像素推断的矩形合成
- 不支持从预览界面保存透明 PNG 帧（WebGL 画布读取限制）
- `.gif` / `.webm` 等其他透明视频容器不在接管范围

---

## English

### Introduction

A [QuickLook](https://github.com/QL-Win/QuickLook) plugin for previewing **alpha/RGB split transparent videos** (`.mp4` / `.mov` / `.m4v`).

Motion-design assets (ByteDance-style effects, live-stream stickers, H5 templates) often pack the alpha mask and RGB frame into one video — side-by-side or top-bottom, and in Tencent VAP the mask is downsampled into an adjacent quadrant. Ordinary players show half of it as a black/white mask; this plugin composites them into a true transparent preview.

### Features

- Two-stage auto-detection: MP4 `tkhd` geometry pre-filter, then a Media Foundation decode pass samples real pixels to confirm the split (side-by-side / top-bottom halves, Tencent VAP quadrant masks all supported); ordinary MP4/MOV/M4V whose decoded halves are both colourful is handed straight back to the built-in VideoViewer (never grabs an unconfirmed match)
- Scored by "how grey the mask is × how colourful the frame is × how the two bounding boxes align"; solid or downsampled masks both match, and an essentially-empty composite falls back to plain video instead of showing nothing
- Toolbar button cycles candidates (including a return to the untouched video preview) with mask-region visualisation
- Self-written minimal WebGL compositor (two source rects from one frame texture, premultiplied `rgb × alpha`) so main and mask can have different aspect ratios
- Checkerboard / white / black / custom background, loop, seek, zoom & pan, volume
- Auto-hide control bar, QuickLook dark/light theme, bilingual UI

### Installation

1. Download `.qlplugin` from [Releases](https://github.com/yancongya/QuickLook.Plugin.AlphaVideoViewer/releases)
2. Ensure QuickLook is running
3. Select the `.qlplugin` file and press Space
4. Click "Install"
5. Restart QuickLook

### Requirements

- Windows 10/11
- [QuickLook](https://github.com/QL-Win/QuickLook) 4.x
- [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/) (usually pre-installed)

### Build from source

```bash
git clone https://github.com/yancongya/QuickLook.Plugin.AlphaVideoViewer.git
cd QuickLook.Plugin.AlphaVideoViewer
dotnet build -c Release
```

Package as `.qlplugin`:

```powershell
powershell -ExecutionPolicy Bypass -File Scripts/build.ps1
```

### Testing

`Samples/` keeps only the native demos shipped **directly by their official repos** — no ffmpeg-generated or hand-recombined fixtures any more, since the loose pixel self-check matches the real files as-is. Each file's source, size and expected behaviour are listed in [Samples/SOURCES.md](Samples/SOURCES.md).

| Sample | Scenario covered |
|--------|------------------|
| `alphaplayer_demo_video.mp4` / `alphaplayer_heartbeats.mp4` | side-by-side halves (RGB right / alpha left) transparent compositing |
| `vap_demo.mp4` | Tencent VAP "main fills a half + downsampled mask quadrant" geometry |
| `yyeva_out.mp4` / `yyeva_demo_ufo.mov` | pixel check says not-a-split / composite ~empty → fall back to plain video |

In a browser, `?autotest=<url-encoded path>[|WxH]` loads a sample straight into the detector (harness only; the plugin runtime does not rely on it).

### Known Limitations

- The geometry pre-filter (halved aspect 1:1–2.5:1) cannot tell real split assets from ordinary videos on its own; the Media Foundation decode pass is what confirms them. Any MP4/MOV/M4V whose decoded halves are both colourful is handed back to the built-in VideoViewer without a manual flip. If the required decoder is missing, sampled frames are dominated by monochrome/dark ambiguity, or the decode path fails, the plugin still takes over and lets the front-end per-frame self-check decide — an intentional "would rather miss than grab" degradation
- Tencent VAP "main fills a half, mask downsampled into an adjacent quadrant" is auto-detected, but `.vap` / ZIP / JSON / YYEVA metadata is not parsed and their dynamic elements are not restored — only the pixel-inferred rects are composited
- Saving transparent PNG frames from the preview is not supported (WebGL readback limitation)
- Other transparent containers (`.gif`, `.webm`) are out of scope

---

## Changelog

See [CHANGELOG.md](CHANGELOG.md)

## License

[GPL-3.0](LICENSE)
