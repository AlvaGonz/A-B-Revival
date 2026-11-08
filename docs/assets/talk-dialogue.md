# Talk Dialogue and Portraits

Source data: `src/assets/binaryData/5_all.AllData_talk.bin` (plain XML).
Source art: `src/assets/binaryData/9_all.AllData_ui.bin` (SWF; the `face` sprite is character id 56).

## Runtime data

`A&B-U/Assets/Resources/Talk/5_all.AllData_talk.xml` is a verbatim copy of the frozen talk XML.
`TalkScreen` parses it once on start; `TalkScriptParser` assigns speaker ids in
first-appearance order (0..12) and keeps lines ordered by their source `<index>`.

`A&B-U/Assets/Scripts/Tests/TalkScriptParserTests.cs` verifies 388 lines, 13 speakers,
deterministic ordering, and that the runtime copy still matches the frozen file.

## Portrait extraction

The `face` sprite has 13 frame labels. Re-extract them with FFDec CLI:

```powershell
python scripts/extract_talk_portraits.py
```

The script runs `ffdec-cli.exe -export sprite <temp> src/assets/binaryData/9_all.AllData_ui.bin`
and copies `DefineSprite_56/<frame>.png` into `A&B-U/Assets/Art/Talk/Portraits/`.
The committed `.png.meta` files must stay with their PNG (the catalog references their GUIDs).

| Frame | Speaker label | File |
| ---: | :--- | :--- |
| 1 | Michael | `Michael.png` |
| 2 | Tom | `Tom.png` |
| 3 | Wang | `Wang.png` |
| 4 | Captain | `Captain.png` |
| 5 | Pilot XA | `Pilot XA.png` |
| 6 | Officer XA | `Officer XA.png` |
| 7 | CommanderX | `CommanderX.png` |
| 8 | Cp.Monkey | `Cp.Monkey.png` |
| 9 | Head Of XA | `Head Of XA.png` |
| 10 | ROBOT-S1 | `ROBOT-S1.png` |
| 11 | Unknown Cp. | `Unknown Cp.png` |
| 12 | Unknown Cf. | `Unknown Cf.png` |
| 13 | Somebody | `Somebody.png` |

`A&B-U/Assets/Resources/Talk/TalkPortraitCatalog.asset` stores those sprites in
speaker-id order, not frame order. Speaker id 0 is `Somebody` (the first speaker in the
talk XML), so the catalog is not a simple 1..13 copy of the frames.

## Remaining integration

- No scene wires `TalkScreen` yet. The PlayMode test exercises the presenter, view, portrait
  lookup, and click-sound seam directly; a later playability ticket owns the scene.
- `ITalkAudio` currently resolves to `NoOpTalkAudio`. Ticket #158 supplies the sound-bank
  implementation and should be picked up automatically when it sits on the same GameObject.
