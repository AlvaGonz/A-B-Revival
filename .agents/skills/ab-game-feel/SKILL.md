---
name: ab-game-feel
description: >
  Add juice and game feel that makes combat actions satisfying — hit-stop,
  screen shake, recoil, flashes, knockback, eased motion and layered feedback —
  without new art, new colors or effects that obscure gameplay. Targets this
  project's Unity 6.6 presentation layer and the original Flash palette. Use
  when the user mentions game feel, juice, punchy, screen shake, hit stop,
  recoil, impact frames, damage numbers or feedback polish.
---

> **Project constraint (A&B Revival).** Original Flash art and fonts are the baseline (ADR 0020). Layout, UX and art improvements need explicit owner approval recorded in the issue/PR, and feedback uses the original palette only: faction hues from ADR 0022, no invented colors. No effect may obscure gameplay or HUD information. The repository workflow (wayfinder -> to-spec -> to-tickets -> TDD -> review) prevails over any step in this skill.

# Game feel (juice)

The difference between a mechanic that *works* and one that feels *good* is
feedback: the layered, slightly exaggerated response an action provokes. This
skill covers hit-stop, screen shake, recoil, flashes, knockback and stacked
feedback, and how to add them without burying the simulation or changing the
original art direction. It **adds polish on top of** an existing mechanic; it
does not implement the mechanic.

## When to use

- Use when a shot, hit, explosion, pickup, death or button press is mechanically
  correct but feels weak, weightless or unsatisfying.
- Use to add hit-stop, camera shake, recoil, flashes, eased motion or to layer
  several feedback channels onto one event.
- Use to decide how much juice is enough and where it crosses into noise.

**When *not* to use:** for camera follow, deadzone or framing, use
`ab-camera-systems` (this skill only feeds it shake). For damage telemetry and
HUD alerts, use the ADR 0004 patterns. For movement and knockback stability, use
`ab-physics-tuning`. For new sprite, font or palette work, stop: that needs owner
approval under ADR 0020.

## Core principle: feedback is layered, brief and in palette

One satisfying hit is usually several tiny responses firing together within
~100 ms: a sound, a particle burst, a brief hit-stop, a flash, a recoil, a small
shake, a damage number. Each is cheap; stacked, they read as impact. Three rules
keep it coherent:

1. **Exaggerate briefly and return to rest.** Juice is transient, not a new
   resting state.
2. **Scale juice to importance.** A routine cannon shot is not a capital-ship
   death.
3. **Stay in the original palette.** Tint with the frozen faction rows and the
   existing white/amber assets (ADR 0022), never a new saturated color.

## Core workflow

1. **Confirm the event hooks exist.** Juice attaches to discrete events:
   `on_hit`, `on_kill`, `on_launch`, `on_death`, `on_pickup`. If the simulation
   does not expose them, add a presentation event at the boundary first -- do not
   poll for state changes every frame.
2. **Pick feedback channels per event** from the menu (sound, particles, shake,
   hit-stop, flash, recoil, tween, number pop). Start with two or three; add
   until it reads, then stop.
3. **Make motion eased, not linear.** Route scale, position and UI changes
   through an ease (overshoot for "pop", ease-out for "settle"). Linear motion
   feels robotic.
4. **Reserve hit-stop and shake for impact.** They are the strongest and most
   abusable tools: short, scaled to importance, never on routine actions.
5. **Keep feedback off the simulation.** Shake moves the camera or a visual
   pivot, not the ship transform; hit-stop pauses presentation, not the 60 Hz
   gameplay logic (ADR 0010).
6. **Tune by importance tiers.** Define small/medium/large presets and assign
   every event to a tier so the whole game stays proportional.
7. **Verify by playing and watching.** Trigger the event repeatedly; confirm the
   feedback fires, returns to rest, keeps the HUD readable and does not block
   input. Report what you observed.

## Patterns

### 1. Screen shake by decaying trauma (smooth, not random jitter)

```csharp
// Trauma 0..1; shake = trauma^2. Write the offset to the rig's `_shakeOffset`
// hook (see ab-camera-systems), never to the ship body. It decays every frame
// and ends on its own.
float _trauma;
float _time;
const float Decay = 1.2f;
static readonly Vector2 MaxOffset = new Vector2(8f, 6f); // world units

public void AddTrauma(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

void LateUpdate()
{
    if (_trauma <= 0f) { _cameraRig.SetShakeOffset(Vector2.zero); return; }
    _trauma = Mathf.Max(_trauma - Decay * Time.deltaTime, 0f);
    float shake = _trauma * _trauma;            // quadratic: gentle low, sharp high
    _time += Time.deltaTime * 30f;
    // Layered sines: smooth and self-ending. A fresh Random per frame buzzes.
    var offset = new Vector2(MaxOffset.x * shake * Mathf.Sin(_time * 1.7f),
                             MaxOffset.y * shake * Mathf.Sin(_time * 2.3f));
    _cameraRig.SetShakeOffset(offset);
}
```

The rig composes `_focus + _shakeOffset` in `LateUpdate` (see
`ab-camera-systems`) and keeps `ScreenToWorld` on `_focus` so aiming does not
drift. Keep the offset small: one world unit is one frozen stage pixel (ADR
0017) and the Short view is 1920x1080 design px -- the 640x360 stage at 3x (ADR
0021) -- so 8 units is already a visible jolt.

