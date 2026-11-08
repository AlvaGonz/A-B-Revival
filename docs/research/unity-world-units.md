# Research: Unity World Units for the 2D Stage

**Date:** 2026-11-07
**Status:** Complete
**Related:** ADR 0015 (frozen AS3 baseline), `CODING_STANDARDS.md` §2

## Recommendation

Use **Option A: 1 Unity unit = 1 stage pixel (sprite PPU = 1)** and set **`PhysicsCoreSettings2D.lengthUnitsPerMeter = 100`**.

This keeps every value from the frozen AS3 baseline (`src/as3`) in its original unit: a position of 960 stays 960, a speed of 12 stays 12, and no gameplay formula needs a unit suffix or conversion layer. `PhysicsCore2D` explicitly supports this mode: `lengthUnitsPerMeter` tells the solver how many of your units equal one metre, and Unity then scales its internal tolerances (linear slop, speculative contact distance, AABB margin) accordingly. Physically this is the same simulation as Option B (100 px = 1 m); the only difference is that the ×100 lives in one global setting and the physics tunables instead of being applied at every Flash-to-Unity boundary. Option B would put ×0.01 on every position, velocity, gravity value, ray length and force, which is exactly the silent drift a source-parity port should avoid.

No evidence supports a third ratio. Any other value would need the same conversions as B with no compensating benefit.

## Settings

`A&B-U/ProjectSettings/PhysicsCoreProjectSettings2D.asset` currently assigns **no settings asset** (`m_PhysicsCoreSettings: {fileID: 0}`), so Unity's defaults are active. Create a `PhysicsCoreSettings2D` asset, assign it under **Edit > Project Settings > PhysicsCore 2D > Physics Core Settings**, then set the world and body rows under **Default Definitions** in the same asset:

| Property | Value | Note |
|---|---|---|
| `PhysicsCoreSettings2D.lengthUnitsPerMeter` | `100` | 100 world units = 1 m = 100 px. Set once, before any physics object exists; restart play mode. |
| Sprite import **Pixels Per Unit** | `1` | Every stage sprite: 1 texture pixel = 1 world unit. |
| Camera **Orthographic Size** | `540` | Half the 1080 px stage height. |
| `PhysicsWorldDefinition.gravity` | `(0, -981)` | −9.81 m/s² × 100. Only if gravity is simulated. |
| `PhysicsWorldDefinition.maximumLinearSpeed` | `40000` | The cap is in px/s once lum=100. 40000 px/s = 400 m/s; the official example profile's 400 would cap at 4 m/s. |
| `PhysicsWorldDefinition.bounceThreshold` | `100` | 1 m/s × 100. |
| `PhysicsWorldDefinition.contactHitEventThreshold` | `100` | 1 m/s × 100. |
| `PhysicsWorldDefinition.contactSpeed` | `300` | 3 m/s × 100. |
| `PhysicsWorldDefinition.contactRecycleDistance` | `5` | 0.05 m × 100. |
| `PhysicsBodyDefinition.sleepThreshold` | `5` | 0.05 m/s × 100. |
| `PhysicsWorldDefinition.contactFrequency` | `30` | Hz; scale-independent. |
| `PhysicsWorldDefinition.contactDamping` | `10` | Dimensionless; scale-independent. |
| `PhysicsBodyDefinition.collisionThreshold` | `0.5` | Fraction of shape extent, not a length; scale-independent. |
| `PhysicsBodyDefinition.gravityScale` | `1` | Dimensionless. |
| `Time.fixedDeltaTime` | `0.0166667` | 60 Hz, matches the Flash baseline. |

`simulationSubSteps = 4` (default) gives a 240 Hz solve; `continuousAllowed = true` (default) gives dynamic-vs-static CCD. Both are scale-independent and can stay.

## Evidence

### 1. Documented scale guidance and precision at large coordinates

- Box2D 3.2 (the solver lineage PhysicsCore2D follows) is tuned for MKS: moving shapes 0.1–10 m, static shapes up to 50 m, worlds under 16 km. Pixels are discouraged unless a length-units-per-metre conversion is used. <https://box2d.org/documentation/>
- Unity documents the same limit: positions beyond ~16 km lose precision, with 100 km used as the bad-value detection limit. `PhysicsWorld.hugeWorldExtent` is `100000 × lengthUnitsPerMeter`. <https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.U2D.Physics.PhysicsWorld-hugeWorldExtent.html>
- At `lengthUnitsPerMeter = 100`, 16 km is 1,600,000 px. The 1920×1080 stage is 19.2 m wide, about 0.1% of the problem zone. A float32 world coordinate near 1,000,000 px resolves to ~0.06 px, so even a 100k-px level is safe without origin rebasing.

### 2. Which defaults and tunables are scale-dependent

- Auto-scaled by `lengthUnitsPerMeter` (documented in the API source): `linearSlop = 0.005 × lum`, `speculativeContactDistance = 4 × linearSlop`, `aabbMargin = 0.05 × lum`, `hugeWorldExtent = 100000 × lum`. At lum=100 these become 0.5 px, 2 px, 5 px and 10,000,000 px. <https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.U2D.Physics.PhysicsWorld-linearSlop.html>
- Definition values are **not** auto-scaled. The settings source states: "All length values returned from the physics system will also then naturally be in pixels because the physics system does not do any scaling internally, however, you are now responsible for creating appropriate values for gravity, density, and forces." The manual lists bounce threshold, contact hit threshold, contact speed and maximum linear speed in metres per second. Multiply them by 100 (table above). <https://docs.unity.com/en-us/engine/6000.6/manual/unity2d/2d-physics-api/reference/world>
- This is a read of the prose plus the getter source, not an engine measurement. See the verification test in Risks.

