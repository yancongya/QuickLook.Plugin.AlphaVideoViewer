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
Two-stage detection: MP4 `tkhd` geometry pre-filter, then a Media Foundation decode pass
(`Probing/FrameProbe.cs`) samples RGB32 pixels on the UI thread to confirm the split layout
automatically. Ordinary MP4/MOV/M4V whose halves are both colourful returns `false` from
`CanHandle` so the built-in VideoViewer takes them; a toolbar button cycles candidates and
can fall back to the plain, non-composited video preview when the front-end self-check
disagrees. Any decode failure or ambiguous frame degrades to `Unknown` and preserves the
geometry grab — monotonic-safe "宁漏勿抢".

## Key Files

- `Plugin.cs` - IViewer entry point (Priority 100), CanHandle = Mp4Probe geometry → FrameProbe decode gate
- `Probing/Mp4Probe.cs` - ISO BMFF tkhd size + mvhd duration reader with 60s cache
- `Probing/FrameProbe.cs` - Media Foundation `IMFSourceReader` RGB32 pixel self-check (hides manual COM vtable dispatch)
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

## Testing

- `Samples/` holds only native demos pulled directly from their official repos (AlphaPlayer ×2,
  Tencent VAP `vap_demo`, YYEVA ×3). Do NOT add ffmpeg-generated or hand-recombined fixtures —
  the loose pixel self-check matches the real files as-is. Provenance/expected behaviour: `Samples/SOURCES.md`.
- Browser harness: serve the repo root over http and open
  `Resources/Web/alpha-player.html?autotest=<url-encoded sample path>[|WxH]`; it loads the file
  straight into the detector. This hook is dev-only — the plugin runtime never uses it.
- FrameProbe harness: `scratch/fp/` (meta workspace) compiles `Mp4Probe.cs` + `FrameProbe.cs`
  into `fpharness.exe`; run it with and without `FPSEEK=1` to verify both the seek-sampled
  and sequential paths classify each sample as `AlphaLike` (grab) or `NotAlpha` (native).
  GUIDs must come from `Windows Kits/10/Include/…/um/mfapi.h` — do not trust web snippets.
- QuickLook only scans the plugin dir at process start, so a rebuilt `.qlplugin` needs a restart to load.

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
