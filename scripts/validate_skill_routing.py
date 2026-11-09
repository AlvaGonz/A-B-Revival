#!/usr/bin/env python3
"""Validate that AGENTS.md routes every vendored skill and project agent."""

import argparse
import re
import sys
from pathlib import Path
from typing import Dict, List, Set, Tuple

SKILLS_DIR = Path(".agents/skills")
AGENT_DIRS = (Path(".opencode/agents"), Path(".agents/agents"))
AGENTS_MD = Path("AGENTS.md")
PREFIX = "ab-"
TOKEN = re.compile(r"\bab-[a-z0-9]+(?:-[a-z0-9]+)*\b")


def collect(root: Path) -> Tuple[Set[str], Dict[str, Path]]:
    """Return skill IDs and agent IDs mapped to their first file."""
    skills_root = root / SKILLS_DIR
    skill_ids = {
        path.name for path in skills_root.iterdir() if path.is_dir() and path.name.startswith(PREFIX)
    }

    agent_paths: Dict[str, Path] = {}
    for agents_dir in AGENT_DIRS:
        directory = root / agents_dir
        if not directory.is_dir():
            continue
        for path in directory.iterdir():
            if path.is_file() and path.suffix == ".md" and path.name.startswith(PREFIX):
                agent_paths.setdefault(path.stem, agents_dir / path.name)

    return skill_ids, agent_paths


def validate(root: Path) -> List[str]:
    errors: List[str] = []

    if not (root / SKILLS_DIR).is_dir():
        return [f"{SKILLS_DIR}: missing skills directory"]

    agents_md = root / AGENTS_MD
    if not agents_md.is_file():
        return [f"{AGENTS_MD}: missing routing table"]
    try:
        text = agents_md.read_text(encoding="utf-8-sig")
    except (UnicodeDecodeError, OSError) as exc:
        return [f"{AGENTS_MD}: cannot read as UTF-8: {exc}"]

    skill_ids, agent_paths = collect(root)
    tokens = set(TOKEN.findall(text))

    for skill_id in sorted(skill_ids - tokens):
        errors.append(f"{SKILLS_DIR / skill_id}: not routed in {AGENTS_MD}")
    for agent_id in sorted(agent_paths.keys() - tokens):
        errors.append(f"{agent_paths[agent_id]}: not routed in {AGENTS_MD}")
    for token in sorted(tokens - skill_ids - agent_paths.keys()):
        errors.append(f"{AGENTS_MD}: stale routing token '{token}' (no skill folder or agent file)")

    return errors


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate that AGENTS.md routes every vendored skill and project agent."
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
        print(f"[validate_skill_routing] {len(errors)} error(s):", file=sys.stderr)
        for error in errors:
            print(f" - {error}", file=sys.stderr)
        return 1

    skill_ids, agent_paths = collect(args.root)
    print(f"Routing OK ({len(skill_ids)} skills, {len(agent_paths)} agents)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
