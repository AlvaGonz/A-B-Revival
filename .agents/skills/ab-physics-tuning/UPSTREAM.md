# Upstream

- Source: https://github.com/gamedev-skills/awesome-gamedev-agent-skills
- Path: skills/disciplines/physics-tuning
- Commit: 19d93f97c9c1ea0f8b306e77aa81e0d3e352d3e9
- License: Apache-2.0
- Notice: required
- Vendored: 2026-11-09
- Local modifications:
  - Renamed frontmatter `name` to `ab-physics-tuning`.
  - Rewrote the body for Unity 6.6 PhysicsCore2D (`Unity.U2D.Physics`), 60 Hz step, 100 px = 1 m, swept projectiles and contact filters. Removed Godot and legacy Rigidbody2D guidance.
  - Kept the engine-neutral reference `references/timestep-and-ccd.md` with a pointer that the Unity API lives in SKILL.md.

## Why this copy exists

Physics stability guidance matching the project's actual simulation layer (ticket #192).
