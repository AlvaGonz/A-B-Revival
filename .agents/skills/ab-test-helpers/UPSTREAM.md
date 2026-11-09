# Upstream

- Source: https://github.com/Donchitos/Claude-Code-Game-Studios
- Path: .claude/skills/test-helpers
- Commit: 05ad7c8a5b8aa2aa86bbd3439ae7cf7295cac418
- License: MIT
- Notice: not required
- Vendored: 2026-11-09
- Local modifications:
  - Renamed frontmatter `name` to `ab-test-helpers`; removed harness-specific frontmatter fields and the file-injection line.
  - Added a project constraint header (ADR 0016, ADR 0020, repository workflow precedence).
  - Retargeted the helper layout to `A&B-U/Assets/Scripts/Tests` and `A&B-U/Assets/Scripts/Tests/PlayMode`, with the existing `ABRevival.Tests` and `ABRevival.Tests.PlayMode` assemblies.
  - Reworked the content from a generated helper library into project-wide patterns and small reusable helpers for the whole suite, not one screen.
  - Removed non-Unity engine sections, the generated-helper workflow, studio automation modes, studio output paths and upstream slash commands.

## Why this copy exists

Project-wide Unity Test Framework guidance for fixtures, teardown, Boot scene loading and
deterministic 60 Hz time across EditMode and PlayMode suites (ticket #201).
