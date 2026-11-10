# Main-Menu Layers (`UI_Main`)

Provenance and import contract for the main-menu layers split out of the frozen UI library.
Decision: [ADR 0024](../adr/0024-main-menu-foreground-composition.md). Follow-ups: #206, #207, #210, #216.

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
| Title (full) | new 2K title source (black panel + plate + letters) | `A&B-U/Assets/Art/Sprites/UI/Screens/Title/UI_Main_TitleFull.png` | 1935×416 (page 460×99) | (0, 1) top-left | 0–255, transparent outside the panel |
| Ship | bitmap **441** | `A&B-U/Assets/Art/Sprites/UI/Screens/UI_Main_Ship.png` | 427×344 | (0.5, 0.5) centre | 0–255, 75,400 fully transparent px |

### Background: no new file

FFDec bitmap 5 is byte-identical to `ui_0005.png`:

```text
sha256(sprite 5 export) = 8FD2CC34B81032A518DE120BC14C16149028FE06B37E72D82CBD602912107DB9
sha256(ui_0005.png)     = 8FD2CC34B81032A518DE120BC14C16149028FE06B37E72D82CBD602912107DB9
```

`ui_0005.png` **is** the background layer; do not stack a second starfield.

### Title components (#210)

The runtime full title is the new 2K source with the black panel, cropped from the black-background `Title.jpg` (outer black keyed out; the enclosed panel stays opaque). The plate/letters rows below are the previous grey-design components; new transparent component exports are pending.

| Component | File | Size | Offset | GUID |
|---|---|---|---:|---|
| Plate only | `Screens/Title/UI_Main_TitlePlate.png` | 460×108 | 0, 0 | `9add6c342666455c872f8b91a6e96d5a` |
| Letters only | `Screens/Title/UI_Main_TitleLetters.png` | 460×108 | 0, 0 | `3aad2517b8a74ea1a4c31e6ffcc2992e` |
| Main title only | `Screens/Title/UI_Main_TitleMain.png` | 418×63 | 8, 0 | `54c3109ef3f248cfbb2717338f5fa171` |
| Subtitle only | `Screens/Title/UI_Main_TitleSubtitle.png` | 402×21 | 16, 47 | `994b151d84604c0db0480b2b9971a554` |
| Full (runtime) | `Screens/Title/UI_Main_TitleFull.png` | 1935×416 | 0, 0 | `548dea93128743d5978f373d56082727` |

- **Plate:** full shape 442 vector render in its original grey, no colour tint; includes the top bar
  and side decoration.
- **Main title:** sprite 444 main lettering with a blue outer glow (12 px padding).
- **Subtitle:** sprite 444 subtitle rebuilt as black fill with a white outline.
- **Full:** plate + letters, the runtime sprite used by `Main.uxml` (`main-title`).

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

Neither is in `UiScreensAtlas.spriteatlasv2`; the menu renders them through `SpriteElement`'s
`Image` child, not UI Toolkit's atlased `background-image` path.

## Layout controls (#210, #216)

The main menu is authored in UI Builder (ADR 0025). Static art is a `SpriteElement` in
`Main.uxml`; it renders an `Image` child (`sprite-art`) and never uses UI Toolkit's atlased
`background-image` path. Position and size the backdrop, title, ship and six buttons in
`Main.uss`. Open `Main.uxml` in UI Builder to see and move the real art; save the UXML/USS
and run Boot to see the change. Hover frames remain in `MainMenuController`.

The following still live in C# because they depend on the live output:

- `shipScale` — multiplier on the auto-fitted ship band; the ship stays centred between the
  title bottom and the bottom control row
- `anchorBottomRow` — keeps the four bottom buttons anchored; off uses the USS `top`
- `bottomMargin` — bottom inset in page pixels
- safe area — the title and bottom margin use the larger of the designed inset and the
  reported safe-area inset + 8 logical px

The automatic ship band uses equal top and bottom gaps; the PlayMode guard allows a mismatch
of at most **1 % of the band height**. Page values at the 1920x1080 reference: title 460×99,
buttons 161×45. USS stores them as width percentages plus `aspect-ratio`, so the layout keeps
the frozen proportions at 16:9, 16:10 and 4:3.

## PanelRenderer note (#216)

MainMenu uses `PanelRenderer` + `ScreenPanel`; the other ten screens still use `UIDocument`
until #217. `PanelRenderer` has no `rootVisualElement`: `ScreenPanel.Root` is the only root and
is delivered through the 3-arg `PanelRenderer.VersionedUIReloadCallback`. Never disable the
`PanelRenderer` (that removes its visual elements); visibility is a `display` toggle.

## Revert point

- Tag **`pre-187-foreground-split`** (commit `9fafa14`) holds the pre-split
  `UI_Main.png`, `UI_Main_Hero.png`, `ui_0005.png` and the UI library.
- External copy: `%TEMP%\opencode\ab-main-menu-backup-2026-11-09-pre-187\`.
- The new layer PNGs are additive; no original asset was edited or deleted.
