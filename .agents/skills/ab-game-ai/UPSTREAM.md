# Upstream

- Source: https://github.com/gamedev-skills/awesome-gamedev-agent-skills
- Path: skills/disciplines/game-ai
- Commit: 19d93f97c9c1ea0f8b306e77aa81e0d3e352d3e9
- License: Apache-2.0
- Notice: required
- Vendored: 2026-11-09
- Local modifications:
  - Renamed frontmatter `name` to `ab-game-ai` and rewrote the description for this project.
  - Added the project constraint header (ADR 0020 assets, ADR 0006 tactical modes, `TacticalAIJob`, `NativeArray`/Burst, repository workflow precedence).
  - Replaced the Godot snippets with C# examples in the shape of `TacticalAIJob` (`NativeArray`, `float2`, `math`).
  - Removed A* pathfinding, waypoint/grid graphs and the `references/pathfinding.md` file.
  - Removed behavior trees, blackboards and the `references/behavior-trees.md` file.
  - Removed all 3D navigation and engine navmesh guidance; the project has no navmesh, ECS or 3D movement layer.
  - Replaced the target-selection guidance with a nearest-hostile `NativeArray` loop and deterministic tie-break.
  - Retargeted related skills to the project's `ab-*` tree.

## Why this copy exists

FSM, target-selection and steering guidance for `TacticalAIJob` and the ADR 0006
Captain modes (ticket #199). Pathfinding and behavior-tree material was dropped
because the project does not use it.
