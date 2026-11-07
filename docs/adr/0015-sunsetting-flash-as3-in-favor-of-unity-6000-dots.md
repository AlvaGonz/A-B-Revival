# 15. Sunsetting Flash AS3 Target in Favor of Unity 6.6 DOTS Runtime

Date: 2026-11-07

## Status

Accepted

## Context

During Phase 1 of *Assembly & Battle Revival*, the team successfully decompiled the original 2011 Flash game into 156 ActionScript 3 classes, stabilized mission autosaves, restored companion persistence, capped particle leaks, and implemented a high-performance Stage3D 60 FPS 1080p GPU baseline (ADR 0013), verified and merged into `main` via PR #135 and #136.

While Stage3D modernised the Flash binary, the underlying single-threaded ActionScript Virtual Machine (AVM2), garbage collector stutters, and archaic tooling (FFDec CLI bytecode patching, Adobe AIR SDK) impose hard ceilings on fleet scale, physics fidelity, and multiplatform distribution.

In ADR 0014, the project initialized the Unity 6.6 (6000.6.8f1) Data-Oriented Design (DoD) target at `A&B-U/`. Spec #129 proved that unmanaged blittable structs (`ShipEntity`, `MountedPartData`, `ProjectileData`) and Burst-compiled `IJobParallelFor` jobs achieve sub-millisecond simulation cycles with zero runtime GC allocations.

Continuing parallel dual-engine development introduces cognitive drag, duplicated test burdens, and split engineering focus.

## Decision

1. **Formal Freeze of ActionScript 3 / Stage3D Target**:
   - Zero new features, UI screens, or bugfixes will be authored in ActionScript 3 (`src/as3/`).
   - The compiled `game/a&b_space_mod.swf` and Adobe AIR descriptors represent the definitive, completed Flash preservation milestone.
2. **Designation of `src/as3/` as Frozen Reference Baseline**:
   - The AS3 codebase remains in the repository in a strictly **read-only** role.
   - It serves exclusively as a mathematical, state-machine, and asset reference for ongoing reverse-engineering while implementing the Unity 6.6 codebase.
3. **Unity 6.6 (6000.6.8f1) as Sole Production Target**:
   - All active gameplay implementation, combat mechanics, AI behaviors, and asset pipelines are developed exclusively within `A&B-U/Assets/Scripts/`.
   - Map #6 is closed as completed/superseded, and outstanding gameplay enhancements (such as Ticket #5 custom weapons) are transferred to Map #122.

## Consequences

### Positive
- **Concentrated Velocity**: 100% of engineering bandwidth is focused on the modern, multithreaded Unity 6.6 engine without FFDec recompilation friction.
- **Modern Tooling & Testing**: Fast NUnit CLI test runs (`dotnet test`), Burst Inspector, Native Profiler, and URP modern shaders replace Flash debugging hacks.
- **True Multiplatform**: Direct native deployment to PC (Vulkan/DirectX 12), Android (Vulkan), and iOS (Metal) without Adobe AIR wrappers.

### Negative / Trade-offs
- The Unity version must absorb all remaining reverse-engineered systems (e.g. mission script parser, shop UI, dialog trees) from the AS3 reference before achieving full campaign parity.
- Contributors must adhere to strict zero-alloc DOTS constraints (`NativeArray<T>`, `[BurstCompile]`, no dynamic LINQ/`new` in simulation loops).

