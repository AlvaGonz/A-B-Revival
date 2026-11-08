#!/usr/bin/env python3
"""
Extract the frozen A&B sound bank into Unity audio clips with deterministic names.

The bank (src/assets/binaryData/6_all.AllData_sound.bin) is a compressed SWF that
JPEXS FFDec exports as "<characterId>_<className>.mp3". This script strips the
character id, validates the expected 33-clip inventory, writes the clips to
A&B-U/Assets/Resources/Audio/Original, and regenerates the documented inventory.

Usage:
    python scripts/export_audio.py
    python scripts/export_audio.py --verify
"""

import argparse
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

DEFAULT_FFDEC = r"C:\Program Files (x86)\FFDec\ffdec-cli.exe"
BANK_RELATIVE = Path("src/assets/binaryData/6_all.AllData_sound.bin")
OUT_RELATIVE = Path("A&B-U/Assets/Resources/Audio/Original")
INVENTORY_RELATIVE = Path("docs/audio/sound-bank-inventory.md")

# (original musicArray index, clip name, original playback site)
MUSIC = [
    (0, "M_Main", "UIMain and UIPlayer return music, volume 1."),
    (1, "M_Menu", "Director pre/post-mission briefing at volume 0.25; UIChooseFlyAI at volume 1."),
    (2, "M_Game1", "Mission playlist pool (early and mid story sets)."),
    (3, "M_Game2", "Mission playlist pool (early and mid story sets)."),
    (4, "M_Game3", "Story 0 and 7 mission music."),
    (5, "M_Game4", "Mission playlist pool (early and mid story sets)."),
    (6, "M_Game5", "Story 34 mission opener."),
    (7, "M_Game6", "Story 8 mission music; story 34 playlist."),
    (8, "M_Game7", "Story 9 and 10 mission music; late playlists."),
    (9, "M_Creidts", "Story 49 credits, single play."),
    (10, "M_Win2", "UIWin and UIAllWin victory music."),
]

# (original soundArray index or None, clip name, original playback site)
EFFECTS = [
    (0, "S_Fire100", "Heavy projectile report (bullet width >= 10); also the looped engine source."),
    (1, "S_FireGT", "FireMode fast-fire report (fireTimer.stopFrame < 5)."),
    (2, "S_FireL1", "Light projectile report (bullet width < 5)."),
    (3, "S_FireL2", "Medium projectile report (bullet width < 10)."),
    (4, "S_Bomb", "Explosion and destroyed-part blast."),
    (5, "S_Hit", "Present in the bank; no AS3 call site found (legacy unused)."),
    (6, "S_Pow", "Player booster."),
    (7, "S_Rocket", "Rocket report (FireMode param1 < 5, volume 0.2)."),
    (8, "S_Fuel", "Present in the bank; no AS3 call site found (legacy unused)."),
    (9, "S_Money", "Money pickup (volume 0.5)."),
    (10, "S_Repair", "Health pickup (volume 1.4)."),
    (11, "S_Weapon", "Part pickup."),
    (12, "S_Zin", "Assembly and canvas zoom in."),
    (13, "S_Cant", "Rejected action; lost component at volume 0.35."),
    (14, "S_Check", "Accepted action, tactical mode change and camera distance change."),
    (15, "S_Word", "Dialogue advance (UITalk)."),
    (16, "S_Zout", "Assembly zoom out and options scroll."),
    (17, "S_Yes", "Confirm action and victory debug shortcut."),
    (18, "S_No", "Cancel action and part removal."),
    (19, "S_Start", "Menu button click (UIMain, UIOptions, UIData)."),
    (20, "S_Eng", "Power restored (AIUnity.runPowMut, volume 1.15)."),
    (None, "S_Click", "Present in the bank; no AS3 call site found (legacy unused)."),
]

EXPORTED_NAME = re.compile(r"^\d+_((?:M|S)_[A-Za-z0-9]+)\.mp3$", re.IGNORECASE)


def repo_root() -> Path:
    return Path(__file__).resolve().parent.parent


def expected_names():
    return {name for _, name, _ in MUSIC} | {name for _, name, _ in EFFECTS}


def run_ffdec(ffdec: Path, bank: Path, output: Path) -> None:
    completed = subprocess.run(
        [str(ffdec), "-export", "sound", str(output), str(bank)],
        capture_output=True,
        text=True,
    )
    if completed.returncode != 0:
        raise RuntimeError(
            f"FFDec failed with exit code {completed.returncode}:\n{completed.stderr}"
        )


