# AGENTS.md

## Build Commands

```bash
# Build
dotnet build -c Release

# Package（快速打包；CI 用 Scripts/build.ps1 -SkipBuild -SkipVersion，命名只在 build.ps1 维护）
powershell -ExecutionPolicy Bypass -File Scripts/pack-zip.ps1
```

## Project Overview

QuickLook plugin for previewing alpha/RGB split transparent videos (MP4/MOV).
Uses WebView2 + alpha-video-player-js for WebGL alpha compositing.
Only takes over files whose dimensions match a 2:1 / 1:2 alpha-layout convention (Mp4Probe);
ordinary videos stay with the built-in VideoViewer ("宁漏勿抢").

## Key Files

- `Plugin.cs` - IViewer entry point (Priority 100), CanHandle via Mp4Probe
- `Probing/Mp4Probe.cs` - ISO BMFF tkhd size reader with 60s cache
- `AlphaVideoViewerPanel.xaml.cs` - WebView2 panel, manages lifecycle and theme
- `Resources/Web/alpha-player.html` - Player UI (HTML/CSS/JS)
- `Resources/Web/alpha-video-player-js.js` - alpha compositing library (ES module, vendored)

## Architecture

```
Plugin.cs (IViewer)
  └── AlphaVideoViewerPanel (WPF UserControl)
        └── WebView2
              └── alpha-player.html
                    └── alpha-video-player-js.js (WebGL compositing)
```

## Layout Convention

- Default: left half = Alpha (R channel), right half = RGB (字节 AlphaPlayer style) → `orientation:'landscape', side:'back'`
- Top/bottom split → `orientation:'portrait'`, alpha on top
- Reversed assets: user flips via the toolbar button (`player.updateConfig({side})`)
- Supported extensions: .mp4 / .mov / .m4v only; no ZIP/JSON/VAP-metadata/YYEVA handling

## CI / Release

- push `master` / PR / 手动：构建 + 打包，上传 Artifact（14 天），**不**发 Release
- tag `v*`：构建打包后自动创建 GitHub Release，资产为 `QuickLook.Plugin.AlphaVideoViewer.qlplugin`
- 打包命名唯一维护在 `Scripts/build.ps1`；workflow 调用它，**禁止**在 workflow 内联第二份打包逻辑
- 发版只需 `git tag vX.Y.Z && git push --tags`，无需手动上传

## Conventions

- Target framework: .NET Framework 4.6.2
- Namespace: `QuickLook.Plugin.AlphaVideoViewer`
- Follow official QuickLook plugin structure and quicklook-plugin-ship SKILL.md
- XAML files at project root (not in subdirectories)
- Web assets in `Resources/Web/`
- `.ps1` files must stay UTF-8 with BOM (PowerShell 5.1 Chinese comments)
