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

### 3. `install-hooks.bat` / `install-hooks.sh`
Configures git to use `.githooks/` for pre-commit verification:
```cmd
scripts\install-hooks.bat
```

### 4. `lint_domain_leakage.py`
Static architecture linter preventing domain identity leakage and magic numbers in UI controllers:
```cmd
python scripts\lint_domain_leakage.py [--staged]
```

### 5. `as3_search.py`
High-speed ActionScript 3 symbol and callers search CLI for agents and developers:
```cmd
python scripts\as3_search.py symbol <name>
python scripts\as3_search.py callers <method>
python scripts\as3_search.py class <ClassName>
```

### 6. `run-game.bat` (Root)
Launches the game directly in standalone Flash Player 32.

### 7. `run_air.bat`
Launches the game natively via Adobe AIR Debug Launcher (ADL) with Stage3D direct hardware acceleration enabled:
```cmd
scripts\run_air.bat
```

### 8. `package_android_apk.bat`
Generates a self-signed developer certificate and packages the modified SWF into an installable Android APK (`dist\AssemblyAndBattle.apk`) via ADT:
```cmd
scripts\package_android_apk.bat
```

