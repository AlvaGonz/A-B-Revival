# ABRevival.Editor

Assembly containing Unity Editor tooling, custom inspectors, and asset pipeline importers (e.g. `PartCatalogImporter`).
Excluded from standalone player builds via `includePlatforms: ["Editor"]`.

## Sprite export pipeline

`SpriteExportPipeline` regenerates the modular part and UI sprites from the frozen SWF symbol
libraries and rebuilds the sprite atlases. Original game IP: Vargames (2011).

### Run it

Menu: **Tools > A&B Revival > Regenerate Sprites and Atlases**

Batchmode (one command):

```cmd
"C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe" -batchmode -quit -nographics -projectPath "C:\Dev\A&B revival\A&B-U" -executeMethod ABRevival.Editor.SpriteExportPipeline.RegenerateBatch -logFile -
```

FFDec is resolved from `FFDEC_CLI`, then `C:\Program Files (x86)\FFDec\ffdec-cli.exe`, then `ffdec-cli` on `PATH`.

### Inputs and outputs

| Input | Use |
|---|---|
| `src/assets/binaryData/8_all.AllData_defPart.bin` | part catalog ids, names, and SWF symbol (`mcName`) |
| `src/assets/binaryData/4_all.AllData_data.bin` | part symbols (`Body_*`, `Wing_*`, `Gun_*`, `Pow_*`) |
| `src/assets/binaryData/9_all.AllData_ui.bin` | raster UI images |

| Output | Contents |
|---|---|
| `Assets/Art/Sprites/Parts/part_<id>_<symbol>.png` | one PNG per SWF part symbol |
| `Assets/Art/Sprites/UI/ui_<characterId>.png` | UI bitmap images from the UI asset |
| `Assets/Art/Atlases/PartsAtlas.spriteatlasv2` | part sprites, BC7 on desktop / ASTC 6x6 on mobile |
| `Assets/Art/Atlases/UiAtlas.spriteatlasv2` | UI images, same compression policy |

The `<id>` is the lowest catalog part id that uses the symbol. Left/right wing variants share
one symbol (`Wing_1` is used by ids 173 and 222); the right variant is mirrored at runtime, as
in the Flash baseline. `PartSpriteMap` in `ABRevival.Core.Data` owns this mapping and a Tier 1
test asserts it is deterministic and covers all 271 catalog ids.

### Scale and import contract

Parts are exported at native symbol pixels (zoom 1). The 60 FPS baseline draws part bitmaps
1:1 into the 1920x1080 canvas (`RenderData.W/H`, `BMPD2D.copyPixels`), and ticket #123 sets
Unity to one unit per stage pixel with sprite PPU 1. Upscaling the vectors would make every
part three times too large in world units.

Every PNG is imported as a single sprite, `Pixels Per Unit = 1`, custom pivot `(0.5, 0.5)` at
the symbol origin (the socket grid origin), full-rect mesh, no mipmaps, no POT rescaling, and
uncompressed source pixels. Compression happens only in the atlases: BC7 for Standalone,
ASTC 6x6 for Android and iOS.
