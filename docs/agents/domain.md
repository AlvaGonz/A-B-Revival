# Domain Documentation Layout

This repository uses a single-context domain documentation layout:

- `GLOSSARY.md` at the repo root defines the ubiquitous language of the game and codebase.
- `docs/adr/` contains Architecture Decision Records documenting irreversible or significant architectural choices.

## Guidelines for Agents
- When encountering ambiguous terminology or introducing new concepts, consult and update `GLOSSARY.md`.
- Keep terms sharp, concise, and aligned with the game mechanics and decompiled ActionScript engine architecture.
- For Unity 6000.6 migration work, adhere strictly to the entity mappings in ADR 0014:
  - Static catalogs/blueprints: `ScriptableObject`.
  - Simulation instances: Unmanaged blittable `struct` in `NativeArray<T>`.
  - Simulation jobs: `IJobParallelFor` with `[BurstCompile]`.
  - 2D collisions: `PhysicsCore2D` (`Unity.U2D.Physics`).


