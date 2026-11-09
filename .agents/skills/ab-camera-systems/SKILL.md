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
6. **Update after the simulation.** `RenderMissionFrame` calls `Focus` once per
   rendered frame, after the 60 Hz mission step; the rig then applies the
   composed transform in `LateUpdate`. Never read the player pose mid-step.
7. **Preserve the input mapping.** `ScreenToWorld` and `ScreenToViewport`
   already account for the preset and real aspect; `ScreenToWorld` must read
   `_focus`, so follow is included but the shake offset never is.
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

`GameFlowController.RenderMissionFrame` is the only per-frame caller: after the
mission step it calls `_cameraRig?.Focus(player.position)` and then
`_spriteView.Render(deltaTime)`. Follow work belongs in the rig that call
reaches, not in a second component.

### 2. Follow, deadzone, look-ahead and bounds live in the rig

Keep the existing `Focus(Vector2)` entry point and make it the follow step:
smooth toward the requested point with exponential smoothing
(`1 - exp(-rate * dt)`), move `_focus` only by the overflow past the deadzone,
ease the look-ahead offset toward the direction of travel, then clamp the
visible rectangle with `WorldViewportWidth` / `WorldViewportHeight`.

Do not add a parallel follow component with its own `_focus` or `CameraTarget`:
two owners of the camera position fight each other and break `ScreenToWorld`.
The worked implementation, tuning ranges and the clamp helper are in
`references/follow-and-framing.md` §1 and §2.

### 3. Shake is composed inside the rig

`ab-game-feel` only calls `SetShakeOffset`; the rig keeps `_focus` unshaken and
applies `_focus + _shakeOffset` in `LateUpdate`. Never pass a shaken position to
`Focus`, and never let the shake offset enter `ScreenToWorld`, or the turret
reticle jitters with the effect.

`SetShakeOffset(Vector2)` is **a hook to add** to `CombatCameraRig`, not an
existing API. The full component (fields, `Focus`, `LateUpdate`, `ScreenToWorld`
reading `_focus`, `SetShakeOffset`) is in
`references/follow-and-framing.md` §4.

## Pitfalls

- **`Lerp(pos, target, const)` per frame** is frame-rate dependent -- floatier at
  30 FPS, snappier at 144. Use `1 - exp(-rate * dt)` or `SmoothDamp`.
- **Following before the target has moved** gives a one-frame lag jitter. Focus
  from `RenderMissionFrame`, after the mission step, and render in `LateUpdate`.
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
- **Shake driving the follow target** makes follow fight shake. The rig keeps
  `_focus` unshaken and composes `_focus + _shakeOffset` for rendering only.
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