### 3. Is PPU = 1 supported?

- There is no documented minimum for sprite PPU; it is an import setting. With PPU 1, one texel is one world unit.
- `lengthUnitsPerMeter` exists precisely so the solver can run in pixel units. The API doc's example: "if your player character is 32 pixels high, then pass 32 to this function. Then you may confidently use pixels for all the length values." <https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.U2D.Physics.PhysicsCoreSettings2D-lengthUnitsPerMeter.html>
- Caveat: Unity 2D tooling defaults to PPU 100, so set PPU 1 explicitly on every imported sprite and on any package that assumes 100.

### 4. 60 Hz timestep and tunnelling

- At 60 Hz, 4000 px/s moves 66.7 px per step. Speculative contacts only reach 2 px at lum=100, so they do not prevent tunnelling at that speed.
- `continuousAllowed` (default true) enables CCD against **static** bodies only. `PhysicsBodyDefinition.fastCollisionsAllowed` extends CCD to dynamic and kinematic bodies but explicitly is not a general dynamic-vs-dynamic solution; it also does not work between two fast bodies.
- For fast projectiles, prefer a `PhysicsQuery` raycast/sweep per step, or enable `fastCollisionsAllowed` on the projectile and keep the receiving bodies non-fast.
- `simulationSubSteps` 4 lowers the effective step to 1/240 s but does not replace CCD.
- Keep `Time.fixedDeltaTime = 1/60` so AS3 constants tuned per frame retain their behaviour.

### 5. Unity 6.6 / PhysicsCore2D caveats

- `lengthUnitsPerMeter` is global and one-shot: "should only be modified before any other calls to the physics system occur and only modified once. Changing this value after any physics object has been created can result in severe simulation instabilities." Changes take effect after exiting play mode. <https://docs.unity.com/en-us/engine/6000.6/manual/unity2d/2d-physics-api/reference/world>
- It lives on the settings asset, not on a world; every world inherits it. `initialWorldCapacity` is restart-scoped, and `concurrentSimulations` is also global.
- `simulationWorkers` defaults to 64 and is capped by the device; changing it continuously recreates the task queue and hurts performance. <https://docs.unity.com/en-us/engine/6000.6/manual/unity2d/2d-physics-api/multithreading>
- `PhysicsCore2D` is separate from the legacy `Physics2D`/`Rigidbody2D` stack. `A&B-U/ProjectSettings/Physics2DSettings.asset` has no effect on it.
- World creation and object create/destroy are main-thread operations; do not call them from jobs.
- The project has no gameplay scripts using `PhysicsCore2D` yet, so adopting lum=100 now costs nothing. After the first physics object exists, changing it is a migration.

## Risks and unknowns

1. **Definition scaling is inferred, not measured.** If the solver already multiplies `gravity`/`maximumLinearSpeed` by lum, the table over-scales by 100. Verify with a drop test: at lum=100 and gravity −981, a body dropped 540 px should land in ~1.05 s (`t = sqrt(2 × 5.4 / 9.81)`); a ~10.5 s fall means gravity is scaled internally. Also spawn a body at 1000 px/s and confirm it is not clamped to 400.
2. **Speed cap.** Leaving `maximumLinearSpeed` at a metre-scale default (400 in Unity's example profile) would cap everything at 400 px/s (4 m/s). Set it explicitly.
3. **Small shapes.** Box2D's 0.1 m floor is 10 px at lum=100. Bullets and debris below 10 px are outside the tuned range; handle them with raycasts or accept some instability. If most gameplay objects are under 10 px, use lum=50 or 32 and scale the table values by the same factor.
4. **Player-size rule of thumb.** Unity suggests lum ≈ player height in pixels (a 32 px player → 32). At lum=100 that player is 0.32 m, still above the 0.1 m floor. Only change lum before the first physics object exists.
5. **Tooling assumptions.** At PPU 1, camera sizes, particle velocities and third-party 2D packages are in pixels. Audit imports for hidden 100 PPU assumptions.
6. **Precision.** Keep gameplay near the origin; origin rebasing is unnecessary below ~100k px.

## Sources

- PhysicsCoreSettings2D API: <https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Unity.U2D.Physics.PhysicsCoreSettings2D-lengthUnitsPerMeter.html>
- PhysicsCore 2D project settings: <https://docs.unity3d.com/6000.6/Documentation/Manual/class-PhysicsCore2DProjectSettings.html>
- World definition reference: <https://docs.unity.com/en-us/engine/6000.6/manual/unity2d/2d-physics-api/reference/world>
- Body definition reference: <https://docs.unity.com/en-us/engine/6000.6/manual/unity2d/2d-physics-api/reference/body>
- Collisions (CCD) guide: <https://docs.unity.com/en-us/engine/6000.6/manual/unity2d/2d-physics-api/interactions/collisions-enable>
- Multithreading: <https://docs.unity.com/en-us/engine/6000.6/manual/unity2d/2d-physics-api/multithreading>
- `PhysicsWorld` source: <https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/PhysicsCore2D/Scripting/PhysicsWorld.cs>
- `PhysicsCoreSettings2D` source: <https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/PhysicsCore2D/Scripting/Assets/PhysicsCoreSettings2D.cs>
- `PhysicsWorldDefinition` source: <https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/PhysicsCore2D/Scripting/PhysicsWorldDefinition.cs>
- Official examples settings (default profile): <https://github.com/Unity-Technologies/PhysicsExamples2D/blob/master/Sandbox/Assets/Settings/PhysicsCoreSettings2D.asset>
- Box2D 3.2 manual, Units: <https://box2d.org/documentation/>
