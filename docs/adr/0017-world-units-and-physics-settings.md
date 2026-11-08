# 0017. Pixel-Based World Units, 60 Hz Timestep, and PhysicsCore2D Settings

**Date:** 2026-11-08
**Author:** Architecture & Engineering Team
**Status:** Accepted
**Builds on:**
- `docs/adr/0013-unified-60fps-1080p-stage3d-baseline.md` (60 Hz lockstep, no `SIM_SCALE`)
- `docs/adr/0014-unity-6000-dots-physics-core-2d-migration.md` (PhysicsCore2D target)
- `docs/adr/0016-two-tier-automated-testing-gates-unity-utf.md` (Tier 1 / Tier 2 gates)
- `docs/research/unity-world-units.md` (scale research and threshold table)

---

## Context

Every ported formula from the frozen AS3 baseline (`src/as3`, ADR 0015) uses stage pixels and per-frame constants. A unit conversion layer at each Flash-to-Unity boundary would silently change velocities, ranges, gravity, and projectile lifetimes, making source parity impossible to audit.

PhysicsCore2D follows the Box2D 3.2 solver lineage, which is tuned for metre-scale objects. It provides `PhysicsCoreSettings2D.lengthUnitsPerMeter` so a project can run the solver in its own length unit while the solver's internal tolerances (linear slop, speculative contact distance, AABB margin) are scaled to suit. That setting is global to every physics world in the project, and Unity documents that it must be set once, before any physics object exists; changing it later can produce severe simulation instability.

The project previously ran on Unity's defaults: `Fixed Timestep` 0.02 and no `PhysicsCoreSettings2D` asset assigned.

## Decision

1. **1 Unity unit = 1 stage pixel.**
   - Sprite import uses **Pixels Per Unit = 1**; no gameplay constant is converted. Positions, velocities, ranges, and projectile lifetimes keep their original numbers.
   - `PhysicsCoreSettings2D.lengthUnitsPerMeter` is **100** (100 world units = 1 m), which keeps the solver inside its tuned regime while all game-facing values remain pixels.

2. **`lengthUnitsPerMeter` is one-shot and global.**
   - The settings asset must exist and be assigned before any physics object is created.
   - Changing `lengthUnitsPerMeter` after the first physics object is a migration, not a tuning change. `Assets/Scripts/Editor/PhysicsSettingsConfigurator.cs` creates the asset and assigns it under **Project Settings > Physics Core 2D > Physics Core Settings**.

3. **Physics thresholds are authored in metres and therefore scaled by 100.**

   | Setting | Value |
   |---|---|
   | `PhysicsWorldDefinition.gravity` | `(0, -981)` |
   | `PhysicsWorldDefinition.maximumLinearSpeed` | `40000` |
   | `PhysicsWorldDefinition.bounceThreshold` | `100` |
   | `PhysicsWorldDefinition.contactHitEventThreshold` | `100` |
   | `PhysicsWorldDefinition.contactSpeed` | `300` |
   | `PhysicsWorldDefinition.contactRecycleDistance` | `5` |
   | `PhysicsBodyDefinition.sleepThreshold` | `5` |

   `contactFrequency`, `contactDamping`, `collisionThreshold`, `gravityScale`, `simulationSubSteps`, and `continuousAllowed` are scale-independent and keep their defaults.

4. **`Time.fixedDeltaTime` is 1/60 = 0.0166667 s.**
   - ADR 0013 permanently removed `SIM_SCALE` and the 36 Hz accumulator, so no kinematic conversion exists. Per-frame AS3 constants retain their behaviour at 60 Hz; projectile lifetime remains in seconds.

5. **The baseline is measured, not assumed.**
   - Tier 1: `ProjectConfigurationTests` parses `ProjectSettings/TimeManager.asset` and `PhysicsCoreSettings2D.asset`, failing on any drift in the timestep, assignment, or scaled values.
   - Tier 2 PlayMode: a body dropped 540 px at gravity -981 lands in approximately 1.05 s, and a body spawned at 1000 px/s is not clamped. Both passed with `lengthUnitsPerMeter` 100, confirming the solver does not scale definition values a second time.

## Rejected Options

- **100 px = 1 m (Option B):** physically identical to this decision, but requires x0.01 on every position, velocity, gravity value, ray length, and force crossing the Flash-to-Unity boundary. That is the silent drift a source-parity port must avoid, and it would reverse the frozen baseline's numbers at every call site.
- **Pure MKS world units:** would require rewriting every ported formula and every imported asset's scale, destroying direct comparison with `src/as3`.

## Consequences

### Positive
- All ported formulas keep their frozen AS3 numbers; parity is reviewable value-by-value.
- The solver still receives metre-scale tolerances through one global setting.
- Scale and timestep regressions fail the fast pre-commit gate instead of surfacing in playtesting.

### Negative / Trade-offs
- `lengthUnitsPerMeter` cannot be tuned after the first physics object; experimentation is a project-level migration.
- Unity 2D tooling defaults to 100 PPU, so every imported sprite must explicitly use PPU 1.
- Box2D's 0.1 m floor is 10 px at `lengthUnitsPerMeter` 100; bullets and debris below 10 px are outside the tuned range and need raycasts or `fastCollisionsAllowed`.
