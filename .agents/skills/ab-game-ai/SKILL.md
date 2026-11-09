---
name: ab-game-ai
description: >
  Design enemy and ally decision-making with finite state machines, target
  selection and steering behaviors that run as Burst jobs over NativeArray in
  this Unity 6.6 / PhysicsCore2D project. Use when building or tuning tactical
  AI modes, patrol/chase/guard states, seek/arrive steering, target selection,
  or when the user mentions state machine, steering, seek, arrive, throttle or
  tactical AI.
---

> **Project constraint (A&B Revival).** Original Flash art and fonts are the baseline (ADR 0020). Layout, UX and art improvements need explicit owner approval recorded in the issue/PR. This project simulates AI with `TacticalAIJob`, a Burst-compiled `IJobParallelFor` over `NativeArray` (ADR 0006), not with ECS entities. The repository workflow (wayfinder -> to-spec -> to-tickets -> TDD -> review) prevails over any step in this skill.

# Game AI: decisions, target selection and steering

Build believable NPC behavior from two separable layers: **decide** (which
tactical mode and which target) and **steer** (what throttle and turn the ship
should apply). Keep them decoupled: the decision layer writes a `ShipInput`;
the movement job consumes it. This project already owns that shape in
`TacticalAIJob` -- extend it instead of adding a parallel AI system.

## When to use

- Use when adding or tuning a tactical mode (`Anchor`, `Escort`, `Evade`,
  `SeekAndDestroy`, `FireSupport`, `Shield`), a patrol/chase/guard loop, or a
  formation behavior.
- Use to choose between a small FSM (few clear modes) and utility scoring
  (many competing priorities).
- Use when agents oscillate between modes, pick targets inconsistently, or
  steer into corners.

**When *not* to use:** for movement integration, collision response and mass,
use `ab-physics-tuning`; for per-frame scheduling and allocation budgets, use
`ab-performance-optimization` and `CODING_STANDARDS.md` section 2. For the
projectile or hit feedback that follows an AI decision, use `ab-game-feel`.

## Core workflow

1. **Pick the decision model by complexity.** Two to five states with obvious
   transitions -> FSM. Many competing priorities -> utility scoring that sums
   weighted terms. If the mode set is fixed, an enum switch is the clearest FSM.
2. **Keep the decision pure and allocation-free.** `TacticalAIJob` reads
   `NativeArray<ShipEntity>` and writes `NativeArray<ShipInput>`; no managed
   list, lambda, LINQ or string is allowed inside the job.
3. **Select the target once per evaluation.** Nearest living hostile with
   squared distance, deterministic tie-break by ship index. The player craft is
   skipped so its input stays under player control.
4. **Steer by throttle and turn, not by force.** Write a `ShipInput`
   (`throttle` 0..1, `steering` -1..1, `boost`); the movement job turns throttle
   into acceleration and steering into rotation at the fixed 60 Hz step (ADR
   0010). Do not integrate a velocity or a force inside the AI job.
5. **Clamp every output.** Throttle is `0..1`, steering `-1..1`; both feed the
   ship's turn rate and thrust directly, so an unclamped value makes it twitch.
6. **Verify by observation.** Watch the ship, not only the numbers: does it
   reach the holder/escort slot, does it overshoot, does it flip between two
   modes at a boundary? Add hysteresis rather than shrinking the threshold.

## Patterns

### 1. Mode FSM as an enum switch (one mode, explicit transitions)

```csharp
// Matches TacticalAIJob.EvaluateTacticalSteering (ADR 0006).
input = default;
switch (mode)
{
    case CaptainTacticalMode.Anchor:
        targetPos = ship.spawnPosition;          // hold the mission spawn point
        input.throttle = ThrottleTo(ship.position, targetPos);
        steerToTarget = true;
        break;
    case CaptainTacticalMode.Escort:
        targetPos = player.position - ShipEntity.Forward(player.rotation) * EscortTrailDistance
                    + SlotOffset(ShipEntity.Forward(player.rotation), ship.formationSlot);
        input.throttle = ThrottleTo(ship.position, targetPos);
        steerToTarget = true;
        break;
    // Evade, SeekAndDestroy, FireSupport, Shield follow the same shape.
}

// Shared tail of every steering mode: point the nose at the target.
if (steerToTarget)
{
    float2 toTarget = math.normalize(targetPos - ship.position);
    float2 forward = ShipEntity.Forward(ship.rotation);
    float2 right = new float2(forward.y, -forward.x);
    input.steering = math.clamp(-math.dot(right, toTarget), -1f, 1f);
}
```

