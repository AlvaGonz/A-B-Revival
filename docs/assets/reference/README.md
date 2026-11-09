# Menu Page References

Full-page renders of the frozen 2011 menu symbols, used as the 1:1 layout reference
before any menu section is rebuilt (ADR 0020).

| File | Frozen symbol | Character id | Size |
|---|---|---:|---|
| `factory-page.png` | `UI_MakePlayer` | 440 | 851x510 |
| `shop-page.png` | `UI_Shop` | 205 | 770x510 |
| `UI_HelpGame.png` | `UI_HelpGame` | 824 | 750x510 |
| `UI_HelpGame2.png` | `UI_HelpGame2` | 794 | 750x510 |
| `UI_Data.png` | `UI_Data` | 534 | 640x480 |
| `UI_Mission.png` | `UI_Mission` | 300 | 640x480 |
| `UI_MapBuilding.png` | `UI_MapBuilding` | 311 | 851x521 |

`factory-page.png` is the original `UI_MakePlayer` page export from
`src/assets/binaryData/9_all.AllData_ui.bin` (FFDec page sprite, frame 1). It also ships
as `A&B-U/Assets/Art/Sprites/UI/ui_0440_reference_hangar_page.png`; the copy here is the
canonical review reference.

`shop-page.png` is the original `UI_Shop` page export from the same library (FFDec page
sprite, frame 1). It also ships as
`A&B-U/Assets/Art/Sprites/UI/ui_0205_reference_shop_page.png`; the copy here is the
canonical review reference.

The Info/Records references were used for #177. `UI_HelpGame.png` and `UI_HelpGame2.png`
also ship under `A&B-U/Assets/Art/Sprites/UI/Help/`; `UI_Data.png` and `UI_Mission.png`
under `A&B-U/Assets/Art/Sprites/UI/Screens/`. Both copies are byte-identical exports.

`UI_MapBuilding.png` is the actual Ready/Briefing handoff used by #178 and is now the
runtime page art: the 851x521 render supplies the metal panel, window frame, MAP LOADING
badge and OK position, while the live window covers its placeholder title/word lines with
`MISSIONS.missionWords[STORY]` and the validated loadout. `UI_Mission.png` is the pause-menu
objective sheet (`UIPause`), not the ready handoff; it stays committed for the
pause/objectives surface. The source copy lives at
`A&B-U/Assets/Art/Sprites/UI/Screens/UI_MapBuilding.png` and the widget exports
(`306` loader mark, `309` title plate, `310` OK button) at
`A&B-U/Assets/Art/Sprites/UI/UI_MapBuilding/`.

Regenerate the asset-side copy with `scripts\export-ui-sprites.bat`
(`ABRevival.Editor.UiSpriteExportPipeline.ExportBatch`), which also mirrors page
references into this folder.

Do not redraw, crop or annotate these files.

## Original font mapping (ADR 0020)

All game UI text is rendered with the original faces extracted from the frozen SWF; the
eight extracted assets are the only permitted fonts. The shared theme
`A&B-U/Assets/UI/UnityDefaultRuntimeTheme.tss` sets SF TransRobotics (font 7) on `:root`,
so every `TextElement` inherits the original face. Data surfaces opt into the other
families through the shared `font-*` classes in the same theme; a screen never declares
a font URL itself.

| Asset (`A&B-U/Assets/Art/Fonts/`) | Family | SWF symbol | Source library | Frozen usage | Shared class |
|---|---|---:|---|---|---|
| `font_0007_SF_TransRobotics.ttf` | SF TransRobotics | 7 | `9_all.AllData_ui.bin` | Menu chrome, buttons, help pages and HUD labels (211 tags) | theme `:root` / `.font-ui` |
| `font_0081_Arial.ttf` | Arial | 81 | `9_all.AllData_ui.bin` | Center, Factory and Shop table headers/rows, stats and detail values (116 tags) | `.font-data` |
| `font_0002_sans.ttf` | _sans | 2 | `9_all.AllData_ui.bin` | Options/Data labels, Factory undo slots, page counters and the Center instruction (41 tags) | `.font-sans` |
| `font_0478_sans.ttf` | _sans | 478 | `9_all.AllData_ui.bin` | Companion grade letters and the HUD pilot name (6 tags) | `.font-grade` |
| `font_0627_SF_TransRobotics.ttf` | SF TransRobotics | 627 | `9_all.AllData_ui.bin` | `Mov1`-`Mov9` cinematic captions (10 tags) | `.font-cinematic` |
| `font_0701_SF_TransRobotics.ttf` | SF TransRobotics | 701 | `4_all.AllData_data.bin` | `BGWord` credits (31 tags) | `.font-credits` |
| `font_0595_Times_New_Roman.ttf` | Times New Roman | 595 | `9_all.AllData_ui.bin` | "35" version plate (only the `3` and `5` glyphs) | `.font-version` |
| `font_0496_SF_Zero_Gravity.ttf` | SF Zero Gravity | 496 | `9_all.AllData_ui.bin` | HUD `Q` logo mark (only the `Q` glyph) | `.font-logo` |

`font_0595_Times_New_Roman.ttf` and `font_0496_SF_Zero_Gravity.ttf` are two-glyph and
one-glyph subsets. UI Toolkit cannot render arbitrary UI strings with them, and the
current documents have no version plate or `Q` logo element to bind them to. If a
future surface adds one, the closest original family is used instead: Arial for the
numeric version plate and SF TransRobotics for logo text. `.font-cinematic` and
`.font-credits` are bound to the shared theme for the caption and credits surfaces,
which are not implemented yet. No Unity default or operating-system font is used
anywhere.

The page references above are the frozen Flash page symbols at their native stage size;
the Unity layout targets the ADR 0013 1920x1080 canvas. Text metrics were checked at
1920x1080, 1280x720 and 1680x1050 (16:10) by the PlayMode `FontPlayModeTests` font and
overflow assertions, with captures written to `%TEMP%\opencode\fonts-*.png`. The taller
original line box needed two spacing corrections: the Factory detail card grew from 30%
to 36%, and the HUD hull value from 16px to 19px.

