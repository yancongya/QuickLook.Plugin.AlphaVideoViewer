# QuickLook.Plugin.AlphaVideoViewer

> 落地页源码 / Landing source: [docs/index.html](docs/index.html)。公开地址为 https://yancongya.github.io/QuickLook.Plugin.AlphaVideoViewer/ ；当前部署结果需单独核对 Pages（插件构建成功不代表页面发布成功）。


> 同系列插件 / More plugins: [SvgaViewer](https://github.com/yancongya/QuickLook.Plugin.SvgaViewer) · [PagViewer](https://github.com/yancongya/QuickLook.Plugin.PagViewer) · [PluginManager](https://github.com/yancongya/QuickLook.Plugin.PluginManager)

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
| 普通 MP4/MOV/M4V | — | 严格模式下仅在 `vapc` 或 `FrameProbe` 明确判定 `AlphaLike` 时接管，其余交回内置 VideoViewer，绝不抢普通视频（支持文件名末尾加 `_alpha` / `.alpha` / `-alpha` / `_vap` / `.vap` / `-vap` 强行接管） |

### 功能特性

- **自研识别 = 元数据优先 + 严格像素自检**：`vapc` 命中即定案、跳过一切猜测；无元数据的拼接素材走 Media Foundation 解码像素自检（自动跳过全黑片头关键帧并校验前景彩度）；普通视频自动放行，同时支持文件名标记强行接管
- **自研渲染 = 自写极简 WebGL 合成器 + 按帧回调**：单帧纹理取两个任意源矩形，`rgb × alpha` 预乘输出，由 `requestVideoFrameCallback` 按帧驱动（暂停零空转），并支持「仅 Alpha 遮罩」灰度调试视图
- **流式加载**：通过本地虚拟路由 `WebResourceRequested` + `RangeStream` 按字节切片读取视频，免除全文件 base64 内存拷贝
- 候选方案一键切换（左/右/上/下/四分幅候选/普通视频/仅 Alpha 遮罩），透明合成结果近空时自动退回普通预览
- **单帧步进与倍速播放**：支持上一帧 / 下一帧（`←` / `→` / `Space`）与 `0.5x` / `1x` / `1.5x` / `2x` 倍速切换
- **透明 PNG 导出与复制**：支持一键保存当前帧为透明 PNG（`#btnSave`），或 `Ctrl+C` 直接复制透明 PNG 文件到剪贴板
- **右下角透明「信息与性能」面板**：统一展示显示尺寸、源视频尺寸、文件大小、布局、`VAPX` 标识、时长、实时 FPS、帧耗时波形图（含 `16.67ms` 帧预算参考线与悬停读数），支持拖拽移动、边缘缩放、双击复位与一键复制摘要
- 棋盘格 / 白色 / 黑色 / 自定义背景，循环、进度条拖拽、缩放平移、音量
- 原生黑白灰主题 + 统一旋转 Loading 动画，中英双语全覆盖（含 `Translations.config`）

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
| 播放 / 暂停 | 点击按钮 / `Space` |
| 单帧步进 | 上一帧 / 下一帧按钮 / `←` / `→` |
| 倍速播放 | 底部倍速按钮（`0.5x` / `1x` / `1.5x` / `2x`） |
| 跳转 | 点击/拖拽进度条 |
| 音量 | 鼠标滚轮 / 滑块 |
| 预览模式 | 右上角模式按钮（自动 / 透明合成 / 仅 Alpha 遮罩 / 普通视频） |
| 切换拼接方式 | 右上角翻转按钮 |
| 保存 / 复制 PNG | 右上角保存按钮 / `Ctrl+C` |
| 信息与性能面板 | 右上角信息按钮（悬停预览 / 点击固定，支持拖拽与边缘缩放） |
| 背景 | 右上角背景按钮（右键选择颜色） |
| 缩放/平移 | Ctrl+滚轮 / 拖拽 / 双击复位 |
| 强制接管文件 | 文件名末尾（扩展名前）加 `_alpha` / `.alpha` / `-alpha` / `_vap` / `.vap` / `-vap` |

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

CLI 端到端测试与截图自检：

```powershell
powershell -STA -ExecutionPolicy Bypass -File Scripts/test-cli.ps1 -VerifyFeatures
powershell -STA -ExecutionPolicy Bypass -File Scripts/test-cli.ps1 -ScreenshotPath preview.png
```

### 已知限制

- 严格模式下仅命中 `vapc` 或 `FrameProbe` 明确确认 `AlphaLike` 的素材才接管；若少数极特殊编码或极端低彩素材被放行给原生播放器，可通过文件名添加 `_alpha` / `.alpha` / `_vap` 等标记强行接管
- 腾讯 VAP 的“主区占半幅、遮罩降采样塞进相邻象限”能自动识别，但不解析 `.vap` / ZIP / JSON / YYEVA 元数据，也不还原其动态元素，仅按像素推断的矩形合成
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
| Ordinary MP4/MOV/M4V | — | Strict mode only takes over on `vapc` or confirmed `AlphaLike`; ordinary/ambiguous files fall back to VideoViewer (suffix `_alpha` / `.alpha` / `-alpha` / `_vap` / `.vap` / `-vap` forces takeover) |

### Features

- **Self-written detection = metadata first, strict pixel self-check**: a valid `vapc` box settles alignment immediately; split assets go through a Media Foundation decode pass (skipping black keyframes and checking foreground chroma); ordinary videos are released to the built-in player, with filename marker override when needed
- **Self-written rendering = minimal WebGL compositor + per-frame callbacks**: two arbitrary source rects from one frame texture, premultiplied `rgb × alpha`, driven by `requestVideoFrameCallback` (zero idle GPU spin when paused), plus an Alpha-only grayscale mask debug mode
- **Streaming video loading**: served via `WebResourceRequested` + `RangeStream` byte slices without full-file base64 duplication
- Frame-by-frame stepping (`←` / `→` / `Space`) and playback speed cycling (`0.5x` / `1x` / `1.5x` / `2x`)
- Transparent PNG export (`#btnSave`) and clipboard copy (`Ctrl+C`)
- Bottom-right transparent **Info & Performance** overlay (dimensions, file size, layout, `VAPX` tag, live FPS, frame-time waveform with `16.67ms` budget line, bar hover readout, drag/edge-resize, double-click reset, summary copy)
- Checkerboard / white / black / custom background, loop, seek, zoom & pan, volume
- Native monochrome gray/white styling with unified spinning Loading indicator and full bilingual i18n (`Translations.config`)

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

- Strict mode only takes over on `vapc` metadata or confirmed `AlphaLike` frames; for edge-case files released to the built-in player, append `_alpha` / `.alpha` / `-alpha` / `_vap` / `.vap` / `-vap` before the extension to force takeover
- Tencent VAP "main fills a half, mask downsampled into an adjacent quadrant" is auto-detected, but `.vap` / ZIP / JSON / YYEVA metadata is not parsed and their dynamic elements are not restored — only the pixel-inferred rects are composited
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
