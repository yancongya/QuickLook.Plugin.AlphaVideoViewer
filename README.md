# QuickLook.Plugin.AlphaVideoViewer

[中文](#中文) | [English](#english)

---

## 中文

### 简介

一个用于预览 **Alpha/RGB 拼接透明视频**（`.mp4` / `.mov` / `.m4v`）的 [QuickLook](https://github.com/QL-Win/QuickLook) 插件。

很多动效素材（字节系特效、直播贴纸、H5 模板等）会把 Alpha 遮罩和 RGB 画面拼接在同一个视频里（左右或上下各占一半）。这类文件用普通播放器预览会出现"半边黑白遮罩"，本插件将其合成为真正的透明画面预览。

### 功能特性

- 自动识别 2:1 / 1:2 拼接布局（读取 MP4 `tkhd` 尺寸，不抢普通视频——宁漏勿抢）
- 左右拼接（默认：左 Alpha / 右 RGB）与上下拼接（默认：上 Alpha / 下 RGB）
- WebGL 透明合成实时播放，工具栏一键翻转 Alpha 位置
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
├── Probing/Mp4Probe.cs                # MP4 尺寸探测 + 拼接布局判定
├── AlphaVideoViewerPanel.xaml(.cs)    # WebView2 播放面板
├── Resources/Web/
│   ├── alpha-video-player-js.js       # Alpha 合成库（vendored）
│   └── alpha-player.html              # 播放器 UI
├── Samples/                           # 测试用示例文件
├── Translations.config                # 中英文翻译
├── Scripts/
└── *.csproj / *.sln
```

### 已知限制

- 仅按"宽高比 ≈ 2:1 / 1:2"判定拼接视频，普通宽屏/竖屏视频按比例不会误抢，但恰好 2:1 的普通视频会被接管（可翻转或关闭插件）
- Alpha 半区约定为 R 通道遮罩；带 VAP/YYEVA 元数据（自定义遮罩区域）的素材不做特殊解析，按整半区合成
- 不支持从预览界面保存透明 PNG 帧（WebGL 画布读取限制）
- `.gif` / `.webm` 等其他透明视频容器不在接管范围

---

## English

### Introduction

A [QuickLook](https://github.com/QL-Win/QuickLook) plugin for previewing **alpha/RGB split transparent videos** (`.mp4` / `.mov` / `.m4v`).

Motion-design assets (ByteDance-style effects, live-stream stickers, H5 templates) often pack the alpha mask and RGB frame side-by-side or top-bottom into one video. Ordinary players show half of it as a black/white mask; this plugin composites them into a true transparent preview.

### Features

- Auto-detects 2:1 / 1:2 split layouts by reading MP4 `tkhd` dimensions — never grabs ordinary videos
- Side-by-side (default: alpha left) and top-bottom (default: alpha top) conventions
- Real-time WebGL alpha compositing via `alpha-video-player-js`; one-click alpha-side flip
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

### Known Limitations

- Detection is aspect-ratio based (≈2:1 / ≈1:2); a normal video that happens to be exactly 2:1 will be taken over
- Only full-half R-channel masks are composited; VAP/YYEVA metadata (custom mask regions) is not parsed
- Saving transparent PNG frames from the preview is not supported (WebGL readback limitation)
- Other transparent containers (`.gif`, `.webm`) are out of scope

---

## Changelog

See [CHANGELOG.md](CHANGELOG.md)

## License

[GPL-3.0](LICENSE)
