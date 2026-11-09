# Full Asset Inventory — Frozen Flash Build

Research ticket: **#173**. Scope: every character-defining asset tag in the frozen
Flash build, classified once, with the exact symbol ids, native sizes, pivots,
composite/standalone status, intended Unity path, gaps, and a reproducible
extraction pipeline.

This is a research artifact. No runtime, UI, gameplay or `.csproj`/`.asmdef`
change is part of this ticket.

## 1. Sources and method

| Source | Role | Evidence |
|---|---|---|
| `game/a&b_space.swf` (8,999,085 B) | Shipped loader. It contains **no art**; only 12 `DefineBinaryData` payloads, the ABC bootstrap and a `VeryOutShitABC` loading symbol. | `ffdec-cli -dumpSWF game/a&b_space.swf` = 26 top-level tags |
| `src/assets/binaryData/4_all.AllData_data.bin` | **Part / combat art library** (SWF, 181,250 B compressed payload). | 844 characters |
| `src/assets/binaryData/9_all.AllData_ui.bin` | **UI / HUD / fonts / cinematics library** (SWF, 485,377 B payload). | 917 characters |
| `src/assets/binaryData/6_all.AllData_sound.bin` | **Sound bank** (SWF, 8,151,498 B payload). | 33 `DefineSound` tags |
| `src/assets/binaryData/5_all.AllData_talk.bin` | Dialogue XML. | 388 lines, 13 speakers |
| `src/assets/binaryData/8_all.AllData_defPart.bin` | Part catalogue. | 271 `partOne` rows |
| `src/assets/binaryData/11_all.AllData_defBullet.bin` | Bullet catalogue. | 65 `bulletOne` rows |
| `src/assets/binaryData/12_all.AllData_movie.bin` | Cinematic order (Mov1…Mov9). | 9 rows |
| `src/as3/scripts/` | Frozen AS3 reference; used to resolve runtime meaning and playback sites. | ADR 0015 read-only baseline |

The main SWF's `DefineBinaryData` character ids are stable and were verified
against the AS3 `AllData` embed classes:

`1` loading symbol, `2` defFire XML, `3` defFly (zlib AMF), `4` part/data SWF,
`5` talk XML, `6` sound SWF, `7` startFly (zlib AMF), `8` defPart XML,
`9` UI SWF, `10` levels (zlib AMF), `11` defBullet XML, `12` movie XML.

> **Source note:** `game/a&b_space_mod.swf` is not present in this worktree
> (it is a generated/ignored output). The research used the documented fallback
> `game/a&b_space.swf` and extracted the embedded libraries directly from
> `src/assets/binaryData/`. The payload ids above were confirmed before using
> them; no symbol ids were inferred from the main SWF.

Verification commands used for every count in this document:

```powershell
& "C:\Program Files (x86)\FFDec\ffdec-cli.exe" -dumpSWF <swf>
& "C:\Program Files (x86)\FFDec\ffdec-cli.exe" -swf2xml <swf> <xml>
& "C:\Program Files (x86)\FFDec\ffdec-cli.exe" -export symbolClass <dir> <swf>
& "C:\Program Files (x86)\FFDec\ffdec-cli.exe" -selectid <ids> -format sprite:png -zoom 1.0 -ignorebackground -export sprite <dir> <swf>
```

Symbol classes were **not** guessed from filenames. Every named symbol id below
comes from `symbols.csv` produced by `-export symbolClass`; every unnamed child
comes from the `-swf2xml` placement tree.

## 2. Headline counts

| Measure | Count |
|---|---:|
| Character-defining tags inventoried | **1,794** |
| Manifest rows (adds the procedural `normal` bullet) | **1,795** |
| Data-library characters | **844** |
| UI-library characters | **917** |
| Sound-library characters | **33** |
| `SymbolClass`-attachable symbols (data 271 + UI 44 + audio 33) | **348** |
| Composite-only descendants (data 541 + UI 321) | **862** |
| Standalone-renderable but unnamed (data 32 + UI 552 + procedural normal) | **585** |
| Already extracted to `A&B-U/Assets` | **322** (227 data + 62 UI + 33 audio) |
| Not yet extracted but technically extractable | **420** (manifest `extractable=true`, `status != extracted`) |

Tag breakdown:

| Library | Sprites | Shapes | Static text | Dynamic text | Morph | Bitmap | Font | Sound |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `4_all` data | 557 | 255 | 31 | 0 | 0 | 0 | 1 | 0 |
| `9_all` UI | 250 | 255 | 199 | 191 | 8 | 4 | 7 | 3 |
| `6_all` sound | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 33 |
| **Total** | **807** | **510** | **230** | **191** | **8** | **4** | **8** | **36** |

`-dumpSWF` also reports non-character metadata tags (`DefineFontAlignZones`,
`CSMSettings`, `DefineSceneAndFrameLabelData`, `Metadata`, `FileAttributes`).
They are not assets and are intentionally not manifest rows.

Artifacts:

- `docs/research/asset-manifest.json` — every symbol with classification, parents,
  native size, pivot, intended Unity path, status.
- `docs/research/asset-manifest.csv` — same rows for spreadsheet/diff workflows.
- `docs/assets/contact-sheets/ui-screens.png`
- `docs/assets/contact-sheets/ui-widgets.png`
- `docs/assets/contact-sheets/parts-thumbnails.png`
- `docs/assets/contact-sheets/part-mounts.png`
- `docs/assets/contact-sheets/combat-objects.png`
- `docs/assets/contact-sheets/backgrounds.png`

