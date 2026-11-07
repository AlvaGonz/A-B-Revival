# 6. Captain Ship Tactical AI Modes (E hotkey)

Date: 2026-11-06

## Status
Accepted

## Context
During combat the Captain's ship (allied flagship) runs a single fixed AI. The player has no way to redirect Captain Tom's tactical behavior without changing the underlying mission script. We want an in-battle hotkey (`E`) that cycles Captain Tom through a small set of tactical modes, with a center-bottom HUD notification naming the newly selected mode and a two-letter code on his portrait.

## Decision
1. **Modes & cycle order** (`E` advances, wrapping): Anchor → Escort → Evade → Seek & Destroy → Fire Support → Shield/Intercept.
2. **Default on deploy:** Anchor (preserves base-game "stand ground" behavior).
3. **Hotkey gating:** `E` is active only when the Captain is deployed and alive; otherwise the center-bottom badge shows `NO CAPTAIN!`.
4. **Anchor reference:** holds the Captain's **mission spawn point** (base-game fixed anchor), not his activation position.
5. **Per-mode behavior:**
   - `AN` Anchor/Hold: hold spawn point; fire at enemies in range.
   - `ES` Escort/Follow: trail ~80px behind the player, match speed; defend nearby hostiles.
   - `EV` Evade/Flee: flee from nearest threat; once safe, hold position but remain in Evade mode.
   - `SD` Seek & Destroy: pursue & engage the closest hostile regardless of player position.
   - `FS` Fire Support: maintain engagement range to target; focus fire on the player's target, falling back to nearest hostile.
   - `SH` Shield/Intercept: position between player and nearest hostile (positioning only; no projectile blocking is modeled).
6. **Firing:** Captain fires in every mode whenever a valid target is in range.
7. **HUD:** center-bottom ephemeral badge with mode title, auto-fading ~1.5s (45 frames @30fps), yellow bold, reusing the camera-distance badge pattern; two-letter code shown on the Captain's portrait during battle.
8. **Audio:** reuse an existing short UI blip (no new asset).
9. **Persistence:** mode resets to Anchor each mission (tactical state is per-battle, not a user preference).

## Consequences
- Gives the player real-time tactical command of the allied flagship without breaking the scripted story deployment.
- Keeps HUD uncluttered by reusing established badge/abbreviation patterns.
- Mode is ephemeral per mission, so a bad selection cannot silently carry into the next battle.
- Shield/Intercept is positioning-only; actual projectile absorption is out of scope and would need separate modeling.
- Pre-battle companion behavior assignment and assigning companions to Michael vs. Captain are deliberately out of scope (separate ticket).