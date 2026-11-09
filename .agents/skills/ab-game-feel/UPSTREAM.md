# Upstream

- Source: https://github.com/gamedev-skills/awesome-gamedev-agent-skills
- Path: skills/disciplines/game-feel
- Commit: 19d93f97c9c1ea0f8b306e77aa81e0d3e352d3e9
- License: Apache-2.0
- Notice: required
- Vendored: 2026-11-09
- Local modifications:
  - Renamed frontmatter `name` to `ab-game-feel` and rewrote the description for this project.
  - Added the project constraint header (ADR 0020 owner approval and palette, ADR 0022 faction hues, repository workflow precedence).
  - Added the "no effect that obscures gameplay" rule to the principles, pitfalls and accessibility section.
  - Replaced the Godot shake/tween snippets with Unity C# examples driven by decaying trauma and `CombatCameraRig`.
  - Added a recoil recipe that moves a visual pivot, never the simulated ship transform.
  - Replaced the Godot tween/particle/Cinemachine binding summary with this project's presentation hooks and `AudioTrigger` values.
  - Removed the 3D orbit/Cinemachine shake references and the Godot/Phaser bindings.
  - Added references to ADR 0004 combat telemetry, ADR 0010 fixed timestep and ADR 0022 palette roles.

## Why this copy exists

Impact feedback recipes for combat (hit-stop, shake, recoil, flashes) that stay
inside the original palette and ADR 0020 owner approval (ticket #199).
