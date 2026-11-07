# Research: Modern Resolution and Asset Rescaling Plan

**Date:** 2026-11-07  
**Parent Ticket:** #56 (Spec: AIR port of A&B to Android)  
**Ticket:** #64 (Research: modern resolution and asset rescaling plan)  
**Status:** Complete  

---

## 1. Executive Summary

*Assembly & Battle: The Story in the Space* was originally designed for a 640×480 Flash Player container. As the game transitions toward modern Desktop (1080p, 1440p, 4K) and Mobile (1080×2400 FHD+) form factors, an asset and resolution modernization plan is required to upgrade the visual fidelity while preserving game stability and performance.

This research document analyzes the current asset architecture, identifies modern scaling opportunities, proposes concrete tooling pipelines, and provides effort/risk projections.

---

## 2. Current Asset Pipeline Analysis

### 2.1 Storage & Embedding Architecture
The game assets are stored as embedded sub-SWF binaries in `src/assets/binaryData/`:
- `9_all.AllData_ui.bin` (485 KB): UI controls, dialogs, forms, fonts (`SF TransRobotics`), HUD.
- `4_all.AllData_data.bin` (181 KB): Spacecraft chassis, modular components, guns, thrusters, shields.
- `8_all.AllData_defPart.bin`, `11_all.AllData_defBullet.bin`, `2_all.AllData_defFire.bin`: XML definitions of parts, projectiles, and emitters.
- `6_all.AllData_sound.bin` (8.15 MB): Embedded 32 kHz sound effects and audio tracks.

### 2.2 Vector vs. Raster Balance
- **Ship Components & Projectiles:** Over 90% of game craft parts and UI symbols are defined as **Flash Vector Shapes** (`DefineShape` / `DefineShape4`). Flash vectors are mathematically resolution-independent: they can be rendered at arbitrary scale without pixelation or blurriness.
- **Combat Blitting Canvas (`RenderData.as`):** The software renderer currently rasterizes combat entities onto fixed $640 \times 480$ 32-bit `BitmapData` buffers (`BMPD`, `BMPDFX`, `BMPDBLTFX`). Upscaling the window via `scaleMode = EXACT_FIT` simply stretches these 480p bitmaps, resulting in soft pixelation on 1080p+ monitors.

---

## 3. Modern Resolution & Scaling Strategies

### Strategy A: High-Resolution Native Blitting Buffer ($1.5\times$ to $2\times$ Scaling)
- **Mechanics:**
  - Increase the internal `RenderData` canvas from $640 \times 480$ to $1280 \times 720$ or $1920 \times 1080$.
  - When calling `fullshit.tool.Attach.attach(symbolName)`, render the underlying vectors directly at $2\times$ scale using `Matrix(2, 0, 0, 2)`.
- **Pros:** Crisp, razor-sharp vector contours on all ships and weapon effects.
- **Cons:** A $1920 \times 1080$ software `BitmapData` requires $4\times$ to $6\times$ more CPU memory bandwidth per frame, which drops software frame rates on low-end hardware.

### Strategy B: Stage3D Texture Atlas Generation (Recommended)
- **Mechanics:**
  - In Adobe AIR with Stage3D enabled (`renderMode="direct"`), unpack vector symbols at initialization into high-resolution GPU Texture Atlases ($2048 \times 2048$ at $2\times$ native vector resolution).
  - Render all ships, weapons, and particle quads via GPU vertex buffers at native screen resolution.
  - UI remains on the native DisplayList vector layer.
- **Pros:**
  - 60 FPS locked performance even at 4K resolution.
  - GPU hardware bloom and additive blending without CPU overhead.
  - Preserves original ActionScript logic completely.

---

## 4. Modern Tooling Pipeline

1. **HARMAN Adobe AIR SDK (Current & Future):**
   - Enables native packaging for Windows Desktop (`.exe`), Android (`.apk` / `.aab`), and iOS (`.ipa`).
   - Supports Stage3D hardware acceleration and multi-resolution manifest configurations.
2. **JPEXS Free Flash Decompiler (FFDec) Automation:**
   - Headless CLI (`scripts/recompile.bat`) allows continuous programmatic modification and repacking of ActionScript and SWF tags.
3. **TexturePacker / Custom Python Atlas Baker:**
   - Automated script extracting vector shapes from `4_all.AllData_data.bin` into rasterized high-DPI sprite sheets for Stage3D deployment.

---

## 5. Effort Estimate & Risk Assessment

| Milestone | Scope | Effort | Risk |
|---|---|---|---|
| **Phase 1: High-DPI Canvas Scaling** | Rescale `RenderData` buffers for 720p/1080p software blitting | 16–20 hrs | Low |
| **Phase 2: Stage3D GPU Texture Atlas** | High-resolution GPU quad batching pipeline | 35–45 hrs | Medium |
| **Phase 3: Responsive UI & Fonts** | Vector font dynamic kerning & responsive HUD | 20–25 hrs | Low |
| **Total** | Full HD/4K Modernization | **70–90 hrs** | Low–Medium |

### Key Risks & Mitigations
- **Risk:** High CPU usage with $1080\text{p}$ software blitting.  
  *Mitigation:* Gate 1080p native blitting to Stage3D GPU mode; fallback to $720\text{p}$ SHOW_ALL for CPU software mode.
- **Risk:** Socket coordinate drift in `UIMakePlayer` assembly screen.  
  *Mitigation:* Keep assembly screen coordinates normalized to fixed 640×480 grid space.
