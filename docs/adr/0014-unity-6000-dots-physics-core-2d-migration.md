# 0014. Unity 6.6 (6000.6.8f1) Migration: DOTS Job System, PhysicsCore2D, Burst, and BatchRendererGroup

**Date:** 2026-11-07  
**Author:** Architecture & Engineering Team  
**Status:** Approved  
**Supersedes/Builds On:**
- `docs/adr/0009-monolith-decomposition-and-domain-seams.md`
- `docs/adr/0010-fixed-timestep-simulation-and-runtime-performance-gate.md`
- `docs/adr/0011-adobe-air-mobile-target-and-true-60fps-gpu-architecture.md`
- `docs/adr/0013-unified-60fps-1080p-stage3d-baseline.md`

---

## Context & Problem Statement

The ActionScript 3 (AS3) codebase, even when modernized with Stage3D GPU quads and 60 FPS lockstep (ADR 0013), operates within the single-threaded garbage-collected runtime of the Flash/AIR platform. While viable for preservation and modding, scaling to large-scale fleet battles (20+ wingmen, hundreds of simultaneous projectiles, modular damage states, and tactical AI routines) encounters fundamental architectural ceilings:
1. **Garbage Collection Jitter:** Object-oriented allocation patterns (`new FlyPart()`, dynamic Arrays, blitting objects) trigger periodic GC pauses that breach the 16.666 ms frame budget.
2. **Single-Core Simulation Bottleneck:** Projectile integration, collision sweeps, and fleet steering runs on a single CPU core.
3. **Target Platform Expansion:** Preserving *Assembly & Battle* long-term requires modern multi-platform delivery (PC, mobile, web) using industry-standard tooling with native 64-bit performance.

The target environment is **Unity 6.6 (6000.6.8f1)** located at `A&B-U`, using the Universal Render Pipeline (URP), C# Job System, Burst compiler, and multithreaded 2D physics.

---

## Decision

1. **Architecture Pivot to Data-Oriented Design (DoD):**
   - Replace heap-allocated ActionScript classes (`SimpleFly`, `F4DataBullet`, `AIUnity`) with blittable unmanaged `struct` components laid out in contiguous memory (`NativeArray<T>`).
   - Static configuration and metadata (part catalogs, hull definitions, tech trees) are authored as immutable `ScriptableObject` assets.
   - Simulation state (position, velocity, health, orientation, timers) resides strictly in flat value-type buffers processed by parallel jobs.

2. **Multithreaded Physics via PhysicsCore2D (`Unity.U2D.Physics`):**
   - Decommission legacy AS3 custom bounding box collision routines (`CkBltObj`, `Targets.chackAllHit`).
   - Implement collision detection using the multithreaded `PhysicsCore2D` API. Projectile raycasts and hull overlap tests execute across worker threads using Burst-compiled jobs.

3. **C# Job System & Burst Compilation:**
   - Simulate fleets, projectile trajectories, and Captain tactical AI modes (ADR 0006) using `IJobParallelFor` decorated with `[BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]`.
   - Ensure linear scaling across all available CPU cores on mobile (ARM big.LITTLE) and desktop (x86_64).

4. **BatchRendererGroup (BRG) & Modular Sprite Atlases:**
   - Modular spacecraft are composed of multiple distinct parts (chassis, wings, engines, hardpoints). To avoid draw-call fragmentation, pack modular 1080p sprites into unified Sprite Atlases (ASTC for mobile, BC7 for PC/desktop).
   - Render ships, projectiles, and starfields using `BatchRendererGroup` with GPU instancing. Pass per-part visual parameters (damage flashes, tinting, faction identifiers) via `unity_RendererUserValue` instanced properties to maintain a single draw call per material.

5. **Strict Zero-Allocation Runtime Policy:**
   - Zero dynamic allocations (`new`, boxing, LINQ, closures, dynamic `foreach`) inside `Update`, `FixedUpdate`, or `LateUpdate`.
   - All scratch buffers, collision query results, and entity transformations must use preallocated `NativeArray<T>` containers with `Allocator.Persistent` or `Allocator.TempJob`.

6. **Isolated Assembly Definitions (`.asmdef`):**
   - Decompose the Unity codebase into isolated assemblies to guarantee sub-second compile and test execution:
     - `ABRevival.Core.asmdef`: Core logic, data structs, jobs, and physics (with `allowUnsafeCode: true`).
     - `ABRevival.Tests.asmdef`: Unity Test Framework (NUnit) suites targeting formulas and domain logic.

---

## Domain Entity Mapping (AS3 -> Unity C# DoD)

| ActionScript 3 Entity (OOP / Monolith) | Unity 6000.6 Architecture | Primary Storage / Pipeline |
| :--- | :--- | :--- |
| `F4DataOrg` (Part definition catalog) | `PartDefinitionSO` (`ScriptableObject`) | Read-only asset catalog in `Assets/Data/` |
| `FlyData` / `plMc` (Craft blueprint) | `CraftBlueprintSO` / `CraftLayout` struct | Serialized JSON / ScriptableObject |
| `SimpleFly` / `FlyBase` (Active ship) | `ShipEntity` (`struct`) + `ShipStateData` | `NativeArray<ShipEntity>` in `ShipSimulationJob` |
| `F4PartMC` / `FlyPart` (Mounted module) | `MountedPart` (`struct`) | Contiguous array of parts per craft |
| `F4DataBullet` / `CkBltObj` (Projectiles) | `ProjectileData` (`struct`) | `NativeArray<ProjectileData>` in `ProjectileMoveJob` |
| `AIUnity` (Captain & wingman AI) | `TacticalAIJob` (`IJobParallelFor`) | Burst-compiled multi-threaded execution |
| `RenderData` / Stage3D quads | `BatchRendererGroup` + URP 2D | GPU instanced draw calls via unified atlas |
| `UIShop` / `UIMakePlayer` (Assembly UI) | UI Toolkit (`UIDocument`) + Presenter | Domain events bound to `CraftBlueprint` |

---

## Consequences

- **Pros:**
  - Sustained 60–120 FPS on low-power mobile devices and desktops under maximum combat load (hundreds of entities).
  - Clean separation between immutable configuration data and high-speed simulation data.
  - Sub-second unit test iteration through isolated assembly definitions.
  - Elimination of Flash/AIR platform deprecation risks.
- **Cons:**
  - Requires migrating from traditional OOP inheritance to data-oriented struct transformations.
  - Zero-alloc constraint necessitates strict code review discipline and automated allocation checks.

