# 18. Cinematics as Lossy H.264 Video Instead of Vector Animation

**Date:** 2026-11-08
**Author:** Architecture & Engineering Team
**Status:** Accepted
**Resolves:** Ticket #157 (Cinematics: export Mov1-Mov9 to 1080p MP4 and play with skip)
**Builds On:** Spec #152, ADR 0014 (Unity migration), ADR 0015 (frozen AS3 baseline)

---

## Context & Problem Statement

The nine story cinematics (`Mov1`-`Mov9`) are vector `DefineSprite` timelines inside the frozen UI SWF (`9_all.AllData_ui.bin`, symbol ids 717, 708, 704, 685, 681, 669, 657, 647, 633). The Unity production target cannot execute SWF timelines, and the spec explicitly excludes native vector re-authoring. The clips are silent in the source; the audio system supplies sound separately.

The source stage is 640x480 at 50 fps, and every clip letterboxes a 640x360 picture into that stage. The modernized Flash baseline runs at 60 fps (ADR 0013), so the clips advance at 60 fps there.

## Decision

1. Pre-render each clip from the frozen SWF with FFDec and encode the result as H.264 MP4 through Unity's `UnityEditor.Media.MediaEncoder`.
2. Crop the 640x360 picture from the 640x480 stage and upscale 3x to exactly 1920x1080. Encode at 60 fps, matching the modernized Flash baseline.
3. Deterministic filenames: `Mov1.mp4` through `Mov9.mp4`, ordered by `12_all.AllData_movie.bin` and stored in `A&B-U/Assets/Resources/Cinematics/`.
4. One menu item (`Tools/A&B Revival/Export Cinematics`) or one command (`scripts/export-cinematics.bat`) regenerates all nine clips. FFDec CLI is resolved from `FFDEC_CLI`, then the default Windows install path. The pipeline extracts the UI binary from `game/a&b_space_mod.swf` when present, otherwise `game/a&b_space.swf`.
5. Playback is runtime `VideoPlayer` with an event-driven skip/completion path; no managed allocation per frame.

## Consequences

### Positive

- Original artwork and motion are preserved without hand re-authoring.
- Regeneration is reproducible from the frozen binary with one command.
- H.264 1080p is a standard, hardware-decoded format on desktop.

### Negative / Trade-offs

- **Lossy deviation:** the cinematics are no longer vector; scaling beyond 1080p will soften edges and compression artifacts can appear on sharp line art.
- The clips are baked at 60 fps. A future 120 fps presentation would need a re-export (frames can be duplicated, but the source timeline is the limit).
- FFDec is a local external dependency for asset regeneration; it is not required to play or build the committed assets.
- The pipeline is Windows-first because the current FFDec installation is Windows. `FFDEC_CLI` allows another path.
