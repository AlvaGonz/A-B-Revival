#!/usr/bin/env python3
"""
scripts/test_core_null_safety.py

Automated Quality Gate for Runtime Null Safety in Game Core:
Verifies that f4.core.OpOneCore guards all object references in
playerCtrl() and runGame() against null dereferencing (Flash Error #1009).

Common failure modes:
- Dereferencing this.playerFly.isDeadA1 when playerFly is null (e.g. before level init or after level cleanup)
- Calling this.targets.evts.gameui methods (.mouseMove, .mouseFire, .changeKM) when gameui is null
- Calling this.targets.ckSWU(this.playerFly.unity) when playerFly or unity is null
"""

import os
import sys
import re

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
OP_ONE_CORE_PATH = os.path.join(REPO_ROOT, "src", "as3", "scripts", "f4", "core", "OpOneCore.as")

def test_core_null_safety():
    if not os.path.isfile(OP_ONE_CORE_PATH):
        print(f"[FAIL] OpOneCore.as not found at {OP_ONE_CORE_PATH}", file=sys.stderr)
        return 1

    with open(OP_ONE_CORE_PATH, "r", encoding="utf-8") as f:
        content = f.read()

    violations = []

    # 1. Check playerFly null check in playerCtrl()
    m_pctrl = re.search(r'public function playerCtrl\(\)\s*:\s*void\s*\{(?P<body>.+?)(?=\n\s*public function|\n\s*\}\s*\}\s*\n)', content, re.DOTALL)
    if not m_pctrl:
        violations.append("Unable to parse playerCtrl() body in OpOneCore.as")
    else:
        body = m_pctrl.group("body")
        # Ensure playerFly is checked for null before accessing properties
        if re.search(r'^\s*if\(!this\.playerFly\.isDeadA1\)', body, re.MULTILINE):
            violations.append(
                "playerCtrl() line accesses '!this.playerFly.isDeadA1' without checking 'this.playerFly != null'. "
                "Causes TypeError: Error #1009 when playerFly is null."
            )

        # Ensure unguarded calls to gameui are not present
        unguarded_calls = re.findall(r'(?<![?\w])this\.targets\.evts\.gameui\.(mouseMove|mouseFire|changeKM)\(', body)
        if unguarded_calls:
            violations.append(
                f"playerCtrl() contains {len(unguarded_calls)} unguarded calls to this.targets.evts.gameui "
                f"({', '.join(set(unguarded_calls))}). Causes Error #1009 if gameui is null."
            )

    # 2. Check runGame() null guard on playerFly
    m_rg = re.search(r'public function runGame\(\)\s*:\s*void\s*\{(?P<body>.+?)(?=\n\s*public function|\n\s*\}\s*\}\s*\n)', content, re.DOTALL)
    if not m_rg:
        violations.append("Unable to parse runGame() body in OpOneCore.as")
    else:
        rg_body = m_rg.group("body")
        if "this.playerFly != null" not in rg_body and "if(this.playerFly" not in rg_body and "!this.playerFly" not in rg_body:
            violations.append(
                "runGame() executes playerCtrl() and ckSWU(this.playerFly.unity) without checking if this.playerFly is null."
            )

    if violations:
        print(f"[FAIL] Found {len(violations)} null-safety violation(s) in OpOneCore.as:")
        for v in violations:
            print(f"  - {v}")
        return 1

    print("[PASS] OpOneCore null safety checks passed: playerCtrl() and runGame() are properly guarded against null dereferencing.")
    return 0

if __name__ == "__main__":
    sys.exit(test_core_null_safety())

