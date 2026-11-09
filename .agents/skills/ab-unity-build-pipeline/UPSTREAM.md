# Upstream

- Source: https://github.com/gamedev-skills/awesome-gamedev-agent-skills
- Path: skills/unity/unity-build-pipeline
- Commit: 19d93f97c9c1ea0f8b306e77aa81e0d3e352d3e9
- License: Apache-2.0
- Notice: required
- Vendored: 2026-11-09
- Local modifications:
  - Renamed frontmatter `name` to `ab-unity-build-pipeline`.
  - Rewrote for Unity 6000.6.8f1; dropped the uninstalled content-pipeline section and its CI call.
  - Removed LINQ from the reference build script.
  - Added Android/iOS mobile profile guidance and ADR 0016 build verification.
  - Dropped the CoreCLR forward note (6.7 beta, not this target).
  - Added the ADR 0020 and repository-workflow project header.

## Why this copy exists

IL2CPP, managed stripping, build verification and headless builds for the Unity 6.6 port
(ticket #200).
