# Follow and framing — depth for `ab-camera-systems`

Detail the `ab-camera-systems` body defers here: the smoothing math, the follow
step inside the rig, shake composition, framing and transitions. All snippets
target this project's Unity 6.6 URP 2D orthographic camera. One world unit is
one frozen stage pixel (ADR 0017); the Short view is 1920x1080 design px, the
640x360 stage at 3x (ADR 0021).

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

## 2. The follow step inside `CombatCameraRig`

`GameFlowController.RenderMissionFrame` calls `_cameraRig?.Focus(playerPosition)`
once per rendered frame, after the 60 Hz mission step, then submits the sprite
frame. The rig owns the whole follow — deadzone, look-ahead, smoothing and
bounds — and applies shake in `LateUpdate`. One owner; do not add a second
component with its own `_focus` or `CameraTarget`.

Order inside `Focus`:

1. Compute the target's overflow beyond the **deadzone** and move the follow
   centre by the overflow only.
2. Add **look-ahead**: `dir_of_motion * lookAheadDistance`, easing the
   look-ahead offset toward its goal so a direction flip does not snap the view.
3. **Smooth** the centre toward that goal (section 1).
4. **Clamp** the visible rectangle: clamp the camera center to
   `[min + halfView, max - halfView]` per axis. Clamp the view, not the center.

```csharp
// Inside CombatCameraRig. `_focus` is the follow centre; shake is separate.
Vector2 _focus;
Vector2 _lead;
Vector2 _playerVelocity;          // from the simulation, never a frame delta
Rect _level;
static readonly Vector2 Deadzone = new Vector2(48f, 32f);
const float LookAheadDistance = 64f;
const float Rate = 8f;

public void Focus(Vector2 target)
{
    float dt = Time.deltaTime;

    // 1. Deadzone: move the centre only by the overflow past the box.
    Vector2 to = target - _focus;
    Vector2 goal = _focus + new Vector2(
        Mathf.Max(Mathf.Abs(to.x) - Deadzone.x, 0f) * Mathf.Sign(to.x),
        Mathf.Max(Mathf.Abs(to.y) - Deadzone.y, 0f) * Mathf.Sign(to.y));

    // 2. Ease the look-ahead so a direction flip does not whip the view.
    Vector2 leadGoal = _playerVelocity.sqrMagnitude > 0.0001f
        ? _playerVelocity.normalized * LookAheadDistance
        : Vector2.zero;
    _lead = Vector2.Lerp(_lead, leadGoal, 1f - Mathf.Exp(-6f * dt));
    goal += _lead;

    // 3. Frame-rate-independent smoothing, then clamp the visible rect.
    _focus = Vector2.Lerp(_focus, goal, 1f - Mathf.Exp(-Rate * dt));
    _focus = ClampToLevel(_focus, _level);
}

// Clamp against the rig's real aspect, not the preset buffer size.
Vector2 ClampToLevel(Vector2 center, Rect level)
{
    float viewW = WorldViewportWidth;
    float viewH = WorldViewportHeight;
    float halfW = viewW * 0.5f, halfH = viewH * 0.5f;

    float x = level.width  <= viewW ? level.center.x
                                    : Mathf.Clamp(center.x, level.xMin + halfW, level.xMax - halfW);
    float y = level.height <= viewH ? level.center.y
                                    : Mathf.Clamp(center.y, level.yMin + halfH, level.yMax - halfH);
    return new Vector2(x, y);
}
```

The lead is folded into `_focus` because `ScreenToWorld` must map the view that
was actually rendered. If the level is smaller than the view on an axis, center
on that axis instead of clamping. Tunables in world units at Short: deadzone
`32–64`, look-ahead `40–120` eased over `0.2–0.4 s`, smoothing rate `6–10`.
Scale by `CameraDistancePresets.ViewScale(preset)` when the preset changes so the
feel stays proportional at Medium and Far.

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

## 4. Shake composition inside the rig

`ab-game-feel` only writes the offset; the rig composes it:

```csharp
// Add to CombatCameraRig. `_focus` is unshaken; the rendered transform is not.
Vector2 _shakeOffset;

public void SetShakeOffset(Vector2 offset) => _shakeOffset = offset; // hook to add

void LateUpdate()
{
    if (_camera == null) return;

    Transform t = _camera.transform;
    t.position = new Vector3(_focus.x + _shakeOffset.x,
                             _focus.y + _shakeOffset.y,
                             t.position.z);
}

// ScreenToWorld reads _focus, never the composed transform.
public Vector2 ScreenToWorld(Vector2 screen, Vector2 screenSize)
{
    if (screenSize.x <= 0f || screenSize.y <= 0f) return Vector2.zero;
    if (_camera == null) return Vector2.zero; // keep the preset fallback in the real method

    float height = _camera.orthographicSize * 2f;
    float width = height * _camera.aspect;
    return new Vector2(
        _focus.x + ((screen.x / screenSize.x) - 0.5f) * width,
        _focus.y + ((screen.y / screenSize.y) - 0.5f) * height);
}
```

Requirements:

- `SetShakeOffset` does not exist in `CombatCameraRig` today: add it with the
  rest of this composition, then test `ScreenToWorld` at a non-zero offset.
- Shake must not accumulate into `_focus`; it is a per-frame visual offset.
- `ScreenToWorld` must use `_focus`. If the offset is large enough to move the
  reticle, the fix is keeping the mapping unshaken, not accepting drift.
- Shake amplitude is in world units; keep it below the deadzone size so it never
  reads as camera follow.

## 5. Transitions and hard cuts

- **Mission start / respawn:** hard-cut to the player, reset `_focus` to the
  player position and zero the look-ahead and shake. Never let a large smoothing
  distance whip across the level.
- **Preset change:** `ApplyPreset` changes `orthographicSize` immediately; the
  HUD badge announces the new distance. Do not animate the orthographic size
  unless the owner approves it and the reticle mapping is re-verified.
- **Cinematic moments:** if a scripted shot needs a different framing, add a
  temporary focus override to the existing rig and restore the combat follow
  afterwards. Do not instantiate a second camera.

## 6. Project rig mapping

| Need | This project |
|------|--------------|
| 2D follow | `CombatCameraRig.Focus(Vector2)` smooths and stores `_focus` |
| View scale | `CameraDistancePresets.ViewScale(preset)`; Short 1.0x, Medium 1.3x, Far 1.5x |
| Visible size | `WorldViewportWidth` / `WorldViewportHeight` at the real aspect |
| Bounds | manual clamp against the level rect (section 2) |
| Aim mapping | `ScreenToWorld` / `ScreenToViewport`; `ScreenToWorld` reads `_focus` |
| Shake | `_shakeOffset` composed in `LateUpdate`; trigger owned by `ab-game-feel` |

There is no virtual-camera stack, no camera blending package and no 3D depth or
collision push-in in this project. Extend `CombatCameraRig` and test the mapping
at every preset and at 16:9/16:10.
