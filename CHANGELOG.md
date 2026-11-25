# Changelog

## [Unreleased]

## [1.1.0] - 2026-11-25

### Added
- Streaming virtual-host video loading (`https://alphavideo.local/`) in `AlphaVideoViewerPanel`, removing full-file `File.ReadAllBytes` + base64 memory duplication
- `requestVideoFrameCallback`-driven WebGL rendering in `alpha-compositor.js`, eliminating GPU idle spinning while paused
- Alpha-only monochrome mask debug view (`MODE_AlphaOnly`) via `uViewMode` uniform in `alpha-compositor.js`
- Transparent PNG current-frame export (`#btnSave`) and clipboard copy (`Ctrl+C`)
- Frame-by-frame stepping (`#btnPrevFrame` / `#btnNextFrame`, `←` / `→` / `Space` shortcuts) and playback speed cycling (`0.5x` / `1x` / `1.5x` / `2x`)
- Unified bottom-right transparent Info & Performance overlay (`#perfOverlay`) with live FPS, frame-time waveform chart (`16.67ms` budget reference line + bar hover readout), drag/edge-resize, double-click reset, summary copy, file size, and `VAPX` tag
- Full bilingual Chinese/English `Translations.config` coverage for all new controls and telemetry labels
- Strict takeover mode in `Plugin.CanHandle` (takes over only on `Vapc` or `SplitCheck.AlphaLike`), plus force-takeover filename markers (`_alpha`, `.alpha`, `-alpha`, `_vap`, `.vap`, `-vap`)
- Unified monochrome spinning Loading indicator in `AlphaVideoViewerPanel.xaml(.cs)` with localized status text
- Headless CLI E2E test & screenshot script (`Scripts/test-cli.ps1`) supporting `-VerifyFeatures` and `-ScreenshotPath`

### Changed
- `Mp4Probe` and `FrameProbe` caches now validate `FileLength` + `LastWriteTimeUtc` fingerprints to prevent stale hits when files are replaced in place
- `FrameProbe` now skips black keyframe intros and uses foreground-only chroma contrast so all 5 official reference samples pass in strict mode
- Replaced `System.Text.Json` with built-in `System.Web.Extensions` (`JavaScriptSerializer`), eliminating `System.Runtime.CompilerServices.Unsafe` binding errors and shrinking `.qlplugin` size (`0.84 MB -> 0.48 MB`)
- Served streaming video via `https://alphavideo.local/__video__` (`WebResourceRequested` + `RangeStream` with CORS) instead of `SetVirtualHostNameToFolderMapping`, fixing `MEDIA_ERR_SRC_NOT_SUPPORTED` (code 4)
- Native monochrome gray/white styling (`--accent: #e0e0e0`), raised control/toolbar z-index (`25`), tightened `#perfOverlay` chart default height (`40px`), and removed top-left `#info` / old `#metaPanel`

## [1.0.0] - 2026-10-29

### Added
- Official VAP `vapc` metadata path: `Mp4Probe` scans top-level boxes for the Tencent VAP v2 `vapc` box and parses its JSON (`videoW/H`, `w/h`, `rgbFrame`, `aFrame`); when present the exact normalized rects go straight to the compositor (pixel-level alignment, no guessing) and `CanHandle` skips the decode self-check. Pixel detection remains the fallback for flattened assets without the box
- WebView2 transparent preview with a self-written WebGL compositor (`alpha-compositor.js`): two source rects from one frame texture, premultiplied `rgb × alpha`
- Loose split detection: MP4 `tkhd` pre-filter + per-frame pixel self-check (side-by-side / top-bottom halves, Tencent VAP quadrant masks at half resolution), scored by alpha purity × content colourfulness × bounding-box alignment
- Decode gate in `CanHandle`: Media Foundation `IMFSourceReader` samples a few RGB32 frames; ordinary MP4/MOV/M4V (both halves colourful) returns `false` so the built-in VideoViewer takes over; any uncertainty (missing decoder, ambiguous monochrome, decode error) keeps the pre-filter result — "would rather miss than grab"
- Auto fallback to the untouched video preview when no split is found or the composite is essentially empty
- Toolbar candidate flip, background cycle (default/checker/white/black/custom), zoom & pan
- `?autotest=<path>[|WxH]` browser harness hook for loading a sample straight into the detector
- README/AGENTS testing section documenting the native sample corpus
- CI build + packaging + tag-driven release

### Changed
- Preview-mode control is now a dropdown list (expand icon on the toolbar): **Auto** (default, pixel self-check decides) / **Alpha compositing** / **Plain video**, replacing the binary toggle; picking Auto re-runs the detection pass
- Alpha-side control is now a dropdown too: **Auto detect** (default) / manual half-split side (left/right/top/bottom) / next quarter-mask candidate, replacing the one-by-one flip cycling; with vapc metadata the manual items are disabled
- Flattened `Samples/external/` into a single `Samples/` and trimmed to the native demos shipped by their official repos (AlphaPlayer ×2, Tencent VAP `vap_demo`, YYEVA ×3); VAP-quadrant regression now keys off the native `vap_demo.mp4`

### Fixed
- `Scripts/update-version.ps1` no longer writes a raw commit hash into `AssemblyVersion` when the repo has no tags yet (`git describe --always` fallback) — local packaging and pre-tag CI builds used to fail with CS7034 (fix applied to all three plugin repos' scripts)

### Removed
- ffmpeg-generated placeholder samples and the hand-recombined `*_2to1` / transcoded `vap_*` fixtures — the pixel self-check matches the real files directly
- `?autotest=side|top` shortcuts (they pointed at the removed synthetic files); the generic `?autotest=<path>[|WxH]` form stays