Keep transition conditions next to the mode that owns them. A growing pile of
`if (mode == ...)` checks outside the switch recreates the spaghetti the enum
exists to prevent. `ThrottleTo` lives as a private static helper in the same
struct so the job stays Burst-friendly.

### 2. Target selection: nearest, squared, deterministic

```csharp
// NativeArray-friendly: no LINQ, no allocation, no sqrt in the inner loop.
public static bool TryFindNearestHostile(
    in NativeArray<ShipEntity> ships, int shipCount,
    float2 selfPosition, int selfFaction, out ShipEntity target)
{
    target = default;
    float bestSq = float.MaxValue;
    bool found = false;

    for (int i = 0; i < shipCount; i++)
    {
        ShipEntity other = ships[i];
        if (other.factionId == selfFaction || other.health <= 0f) continue;

        float2 delta = other.position - selfPosition;
        float sq = math.lengthsq(delta);
        if (sq < bestSq)                  // strict `<` keeps the lowest index on ties
        {
            bestSq = sq;
            target = other;
            found = true;
        }
    }
    return found;
}
```

Tie-breaking on index matters: two identical distances otherwise resolve by
iteration order accidentally, and replays/tests drift. The real job also skips
the player index so the player craft stays under player control.

### 3. Seek and arrive as `ShipInput` (throttle and turn, not force)

```csharp
// Seek: full throttle at the target; Arrive: throttle ramps down inside
// SlowRadius. Output is the same ShipInput the movement job consumes.
static void SeekArrive(in ShipEntity ship, float2 target, out ShipInput input)
{
    float2 toTarget = target - ship.position;
    float dist = math.length(toTarget);

    input = default;
    input.throttle = math.clamp(dist / SlowRadius, 0f, 1f); // ramps down on arrival
    if (dist <= 0.001f) return;

    float2 forward = ShipEntity.Forward(ship.rotation);
    float2 right = new float2(forward.y, -forward.x);
    input.steering = math.clamp(-math.dot(right, toTarget / dist), -1f, 1f);
}
```

The movement job (`ShipMovementJob.Integrate`) turns those three fields into
motion at the fixed 60 Hz step: `appliedThrust = throttle * thrustPower` (boost
multiplies it), `velocity += forward * appliedThrust / mass * dt`, then
`velocity *= clamp(1 - drag * dt, 0, 1)`, then `position += velocity * dt`.
Steering only rotates the hull; there is no steering force to integrate. Never
apply the input directly to the transform.

### 4. Utility scoring when modes compete

```csharp
// Score each option, take the best; weights live in one place for tuning.
float anchor  = 1.0f;
float escort  = playerAlive ? 1.2f : 0f;
float evade   = threatDistance < EvadeSafeRadius ? 2.0f : 0f;
float attack  = hasHostile ? 1.5f : 0f;
// Best = max(anchor + fatigue, ...); add hysteresis so the mode does not flicker.
```

Use utility scoring only when the mode count is genuinely open-ended. For the
six ADR 0006 modes, the enum switch is easier to test and review.

## Pitfalls

- **Allocating inside the job.** `new List<int>`, LINQ, `string` formatting and
  closures break Burst and the zero-alloc rule. Use `NativeArray` and indexes.
- **Calling `Mathf` or managed math in a Burst job.** Use `Unity.Mathematics`
  (`math.length`, `math.normalizesafe`, `float2`) so the job vectorizes.
- **Steering to the goal instead of the mode's target** makes escorts collide
  with the player and interceptors overshoot their slot.
- **Using `sqrt` to compare distances.** Compare squared distances; take the
  root only for a radius check that needs real units.
- **Mode flicker at a threshold.** Add hysteresis or a minimum dwell time
  instead of tuning the same threshold forever.
- **Unbounded turn or throttle.** Clamp `steering` to `-1..1` and `throttle` to
  `0..1` in the job; the movement job multiplies them by turn speed and thrust.
- **State kept in a MonoBehaviour.** The decision state that affects gameplay
  belongs in the simulation data (`ShipEntity`), not in presentation fields that
  tests cannot see. Presentation-only state (HUD badge fade) stays in
  presentation.

## References

- `docs/adr/0006-captain-tactical-ai-modes.md` -- the six mode contracts.
- `docs/adr/0010-fixed-timestep-simulation-and-runtime-performance-gate.md` --
  the 60 Hz step the steering feeds.
- `CODING_STANDARDS.md` section 2 -- allocation and Burst rules.

## Related skills

- `ab-physics-tuning` -- movement integration, drag and collision response.
- `ab-performance-optimization` -- job scheduling, allocations and frame budget.
- `ab-game-feel` -- impact feedback after an AI shot lands.
- `ab-camera-systems` -- keeping the AI action readable on screen.
