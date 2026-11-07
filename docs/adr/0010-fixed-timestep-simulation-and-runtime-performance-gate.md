# 10. Fixed-Timestep Simulation Loop, Runtime Performance Gate & Hallmark Resolution Selector

Date: 2026-11-07

## Status
Superseded by ADR 0014, ADR 0015, and ADR 0016.
*Note: Flash accumulator hacks and Flash Player trace gates (`scripts/test_perf_runtime.py`) are retired. Unity 6.6 executes physics at deterministic 60 Hz in `FixedUpdate` (PhysicsCore2D / Burst jobs), and runtime testing is governed by the two-tier automated harness in ADR 0016.*

## Context
Following the initial 60 FPS modernization in ADR 0008, playtesting revealed two critical gameplay regressions:
1. **Game Acceleration (+67% speedup):** While event timers were rescaled by $60/36$, the ActionScript 3 physics simulation (`OpOneCore.runGame()`) executes translation and rotation vectors directly per `ENTER_FRAME`. Ticking physics 60 times/sec rather than 36 times/sec accelerated entity velocity, turning rates, and bullet speeds by $1.67\times$.
2. **CPU Rendering Saturation & Frame Jitter (~28 FPS):** Flash Player's software rasterizer and un-throttled blitting pipeline (`RenderData.render()`) could not complete logic + software blits within the 16.6 ms frame budget under heavy scenes, dropping frames down to ~28 FPS.
3. **Lack of Automated Runtime Behavioral & Performance Gates (Issue #90):** Previously, all quality gates were compile-time only (`lint_domain_leakage.py` and `recompile.bat`). The agent could not autonomously verify real FPS or memory stability.
4. **Missing In-Game Resolution Selection:** The original `UI_Options` vector MovieClip lacked dedicated controls for cycling resolutions (640x480, 720p, 1080p, Fullscreen).

## Decision

1. **Fixed-Timestep Simulation Loop (Decoupled 36 Hz Simulation / 60 Hz Stage):**
   - In `Fly4UI.as:loop()`, implemented a fixed-timestep accumulator pattern (`SIM_STEP_MS = 27.777778 ms`, representing exact 36 Hz simulation steps).
   - `core.runGame()`, `uictrl.run()`, and sound loops only advance in deterministic 36 Hz slices, clamped to a maximum of 3 substeps per frame with a 100 ms delta cap to prevent spirals of death.
   - Restored original 36 Hz timing constants across `EvtCtrl`, `AIData`, `AIPart`, `AIRunMode`, `AIUnity`, `FireTimer`, `Item`, `UIGame`, `UIHelpX2`, and `UIMovie`.
   - Result: 100% authentic physics velocity, weapon cooldowns, and bullet travel, completely immune to frame rate fluctuations.

2. **Automated Runtime Performance Quality Gate (`scripts/test_perf_runtime.py`):**
   - Implemented an automated test harness leveraging the standalone Flash Player debug binary (`flashplayer11_9r900_152_win_sa_debug.exe`) and trace capture (`flashlog.txt`).
   - Enabled automatic debug telemetry initialization when `flash.system.Capabilities.isDebugger` is detected.
   - Script runs the SWF headlessly for a specified duration, parses `[PERF_METRICS]`, verifies absence of fatal ActionScript exceptions, and enforces gates for minimum FPS and maximum memory footprint.

3. **Hallmark-Crafted Resolution Selector in `UIOptions.as`:**
   - Injected an interactive, chamfered display selector into `UIOptions` positioned at $(318, 352)$ using the game's native visual geometry tokens and typography.
   - Provides cyclic switching across: `640 x 480 (Native)`, `1280 x 720 (HD)`, `1920 x 1080 (FHD)`, and `Fullscreen`.
   - Wired into `changes()` dirty checking and `okFunc()` persistence in `ConfigAndStory.CONFIG` and `SharedObject` (`fly4`).

## Consequences

- Game speed is completely stable and authentically paced at 1:1 original velocity.
- The stage runs up to 60 FPS without accelerating combat simulation.
- The agent can autonomously execute behavioral and performance tests via `python scripts/test_perf_runtime.py`.
- Players can adjust resolution directly from the in-game options menu with immediate visual and audio feedback.