## 3. UI screens and HUD

### 3.1 Pages and full-screen symbols (21)

All UI pages are single-frame `DefineSprite` composites on a **640×480** library
stage. Some content bounds exceed 640×480 because off-stage rows exist in the
symbol (table rows are moved into view at runtime). The FFDec render includes
those rows; the game clips them. Native sizes below are the raw content bounds,
not the visible 640×480 viewport.

| Screen | Class | Id | Frames | Content size (px) | Unity section | Extracted |
|---|---|---:|---:|---|---|---|
| Main menu | `UI_Main` | 459 | 1 | 641×506 | Main menu | no (reference capture missing) |
| Center | `UI_ChooseFlyAI` | 608 | 1 | 640×480 | Center | `ui_0608_reference_mission_select_page.png` |
| Factory | `UI_MakePlayer` | 440 | 1 | 640×490 | Factory | `ui_0440_reference_hangar_page.png` |
| Shop | `UI_Shop` | 205 | 1 | 640×480 | Shop | `ui_0205_reference_shop_page.png` |
| Info/Help shell | `UI_Help` | 467 | 1 | 640×480 | Info | no |
| Mission briefing | `UI_Mission` | 300 | 1 | 640×480 | Ready | no |
| Map loading | `UI_MapBuilding` | 311 | 1 | 641×500 | Ready | no |
| Movie overlay | `UI_Movie` | 284 | 1 | 654×490 | Ready | no |
| Mission marker | `MissionMark` | 4 | 1 | 20×20 | Ready | no |
| Combat HUD | `UI_Game` | 510 | 1 | 654×490 | Combat | `ui_0510_reference_hud_page.png` |
| Pause | `UI_Pause` | 251 | 1 | 640×480 | Combat | no |
| Dialogue | `UI_Talk` | 67 | 1 | 654×490 | Combat | no |
| Dialogue (wide) | `UI_TalkX` | 66 | 1 | 654×490 | Combat | no |
| Options | `UI_Options` | 281 | 1 | 640×480 | Options | no |
| Victory | `UI_Win` | 30 | 1 | 640×480 | Results | no |
| Defeat | `UI_Fail` | 519 | 1 | 640×480 | Results | no |
| Campaign complete | `UI_AllWin` | 617 | 1 | 640×480 | Results | no |
| Mode-X results | `UI_ModeX` | 293 | 1 | 640×480 | Results | no |
| Repair | `UI_Repair` | 226 | 1 | 640×480 | Results | no |
| Confirm dialog | `UI_Really` | 238 | 1 | 640×480 | System | no |
| Save/load | `UI_Data` | 534 | 1 | 640×480 | Info | no |

### 3.2 Help pages (11 symbols + shell)

`UI_Help` (467) is only the metal shell: five static text tags and a BACK button
(466). Each help topic is a separate composite attached by `UIHelpX2`:

| Page | Class | Id | Body symbol | Title field |
|---|---|---:|---:|---:|
| Build a craft | `UI_HelpBuildCraft` | 899 | 897 | 898 |
| Build one first | `UI_HelpBuildOneFirst` | 861 | 859 | 860 |
| Choose craft | `UI_HelpChooseCraft` | 856 | 854 | 855 |
| Choose craft AI | `UI_HelpChooseCraftAI` | 852 | 850 | 851 |
| Edit | `UI_HelpEdit` | 829 | 827 | 828 |
| How to play | `UI_HelpGame` | 824 | 822 | 823 |
| How to play 2 | `UI_HelpGame2` | 794 | 792 | 793 |
| One second | `UI_HelpOneSec` | 758 | 756 | 757 |
| Repair | `UI_HelpRepair` | 750 | 748 | 749 |
| Shop | `UI_HelpShop` | 744 | 737 | 738 |
| Break | `UI_HelpBreak` | 905 | 903 | 904 |

All eleven are 640×480 composites and share `$big` (466), `$skip` (743) and the
page arrows (114/119). They are **not** extracted today.

### 3.3 Cinematics

`Mov1`…`Mov9` are `DefineSprite` symbols in the UI library, ordered by
`12_all.AllData_movie.bin`. All nine already exist as
`A&B-U/Assets/Resources/Cinematics/MovN.mp4` (1920×1080 H.264). Ids:
`Mov1 717`, `Mov2 708`, `Mov3 704`, `Mov4 685`, `Mov5 681`, `Mov6 669`,
`Mov7 657`, `Mov8 647`, `Mov9 633`.

### 3.4 Cursor

`MouseOut` (917, 44×26, pivot 13,13) is the in-game cursor/reticle. `UIGame`
hides the OS cursor (`Mouse.hide()`) and attaches this symbol. It is already
extracted as `ui_0917_hud_reticle.png`. The custom pivot matters: the hotspot is
at (13,13), not the texture centre.

### 3.5 HUD composition

`UI_Game` (510) direct children:

