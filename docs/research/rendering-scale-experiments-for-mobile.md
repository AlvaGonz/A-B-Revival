# Research: Rendering-Scale Experiments for Mobile

**Date:** 2026-11-07  
**Parent Ticket:** #56 (Spec: AIR port of A&B to Android)  
**Ticket:** #63 (Research: rendering-scale experiments for mobile)  
**Status:** Complete  

---

## 1. Executive Summary

The original Flash engine for *Assembly & Battle* was authored in 2011 for a fixed 640×480 (4:3) canvas. Modern mobile devices predominantly feature 16:9, 19.5:9, and 20:9 ultra-wide displays (e.g., 2400×1080, 2560×1440). 

We investigated three rendering-scale architectures:
1. **Option 1: Fixed 4:3 with Pillarboxing (`StageScaleMode.SHOW_ALL`)**
2. **Option 2: Full Dynamic Stage Resize (`StageScaleMode.NO_SCALE` throughout)**
3. **Option 3: Hybrid Adaptive Widescreen (Recommended: Widescreen Combat + Centered Menus)**

We recommend **Option 3 (Hybrid Adaptive Widescreen)** as the optimal balance between visual excellence and zero-risk menu stability.

---

## 2. Detailed Options Assessment

### Option 1: Fixed 4:3 with Aspect Pillarboxing (`StageScaleMode.SHOW_ALL`)
- **Mechanics:** The Flash runtime scales the 640×480 stage to fit the device's vertical height and inserts vertical black pillar bars on the left and right.
- **Pros:**
  - Zero code modifications required for the 156 ActionScript classes.
  - Mouse/touch input coordinates automatically map to the virtual 640×480 stage.
  - Shipbuilder (`UIMakePlayer`) grid sockets and drag-drop physics behave identically.
- **Cons:**
  - Wastes up to 35% of horizontal screen real estate on modern 19.5:9 smartphones.
  - Perceived as a dated legacy port by modern mobile users.

### Option 2: Full Dynamic Stage Resize (`StageScaleMode.NO_SCALE` throughout)
- **Mechanics:** The stage runs at native device resolution or a fixed vertical height (e.g., height = 480, width = $480 \times \text{aspectRatio} \approx 960$ to $1040$).
- **Impact on Combat:**
  - `Cam.rect` and `RenderData.bmp` allocate canvas matching the native aspect ratio.
  - Combat dogfighting feels modern, spacious, and immersive.
  - Zero distortion or stretch.
- **Impact on UI & Menus:**
  - Flash UI assets (`9_all.AllData_ui.bin`) were authored as fixed 640×480 MovieClips with hardcoded element positions.
  - Menus pinned to `(0, 0)` shift off-center; backgrounds fail to fill the edges.
  - Assembly drag-and-drop (`UIMakePlayer`, `F4DataHole`) uses local-to-global coordinate conversions that risk offsets if container scaling is misaligned.
- **Cons:** High migration effort across ~30 UI screens (~40-60 engineering hours).

### Option 3: Hybrid Adaptive Widescreen (Recommended)
- **Architecture:**
  - **In Menus & Shipbuilder:** Set `stage.scaleMode = StageScaleMode.SHOW_ALL`. The 640×480 UI remains perfectly centered with zero risk of coordinate drift or socket misalignment in `UIMakePlayer`. Pillar bars can be filled with subtle dark starry nebula textures.
  - **In Combat (`OpOneCore` & `RenderData`):** Switch to widescreen viewport. The camera buffer `RenderData.bmp` dynamically dimensions to $W = 480 \times \text{aspectRatio}$ (e.g., $960 \times 480$). Combat HUD elements (`UIGame`) anchor to screen corners using existing responsive anchoring helpers.
- **Effort Estimate:** **12–16 engineering hours** (confined strictly to `RenderData.as`, `Cam.as`, and `UIGame.as`).

---

## 3. Impact Matrix

| Dimension | Option 1 (Pillarbox) | Option 2 (Full Dynamic) | Option 3 (Hybrid Recommended) |
|---|---|---|---|
| **Space Combat Immersion** | Low (Letterboxed) | High (Widescreen) | **High (Widescreen)** |
| **Touch Aim Accuracy** | Native (Auto-scaled) | Requires scale factor | **1:1 Native Canvas** |
| **Shipbuilder Stability** | 100% Preserved | High risk of offset | **100% Preserved** |
| **Menu Layout Risk** | Zero | High | **Zero** |
| **Implementation Effort** | 0 hours | 40–60 hours | **12–16 hours** |

---

## 4. Final Recommendation & Implementation Roadmap

1. **Adopt Option 3 (Hybrid Adaptive Widescreen).**
2. In `scripts/` and `src/as3/scripts/f4/gfx/RenderData.as`:
   - Dimension `RenderData.bmp` based on display aspect ratio ($H = 480$, $W = \text{round}(480 \times \text{stageWidth} / \text{stageHeight})$).
   - Update `Cam.rect` to match $W \times H$.
3. Keep `UIMakePlayer`, `UIShop`, and `UIRepair` within their validated 640×480 layout boundaries.
