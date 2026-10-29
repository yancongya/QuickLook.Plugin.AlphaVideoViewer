# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- Initial scaffold: WebView2 transparent preview with a self-written minimal WebGL compositor (`alpha-compositor.js`) — two source rects from one frame texture, premultiplied `rgb × alpha`
- Loose detection: MP4 `tkhd` pre-filter + per-frame pixel self-check that scores every candidate (side-by-side / top-bottom halves, Tencent VAP quadrant masks at half resolution) by alpha purity × content colourfulness × bounding-box alignment
- Decode-gate in `CanHandle`: `Probing/FrameProbe.cs` runs Media Foundation `IMFSourceReader` synchronously on the UI thread, samples a few RGB32 frames, and returns `false` for ordinary MP4/MOV/M4V (both halves colourful) so the built-in VideoViewer takes them; genuine Alpha/RGB-split assets continue to be grabbed. Any uncertainty (missing decoder, ambiguous monochrome, decode error) degrades to `Unknown` and preserves the geometry pre-filter result — monotonic-safe "would rather miss than grab"
- Auto fallback to the untouched video preview when no split is found or the composite is essentially empty
- Toolbar candidate flip, background cycle (default/checker/white/black/custom), zoom & pan
- README/AGENTS testing section documenting the native sample corpus and the `?autotest` hook
- CI build + packaging + tag-driven release

### Changed

- `Samples/` flattened from `Samples/external/` into a single directory and trimmed to only the native demos shipped by their official repos (AlphaPlayer ×2, Tencent VAP `vap_demo`, YYEVA ×3); VAP-quadrant regression now keys off the native `vap_demo.mp4`

### Removed

- ffmpeg-generated placeholder samples (`alpha_side_by_side`, `alpha_top_bottom`, `normal_16x9`) and the hand-recombined `*_2to1` / transcoded `vap_*` fixtures — the loose pixel self-check matches the real files directly, so the synthetic ones are no longer needed
- `?autotest=side|top` shortcuts (they pointed at the removed synthetic files); the generic `?autotest=<path>[|WxH]` form stays
