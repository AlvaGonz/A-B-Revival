#!/usr/bin/env python3
"""
scripts/extract_talk_portraits.py

Re-extracts the talk portrait frames from the frozen UI SWF with FFDec CLI.
The frame labels on the `face` sprite are the speaker names used by the talk XML;
the portrait catalog is then indexed by the speaker id assigned by TalkScriptParser.

Usage:
    python scripts/extract_talk_portraits.py
    python scripts/extract_talk_portraits.py --ffdec "C:\\path\\to\\ffdec-cli.exe"

Original art: Vargames (2011).
"""

import argparse
import os
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[1]
UI_ASSET = REPO_ROOT / "src" / "assets" / "binaryData" / "9_all.AllData_ui.bin"
OUT_DIR = REPO_ROOT / "A&B-U" / "Assets" / "Art" / "Talk" / "Portraits"
DEFAULT_FFDEC = Path(r"C:\Program Files (x86)\FFDec\ffdec-cli.exe")
FACE_SPRITE_ID = 56

ANSI_ESCAPE = re.compile(r"\x1b\[[0-9;]*m")
FRAME_LABEL = re.compile(r'FrameLabel \(name: "([^"]*)"\)')


def read_face_labels(ffdec: Path) -> list[str]:
    result = subprocess.run(
        [str(ffdec), "-dumpSWF", str(UI_ASSET)],
        capture_output=True,
        text=True,
        check=True,
    )
    labels = []
    in_face_sprite = False
    for line in result.stdout.splitlines():
        line = ANSI_ESCAPE.sub("", line)
        if f"DefineSprite (chid: {FACE_SPRITE_ID})" in line:
            in_face_sprite = True
            continue
        if in_face_sprite and "DefineSprite (chid:" in line:
            break
        if in_face_sprite:
            match = FRAME_LABEL.search(line)
            if match:
                labels.append(match.group(1))
    return labels


def safe_filename(speaker: str) -> str:
    cleaned = re.sub(r'[<>:"/\\|?*]', "_", speaker).strip().rstrip(".")
    return cleaned or "Unknown"


def extract(ffdec: Path, labels: list[str]) -> None:
    if not UI_ASSET.is_file():
        raise SystemExit(f"Frozen UI asset not found: {UI_ASSET}")
    if not ffdec.is_file():
        raise SystemExit(f"FFDec CLI not found: {ffdec} (pass --ffdec)")
    if not labels:
        raise SystemExit(f"No FrameLabel tags found on sprite {FACE_SPRITE_ID}.")

    OUT_DIR.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="ab-talk-portraits-") as temp_dir:
        subprocess.run(
            [str(ffdec), "-export", "sprite", temp_dir, str(UI_ASSET)],
            check=True,
            stdout=subprocess.DEVNULL,
        )
        frame_dir = Path(temp_dir) / f"DefineSprite_{FACE_SPRITE_ID}"
        for frame_number, speaker in enumerate(labels, start=1):
            source = frame_dir / f"{frame_number}.png"
            if not source.is_file():
                raise SystemExit(f"FFDec did not export frame {frame_number} for {speaker!r}.")
            destination = OUT_DIR / f"{safe_filename(speaker)}.png"
            shutil.copyfile(source, destination)
            if not destination.with_suffix(destination.suffix + ".meta").is_file():
                print(f"WARNING: missing {destination.name}.meta; Unity will assign a new GUID and the portrait catalog reference will break.")
            print(f"{frame_number:2d}  {speaker}  ->  {destination.name}")

    print(f"Extracted {len(labels)} portraits into {OUT_DIR}")


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ffdec", type=Path, default=DEFAULT_FFDEC, help="Path to ffdec-cli.exe")
    args = parser.parse_args()

    labels = read_face_labels(args.ffdec)
    print("Frame labels:", ", ".join(labels))
    extract(args.ffdec, labels)
    return 0


if __name__ == "__main__":
    sys.exit(main())
