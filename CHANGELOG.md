# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Added

- Initial scaffold: WebView2 transparent preview with a self-written minimal WebGL compositor (`alpha-compositor.js`) — two source rects from one frame texture, premultiplied `rgb × alpha`
- Loose detection: MP4 `tkhd` pre-filter + per-frame pixel self-check that scores every candidate (side-by-side / top-bottom halves, Tencent VAP quadrant masks at half resolution) by alpha purity × content colourfulness × bounding-box alignment
- Auto fallback to the untouched video preview when no split is found or the composite is essentially empty
- Toolbar candidate flip, background cycle (default/checker/white/black/custom), zoom & pan
- CI build + packaging + tag-driven release
