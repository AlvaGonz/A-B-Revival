#!/usr/bin/env python3
"""
scripts/recompile.py

Cross-platform recompilation runner for ActionScript 3 codebase.
Invokes FFDec CLI -importScript and propagates deterministic exit codes.
"""

import os
import sys
import subprocess

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
IN_SWF = os.path.join(REPO_ROOT, "game", "a&b_space.swf")
OUT_SWF = os.path.join(REPO_ROOT, "game", "a&b_space_mod.swf")
SCRIPTS_DIR = os.path.join(REPO_ROOT, "src", "as3", "scripts")

FFDEC_CANDIDATES = [
    r"C:\Program Files (x86)\FFDec\ffdec-cli.exe",
    r"C:\Program Files\FFDec\ffdec-cli.exe",
    "ffdec-cli",
    "ffdec",
]

def find_ffdec():
    for cand in FFDEC_CANDIDATES:
        if os.path.isfile(cand):
            return cand
    import shutil
    for cmd in ["ffdec-cli", "ffdec"]:
        found = shutil.which(cmd)
        if found:
            return found
    return FFDEC_CANDIDATES[0]

def main():
    print("Recompiling SWF with modified ActionScript 3 sources...")
    ffdec = find_ffdec()
    
    cmd = [
        ffdec,
        "-importScript",
        IN_SWF,
        OUT_SWF,
        SCRIPTS_DIR,
    ]
    
    result = subprocess.run(cmd)
    if result.returncode == 0:
        print(f"Build SUCCESS: {OUT_SWF} created!")
        return 0
    else:
        print(f"Build FAILED with error code {result.returncode}.", file=sys.stderr)
        return result.returncode

if __name__ == "__main__":
    sys.exit(main())
