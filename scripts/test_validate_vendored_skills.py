#!/usr/bin/env python3
"""Self-checks for scripts/validate_vendored_skills.py. Run: python scripts/test_validate_vendored_skills.py"""

import json
import sys
import tempfile
from pathlib import Path
from typing import List

sys.path.insert(0, str(Path(__file__).resolve().parent))

import validate_vendored_skills as validator  # noqa: E402


def write(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def lock_entry(skill_id: str) -> dict:
    return {
        "id": skill_id,
        "sourceRepo": "https://example.com/repo",
        "sourcePath": f"skills/{skill_id}",
        "commit": "0" * 40,
        "license": "MIT",
        "localPath": f".agents/skills/{skill_id}",
        "modifications": "none",
        "adoptedAt": "2026-11-09",
    }


def make_root(tmp: Path, skill_id: str = "ab-demo") -> Path:
    root = tmp / "repo"
    write(root / ".agents/skills/README.md", "# Skills\n")
    write(
        root / "docs/skills/vendored-skills.lock.json",
        json.dumps({"schemaVersion": 1, "skills": []}, indent=2),
    )
    if skill_id:
        skill = root / ".agents/skills" / skill_id
        write(skill / "SKILL.md", f"---\nname: {skill_id}\ndescription: demo\n---\n\n# Demo\n")
        write(
            skill / "UPSTREAM.md",
            "# Upstream\n\n- Source: https://example.com/repo\n- Path: skills/demo\n"
            "- Commit: " + "0" * 40 + "\n- License: MIT\n- Notice: not required\n",
        )
        write(skill / "LICENSE", "MIT\n")
    return root


def set_lock(root: Path, entries: List[dict]) -> None:
    write(
        root / "docs/skills/vendored-skills.lock.json",
        json.dumps({"schemaVersion": 1, "skills": entries}, indent=2),
    )


def test_empty_tree_passes() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp), skill_id="")
        assert validator.validate(root) == []


def test_correct_skill_passes() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        set_lock(root, [lock_entry("ab-demo")])
        assert validator.validate(root) == []


def test_missing_upstream_fails() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        (root / ".agents/skills/ab-demo/UPSTREAM.md").unlink()
        set_lock(root, [lock_entry("ab-demo")])
        assert any("missing UPSTREAM.md" in error for error in validator.validate(root))


def test_missing_license_fails() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        (root / ".agents/skills/ab-demo/LICENSE").unlink()
        set_lock(root, [lock_entry("ab-demo")])
        assert any("missing LICENSE" in error for error in validator.validate(root))


def test_notice_required_fails() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        upstream = root / ".agents/skills/ab-demo/UPSTREAM.md"
        write(upstream, upstream.read_text(encoding="utf-8").replace("not required", "required"))
        set_lock(root, [lock_entry("ab-demo")])
        assert any("missing NOTICE" in error for error in validator.validate(root))


def test_name_mismatch_fails() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        write(root / ".agents/skills/ab-demo/SKILL.md", "---\nname: ab-other\ndescription: demo\n---\n")
        set_lock(root, [lock_entry("ab-demo")])
        assert any("frontmatter name" in error for error in validator.validate(root))


def test_missing_description_fails() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        write(root / ".agents/skills/ab-demo/SKILL.md", "---\nname: ab-demo\n---\n")
        set_lock(root, [lock_entry("ab-demo")])
        assert any("missing frontmatter description" in error for error in validator.validate(root))


def test_unfilled_template_fails() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        template = Path(__file__).resolve().parents[1] / "docs/skills/UPSTREAM.template.md"
        (root / ".agents/skills/ab-demo/UPSTREAM.md").write_text(
            template.read_text(encoding="utf-8"), encoding="utf-8"
        )
        set_lock(root, [lock_entry("ab-demo")])
        assert any("template placeholder" in error for error in validator.validate(root))


def test_folder_missing_from_lock_fails() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        assert any("not listed" in error for error in validator.validate(root))


def test_stale_lock_entry_fails() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp), skill_id="")
        set_lock(root, [lock_entry("ab-gone")])
        assert any("has no folder" in error for error in validator.validate(root))


def test_duplicate_lock_id_fails() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        set_lock(root, [lock_entry("ab-demo"), lock_entry("ab-demo")])
        assert any("duplicate id" in error for error in validator.validate(root))


def test_lock_entry_missing_field_fails() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        entry = lock_entry("ab-demo")
        entry.pop("license")
        set_lock(root, [entry])
        assert any("entry missing license" in error for error in validator.validate(root))


def test_non_object_lock_fails() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        write(root / "docs/skills/vendored-skills.lock.json", "[]")
        assert any("top level must be a JSON object" in error for error in validator.validate(root))


def test_invalid_lock_json_fails() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        write(root / "docs/skills/vendored-skills.lock.json", "{not json")
        assert any("invalid JSON" in error for error in validator.validate(root))


def test_non_utf8_skill_fails() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        (root / ".agents/skills/ab-demo/SKILL.md").write_bytes(b"\xff\xfe\x00bad")
        set_lock(root, [lock_entry("ab-demo")])
        assert any("cannot read as UTF-8" in error for error in validator.validate(root))


def test_non_prefixed_folder_fails() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp), skill_id="")
        write(root / ".agents/skills/demo/SKILL.md", "---\nname: demo\ndescription: demo\n---\n")
        assert any("must start with 'ab-'" in error for error in validator.validate(root))


def run_all() -> int:
    tests = [value for name, value in sorted(globals().items()) if name.startswith("test_")]
    for test in tests:
        test()
    print(f"[test_validate_vendored_skills] {len(tests)} tests passed.")
    return 0


if __name__ == "__main__":
    sys.exit(run_all())
