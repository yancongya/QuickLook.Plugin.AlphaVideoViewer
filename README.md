# QuickLook.Plugin.AlphaVideoViewer

[中文](#中文) | [English](#english)

---

## 中文

### 简介

面向**直播领域**的透明视频预览插件，适配市面主流的直播礼物/特效透明视频方案：**字节 AlphaPlayer**、**腾讯 VAP**、**YYEVA** 等 Alpha/RGB 拼接素材（`.mp4` / `.mov` / `.m4v`）。

直播礼物、开屏特效、互动玩法里的动画，大多把 Alpha 遮罩和 RGB 画面打包在同一个视频里——左右或上下半幅拼接（AlphaPlayer），或遮罩降采样塞进象限（腾讯 VAP）。这类文件用普通播放器预览会露出"半边黑白遮罩"。本插件用**自研的渲染与识别方法**将其合成为真正的透明画面。

### 适配方案

| 方案 | 识别方式 | 说明 |
|------|---------|------|
| 腾讯 VAP v2 | `vapc` 元数据盒直读 | 官方写在文件里的对位坐标（`rgbFrame` / `aFrame`），解析后直送合成器，像素级对位、零猜测 |
| 字节 AlphaPlayer | 逐帧像素自检 | 左右/上下半幅拼接，解几帧按「遮罩纯灰 × 画面彩色 × 包围盒对齐」评分确认 |
| 腾讯 VAP 压平素材 / v1 | 逐帧像素自检 | 无 `vapc` 盒的 VAP 素材，含象限遮罩候选网格 |
| YYEVA（快手） | 逐帧像素自检 | 拼接类素材可识别；纯视频自动交回内置播放器。不解析 YYEVA 元数据 |
| 普通 MP4/MOV/M4V | — | 自检确认两半皆彩色后交回内置 VideoViewer，绝不抢普通视频（宁漏勿抢） |

### 功能特性

- **自研识别 = 元数据优先 + 像素自检兜底**：`vapc` 命中即定案、跳过一切猜测；无元数据的拼接素材走 Media Foundation 解码像素自检；普通视频自动放行
- **自研渲染 = 自写极简 WebGL 合成器**：单帧纹理取两个任意源矩形，`rgb × alpha` 预乘输出，可原生表达主区与遮罩不同长宽比的 VAP 素材（主流库假设两块同长宽比，做不到）
- 像素判据是「遮罩有多纯灰 × 画面有多彩 × 两块包围盒是否对齐」，纯色/降采样遮罩都能命中；合成结果几乎全透明时自动退回普通预览，不会显示一片空白
- 工具栏一键在候选方案间翻转（含切回不处理的原始视频预览），带遮罩区域可视化
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
├── Probing/Mp4Probe.cs                # 顶层 vapc 元数据盒直读 + MP4 tkhd 尺寸 + mvhd 时长 + 几何初筛
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

A [QuickLook](https://github.com/QL-Win/QuickLook) plugin for previewing **live-stream gift/effect transparent videos** — the alpha/RGB-split assets used across the live industry: **ByteDance AlphaPlayer**, **Tencent VAP** and **YYEVA** (`.mp4` / `.mov` / `.m4v`).

Gift animations, splash effects and interactive stickers in live streaming usually pack the alpha mask and the RGB frame into one video — side-by-side or top-bottom halves (AlphaPlayer), or a downsampled mask in an adjacent quadrant (Tencent VAP). Ordinary players show half of it as a black/white mask; this plugin composites them into a true transparent preview using **self-written rendering and detection methods**.

### Supported formats

| Scheme | Detection | Notes |
|--------|-----------|-------|
| Tencent VAP v2 | `vapc` metadata box, direct read | Official alignment coordinates (`rgbFrame` / `aFrame`) written in the file; fed straight to the compositor — pixel-perfect, zero guessing |
| ByteDance AlphaPlayer | Per-frame pixel self-check | Side-by-side / top-bottom halves, confirmed by sampling decoded frames ("mask greyness × frame colourfulness × bounding-box alignment") |
| Flattened VAP / VAP v1 | Per-frame pixel self-check | VAP assets without a `vapc` box, including quadrant-mask candidate grids |
| YYEVA (Kuaishou) | Per-frame pixel self-check | Split assets are recognised; plain videos fall back to the built-in player. YYEVA metadata is not parsed |
| Ordinary MP4/MOV/M4V | — | Once both halves decode as colourful the file is handed back to the built-in VideoViewer — never grabs an unconfirmed match |

### Features

- **Self-written detection = metadata first, pixel self-check as fallback**: a valid `vapc` box settles alignment immediately; flattened split assets go through a Media Foundation decode pass; ordinary videos are released to the built-in player
- **Self-written rendering = a minimal WebGL compositor**: two arbitrary source rects from one frame texture, premultiplied `rgb × alpha`, natively expressing VAP assets whose main and mask have different aspect ratios (mainstream libraries assume equal aspects and cannot)
- Scored by "how grey the mask is × how colourful the frame is × how the two bounding boxes align"; solid or downsampled masks both match, and an essentially-empty composite falls back to plain video instead of showing nothing
- Toolbar button cycles candidates (including a return to the untouched video preview) with mask-region visualisation
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

## Credits

- [QuickLook](https://github.com/QL-Win/QuickLook) by QL-Win
- UI design references official QuickLook plugins (VideoViewer, ImageViewer)
- Sample files come from the official AlphaPlayer / Tencent VAP / YYEVA repositories — sources and licences listed in [Samples/SOURCES.md](Samples/SOURCES.md)

## License

[GPL-3.0](LICENSE)
