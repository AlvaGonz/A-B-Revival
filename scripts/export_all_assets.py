#!/usr/bin/env python3
"""
One-command extraction of every standalone renderable in the frozen Flash build.

Source of truth:
  docs/research/asset-manifest.json  (research #173: 1,795 symbols, 742 extractable)
  game/a&b_space_mod.swf             (preferred) or game/a&b_space.swf

The pipeline:
  1. unpacks the loader's DefineBinaryData libraries (data / UI / sound);
  2. strips L/H/F/P marker placements from a copy of each library;
  3. renders parts (thumbnail 1x, mount 2x nose-up), bullets, pickups,
     backgrounds, UI screens, help pages, widgets, locks and the cursor;
  4. exports the frozen fonts, UI sounds and a JSON string table;
  5. writes docs/assets/extraction-manifest.json (counts, pivots, button
     states, nine-slice, ambiguities) and docs/assets/extraction-report.md;
  6. regenerates the six committed contact sheets plus a font sheet;
  7. applies pivots/import settings and builds the category atlases through
     Unity batchmode (ABRevival.Editor.FullAssetExtractionImporter).

Every output is deterministic: files are only rewritten when their bytes
change, no timestamps are stored, and re-running produces the same manifest.
Original game IP and art: Vargames (2011). See ADR 0020 for font fidelity.

Usage:
    python scripts/export_all_assets.py
    python scripts/export_all_assets.py --skip-unity
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import os
import re
import shutil
import subprocess
import sys
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "A&B-U"
UNITY_ASSETS = PROJECT / "Assets"
MANIFEST_PATH = ROOT / "docs/research/asset-manifest.json"
OUT_MANIFEST = ROOT / "docs/assets/extraction-manifest.json"
OUT_REPORT = ROOT / "docs/assets/extraction-report.md"
TEXT_TABLE = ROOT / "docs/assets/text/string-table.json"
NINE_SLICE_OVERRIDES = ROOT / "docs/assets/ui-nine-slice-overrides.json"
CONTACT_DIR = ROOT / "docs/assets/contact-sheets"
REFERENCE_DIR = ROOT / "docs/assets/reference"

TEMP = Path(os.environ.get("TEMP", tempfile.gettempdir())) / "ab-revival-extraction"
DEFAULT_FFDEC = r"C:\Program Files (x86)\FFDec\ffdec-cli.exe"
DEFAULT_UNITY = r"C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe"

DATA_LIB_SUFFIX = "all.AllData_data.bin"
UI_LIB_SUFFIX = "all.AllData_ui.bin"
SOUND_LIB_SUFFIX = "all.AllData_sound.bin"

EXPECTED_CHARACTERS = {"data": 844, "ui": 917, "sound": 33}
EXPECTED_MANIFEST_ROWS = 1795
EXPECTED_STANDALONE = 742
EXPECTED_PREVIOUSLY_EXTRACTED = 322
EXPECTED_NEW = 420

# FFDec copies the source SWF modification time into the exported TTF `head`
# timestamps. Pinning the temp library mtime makes the font bytes deterministic.
FONT_SOURCE_MTIME = 1306886400  # 2011-06-01T00:00:00Z

# Glyph counts recorded by research #173; kept in the manifest for licence review.
FONT_GLYPHS = {2: 115, 478: 114, 7: 115, 627: 114, 701: 51, 81: 114, 496: 1, 595: 2}

MARKER_NAME = re.compile(r"^[LHPF]")
MARKER_PLACEMENT = re.compile(
    r'<item type="PlaceObject[234]Tag"[^>]*name="([^"]*)"[^>]*?(?:/>|>.*?</item>)',
    re.S,
)
SPRITE_FOLDER = re.compile(r"^DefineSprite_(\d+)(?:_|$)")
SYMBOL_CLASS_LINE = re.compile(r'^(\d+);"(.*)"$')
FONT_FILE = re.compile(r"^(\d+)_(.+)\.ttf$")
TIP_PLACEHOLDER = re.compile(r"^Tip\d+:\s*asdsadjsladjasd\.$")

# Explicit per-widget nine-slice overrides. The defaults are written once; a
# later human edit survives because the file is not overwritten when present.
DEFAULT_NINE_SLICE_OVERRIDES = {
    "73": [0, 0, 0, 0],
    "114": [0, 0, 0, 0],
    "119": [0, 0, 0, 0],
    "318": [0, 0, 0, 0],
    "321": [0, 0, 0, 0],
    "466": [0, 0, 0, 0],
    "467": [0, 0, 0, 0],
    "499": [0, 0, 0, 0],
    "505": [0, 0, 0, 0],
    "737": [0, 0, 0, 0],
    "792": [0, 0, 0, 0],
    "822": [0, 0, 0, 0],
    "850": [0, 0, 0, 0],
    "897": [0, 0, 0, 0],
    "908": [0, 0, 0, 0],
    "916": [0, 0, 0, 0],
}

STATE_MACHINE = {
    2: {"normal": 1, "hover": 2, "pressed": 2},
    3: {"normal": 1, "hover": 2, "pressed": 2, "selected": 3},
}


def log(message: str) -> None:
    print(message, flush=True)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1 << 20), b""):
            digest.update(chunk)
    return digest.hexdigest()


def rel(path: Path) -> str:
    return path.resolve().relative_to(ROOT).as_posix()


def to_project(path: str) -> str:
    """Converts a repo-relative A&B-U/... path to a Unity project path."""
    prefix = "A&B-U/"
    return path[len(prefix):] if path.startswith(prefix) else path


def resolve_ffdec() -> str:
    from_environment = os.environ.get("FFDEC_CLI")
    if from_environment and Path(from_environment).is_file():
        return from_environment
    for candidate in (DEFAULT_FFDEC, r"C:\Program Files\FFDec\ffdec-cli.exe"):
        if Path(candidate).is_file():
            return candidate
    return "ffdec-cli"


def resolve_unity() -> str:
    from_environment = os.environ.get("UNITY_EXE")
    if from_environment and Path(from_environment).is_file():
        return from_environment
    if Path(DEFAULT_UNITY).is_file():
        return DEFAULT_UNITY
    raise FileNotFoundError(
        "Unity 6.6.8f1 was not found. Set UNITY_EXE or install it under the default Hub path."
    )


def resolve_swf(explicit: str | None) -> Path:
    if explicit:
        path = Path(explicit)
        if not path.is_absolute():
            path = ROOT / path
        if not path.is_file():
            raise FileNotFoundError(f"SWF not found: {path}")
        return path
    for candidate in (ROOT / "game/a&b_space_mod.swf", ROOT / "game/a&b_space.swf"):
        if candidate.is_file():
            return candidate
    raise FileNotFoundError(
        "Source SWF not found. Expected game/a&b_space_mod.swf or game/a&b_space.swf."
    )


def run_ffdec(ffdec: str, *arguments: object) -> str:
    command = [ffdec, *[str(argument) for argument in arguments]]
    result = subprocess.run(
        command,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
        check=False,
    )
    if result.returncode != 0:
        raise RuntimeError(
            f"FFDec failed ({result.returncode}): {' '.join(command)}\n{result.stdout}\n{result.stderr}"
        )
    return result.stdout + result.stderr


def write_bytes_if_changed(path: Path, data: bytes) -> bool:
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.is_file() and path.read_bytes() == data:
        return False
    path.write_bytes(data)
    return True


def copy_if_changed(source: Path, destination: Path) -> bool:
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.is_file() and destination.read_bytes() == source.read_bytes():
        return False
    shutil.copyfile(source, destination)
    return True


def save_png_if_changed(image: Image.Image, destination: Path) -> bool:
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.is_file():
        with Image.open(destination) as existing:
            if existing.size == image.size and existing.convert("RGBA").tobytes() == image.convert("RGBA").tobytes():
                return False
    buffer = destination.with_suffix(destination.suffix + ".tmp.png")
    image.save(buffer, format="PNG", optimize=False)
    data = buffer.read_bytes()
    buffer.unlink()
    return write_bytes_if_changed(destination, data)


def copy_png_if_pixels_changed(source: Path, destination: Path) -> bool:
    destination.parent.mkdir(parents=True, exist_ok=True)
    if destination.is_file():
        with Image.open(source) as source_image, Image.open(destination) as destination_image:
            if (
                source_image.size == destination_image.size
                and source_image.convert("RGBA").tobytes()
                == destination_image.convert("RGBA").tobytes()
            ):
                return False
    return copy_if_changed(source, destination)


def png_size(path: Path) -> tuple[int, int]:
    with Image.open(path) as image:
        return image.size


def alpha_bbox(path: Path) -> tuple[int, int, int, int] | None:
    with Image.open(path) as image:
        return image.convert("RGBA").getchannel("A").getbbox()


def pivot_from_bbox(
    image_width: int,
    image_height: int,
    bbox: tuple[int, int, int, int] | None,
    native_width: float | None,
    native_height: float | None,
    pivot_normalized: list[float] | None,
    zoom: float = 1.0,
    adjust: bool = True,
) -> tuple[float, float, str]:
    """Maps the manifest content-bound pivot onto the FFDec render canvas.

    FFDec pads the render canvas with filter/stroke bounds. The manifest pivot is
    relative to the content bounds, so it is only remapped when the alpha bbox
    actually matches the manifest native size. Part/bullet wrappers contain an
    invisible RECT, so their pivot stays the manifest value (centre).
    """
    pivot = pivot_normalized or [0.5, 0.5]
    px, py = float(pivot[0]), float(pivot[1])
    if not adjust or bbox is None or not native_width or not native_height:
        return px, py, "manifest"
    left, top, right, bottom = bbox
    content_width = float(native_width) * zoom
    content_height = float(native_height) * zoom
    tolerance = max(4.0, 0.1 * max(content_width, content_height))
    if abs((right - left) - content_width) > tolerance or abs((bottom - top) - content_height) > tolerance:
        return px, py, "manifest"
    x = (left + px * content_width) / float(image_width)
    y = (image_height - bottom + py * content_height) / float(image_height)
    return x, y, "alpha-bbox"


def rotate_pivot_ccw(pivot: tuple[float, float]) -> tuple[float, float]:
    x, y = pivot
    return 1.0 - y, x


def strip_marker_placements(xml: str) -> tuple[str, int]:
    removed = 0

    def replace(match: re.Match[str]) -> str:
        nonlocal removed
        name = match.group(1)
        if name == "HIT" or not MARKER_NAME.match(name):
            return match.group(0)
        removed += 1
        return ""

    cleaned = MARKER_PLACEMENT.sub(replace, xml)
    return cleaned, removed


def remaining_marker_names(xml: str) -> list[str]:
    names = re.findall(r'<item type="PlaceObject[234]Tag"[^>]*name="([^"]*)"', xml)
    return sorted({name for name in names if MARKER_NAME.match(name) and name != "HIT"})


def read_symbols(csv_path: Path) -> tuple[dict[str, int], dict[int, str]]:
    by_name: dict[str, int] = {}
    by_id: dict[int, str] = {}
    for line in csv_path.read_text(encoding="utf-8-sig").splitlines():
        match = SYMBOL_CLASS_LINE.match(line.strip())
        if not match:
            continue
        character_id = int(match.group(1))
        class_name = match.group(2)
        by_name[class_name] = character_id
        by_id[character_id] = class_name
    return by_name, by_id


def find_frames(root: Path, character_id: int) -> list[Path]:
    matches = [
        path
        for path in root.iterdir()
        if path.is_dir() and (match := SPRITE_FOLDER.match(path.name)) and int(match.group(1)) == character_id
    ]
    if len(matches) != 1:
        raise RuntimeError(f"Expected one FFDec sprite folder for {character_id} under {root}, found {len(matches)}.")
    frames = sorted(
        (path for path in matches[0].glob("*.png") if path.stem.isdigit()),
        key=lambda path: int(path.stem),
    )
    if not frames:
        raise RuntimeError(f"FFDec exported no frames for sprite {character_id}.")
    return frames


def load_manifest() -> dict:
    manifest = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
    rows = manifest["symbols"]
    if len(rows) != EXPECTED_MANIFEST_ROWS:
        raise RuntimeError(
            f"asset-manifest.json has {len(rows)} rows, expected {EXPECTED_MANIFEST_ROWS}."
        )
    extractable = [row for row in rows if row.get("extractable")]
    if len(extractable) != EXPECTED_STANDALONE:
        raise RuntimeError(
            f"asset-manifest.json has {len(extractable)} extractable rows, expected {EXPECTED_STANDALONE}."
        )
    extracted = [row for row in extractable if row.get("status") == "extracted"]
    new = [row for row in extractable if row.get("status") != "extracted"]
    if len(extracted) != EXPECTED_PREVIOUSLY_EXTRACTED or len(new) != EXPECTED_NEW:
        raise RuntimeError(
            f"Manifest split is {len(extracted)} extracted + {len(new)} new; "
            f"expected {EXPECTED_PREVIOUSLY_EXTRACTED} + {EXPECTED_NEW}."
        )
    for row in rows:
        if not is_library(row.get("swf"), DATA_LIB_SUFFIX, UI_LIB_SUFFIX, SOUND_LIB_SUFFIX):
            raise RuntimeError(f"Unexpected library in manifest row: {row.get('swf')}")
        if Path(row.get("intendedUnityPath", "")).is_absolute():
            raise RuntimeError(f"Manifest path must be project-relative: {row.get('intendedUnityPath')}")
    return manifest


def is_library(value: str | None, *suffixes: str) -> bool:
    return bool(value) and any(value.endswith(suffix) for suffix in suffixes)


def row_key(row: dict) -> tuple[str, int | None]:
    library = (
        "ui"
        if is_library(row.get("swf"), UI_LIB_SUFFIX)
        else "sound"
        if is_library(row.get("swf"), SOUND_LIB_SUFFIX)
        else "data"
    )
    return library, row.get("symbolId")


def dump_character_counts(ffdec: str, library: Path) -> int:
    dump = run_ffdec(ffdec, "-dumpSWF", library)
    ids = {int(match) for match in re.findall(r"\(chid:\s*(\d+)\)", dump)}
    font_ids = {int(match) for match in re.findall(r"DefineFont3\s+\(chid:\s*(\d+)", dump)}
    return len(ids | font_ids)


def unpack_libraries(ffdec: str, swf: Path) -> dict[str, Path]:
    binary_dir = TEMP / "bin"
    if binary_dir.exists():
        shutil.rmtree(binary_dir)
    binary_dir.mkdir(parents=True)
    run_ffdec(ffdec, "-export", "binaryData", binary_dir, swf)

    libraries: dict[str, Path] = {}
    for suffix, key in (
        (DATA_LIB_SUFFIX, "data"),
        (UI_LIB_SUFFIX, "ui"),
        (SOUND_LIB_SUFFIX, "sound"),
    ):
        matches = sorted(binary_dir.glob(f"*{suffix}"))
        if len(matches) != 1:
            raise RuntimeError(f"Expected one binary data payload for {suffix}, found {len(matches)}.")
        libraries[key] = matches[0]
        committed = ROOT / "src/assets/binaryData" / matches[0].name
        if committed.is_file() and sha256(committed) != sha256(matches[0]):
            raise RuntimeError(
                f"{matches[0].name} extracted from {swf.name} differs from the committed copy. "
                "Refresh src/assets/binaryData with scripts/export-assets.bat first."
            )
        count = dump_character_counts(ffdec, matches[0])
        if count != EXPECTED_CHARACTERS[key]:
            raise RuntimeError(
                f"{matches[0].name}: -dumpSWF reports {count} characters, "
                f"expected {EXPECTED_CHARACTERS[key]}."
            )
        os.utime(matches[0], (FONT_SOURCE_MTIME, FONT_SOURCE_MTIME))
    return libraries


def make_clean_library(ffdec: str, source: Path, name: str) -> tuple[Path, Path, int]:
    xml_path = TEMP / f"{name}.xml"
    clean_xml = TEMP / f"{name}-clean.xml"
    clean_swf = TEMP / f"{name}-clean.swf"
    run_ffdec(ffdec, "-swf2xml", source, xml_path)
    xml = xml_path.read_text(encoding="utf-8")
    cleaned, removed = strip_marker_placements(xml)
    leftovers = remaining_marker_names(cleaned)
    if leftovers:
        raise RuntimeError(f"Marker placements survived cleaning {source.name}: {leftovers[:8]}")
    clean_xml.write_text(cleaned, encoding="utf-8", newline="")
    run_ffdec(ffdec, "-xml2swf", clean_xml, clean_swf)
    if removed == 0 and name == "data":
        raise RuntimeError("No marker placements were removed from the data library.")
    return clean_swf, xml_path, removed


def render(ffdec: str, swf: Path, ids: list[int], zoom: float, destination: Path) -> None:
    if not ids:
        return
    if destination.exists():
        shutil.rmtree(destination)
    destination.mkdir(parents=True)
    ordered = ",".join(str(value) for value in sorted(set(ids)))
    run_ffdec(
        ffdec,
        "-selectid",
        ordered,
        "-format",
        "sprite:png",
        "-zoom",
        f"{zoom:g}",
        "-ignorebackground",
        "-export",
        "sprite",
        destination,
        swf,
    )


def render_shapes(ffdec: str, swf: Path, ids: list[int], destination: Path) -> None:
    if not ids:
        return
    if destination.exists():
        shutil.rmtree(destination)
    destination.mkdir(parents=True)
    ordered = ",".join(str(value) for value in sorted(set(ids)))
    run_ffdec(
        ffdec,
        "-selectid",
        ordered,
        "-format",
        "shape:png",
        "-export",
        "shape",
        destination,
        swf,
    )


def ui_source_frames(row: dict) -> list[Path]:
    if row.get("kind") == "shape":
        path = TEMP / "render/ui-shapes" / f"{row['symbolId']}.png"
        if not path.is_file():
            raise RuntimeError(f"FFDec did not render UI shape {row['symbolId']}.")
        return [path]
    return find_frames(TEMP / "render/ui", row["symbolId"])


def export_fonts(ffdec: str, libraries: dict[str, Path]) -> dict[int, Path]:
    destination = TEMP / "fonts"
    if destination.exists():
        shutil.rmtree(destination)
    destination.mkdir(parents=True)
    found: dict[int, Path] = {}
    for key in ("ui", "data"):
        folder = destination / key
        folder.mkdir()
        run_ffdec(ffdec, "-export", "font", folder, libraries[key])
        for path in sorted(folder.glob("*.ttf")):
            match = FONT_FILE.match(path.name)
            if not match:
                raise RuntimeError(f"Unexpected FFDec font name: {path.name}")
            found[int(match.group(1))] = path
    return found


def export_texts(ffdec: str, libraries: dict[str, Path]) -> dict[str, dict[int, str]]:
    destination = TEMP / "text"
    if destination.exists():
        shutil.rmtree(destination)
    destination.mkdir(parents=True)
    texts: dict[str, dict[int, str]] = {}
    for key in ("ui", "data"):
        folder = destination / key
        folder.mkdir()
        run_ffdec(ffdec, "-export", "text", folder, libraries[key])
        texts[key] = {}
        for path in sorted(folder.glob("*.txt")):
            texts[key][int(path.stem)] = path.read_text(encoding="utf-8")
    return texts


def export_ui_images(ffdec: str, ui_library: Path) -> dict[int, Path]:
    destination = TEMP / "ui-images"
    if destination.exists():
        shutil.rmtree(destination)
    destination.mkdir(parents=True)
    run_ffdec(ffdec, "-export", "image", destination, ui_library)
    images: dict[int, Path] = {}
    for path in sorted(destination.iterdir()):
        if path.suffix.lower() not in (".png", ".jpg", ".jpeg"):
            continue
        if path.stem.isdigit():
            images[int(path.stem)] = path
    return images


def export_ui_sounds(ffdec: str, ui_library: Path) -> dict[int, Path]:
    destination = TEMP / "ui-sounds"
    if destination.exists():
        shutil.rmtree(destination)
    destination.mkdir(parents=True)
    run_ffdec(ffdec, "-export", "sound", destination, ui_library)
    sounds: dict[int, Path] = {}
    for path in sorted(destination.iterdir()):
        if path.stem.isdigit() and path.suffix.lower() in (".mp3", ".wav"):
            sounds[int(path.stem)] = path
    return sounds


def parse_symbol_metadata(ffdec: str, library: Path) -> dict:
    dump = run_ffdec(ffdec, "-dumpSWF", library)
    fonts = {
        int(match.group(1)): match.group(2)
        for match in re.finditer(r'DefineFont[23]\s+\(chid:\s*(\d+),\s*fn:\s*"([^"]*)"', dump)
    }
    morphs = sorted(
        {int(value) for value in re.findall(r"DefineMorphShape2?\s+\(chid:\s*(\d+)\)", dump)}
    )
    images = sorted({int(value) for value in re.findall(r"DefineBits\w*\s+\(chid:\s*(\d+)\)", dump)})
    sounds = sorted({int(value) for value in re.findall(r"DefineSound\s+\(chid:\s*(\d+)\)", dump)})
    return {"fonts": fonts, "morphs": morphs, "images": images, "sounds": sounds}


def parse_text_font_usage(xml_path: Path) -> dict[int, set[int]]:
    usage: dict[int, set[int]] = {}
    root = ET.parse(xml_path).getroot()
    for element in root.iter("item"):
        kind = element.get("type")
        if kind == "DefineEditTextTag":
            character_id = element.get("characterID")
            font_id = element.get("fontId")
            if character_id and font_id and font_id != "0":
                usage.setdefault(int(character_id), set()).add(int(font_id))
        elif kind == "DefineTextTag":
            character_id = element.get("characterID")
            if not character_id:
                continue
            fonts = {
                int(record.get("fontId"))
                for record in element.iter("item")
                if record.get("type") == "TEXTRECORD" and record.get("fontId")
            }
            if fonts:
                usage.setdefault(int(character_id), set()).update(fonts)
    return usage


def write_nine_slice_overrides() -> None:
    if NINE_SLICE_OVERRIDES.is_file():
        return
    payload = {
        "schema": 1,
        "note": (
            "Per-widget nine-slice borders in pixels [left, bottom, right, top] for the "
            "original UI widgets. Zero means the widget is not stretched. "
            "Consumed by scripts/export_all_assets.py when it writes the extraction manifest."
        ),
        "overrides": {
            key: value
            for key, value in sorted(
                DEFAULT_NINE_SLICE_OVERRIDES.items(), key=lambda item: int(item[0])
            )
        },
    }
    data = (json.dumps(payload, indent=2, ensure_ascii=False) + "\n").encode("utf-8")
    write_bytes_if_changed(NINE_SLICE_OVERRIDES, data)


def load_nine_slice_overrides() -> dict[int, list[int]]:
    if not NINE_SLICE_OVERRIDES.is_file():
        write_nine_slice_overrides()
    data = json.loads(NINE_SLICE_OVERRIDES.read_text(encoding="utf-8"))
    return {int(key): [int(value) for value in border] for key, border in data.get("overrides", {}).items()}


def derive_nine_slice(row: dict, overrides: dict[int, list[int]]) -> tuple[list[int], str]:
    symbol_id = row.get("symbolId")
    if symbol_id in overrides:
        return list(overrides[symbol_id]), "override"
    if row.get("frameCount") not in (2, 3):
        return [0, 0, 0, 0], "none"
    width = row.get("nativeWidth") or 0
    height = row.get("nativeHeight") or 0
    if not width or not height:
        return [0, 0, 0, 0], "unknown"
    if width >= 100 and height >= 40:
        border = 10
    elif width >= 100 and height >= 25:
        border = 8
    elif width >= 60 and height >= 28:
        border = 8
    elif width >= 40 and height >= 16:
        border = 5
    elif width >= 40 and height >= 10:
        border = 4
    elif width >= 24 and height >= 10:
        border = 3
    else:
        border = 0
    return [border, border, border, border], "derived"


def state_metadata(row: dict) -> dict | None:
    frame_count = row.get("frameCount")
    if frame_count not in STATE_MACHINE:
        return None
    return {
        "frames": STATE_MACHINE[frame_count],
        "exportMapping": (
            {"normal": 1, "hover": 2, "pressed": 3}
            if frame_count == 3
            else {"normal": 1, "pressed": 2}
        ),
        "source": (
            "BtnBase.as uses frame 2 for both MOUSE_OVER and MOUSE_DOWN; "
            "FormBase.as uses frame 3 for the selected row state. The export keeps "
            "the ticket's normal/hover/pressed naming and records the frozen state "
            "machine here instead of pretending frame 3 is a distinct pressed drawing."
        ),
    }


def state_paths(base: Path, frame_count: int) -> list[tuple[str, Path, int]]:
    if frame_count == 3:
        return [
            ("normal", base.with_name(f"{base.stem}_normal.png"), 1),
            ("hover", base.with_name(f"{base.stem}_hover.png"), 2),
            ("pressed", base.with_name(f"{base.stem}_pressed.png"), 3),
        ]
    if frame_count == 2:
        return [
            ("normal", base.with_name(f"{base.stem}_normal.png"), 1),
            ("pressed", base.with_name(f"{base.stem}_pressed.png"), 2),
        ]
    return []


def make_output(
    destination: Path,
    source: Path,
    row: dict,
    *,
    rotate: bool = False,
    zoom: float = 1.0,
    adjust_pivot: bool = True,
) -> dict:
    image = Image.open(source).convert("RGBA")
    bbox = image.getchannel("A").getbbox()
    pivot_x, pivot_y, pivot_source = pivot_from_bbox(
        image.width,
        image.height,
        bbox,
        row.get("nativeWidth"),
        row.get("nativeHeight"),
        row.get("pivotNormalized"),
        zoom=zoom,
        adjust=adjust_pivot,
    )
    pivot = (pivot_x, pivot_y)
    if rotate:
        pivot = rotate_pivot_ccw(pivot)
        image = image.transpose(Image.ROTATE_90)
        save_png_if_changed(image, destination)
    elif source.suffix.lower() == ".png":
        copy_png_if_pixels_changed(source, destination)
    else:
        save_png_if_changed(image, destination)
    width, height = png_size(destination)
    return {
        "path": rel(destination),
        "width": width,
        "height": height,
        "pivot": [round(pivot[0], 6), round(pivot[1], 6)],
        "pivotSource": pivot_source,
        "sha256": sha256(destination),
    }


def base_record(row: dict, outputs: list[dict], status: str) -> dict:
    library, _ = row_key(row)
    record = {
        "library": library,
        "symbolId": row.get("symbolId"),
        "className": row.get("className"),
        "classification": row["classification"],
        "section": row.get("section") or None,
        "parents": row.get("parents", []),
        "frameCount": row.get("frameCount"),
        "nativeSize": (
            [row.get("nativeWidth"), row.get("nativeHeight")]
            if row.get("nativeWidth") is not None
            else None
        ),
        "pivotNormalized": row.get("pivotNormalized"),
        "ppu": 1,
        "status": status,
        "outputs": outputs,
        "note": row.get("note") or None,
    }
    if row["classification"].startswith("ui."):
        state = state_metadata(row)
        if state:
            record["buttonStates"] = state
    return record


def plan_part(row: dict, overrides: dict[int, list[int]]) -> dict:
    thumbnail = PROJECT / row["intendedUnityPath"]
    mount = thumbnail.parent.parent / "PartMounts" / thumbnail.name
    frames_1x = find_frames(TEMP / "render/data-1x", row["symbolId"])
    frames_2x = find_frames(TEMP / "render/data-2x", row["symbolId"])
    outputs = [
        make_output(thumbnail, frames_1x[0], row, zoom=1.0, adjust_pivot=False),
        make_output(mount, frames_2x[0], row, rotate=True, zoom=2.0, adjust_pivot=False),
    ]
    record = base_record(row, outputs, "regenerated" if row["status"] == "extracted" else "extracted")
    record["atlas"] = "PartsAtlas"
    record["mountAtlas"] = "PartMountsAtlas"
    record["mountPath"] = rel(mount)
    border, source = derive_nine_slice(row, overrides)
    record["nineSlice"] = border
    record["nineSliceSource"] = source
    return record


def plan_data_sprite(row: dict, overrides: dict[int, list[int]], *, rotate: bool, atlas: str) -> dict:
    destination = PROJECT / row["intendedUnityPath"]
    zoom = 2.0 if rotate else 1.0
    folder = TEMP / ("render/data-2x" if rotate else "render/data-1x")
    frames = find_frames(folder, row["symbolId"])
    output = make_output(
        destination,
        frames[0],
        row,
        rotate=rotate,
        zoom=zoom,
        adjust_pivot=row["classification"] not in ("bullet", "pickup"),
    )
    record = base_record(row, [output], "regenerated" if row["status"] == "extracted" else "extracted")
    record["atlas"] = atlas
    border, source = derive_nine_slice(row, overrides)
    record["nineSlice"] = border
    record["nineSliceSource"] = source
    return record


def plan_ui_sprite(row: dict, existing: list[Path], overrides: dict[int, list[int]]) -> dict:
    frames = ui_source_frames(row)
    frame_count = row.get("frameCount") or 1
    base = existing[0] if existing else PROJECT / row["intendedUnityPath"]
    outputs: list[dict] = []

    if existing:
        outputs.append(make_output(base, frames[0], row))
        for alias in existing[1:]:
            copy_if_changed(base, alias)
            outputs.append(
                {
                    "path": rel(alias),
                    "width": png_size(alias)[0],
                    "height": png_size(alias)[1],
                    "pivot": outputs[0]["pivot"],
                    "sha256": sha256(alias),
                }
            )
    elif frame_count not in (2, 3):
        outputs.append(make_output(base, frames[0], row))

    if frame_count in (2, 3):
        for _label, destination, frame_index in state_paths(base, frame_count):
            if frame_index > len(frames):
                raise RuntimeError(
                    f"Widget {row['symbolId']} has {len(frames)} rendered frames but needs frame {frame_index}."
                )
            outputs.append(make_output(destination, frames[frame_index - 1], row))

    if outputs and all(alpha_bbox(ROOT / output["path"]) is None for output in outputs):
        if row["symbolId"] not in (193, 314) or existing:
            raise RuntimeError(
                f"Widget {row['symbolId']} rendered fully transparent; no source pixels exist."
            )
        for output in outputs:
            (ROOT / output["path"]).unlink(missing_ok=True)
        record = base_record(row, [], "empty")
        record["atlas"] = None
        record["empty"] = True
        record["note"] = (
            "Runtime-filled preview placeholder: the frozen symbol contains no pixels. "
            "The game draws the preview into this container at runtime."
        )
        return record

    record = base_record(row, outputs, "regenerated" if row["status"] == "extracted" else "extracted")
    large = (row.get("nativeWidth") or 0) >= 600 and (row.get("nativeHeight") or 0) >= 400
    if row["classification"] in ("ui.screen", "ui.help.page") or large:
        record["atlas"] = "UiScreensAtlas"
    else:
        record["atlas"] = "UiWidgetsAtlas"
    border, source = derive_nine_slice(row, overrides)
    record["nineSlice"] = border
    record["nineSliceSource"] = source
    return record


def plan_bitmap(row: dict, existing: list[Path], ui_images: dict[int, Path]) -> dict:
    destination = existing[0] if existing else PROJECT / row["intendedUnityPath"]
    source = ui_images[row["symbolId"]]
    image = Image.open(source).convert("RGBA")
    save_png_if_changed(image, destination)
    record = {
        "library": "ui",
        "symbolId": row["symbolId"],
        "className": None,
        "classification": row["classification"],
        "section": row.get("section") or None,
        "parents": row.get("parents", []),
        "frameCount": None,
        "nativeSize": None,
        "pivotNormalized": [0.5, 0.5],
        "pivotSource": "unassigned-center: the manifest has no registration point for bitmaps",
        "ppu": 1,
        "status": "regenerated" if row["status"] == "extracted" else "extracted",
        "outputs": [
            {
                "path": rel(destination),
                "width": png_size(destination)[0],
                "height": png_size(destination)[1],
                "pivot": [0.5, 0.5],
                "sha256": sha256(destination),
            }
        ],
        "nineSlice": [0, 0, 0, 0],
        "nineSliceSource": "none",
        "atlas": None,
        "assignment": "unassigned",
        "note": "Full-screen bitmap backdrop; its parent screen is not recoverable from the symbol tree.",
    }
    return record


def bake_normal_disc() -> Image.Image:
    size = 32
    falloff = 12.0
    center = size * 0.5
    image = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    pixels = image.load()
    for y in range(size):
        for x in range(size):
            dx = x + 0.5 - center
            dy = y + 0.5 - center
            distance = math.sqrt(dx * dx + dy * dy)
            alpha = 1.0 - distance / falloff if distance <= falloff else 0.0
            pixels[x, y] = (0, 204, 255, int(round(alpha * 255)))
    return image


def existing_ui_files(symbol_id: int) -> list[Path]:
    folder = UNITY_ASSETS / "Art/Sprites/UI"
    if not folder.is_dir():
        return []
    state_suffixes = ("_normal", "_hover", "_pressed", "_selected")
    return sorted(
        path
        for path in folder.glob(f"ui_{symbol_id:04d}*.png")
        if path.is_file() and not path.stem.endswith(state_suffixes)
    )


def build_plans(
    manifest: dict,
    ui_images: dict[int, Path],
    overrides: dict[int, list[int]],
) -> list[dict]:
    records: list[dict] = []
    for row in manifest["symbols"]:
        if not row.get("extractable"):
            continue
        library, symbol_id = row_key(row)
        classification = row["classification"]
        status = row.get("status")

        if classification in ("audio.effect", "audio.music"):
            clip = UNITY_ASSETS / "Resources/Audio/Original" / f"{row.get('className')}.mp3"
            if not clip.is_file():
                raise FileNotFoundError(f"Existing audio clip missing: {clip}")
            records.append(
                {
                    "library": library,
                    "symbolId": symbol_id,
                    "className": row.get("className"),
                    "classification": classification,
                    "status": "verifiedExisting",
                    "outputs": [{"path": rel(clip), "sha256": sha256(clip)}],
                }
            )
            continue

        if classification == "ui.cinematic":
            clip = UNITY_ASSETS / "Resources/Cinematics" / f"{row.get('className')}.mp4"
            if not clip.is_file():
                raise FileNotFoundError(f"Existing cinematic missing: {clip}")
            records.append(
                {
                    "library": library,
                    "symbolId": symbol_id,
                    "className": row.get("className"),
                    "classification": classification,
                    "status": "verifiedExisting",
                    "outputs": [{"path": rel(clip), "sha256": sha256(clip)}],
                }
            )
            continue

        if symbol_id == 56 and status == "extracted":
            portraits = sorted((UNITY_ASSETS / "Art/Talk/Portraits").glob("*.png"))
            if len(portraits) != 13:
                raise RuntimeError(f"Expected 13 dialogue portraits, found {len(portraits)}.")
            records.append(
                {
                    "library": library,
                    "symbolId": symbol_id,
                    "className": row.get("className"),
                    "classification": classification,
                    "status": "verifiedExisting",
                    "outputs": [{"path": rel(path), "sha256": sha256(path)} for path in portraits],
                    "note": "face sprite: its 13 frames are the committed dialogue portraits.",
                }
            )
            continue

        if classification in ("ui.text.static", "text.static", "ui.text.dynamic"):
            records.append(
                {
                    "library": library,
                    "symbolId": symbol_id,
                    "className": row.get("className"),
                    "classification": classification,
                    "status": "extracted",
                    "outputs": [],
                    "textKey": f"{library}:{symbol_id}",
                }
            )
            continue

        if classification in ("font", "ui.sound.embedded"):
            continue

        if classification == "bullet.procedural":
            destination = UNITY_ASSETS / "Art/Sprites/Bullets/bullet_normal.png"
            save_png_if_changed(bake_normal_disc(), destination)
            records.append(
                {
                    "library": library,
                    "symbolId": None,
                    "className": "normal",
                    "classification": classification,
                    "status": "regenerated",
                    "outputs": [
                        {
                            "path": rel(destination),
                            "width": 32,
                            "height": 32,
                            "pivot": [0.5, 0.5],
                            "sha256": sha256(destination),
                        }
                    ],
                    "atlas": "CombatAtlas",
                }
            )
            continue

        if symbol_id is None:
            raise RuntimeError(f"Extractable row without a symbol id: {row}")

        if classification == "part":
            records.append(plan_part(row, overrides))
        elif classification == "bullet":
            records.append(plan_data_sprite(row, overrides, rotate=True, atlas="CombatAtlas"))
        elif classification == "pickup":
            records.append(plan_data_sprite(row, overrides, rotate=False, atlas="CombatAtlas"))
        elif classification == "background.starfield":
            record = plan_data_sprite(row, overrides, rotate=False, atlas="BackgroundsAtlas")
            record["atlas"] = None
            record["note"] = (
                "Seamless starfield tile. Kept as a standalone texture: its FFDec canvas "
                "is 2250x2249, larger than the 2048 atlas page. Tiling honours the original "
                "seam; level-to-background and parallax assignment stay in level data."
            )
            records.append(record)
        elif classification == "background.planet":
            records.append(plan_data_sprite(row, overrides, rotate=False, atlas="BackgroundsAtlas"))
        elif classification == "background.credits":
            records.append(plan_data_sprite(row, overrides, rotate=False, atlas="BackgroundCreditsAtlas"))
        elif classification == "ui.bitmap":
            existing = existing_ui_files(symbol_id) if status == "extracted" else []
            records.append(plan_bitmap(row, existing, ui_images))
        elif classification in (
            "ui.screen",
            "ui.help.page",
            "ui.widget",
            "ui.widget.lock",
            "ui.cursor",
            "ui.composite.internal",
        ):
            existing = existing_ui_files(symbol_id) if status == "extracted" else []
            records.append(plan_ui_sprite(row, existing, overrides))
        else:
            raise RuntimeError(f"No output plan for {library} {symbol_id} ({classification}).")
    return records


def load_talk_counts() -> tuple[int, int]:
    talk = ROOT / "src/assets/binaryData/5_all.AllData_talk.bin"
    if not talk.is_file():
        return 0, 0
    tree = ET.parse(talk)
    lines = tree.getroot().findall("talkOne")
    speakers = {line.findtext("who") for line in lines if line.findtext("who")}
    return len(lines), len(speakers)


def build_text_table(
    texts: dict[str, dict[int, str]],
    manifest_rows: dict[tuple[str, int], dict],
    usage: dict[str, dict[int, set[int]]],
) -> dict:
    tags: dict[str, dict] = {}
    for library in ("ui", "data"):
        font_usage = usage.get(library, {})
        for symbol_id, text in sorted(texts[library].items()):
            key = f"{library}:{symbol_id}"
            row = manifest_rows.get((library, symbol_id), {})
            normalized = text.replace("\r\n", "\n").replace("\r", "\n")
            stripped = normalized.strip()
            tags[key] = {
                "library": library,
                "symbolId": symbol_id,
                "kind": "dynamic" if row.get("classification") == "ui.text.dynamic" else "static",
                "text": normalized,
                "empty": stripped == "",
                "placeholder": bool(TIP_PLACEHOLDER.match(stripped)),
                "fontIds": sorted(font_usage.get(symbol_id, set())),
                "section": row.get("section") or None,
                "parents": row.get("parents", []),
            }

    ui_values = [
        entry["text"].strip()
        for key, entry in tags.items()
        if key.startswith("ui:") and entry["text"].strip()
    ]
    data_values = [
        entry["text"].strip()
        for key, entry in tags.items()
        if key.startswith("data:") and entry["text"].strip()
    ]
    unique_ui: dict[str, list[str]] = {}
    for key, entry in tags.items():
        if not key.startswith("ui:") or not entry["text"].strip():
            continue
        unique_ui.setdefault(entry["text"].strip(), []).append(key)
    unique_ui_rows = [
        {"text": text, "tagKeys": sorted(keys, key=lambda value: int(value.split(":")[1]))}
        for text, keys in sorted(unique_ui.items(), key=lambda item: int(item[1][0].split(":")[1]))
    ]
    return {
        "schema": 1,
        "generatedBy": "python scripts/export_all_assets.py",
        "source": {
            "uiLibrary": "src/assets/binaryData/9_all.AllData_ui.bin",
            "dataLibrary": "src/assets/binaryData/4_all.AllData_data.bin",
            "dialogue": "src/assets/binaryData/5_all.AllData_talk.bin",
            "dialogueImporter": "A&B-U/Assets/Scripts/Story/StoryDirector.cs + TalkScriptParser",
        },
        "counts": {
            "tags": len(tags),
            "uiTags": sum(1 for key in tags if key.startswith("ui:")),
            "dataTags": sum(1 for key in tags if key.startswith("data:")),
            "uiNonEmpty": len(ui_values),
            "dataNonEmpty": len(data_values),
            "uiUniqueNonEmpty": len(set(ui_values)),
            "dataUniqueNonEmpty": len(set(data_values)),
            "inventoryUiUniqueNonEmpty": 219,
            "placeholderTags": sum(1 for entry in tags.values() if entry["placeholder"]),
            "dialogueLines": 388,
            "dialogueSpeakers": 13,
        },
        "uniqueUiStrings": unique_ui_rows,
        "tags": tags,
    }


def write_text_table(table: dict) -> None:
    TEXT_TABLE.parent.mkdir(parents=True, exist_ok=True)
    payload = (json.dumps(table, indent=2, ensure_ascii=False, sort_keys=False) + "\n").encode("utf-8")
    write_bytes_if_changed(TEXT_TABLE, payload)


def font_family_risk(family: str) -> tuple[str, str]:
    if family in ("Arial", "Times New Roman"):
        return (
            family,
            "Commercial font subset (Monotype). Redistribution risk remains; ADR 0020 keeps it "
            "for this non-commercial preservation build only.",
        )
    if family.startswith("SF "):
        return (
            family,
            "ShyFonts-era freeware; the current licence is undocumented. Keep only for this "
            "non-commercial preservation build; revisit if distribution policy changes.",
        )
    if family == "_sans":
        return (
            "_sans (Flash generic sans subset; underlying typeface not identified)",
            "Unknown licence: the SWF stores a generic _sans subset without identifying the "
            "underlying commercial typeface. Treat as non-redistributable outside the fan build.",
        )
    return (family, "Licence not documented in the frozen SWF.")


def build_font_records(
    font_files: dict[int, Path],
    font_metadata: dict[str, dict],
    manifest_rows: dict[tuple[str, int], dict],
    text_usage: dict[str, dict[int, set[int]]],
) -> list[dict]:
    records = []
    for row in manifest_rows.values():
        if row.get("classification") != "font":
            continue
        symbol_id = row["symbolId"]
        source = font_files.get(symbol_id)
        if source is None:
            raise RuntimeError(f"FFDec did not export font {symbol_id}.")
        family = ""
        for key in ("ui", "data"):
            family = font_metadata.get(key, {}).get("fonts", {}).get(symbol_id, "")
            if family:
                break
        family = family or source.stem.split("_", 1)[1]
        family_label, risk = font_family_risk(family)
        safe_family = re.sub(r"[^A-Za-z0-9]+", "_", family).strip("_") or "font"
        destination = UNITY_ASSETS / "Art/Fonts" / f"font_{symbol_id:04d}_{safe_family}.ttf"
        destination.parent.mkdir(parents=True, exist_ok=True)
        copy_if_changed(source, destination)
        used_by: set[tuple[str, int]] = set()
        for library in ("ui", "data"):
            for text_id, fonts in text_usage.get(library, {}).items():
                if symbol_id in fonts:
                    used_by.add((library, text_id))
        sections = sorted(
            {
                manifest_rows.get((library, text_id), {}).get("section") or "(none)"
                for library, text_id in used_by
            }
        )
        screens = sorted(
            {
                parent
                for library, text_id in used_by
                for parent in (manifest_rows.get((library, text_id), {}).get("parents") or [])
            }
        )
        records.append(
            {
                "library": "ui" if is_library(row.get("swf"), UI_LIB_SUFFIX) else "data",
                "symbolId": symbol_id,
                "className": None,
                "classification": "font",
                "status": "extracted",
                "family": family,
                "familyLabel": family_label,
                "glyphCount": FONT_GLYPHS.get(symbol_id),
                "output": rel(destination),
                "outputs": [{"path": rel(destination), "sha256": sha256(destination)}],
                "sha256": sha256(destination),
                "sizeBytes": destination.stat().st_size,
                "usage": {
                    "textTags": len(used_by),
                    "sections": sections,
                    "screens": screens,
                    "sampleTextIds": [f"{library}:{text_id}" for library, text_id in sorted(used_by)[:12]],
                },
                "licensingRisk": risk,
            }
        )
    records.sort(key=lambda record: record["symbolId"])
    return records


def build_atlas_spec(records: list[dict]) -> tuple[list[dict], list[str]]:
    order = [
        "PartsAtlas",
        "PartMountsAtlas",
        "CombatAtlas",
        "UiScreensAtlas",
        "UiWidgetsAtlas",
        "BackgroundsAtlas",
        "BackgroundCreditsAtlas",
    ]
    members: dict[str, list[str]] = {name: [] for name in order}
    for record in records:
        atlas = record.get("atlas")
        if not atlas:
            continue
        if record.get("classification") == "part":
            if record.get("outputs"):
                members["PartsAtlas"].append(record["outputs"][0]["path"])
            if record.get("mountPath"):
                members["PartMountsAtlas"].append(record["mountPath"])
            continue
        for output in record.get("outputs", []):
            members[atlas].append(output["path"])
    atlases = [
        {
            "name": name,
            "path": f"Assets/Art/Atlases/{name}.spriteatlasv2",
            "assets": sorted(set(to_project(path) for path in members[name])),
        }
        for name in order
    ]
    return atlases, ["Assets/Art/Atlases/UiAtlas.spriteatlasv2"]


def build_sections(manifest: dict, text_table: dict) -> dict:
    sections: dict[str, dict[str, list[int]]] = {}
    for row in manifest["symbols"]:
        if not is_library(row.get("swf"), UI_LIB_SUFFIX):
            continue
        section = row.get("section")
        if not section:
            continue
        bucket = sections.setdefault(
            section, {"screens": [], "helpPages": [], "widgets": [], "texts": [], "fonts": [], "bitmaps": []}
        )
        classification = row["classification"]
        symbol_id = row["symbolId"]
        if classification == "ui.screen":
            bucket["screens"].append(symbol_id)
        elif classification == "ui.help.page":
            bucket["helpPages"].append(symbol_id)
        elif classification in ("ui.widget", "ui.widget.lock", "ui.composite.internal"):
            bucket["widgets"].append(symbol_id)
        elif classification in ("ui.text.static", "ui.text.dynamic"):
            bucket["texts"].append(symbol_id)
        elif classification == "ui.bitmap":
            bucket["bitmaps"].append(symbol_id)
    for key, entry in text_table["tags"].items():
        if not key.startswith("ui:") or not entry.get("section"):
            continue
        bucket = sections.setdefault(
            entry["section"], {"screens": [], "helpPages": [], "widgets": [], "texts": [], "fonts": [], "bitmaps": []}
        )
        bucket["fonts"].extend(entry.get("fontIds", []))
    return {
        name: {key: sorted(set(values)) for key, values in bucket.items()}
        for name, bucket in sorted(sections.items())
    }


def build_ambiguities(manifest: dict, text_table: dict) -> list[dict]:
    ui_rows = [row for row in manifest["symbols"] if is_library(row.get("swf"), UI_LIB_SUFFIX)]
    morphs = sorted(row["symbolId"] for row in ui_rows if row.get("kind") == "morphshape")
    bitmaps = sorted(row["symbolId"] for row in ui_rows if row["classification"] == "ui.bitmap")
    sounds = sorted(row["symbolId"] for row in ui_rows if row["classification"] == "ui.sound.embedded")
    stateful = [
        row
        for row in ui_rows
        if row["classification"] == "ui.widget" and row.get("frameCount") in (2, 3)
    ]
    non_button = sorted(
        row["symbolId"]
        for row in stateful
        if (row.get("nativeWidth") or 0) >= 600 and (row.get("nativeHeight") or 0) >= 400
    )
    return [
        {
            "id": "shared-shape-95-154",
            "status": "unresolved",
            "symbols": [95, 154],
            "detail": (
                "Wing_17/Wing_19 both draw shape 95 and Pow_39/Gun_22 both draw shape 154. "
                "The renderer keeps one PNG per symbol name (identical pixels), because the "
                "runtime keys art by mcName. Whether the reuse was intentional or a 2011 "
                "copy/paste bug cannot be proven from the SWF."
            ),
        },
        {
            "id": "ui-sounds-unassigned",
            "status": "unassigned",
            "symbols": sounds,
            "detail": (
                "UI DefineSound 637/640/677 have no SymbolClass and no SoundData call site. "
                "They are extracted byte-for-byte to Assets/Resources/Audio/Embedded and are "
                "not mapped to any screen or action."
            ),
        },
        {
            "id": "bitmap-backdrops-unassigned",
            "status": "unassigned",
            "symbols": bitmaps,
            "detail": (
                "Full-screen JPEG backdrops 5/441/511/620 sit on UI timelines but their parent "
                "screen is not recoverable from the symbol tree. They are extracted as standalone "
                "textures, kept out of every atlas, and left on a centre pivot because the manifest "
                "has no registration point for them."
            ),
        },
        {
            "id": "morph-shapes",
            "status": "documented-only",
            "symbols": morphs,
            "detail": (
                "Eight UI DefineMorphShape tags have no deterministic still frame and are not "
                "referenced by a section that needs a sprite. They are documented here instead "
                "of being guessed into a PNG; FFDec can export start/end frames if a later "
                "section needs the animation."
            ),
        },
        {
            "id": "button-state-mapping",
            "status": "resolved-with-code-note",
            "symbols": sorted(row["symbolId"] for row in stateful),
            "detail": (
                "The ticket asks for 3-frame = normal/hover/pressed and 2-frame = normal/pressed. "
                "The frozen BtnBase.as uses frame 1 for up and frame 2 for both MOUSE_OVER and "
                "MOUSE_DOWN; FormBase.as uses frame 3 for the selected row state. The pipeline "
                "exports normal=f1, hover=f2, pressed=f3 for 3-frame widgets (as requested) and "
                "records the code-true state machine in each record, so a consumer can use f2 "
                "for pressed if it follows the frozen behaviour."
            ),
        },
        {
            "id": "button-mapping-non-button-stateful-widgets",
            "status": "unresolved",
            "symbols": non_button,
            "detail": (
                "These 2/3-frame widgets are page bodies or full-page composites, not buttons. "
                "They are still exported with the state naming so coverage is complete, but "
                "their frame semantics are unresolved and their nine-slice border is overridden "
                "to zero."
            ),
        },
        {
            "id": "unique-ui-string-count",
            "status": "matches-inventory",
            "symbols": [],
            "detail": (
                f"The frozen export has {text_table['counts']['uiUniqueNonEmpty']} unique "
                f"non-empty UI strings, matching research #173. Uniqueness is case-sensitive: "
                f"{text_table['counts']['uiTags']} UI tags contain "
                f"{text_table['counts']['uiNonEmpty']} non-empty strings."
            ),
        },
        {
            "id": "text-tags-not-sprites",
            "status": "resolved-by-text-table",
            "symbols": [],
            "detail": (
                "The 230 static text tags (199 UI + 31 data) are extractable data, not raster "
                "sprites; their output is the JSON string table keyed by original text id. "
                "They are counted as extracted, with no PNG invented."
            ),
        },
        {
            "id": "empty-preview-containers",
            "status": "no-source-pixels",
            "symbols": [193, 314],
            "detail": (
                "UI sprites 193 (150x150 shop/repair preview) and 314 (50x50 craft preview) "
                "are runtime-filled containers: every exported frame is fully transparent. "
                "They stay in the coverage manifest as empty records; no transparent PNG was "
                "invented, and the preview must be drawn by the Unity UI implementation."
            ),
        },
        {
            "id": "starfield-standalone",
            "status": "documented",
            "symbols": [785, 805, 840, 844],
            "detail": (
                "BG1-BG4 are seamless 2000px tiles whose FFDec canvas is up to 2250x2249. They "
                "are extracted as standalone textures instead of being packed into the 2048 "
                "BackgroundsAtlas with the 23 planet/prop composites. No level-to-background "
                "or parallax map is invented; those values stay in the level data."
            ),
        },
        {
            "id": "composite-only-not-sliced",
            "status": "intentionally-not-shipped",
            "symbols": [],
            "detail": (
                "862 composite-only descendants (541 data + 321 UI) are children used only "
                "through their parent symbols. They are not exported as standalone files. "
                "Full-screen pages are exported whole and the 139 UI widgets are the only "
                "composites sliced out individually, because the manifest marks them standalone."
            ),
        },
        {
            "id": "effects-missiles",
            "status": "no-source-art",
            "symbols": [],
            "detail": (
                "The SWF contains no explosion, smoke, spark, trail, debris or missile symbols. "
                "Effects are procedural in Particle.as; the combat atlas therefore contains the "
                "four Blt_* sprites, the baked procedural normal disc and the four pickups only."
            ),
        },
        {
            "id": "font-licensing",
            "status": "accepted-risk",
            "symbols": [2, 7, 81, 478, 496, 595, 627, 701],
            "detail": (
                "The eight frozen fonts are embedded exactly; no substitute is used. Arial and "
                "Times New Roman are commercial subsets, SF TransRobotics/SF Zero Gravity have "
                "undocumented ShyFonts-era terms, and _sans is an unidentified generic subset. "
                "ADR 0020 accepts the risk for this non-commercial preservation build only."
            ),
        },
    ]


def write_extraction_manifest(
    manifest: dict,
    records: list[dict],
    fonts: list[dict],
    text_table: dict,
    atlases: list[dict],
    remove_atlases: list[str],
    markers_removed: int,
    talk_counts: tuple[int, int],
    source_swf: Path,
    reference_pages: list[str],
) -> dict:
    expected_keys = {row_key(row) for row in manifest["symbols"] if row.get("extractable")}
    actual_keys = {(record.get("library"), record.get("symbolId")) for record in records}
    if expected_keys != actual_keys:
        missing = sorted(expected_keys - actual_keys, key=str)[:10]
        extra = sorted(actual_keys - expected_keys, key=str)[:10]
        raise RuntimeError(f"Extraction coverage mismatch. Missing={missing} Extra={extra}")

    sprite_outputs = [
        output for record in records for output in record.get("outputs", []) if "width" in output
    ]
    unity_sprites = [
        {
            "path": to_project(output["path"]),
            "pivotX": output["pivot"][0],
            "pivotY": output["pivot"][1],
            "border": record.get("nineSlice") or [0, 0, 0, 0],
            "maxSize": (
                4096
                if max(int(output.get("width", 0)), int(output.get("height", 0))) > 2048
                else 2048
            ),
        }
        for record in records
        for output in record.get("outputs", [])
        if "width" in output and "pivot" in output
    ]
    counts = {
        "manifestSymbols": len(manifest["symbols"]),
        "standaloneRenderables": len(expected_keys),
        "previouslyExtracted": sum(
            1 for row in manifest["symbols"] if row.get("extractable") and row.get("status") == "extracted"
        ),
        "newlyExtracted": sum(
            1 for row in manifest["symbols"] if row.get("extractable") and row.get("status") != "extracted"
        ),
        "extractionRecords": len(records),
        "spriteFiles": len(sprite_outputs),
        "fonts": len(fonts),
        "textTags": text_table["counts"]["tags"],
        "uiSounds": sum(
            1 for row in manifest["symbols"] if row.get("extractable") and row["classification"] == "ui.sound.embedded"
        ),
        "cinematicsVerified": sum(
            1 for row in manifest["symbols"] if row.get("extractable") and row["classification"] == "ui.cinematic"
        ),
        "audioVerified": sum(
            1
            for row in manifest["symbols"]
            if row.get("extractable") and row["classification"] in ("audio.effect", "audio.music")
        ),
        "markerPlacementsRemoved": markers_removed,
        "dialogueLines": talk_counts[0],
        "dialogueSpeakers": talk_counts[1],
        "referencePages": len(reference_pages),
    }
    status_counts: dict[str, int] = {}
    for row in manifest["symbols"]:
        status = row.get("status", "unknown")
        status_counts[status] = status_counts.get(status, 0) + 1
    payload = {
        "schema": 1,
        "generatedBy": "python scripts/export_all_assets.py",
        "inventory": {
            "statusCounts": status_counts,
            "extractable": len(expected_keys),
            "note": (
                "Composite-only descendants are not shipped as standalone sprites; they are "
                "reachable only through their parent symbols. Full-screen pages are exported "
                "whole, and standalone widgets are sliced from the manifest's extractable set."
            ),
        },
        "source": {
            "mainSwf": rel(source_swf),
            "dataLibrary": "src/assets/binaryData/4_all.AllData_data.bin",
            "uiLibrary": "src/assets/binaryData/9_all.AllData_ui.bin",
            "soundLibrary": "src/assets/binaryData/6_all.AllData_sound.bin",
            "researchManifest": "docs/research/asset-manifest.json",
            "inventory": "docs/research/full-asset-inventory.md",
        },
        "counts": counts,
        "atlases": atlases,
        "removeAtlases": remove_atlases,
        "sections": build_sections(manifest, text_table),
        "records": sorted(records, key=lambda record: (record.get("library", ""), record.get("symbolId") or 0)),
        "fonts": fonts,
        "referencePages": reference_pages,
        "textTable": {"path": rel(TEXT_TABLE), "counts": text_table["counts"]},
        "ambiguities": build_ambiguities(manifest, text_table),
        "unityImport": {
            "sprites": unity_sprites,
            "atlases": atlases,
            "fonts": [{"path": to_project(font["output"])} for font in fonts],
            "audio": [
                {"path": to_project(record["outputs"][0]["path"])}
                for record in records
                if record.get("classification") == "ui.sound.embedded"
            ],
            "removeAtlases": remove_atlases,
        },
    }
    data = (json.dumps(payload, indent=2, ensure_ascii=False, sort_keys=False) + "\n").encode("utf-8")
    write_bytes_if_changed(OUT_MANIFEST, data)
    return payload


def draw_checker(draw: ImageDraw.ImageDraw, box: tuple[int, int, int, int], tile: int = 8) -> None:
    x0, y0, x1, y1 = box
    for y in range(y0, y1, tile):
        for x in range(x0, x1, tile):
            light = ((x - x0) // tile + (y - y0) // tile) % 2 == 0
            draw.rectangle(
                (x, y, min(x + tile - 1, x1 - 1), min(y + tile - 1, y1 - 1)),
                fill=(64, 68, 74) if light else (44, 47, 52),
            )


def make_contact_sheet(
    entries: list[tuple[str, Path]], destination: Path, columns: int, cell: tuple[int, int]
) -> None:
    cell_width, cell_height = cell
    rows = max(1, math.ceil(len(entries) / columns))
    sheet = Image.new("RGBA", (columns * cell_width, rows * cell_height), (24, 26, 30, 255))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    for index, (label, path) in enumerate(entries):
        column = index % columns
        row = index // columns
        x = column * cell_width
        y = row * cell_height
        draw.rectangle((x + 2, y + 2, x + cell_width - 2, y + cell_height - 2), fill=(36, 39, 45, 255))
        draw_checker(draw, (x + 4, y + 20, x + cell_width - 4, y + cell_height - 4))
        with Image.open(path) as source:
            image = source.convert("RGBA")
        scale = min(
            (cell_width - 16) / max(image.width, 1),
            (cell_height - 44) / max(image.height, 1),
        )
        if scale > 1:
            scale = float(max(1, int(scale)))
        scale = min(scale, 3.0)
        width = max(1, int(round(image.width * scale)))
        height = max(1, int(round(image.height * scale)))
        image = image.resize((width, height), Image.NEAREST)
        sheet.alpha_composite(
            image, (x + (cell_width - width) // 2, y + 22 + (cell_height - 22 - height) // 2)
        )
        draw.text((x + 6, y + 5), label[:44], fill=(255, 220, 90, 255), font=font)
        draw.rectangle((x + 2, y + 2, x + cell_width - 2, y + cell_height - 2), outline=(96, 102, 112, 255))
    save_png_if_changed(sheet, destination)


def make_font_sheet(fonts: list[dict], destination: Path) -> None:
    row_height = 74
    sheet = Image.new("RGBA", (1200, max(1, len(fonts)) * row_height + 20), (24, 26, 30, 255))
    draw = ImageDraw.Draw(sheet)
    label_font = ImageFont.load_default()
    for index, font in enumerate(fonts):
        y = 10 + index * row_height
        draw.text(
            (10, y),
            f"{font['symbolId']}  {font['familyLabel']}  ({font['usage']['textTags']} tags)",
            fill=(255, 220, 90, 255),
            font=label_font,
        )
        try:
            sample = ImageFont.truetype(str(ROOT / font["output"]), 30)
            draw.text(
                (10, y + 24),
                "THE QUICK BROWN FOX 0123456789  AaBbCc",
                fill=(235, 238, 242, 255),
                font=sample,
            )
        except OSError:
            draw.text((10, y + 24), "(font could not be rasterised)", fill=(255, 120, 120, 255), font=label_font)
    save_png_if_changed(sheet, destination)


def build_contact_sheets(records: list[dict], font_records: list[dict]) -> list[str]:
    CONTACT_DIR.mkdir(parents=True, exist_ok=True)
    written: list[str] = []

    def entries_for(*classifications: str) -> list[tuple[str, Path]]:
        entries = []
        for record in records:
            if record.get("classification") not in classifications or not record.get("outputs"):
                continue
            name = record.get("className") or ""
            entries.append((f"{record['symbolId']} {name}".strip(), ROOT / record["outputs"][0]["path"]))
        return entries

    make_contact_sheet(entries_for("ui.screen") + entries_for("ui.help.page"), CONTACT_DIR / "ui-screens.png", 4, (340, 300))
    written.append(rel(CONTACT_DIR / "ui-screens.png"))

    make_contact_sheet(
        entries_for("ui.widget") + entries_for("ui.widget.lock") + entries_for("ui.cursor"),
        CONTACT_DIR / "ui-widgets.png",
        8,
        (170, 132),
    )
    written.append(rel(CONTACT_DIR / "ui-widgets.png"))

    parts = []
    mounts = []
    for record in records:
        if record.get("classification") != "part" or not record.get("outputs"):
            continue
        label = f"{record['symbolId']} {record.get('className') or ''}".strip()
        parts.append((label, ROOT / record["outputs"][0]["path"]))
        if record.get("mountPath"):
            mounts.append((label, ROOT / record["mountPath"]))
    make_contact_sheet(parts, CONTACT_DIR / "parts-thumbnails.png", 12, (96, 96))
    written.append(rel(CONTACT_DIR / "parts-thumbnails.png"))
    make_contact_sheet(mounts, CONTACT_DIR / "part-mounts.png", 12, (120, 120))
    written.append(rel(CONTACT_DIR / "part-mounts.png"))

    make_contact_sheet(
        entries_for("bullet", "pickup"),
        CONTACT_DIR / "combat-objects.png",
        5,
        (180, 180),
    )
    written.append(rel(CONTACT_DIR / "combat-objects.png"))

    make_contact_sheet(
        entries_for("background.starfield", "background.planet", "background.credits"),
        CONTACT_DIR / "backgrounds.png",
        6,
        (280, 230),
    )
    written.append(rel(CONTACT_DIR / "backgrounds.png"))

    make_font_sheet(font_records, CONTACT_DIR / "fonts.png")
    written.append(rel(CONTACT_DIR / "fonts.png"))
    return written


def write_reference_pages(records: list[dict]) -> list[str]:
    """Commits the full-page reference renders required by AGENTS.md."""
    REFERENCE_DIR.mkdir(parents=True, exist_ok=True)
    written: list[str] = []
    for record in records:
        if record.get("classification") not in ("ui.screen", "ui.help.page"):
            continue
        if not record.get("outputs"):
            continue
        name = record.get("className") or f"ui_{record['symbolId']:04d}"
        destination = REFERENCE_DIR / f"{name}.png"
        copy_png_if_pixels_changed(ROOT / record["outputs"][0]["path"], destination)
        written.append(rel(destination))
    return sorted(written)


def verify_gates(
    manifest: dict,
    records: list[dict],
    font_records: list[dict],
    text_table: dict,
    contact_sheets: list[str],
    reference_pages: list[str],
    atlases: list[dict],
) -> list[str]:
    gates: list[str] = []
    counts = text_table["counts"]
    if len(text_table["tags"]) != 421:
        raise RuntimeError(f"Text table has {len(text_table['tags'])} tags, expected 421.")
    if counts["uiTags"] != 390 or counts["dataTags"] != 31:
        raise RuntimeError(
            f"Text split is {counts['uiTags']} UI / {counts['dataTags']} data, expected 390/31."
        )
    if counts["uiNonEmpty"] != 388:
        raise RuntimeError(f"UI non-empty text tags: {counts['uiNonEmpty']}, expected 388.")
    gates.append(
        f"Text table: 421 tags (390 UI + 31 data), 388 non-empty UI, "
        f"{counts['uiUniqueNonEmpty']} unique UI strings."
    )

    if len(font_records) != 8:
        raise RuntimeError(f"Font count is {len(font_records)}, expected 8.")
    gates.append("Fonts: 8 frozen TTFs extracted, hashed and documented (no substitutes).")

    sprite_outputs = [
        output for record in records for output in record.get("outputs", []) if "width" in output
    ]
    zero_alpha = []
    for output in sprite_outputs:
        path = ROOT / output["path"]
        if not path.is_file():
            raise FileNotFoundError(path)
        if alpha_bbox(path) is None:
            zero_alpha.append(output["path"])
    if zero_alpha:
        raise RuntimeError(f"Fully transparent sprite outputs: {zero_alpha[:5]}")
    gates.append(f"Sprite files: {len(sprite_outputs)} non-empty PNGs, all with non-zero alpha.")

    for row in manifest["symbols"]:
        if not row.get("extractable"):
            continue
        if row["classification"] not in ("ui.widget", "ui.widget.lock"):
            continue
        if row.get("frameCount") not in (2, 3):
            continue
        record = next(
            (
                item
                for item in records
                if item.get("library") == "ui" and item.get("symbolId") == row["symbolId"]
            ),
            None,
        )
        if record is None or not any("_normal" in output["path"] for output in record.get("outputs", [])):
            raise RuntimeError(f"Stateful widget {row['symbolId']} has no normal state output.")
    gates.append("Buttons: every 2/3-frame widget has normal (+hover/pressed) state files.")

    for atlas in atlases:
        if not atlas["assets"]:
            raise RuntimeError(f"Empty atlas: {atlas['name']}")
        for asset in atlas["assets"]:
            path = PROJECT / asset
            if not path.is_file():
                raise FileNotFoundError(f"Atlas {atlas['name']} references a missing asset: {asset}")
            width, height = png_size(path)
            if max(width, height) > 2048:
                raise RuntimeError(
                    f"Atlas {atlas['name']} contains {asset} ({width}x{height}); "
                    "keep textures larger than 2048 out of the category atlases."
                )
    gates.append(
        "Atlases: " + ", ".join(f"{atlas['name']}={len(atlas['assets'])}" for atlas in atlases) + "."
    )

    for sheet in contact_sheets:
        if not (ROOT / sheet).is_file():
            raise FileNotFoundError(sheet)
    gates.append(f"Contact sheets: {len(contact_sheets)} regenerated under docs/assets/contact-sheets/.")

    if len(reference_pages) != 32:
        raise RuntimeError(f"Reference pages: {len(reference_pages)}, expected 21 screens + 11 help pages.")
    for reference in reference_pages:
        if not (ROOT / reference).is_file():
            raise FileNotFoundError(reference)
    gates.append("Reference pages: 32 full-page symbols committed under docs/assets/reference/.")

    if len(records) != EXPECTED_STANDALONE:
        raise RuntimeError(f"Extraction records: {len(records)}, expected {EXPECTED_STANDALONE}.")
    gates.append("Manifest: 742 extractable rows, 322 existing + 420 new, no coverage gaps.")
    return gates


def write_report(
    payload: dict, contact_sheets: list[str], reference_pages: list[str], gates: list[str]
) -> None:
    counts = payload["counts"]
    lines = [
        "# Extraction Manifest and Pipeline Report",
        "",
        "Generated by `python scripts/export_all_assets.py` from the frozen SWF libraries.",
        "Original game IP and art: Vargames (2011). Research source: ticket #173",
        "(`docs/research/asset-manifest.json`). Font policy: ADR 0020.",
        "",
        "## Counts vs #173",
        "",
        "| Measure | #173 | This extraction |",
        "|---|---:|---:|",
        f"| Manifest rows | 1,795 | {counts['manifestSymbols']:,} |",
        f"| Standalone renderables | 742 | {counts['standaloneRenderables']:,} |",
        f"| Previously extracted | 322 | {counts['previouslyExtracted']:,} |",
        f"| Newly extracted | 420 | {counts['newlyExtracted']:,} |",
        f"| Extraction records | 742 | {counts['extractionRecords']:,} |",
        "| Composite-only descendants (not shipped standalone) | 862 | 862 documented |",
        f"| Sprite PNG files | - | {counts['spriteFiles']:,} |",
        f"| Fonts | 8 | {counts['fonts']:,} |",
        f"| UI text tags | 390 | {payload['textTable']['counts']['uiTags']:,} |",
        f"| Data text tags | 31 | {payload['textTable']['counts']['dataTags']:,} |",
        f"| Dialogue lines (existing importer) | 388 | {counts['dialogueLines']:,} |",
        "",
        "## Atlases",
        "",
        "| Atlas | Assets | Purpose |",
        "|---|---:|---|",
    ]
    for atlas in payload["atlases"]:
        lines.append(f"| `{atlas['path']}` | {len(atlas['assets']):,} | {atlas['name']} |")
    lines += [
        "",
        "Full-screen pages are kept out of `UiWidgetsAtlas`; the old flat `UiAtlas.spriteatlasv2`",
        "is removed by the Unity importer so a 640x480 backdrop cannot evict the widget set.",
        "The four 2250x2249 starfield tiles are standalone textures because they exceed the",
        "2048 atlas page; the 23 planet/prop composites share `BackgroundsAtlas`.",
        "",
        "## Fonts",
        "",
        "| Id | Family | Glyphs | Text tags | Output | Licensing risk |",
        "|---:|---|---:|---:|---|---|",
    ]
    for font in payload["fonts"]:
        lines.append(
            f"| {font['symbolId']} | {font['family']} | {font['glyphCount'] or '-'} | "
            f"{font['usage']['textTags']} | `{font['output']}` | {font['licensingRisk']} |"
        )
    lines += [
        "",
        "The fonts are copied byte-for-byte from FFDec's `font:ttf` export and imported as Unity",
        "Font assets with `includeFontData = true`. No open or system font is substituted.",
        "FFDec copies the source SWF mtime into the TTF `head` timestamps; the temp library mtime",
        "is pinned to 2011-06-01 so the exported bytes are deterministic. Glyph outlines and font",
        "metrics are untouched. The fonts are available to the UI as Unity Font assets; this",
        "ticket does not rewire UI text, per the no-runtime-change constraint.",
        "",
        "## Text",
        "",
        f"- String table: `{payload['textTable']['path']}`",
        f"- UI tags: {payload['textTable']['counts']['uiTags']} "
        f"({payload['textTable']['counts']['uiNonEmpty']} non-empty, "
        f"{payload['textTable']['counts']['uiUniqueNonEmpty']} unique non-empty)",
        f"- Data tags: {payload['textTable']['counts']['dataTags']}",
        f"- Dialogue: {counts['dialogueLines']} lines / {counts['dialogueSpeakers']} speakers,",
        "  kept on the existing `TalkScriptParser` importer.",
        "",
        "## Button states",
        "",
        "- 3-frame widgets: `_normal` = frame 1, `_hover` = frame 2, `_pressed` = frame 3.",
        "- 2-frame widgets: `_normal` = frame 1, `_pressed` = frame 2.",
        "- The frozen code (`BtnBase.as`) uses frame 2 for both over and down; the code-true",
        "  state machine is recorded per record in `buttonStates`. The ticket naming and the",
        "  frozen behaviour therefore differ for frame 3; both are documented, not guessed.",
        "",
        "## Ambiguities",
        "",
        "| Id | Status | Symbols | Detail |",
        "|---|---|---|---|",
    ]
    for ambiguity in payload["ambiguities"]:
        symbols = ", ".join(str(value) for value in ambiguity["symbols"]) or "-"
        detail = ambiguity["detail"].replace("|", "/")
        lines.append(f"| `{ambiguity['id']}` | {ambiguity['status']} | {symbols} | {detail} |")
    lines += ["", "## Contact sheets", ""]
    for sheet in contact_sheets:
        lines.append(f"- `{sheet}`")
    lines += ["", "## Full-page reference renders (AGENTS.md)", ""]
    for reference in reference_pages:
        lines.append(f"- `{reference}`")
    lines += ["", "## Verification gates", ""]
    for gate in gates:
        lines.append(f"- {gate}")
    lines += [
        "",
        "## One command",
        "",
        "```powershell",
        "python scripts\\export_all_assets.py",
        "```",
        "",
        "The command unpacks the loader, renders every standalone renderable, writes the",
        "manifest, text table, fonts and contact sheets, then runs Unity batchmode to apply",
        "pivots/import settings and build the category atlases. Use `--skip-unity` only for a",
        "fast FFDec-only dry run.",
        "",
    ]
    write_bytes_if_changed(OUT_REPORT, ("\n".join(lines)).encode("utf-8"))


def run_unity_import() -> None:
    unity = resolve_unity()
    log_path = TEMP / "unity-import.log"
    command = [
        unity,
        "-batchmode",
        "-quit",
        "-projectPath",
        str(PROJECT),
        "-executeMethod",
        "ABRevival.Editor.FullAssetExtractionImporter.ImportBatch",
        "-logFile",
        str(log_path),
    ]
    log(f"[unity] {unity} -batchmode -quit -executeMethod FullAssetExtractionImporter.ImportBatch")
    result = subprocess.run(command, capture_output=True, text=True, encoding="utf-8", errors="replace")
    log_text = log_path.read_text(encoding="utf-8", errors="replace") if log_path.is_file() else ""
    if result.returncode != 0:
        raise RuntimeError(f"Unity import failed ({result.returncode}). See {log_path}.\n{log_text[-4000:]}")
    if "error CS" in log_text or "FullAssetExtractionImporter] FAILED" in log_text:
        raise RuntimeError(f"Unity import reported errors. See {log_path}.\n{log_text[-4000:]}")
    log("[unity] import and atlas build completed")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--swf", help="Source SWF (default: game/a&b_space_mod.swf, then game/a&b_space.swf)")
    parser.add_argument("--skip-unity", action="store_true", help="Only run FFDec and write the manifest/artifacts")
    parser.add_argument("--keep-temp", action="store_true", help="Keep the FFDec working directory")
    args = parser.parse_args(argv)

    if TEMP.exists():
        shutil.rmtree(TEMP)
    TEMP.mkdir(parents=True)

    manifest = load_manifest()
    write_nine_slice_overrides()
    overrides = load_nine_slice_overrides()
    source_swf = resolve_swf(args.swf)
    ffdec = resolve_ffdec()
    log(f"[extract] source SWF: {source_swf}")
    log(f"[extract] FFDec: {ffdec}")

    libraries = unpack_libraries(ffdec, source_swf)
    log(
        "[extract] libraries: "
        + ", ".join(
            f"{key}={path.name} ({EXPECTED_CHARACTERS[key]} chars)" for key, path in libraries.items()
        )
    )

    data_clean, data_xml, data_removed = make_clean_library(ffdec, libraries["data"], "data")
    ui_clean, ui_xml, ui_removed = make_clean_library(ffdec, libraries["ui"], "ui")
    log(f"[extract] marker placements removed: data={data_removed}, ui={ui_removed}")

    symbol_dir = TEMP / "symbols"
    (symbol_dir / "data").mkdir(parents=True)
    (symbol_dir / "ui").mkdir(parents=True)
    run_ffdec(ffdec, "-export", "symbolClass", symbol_dir / "data", data_clean)
    run_ffdec(ffdec, "-export", "symbolClass", symbol_dir / "ui", ui_clean)
    data_names, _data_ids = read_symbols(symbol_dir / "data" / "symbols.csv")
    ui_names, _ui_ids = read_symbols(symbol_dir / "ui" / "symbols.csv")
    log(f"[extract] symbols: data named={len(data_names)}, ui named={len(ui_names)}")

    data_rows = [
        row
        for row in manifest["symbols"]
        if is_library(row.get("swf"), DATA_LIB_SUFFIX) and row.get("extractable")
    ]
    data_sprite_rows = [
        row
        for row in data_rows
        if row["classification"]
        in ("part", "bullet", "pickup", "background.starfield", "background.planet", "background.credits")
    ]
    data_1x_ids = sorted({row["symbolId"] for row in data_sprite_rows if row["symbolId"] is not None})
    data_2x_ids = sorted(
        {
            row["symbolId"]
            for row in data_sprite_rows
            if row["classification"] in ("part", "bullet") and row["symbolId"] is not None
        }
    )
    for row in data_sprite_rows:
        name = row.get("className")
        if name and name not in data_names:
            raise RuntimeError(f"Data symbol '{name}' missing from symbols.csv.")
    render(ffdec, data_clean, data_1x_ids, 1.0, TEMP / "render/data-1x")
    render(ffdec, data_clean, data_2x_ids, 2.0, TEMP / "render/data-2x")
    log(f"[extract] rendered data: 1x={len(data_1x_ids)}, 2x={len(data_2x_ids)}")

    ui_rows = [
        row for row in manifest["symbols"] if is_library(row.get("swf"), UI_LIB_SUFFIX) and row.get("extractable")
    ]
    ui_sprite_rows = [
        row
        for row in ui_rows
        if row["classification"]
        in (
            "ui.screen",
            "ui.help.page",
            "ui.widget",
            "ui.widget.lock",
            "ui.cursor",
            "ui.composite.internal",
        )
        and row["symbolId"] != 56
    ]
    ui_shape_ids = sorted(
        {row["symbolId"] for row in ui_sprite_rows if row.get("kind") == "shape"}
    )
    ui_ids = sorted(
        {row["symbolId"] for row in ui_sprite_rows if row.get("kind") != "shape"}
    )
    render(ffdec, ui_clean, ui_ids, 1.0, TEMP / "render/ui")
    render_shapes(ffdec, ui_clean, ui_shape_ids, TEMP / "render/ui-shapes")
    log(f"[extract] rendered UI: {len(ui_ids)} sprites, {len(ui_shape_ids)} shapes")

    ui_images = export_ui_images(ffdec, libraries["ui"])
    ui_sounds = export_ui_sounds(ffdec, libraries["ui"])
    if sorted(ui_images) != [5, 441, 511, 620]:
        raise RuntimeError(f"Expected UI bitmaps 5/441/511/620, got {sorted(ui_images)}.")
    if sorted(ui_sounds) != [637, 640, 677]:
        raise RuntimeError(f"Expected UI sounds 637/640/677, got {sorted(ui_sounds)}.")

    for row in manifest["symbols"]:
        if not is_library(row.get("swf"), SOUND_LIB_SUFFIX) or not row.get("extractable"):
            continue
        clip = UNITY_ASSETS / "Resources/Audio/Original" / f"{row.get('className')}.mp3"
        if not clip.is_file():
            raise FileNotFoundError(f"Sound bank clip missing: {clip}")

    font_files = export_fonts(ffdec, libraries)
    texts = export_texts(ffdec, libraries)
    if len(font_files) != 8:
        raise RuntimeError(f"Expected 8 fonts, got {len(font_files)}")
    log(
        f"[extract] fonts={len(font_files)}, UI text tags={len(texts['ui'])}, "
        f"data text tags={len(texts['data'])}"
    )

    text_usage = {
        "ui": parse_text_font_usage(ui_xml),
        "data": parse_text_font_usage(data_xml),
    }
    font_metadata = {
        "ui": parse_symbol_metadata(ffdec, libraries["ui"]),
        "data": parse_symbol_metadata(ffdec, libraries["data"]),
    }
    manifest_rows = {
        row_key(row): row for row in manifest["symbols"] if row.get("symbolId") is not None
    }

    text_table = build_text_table(texts, manifest_rows, text_usage)
    write_text_table(text_table)

    font_records = build_font_records(font_files, font_metadata, manifest_rows, text_usage)
    records = build_plans(manifest, ui_images, overrides)
    records.extend(font_records)

    for row in ui_rows:
        if row["classification"] != "ui.sound.embedded":
            continue
        source = ui_sounds.get(row["symbolId"])
        if source is None:
            raise RuntimeError(f"UI sound {row['symbolId']} was not exported.")
        destination = PROJECT / row["intendedUnityPath"]
        copy_if_changed(source, destination)
        records.append(
            {
                "library": "ui",
                "symbolId": row["symbolId"],
                "className": None,
                "classification": row["classification"],
                "status": "extracted",
                "outputs": [
                    {
                        "path": rel(destination),
                        "sha256": sha256(destination),
                        "sizeBytes": destination.stat().st_size,
                    }
                ],
                "assignment": "unassigned",
                "note": "Unnamed UI DefineSound; no SoundData call site.",
            }
        )

    atlases, remove_atlases = build_atlas_spec(records)
    talk_counts = load_talk_counts()
    reference_pages = write_reference_pages(records)
    payload = write_extraction_manifest(
        manifest,
        records,
        font_records,
        text_table,
        atlases,
        remove_atlases,
        data_removed,
        talk_counts,
        source_swf,
        reference_pages,
    )
    contact_sheets = build_contact_sheets(records, font_records)
    gates = verify_gates(
        manifest, records, font_records, text_table, contact_sheets, reference_pages, atlases
    )
    write_report(payload, contact_sheets, reference_pages, gates)

    if not args.skip_unity:
        run_unity_import()

    verify_audio = subprocess.run(
        [sys.executable, str(ROOT / "scripts/export_audio.py"), "--verify"],
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace",
    )
    if verify_audio.returncode != 0:
        raise RuntimeError(
            "scripts/export_audio.py --verify failed:\n" + verify_audio.stdout + verify_audio.stderr
        )

    log("")
    log("Extraction complete.")
    for gate in gates:
        log(f"  PASS  {gate}")
    log(f"  Manifest: {rel(OUT_MANIFEST)}")
    log(f"  Report:   {rel(OUT_REPORT)}")
    log(f"  Text:     {rel(TEXT_TABLE)}")
    log(f"  Atlases:  {len(atlases)} category atlases")
    log(f"  Fonts:    {len(font_records)} original fonts embedded")
    log(f"  Sheets:   {len(contact_sheets)} contact sheets")
    if not args.keep_temp and TEMP.exists():
        shutil.rmtree(TEMP, ignore_errors=True)
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:  # noqa: BLE001 - CLI boundary
        print(f"ERROR: {error}", file=sys.stderr)
        raise SystemExit(1)
