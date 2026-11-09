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
| Title banner | rendered `UI_Main.png` crop, masked by the full shape 442 plate | `A&B-U/Assets/Art/Sprites/UI/Screens/UI_Main_TitleBanner.png` | 460×92 | (0, 1) top-left | 0–255, transparent outside the plate |
| Ship | bitmap **441** | `A&B-U/Assets/Art/Sprites/UI/Screens/UI_Main_Ship.png` | 427×344 | (0.5, 0.5) centre | 0–255, 75,400 fully transparent px |

### Background: no new file

FFDec bitmap 5 is byte-identical to `ui_0005.png`:

```text
sha256(sprite 5 export) = 8FD2CC34B81032A518DE120BC14C16149028FE06B37E72D82CBD602912107DB9
sha256(ui_0005.png)     = 8FD2CC34B81032A518DE120BC14C16149028FE06B37E72D82CBD602912107DB9
```

`ui_0005.png` **is** the background layer; do not stack a second starfield.

### Title banner (#210)

The shipped title is a single 460×92 banner cropped from the frozen page render
`UI_Main.png` at the page rectangle `(0, 0)–(460, 92)`, alpha-masked by the full shape 442
plate silhouette. It contains the complete plate, the blue title glow and the subtitle with
its white outline — exactly the 2011 composited look. The crop excludes only the pixels
outside the plate.

This supersedes the earlier split attempt: the unfiltered sprite 444 lost the blue glow and
the baked subtitle colour, so the page-rendered banner is the faithful source. The plate is
complete (top bar and side decoration included); no path edit is required.

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
| `UI_Main_TitleBanner.png` | `1a42bcc1614a49d6a0a317104ea6ffa4` |
| `UI_Main_Ship.png` | `091aa3c533f7116874136a9a56468412` |

Neither is in `UiScreensAtlas.spriteatlasv2`; the menu renders them through `UiSpriteSkin`'s
`Image` child, not UI Toolkit's atlased `background-image` path.

## Layout controls (#210)

`MainMenuController` exposes page-pixel layout for every visible menu object:

- `titleBannerPosition` / `titleBannerSize` (default 0,0 / 460×92)
- `manualShipLayout` + `shipPosition` / `shipSize` (default 427×344)
- `buttonSize` and six per-button positions (`playButtonPosition`, … `playerButtonPosition`)
- `backdropOffset` / `backdropSize` (zero size = full screen)
- `anchorBottomRow` keeps the four bottom buttons anchored; off uses each authored Y
- `buttonScale` / `bottomMargin` remain for size and bottom inset

The automatic ship band remains the default when `manualShipLayout` is off: title bottom to
the bottom control row, equal gaps, 1 % band-height tolerance.

## Revert point

- Tag **`pre-187-foreground-split`** (commit `9fafa14`) holds the pre-split
  `UI_Main.png`, `UI_Main_Hero.png`, `ui_0005.png` and the UI library.
- External copy: `%TEMP%\opencode\ab-main-menu-backup-2026-11-09-pre-187\`.
- The new layer PNGs are additive; no original asset was edited or deleted.
