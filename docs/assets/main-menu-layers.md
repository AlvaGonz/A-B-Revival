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
| Title (full) | plate (shape 442) + letters | `A&B-U/Assets/Art/Sprites/UI/Screens/Title/UI_Main_TitleFull.png` | 460×108 | (0, 1) top-left | 0–255, transparent outside the plate |
| Ship | bitmap **441** | `A&B-U/Assets/Art/Sprites/UI/Screens/UI_Main_Ship.png` | 427×344 | (0.5, 0.5) centre | 0–255, 75,400 fully transparent px |

### Background: no new file

FFDec bitmap 5 is byte-identical to `ui_0005.png`:

```text
sha256(sprite 5 export) = 8FD2CC34B81032A518DE120BC14C16149028FE06B37E72D82CBD602912107DB9
sha256(ui_0005.png)     = 8FD2CC34B81032A518DE120BC14C16149028FE06B37E72D82CBD602912107DB9
```

`ui_0005.png` **is** the background layer; do not stack a second starfield.

### Title components (#210)

The title is extracted both as a ready-to-render full image and as separate components for
editing. All are 460×108 canvases except the two letter crops; offsets are page pixels inside
the full canvas.

| Component | File | Size | Offset | GUID |
|---|---|---|---:|---|
| Plate only | `Screens/Title/UI_Main_TitlePlate.png` | 460×108 | 0, 0 | `9add6c342666455c872f8b91a6e96d5a` |
| Letters only | `Screens/Title/UI_Main_TitleLetters.png` | 460×108 | 0, 0 | `3aad2517b8a74ea1a4c31e6ffcc2992e` |
| Main title only | `Screens/Title/UI_Main_TitleMain.png` | 418×63 | 8, 0 | `54c3109ef3f248cfbb2717338f5fa171` |
| Subtitle only | `Screens/Title/UI_Main_TitleSubtitle.png` | 402×21 | 16, 47 | `994b151d84604c0db0480b2b9971a554` |
| Full (runtime) | `Screens/Title/UI_Main_TitleFull.png` | 460×108 | 0, 0 | `548dea93128743d5978f373d56082727` |

- **Plate:** full shape 442 vector render in its original grey, no colour tint; includes the top bar
  and side decoration.
- **Main title:** sprite 444 main lettering with a blue outer glow (12 px padding).
- **Subtitle:** sprite 444 subtitle rebuilt as black fill with a white outline.
- **Full:** plate + letters, the runtime sprite used by `MainMenuController` (`titleArt`).

The raw export's unreadable black subtitle and missing glow are corrected; the original page
and library remain the source of truth.

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
| `Title/UI_Main_TitlePlate.png` | `9add6c342666455c872f8b91a6e96d5a` |
| `Title/UI_Main_TitleLetters.png` | `3aad2517b8a74ea1a4c31e6ffcc2992e` |
| `Title/UI_Main_TitleMain.png` | `54c3109ef3f248cfbb2717338f5fa171` |
| `Title/UI_Main_TitleSubtitle.png` | `994b151d84604c0db0480b2b9971a554` |
| `Title/UI_Main_TitleFull.png` | `548dea93128743d5978f373d56082727` |
| `UI_Main_Ship.png` | `091aa3c533f7116874136a9a56468412` |

Neither is in `UiScreensAtlas.spriteatlasv2`; the menu renders them through `UiSpriteSkin`'s
`Image` child, not UI Toolkit's atlased `background-image` path.

## Layout controls (#210)

`MainMenuController` exposes page-pixel layout for every visible menu object:

- `titlePosition` / `titleSize` (default 0,0 / 460×108) — one composite image
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
