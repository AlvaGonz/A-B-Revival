---
name: ab-camera-systems
description: >
  Build and tune the combat camera in this project's URP 2D orthographic rig —
  smooth follow, deadzone, look-ahead, level-bounds clamping and framing —
  through the existing CombatCameraRig and CameraDistancePresets. Use when the
  user mentions camera follow, deadzone, look-ahead, smoothing, camera bounds,
  camera jitter, combat framing or the camera distance presets.
---

> **Project constraint (A&B Revival).** Original Flash art and fonts are the baseline (ADR 0020). Layout, UX and art improvements need explicit owner approval recorded in the issue/PR. This project renders through the URP 2D renderer with a single orthographic combat camera driven by `CombatCameraRig` and the ADR 0005 distance presets. The repository workflow (wayfinder -> to-spec -> to-tickets -> TDD -> review) prevails over any step in this skill.

# Camera systems

The camera is the player's window; bad camera work makes a good game feel
awful. This project already has a 2D orthographic rig: `CombatCameraRig` owns
the camera, applies the Short/Medium/Far distance presets (ADR 0005), centers
the view on the player craft and exposes the screen-to-world mapping the HUD and
turret aiming depend on. This skill covers follow smoothing, deadzone,
look-ahead, bounds clamping and framing **inside that rig**. Extend it; do not
build a second camera system.

## When to use

- Use when the camera should follow the player more smoothly, ignore small
  movements (deadzone), lead the craft's motion, or stop at the level edge.
- Use when the view jitters, snaps on respawn, or shows past the playable area.
- Use when framing a wider battlefield through the existing distance presets.

**When *not* to use:** for shake magnitude and impact triggers, use
`ab-game-feel` (this skill exposes the offset hook it drives). For movement
itself, use `ab-game-ai` or the player input path. For render cost, use
`ab-performance-optimization`.

## Core workflow

1. **Decide what the camera serves.** This is a top-down/2.5D space battle: the
   frozen baseline follows the player craft 1:1 and the distance preset controls
   how much of the sector is visible. Do not change that contract without owner
   approval.
2. **Follow smoothly and frame-rate independently.** Move toward the target with
   exponential smoothing (`1 - exp(-rate * dt)`) or `Vector2.SmoothDamp`, not a
   fixed `Lerp(a, b, 0.1)` -- the 0.1 is per-frame and changes with frame rate.
3. **Add a deadzone** so micro-movements do not nudge the view; follow only once
   the target leaves a box or circle.
4. **Lead with look-ahead** by offsetting the follow target in the direction of
   travel, eased in and out so a direction flip does not whip the view.
5. **Clamp the visible rectangle, not the center.** Use the rig's
   `WorldViewportWidth` / `WorldViewportHeight` so the camera never shows outside
   the level at any aspect ratio.
6. **Update after the simulation.** Read the player position after the 60 Hz
   jobs complete and apply the camera in `LateUpdate` (or an equivalent
   post-simulation step) to avoid one-frame lag.
7. **Preserve the input mapping.** `ScreenToWorld` and `ScreenToViewport`
   already account for the preset and real aspect; any follow offset must be
   removed from that mapping so aiming does not drift.
8. **Verify at every preset and aspect.** Move the craft to corners and level
   edges at Short, Medium and Far, at 16:9 and 16:10; confirm no jitter, no
   peeking past bounds and a smooth stop.

## Patterns

### 1. Use the existing rig (do not replace it)

```csharp
// CombatCameraRig already does this. Call it; do not duplicate the maths.
rig.ApplyPreset(CameraDistancePreset.Medium);   // ADR 0005: 1.3x view
rig.Focus(playerPosition);                      // orthographic centre, depth kept
float width  = rig.WorldViewportWidth;          // real aspect, matches ScreenToWorld
float height = rig.WorldViewportHeight;
```

`ApplyPreset` drives `camera.orthographic = true` and
`orthographicSize = DesignHeight * 0.5 * viewScale`. Directly setting
`orthographicSize`, `camera.rect` or a second camera breaks the HUD reticle and
turret mapping; if a new behavior needs those, add it to the rig behind owner
approval.

### 2. Frame-rate-independent smooth follow

```csharp
// RIGHT: exponential smoothing; same feel at 30, 60 or 144 FPS. rate ~ 5..12.
Vector2 Smooth(Vector2 current, Vector2 target, float rate, float dt)
{
    float t = 1f - Mathf.Exp(-rate * dt);
    return Vector2.Lerp(current, target, t);
}
// WRONG: Vector2.Lerp(current, target, 0.1f) every frame.
// Unity alternative: Vector2.SmoothDamp(current, target, ref velocity, smoothTime, maxSpeed, dt).
```

