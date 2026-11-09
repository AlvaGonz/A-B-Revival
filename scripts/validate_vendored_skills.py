#!/usr/bin/env python3
"""Validate vendored skills: layout, provenance, licenses and lockfile."""

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Dict, List, Optional

SKILLS_DIR = Path(".agents/skills")
LOCKFILE = Path("docs/skills/vendored-skills.lock.json")
SKILL_PREFIX = "ab-"

SKILL_ID = re.compile(r"^[a-z0-9]+(-[a-z0-9]+)*$")
PINNED_SHA = re.compile(r"^[0-9a-f]{7,40}$")
SOURCE_URL = re.compile(r"^https?://\S+$")
FRONTMATTER_NAME = re.compile(r"^name:\s*(\S+)\s*$", re.MULTILINE)
FRONTMATTER_DESCRIPTION = re.compile(r"^description:\s*(\S.*)$", re.MULTILINE)
UPSTREAM_FIELD = re.compile(r"^-\s*(Source|Path|Commit|License|Notice):\s*(\S.*)$", re.MULTILINE)
REQUIRED_UPSTREAM = ("Source", "Path", "Commit", "License")
REQUIRED_LOCK = ("id", "sourceRepo", "sourcePath", "commit", "license", "localPath")


def safe_read(path: Path, errors: List[str]) -> Optional[str]:
    """Read UTF-8 text; append a path-specific error instead of raising."""
    try:
        return path.read_text(encoding="utf-8-sig")
    except (UnicodeDecodeError, OSError) as exc:
        errors.append(f"{path}: cannot read as UTF-8: {exc}")
        return None


def read_frontmatter(text: str) -> Dict[str, Optional[str]]:
    """Return `name` and `description` from the leading frontmatter block."""
    if not text.startswith("---"):
        return {}
    end = text.find("\n---", 3)
    if end == -1:
        return {}
    block = text[:end]
    name = FRONTMATTER_NAME.search(block)
    description = FRONTMATTER_DESCRIPTION.search(block)
    return {
        "name": name.group(1).strip("\"'") if name else None,
        "description": description.group(1).strip() if description else None,
    }


def read_upstream(text: str) -> Dict[str, str]:
    return {match.group(1): match.group(2).strip() for match in UPSTREAM_FIELD.finditer(text)}


def has_placeholder(value: str) -> bool:
    return "<" in value or ">" in value


def validate_skill(skill_dir: Path, errors: List[str]) -> None:
    skill_md = skill_dir / "SKILL.md"
    if not skill_md.is_file():
        errors.append(f"{skill_dir}: missing SKILL.md")
        return

    text = safe_read(skill_md, errors)
    if text is None:
        return

    frontmatter = read_frontmatter(text)
    if frontmatter.get("name") != skill_dir.name:
        found = frontmatter.get("name")
        errors.append(f"{skill_md}: frontmatter name must be '{skill_dir.name}' (found '{found}')")
    if not frontmatter.get("description"):
        errors.append(f"{skill_md}: missing frontmatter description")

    upstream_md = skill_dir / "UPSTREAM.md"
    if not upstream_md.is_file():
        errors.append(f"{skill_dir}: missing UPSTREAM.md")
        return

    upstream_text = safe_read(upstream_md, errors)
    if upstream_text is None:
        return
    upstream = read_upstream(upstream_text)

    for field in REQUIRED_UPSTREAM:
        value = upstream.get(field, "")
        if not value:
            errors.append(f"{upstream_md}: missing '{field}'")
        elif has_placeholder(value):
            errors.append(f"{upstream_md}: '{field}' still contains a template placeholder")

    commit = upstream.get("Commit", "")
    if commit and not has_placeholder(commit) and not PINNED_SHA.match(commit):
        errors.append(f"{upstream_md}: 'Commit' must be a pinned hex SHA (found '{commit}')")

    source = upstream.get("Source", "")
    if source and not has_placeholder(source) and not SOURCE_URL.match(source):
        errors.append(f"{upstream_md}: 'Source' must be an http(s) URL")

    if upstream.get("License") and not (skill_dir / "LICENSE").is_file():
        errors.append(f"{skill_dir}: missing LICENSE")

    if upstream.get("Notice", "").strip().lower() == "required" and not (skill_dir / "NOTICE").is_file():
        errors.append(f"{skill_dir}: missing NOTICE")


