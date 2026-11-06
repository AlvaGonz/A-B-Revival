#!/usr/bin/env bash
echo "Configuring Git hooks directory to .githooks..."
git config core.hooksPath .githooks
chmod +x .githooks/pre-commit 2>/dev/null || true
echo "Git pre-commit hooks successfully installed!"

