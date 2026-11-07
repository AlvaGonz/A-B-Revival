#!/usr/bin/env python3
"""
Lint Windows .bat scripts to guard against ampersand splitting and path quoting pitfalls.
Enforces safe quoted variable assignment: set "VAR=..." and escaped ampersands: ^&.
"""

import argparse
import re
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import List


@dataclass
class Violation:
    filename: str
    line_no: int
    line_text: str
    message: str


PATH_VARS_PATTERN = re.compile(
    r"(%~dp0|%SCRIPT_DIR%|%REPO_ROOT%|%PROJECT_DIR%|%TEST_PROJ%|%CD%|%~f0|%~d0|%~p0)",
    re.IGNORECASE,
)
SET_UNQUOTED_PATTERN = re.compile(r"^\s*set\s+([^\"=\s]+)\s*=\s*(.*)$", re.IGNORECASE)
ECHO_PATTERN = re.compile(r"^\s*echo(?:\s+|\.)(.*)$", re.IGNORECASE)


def has_unescaped_ampersand(text: str) -> bool:
    """Check if text contains an ampersand '&' that is not escaped with '^' or inside double quotes."""
    in_quotes = False
    i = 0
    while i < len(text):
        char = text[i]
        if char == '"':
            in_quotes = not in_quotes
        elif char == "^":
            # Skip escaped character
            i += 1
        elif char == "&" and not in_quotes:
            return True
        i += 1
    return False


def lint_content(content: str, filename: str) -> List[Violation]:
    violations: List[Violation] = []
    lines = content.splitlines()

    for idx, raw_line in enumerate(lines, start=1):
        line = raw_line.strip()
        if not line or line.startswith("::") or line.lower().startswith("rem "):
            continue

        # Check unquoted set assignments: set VAR=...
        set_match = SET_UNQUOTED_PATTERN.match(line)
        if set_match:
            var_name = set_match.group(1).strip()
            val = set_match.group(2).strip()

            # Flag if assignment uses path tokens without quotes around the full statement
            if PATH_VARS_PATTERN.search(val):
                violations.append(
                    Violation(
                        filename=filename,
                        line_no=idx,
                        line_text=raw_line,
                        message=f'Unquoted path variable assignment for {var_name}. Use `set "{var_name}={val}"` instead.',
                    )
                )
            elif has_unescaped_ampersand(val):
                violations.append(
                    Violation(
                        filename=filename,
                        line_no=idx,
                        line_text=raw_line,
                        message=f'Unquoted variable assignment containing ampersand for {var_name}. Use `set "{var_name}={val}"` instead.',
                    )
                )

        # Check echo lines for unescaped ampersands: echo ... A&B ...
        echo_match = ECHO_PATTERN.match(line)
        if echo_match:
            echo_text = echo_match.group(1)
            if has_unescaped_ampersand(echo_text):
                violations.append(
                    Violation(
                        filename=filename,
                        line_no=idx,
                        line_text=raw_line,
                        message="Unescaped ampersand in echo. Use `^&` instead of `&` to avoid command splitting.",
                    )
                )

    return violations


def lint_file(file_path: Path) -> List[Violation]:
    try:
        content = file_path.read_text(encoding="utf-8", errors="replace")
        return lint_content(content, str(file_path))
    except Exception as ex:
        return [
            Violation(
                filename=str(file_path),
                line_no=0,
                line_text="",
                message=f"Failed to read file: {ex}",
            )
        ]


def get_staged_bat_files() -> List[Path]:
    try:
        out = subprocess.check_output(
            ["git", "diff", "--cached", "--name-only"], text=True
        )
        files = []
        for line in out.splitlines():
            path_str = line.strip()
            if path_str.lower().endswith(".bat") and Path(path_str).is_file():
                files.append(Path(path_str))
        return files
    except Exception as ex:
        print(f"[lint_shell_scripts] Error getting staged files from git: {ex}", file=sys.stderr)
        return []


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Lint Windows .bat files for quoting safety and ampersand escaping."
    )
    parser.add_argument(
        "--staged",
        action="store_true",
        help="Lint only staged .bat files in the git index.",
    )
    parser.add_argument(
        "paths",
        nargs="*",
        type=Path,
        help="Optional paths or directories of .bat files to lint.",
    )
    args = parser.parse_args()

    files_to_lint: List[Path] = []

    if args.staged:
        files_to_lint = get_staged_bat_files()
        if not files_to_lint:
            print("[lint_shell_scripts] No staged .bat files to lint.")
            return 0
    elif args.paths:
        for p in args.paths:
            if p.is_dir():
                files_to_lint.extend(p.glob("**/*.bat"))
            elif p.is_file() and p.suffix.lower() == ".bat":
                files_to_lint.append(p)
    else:
        # Default: scan scripts/ directory
        scripts_dir = Path(__file__).resolve().parent
        files_to_lint = list(scripts_dir.glob("*.bat"))

    all_violations: List[Violation] = []
    for f in files_to_lint:
        all_violations.extend(lint_file(f))

    if not all_violations:
        print(f"[lint_shell_scripts] All {len(files_to_lint)} .bat files passed quoting checks.")
        return 0

    print(f"\n[lint_shell_scripts ERROR] Found {len(all_violations)} violation(s):", file=sys.stderr)
    for v in all_violations:
        print(f"  {v.filename}:{v.line_no}: {v.message}", file=sys.stderr)
        if v.line_text:
            print(f"    Line: {v.line_text.strip()}", file=sys.stderr)

    return 1


if __name__ == "__main__":
    sys.exit(main())
