# 0024. Main-Menu Foreground Composition

**Date:** 2026-11-09
**Status:** Accepted (owner decision, #187)
**Related:** ADR 0017, ADR 0020, ADR 0021, #124, #187

## Context

The frozen `UI_Main` page was one flattened 667×535 render. The mobile workaround crops it at `y=455` (`UI_Main_Hero`), feathers the bottom into `ui_0005`, and hides the top plate line with an offset. The side spikes and the ship position stay baked into the image, so the menu cannot put the title at the top, the ship in the middle and the controls at the bottom on tall screens.

The owner wants the spikes hidden and the ship centred between the title and the controls.

## Research

FFDec inspection of the UI library (`9_all.AllData_ui.bin`) shows that `UI_Main` (sprite 459, one frame) is not a single bitmap:

- `DefineShape4` 442 contains two bitmap fills and 47 vector paths.
- Bitmap **5** is the 640×480 opaque background (starfield and planets).
- Bitmap **441** is the 427×344 ship, with a real alpha channel (75,400 transparent pixels).
- The title plate and its side spikes are vector paths in the same shape.

Separate transparent layers are therefore possible without redrawing the ship or the background.

## Decision

1. **Split `UI_Main` into three layers:** background (bitmap 5), ship (bitmap 441, alpha preserved), and title plate (vector shape 442 without the two bitmap fills).
2. **Edit the title plate vector** to remove the top grey bar and the side spikes. This is an owner-approved improvement to the original art; the title lettering itself is preserved.
3. **Recompose the menu by layout:** title at the top, ship centred in the space between the title and the controls, controls at the bottom, one background. Do not use per-profile `heroOffsetX` values for centring.
4. **Keep device profiles manual.** 750×1334, 1170×2532 and 2048×2732 are edge-case checks, not an automated PlayMode gate.
5. **Fallback:** if the vector title cannot be separated cleanly, edit the flattened hero PNG to remove the bar and spikes, with owner approval and a before/after record. Do not redraw the title.
6. **Backup before edits:** tag `pre-187-foreground-split` plus an external copy of the original `UI_Main.png`, `UI_Main_Hero.png` and the UI library. Revert by returning to the tag.

## Consequences

- Positive: the ship can be centred on any aspect ratio; the spikes and top bar disappear from the rendered menu; the background remains the original starfield.
- Positive: no ship or background redraw; only the title plate frame is edited.
- Negative: the title plate is no longer byte-identical to 2011; the edit is documented and reversible.
- Negative: three layers and their pivots replace one crop, so the menu layout code grows slightly.
- Constraint: `UI_Main_Hero` is retired once the new composition is verified; the original screen render stays as reference.

## Enforcement

- `docs/adr/0020` still governs asset fidelity: this edit is approved and recorded.
- #187 owns the decision; the layer-extraction and recomposition tickets implement it.
- The pre-edit tag is the revert point if the owner rejects the result.

## Implementation note (#210)

The shipped title is the page-rendered banner (plate + blue glow + subtitle), not the split
plate/lettering pair. The shape 442 / sprite 444 analysis above remains the source map; the
banner is the faithful composite of that art.

## Implementation note (#206)

Extraction confirmed the split described in Research and refined it in one way:

- The title lettering is **not** a path inside shape 442. It is a separate `DefineSprite` **444**
  wrapping shape **443** (394×52 lettering, both title lines, clean alpha). It is exported as
  `UI_Main_Title.png` with a top-centre pivot.
- Bitmap **441** (ship, 427×344, alpha preserved) is exported unchanged as `UI_Main_Ship.png`,
  centre pivot.
- Bitmap **5** is byte-identical to the already committed `A&B-U/Assets/Art/Sprites/UI/ui_0005.png`
  (matching SHA-256), so no duplicate background file was added; `ui_0005.png` is the background
  layer.
- Shape 442's remaining vector paths are the page plate, top grey bar and side spikes. They are
  **dropped wholesale**, so decision item 2's "edit the title plate" collapses to "do not draw the
  plate": no lettering path had to be edited or deleted, and no redrawn title asset is produced.

Provenance, pivots and import settings: `docs/assets/main-menu-layers.md`.

## Implementation note (#207)

The unfiltered `DefineSprite` 444 export carried a black subtitle (`THE STORY IN THE SPACE`)
that disappeared against the darker starfield. With owner approval the subtitle was
recoloured white; the main title gradient and the alpha channel are unchanged, and the
sprite stays 394×52 with the same top-centre pivot and `.meta` GUID. This is the second
owner-approved art adjustment under this ADR.