def load_lock(root: Path, errors: List[str]) -> List[dict]:
    lock_path = root / LOCKFILE
    if not lock_path.is_file():
        errors.append(f"{lock_path}: missing lockfile")
        return []

    text = safe_read(lock_path, errors)
    if text is None:
        return []

    try:
        data = json.loads(text)
    except json.JSONDecodeError as exc:
        errors.append(f"{lock_path}: invalid JSON: {exc}")
        return []

    if not isinstance(data, dict):
        errors.append(f"{lock_path}: top level must be a JSON object")
        return []
    if data.get("schemaVersion") != 1:
        errors.append(f"{lock_path}: schemaVersion must be 1")

    skills = data.get("skills")
    if not isinstance(skills, list):
        errors.append(f"{lock_path}: 'skills' must be a list")
        return []
    return skills


def validate_lock(root: Path, folders: List[Path], errors: List[str]) -> None:
    entries = load_lock(root, errors)
    folder_ids = {folder.name for folder in folders}
    lock_ids = set()

    for entry in entries:
        if not isinstance(entry, dict):
            errors.append(f"{LOCKFILE}: every entry must be an object")
            continue

        missing = [field for field in REQUIRED_LOCK if not entry.get(field)]
        if missing:
            errors.append(f"{LOCKFILE}: entry missing {', '.join(missing)}")
            continue

        skill_id = entry["id"]
        if skill_id in lock_ids:
            errors.append(f"{LOCKFILE}: duplicate id '{skill_id}'")
        lock_ids.add(skill_id)

        expected_path = f".agents/skills/{skill_id}"
        if entry["localPath"] != expected_path:
            errors.append(f"{LOCKFILE}: '{skill_id}' localPath must be '{expected_path}'")
        if skill_id not in folder_ids:
            errors.append(f"{LOCKFILE}: '{skill_id}' has no folder in {SKILLS_DIR}")
        if not PINNED_SHA.match(str(entry["commit"])):
            errors.append(f"{LOCKFILE}: '{skill_id}' commit must be a pinned hex SHA")

    for folder_id in sorted(folder_ids - lock_ids):
        errors.append(f"{SKILLS_DIR / folder_id}: not listed in {LOCKFILE}")


def validate(root: Path) -> List[str]:
    errors: List[str] = []
    skills_root = root / SKILLS_DIR
    if not skills_root.is_dir():
        return [f"{skills_root}: missing skills directory"]

    folders = sorted(path for path in skills_root.iterdir() if path.is_dir())
    for folder in folders:
        if not folder.name.startswith(SKILL_PREFIX):
            errors.append(f"{folder}: skill folders must start with '{SKILL_PREFIX}'")
        elif not SKILL_ID.match(folder.name):
            errors.append(f"{folder}: skill folder must be lowercase kebab-case")
        validate_skill(folder, errors)

    validate_lock(root, folders, errors)
    return errors


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate vendored skill layout, provenance, licenses and lockfile."
    )
    parser.add_argument(
        "--root",
        type=Path,
        default=Path(__file__).resolve().parents[1],
        help="Repository root (defaults to the parent of this script).",
    )
    args = parser.parse_args()

    errors = validate(args.root)
    if errors:
        print(f"[validate_vendored_skills] {len(errors)} error(s):", file=sys.stderr)
        for error in errors:
            print(f" - {error}", file=sys.stderr)
        return 1

    print("[validate_vendored_skills] Vendored skills OK.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
