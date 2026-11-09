# Follow and framing — depth for `ab-camera-systems`

Detail the `ab-camera-systems` body defers here: the smoothing math, the full 2D
follow rig, framing and transitions, and the mapping onto
`CombatCameraRig`. All snippets target this project's Unity 6.6 URP 2D
orthographic camera and 640x480 design space.

## 1. Why `1 - exp(-rate * dt)` (frame-rate-independent smoothing)

A naive `pos = Lerp(pos, target, k)` applies `k` *per frame*, so at 144 FPS it
smooths far more per second than at 30 FPS — the feel changes with hardware.
Exponential smoothing fixes the rate *per second*:

```text
t = 1 - exp(-rate * dt)      # rate ~ 5 (floaty) .. 12 (snappy)
pos = lerp(pos, target, t)   # identical convergence at any frame rate
```

`Vector2.SmoothDamp` is a critically damped spring that gives the same frame-rate
independence plus velocity continuity (no overshoot). Prefer it when the camera
must come to rest without wobbling; use exponential smoothing when you want a
simple, always-easing follow.

## 2. Full 2D rig: deadzone + look-ahead + bounds

Order of operations each frame, after the simulation completes:

1. Compute target overflow beyond the **deadzone** (box or circle) and move the
   focus by the overflow only.
2. Add **look-ahead**: `focus += dir_of_motion * lookAheadDistance`, easing the
   look-ahead offset itself toward its goal so a direction flip does not snap
   the view.
3. **Smooth** the camera toward the focus (section 1).
4. **Clamp** the visible rectangle: clamp the camera center to
   `[min + halfView, max - halfView]` per axis. Clamp the view, not the center.
5. **Apply shake** as a final additive offset (section 4).

```csharp
// Clamp against the rig's real aspect, not the preset buffer size.
Vector2 ClampToLevel(Vector2 center, Rect level, CombatCameraRig rig)
{
    float viewW = rig.WorldViewportWidth;
    float viewH = rig.WorldViewportHeight;
    float halfW = viewW * 0.5f, halfH = viewH * 0.5f;

    float x = level.width  <= viewW ? level.center.x
                                    : Mathf.Clamp(center.x, level.xMin + halfW, level.xMax - halfW);
    float y = level.height <= viewH ? level.center.y
                                    : Mathf.Clamp(center.y, level.yMin + halfH, level.yMax - halfH);
    return new Vector2(x, y);
}
```

If the level is smaller than the view on an axis, center on that axis instead of
clamping. Tunables in design-space pixels at Short: deadzone `32–64`,
look-ahead `40–120` eased over `0.2–0.4 s`, smoothing rate `6–10`. Scale by
`CameraDistancePresets.ViewScale(preset)` when the preset changes so the feel
stays proportional at Medium and Far.

## 3. Framing the battle

- The frozen baseline centers the view on the player craft; projectiles,
  enemies and allied ships are read relative to that center. Keep the player at
  the center of the framing unless the owner approves a different composition.
- **Group framing** (for a scripted cinematic or a multi-ship moment): compute
  the bounding box of the targets, center on it, and choose the largest preset
  that fits the box with padding. Do not add a per-frame auto-zoom; preset
  changes are discrete and HUD-badged (ADR 0005).
- **Aspect ratio:** the HUD stays at native design resolution while the world
  view uses the camera's real aspect. Use `WorldViewportWidth` /
  `WorldViewportHeight`, never the preset buffer dimensions, for bounds and
  framing.
- **Depth:** the world is layered with sprite sorting, not 3D distance. Keep the
  camera z fixed; `Focus` already preserves depth.

## 4. Shake composition

Expose one additive offset that `ab-game-feel` writes:

```csharp
// Follow, clamp, then add shake. Keep the un-shaken focus for input mapping.
Vector2 focus = ClampToLevel(Smooth(_focus, target, rate, dt), level, rig);
rig.Focus(focus + _shakeOffset);
```

Requirements:

- Shake must not accumulate into `_focus`; it is a per-frame visual offset.
- `ScreenToWorld` must use the un-shaken focus. If the offset is large enough to
  move the reticle, add a dedicated mapping path in `CombatCameraRig` rather
  than accepting drift.
- Shake amplitude is in design-space pixels; keep it below the deadzone size so
  it never reads as camera follow.

## 5. Transitions and hard cuts

- **Mission start / respawn:** hard-cut to the player and reset the smoothing
  velocity and look-ahead. Never let a large smoothing distance whip across the
  level.
- **Preset change:** `ApplyPreset` changes `orthographicSize` immediately; the
  HUD badge announces the new distance. Do not animate the orthographic size
  unless the owner approves it and the reticle mapping is re-verified.
- **Cinematic moments:** if a scripted shot needs a different framing, add a
  temporary focus override to the existing rig and restore the combat follow
  afterwards. Do not instantiate a second camera.

## 6. Project rig mapping

| Need | This project |
|------|--------------|
| 2D follow | `CombatCameraRig.Focus(Vector2)` centers the orthographic camera |
| View scale | `CameraDistancePresets.ViewScale(preset)`; Short 1.0x, Medium 1.3x, Far 1.5x |
| Visible size | `WorldViewportWidth` / `WorldViewportHeight` at the real aspect |
| Bounds | manual clamp against the level rect (section 2) |
| Aim mapping | `ScreenToWorld` / `ScreenToViewport`; must ignore shake |
| Shake | additive offset applied after focus; trigger owned by `ab-game-feel` |

There is no virtual-camera stack, no camera blending package and no 3D depth or
collision push-in in this project. Extend `CombatCameraRig` and test the mapping
at every preset and at 16:9/16:10.
