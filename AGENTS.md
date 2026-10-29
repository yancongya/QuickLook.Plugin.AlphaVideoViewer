# AGENTS.md

## Build Commands

```bash
# Build
dotnet build -c Release

# Package（快速打包；CI 用 Scripts/build.ps1 -SkipBuild -SkipVersion，命名只在 build.ps1 维护）
powershell -ExecutionPolicy Bypass -File Scripts/pack-zip.ps1
```

## Project Overview

QuickLook plugin for previewing **live-stream gift/effect transparent videos** (MP4/MOV/M4V):
ByteDance AlphaPlayer, Tencent VAP, YYEVA and other alpha/RGB-split assets used across the
live industry. Rendering and detection are both self-written:

- **Rendering** — WebView2 + a minimal self-written WebGL compositor (`alpha-compositor.js`)
  that samples two arbitrary source rects from one frame texture and outputs `rgb × alpha`
  with premultiplied blending, so main and mask can have different aspect ratios (VAP).
- **Detection — metadata first, pixel self-check as fallback**: a Tencent VAP v2 `vapc`
  top-level box carries official alignment coordinates (`rgbFrame`/`aFrame`); `Mp4Probe`
  parses it and the exact rects go straight to the compositor (no guessing, decode gate
  skipped). Without `vapc`, MP4 `tkhd` geometry pre-filters, then a Media Foundation decode
  pass (`Probing/FrameProbe.cs`) samples RGB32 pixels on the UI thread to confirm the split
  layout automatically. Ordinary MP4/MOV/M4V whose halves are both colourful return `false`
  from `CanHandle` so the built-in VideoViewer takes them; a toolbar button cycles candidates
  and can fall back to the plain, non-composited video preview when the front-end self-check
  disagrees. Any decode failure or ambiguous frame degrades to `Unknown` and preserves the
  geometry grab — monotonic-safe "宁漏勿抢".

## Key Files

- `Plugin.cs` - IViewer entry point (Priority 100), CanHandle = Mp4Probe (vapc metadata → geometry pre-filter) → FrameProbe decode gate
- `Probing/Mp4Probe.cs` - ISO BMFF reader with 60s cache: top-level `vapc` box (VAP metadata, may precede `moov`), tkhd size, mvhd duration
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

- **vapc first**: `Mp4Probe` scans top-level boxes for the Tencent VAP v2 `vapc` box (it can
  precede `moov`); a parseable payload means official alignment coordinates — `Plugin.CanHandle`
  takes the file without the pixel self-check, and the exact normalized rgbFrame/aFrame rects are
  handed to the compositor via the `loadFile` message (`msg.vapc`). No guessing for official VAP.
- Without vapc, candidates are generated from frame geometry: strict left/right & top/bottom
  halves (`pack:'half'`) plus Tencent-VAP style where RGB fills one half and the mask sits in a
  quadrant of the other half at half resolution (`pack:'vap'`) — this path serves truly flattened
  assets (e.g. AlphaPlayer's side-by-side packing).
- `runAutoDetect` samples several frames, scores each candidate by alpha purity × content
  colorfulness × region alignment, and applies the best; if the composited frame is ~empty
  it falls back to plain video. Skipped entirely when host vapc metadata is present.
- Manual flip cycles candidates; with vapc the candidate list is a single element (metadata is
  the one solution), so flipping is a no-op.
- Preview mode is a dropdown (toolbar expand icon): Auto (default, `runAutoDetect` decides) /
  Alpha / Plain video. `runAutoDetect` early-returns when `viewMode !== 'auto'`; picking Auto
  resets `viewMode` to `auto` and re-runs detection.
- Supported extensions: .mp4 / .mov / .m4v only; no ZIP/JSON/YYEVA handling
  (`isVapx==1` per-frame rect sequences are parsed but not yet rendered)

## Testing

- `Samples/` holds only native demos pulled directly from their official repos (AlphaPlayer ×2,
  Tencent VAP `vap_demo`, YYEVA ×3). Do NOT add ffmpeg-generated or hand-recombined fixtures —
  the loose pixel self-check matches the real files as-is. Provenance/expected behaviour: `Samples/SOURCES.md`.
- Browser harness: serve the repo root over http and open
  `Resources/Web/alpha-player.html?autotest=<url-encoded sample path>[|WxH][&vapc=<url-encoded vapc JSON>]`;
  it loads the file straight into the detector (with `&vapc` it exercises the official-metadata
  path instead). This hook is dev-only — the plugin runtime never uses it.
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
- Follow official QuickLook plugin structure and quicklook-plugin-ship SKILL.md (see VideoViewer, ImageViewer)
- XAML files at project root (not in subdirectories)
- Web assets in `Resources/Web/`
- `.ps1` files must stay UTF-8 with BOM (PowerShell 5.1 Chinese comments)
