# Main-Menu Layers (`UI_Main`)

Provenance and import contract for the main-menu layers split out of the frozen UI library.
Decision: [ADR 0024](../adr/0024-main-menu-foreground-composition.md). Follow-ups: #206, #207, #210.

## Source

| | |
|---|---|
| Library | `src/assets/binaryData/9_all.AllData_ui.bin` (frozen UI SWF, ADR 0015) |
| Page symbol | `UI_Main` sprite **459**, one frame |
| Page shape | `DefineShape4` **442** — two bitmap fills plus the vector plate |
| Title lettering | `DefineSprite` **444** → shape **443** |

`UI_Main` is not a single bitmap. Sprite 459 draws shape 442, whose two bitmap fills are the
background and the ship; the plate is vector; the lettering comes from sprite 444.

## Layers

| Layer | Source | Committed PNG | Native size | Pivot | Alpha |
|---|---|---|---:|---|---|
| Background | bitmap **5** | `A&B-U/Assets/Art/Sprites/UI/ui_0005.png` (already committed) | 640×480 | (0.021028, 0.923337) | opaque |
| Title composite | plate (shape 442) + main title + subtitle | `A&B-U/Assets/Art/Sprites/UI/Screens/UI_Main_TitleComposite.png` | 460×92 | (0, 1) top-left | 0–255, transparent outside the plate |
| Ship | bitmap **441** | `A&B-U/Assets/Art/Sprites/UI/Screens/UI_Main_Ship.png` | 427×344 | (0.5, 0.5) centre | 0–255, 75,400 fully transparent px |

### Background: no new file

FFDec bitmap 5 is byte-identical to `ui_0005.png`:

```text
sha256(sprite 5 export) = 8FD2CC34B81032A518DE120BC14C16149028FE06B37E72D82CBD602912107DB9
sha256(ui_0005.png)     = 8FD2CC34B81032A518DE120BC14C16149028FE06B37E72D82CBD602912107DB9
```

`ui_0005.png` **is** the background layer; do not stack a second starfield.

### Title composite (#210)

One image, built from the original parts — not a page crop:

- **Plate:** full shape 442 vector render, cool-tinted to match the page.
- **Main title:** sprite 444 main lettering with a blue outer glow (12 px padding).
- **Subtitle:** sprite 444 subtitle rebuilt as black fill with a white outline.

The parts are composited at page scale into a 460×92 image. The composite is one runtime layer,
so the whole title moves and scales as a unit; the position and size are serialized on
`MainMenuController`. The raw export's unreadable black subtitle and missing glow are corrected.

### Ship

Bitmap 441 as exported, unmodified. Opaque bbox `(3, 6)–(427, 342)`; the canvas centre sits
~1.5 px left and ~2 px above the visible hull centre. That offset is original art and is not
corrected. 427×344 at PPU 1.

## Import contract

Settings copied from `ui_0005.png.meta`:

- Sprite (Single), `spriteMode: 1`, PPU 1, `alignment: 9` (custom pivot)
- No mipmaps, bilinear, clamp, `alphaIsTransparency: 1`
- `textureCompression: 0` on every platform profile
- `maxTextureSize: 2048`, no nine-slice, full-texture sprite rect

| PNG | Meta GUID |
|---|---|
| `UI_Main_TitleComposite.png` | `1d621215c8d5403c82fcdc07d54d2c02` |
| `UI_Main_Ship.png` | `091aa3c533f7116874136a9a56468412` |

Neither is in `UiScreensAtlas.spriteatlasv2`; the menu renders them through `UiSpriteSkin`'s
`Image` child, not UI Toolkit's atlased `background-image` path.

## Layout controls (#210)

`MainMenuController` exposes page-pixel layout for every visible menu object:

- `titlePosition` / `titleSize` (default 0,0 / 460×92) — one composite image
- `shipScale` — multiplier on the auto-fitted ship scale; the ship position remains automatic,
  centred in the band between the title and the bottom control row
- `buttonSize` and six per-button positions (`playButtonPosition`, … `playerButtonPosition`)
  — all six buttons share the same size
- `backdropOffset` / `backdropSize` (zero size = full screen)
- `anchorBottomRow` keeps the four bottom buttons anchored; off uses each authored Y
- `buttonScale` / `bottomMargin` remain for size and bottom inset

The automatic ship band uses equal top and bottom gaps; the PlayMode guard allows a mismatch
of at most **1 % of the band height**. Safe area: the title and the bottom control margin use
the larger of the designed inset and the reported safe-area inset + 8 logical px.

## Revert point

- Tag **`pre-187-foreground-split`** (commit `9fafa14`) holds the pre-split
  `UI_Main.png`, `UI_Main_Hero.png`, `ui_0005.png` and the UI library.
- External copy: `%TEMP%\opencode\ab-main-menu-backup-2026-11-09-pre-187\`.
- The new layer PNGs are additive; no original asset was edited or deleted.