| Role | Instance | Symbol | Notes |
|---|---|---:|---|
| Portrait plate | child | 61 | reused by every dialogue/HUD screen |
| Minimap | `map` | 469 | 170×176 cluster |
| Radar | `dir` | 476 | contains `txtDist` 471, `arrow` 474 (71 frames), `txtTime` 475 |
| Top-left plate | child | 477 | only HUD frame that exists |
| Player + captain tile | `life` | 489 | `pic` 61, `face` 56, `txtName` 479, `hp` 483, `powb` 485, `pow` 487 |
| Wingman tiles | `fly1` / `fly2` | 492 | share one tile symbol; `face` 56, `hp` 491 |
| AI count | `ai` | 495 | `txtNum` 494 |
| Kill counter | `km` | 499 | |
| Money text | `money` | 500 | dynamic TextField |
| Pause button | `$pause` | 505 | 3-frame button |
| Help button | `$help` | 73 | 3-frame button |
| Standby badge | `stand` | 509 | 161 frames |
| Skip | `$skip` | 65 | 3-frame button |

Battle-damage telemetry, tactical-mode badge, camera-distance badge and captain
alert are **not** symbols: they are runtime-created `TextField`/`Shape` objects
in `UIGame.as`. The revival currently reuses plates 481/493 for them; that is a
deliberate fallback, not original art.

## 4. Ships and parts

### 4.1 Catalogue vs art symbols

| Measure | Count |
|---|---:|
| `defPart` catalogue rows | **271** |
| Unique `mcName` art symbols | **222** |
| Body | 71 |
| Wing | 49 symbols / 98 catalogue rows |
| Gun | 62 |
| Engine (`Pow`) | 40 |
| Left/right catalogue variants (`lr=0/1`) | 49 + 49, all Wing |
| Shared symbols (`lr=2`) | 173 |
| Unique part art shapes | 220 (two shapes are shared, see §7) |

Every Wing symbol is used twice: one `lr=0` and one `lr=1` catalogue row point
at the same SWF symbol; the runtime mirrors it with `scaleX = -1`
(`MC2F4Data.as`). There is no separate mirrored art. This is why 98 wing rows
produce only 49 mount/thumbnail files.

### 4.2 Mount art vs thumbnail vs editor icon

There is **one** art symbol per part, used for three different presentations:

| Presentation | Source | Zoom | Orientation | Current output |
|---|---|---:|---|---|
| Shop/table thumbnail | Part wrapper, markers stripped | 1× | as drawn (nose-east) | `Assets/Art/Sprites/Parts/part_<id>_<mc>.png` |
| In-game mount art | Same wrapper, markers stripped | 2× | rotated 90° CCW (nose-up) | `Assets/Art/Sprites/PartMounts/part_<id>_<mc>.png` |
| Editor preview | Same wrapper, rendered inside `showFly`/`formFly` at runtime | live | live rotation | no separate icon; runtime uses the mount art |

There are **no dedicated editor icons** and **no separate thumbnail symbols**.
The 222 part wraps produce 444 PNG files (222 thumbnails + 222 mounts) from 222
source symbols. The wrapper is a composite:

```text
Body_1 (680, DefineSprite, 1 frame)
├─ RECT (2)      -> shared 50×50 hit rectangle (shape 1)
├─ SHOW (679)    -> art + markers
│  ├─ <unnamed>  -> 678, the actual 12×20 px body art
│  ├─ H_2_234    -> socket marker sprite 5
│  ├─ H_0_1234   -> socket marker sprite 5
│  ├─ H_1_1234   -> socket marker sprite 5
│  └─ F          -> muzzle marker sprite 5
└─ HIT (2)       -> same shared hit rectangle
```

The 2× mount canvas is the **wrapper** canvas, which includes the invisible RECT
bounds; the visible art is smaller and centred. Do not crop the mount canvas to
visible pixels without re-deriving the pivot.

### 4.3 Marker layers (`H` / `L` / `F` / `P`)

Marker grammar is implemented in `PartMountExtractor` and traced to
`MC2F4Data.readMcData`:

- `H_<lr>_<types>` — socket. `lr` 0 left, 1 right, 2 both. `types` is a digit
  list of accepted part categories.
- `F` — muzzle/fire point.
- `L` — child link point.
- `P` — engine exhaust point.

Totals across the 222 part symbols:

| Marker | Count | Symbols missing it |
|---|---:|---:|
| `H` sockets | **749** | 102 (all 62 Guns have 0 sockets) |
| `F` muzzles | **206** | 89 |
| `L` link points | **151** | 71 |
| `P` exhaust points | **40** | 182 (exactly the 40 `Pow_*` engines have one) |

Per category:

| Category | Parts | Sockets | Max sockets | Muzzles | Max muzzles |
|---|---:|---:|---:|---:|---:|
| Body | 71 | 614 | 20 | 124 | 3 |
| Wing | 49 | 135 | 5 | 0 | 0 |
| Gun | 62 | 0 | 0 | 82 | 5 |
| Engine | 40 | 0 | 0 | 0 | 0 |

Missing markers are not errors: the runtime has a documented category fallback
(wings outboard, engines aft, guns forward). The research finding is that
**102 of 222 parts cannot be placed from sockets alone**; the fallback is part
of the port, not an edge case. Marker coordinates are twips, Y-down; the
extractor converts once to stage pixels, Y-up.

### 4.4 Catalog data notes

- Every part has `deadAct=no`; no part has an independent death animation.
- `type` mapping: 0 Body (71), 1 Wing (98), 2 Engine (40), 3 Gun (62).
- 49 Wing symbols serve two catalogue ids each. `PartMounts.txt` emits one row
  per catalogue id, so it has 271 `P;` rows but only 222 unique art files.

## 5. Combat art

