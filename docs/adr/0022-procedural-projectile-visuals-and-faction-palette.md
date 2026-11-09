# 0022. Procedural Projectile Visuals and the Faction Palette

**Date:** 2026-11-09
**Status:** Accepted
**Builds on:** ADR 0012, ADR 0017, ADR 0019, ADR 0020, ADR 0021
**Implements:** ticket #184

---

## Context

The frozen release almost never draws a bullet sprite. `F4DataCPX.updateBltByMc` gives a
`normal` bullet `hit = (btwh, btwh)` (2 px when `btwh` is absent), and `Render.render` draws a
Bresenham stroke of `hit.width / 2` buffer pixels from the previous to the current position,
tinted by `Render.changeColor(part.hue + 180)`. Only bullets whose `mcName` is not `normal`
(`Blt_1..Blt_4`) use extracted art, and all four are rockets/missiles.

The Unity port drew every projectile with the baked `bullet_normal.png` disc at a fixed size and
pale hand-picked tints. Weak and heavy weapons looked identical, railgun rounds were not lines,
and projectiles could fall back to the synthetic `__white` cell.

## Decision

1. **Non-missile bullets are procedural square strokes.**
   - Body thickness is `max(2, btwh)` frozen stage pixels (`BresenhamLine.lineRect32` clamps to
     two buffer pixels), multiplied by `CameraDistancePresets.DesignScale` so the stroke shares
     the 3x world-art density of ADR 0021.
   - Body length is one 60 Hz frame of travel (`speed / 60 * DesignScale`), never shorter than
     the square body. Fast, low-`btwh` weapons (`G.L.`, spdS 70-100, btwh 2-6) therefore render as
     long thin railgun lines, while `G.25 -> G.30 -> G.50 -> G.70` grow from 6 px to 22.5 px thick.
   - The body uses the atlas white cell; no new bullet sprite is invented.

2. **Missiles are the only extracted bullet art.**
   - `F4DataBullet.mcName != "normal"` selects the `Blt_*` PNG and its `BulletSprites.txt` world
     size. Because a multiplicative faction tint would destroy the grey/blue art, missiles use a
     35% faction wash into white; their trail still carries the full faction colour.

3. **The palette comes from the frozen faction hues.**
   - `ProjectilePalette` evaluates `RGBHSLHSV.HSV2RGB` with the S = 0.8 / V = 1 that `Particle`
     leaves on the shared singleton: friendly hue 0 + 180 = cyan (51, 255, 255); hostile hue
     180 + 180 = red (255, 51, 51). Level data also uses hostile hue -140 (orange, 40) in later
     missions; the port has no per-craft hue yet and uses the canonical red row.
   - The frozen bullet rule alone would give Captain Tom the same cyan as every other friendly
     (level hue 0), but the HUD already gives him a dedicated identity colour,
     `UIBase.COLOR_CAPTAIN_AMBER` (0xFFCC00). The Captain's projectiles use that amber so the
     allied flagship reads apart from the player's craft. The palette role is explicit, so a
     later per-craft hue can change it in one place.
   - Trails keep the body RGB at alpha 110, replacing the former pale fixed tints.

4. **Palette role is presentation state.**
   - `ProjectileData.paletteRole` is written by `WeaponSystem` at spawn from the shooter's faction
     and `isCaptain`; it never participates in targeting or damage.

5. **Capacity is unchanged.**
   - Each live projectile submits one trail plus one body, the same two instances as before. The
     resolver arrays are built once at mission start and the per-frame path allocates nothing.

## Rejected Options

- **Keep the baked `normal` disc for every normal bullet:** fixed 32x32 art cannot express the
  `btwh` families or the railgun lines.
- **Draw a multi-quad pixelated Bresenham stroke:** costs one instance per pixel step and would
  break the render capacity budget; one tinted quad per body preserves the silhouette.
- **Tint missiles with the full saturated palette:** the multiply shader makes the extracted art
  unreadable; a 35% wash keeps the art and still shows friend/foe.

## Consequences

- Weak, medium and heavy weapons are visibly different, and `G.L.` reads as a railgun.
- The HUD/world palette and the 3x art density now agree (ADR 0021).
- The 65 frozen bullet ids keep one source of truth: `11_all.AllData_defBullet.bin` for
  `btwh`/`mcName`, `BulletSprites.txt` for extracted art.
- Per-mission hostile hues (orange) and per-craft hues remain a documented gap until the level
  hue reaches `ShipEntity`.