### 2. Hit-stop / freeze frame (sell impact without stalling logic)

```csharp
// Presentation-only freeze: leaves the time scale untouched, so the 60 Hz
// simulation keeps stepping (ADR 0010). A later hit only extends the deadline.
float _hitStopUntilUnscaled;
float _animTime;      // advanced by hand so the held frames are exact

public void RequestHitStop(float seconds) =>
    _hitStopUntilUnscaled = Mathf.Max(_hitStopUntilUnscaled, Time.unscaledTime + seconds);

bool IsFrozen => Time.unscaledTime < _hitStopUntilUnscaled;

void LateUpdate()
{
    // Frozen frames hold the VFX pose and animation; gameplay, input and audio
    // keep running. Advance the effect with the supplied step, not a coroutine.
    float step = IsFrozen ? 0f : Time.unscaledDeltaTime;
    _animTime += step;
    AdvanceEffect(step);
}
```

The freeze lives entirely in presentation state, evaluated in `LateUpdate`.
Never scale game time: the simulation, input and audio must stay on the real
60 Hz step, and a stopped scale breaks job scheduling and the test gate.

### 3. Recoil on the firing craft (visual only)

```csharp
// Push the rendered sprite/visual pivot back along the shot direction and
// spring it home. Never move the simulated ship transform.
Vector2 _visualOffset;
void OnFired(Vector2 shotDirection, float impulse)
{
    _visualOffset -= shotDirection.normalized * impulse; // 1..3 world units
}
void Update()
{
    _visualOffset = Vector2.Lerp(_visualOffset, Vector2.zero, 1f - Mathf.Exp(-20f * Time.deltaTime));
    _visual.localPosition = _visualOffset;
}
```

The muzzle flash and projectile already carry the faction color (ADR 0022); do
not recolor them per effect.

### 4. A feedback bundle scaled by importance

```csharp
// One call per event; the tier keeps the whole game consistent. Particles go
// through MissionSpriteView.SpawnImpact, which fans out to the pooled
// explosion/flash paths; never play a particle system directly.
void Feedback(float2 at, Tier tier)
{
    switch (tier)
    {
        case Tier.Small:
            _audio.PlayTrigger(AudioTrigger.CombatWeaponLight);
            _shake.AddTrauma(0.15f);
            break;
        case Tier.Medium:
            _audio.PlayTrigger(AudioTrigger.CombatWeaponMedium);
            _shake.AddTrauma(0.4f);
            RequestHitStop(0.04f);
            _spriteView.SpawnImpact(at, partDetached: true, hullDestroyed: false);
            break;
        case Tier.Large:
            _audio.PlayTrigger(AudioTrigger.CombatExplosion);
            _shake.AddTrauma(0.8f);
            RequestHitStop(0.10f);
            _spriteView.SpawnImpact(at, partDetached: false, hullDestroyed: true);
            break;
    }
}
```

Use the real `AudioTrigger` values and the existing `MissionSpriteView` effect
paths; adding a new sound or sprite is asset work that needs ADR 0020 approval.

## Pitfalls

- **Shaking the ship body instead of the camera or visual pivot** desyncs
  collision, aiming and the HUD reticle mapping.
- **Random offset every frame** buzzes like static. Drive shake from sampled
  sines and a decaying trauma so it is smooth and self-ending.
- **Hit-stop by slowing game time** freezes the simulation, input and the
  fixed-step gate. Hold the presentation state instead.
- **Hit-stop on every frame of a held attack** locks the presentation. Trigger
  once per impact.
- **Overlapping hit-stops** let an older timer end a newer freeze. Guard with a
  `Mathf.Max` on the deadline, as above.
- **Permanent exaggeration** (scale never returns, shake never decays) becomes
  the new normal and stops reading as feedback.
- **New colors or full-screen flashes.** They break the original palette and can
  hide enemies, projectiles or HUD text. Tint within ADR 0022 and keep effects
  behind the play field.
- **Photosensitivity.** Keep large-area flashes at or below three in any
  one-second window and offer a reduce-shake/reduce-flash setting.
- **Feedback that blocks input** (long freeze, un-cancelable animation) hurts
  responsiveness. Keep juice short and let input buffer through it.

## References

- `references/feedback-recipes.md` -- trauma math, easing cheat sheet, feedback
  menu, importance tiers and accessibility toggles for this project.
- `docs/adr/0004-combat-damage-telemetry-and-tactical-hud.md` -- which combat
  events already have HUD/audio feedback.
- `docs/adr/0020-original-fonts-and-asset-fidelity.md` -- owner approval and
  palette rules.
- `docs/adr/0022-procedural-projectile-visuals-and-faction-palette.md` --
  faction hues and Captain amber.

## Related skills

- `ab-camera-systems` -- owns follow/deadzone/framing; this skill only feeds it
  shake.
- `ab-physics-tuning` -- knockback forces and timestep stability.
- `ab-performance-optimization` -- pooled particles and VFX budgets.
- `ab-game-ui-ux` -- damage numbers and HUD presentation rules.
