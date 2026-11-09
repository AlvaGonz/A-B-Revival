# Upstream

- Source: https://github.com/gamedev-skills/awesome-gamedev-agent-skills
- Path: skills/disciplines/camera-systems
- Commit: 19d93f97c9c1ea0f8b306e77aa81e0d3e352d3e9
- License: Apache-2.0
- Notice: required
- Vendored: 2026-11-09
- Local modifications:
  - Renamed frontmatter `name` to `ab-camera-systems` and rewrote the description for this project.
  - Added the project constraint header (ADR 0020 owner approval, ADR 0005 distance presets, repository workflow precedence).
  - Retargeted every pattern to the URP 2D orthographic rig in `CombatCameraRig` and `CameraDistancePresets`.
  - Replaced the Godot `Camera2D` and Cinemachine examples with C# follow, deadzone, look-ahead and bounds code against `WorldViewportWidth` / `WorldViewportHeight`.
  - Removed the 3D orbit, spring-arm collision, first-person look, pitch clamp and cinematic camera-blend guidance.
  - Removed all Cinemachine and virtual-camera references from the body and the references file.
  - Reworked `references/follow-and-framing.md` for the 2D rig; kept the smoothing derivation, deadzone/look-ahead tuning, bounds clamp, group framing and transitions.
  - Retargeted related skills to the project's `ab-*` tree.

## Why this copy exists

Follow, deadzone, look-ahead and framing guidance for the ADR 0005 combat
camera (ticket #199). The project has one URP 2D orthographic rig, so the
3D/Cinemachine material was dropped.
