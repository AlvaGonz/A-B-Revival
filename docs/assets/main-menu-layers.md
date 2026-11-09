# Main-Menu Layer Split (`UI_Main`)

Provenance and import contract for the three main-menu layers split out of the frozen UI
library. Implements #206; decision in [ADR 0024](../adr/0024-main-menu-foreground-composition.md).

## Source

| | |
|---|---|
| Library | `src/assets/binaryData/9_all.AllData_ui.bin` (frozen UI SWF, ADR 0015) |
| Page symbol | `UI_Main` sprite **459**, one frame |
| Page shape | `DefineShape4` **442** — two bitmap fills plus 47 vector paths (title plate, top grey bar, side spikes) |

`UI_Main` is not a single bitmap. Sprite 459 draws shape 442, whose two bitmap fills are the
background and the ship; the lettering comes from a separate sprite 444 / shape 443.

## Layers

| Layer | Source symbol | Committed PNG | Native size | Pivot | Alpha |
|---|---|---|---:|---|---|
| Background | bitmap **5** | `A&B-U/Assets/Art/Sprites/UI/ui_0005.png` (unchanged, already committed) | 640×480 | (0.021028, 0.923337) | opaque, alpha 255 everywhere |
| Title plate | shape **442** (vector, plate only) | `A&B-U/Assets/Art/Sprites/UI/Screens/UI_Main_TitlePlate.png` | 440×71 | (0, 1) top-left | 0–255, transparent outside the plate |
| Ship | bitmap **441** | `A&B-U/Assets/Art/Sprites/UI/Screens/UI_Main_Ship.png` | 427×344 | (0.5, 0.5) centre | 0–255, 75,400 fully transparent px, all four corners transparent |
| Title lettering | `DefineSprite` **444** → shape **443** | `A&B-U/Assets/Art/Sprites/UI/Screens/UI_Main_Title.png` | 394×52 | (0.5, 1.0) top-centre | 0–255, clean edges, all four corners transparent |

### Background: no new file

FFDec bitmap 5 exports 640×480 opaque and is **byte-identical** to the sprite already in the
tree:

```text
sha256(sprite 5 export)        = 8FD2CC34B81032A518DE120BC14C16149028FE06B37E72D82CBD602912107DB9
sha256(UI/ui_0005.png)         = 8FD2CC34B81032A518DE120BC14C16149028FE06B37E72D82CBD602912107DB9
```

So `ui_0005.png` **is** the background layer; no `UI_Main_Background.png` is committed and the
runtime must not stack a second copy of the starfield.

### Ship

Bitmap 441 as exported, unmodified. The opaque bounding box is `(3, 6)–(427, 342)`, so the
canvas centre sits ~1.5 px left and ~2 px above the visual centre of the hull; that offset is
part of the original art and is not corrected. The sprite is 427×344 at PPU 1, meaning it is
427×344 px in UI pixels.

### Title lettering

Two candidates were reviewed:

| Candidate | Size | Alpha 0 px | Partial alpha | Result |
|---|---:|---:|---:|---|
| `DefineSprite_444/1.png` (original filtered sprite) | 394×52 | 11,518 | 4.95 % | **chosen** |
| shape `443.png` vector render | 641×506 canvas, lettering in a 394×51 corner box | 314,453 (on the padded canvas) | 13.29 % | rejected: stage-sized canvas, antialiased edges only |

Both render the same artwork (31 of 8,690 shared opaque pixels differ by more than 30 summed
RGB), but sprite 444 is already cropped to the lettering, has cleaner alpha and is the original
compiled asset. It contains `ASSEMBLY AND BATTLE` and the `THE STORY IN THE SPACE` sub-line,
with no clipping on any edge (content bbox `0,0–393,52`). Top-centre pivot `(0.5, 1.0)` =
(197, 0) px, so the sprite hangs down from the anchored top edge.

### Subtitle colour (#207)

