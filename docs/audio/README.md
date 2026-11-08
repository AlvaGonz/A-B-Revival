# Audio

Original soundtrack and effects for *Assembly & Battle: The Story in the Space*.
All audio is extracted from the frozen Flash bank; Vargames remains the sole
copyright owner. Do not add procedural replacements to this folder.

## Assets

- `A&B-U/Assets/Resources/Audio/Original/` — 11 music tracks and 22 effects with deterministic names.
- `A&B-U/Assets/Resources/Audio/ABRevivalAudioMixer.mixer` — Music, SFX and UI groups.
- `docs/audio/sound-bank-inventory.md` — clip inventory and original playback sites.
- `A&B-U/Assets/Scripts/Audio/SoundCatalog.cs` — pure C# ids, buses, trigger names and volumes.
- `A&B-U/Assets/Scripts/Presentation/Audio/AudioService.cs` — runtime playback service.
- `A&B-U/Assets/Scripts/Presentation/Audio/IAudioService.cs` — role interfaces for other tickets.

## Regenerate

```bat
python scripts\export_audio.py
```

`scripts\export_audio.py` exports `src\assets\binaryData\6_all.AllData_sound.bin`
with FFDec, strips the character ids, validates the expected 33-clip set and
rewrites the inventory. Use `python scripts\export_audio.py --verify` to check
committed clips without exporting.

After a fresh import, run `Tools > A&B Revival > Audio > Rebuild Audio Assets`
in Unity. It creates the mixer if missing and configures music as Streaming and
effects as Decompress On Load.

## Runtime use

Create the service once at boot:

```csharp
AudioService.EnsureInstance();
```

Other tickets should depend on the smallest role interface they need:

| Interface | Original trigger coverage |
|---|---|
| `IMenuAudio` | Main-menu music, button click, confirm, cancel, rejected action. |
| `IAssemblyAudio` | Hangar music, part attach/remove, rejected action, craft select/clear, zoom in/out. |
| `ICombatAudio` | Weapon reports, explosion, booster, component lost, power restored, tactical mode, camera distance, pickups. |
| `IMissionAudio` | Briefing music, story-index mission music, dialogue duck/resume, talk advance, win/lose, credits. |

`IAudioService` exposes the low-level `PlayMusic`, `PlaySfx`, `PlayUi` and
`PlayTrigger` calls. All clips and voices are preloaded in `Awake`; gameplay
triggers do not allocate managed memory.

## Fidelity notes

- The original music playlist order is reproduced without the per-session random
  shuffle.
- Music keeps playing at the original 0.01 silence floor when disabled, and
  ducks to 0.2 during dialogue/menus, matching `SoundCtrl2`.
- A music change stops the previous cue immediately. The Flash release faded it
  out at 0.1 per frame while the new cue faded in; matching that would need a
  second music voice and is deferred.
- `S_Hit`, `S_Fuel` and `S_Click` exist in the bank but have no AS3 call site.
  They remain available in the catalogue and are not wired to a trigger.
