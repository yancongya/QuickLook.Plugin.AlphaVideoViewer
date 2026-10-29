# Changelog

## [Unreleased]

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
- Flattened `Samples/external/` into a single `Samples/` and trimmed to the native demos shipped by their official repos (AlphaPlayer ×2, Tencent VAP `vap_demo`, YYEVA ×3); VAP-quadrant regression now keys off the native `vap_demo.mp4`

### Fixed
- `Scripts/update-version.ps1` no longer writes a raw commit hash into `AssemblyVersion` when the repo has no tags yet (`git describe --always` fallback) — local packaging and pre-tag CI builds used to fail with CS7034 (fix applied to all three plugin repos' scripts)

### Removed
- ffmpeg-generated placeholder samples and the hand-recombined `*_2to1` / transcoded `vap_*` fixtures — the pixel self-check matches the real files directly
- `?autotest=side|top` shortcuts (they pointed at the removed synthetic files); the generic `?autotest=<path>[|WxH]` form stays