The unfiltered sprite 444 export rendered the sub-line black, which vanished against the
starfield. Owner-approved #207 recoloured **only the subtitle** white to match the 2011 page
render; the main title gradient and the alpha channel are unchanged. `UI_Main_Title.png`
stays 394×52 and keeps its original `.meta` (GUID `2f3617872ba41259c97de1f2bde65c15`).

## Title plate (#210)

The grey page plate is restored from the same shape 442 vector render, cropped to the page
rectangle `(14, 0)–(454, 71)`. The crop excludes the top grey bar and the outermost spike
tips; the inner plate decoration remains. The lettering stays sprite 444 / shape 443 and is
drawn on top of the plate. The plate is 440×71 at PPU 1, pivot `(0, 1)` top-left, and moves
with the title (including safe-area insets). This supersedes the "not used" note from #206.

## Import contract

Both new PNGs use `TextureImporter` settings copied from `ui_0005.png.meta` (see
`docs/assets/ui-sprites.md` for the manifest-wide contract):

- Sprite (Single), `spriteMode: 1`, PPU 1, `alignment: 9` (custom pivot)
- No mipmaps, bilinear, clamp, `alphaIsTransparency: 1`
- `textureCompression: 0` on every platform profile (uncompressed)
- `maxTextureSize: 2048`, no nine-slice (`spriteBorder` all zero), full-texture sprite rect

| PNG | Meta GUID |
|---|---|
| `UI_Main_TitlePlate.png` | `0d4fca79ec274f1286b3b3e4be21f64b` |
| `UI_Main_Ship.png` | `091aa3c533f7116874136a9a56468412` |
| `UI_Main_Title.png` | `2f3617872ba41259c97de1f2bde65c15` |

Neither sprite is in `UiScreensAtlas.spriteatlasv2`: the main menu renders them through
`UiSpriteSkin`'s `Image` child, not UI Toolkit's atlased `background-image` path (#207).

## Re-extract

```powershell
# bitmaps 5 (background) and 441 (ship)
& "C:\Program Files (x86)\FFDec\ffdec-cli.exe" -export image <temp> `
  "src\assets\binaryData\9_all.AllData_ui.bin"

# title sprite 444, frame 1
& "C:\Program Files (x86)\FFDec\ffdec-cli.exe" -export sprite <temp> `
  "src\assets\binaryData\9_all.AllData_ui.bin"
# -> <temp>\DefineSprite_444\1.png (black subtitle: apply the owner-approved
#    white subtitle recolour from #207 before committing)
```

## Revert point

- Tag **`pre-187-foreground-split`** (commit `9fafa14`) holds the pre-split
  `UI_Main.png`, `UI_Main_Hero.png`, `ui_0005.png` and the UI library.
- External copy: `%TEMP%\opencode\ab-main-menu-backup-2026-11-09-pre-187\` (same three files
  plus `BASE_COMMIT.txt`).
- The new layer PNGs are additive; reverting is `git checkout pre-187-foreground-split` (or
  deleting the two new files) — no original asset was edited or deleted.

## Notes for the #207 implementer

- Background = `ui_0005.png` (640×480, opaque, PPU 1). Stretch it to the screen; do not add a
  second starfield or a `UI_Main_Hero` crop.
- Ship pivot is the sprite centre; centre it in the free vertical space between the title and
  the control row rather than baking per-profile offsets.
- Title pivot is its top-centre; anchor it to the top of the screen so the sprite grows down.
- `UI_Main.png` and `UI_Main_Hero.png` remain in the tree as reference but are no longer drawn
  by the menu.
- The ship sits in the band between the title bottom and the bottom control row with equal
  top and bottom gaps. The PlayMode guard allows a mismatch of at most **1 % of the band
  height**.
- Safe area: the title top and the bottom control margin use the larger of the designed inset
  and the reported safe-area inset + 8 logical px. A full safe area (desktop/editor) leaves
  the designed layout unchanged.
- Button positions are serialized on `MainMenuController` in page pixels. `anchorBottomRow`
  defaults on; turning it off uses each bottom button's authored Y.
- The title plate is drawn behind the title and moves with it; it is not the full shape 442
  page plate (top bar and outer spike tips are cropped).
