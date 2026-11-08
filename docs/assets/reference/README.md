# Menu Page References

Full-page renders of the frozen 2011 menu symbols, used as the 1:1 layout reference
before any menu section is rebuilt (ADR 0020).

| File | Frozen symbol | Character id | Size |
|---|---|---:|---|
| `factory-page.png` | `UI_MakePlayer` | 440 | 851x510 |

`factory-page.png` is the original `UI_MakePlayer` page export from
`src/assets/binaryData/9_all.AllData_ui.bin` (FFDec page sprite, frame 1). It also ships
as `A&B-U/Assets/Art/Sprites/UI/ui_0440_reference_hangar_page.png`; the copy here is the
canonical review reference.

Regenerate the asset-side copy with `scripts\export-ui-sprites.bat`
(`ABRevival.Editor.UiSpriteExportPipeline.ExportBatch`), which also mirrors page
references into this folder.

Do not redraw, crop or annotate these files.
