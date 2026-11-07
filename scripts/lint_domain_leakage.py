#!/usr/bin/env python3
"""
scripts/lint_domain_leakage.py

Static linter to enforce domain encapsulation and prevent domain leakage into UI controllers.
Enforces:
1. Flagship identity encapsulation: Checks in UI controllers must call F4DataLinks.isCaptainShip()
   or .isSpecialProtected instead of ad-hoc string inspections (e.g. name == "Captain's Ship" or .indexOf("Captain")).
2. Component type constants: Usage of F4DataOrg.TYPE_* constants instead of raw magic numbers (1, 2, 3)
   when querying component types in UI craft building and disarm logic.
"""

import sys
import os
import re
import subprocess
from typing import List, Tuple

UI_DIR_PREFIX = "src/as3/scripts/ui/"

# Patterns that indicate domain identity leakage in UI classes
FORBIDDEN_FLAGSHIP_STRINGS = [
    re.compile(r'name\s*==\s*["\']Captain\'s Ship["\']'),
    re.compile(r'name\.indexOf\(\s*["\']Captain["\']\s*\)'),
    re.compile(r'name\.indexOf\(\s*["\']Captain\'s Ship["\']\s*\)'),
]

# Patterns where raw magic integers for component types should use F4DataOrg.TYPE_*
# Matches things like countPartsByType(1) or cleanPartsByType(3)
FORBIDDEN_MAGIC_TYPES = [
    (re.compile(r'countPartsByType\(\s*([0-3])\s*\)'), "Use F4DataOrg.TYPE_* constant instead of raw integer type"),
    (re.compile(r'cleanPartsByType\(\s*([0-3])\s*\)'), "Use F4DataOrg.TYPE_* constant instead of raw integer type"),
]


def get_staged_files() -> List[str]:
    """Retrieve list of staged ActionScript files from git."""
    try:
        out = subprocess.check_output(
            ["git", "diff", "--cached", "--name-only"],
            text=True,
            encoding="utf-8"
        )
        return [f.strip().replace("\\", "/") for f in out.splitlines() if f.strip().endswith(".as")]
    except Exception as e:
        print(f"[lint] Warning: Unable to inspect git staged files ({e})", file=sys.stderr)
        return []


def lint_file(file_path: str) -> List[Tuple[int, str, str]]:
    """Lint a single ActionScript file for architectural domain leakage."""
    norm_path = file_path.replace("\\", "/")
    violations = []
    
    # Only enforce UI controller boundary rules on UI classes
    is_ui_controller = UI_DIR_PREFIX in norm_path or norm_path.startswith("src/as3/scripts/ui/")
    
    if not os.path.exists(file_path):
        return violations

    try:
        with open(file_path, "r", encoding="utf-8", errors="ignore") as f:
            lines = f.readlines()
    except Exception as e:
        violations.append((0, "file-read-error", f"Could not read file: {e}"))
        return violations

    for line_idx, line in enumerate(lines, start=1):
        # 1. Flagship identity string checking in UI
        if is_ui_controller:
            for pattern in FORBIDDEN_FLAGSHIP_STRINGS:
                if pattern.search(line):
                    violations.append((
                        line_idx,
                        "domain-leakage-flagship",
                        f"Direct flagship string check detected in UI: '{line.strip()}'. "
                        f"Use F4DataLinks.isCaptainShip() or isSpecialProtected instead."
                    ))

            # 2. Magic component type integers in UI builder functions
            for pattern, advice in FORBIDDEN_MAGIC_TYPES:
                match = pattern.search(line)
                if match:
                    violations.append((
                        line_idx,
                        "primitive-obsession-part-type",
                        f"{advice} in '{line.strip()}'."
                    ))

    return violations


def main():
    use_staged = "--staged" in sys.argv
    files_to_check = []

    if use_staged:
        staged = get_staged_files()
        files_to_check = [f for f in staged if f.startswith(UI_DIR_PREFIX)]
    elif len(sys.argv) > 1 and not sys.argv[1].startswith("--"):
        files_to_check = [f for f in sys.argv[1:] if not f.startswith("--")]
    else:
        # Full scan of UI controllers
        ui_base = os.path.join("src", "as3", "scripts", "ui")
        if os.path.isdir(ui_base):
            for root, _, files in os.walk(ui_base):
                for f in files:
                    if f.endswith(".as"):
                        files_to_check.append(os.path.join(root, f))

    if not files_to_check:
        print("[lint_domain_leakage] No UI ActionScript files to lint.")
        return 0

    total_violations = 0
    print(f"[lint_domain_leakage] Scanning {len(files_to_check)} UI ActionScript file(s)...")

    for fpath in files_to_check:
        violations = lint_file(fpath)
        for line_num, code, msg in violations:
            total_violations += 1
            print(f"[LINT ERROR] {fpath}:{line_num} [{code}] {msg}")

    if total_violations > 0:
        print(f"\n[lint_domain_leakage] FAILED: {total_violations} domain architecture violation(s) found.", file=sys.stderr)
        return 1
    else:
        print("[lint_domain_leakage] PASSED: All UI files conform to domain encapsulation standards.")
        return 0


if __name__ == "__main__":
    sys.exit(main())