### 5.1 Bullets

| Measure | Count |
|---|---:|
| `defBullet` rows | **65** |
| Rows using procedural `normal` | **57** |
| Rows using the four `Blt_*` symbols | **8** |
| Bullet art symbols | **4**: `Blt_1` 467, `Blt_2` 464, `Blt_3` 461, `Blt_4` 458 |
| Special procedural bullet | `normal` — `GData.packBulletDisc("normal", 8, 0x00CCFF)` |

All four `Blt_*` symbols are extracted (`bullet_Blt_1..4.png`) plus the baked
`bullet_normal.png`. `Blt_*` wrappers follow the same RECT/SHOW/HIT pattern as
parts and use the `P` marker (458 → SHOW 457 → `P`). There is no trail symbol:
trails are drawn procedurally as coloured line/quad particles.

### 5.2 Pickups

| Data key | Symbol | Id | Frames | Extracted |
|---|---|---:|---:|---|
| `fuel` | `It1` | 685 | 1 | no |
| `money` | `It2` | 697 | 1 | no |
| `life` | `It3` | 693 | 1 | no |
| `part` | `It4` | 689 | 1 | no |

Mapping is explicit in `GData.initData()`, not inferred from filenames. `Item.beHit`
plays sound ids 8/10/9/11 for fuel/life/money/part respectively.

### 5.3 Backgrounds and starfields

| Group | Count | Symbols | Meaning |
|---|---:|---|---|
| Starfield tiles | 4 | `BG1`, `BG2`, `BG3`, `BG4` | seamless/scrollable nebula and star tiles |
| Planet / prop composites | 23 | `BGU1`…`BGU14` plus named `copy` variants | individual planets, debris, nebula props |
| Credits / thank-you cards | 14 | `BGWord0`…`BGWord12` plus `BG50` | end-credit panels; `BG50` is a 640×1999 scroll |
| **Total** | **41** | | |

The 41 symbols contain 33 background-only shapes and 284 internal sprites.
They are composite art: extracting a single shape is rarely useful. Parallax
speed, repeat count and level assignment live in level data
(`10_all.AllData_levels.bin`), not in the symbol.

### 5.4 Effects and particles

There are **no explosion, smoke, spark, trail or particle sprites** in either
SWF. `Particle.as` draws coloured rectangles/lines and `Item`/`DeadPart` animate
existing bitmap data. Any Unity VFX for explosions, trails or debris is a
reimplementation, not an extraction. This is the single largest art gap.

## 6. Audio

### 6.1 Original sound bank

`-export symbolClass` on `6_all.AllData_sound.bin` returns **33** named clips:
11 music (`M_*`) and 22 effects (`S_*`). `SoundData.initArray()` fixes the
runtime array order:

- Music array ids **0–10**: `M_Main`, `M_Menu`, `M_Game1`…`M_Game7`, `M_Creidts`, `M_Win2`.
- Effect array ids **0–21**: `S_Fire100`, `S_FireGT`, `S_FireL1`, `S_FireL2`,
  `S_Bomb`, `S_Hit`, `S_Pow`, `S_Rocket`, `S_Fuel`, `S_Money`, `S_Repair`,
  `S_Weapon`, `S_Zin`, `S_Cant`, `S_Check`, `S_Word`, `S_Zout`, `S_Yes`,
  `S_No`, `S_Start`, `S_Eng`, then the bank-only `S_Click` appended as 21.
- `S_Hit`, `S_Fuel`, `S_Click` have no AS3 call site. `S_Fuel` is a **byte-identical
  duplicate of `S_Start`** (SHA-256 `10EB947A…`), which explains the unused row.
- All 33 clips are already extracted to `A&B-U/Assets/Resources/Audio/Original/`
  and mapped by `docs/audio/sound-bank-inventory.md`.

Playback sites (music and effects) are already documented per clip in
`docs/audio/sound-bank-inventory.md`; that table is the authority and is not
duplicated here.

### 6.2 Extra sounds hidden in the UI library

The UI SWF contains **3 unnamed `DefineSound` tags** that are not in the named
sound bank and are not referenced by `SoundData`:

| Id | Exported size | Status |
|---:|---:|---|
| 637 | 21,190 B | unclassified, not extracted |
| 640 | 18,330 B | unclassified, not extracted |
| 677 | 7,800 B | unclassified, not extracted |

They are byte-distinct from all 33 bank clips. Probably legacy menu/voice
leftovers; listen and either map or drop them. They are in the manifest as
`ui.sound.embedded`.

## 7. Fonts, text and localisation

### 7.1 Embedded fonts (8 font tags, 5 families)

| Library | Font id | Family | Glyphs |
|---|---:|---|---:|
| UI | 2 | `_sans` | 115 |
| UI | 478 | `_sans` | 114 |
| UI | 7 | SF TransRobotics | 115 |
| UI | 627 | SF TransRobotics | 114 |
| Data | 701 | SF TransRobotics | 51 |
| UI | 81 | Arial | 114 |
| UI | 496 | SF Zero Gravity | 1 |
| UI | 595 | Times New Roman | 2 |

FFDec can export each as a `.ttf` (`-export font`). Licensing is the blocker:
Arial and Times New Roman are commercial fonts embedded as subsets; do not
redistribute the exported TTF. `SF TransRobotics`/`SF Zero Gravity` are
ShyFonts-era freeware of unknown current licence. Recommendation: reproduce
the UI with one free condensed sans and keep the original TTFs only as
reference, not as shipped assets.

