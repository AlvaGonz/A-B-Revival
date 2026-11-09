---
name: ab-physics-tuning
description: Tune the project's PhysicsCore2D simulation for stable 60 Hz motion — fixed stepping, render interpolation, speed caps and swept queries instead of discrete tunneling, world-unit scale, contact filters, mass/drag and sleeping. Use when motion jitters, projectiles tunnel, stacks explode, or physics feels floaty in this Unity 6.6 project.
---

# Physics tuning (PhysicsCore2D)

> **Project constraint (A&B Revival).** This project uses `Unity.U2D.Physics` (`PhysicsCore2D`, struct-based, multithreaded). Do not introduce `Rigidbody2D`, `Collider2D` or legacy `Physics2D` examples. One Unity unit is one stage pixel; `lengthUnitsPerMeter` is 100 and is one-shot (ADR 0017). Keep writes on the main thread and allocation-free in the step. The repository workflow (wayfinder -> to-spec -> to-tickets -> TDD -> review) prevails over any step in this skill.

Most "bad physics" here is not an engine bug: it is a mismatch between the fixed simulation step and the variable render loop, an un-swept fast mover, or a world-scale change applied after bodies exist.

## Project baseline

| Setting | Value | Source |
|---|---|---|
| World API | `Unity.U2D.Physics` (`PhysicsWorld`, `PhysicsBody`, `PhysicsShape`) | `ProjectileContactSolver.cs` |
| Units | 1 unit = 1 pixel, `lengthUnitsPerMeter = 100` | `PhysicsSettingsConfigurator.cs`, ADR 0017 |
| Gravity | `(0, -981)` px/s^2 | `PhysicsSettingsConfigurator.cs` |
| Max linear speed | `40000` px/s | `PhysicsSettingsConfigurator.cs` |
| Fixed step | `1 / 60` s | `GameFlowController`, `WorldUnitPhysicsTests` |
| Sleep threshold | `5` | `PhysicsSettingsConfigurator.cs` |

Use the settings asset at `Assets/Settings/PhysicsCoreSettings2D.asset`; run **Tools > A&B Revival > Apply Physics Core 2D World-Unit Baseline** before creating bodies. Changing `lengthUnitsPerMeter` after bodies exist is a migration, not a setting.

## Core workflow

1. **Simulate on the fixed step.** Integrate velocities and call `world.Simulate(Time.fixedDeltaTime)` from `FixedUpdate`. Never integrate with `Time.deltaTime` in `Update`.
2. **Interpolate only presentation.** The renderer may lerp between previous and current physics poses; the simulation stays at 60 Hz.
3. **Sweep fast movers.** Projectiles use `Ballistics.SweepSegment` and a ray cast from previous to current position (`ProjectileContactSolver.Step`). Do not rely on discrete contacts for bullets.
4. **Cap speed against the thinnest collider.** `speed / 60` is the distance travelled per step; keep it below the thinnest wall, or sweep.
5. **Filter contacts by category/mask.** `new PhysicsShape.ContactFilter(category, mask, group)` decides who can hit whom; keep named constants for factions and layers.
6. **Tune the body, not the world.** `PhysicsBodyDefinition.gravityScale`, `linearDamping`, `angularDamping`, density and surface material are per body/shape.
7. **Verify by stress, not by eye.** Low/high frame rates, fastest projectile against the thinnest hull, stacked bodies, and a teleported body.

## Patterns

### 1. Fixed step and a scripted world for tests

```csharp
// Runtime: one step per FixedUpdate, fixed dt.
world.Simulate(Time.fixedDeltaTime);

// Tests: deterministic stepping, no automatic simulation.
var definition = PhysicsWorldDefinition.defaultDefinition;
definition.simulationType = PhysicsWorld.SimulationType.Script;
var world = PhysicsWorld.Create(definition);
while (elapsed < 3f) { world.Simulate(1f / 60f); elapsed += 1f / 60f; }
```

### 2. Sweep instead of discrete contacts

