#!/usr/bin/env python3
"""Self-checks for scripts/validate_skill_routing.py. Run: python scripts/test_validate_skill_routing.py"""

import sys
import tempfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

import validate_skill_routing as validator  # noqa: E402


def write(path: Path, text: str) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def make_root(tmp: Path) -> Path:
    root = tmp / "repo"
    (root / ".agents/skills/ab-demo").mkdir(parents=True)
    write(root / ".opencode/agents/ab-helper.md", "---\nmode: subagent\n---\n")
    write(root / ".agents/agents/ab-helper.md", "---\nname: ab-helper\n---\n")
    write(root / "AGENTS.md", "| Read | Delegate |\n|---|---|\n| `ab-demo` | `ab-helper` |\n")
    return root


def test_current_shape_passes() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        assert validator.validate(root) == []


def test_unrouted_skill_fails() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        (root / ".agents/skills/ab-orphan").mkdir()
        errors = validator.validate(root)
        assert any("ab-orphan" in error and "not routed" in error for error in errors), errors


def test_unrouted_agent_fails() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        write(root / ".opencode/agents/ab-orphan.md", "---\nmode: subagent\n---\n")
        errors = validator.validate(root)
        assert any("ab-orphan" in error and "not routed" in error for error in errors), errors


def test_stale_routing_token_fails() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        with (root / "AGENTS.md").open("a", encoding="utf-8") as handle:
            handle.write("| `ab-retired` | — |\n")
        errors = validator.validate(root)
        assert any("ab-retired" in error and "stale" in error for error in errors), errors


def test_agent_listed_once_when_both_harnesses_have_it() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        write(root / "AGENTS.md", "| `ab-demo` |\n")
        errors = validator.validate(root)
        assert sum("ab-helper" in error for error in errors) == 1, errors


def test_missing_agents_md_fails() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        root = make_root(Path(tmp))
        (root / "AGENTS.md").unlink()
        assert any("missing routing table" in error for error in validator.validate(root))


def run_all() -> int:
    tests = [value for name, value in sorted(globals().items()) if name.startswith("test_")]
    for test in tests:
        test()
    print(f"[test_validate_skill_routing] {len(tests)} tests passed.")
    return 0


if __name__ == "__main__":
    sys.exit(run_all())
