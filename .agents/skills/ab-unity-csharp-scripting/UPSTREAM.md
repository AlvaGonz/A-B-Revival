# Upstream

- Source: https://github.com/gamedev-skills/awesome-gamedev-agent-skills
- Path: skills/unity/unity-csharp-scripting
- Commit: 19d93f97c9c1ea0f8b306e77aa81e0d3e352d3e9
- License: Apache-2.0
- Notice: required
- Vendored: 2026-11-09
- Local modifications:
  - Renamed frontmatter `name` to `ab-unity-csharp-scripting`.
  - Rewrote for Unity 6000.6.8f1; removed coroutine, async/Awaitable and per-frame allocation examples and added the explicit zero-allocation and presentation-only constraints.
  - Replaced the coroutine reference with `references/execution-order.md`.
  - Replaced the `Rigidbody` example with `PhysicsCore2D` guidance and updated the `FindFirstObjectByType` pitfall to its Unity 6.6 obsolete status.
  - Dropped the bundled `scripts/` checker to keep the vendored copy Markdown-only.
  - Added the ADR 0020 and repository-workflow project header.

## Why this copy exists

Presentation-layer MonoBehaviour lifecycle, serialization and execution-order guidance for the
Unity 6.6 port (ticket #200).
