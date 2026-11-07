#!/usr/bin/env python3
"""Unit tests for scripts/lint_shell_scripts.py."""

import unittest
from pathlib import Path
import sys

# Add scripts directory to sys.path
sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "scripts"))
import lint_shell_scripts


class TestLintShellScripts(unittest.TestCase):
    def test_valid_quoted_assignments(self):
        content = """@echo off
set "SCRIPT_DIR=%~dp0"
set "REPO_ROOT=%SCRIPT_DIR%.."
set "PROJECT_DIR=%REPO_ROOT%\\A&B-U"
echo A^&B Revival
"""
        violations = lint_shell_scripts.lint_content(content, "valid.bat")
        self.assertEqual(violations, [])

    def test_unquoted_path_assignment_violation(self):
        content = """@echo off
set SCRIPT_DIR=%~dp0
set REPO_ROOT=%SCRIPT_DIR%..
"""
        violations = lint_shell_scripts.lint_content(content, "bad_path.bat")
        self.assertEqual(len(violations), 2)
        self.assertIn("Unquoted path variable assignment", violations[0].message)

    def test_unquoted_set_with_ampersand_violation(self):
        content = """@echo off
set PROJECT_DIR=C:\\Dev\\A&B revival
"""
        violations = lint_shell_scripts.lint_content(content, "bad_amp.bat")
        self.assertEqual(len(violations), 1)
        self.assertIn("Unquoted variable assignment containing ampersand", violations[0].message)

    def test_unescaped_echo_ampersand_violation(self):
        content = """@echo off
echo Welcome to A&B Revival
"""
        violations = lint_shell_scripts.lint_content(content, "bad_echo.bat")
        self.assertEqual(len(violations), 1)
        self.assertIn("Unescaped ampersand in echo", violations[0].message)


if __name__ == "__main__":
    unittest.main()
