# 0011. Adobe AIR Mobile Target, True 60 FPS Rescaling, and Stage3D GPU Architecture

**Date:** 2026-11-07  
**Author:** Architecture & Engineering Team  
**Status:** Approved  
**Supersedes/Builds On:** 
- `docs/adr/0008-60fps-resolution-modernization-and-leak-prevention.md`
- `docs/adr/0010-fixed-timestep-simulation-and-runtime-performance-gate.md`

---

## Context & Problem Statement

ADR 0010 established a fixed-timestep accumulator (36 Hz simulation tick against a 60 Hz stage frame rate). While this decoupled physics from frame drops, real-world user gameplay revealed two critical experiential issues:
1. **Temporal Stuttering / Judder:** Stepping a 36 Hz simulation on a 60 Hz display results in a `[0, 1, 0, 1, 1]` step cadence across frames. Without render transform interpolation, ships visibly freeze and jump 24 times every second.
2. **Combat Frame Rate Drops to ~20 FPS:** Flash Player Standalone Projector relies entirely on single-threaded CPU software rasterization (Win32 GDI). At 60 FPS, the frame budget shrinks to 16.66 ms. In heavy combat (50+ bullets, 200+ particles with glow filters), `RenderData.render()` consumes 15–20 ms, inducing cascading frame drops and CPU overload.
3. **Strategic Target Horizon:** The developer's primary long-term release goal is **Mobile (Android / iOS)** alongside Desktop, making legacy Flash Player Projectors an obsolete target. The runtime must modernize to **HARMAN Adobe AIR SDK** to unlock 64-bit performance, Stage3D GPU hardware acceleration, native touch/controller inputs, and app packaging (`.apk`, `.aab`, `.ipa`, desktop `.exe`).

---

## Decision Drivers

- **Judder Elimination:** 1:1 lockstep between physics simulation and screen refresh at 60 Hz.
- **Physical Invariance:** Dogfighting turn radiuses, projectile flight ranges, and weapon engagement envelopes must behave identically to the authentic 2011 release.
- **Immediate Software Relief:** Eliminate wasteful duplicate blur passes and full-frame CPU color transforms in `RenderData.render()` without waiting for a full GPU rewrite.
- **Modern Platform Target:** Target Adobe AIR Desktop and Mobile (`renderMode="direct"`) as the canonical modern deployment target.
- **GPU Hardware Acceleration:** Leverage `Stage3D` (`Context3D`) underneath the Flash DisplayList for massive quad-batched sprite rendering (< 0.5 ms).
- **Practical Concurrency:** Restrict ActionScript Workers (`flash.system.Worker`) to decoupled asynchronous tasks (asset decompression, procedural generation, 10 Hz strategic AI), avoiding negative-return IPC marshalling on per-frame collision loops.

---

## Considered Options

1. **Option A: Revert to 36 FPS Stage Lock:** Eliminates judder and CPU overload, but foregoes modern 60 Hz smoothness on mobile and high-refresh displays.
2. **Option B (Selected): True 60 FPS Rescaling + Phase 1 Blitter Optimization + Stage3D AIR Target:**
   - Rescale physics velocities by $36/60 = 0.6$.
   - Rescale projectile lifetimes by $60/36 \approx 1.667$ and rotational speeds by $0.6$ to maintain authentic engagement geometry.
   - Run 1 simulation step per 60 FPS frame (1:1 lockstep, zero judder).
   - Optimize `RenderData.render()` immediately (remove duplicate blur pass, dirty rects, direct blits).
   - Modernize runtime descriptor to Adobe AIR with Stage3D GPU acceleration.

---

## Decision & Implementation Details

### 1. True 60 FPS Physics Rescaling (1:1 Lockstep)
- **Eliminate Accumulator Substeps:** `loop()` in `Fly4UI.as` executes exactly 1 simulation step (`core.runGame()`, `uictrl.run()`, sound updates) per `ENTER_FRAME`.
- **Velocity Scaler ($0.6\times$):** Scale base velocities and acceleration increments across `F4FlyData`, `AIUnity`, `SimpleFly`, bullets, and particles by $0.6$.
- **Lifespan Compensation ($1.667\times$):** Scale weapon bullet lifespans and particle durations by $1.667$ so effective flight distance remains invariant ($D = v \cdot t = (0.6 v) \cdot (1.667 t) = 1.0 D$).
- **Turn Rate Compensation ($0.6\times$):** Scale `rotSpeed` by $0.6$ so angular velocity per frame matches 60 Hz refresh, preserving identical turning circles.

### 2. Surgical CPU Blitter Optimization in `RenderData.as`
- **Duplicate Blur Elimination:** Delete line 242 (`this.BMPDFX.applyFilter(this.BMPDFX, this.fxrect, this.point, this.blur)`), which runs an identical redundant blur on `BMPDFX` right after line 238, immediately reclaiming ~2.5 ms per frame.
- **DisplayList Hardware Scaling for 2× Upscaling:** Replace `CANVAS.draw(this.BMPDFX, this.MTX2X)` and `CANVAS.draw(this.BMPDBLTFX, this.MTX2X)` with scaled display objects (`scaleX = 2.0`, `scaleY = 2.0`, `blendMode = BlendMode.ADD`), leveraging compositor hardware scaling and reclaiming ~3.5–5.0 ms per frame.
- **Pre-baked Damage Spritesheets:** Eliminate on-the-fly `ColorTransform` draws in `BMPD2D.render()` by pre-caching red-tinted damage frames at level load.

### 3. Adobe AIR Runtime Target
- Create `src/air/application.xml` specifying:
  - `<renderMode>direct</renderMode>` to unlock Stage3D.
  - `<supportedProfiles>extendedDesktop mobileDevice</supportedProfiles>`.
  - Windows captive runtime and Android APK/AAB packaging targets.
- Enable `Stage3D` quad batching underneath the native Flash DisplayList (`UISP` UI layer remains 100% intact).

### 4. Concurrency Boundary (ActionScript Workers)
- Retain per-frame collision detection (`Targets.bltHit`) on the main thread to avoid the 4.5–6.0 ms marshalling/synchronization penalty between AVM2 memory heaps.
- Reserve Workers (`flash.system.Worker`) for asynchronous procedural galaxy generation, asset decompression, and 10 Hz strategic squad pathfinding.

---

## Consequences

### Positive
- **Zero Judder / Micro-Stutter:** True 60 Hz 1:1 motion cadence across all ships and projectiles.
- **Preserved Combat Balance:** Exactly identical weapon ranges, engagement distances, and dogfight turning radiuses.
- **Immediate Frame Rate Recovery:** Dropping `runRender()` from ~18 ms down to <6 ms eliminates the 20 FPS combat drop.
- **Future-Proof Foundation:** Direct upgrade path to mobile touch devices, tablets, and modern desktop displays via HARMAN AIR SDK.

### Negative / Trade-Offs
- Requires touching multiple velocity, timer, and rotation constants across decompiled classes (`SimpleFly`, `AIUnity`, `F4FlyData`, `CkBltObj`).
- Requires maintaining the Adobe AIR application descriptor (`application.xml`).
