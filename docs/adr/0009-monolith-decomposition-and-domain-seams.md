# 9. Monolith Decomposition and Domain Seams under Flash/AS3 Constraints

Date: 2026-11-06

## Status
Accepted

## Context
The *Assembly & Battle* codebase contains several large decompiled God-Class monoliths (`UIMakePlayer.as` at 1,545 lines, `AIUnity.as` at 1,063 lines, `UIGame.as` at 841 lines, `Targets.as` at 721 lines), alongside ~2,000 lines of obsolete, defunct third-party ad networking code (`mochi/as3/*`). We need to safely decompose these monoliths into deep modules with clean seams while honoring the constraints of ActionScript 3 bytecode and the JPEXS FFDec CLI `-importScript` build pipeline.

## Decision

1. **Tooling Boundary & Compilation Invariant**:
   - The original game binary contains exactly 156 pre-registered ABC script tags.
   - FFDec CLI `-importScript` will silently drop brand new standalone `.as` files if they are not already mapped in the SWF's script index.
   - Therefore, all refactoring must use one of two compiler-safe architectural patterns:
     - **Domain-Hub Delegation**: Moving business logic, calculations, and domain queries to established domain hubs (`F4DataLinks`, `F4DataHole`, `F4DataOrg`, `Targets`).
     - **Translation-Unit Helper Pattern**: In AS3, auxiliary classes can reside in the same `.as` source file outside the `package` block. FFDec compiles these into the primary script's ABC bytecode block without symbol omission.

2. **Decomposition Priority**:
   - **Ticket 1 (#96)**: Neutralize `MochiAd.as`, `MochiServices.as`, and `MochiEvents.as` into zero-overhead no-op stubs and purge startup calls in `VeryOutShitABC.as`.
   - **Ticket 2 (#97)**: Extract craft statistics and complexity math out of `UIMakePlayer` into a pure calculation method on `F4DataLinks` / `F4DataHole`.
   - **Ticket 3 (#98)**: Encapsulate stage traversal and dev overlay metrics out of `Fly4UI.loop()` into a dedicated file-scoped helper.
   - **Ticket 4 (#99)**: Consolidate entity pool teardown in `Targets.as` using a unified pool registry loop.

3. **Domain Encapsulation Contract**:
   - UI views (`src/as3/scripts/ui/`) must remain purely presentational: binding model properties to text fields and handling user input events.
   - No calculation of craft physics, hardpoint validity, or slot rules shall occur inside UI classes.

## Consequences
- Radically shrinks `UIMakePlayer.as` and `Fly4UI.as` without breaking the build.
- Eliminates dead network initialization latency from Mochi Media.
- Preserves 100% build compatibility with `scripts/recompile.bat` and `lint_domain_leakage.py`.
