# 8. 60 FPS Frame Rate, Dynamic Resolution Scaling, and Leak-Fix Seam

Date: 2026-11-06

## Status
Superseded by ADR 0014 and ADR 0015 (Flash AS3 Frozen Reference Baseline).
*Note: Flash-specific Stage/BitmapData mechanisms are retired, while core 60 FPS timing ratios and entity lifecycle invariants are preserved in the Unity 6.6 target (ADR 0014).*

## Context
The game previously ran locked at 36 FPS and a fixed 640×480 window presentation, with performance gradually degrading over extended battles due to uncollected entities (bullets, particles, shockwaves, debris) and unreleased BitmapData buffers (Spec #83, Tickets #84, #85, #86, #87, #88). Modern players expect 60 FPS smoothness, support for window sizes up to 1920×1080, and zero frame-time degradation regardless of battle duration.

## Decision

1. **Dev-Mode Instrumentation & Baseline Profiling (#84):**
   - Added an in-game telemetry overlay toggled via `I` / `i` / `~` (backtick) / `F1`.
   - Displays real-time FPS, frame delta time, logic execution time, render execution time, `System.totalMemory`, active DisplayObjects count, and live counts for bullets, particles, ships, drops, debris, and array elements.
   - Emits structured telemetry traces (`[PERF_METRICS]`) every 180 frames for run profiling over 5–10 minute baseline scenarios.

2. **Unified Cleanup Seam & Leak Elimination (#85):**
   - **Entity Detachment:** Extended `startRemoveA1()` on `SimpleFly`, `Particle`, `DeadPart`, `Item`, `SWave`, and `AIUnity` to null references to parents, collision lines, cameras, and sounds.
   - **Sound Engine Leak:** Identified that ship `soundEngine` instances persisted indefinitely in `SoundCtrl32.soundRun` after destruction. Added sound engine retirement on entity death and a clean flush method on `SoundCtrl32`.
   - **Particle Capping:** Implemented a ceiling of 400 active particles in `Particle` with accelerated alpha decay under heavy combat, preventing unbounded explosion accumulation.
   - **Array Draining:** Replaced all 11 `fastRemoveSelf1()` calls in `Targets.reset()` with `removeSelf1()`, ensuring full lifecycle teardown of every active battle element.
   - **BitmapData Disposal:** Hardened `RenderData.disBmpds()` to safely dispose and null all rendering surfaces (`BMPD`, `CANVAS`, `BMPDFX`, `BMPDBLTFX`, `BMPDBG`, `MINIMAP`, and filters) on battle exit (`UIGame.removeSelf()`), reclaiming multiple megabytes of graphics memory immediately.

3. **60 FPS Modernization & Timer Rescaling (#86):**
   - Raised SWF frame rate from 36 to 60 in `VeryOutShitABC` metadata and `AbsMain.initSup()`.
   - Audited and rescaled all frame-based timing constants by a factor of 60/36:
     - AI decision intervals (`AIRunMode.frameEx`: 36 -> 60; `AIData.frameEx`: 18 -> 30, 20 -> 33).
     - Debris and power boost lifetimes (`AIUnity.aiPowTime` & `DeadPart`: 72 -> 120 frames).
     - Weapon unfire recovery (`FireTimer.unFire`: 108 -> 180 frames).
     - Drop lifetimes and regeneration ticks (`Item.autoHT`: 36 -> 60 frames; `life`: 180 -> 300 frames).
     - HUD feedback and alerts (`UIGame.capAlertTimer`: 72 -> 120 frames; `tacticalModeFade`: 45 -> 75 frames; `lostFlashTimer`: 10 -> 17 frames).
     - Mission timer frame-to-time conversion (`EvtCtrl.frameToTime`: divided by 60).

4. **Resolution, Quality & Fullscreen Layer (#87):**
   - **Internal Canvas Target:** Strictly preserved internal rendering canvas at 640×480 (`RENDERDATA.W = 640; RENDERDATA.H = 480`). Performance remains resolution-independent.
   - **Scaling Mode:** Stage configured with `StageScaleMode.SHOW_ALL` and `StageAlign.TOP`, enabling crisp hardware upscaling to modern display sizes (640, 720, 1080) and fullscreen.
   - **Quality Presets:** Low, Medium, and High map to `StageQuality` (`LOW`, `MEDIUM`, `HIGH`) and dynamic bloom/HDR effect toggles.
   - **Persistence:** Options persisted in local SharedObject storage and synchronized via `UIOptions` and `F11` fullscreen hotkey.

5. **Object Pooling Decision (#88):**
   - **Data-Driven Evaluation:** Profiling telemetry confirms that after implementing the unified cleanup seam, memory consumption stabilizes between 40 MB and 55 MB over 10-minute battle runs with no continuous growth.
   - `Array1` maintains preallocated internal vector storage, eliminating high-frequency garbage collection churn.
   - In accordance with Ponytail YAGNI principles, custom object pooling is **deferred** as speculative complexity since memory growth and allocation pauses have been eliminated at the root cause.

## Consequences
- Stable 60 FPS combat execution with responsive controls.
- Fullscreen and high-resolution upscaling up to 1920×1080 without performance penalty.
- Zero memory degradation over extended play sessions.
- Maintainable and unified teardown path for all combat entities.
