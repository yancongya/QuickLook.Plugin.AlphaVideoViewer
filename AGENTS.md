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
Uses WebView2 + a self-written minimal WebGL compositor (`alpha-compositor.js`) that samples
two source rects from one frame texture and outputs `rgb × alpha` with premultiplied blending.
Detection is loose: pixel self-check picks the split layout automatically; a toolbar button
cycles candidates and can fall back to the plain, non-composited video preview.
Ordinary videos stay with the built-in VideoViewer ("宁漏勿抢").

## Key Files

- `Plugin.cs` - IViewer entry point (Priority 100), CanHandle via Mp4Probe
- `Probing/Mp4Probe.cs` - ISO BMFF tkhd size reader with 60s cache
- `AlphaVideoViewerPanel.xaml.cs` - WebView2 panel, manages lifecycle and theme
- `Resources/Web/alpha-player.html` - Player UI + auto-detection + layout candidates
- `Resources/Web/alpha-compositor.js` - self-written WebGL alpha compositor (ES module)

## Architecture

```
Plugin.cs (IViewer)
  └── AlphaVideoViewerPanel (WPF UserControl)
        └── WebView2
              └── alpha-player.html
                    └── alpha-compositor.js (WebGL compositing)
```

## Layout Convention

- Candidates are generated from frame geometry: strict left/right & top/bottom halves
  (`pack:'half'`) plus Tencent-VAP style where RGB fills one half and the mask sits in a
  quadrant of the other half at half resolution (`pack:'vap'`).
- `runAutoDetect` samples several frames, scores each candidate by alpha purity × content
  colorfulness × region alignment, and applies the best; if the composited frame is ~empty
  it falls back to plain video.
- Manual flip cycles candidates; there is no fixed side/orientation handed over from C#.
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
