# Build and Export Scripts

Automated scripts for reverse-engineering and modding *Assembly and Battle: The Story in the Space*.

## Scripts

### 1. `recompile.bat`
Compiles the decompiled ActionScript 3 codebase from `src/as3/scripts/` back into a playable SWF binary:
```cmd
scripts\recompile.bat
```
- **Input**: `game\a&b_space.swf`
- **Output**: `game\a&b_space_mod.swf`
- **Source**: `src\as3\scripts\`
- **Tool**: JPEXS FFDec CLI (`ffdec-cli.exe -importScript`)

### 2. `export-assets.bat`
Extracts binary definitions, sounds, shapes, and images from the SWF:
```cmd
scripts\export-assets.bat
```
- **Output**: `src\assets\`
- **Core Binary Data Files**:
  - `8_all.AllData_defPart.bin`: XML definitions of all ship chassis, wings, armor, and modules.
  - `11_all.AllData_defBullet.bin`: XML definitions of projectiles (damage, velocity, lifetime, hitboxes).
  - `2_all.AllData_defFire.bin`: XML definitions of weapon firing dynamics (spread, fire rate, recoil, burst count).
  - `10_all.AllData_levels.bin`: zlib-compressed AMF3 mission and event scripting.
  - `6_all.AllData_sound.bin`: Embedded sound bank.
  - `4_all.AllData_data.bin` & `9_all.AllData_ui.bin`: Embedded UI and graphic SWF libraries.

### 3. `export_audio.py`
Extracts the frozen sound bank into deterministic Unity clips and regenerates the inventory:
```cmd
python scripts\export_audio.py
python scripts\export_audio.py --verify
```
- **Input**: `src\assets\binaryData\6_all.AllData_sound.bin`
- **Output**: `A&B-U\Assets\Resources\Audio\Original\` (11 music, 22 effects)
- **Inventory**: `docs\audio\sound-bank-inventory.md`
- **Tool**: JPEXS FFDec CLI (`ffdec-cli.exe -export sound`)

### 4. `install-hooks.bat` / `install-hooks.sh`
Configures git to use `.githooks/` for pre-commit verification:
```cmd
scripts\install-hooks.bat
```

### 5. `lint_domain_leakage.py`
Static architecture linter preventing domain identity leakage and magic numbers in UI controllers:
```cmd
python scripts\lint_domain_leakage.py [--staged]
```

### 6. `as3_search.py`
High-speed ActionScript 3 symbol and callers search CLI for agents and developers:
```cmd
python scripts\as3_search.py symbol <name>
python scripts\as3_search.py callers <method>
python scripts\as3_search.py class <ClassName>
```

### 7. `run-game.bat` (Root)
Launches the game directly in standalone Flash Player 32.

### 8. `run_air.bat`
Launches the game natively via Adobe AIR Debug Launcher (ADL) with Stage3D direct hardware acceleration enabled:
```cmd
scripts\run_air.bat
```

### 9. `export-ui-sprites.bat`
Regenerates the original hangar and HUD sprites, manifest, and contact sheet:
```cmd
scripts\export-ui-sprites.bat
```
- **Input**: `src\assets\binaryData\9_all.AllData_ui.bin` and the existing part sprites
- **Output**: `A&B-U\Assets\Art\Sprites\UI\` (PPU 1, uncompressed)
- **Manifest**: `docs\assets\ui-sprites.md`
- **Contact sheet**: `docs\assets\ui-sprites-contact-sheet.png`
- **Tool**: Unity 6.6.8f1 batchmode (`ABRevival.Editor.UiSpriteExportPipeline.ExportBatch`)

### 10. `export_all_assets.py`
One command for the whole extraction pipeline described in ticket #183:
```cmd
python scripts\export_all_assets.py
```
- **Input**: `game\a&b_space_mod.swf` (preferred) or `game\a&b_space.swf`
- **Output**: every standalone renderable in `docs\research\asset-manifest.json` (742 symbols):
  parts, mounts, bullets, pickups, backgrounds, UI screens, help pages, widgets,
  locks and the cursor under `A&B-U\Assets\Art\`.
- **Data**: 8 original fonts under `A&B-U\Assets\Art\Fonts`, 3 unnamed UI sounds
  under `A&B-U\Assets\Resources\Audio\Embedded`, and the 421-tag string table at
  `docs\assets\text\string-table.json`.
- **Manifest**: `docs\assets\extraction-manifest.json` (counts, pivots, button states,
  nine-slice borders, ambiguities).
- **Report**: `docs\assets\extraction-report.md`.
- **References**: 32 full-page renders under `docs\assets\reference\`.
- **Contact sheets**: seven PNGs under `docs\assets\contact-sheets\`.
- **Unity**: applies PPU 1, per-symbol pivots, nine-slice borders and builds the
  category atlases via `ABRevival.Editor.FullAssetExtractionImporter.ImportBatch`.
- **Tools**: JPEXS FFDec CLI, Unity 6.6.8f1 batchmode and Pillow (`pip install pillow`).
- **Dry run**: `python scripts\export_all_assets.py --skip-unity` runs only FFDec and
  rewrites the manifest/report without touching Unity import settings.

### 11. `package_android_apk.bat`
Generates a self-signed developer certificate and packages the modified SWF into an installable Android APK (`dist\AssemblyAndBattle.apk`) via ADT:
```cmd
scripts\package_android_apk.bat
```

### 12. `validate_vendored_skills.py`
Validates `.agents/skills/` provenance, licenses and lockfile; runs from pre-commit when the tree changes:
```cmd
python scripts\validate_vendored_skills.py
python scripts\test_validate_vendored_skills.py
```

