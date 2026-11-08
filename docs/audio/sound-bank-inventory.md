# Original Sound Bank Inventory

Source: `src/assets/binaryData/6_all.AllData_sound.bin` (compressed SWF, 33 named clips).
Extraction: `python scripts/export_audio.py` (JPEXS FFDec CLI).
Runtime catalogue: `A&B-U/Assets/Scripts/Audio/SoundCatalog.cs`.

The original `SoundData.initArray()` effect order is preserved as the id order.
`S_Click` exists in the frozen bank but is absent from that AS3 array; it is
appended as effect id 21 so original ids remain stable.

## Music (11)

| Id | Clip | Original playback site | Size |
| ---: | --- | --- | ---: |
| 0 | `M_Main.mp3` | UIMain and UIPlayer return music, volume 1. | 586,327 B |
| 1 | `M_Menu.mp3` | Director pre/post-mission briefing at volume 0.25; UIChooseFlyAI at volume 1. | 924,624 B |
| 2 | `M_Game1.mp3` | Mission playlist pool (early and mid story sets). | 802,897 B |
| 3 | `M_Game2.mp3` | Mission playlist pool (early and mid story sets). | 708,520 B |
| 4 | `M_Game3.mp3` | Story 0 and 7 mission music. | 789,958 B |
| 5 | `M_Game4.mp3` | Mission playlist pool (early and mid story sets). | 747,291 B |
| 6 | `M_Game5.mp3` | Story 34 mission opener. | 534,626 B |
| 7 | `M_Game6.mp3` | Story 8 mission music; story 34 playlist. | 699,137 B |
| 8 | `M_Game7.mp3` | Story 9 and 10 mission music; late playlists. | 863,169 B |
| 9 | `M_Creidts.mp3` | Story 49 credits, single play. | 646,410 B |
| 10 | `M_Win2.mp3` | UIWin and UIAllWin victory music. | 838,362 B |

## Effects (22)

| Id | Clip | Original playback site | Size |
| ---: | --- | --- | ---: |
| 0 | `S_Fire100.mp3` | Heavy projectile report (bullet width >= 10); also the looped engine source. | 9,152 B |
| 1 | `S_FireGT.mp3` | FireMode fast-fire report (fireTimer.stopFrame < 5). | 4,576 B |
| 2 | `S_FireL1.mp3` | Light projectile report (bullet width < 5). | 3,120 B |
| 3 | `S_FireL2.mp3` | Medium projectile report (bullet width < 10). | 5,590 B |
| 4 | `S_Bomb.mp3` | Explosion and destroyed-part blast. | 8,320 B |
| 5 | `S_Hit.mp3` | Present in the bank; no AS3 call site found (legacy unused). | 1,456 B |
| 6 | `S_Pow.mp3` | Player booster. | 33,696 B |
| 7 | `S_Rocket.mp3` | Rocket report (FireMode param1 < 5, volume 0.2). | 8,736 B |
| 8 | `S_Fuel.mp3` | Present in the bank; no AS3 call site found (legacy unused). | 8,320 B |
| 9 | `S_Money.mp3` | Money pickup (volume 0.5). | 6,656 B |
| 10 | `S_Repair.mp3` | Health pickup (volume 1.4). | 13,728 B |
| 11 | `S_Weapon.mp3` | Part pickup. | 6,448 B |
| 12 | `S_Zin.mp3` | Assembly and canvas zoom in. | 3,120 B |
| 13 | `S_Cant.mp3` | Rejected action; lost component at volume 0.35. | 2,912 B |
| 14 | `S_Check.mp3` | Accepted action, tactical mode change and camera distance change. | 6,448 B |
| 15 | `S_Word.mp3` | Dialogue advance (UITalk). | 1,664 B |
| 16 | `S_Zout.mp3` | Assembly zoom out and options scroll. | 3,120 B |
| 17 | `S_Yes.mp3` | Confirm action and victory debug shortcut. | 10,816 B |
| 18 | `S_No.mp3` | Cancel action and part removal. | 4,784 B |
| 19 | `S_Start.mp3` | Menu button click (UIMain, UIOptions, UIData). | 8,320 B |
| 20 | `S_Eng.mp3` | Power restored (AIUnity.runPowMut, volume 1.15). | 12,272 B |
| n/a | `S_Click.mp3` | Present in the bank; no AS3 call site found (legacy unused). | 2,080 B |

## Trigger Mapping

Semantic trigger names and their clip/bus/volume mapping live in
`SoundCatalog.Triggers`. The runtime service is
`A&B-U/Assets/Scripts/Presentation/Audio/AudioService.cs`.
