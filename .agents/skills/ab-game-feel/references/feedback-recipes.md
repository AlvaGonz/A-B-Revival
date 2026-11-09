# Feedback recipes — depth for `ab-game-feel`

Detail the `ab-game-feel` body defers here: the trauma math, easing cheat
sheet, the rest of the feedback menu, importance tiers and accessibility.
Snippets target Unity 6.6 C# and this project's 640x480 design space.

## 1. The trauma model (why shake feels good)

Track a single `trauma` value in `[0, 1]`. Events **add** trauma; it **decays**
linearly each frame. The actual shake amount is `trauma^2` (or `trauma^3`):

- small, frequent events barely nudge the screen (low trauma, squared is tiny),
- big events punch hard (high trauma, squared is near full),
- shake **always ends** on its own because trauma decays to 0.

Offset is `maxOffset * shake * noise(t)`. Sample summed sines or a smooth noise
function across time — never a fresh `Random` per frame, which produces a buzz
instead of a shake.

```csharp
// Layered sines at incommensurate rates: smooth, cheap, no addon.
static float ShakeAxis(float phase, float t)
    => 0.6f * Mathf.Sin(t * 11f + phase) + 0.4f * Mathf.Sin(t * 23f + phase * 2f);
```

Starting points in design-space pixels: `maxOffset = (6..10, 4..8)`, `decay =
1.0..1.5` trauma/sec, per-hit trauma `0.15` (light) -> `0.8` (heavy). Keep shake
smaller than the upstream defaults: the base view is 640x480, so 8 px is already
a visible jolt.

## 2. Easing cheat sheet — which curve for which job

| Goal | Ease | Unity helper | Notes |
|------|------|--------------|-------|
| UI/element "pop" in | overshoot | `EaseOutBack` (below) | shoots past target, settles back |
| Bouncy, lively land | bounce/elastic | hand-rolled | use sparingly; reads as cartoonish |
| Settle / decelerate | ease-out | `Mathf.SmoothStep` or `EaseOutCubic` | default for "comes to rest" |
| Anticipation / wind-up | ease-in | `EaseInCubic` | slow start before a fast action |
| Smooth A to B both ends | ease-in-out | `Mathf.SmoothStep` | camera moves, menu slides |

The project has no tween library. Prefer `Vector2.SmoothDamp`, `Mathf.SmoothStep`
or a small hand-rolled ease in a coroutine; do not add a dependency for this.

```csharp
static float EaseOutBack(float k)                 // 0 -> 1, peaks ~10% past 1
{
    const float c1 = 1.70158f, c3 = c1 + 1f;
    float m = k - 1f;
    return 1f + c3 * m * m * m + c1 * m * m;
}

IEnumerator Pop(Transform t, float duration = 0.18f)
{
    Vector3 squash = new Vector3(1.3f, 0.7f, 1f);
    for (float e = 0f; e < duration; e += Time.deltaTime)
    {
        // Unclamped so the overshoot past Vector3.one survives the lerp.
        t.localScale = Vector3.LerpUnclamped(squash, Vector3.one, EaseOutBack(e / duration));
        yield return null;
    }
    t.localScale = Vector3.one;
}
```

`Pop` is for UI or a presentation sprite. Do not scale simulated ships: their
colliders and hit bounds are authored at world scale (ADR 0017).

## 3. The rest of the feedback menu

- **Flash:** tint the sprite white for 1–3 frames on hit, then return to the
  existing faction tint. Do not introduce a new color (ADR 0022).
- **Recoil:** push the visual pivot back along the shot direction and spring it
  home; the simulated transform does not move.
- **Knockback:** apply an impulse away from the hit normal, clamped and short;
  let `ab-physics-tuning` own stability.
- **Number/text pop:** spawn a damage number that rises, fades and eases out;
  randomize horizontal drift so stacked hits fan out. Use the HUD font and
  palette from ADR 0020.
- **Particles:** a short burst at the contact point. Pool them — do not
  instantiate and destroy per hit.
- **Freeze frame:** the hit-stop in the body; 0.04 s light -> 0.15 s heavy.
  Prefer freezing the attacker/target visuals when the simulation must stay
  deterministic (ADR 0010).
- **Anticipation and follow-through:** a tiny wind-up before a big action and a
  settle after read as weight; that is animation, not code.
- **Zoom punch:** a brief orthographic nudge through `CombatCameraRig`, restored
  within ~0.1 s. Verify the HUD reticle still maps correctly; if it does not,
  drop the effect instead of breaking aiming.

## 4. Importance tiers (keep the whole game proportional)

Define three presets and assign every juicy event to one. This stops the game
from feeling either dead (under-juiced) or exhausting (everything maxed).

| Tier | Trauma | Hit-stop | Particles | Extra | Example events |
|------|:------:|:--------:|:---------:|-------|----------------|
| small | 0.10–0.20 | none | 0–4 | tick SFX | UI hover, light cannon hit |
| medium | 0.30–0.50 | 0.04–0.06 s | 6–12 | flash | normal hit, missile impact |
| large | 0.70–1.00 | 0.10–0.15 s | 20–40 | flash + number | critical hit, ship death, explosion |

The ADR 0004 HUD alerts already define the large/urgent presentation for damage
and Captain health; reuse that language instead of inventing a second one.

## 5. Accessibility and gameplay clarity

- **Reduce screen shake** — scale trauma output by a 0–100% setting.
- **Reduce/disable flashing** — replace white flashes with a static tint. Keep
  large-area flashes at or under three in any one-second window (WCAG 2.3.1,
  Xbox Accessibility Guideline 118).
- **Reduce camera motion** — cut shake and zoom punches.
- **Never obscure the play field.** Keep flashes, vignettes and particles behind
  or around the action; enemy projectiles and HUD text stay readable at all
  times. If an effect hides a threat, it is a bug, not juice.

These toggles belong in the settings screen; see `ab-game-ui-ux` for the layout.

## 6. Project binding summary

- **Hit-stop:** `Time.timeScale` + `WaitForSecondsRealtime`, newest-call guard;
  local VFX freeze when global slow would disturb the 60 Hz step.
- **Shake:** decaying trauma -> additive camera/pivot offset, applied after follow.
- **Tweens:** `Vector2.SmoothDamp`, `Mathf.SmoothStep` or a hand-rolled ease; no
  new package.
- **Particles and audio:** the existing pooled presentation systems and
  `AudioTrigger` values; new assets need owner approval under ADR 0020.
- **Palette:** faction hues and Captain amber from ADR 0022 only.