```csharp
Ballistics.SweepSegment(in projectile, deltaTime, out float2 from, out float2 to);
var input = PhysicsQuery.CastRayInput.FromTo(new Vector2(from.x, from.y), new Vector2(to.x, to.y));
PhysicsQuery.CastResult hit = circle.CastRay(input);   // nearest hit along the segment
if (!hit.isValid) continue;
```

A 4000 px/s shot moves ~66.7 px per 60 Hz step; a 40 px hull is smaller than that. Sweeping is what stops tunneling, not a smaller timestep.

### 3. Speed cap

```csharp
const float MaxSpeed = 40000f;                 // engine ceiling from the settings asset
float maxTravelPerStep = MaxSpeed / 60f;        // ~667 px
// Keep projectile speed below MaxSpeed; raise the physics rate only as a last resort.
```

### 4. Contact filters and factions

```csharp
shapeDefinition.contactFilter = new PhysicsShape.ContactFilter(
    category: FactionCategory(factionId),      // 0..30
    mask: PhysicsMask.All,
    group: 0);                                  // same non-zero group = no collision
```

Both sides of a pair must agree. A projectile that ignores its own faction filters by `factionId` before the cast; keep that as an explicit rule.

### 5. Body and shape tuning

```csharp
var bodyDefinition = PhysicsBodyDefinition.defaultDefinition;
bodyDefinition.type = PhysicsBody.BodyType.Dynamic;
bodyDefinition.gravityScale = 0f;               // top-down ships
bodyDefinition.linearDamping = 0.5f;
bodyDefinition.awake = true;

var shapeDefinition = PhysicsShapeDefinition.defaultDefinition;
shapeDefinition.density = 1f;
shapeDefinition.surfaceMaterial = new PhysicsShape.SurfaceMaterial { friction = 0.6f };
body.CreateShape(new CircleGeometry { radius = radius }, shapeDefinition);
```

Mass is relative in collisions; it does not change fall speed (gravity is acceleration). Use `gravityScale` and damping to shape feel.

### 6. Sleeping and teleports

Resting bodies sleep at `sleepThreshold = 5`; do not keep them awake for nothing. When teleporting a body, write the new pose and let the next step run; do not interpolate across the teleport in presentation, or the renderer smears it across the screen.

## Pitfalls

- **Applying movement in `Update`** makes speed frame-rate dependent and collisions flaky.
- **Changing `lengthUnitsPerMeter` after bodies exist** rescales every threshold and impulse. Treat it as a migration.
- **Discrete contacts for bullets.** A fast shot crosses a thin hull between steps; sweep it.
- **Per-frame managed allocation** in the step (`new`, LINQ, boxing) violates `CODING_STANDARDS.md` section 2 and causes GC hitches.
- **Writing physics objects from worker jobs.** `PhysicsBody`/`PhysicsShape` writes are main-thread; parallelize your own `NativeArray` data, not the physics world.
- **Too many solver iterations or extreme mass ratios** explode stacks. Keep ratios modest.
- **Ignoring the maximum step.** A stall must not produce one giant `deltaTime`; clamp the step (Unity *Maximum Allowed Timestep*).
- **Expecting `Rigidbody2D` behaviour.** `interpolation`, `collisionDetectionMode` and `Physics2D.IgnoreLayerCollision` do not apply here.

## Verification checklist

- [ ] One `world.Simulate(Time.fixedDeltaTime)` per `FixedUpdate`.
- [ ] Fastest projectile swept; thinnest hull not tunnelled.
- [ ] `PhysicsWorld.lengthUnitsPerMeter == 100` in a PlayMode test.
- [ ] No managed allocation in the step.
- [ ] Contact filters tested for both directions.
- [ ] Behaviour unchanged at 30, 60 and 120 rendered FPS.

## References

- `references/timestep-and-ccd.md` — engine-neutral accumulator, interpolation and CCD math. Read the Unity-specific API in this file first; the reference keeps Godot/legacy wording.
- `ProjectileContactSolver.cs` — swept projectile contact layer.
- `WorldUnitPhysicsTests.cs` — world-scale and fixed-step assertions.
- `CODING_STANDARDS.md` section 2 — zero-alloc and Burst rules.