### 7.2 Text tags and strings

| Measure | Count |
|---|---:|
| Text tags total | **421** |
| Data static text | 31 |
| UI static text | 199 |
| UI dynamic (`DefineEditText`) | 191 |
| Unique non-empty UI strings | **219** |
| Dialogue lines | **388** |
| Dialogue speakers | **13** |

`ffdec-cli -export text` wrote 390 UI text files; 388 are non-empty. Strings are
inline English; there is **no localisation table** in the SWF. `Talk` strings
live in `5_all.AllData_talk.bin` and are already parsed by `TalkScriptParser`.
The placeholder `Tip12: asdsadjsladjasd.` appears four times and should not be
ported verbatim.

Dynamic text is not art. The manifest marks all 191 edit-text tags
`status=unavailable, extractable=false` with the note that their content is
recoverable from the text export. They need a Unity text implementation, not a
sprite.

## 8. Composite vs standalone map

| Source family | Standalone? | Mapping |
|---|---|---|
| Part wrappers (`Body_*`, `Wing_*`, `Gun_*`, `Pow_*`) | Yes, by class name | 1 symbol → thumbnail (1×) + mount (2×); wrapper contains RECT art + SHOW art/markers + HIT |
| Bullet wrappers (`Blt_*`) | Yes | 1 symbol → 1 bullet PNG; wrapper contains RECT/SHOW/HIT |
| Pickups (`It1..4`) | Yes | 1 symbol → 1 animated/static PNG |
| Backgrounds (`BG*`, `BGU*`, `BGWord*`) | Composite | Export whole symbol; internal shapes are `composite-only` |
| UI pages (`UI_*`) | Composite | Export whole 640×480 page; children are separate widgets |
| UI widgets (139 nameless but named-in-parent sprites) | Renderable standalone | Export each by id; shared across screens |
| UI internal sprites/shapes (330) | Composite-only | Only meaningful as part of a parent; do not ship as individual assets |
| Text/font/bitmap/sound tags | Standalone data | Export text/font/bitmap/sound, not as part of the parent image |
| Cinematics (`Mov1..9`) | Yes | Already rendered to MP4 |

### 8.1 Most-shared UI widgets

These are reused by many screens; extracting them once prevents per-screen
duplication. `parents` in the manifest has the full list.

| Widget | Id | Screens using it |
|---|---:|---:|
| Metal page shell | 34 | 19 |
| Page shell variant | 33 | 19 |
| Shared 50×50 rect / HIT | 1 | 15 |
| Page arrows / filters / sort group (109–114) | 109–114 | 15 each |
| Help back button | 466 | 12 |
| Pause / menu button family (9, 10) | 9, 10 | 11 each |
| Help skip group (739–743) | 739–743 | 11 each |
| Portrait backing | 61 | 9 |
| Result navigation buttons (17, 18) | 17, 18 | 9 each |
| Movie overlays (618, 629) | 618, 629 | 9 |

## 9. Native size and pivot findings

Native size is the content bounding box in stage pixels, computed from the
`-swf2xml` placement tree (twips ÷ 20), not from filename or rendered canvas
alone. Pivot is the registration point relative to the content bounds,
expressed in Unity texture coordinates (bottom-left origin).

Key results:

- **All 222 part wrappers are exactly centred** (`pivotNormalized = 0.5, 0.5`).
  The existing centre-pivot import contract is correct for parts and mounts.
- **Full-screen UI pages are not centred.** Their registration point is the
  stage origin (0,0), so the correct sprite pivot is effectively top-left
  (`pivotNormalized ≈ 0, 1`). `UI_Main`/`UI_MapBuilding`/`UI_Game` have
  off-stage content and therefore non-round bounds.
- **Widget pivots vary**; use the per-symbol values in the manifest. Example:
  `MouseOut` pivot is (13,13) on a 44×26 image; a centre pivot moves the
  reticle hotspot.
- The **mount PNG canvas includes the invisible RECT bounds**, so its content
  bounds are larger than the visible art. Cropping to alpha changes the pivot.
- The UI library stage is 640×480 at 50 fps; the data library stage is
  1000×400 at 24 fps. Neither is the runtime 1920×1080 canvas; scaling is done
  by the game (`RenderData`), not baked into these symbols.

## 10. Menu section → exact symbols

The tables list direct named children only. Nested rows/forms are described
after each table. Full descendant ids are in `asset-manifest.json` (`parents`
field) and in the contact sheets.

### 10.1 Main menu — `UI_Main` (459)

| Element | Symbol |
|---|---:|
| Backdrop shape / backdrop sprite | 442, 444 |
| PLAY | 447 |
| OPTIONS | 242 |
| DATA | 450 |
| CREDITS | 453 |
| Link | 20 |
| `word1` / `word2` text | 454, 455 |
| PLAYER | 458 |

### 10.2 Center — `UI_ChooseFlyAI` (608)

| Element | Symbol |
|---|---:|
| Background shape | 535 |
| Craft table (`form`) | 564 |
| AI pad (`aipad`) | 579 |
| Craft preview (`showFly`) | 581 |
| Stat card (`showDetail`) | 425 |
| MISSION | 582 |
| READY | 585 |
| FACTORY | 588 |
| SHOP | 591 |
| BACK | 299 |
| Help | 73 |
| Level text / battle text | 596, 597 |
| New-shop badge | 162 |
| Lock overlays | 601 (`LockUP_123`), 606 (`LockDOWN_125`) |

