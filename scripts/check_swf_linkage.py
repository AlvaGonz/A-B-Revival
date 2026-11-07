#!/usr/bin/env python3
"""
scripts/check_swf_linkage.py

Automated Linkage Gate:
Verifies that all ActionScript 3 classes defined in src/as3/scripts are
registered and linked in game/a&b_space_mod.swf ABC tags.

FFDec CLI -importScript only replaces existing script slots present in
the base SWF (156 pre-registered scripts). Any standalone .as file not
mapped in the SWF is silently omitted from the build, resulting in:
  ReferenceError: Error #1065: Variable <package>::<Class> is not defined.
"""

import os
import sys
import subprocess
import re

REPO_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
SWF_PATH = os.path.join(REPO_ROOT, "game", "a&b_space_mod.swf")
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

def get_swf_linked_classes(ffdec, swf_path):
    if not os.path.isfile(swf_path):
        raise FileNotFoundError(f"SWF file not found: {swf_path}")
    
    out = subprocess.check_output([ffdec, "-dumpAS3", swf_path], text=True, encoding="utf-8")
    classes = set()
    for line in out.strip().splitlines():
        parts = line.strip().split()
        if parts:
            classes.add(parts[0])
    return classes

def find_declared_classes(scripts_dir):
    class_pattern = re.compile(
        r'package\s*([a-zA-Z0-9_.]*)\s*\{.*?public\s+(?:class|interface)\s+([a-zA-Z0-9_]+)',
        re.DOTALL
    )
    declared = []
    for root, _, files in os.walk(scripts_dir):
        for f in files:
            if f.endswith('.as'):
                path = os.path.join(root, f)
                with open(path, 'r', encoding='utf-8', errors='ignore') as fh:
                    content = fh.read()
                m = class_pattern.search(content)
                if m:
                    pkg = m.group(1).strip()
                    cls = m.group(2).strip()
                    full_name = f"{pkg}.{cls}" if pkg else cls
                    rel_path = os.path.relpath(path, REPO_ROOT)
                    declared.append((full_name, rel_path))
    return declared

def main():
    print("Checking SWF ABC linkage for all src/as3/scripts classes...")
    ffdec = find_ffdec()
    try:
        swf_classes = get_swf_linked_classes(ffdec, SWF_PATH)
    except Exception as e:
        print(f"[ERROR] Failed to read SWF script tags: {e}", file=sys.stderr)
        return 1

    declared_classes = find_declared_classes(SCRIPTS_DIR)
    unlinked = []
    for full_name, rel_path in declared_classes:
        if full_name not in swf_classes:
            unlinked.append((full_name, rel_path))

    if unlinked:
        print(f"\n[FAIL] Found {len(unlinked)} unlinked class(es) in SWF ABC tags:")
        for name, path in unlinked:
            print(f"  - ReferenceError #1065 Risk: '{name}' in {path}")
        print("\nFix: FFDec -importScript cannot inject new script slots.")
        print("Inline helper logic or delegate to established domain/UI hubs per ADR-0007 / ADR-0009.")
        return 1

    print(f"\n[PASS] All {len(declared_classes)} ActionScript classes are linked in {os.path.basename(SWF_PATH)}.")
    return 0

if __name__ == "__main__":
    sys.exit(main())

