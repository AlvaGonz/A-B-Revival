# Upstream

- Source: https://github.com/Donchitos/Claude-Code-Game-Studios
- Path: .claude/skills/soak-test
- Commit: 05ad7c8a5b8aa2aa86bbd3439ae7cf7295cac418
- License: MIT
- Notice: not required
- Vendored: 2026-11-09
- Local modifications:
  - Renamed frontmatter `name` to `ab-soak-test`; removed harness-specific frontmatter fields and the file-injection line.
  - Added a project constraint header (ADR 0020, 60 Hz fixed step, repository workflow precedence).
  - Ported the memory protocol to the Unity Memory Profiler and the ADR 0008 lifecycle invariants.
  - Removed non-Unity engine sections, studio automation modes, studio output paths and upstream slash commands.
  - Aligned checkpoints and cycles with Boot and the GameFlowController mission loop.

## Why this copy exists

Long-run memory and stability protocol for the Unity 6.6 target, aligned with ADR 0008
leak prevention (ticket #201).