`form` (564) contains two row symbols, 547 and 553, each with `index`,
`nameSelf`, `player`, `ai`, `$bai`; eight rows reuse those two symbols, plus
`txtPage` 555, arrows 114/119, and sort buttons 557/559/561/563.

### 10.3 Factory — `UI_MakePlayer` (440)

| Element | Symbol |
|---|---:|
| Portrait mask | 61 |
| Background shape | 312 |
| Craft preview (`showFly`) | 315 |
| Zoom out / in | 318, 321 |
| Fly selector (`$fly`) | 324 |
| Parts table (`formPart`) | 368 |
| Part detail card (`dt`) | 194 |
| Stats card (`dtTemp`) | 384 |
| Fly table (`formFly`) | 409 |
| Detail card (`showDetail`) | 425 |
| New-name text | 426 |
| Help | 73 |
| Shape 427 / tabs `$chooseu0..11` | 427, 428–439 |

`formPart` (368) uses rows 335/345 with fields `index`, `type`, `hp`,
`turnfly`, `powfly`, `nameSelf`, `lr`, `mass`, `fire`; sort 140–158 and 348/350;
filters 124–136; `$add` 356, `$new` 361, `$merge` 364, `$flyF` 367.
`formFly` (409) uses rows 396/399, `$edit` 392, `$break` 395, `$part` 404,
`$add` 356, filters and page arrows.

### 10.4 Shop — `UI_Shop` (205)

| Element | Symbol |
|---|---:|
| Background shapes | 68, 74; text 69 |
| Help | 73 |
| Buy form (`form1`) | 163 |
| Sell form (`form2`) | 163 |
| RESET | 167 |
| ACCEPT | 170 |
| FLY | 173 |
| Detail cards (`dt1`, `dt2`) | 194 |
| Money now / last | 196, 198 |
| INVERT | 204 |

`form1`/`form2` (163) use row symbols 93 and 106 with fields `index`, `type`,
`hp`, `turnfly`, `powfly`, `nameSelf`, `lr`, `mass`, `fire`, `value`, `sel`;
filters 124–136, sort 140–160, arrows 114/119, page text 108, new badges 162.

### 10.5 Info / Help — `UI_Help` (467) + 11 pages

`UI_Help` shell: shape 460, static text 461–465, BACK 466.
Each topic page uses the table in §3.2 and shares `$big` 466, `$skip` 743,
`$prev`/`$next` 114. `UI_Data` (534) is also reachable from the main menu:
CLEAN 524, SAVE 528, LOAD 532, BACK 299, background shape 520, text 533.

### 10.6 Ready — mission briefing and launch

| Element | Symbol |
|---|---:|
| Mission briefing page | `UI_Mission` 300; title 295, words 296, OK 299 |
| Map loading page | `UI_MapBuilding` 311; loader 306, title 307, words 308, OK 310 |
| Movie overlay | `UI_Movie` 284; portrait backing 61, SKIP 283 |
| Mission marker | `MissionMark` 4; shape 1, text 3 |
| Cinematics | `Mov1`…`Mov9` 717/708/704/685/681/669/657/647/633 |
| Launch action | READY 585 (no LAUNCH art exists) |

### 10.7 Combat — `UI_Game` (510)

See §3.5 for the HUD table. Overlays:

| Overlay | Symbol |
|---|---:|
| Pause page | `UI_Pause` 251; shell 34/239, OPTION 242, MISSION 245, RESTART 246, RESUME 249, text 250 |
| Dialogue | `UI_Talk` 67; talk 59 (`face` 56, `txtName` 57, `txtWord` 58), BIG 61, SKIP 65 |
| Dialogue wide | `UI_TalkX` 66; same talk 59, plus 32/34 |
| Cursor | `MouseOut` 917 |
| Help | 73 |
| Skip | 65 |

### 10.8 Results — win / fail / repair

| Page | Symbol | Direct children |
|---|---:|---|
| Victory | `UI_Win` 30 | shape 6, text 8, NEXT 13, REPAIR 16, link 20, texts 21–25, RESTART 28, tip 29 |
| Defeat | `UI_Fail` 519 | shapes/text 512–514, MAIN 299, RETRY 517, link 20, tip 518 |
| Campaign complete | `UI_AllWin` 617 | shapes/text 609–611, SEE 614, link 20, RESTART 615, REPAIR 16, tip 616 |
| Mode-X | `UI_ModeX` 293 | shape 285, NEXT 13, REPAIR 16, link 20, texts 286–292 |
| Repair | `UI_Repair` 226 | shape 206, form 209, detail 194, shape 210, text 211, HELP 73, money 212/217, EXIT 215, RESET 167, ACCEPT 216, text 218–221, ALL 225 |
| Confirm | `UI_Really` 238 | shell 34/227, title 228, input 229, word 230, YES 233, NO 236, names 237 |

`UI_Repair.form1` (209) uses the same row/arrow/filter/sort vocabulary as Shop:
rows 93/106, page text 208, arrows 114/119, filters 124–136, sort 140–160.

## 11. Duplicates, reuse and naming traps

1. **Wing mirror variants**: 49 symbols serve 98 catalogue rows (`lr=0/1`).
   Never produce 98 art files; mirror in Unity.
