#!/usr/bin/env python3
"""
scripts/as3_search.py

Lightweight, high-speed ActionScript 3 symbol and caller search utility for developers and agents.
Provides targeted queries without reading entire decompiled classes into context:
  - symbol <name>: Find classes, methods, variables, constants matching <name>.
  - callers <method>: Find invocation call sites of a method.
  - class <class_name>: Emit class synopsis (extends, fields, public methods).
"""

import sys
import os
import re
from typing import List, Dict, Any

SCRIPTS_DIR = os.path.join("src", "as3", "scripts")


def find_as_files(base_dir: str = SCRIPTS_DIR) -> List[str]:
    """Find all .as source files."""
    as_files = []
    if not os.path.isdir(base_dir):
        return as_files
    for root, _, files in os.walk(base_dir):
        for f in files:
            if f.endswith(".as"):
                as_files.append(os.path.join(root, f))
    return as_files


def search_symbol(query: str):
    """Search for declarations matching query."""
    files = find_as_files()
    regex = re.compile(
        rf'\b(class|interface|function|var|const)\s+([A-Za-z0-9_]*{re.escape(query)}[A-Za-z0-9_]*)',
        re.IGNORECASE
    )
    
    matches = []
    for fpath in files:
        rel_path = os.path.relpath(fpath, ".").replace("\\", "/")
        try:
            with open(fpath, "r", encoding="utf-8", errors="ignore") as f:
                for line_idx, line in enumerate(f, start=1):
                    m = regex.search(line)
                    if m:
                        clean_line = line.strip()
                        matches.append((rel_path, line_idx, m.group(1), m.group(2), clean_line))
        except Exception:
            continue

    if not matches:
        print(f"No symbols found matching '{query}'.")
        return

    print(f"Found {len(matches)} symbol match(es) for '{query}':")
    for rel_path, line_idx, kind, name, line_content in matches:
        print(f"  {rel_path}:{line_idx} [{kind}] {line_content}")


def search_callers(method_name: str):
    """Search for invocation sites of a method."""
    files = find_as_files()
    # Match callers like .methodName( or methodName(
    regex = re.compile(rf'(?:\.\s*|\b){re.escape(method_name)}\s*\(')
    # Ignore declarations
    decl_regex = re.compile(rf'function\s+{re.escape(method_name)}\b')

    matches = []
    for fpath in files:
        rel_path = os.path.relpath(fpath, ".").replace("\\", "/")
        try:
            with open(fpath, "r", encoding="utf-8", errors="ignore") as f:
                for line_idx, line in enumerate(f, start=1):
                    if decl_regex.search(line):
                        continue
                    if regex.search(line):
                        matches.append((rel_path, line_idx, line.strip()))
        except Exception:
            continue

    if not matches:
        print(f"No call sites found for '{method_name}()'.")
        return

    print(f"Found {len(matches)} call site(s) for '{method_name}()':")
    for rel_path, line_idx, line_content in matches:
        print(f"  {rel_path}:{line_idx}: {line_content}")


def class_synopsis(class_name: str):
    """Print synopsis of a class."""
    files = find_as_files()
    target_file = None
    target_filename = f"{class_name}.as".lower()

    for fpath in files:
        if os.path.basename(fpath).lower() == target_filename:
            target_file = fpath
            break

    if not target_file:
        print(f"Class file '{class_name}.as' not found in {SCRIPTS_DIR}.")
        return

    rel_path = os.path.relpath(target_file, ".").replace("\\", "/")
    print(f"=== Class Synopsis: {class_name} ({rel_path}) ===")

    try:
        with open(target_file, "r", encoding="utf-8", errors="ignore") as f:
            lines = f.readlines()
    except Exception as e:
        print(f"Error reading {rel_path}: {e}")
        return

    pkg = ""
    header = ""
    fields = []
    methods = []

    for line_idx, line in enumerate(lines, start=1):
        sline = line.strip()
        if sline.startswith("package"):
            pkg = sline
        elif "class " in sline or "interface " in sline:
            if not header and "public " in sline:
                header = sline
        elif re.match(r'^\s*(?:public|protected|private)?\s*(?:static\s+)?(?:var|const)\s+', sline):
            fields.append((line_idx, sline))
        elif re.match(r'^\s*(?:public|protected|private)?\s*(?:static\s+)?function\s+', sline):
            methods.append((line_idx, sline))

    print(f"Package: {pkg}")
    print(f"Declaration: {header}")
    print(f"\nFields ({len(fields)}):")
    for lidx, fld in fields[:30]:
        print(f"  L{lidx}: {fld}")
    if len(fields) > 30:
        print(f"  ... and {len(fields) - 30} more fields")

    print(f"\nMethods ({len(methods)}):")
    for lidx, mth in methods[:40]:
        print(f"  L{lidx}: {mth}")
    if len(methods) > 40:
        print(f"  ... and {len(methods) - 40} more methods")


def print_help():
    print("""ActionScript 3 AST & Symbol Search Utility
Usage:
  python scripts/as3_search.py symbol <name>     # Find symbol declarations
  python scripts/as3_search.py callers <method>  # Find invocation call sites
  python scripts/as3_search.py class <name>      # Show class synopsis
""")


def main():
    if len(sys.argv) < 3:
        print_help()
        return 1

    cmd = sys.argv[1].lower()
    arg = sys.argv[2]

    if cmd == "symbol":
        search_symbol(arg)
    elif cmd == "callers":
        search_callers(arg)
    elif cmd == "class":
        class_synopsis(arg)
    else:
        print(f"Unknown command '{cmd}'.")
        print_help()
        return 1

    return 0


if __name__ == "__main__":
    sys.exit(main())
