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
| Title plate | full shape 442 vector render, cool-tinted | `A&B-U/Assets/Art/Sprites/UI/Screens/UI_Main_TitlePlate.png` | 460×92 | (0, 1) top-left | 0–255, transparent outside the plate |
| Main title | sprite 444 rows 0–38, blue glow added | `A&B-U/Assets/Art/Sprites/UI/Screens/UI_Main_TitleMain.png` | 418×63 (12 px glow padding) | (0, 1) top-left | 0–255 |
| Subtitle | sprite 444 rows 39–51, black fill + white outline | `A&B-U/Assets/Art/Sprites/UI/Screens/UI_Main_TitleSubtitle.png` | 402×21 (4 px outline padding) | (0, 1) top-left | 0–255 |
| Ship | bitmap **441** | `A&B-U/Assets/Art/Sprites/UI/Screens/UI_Main_Ship.png` | 427×344 | (0.5, 0.5) centre | 0–255, 75,400 fully transparent px |

### Background: no new file

FFDec bitmap 5 is byte-identical to `ui_0005.png`:

```text
sha256(sprite 5 export) = 8FD2CC34B81032A518DE120BC14C16149028FE06B37E72D82CBD602912107DB9
sha256(ui_0005.png)     = 8FD2CC34B81032A518DE120BC14C16149028FE06B37E72D82CBD602912107DB9
```

`ui_0005.png` **is** the background layer; do not stack a second starfield.

### Title parts (#210)

The title is deliberately split so each piece stays legible at 1920×1080 and on tall screens:

- **Plate:** full shape 442 vector render cropped to `(0,0)–(460,92)`, cool-tinted to match the
  page. It keeps the top bar and side decoration.
- **Main title:** sprite 444 rows 0–38, with a blue outer glow and a 12 px transparent padding
  so the glow is not clipped. The original white/gradient lettering is unchanged.
- **Subtitle:** sprite 444 rows 39–51, rebuilt as black fill with a white outline (the raw
  export was black and unreadable on the starfield).

The plate, main title and subtitle are independent layers; the page-space defaults align them
exactly as the 2011 page. No page-rendered banner crop is used.

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
| `UI_Main_TitlePlate.png` | `d860aa0907ab4612b814dd27517519a7` |
| `UI_Main_TitleMain.png` | `bfb1bc4186e94b4188ac2b1e985f33e1` |
| `UI_Main_TitleSubtitle.png` | `7e33ed8b0540454e9d901dc79b659eb1` |
| `UI_Main_Ship.png` | `091aa3c533f7116874136a9a56468412` |

None is in `UiScreensAtlas.spriteatlasv2`; the menu renders them through `UiSpriteSkin`'s
`Image` child, not UI Toolkit's atlased `background-image` path.

## Layout controls (#210)

`MainMenuController` exposes page-pixel layout for every visible menu object:

- `titlePlatePosition` / `titlePlateSize` (default 0,0 / 460×92)
- `titleMainPosition` / `titleMainSize` (default 8,-2 / 418×63)
- `titleSubtitlePosition` / `titleSubtitleSize` (default 16,45 / 402×21)
- `shipScale` — multiplier on the auto-fitted ship scale; the ship position remains automatic,
  centred in the band between the subtitle and the bottom control row
- `buttonSize` and six per-button positions (`playButtonPosition`, … `playerButtonPosition`)
- `backdropOffset` / `backdropSize` (zero size = full screen)
- `anchorBottomRow` keeps the four bottom buttons anchored; off uses each authored Y
- `buttonScale` / `bottomMargin` remain for size and bottom inset

The automatic ship band uses equal top and bottom gaps; the PlayMode guard allows a mismatch
of at most **1 % of the band height**. Safe area: the title layers and the bottom control
margin use the larger of the designed inset and the reported safe-area inset + 8 logical px.

## Revert point

- Tag **`pre-187-foreground-split`** (commit `9fafa14`) holds the pre-split
  `UI_Main.png`, `UI_Main_Hero.png`, `ui_0005.png` and the UI library.
- External copy: `%TEMP%\opencode\ab-main-menu-backup-2026-11-09-pre-187\`.
- The new layer PNGs are additive; no original asset was edited or deleted.