2. **Part thumbnails vs mounts**: 222 symbols → 444 files. They are two renders
   of one source, not two art assets.
3. **Shared part art shapes**: `Wing_17` and `Wing_19` both point at shape 95;
   `Pow_39` and `Gun_22` both point at shape 154. Four names, two drawings.
   Check whether this is intentional reuse or a 2011 copy/paste bug before
   treating them as distinct.
4. **`BG*` copy names are not duplicates**: `BGU7copy`…`copy5`,
   `BGU11copy`…`copy4`, `BGU12copy/copy2`, `BGU14copy`, `BGU1copy` all render
   distinct pixels (0 exact PNG hash matches among 271 data symbols).
5. **Audio duplicate**: `S_Fuel` is byte-identical to `S_Start`; `S_Fuel` has no
   call site. Keep one clip.
6. **UI widget reuse**: 262 UI characters have more than one screen ancestor;
   the top shared shell/arrow/help widgets appear in 11–19 screens. Extract by
   id, not per screen.
7. **UI manifest name collisions**: the existing 59-entry manifest maps two
   logical names to ids 477, 481 and 493 (three ids, six names), so only 56
   files exist. Do not assume 59 files.
8. **No exact duplicate UI renders**: 0 hash matches across the 44 named UI
   symbols. Visual similarity is not duplication.

## 12. Gaps and manual-slicing work

### 12.1 Missing art (cannot be extracted)

| Gap | Why | Owner |
|---|---|---|
| Explosions, smoke, sparks, trails, debris | Procedural `Particle`/`Render` code; no SWF symbols | Unity VFX implementation |
| Battle-damage telemetry row, tactical-mode badge, camera-distance badge, captain alert | Runtime `TextField`/`Shape` added by `UIGame.as`; no original art | Revival UI design (current plates are fallbacks) |
| `normal` bullet colour variants | `packBulletDisc` is code; only one baked cyan disc exists | Shader/tint in Unity |
| AI role dropdown / companion role labels | Runtime-created in `UIChooseFlyAI.as`; only base row art exists | Revival UI design |
| Mobile/desktop hover states | Flash uses frame 1/2/3; mapping is inferred, not documented | Manual state mapping |

### 12.2 Manual slicing / judgement

1. **93 stateful UI widgets**: 74 have 3 frames (likely up/over/down), 19 have
   2 frames. FFDec exports all frames; choosing which frame is which state must
   be done by eye or by tracing `BtnBase`. Start from `ui.base.BtnBase`.
2. **Full-screen page layers**: pages such as `UI_Shop` (shape 68/74 + text 69)
   are flattened into one 640×480 shape. If Unity needs separate backdrop,
   chrome and content layers, those layers must be cut manually or the page
   must be rebuilt from its named children.
3. **Off-stage rows**: `UI_ChooseFlyAI` renders 939×510 because eight table rows
   sit outside the 640×480 stage. Crop to the 640×480 viewport and build the
   list as a Unity scroll view; do not ship the giant canvas.
4. **Morph shapes**: 8 UI morph shapes need either a still frame or vector
   re-creation; FFDec PNG export only gives discrete frames.
5. **Parallax backgrounds**: `BG*`/`BGU*` symbols are single tiles/props. Repeat
   count, scroll speed and level assignment are in `10_all.AllData_levels.bin`;
   the level-to-background map must be extracted from level data, not art.
6. **Fonts**: 8 tags, 5 families, two commercial families. Replace with a
   licensed font or keep only glyphs used by the 219 strings.
7. **UI audio 637/640/677**: listen, identify, then map or drop.
8. **4 UI bitmaps** (5, 441, 511, 620) are full-screen JPEG backdrops placed on
   the UI timeline; their parent screen is not recoverable from the symbol tree.
   Match visually to Main/Win/Fail/credits before wiring. They are extracted.
9. **Pivots**: re-import with per-symbol pivots from the manifest. The current
   uniform centre pivot is correct for parts but wrong for full-screen pages
   and the cursor.
10. **Hitboxes**: every part wrapper shares the same 50×50 RECT/HIT (shape 1/2).
    It is not a per-part collision shape. Physics must use per-part bounds or
    authored colliders, not this rectangle.

## 13. Recommended one-command extraction pipeline

Target command (to be implemented by a follow-up ticket, not this one):

```powershell
python scripts\export_all_assets.py --swf "game\a&b_space.swf" --out "build\assets"
```

The script must perform exactly these steps, in this order:

### Step 1 — unpack the loader

```powershell
& $ffdec -export binaryData build\bin game\a&b_space.swf
```

Resolve `4_all.AllData_data.bin`, `9_all.AllData_ui.bin`,
`6_all.AllData_sound.bin` from `build\bin`; never assume the numeric prefixes.
`-dumpSWF` must confirm the `DefineBinaryData` ids 4/6/9 before continuing.

### Step 2 — make a marker-free render SWF per library

```powershell
& $ffdec -swf2xml $lib build\tmp\lib.xml
# remove every placement whose name matches ^[LHPF] except HIT
# (support PlaceObject2Tag, PlaceObject3Tag and PlaceObject4Tag)
& $ffdec -xml2swf build\tmp\lib-clean.xml build\tmp\lib-clean.swf
```

The current `SpriteExportPipeline` regex only removes `PlaceObject2Tag`. Extend
it to 3/4 or the pipeline will silently bake marker dots into new art.

### Step 3 — symbol table