def normalized_clips(export_dir: Path) -> dict:
    clips = {}
    for path in export_dir.iterdir():
        match = EXPORTED_NAME.match(path.name)
        if match:
            clips[match.group(1)] = path
    return clips


def write_inventory(root: Path, out_dir: Path) -> None:
    inventory = root / INVENTORY_RELATIVE
    inventory.parent.mkdir(parents=True, exist_ok=True)
    lines = [
        "# Original Sound Bank Inventory",
        "",
        "Source: `src/assets/binaryData/6_all.AllData_sound.bin` (compressed SWF, 33 named clips).",
        "Extraction: `python scripts/export_audio.py` (JPEXS FFDec CLI).",
        "Runtime catalogue: `A&B-U/Assets/Scripts/Audio/SoundCatalog.cs`.",
        "",
        "The original `SoundData.initArray()` effect order is preserved as the id order.",
        "`S_Click` exists in the frozen bank but is absent from that AS3 array; it is",
        "appended as effect id 21 so original ids remain stable.",
        "",
        "## Music (11)",
        "",
        "| Id | Clip | Original playback site | Size |",
        "| ---: | --- | --- | ---: |",
    ]
    for index, name, use in MUSIC:
        size = (out_dir / f"{name}.mp3").stat().st_size
        lines.append(f"| {index} | `{name}.mp3` | {use} | {size:,} B |")

    lines += [
        "",
        "## Effects (22)",
        "",
        "| Id | Clip | Original playback site | Size |",
        "| ---: | --- | --- | ---: |",
    ]
    for index, name, use in EFFECTS:
        size = (out_dir / f"{name}.mp3").stat().st_size
        id_text = "n/a" if index is None else str(index)
        lines.append(f"| {id_text} | `{name}.mp3` | {use} | {size:,} B |")

    lines += [
        "",
        "## Trigger Mapping",
        "",
        "Semantic trigger names and their clip/bus/volume mapping live in",
        "`SoundCatalog.Triggers`. The runtime service is",
        "`A&B-U/Assets/Scripts/Presentation/Audio/AudioService.cs`.",
        "",
    ]
    inventory.write_text("\n".join(lines), encoding="utf-8")


def verify(out_dir: Path) -> int:
    missing = []
    for name in sorted(expected_names()):
        path = out_dir / f"{name}.mp3"
        if not path.is_file() or path.stat().st_size == 0:
            missing.append(str(path))
    if missing:
        print("[export_audio] Missing or empty clips:", file=sys.stderr)
        for path in missing:
            print(f"  {path}", file=sys.stderr)
        return 1
    print(f"[export_audio] Verified {len(expected_names())} clips in {out_dir}.")
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ffdec", default=DEFAULT_FFDEC, type=Path)
    parser.add_argument("--verify", action="store_true", help="Only verify committed clips.")
    args = parser.parse_args()

    root = repo_root()
    out_dir = root / OUT_RELATIVE

    if args.verify:
        return verify(out_dir)

    bank = root / BANK_RELATIVE
    if not bank.is_file():
        print(f"[export_audio] Bank not found: {bank}", file=sys.stderr)
        return 1
    if not args.ffdec.is_file():
        print(f"[export_audio] FFDec CLI not found: {args.ffdec}", file=sys.stderr)
        return 1

    expected = expected_names()
    out_dir.mkdir(parents=True, exist_ok=True)

    with tempfile.TemporaryDirectory(prefix="ab-sound-") as temp:
        export_dir = Path(temp)
        run_ffdec(args.ffdec, bank, export_dir)
        clips = normalized_clips(export_dir)

        missing = sorted(expected - {name for name in clips})
        unexpected = sorted({name for name in clips} - expected)
        if missing or unexpected:
            print("[export_audio] Bank inventory changed.", file=sys.stderr)
            print(f"  missing: {missing}", file=sys.stderr)
            print(f"  unexpected: {unexpected}", file=sys.stderr)
            return 1

        for existing in out_dir.glob("*.mp3"):
            existing.unlink()

        for name in sorted(expected):
            shutil.copy2(clips[name], out_dir / f"{name}.mp3")

    write_inventory(root, out_dir)
    print(f"[export_audio] Extracted {len(expected)} clips into {out_dir}.")
    return verify(out_dir)


if __name__ == "__main__":
    sys.exit(main())