Follow the focus point in `LateUpdate`, after `ScheduledCombatSimulation`
completes and the player transform has moved.

### 3. Deadzone + look-ahead (lead the craft, ignore jitter)

```csharp
// Move the focus only by the overflow beyond the deadzone, then aim ahead.
Vector2 _focus;
Vector2 CameraTarget(Vector2 playerPos, Vector2 playerVelocity, float dt)
{
    Vector2 to = playerPos - _focus;
    Vector2 dz = new Vector2(48f, 32f);             // design-space pixels
    _focus.x += Mathf.Max(Mathf.Abs(to.x) - dz.x, 0f) * Mathf.Sign(to.x);
    _focus.y += Mathf.Max(Mathf.Abs(to.y) - dz.y, 0f) * Mathf.Sign(to.y);

    Vector2 leadGoal = playerVelocity.normalized * 64f;   // 40..120 px at 640x480
    _lead = Vector2.Lerp(_lead, leadGoal, 1f - Mathf.Exp(-6f * dt)); // ease the lead
    return _focus + _lead;
}
```

Tune in design-space pixels: the base view is 640x480 and the world art runs at
3x density (ADR 0021). A deadzone that feels right at Short is too tight at Far;
scale the values by `CameraDistancePresets.ViewScale(preset)` if you expose them.

### 4. Clamp the visible rectangle inside the level

```csharp
Vector2 ClampToLevel(Vector2 center, Rect level, float viewWidth, float viewHeight)
{
    float halfW = viewWidth * 0.5f, halfH = viewHeight * 0.5f;
    float x = level.width  <= viewWidth  ? level.center.x : Mathf.Clamp(center.x, level.xMin + halfW, level.xMax - halfW);
    float y = level.height <= viewHeight ? level.center.y : Mathf.Clamp(center.y, level.yMin + halfH, level.yMax - halfH);
    return new Vector2(x, y);
}
```

If the level is smaller than the view on an axis, center on that axis instead of
clamping; otherwise the camera fights itself at the edges.

### 5. Additive shake hook (owned trigger lives in `ab-game-feel`)

```csharp
// Smooth follow first, shake offset last. The offset is visual only and must
// not feed ScreenToWorld, or the turret reticle will jitter with the shake.
Vector2 finalFocus = ClampToLevel(CameraTarget(playerPos, playerVel, dt), level, w, h);
rig.Focus(finalFocus + _shakeOffset);
```

If the rig needs a dedicated `SetShakeOffset` to keep the mapping clean, add it
to `CombatCameraRig` and test `ScreenToWorld` at a non-zero offset.

## Pitfalls

- **`Lerp(pos, target, const)` per frame** is frame-rate dependent -- floatier at
  30 FPS, snappier at 144. Use `1 - exp(-rate * dt)` or `SmoothDamp`.
- **Following before the target has moved** gives a one-frame lag jitter. Follow
  in `LateUpdate`, after the simulation jobs complete.
- **No bounds clamp** shows background past the level edge. Clamp the view rect.
- **No deadzone in a twitchy dogfight** makes the camera shake with every
  correction.
- **Overwriting `orthographicSize` or `camera.rect` outside `ApplyPreset`**
  desyncs the HUD and the ADR 0005 distance badge.
- **Scaling the HUD with the camera.** HUD stays at native design resolution;
  only the world view scales.
- **Snapping on respawn or mission start** is jarring. Hard-cut intentionally and
  reset the smoothing state; never let a large smoothing distance whip across
  the level.
- **Shake driving the follow target** makes follow fight shake. Compose: follow,
  clamp, then add the offset.
- **Assuming 3D depth or collision push-in.** This is a flat orthographic 2D
  view; level bounds and layering are the only spatial constraints.

## References

- `references/follow-and-framing.md` -- smoothing derivation, the full 2D rig,
  deadzone/look-ahead tuning and framing for this project.
- `docs/adr/0005-configurable-combat-camera-distance.md` -- distance presets,
  view scaling and the HUD/aim mapping contract.
- `A&B-U/Assets/Scripts/Presentation/CombatCameraRig.cs` -- the rig to extend.

## Related skills

- `ab-game-feel` -- owns shake trauma/triggers; this skill exposes the offset.
- `ab-game-ai` -- the player and AI motion being framed.
- `ab-performance-optimization` -- camera and render-target cost.
- `ab-game-ui-ux` -- HUD anchoring and the unscaled UI layer.
