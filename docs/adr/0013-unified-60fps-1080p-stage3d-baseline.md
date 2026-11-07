# 0013. Unified 60 FPS Baseline, Native 1920×1080 Widescreen, and Stage3D GPU Architecture

**Date:** 2026-11-07  
**Author:** Architecture & Engineering Team  
**Status:** Superseded by ADR 0014 and ADR 0015 (Flash AS3 Frozen Reference Baseline)  
*Note: Stage3D 1080p GPU baseline and AGAL batching are retired as the completed Flash preservation milestone. Unity 6.6 URP with 1080p native rendering, Sprite Atlases, and BatchRendererGroup serves as the sole active production baseline.*  
**Supersedes/Builds On:**
- `docs/adr/0008-60fps-resolution-modernization-and-leak-prevention.md`
- `docs/adr/0010-fixed-timestep-simulation-and-runtime-performance-gate.md`
- `docs/adr/0011-adobe-air-mobile-target-and-true-60fps-gpu-architecture.md`

---

## Context & Problem Statement

Historically, *Assembly & Battle* executed on a 36 FPS game loop inside a fixed 640×480 Flash Player container. Modernizing through ADR 0008, 0010, and 0011 introduced adapters:
1. **Accumulator and Dual Frame Rate Debt:** Maintaining 36 Hz simulation steps alongside 60 Hz rendering created code dualities (`MoveAs.SIM_SCALE = 0.6`, accumulator substeps, and dual-rate timers), adding cognitive overhead and maintenance costs.
2. **Software Blitter Bottleneck at High Resolution:** Flash Player's single-threaded CPU software rasterizer (`BitmapData.copyPixels`, `applyFilter`, `draw`) struggles to maintain 60 FPS in dense battles, and scaling 640×480 bitmaps to 1080p produces blurry upscaling rather than crisp native graphics.
3. **Clear Strategic Objective:** The engine must establish a **single, unified 60 FPS baseline** and **native 1920×1080 resolution** across the entire codebase—eliminating 36 FPS dual maintenance and shifting rendering to Adobe AIR's Stage3D GPU textured quads.

---

## Decision

1. **Permanent Deprecation of 36 FPS (Unified 60 Hz Lockstep):**
   - Completely decommission 36 FPS across the engine.
   - Run physics simulation, entity movement, combat AI, weapon timers, and rendering in a 1:1 lockstep at 60 Hz (16.666 ms frame budget).
   - Delete the fixed-timestep accumulator (`Fly4UI._simAccumulator`, `SIM_STEP_MS = 27.777778`), inline native 60 Hz constants across all subsystems, and eliminate `MoveAs.SIM_SCALE` runtime conversions.

2. **Native 1920×1080 (16:9 Widescreen) Battlefield Viewport:**
   - Establish $1920 \times 1080$ as the internal canvas and Stage3D viewport dimensions.
   - Expand the horizontal combat field-of-view (FOV) to 16:9 widescreen so ships, projectiles, and stars render in crisp high definition without bitmap stretching.
   - Calibrate the combat camera distance presets (`Cam.as`) to maintain comfortable dogfighting visibility.

3. **Hybrid Stage3D Layering Architecture:**
   - **Layer 1 (Bottom - GPU Stage3D):** Render the space combat simulation (parallax starfields, ship hulls, attached parts, projectiles, laser beams, explosion particles, and additive bloom) via GPU textured quads (`Context3D`). Reduces hundreds of CPU blits to 1–2 GPU draw calls (<0.5 ms).
   - **Layer 2 (Top - Flash DisplayList):** Retain menus, factory assembly screen (`UIMakePlayer`), shop, dialogs, and HUD overlays on the Flash vector DisplayList (`UISP`). This eliminates 100% of the combat render bottleneck while avoiding a massive rewrite of complex UI MovieClips.

4. **Zero-Dependency In-Tree `Stage3DQuadRenderer`:**
   - Because JPEXS FFDec CLI compilation strictly replaces original SWF ABC tags and ignores unlinked external files, third-party frameworks (e.g. Starling) are rejected.
   - Implement a lean, native `Stage3DQuadRenderer` (~200 lines of ActionScript 3) directly in `f4.gfx.RenderData` using raw AGAL vertex/fragment shaders and batched `VertexBuffer3D` / `IndexBuffer3D` buffers.

5. **Boot-Time Dynamic Texture Atlas Generator:**
   - At application boot, unpack embedded vector symbols from `4_all.AllData_data.bin` and rasterize them at high DPI ($2\times$ scale) into a $2048 \times 2048$ GPU `TextureAtlas`.
   - Use linear bilinear filtering without mipmapping (`Context3DTextureFormat.BGRA`, `TextureFilter.LINEAR`, `MipFilter.MIPNONE`) to guarantee sharp vector contours and minimize VRAM footprint.
   - Keeps all game assets self-contained within the compiled SWF/AIR binary with zero external asset loading risks.

6. **16:9 Dynamic Edge-Anchored Tactical HUD:**
   - In `UIGame.as`, anchor HUD widgets dynamically to the 16:9 display bounds (`stage.stageWidth` and `stage.stageHeight`).
   - Player HP is anchored to top-left, Minimap to top-right, Captain distress portrait to bottom-left, and telemetry to bottom-center, providing an open, unobstructed widescreen battlefield view.

7. **Centered Hangar Assembly Grid with Normalized Socket Spacing:**
   - In `UIMakePlayer`, center the ship assembly grid within the 1080p frame with 16:9 framing.
   - Preserve standard integer socket pitch (`F4DataHole`) and coordinates so existing player blueprints and save files load with 100% fidelity.

8. **Dual-Pipeline Graceful Fallback:**
   - If Stage3D `Context3D` is unavailable or initializing (e.g., in headless Flash Player test harnesses or driver initializations), `RenderData` gracefully falls back to the Phase 1 optimized CPU software blitter.
   - Adobe AIR (`renderMode="direct"`) serves as the primary runtime for full GPU hardware acceleration.

---

## Consequences

### Positive
- **Zero Double Maintenance:** A single, unified 60 FPS codebase; no 36 FPS accumulators or dual-rate scaling adapters to maintain.
- **Uncapped Visual Fidelity:** Native 1080p 16:9 presentation with crisp vector rendering instead of blurry upscaled 480p bitmaps.
- **Sub-Millisecond Rendering:** Shifting space combat entities to Stage3D GPU quads slashes render time from ~18 ms down to <0.5 ms, maintaining a locked 60 FPS under heavy combat (500+ projectiles and particles).
- **Safe Evolutionary Path:** Vector UI and complex assembly screens remain completely functional on the DisplayList layer.
- **Automated Verification Compatible:** Retains graceful fallback so headless CI test harnesses continue to pass without a physical GPU.

### Negative / Trade-Offs
- Requires touching coordinate bounds and camera aiming logic for 1920×1080 widescreen space.
- Requires initial boot-time rasterization pass (~300–500 ms on startup) to generate GPU texture atlas from embedded vector definitions.