```powershell
& $ffdec -export symbolClass build\tmp\symbols build\tmp\lib-clean.swf
```

`symbols.csv` is the authority for `mcName → character id`. Never infer ids
from file order or naming patterns.

### Step 4 — render

```powershell
# thumbnails 1x, no markers, no background
& $ffdec -selectid $allPartIds -format sprite:png -zoom 1.0 `
    -ignorebackground -export sprite build\tmp\parts build\tmp\lib-clean.swf

# mounts 2x, no markers, no background
& $ffdec -selectid $allPartIds -format sprite:png -zoom 2.0 `
    -ignorebackground -export sprite build\tmp\mounts build\tmp\lib-clean.swf

# UI widgets/pages 1x, no background
& $ffdec -selectid $allUiIds -format sprite:png -zoom 1.0 `
    -ignorebackground -export sprite build\tmp\ui build\tmp\lib-clean.swf

# shape-only UI art
& $ffdec -selectid $shapeIds -format shape:png -export shape build\tmp\shapes build\tmp\lib-clean.swf
```

Only frame `1.png` is used for static art. For 3-frame buttons, copy all frames
and name them `_up`, `_over`, `_down` after the manual state decision.

### Step 5 — data assets

```powershell
& $ffdec -export font  build\tmp\fonts  $uiLib
& $ffdec -export text  build\tmp\texts  $uiLib
& $ffdec -export sound build\tmp\sounds $soundLib
```

Cinematics stay on the existing `CinematicExporter`: wrapper XML per clip,
`-config export.zoom=1.0 -export frame`, then MediaEncoder to 1920×1080 H.264.

### Step 6 — naming and output contract

| Asset | Pattern | Example |
|---|---|---|
| Part thumbnail | `part_<firstCatalogId:000>_<mcName>.png` | `part_000_Body_1.png` |
| Part mount | same name under `PartMounts/` | `PartMounts/part_000_Body_1.png` |
| Bullet | `bullet_<mcName>.png` | `bullet_Blt_1.png` |
| Pickup | `pickup_<fuel\|money\|life\|part>.png` | `pickup_fuel.png` |
| Background | `bg_<symbol>.png` | `bg_BGU7copy.png` |
| UI page | `ui_<id>_<Class>.png` | `ui_0608_UI_ChooseFlyAI.png` |
| UI widget | `ui_<id>_<role>.png` | `ui_0114_next_page.png` |
| Cursor | `ui_0917_MouseOut.png` | |
| Cinematic | `Mov<N>.mp4` | `Mov1.mp4` |

The intended Unity path for every symbol is already in the manifest
(`intendedUnityPath`); the script should emit a fresh manifest from
`symbols.csv` plus the pivot table and diff it against the committed one.

### Step 7 — import settings

Use the existing contract from `SpriteExportPipeline.ApplyImportSettings`:

- Sprite (Single), PPU 1, uncompressed, bilinear, clamp, no mipmaps,
  alphaIsTransparency, sRGB, FullRect mesh, extrude 0, max 2048.
- Pivot: per manifest. Parts centre; full-screen pages top-left; cursor (13,13).
- Atlas: BC7 (Standalone), ASTC 6×6 (Android/iPhone), 4 px padding, no rotation,
  tight packing, alpha dilation. Keep full-screen pages/backdrops out of the
  widget atlas; give each page family its own atlas or load as standalone
  textures so one 640×480 backdrop does not evict 100 widgets.

### Step 8 — verification gates

1. `-dumpSWF` counts match the committed counts (844 / 917 / 33).
2. Every `symbols.csv` id has exactly one manifest row.
3. Every extracted PNG has non-zero alpha and expected size ±2 px.
4. Marker names `^[LHPF]` are absent from the clean render XML.
5. Contact sheets regenerate and are diffed (or at least reviewed) per run.
6. `python scripts/export_audio.py --verify` still passes.

### 13.1 What cannot be automated

- Explosion/trail/particle VFX (no symbols).
- Runtime-created text/telemetry/HUD badges.
- Button frame → state semantics.
- Full-screen layer separation and off-stage cropping.
- Morph-shape interpolation.
- Background level/parallax assignment.
- Licence decision and replacement of commercial fonts.
- Identification of UI sounds 637/640/677 and UI bitmaps' parent screens.
- Per-part collision shapes (shared RECT is not a hitbox).
- Colour/hue variants used by `MC2BMPD` (`colorHue`), which are runtime tints,
  not separate symbols.

## 14. Acceptance mapping

| Criterion | Evidence |
|---|---|
| Every asset tag classified | `asset-manifest.json` 1,795 rows; statuses `extracted` 322 / `composite-only` 862 / `unavailable` 611; `extractable` boolean differentiates to-do from impossible |
| Every UI section mapped to exact symbols | §10 tables; manifest `section` and `parents` |
| Mount vs thumbnail vs editor icon distinguished | §4.2; 222 symbols → 444 files; no editor icons exist |
| Marker layers and sockets documented | §4.3: 749 H, 206 F, 151 L, 40 P; missing-marker counts |
| Gaps and manual slicing named | §12 and §13.1; no silent fallbacks |
| Manifest/contact sheets committed | `docs/research/asset-manifest.{json,csv}`, six PNG sheets |
| Pipeline reproducible | §13 exact FFDec commands and import contract |
| No runtime/UI changes | Only `docs/` artifacts touched |
