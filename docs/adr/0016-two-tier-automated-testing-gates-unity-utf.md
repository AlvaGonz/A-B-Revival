# 16. Two-Tier Automated Testing Gate for Unity 6.6 (Fast CLI .NET + Headless Unity UTF)

**Date:** 2026-11-07  
**Author:** Architecture & Engineering Team  
**Status:** Accepted  
**Supersedes/Resolves:**
- Ticket #90 (*Research & grilling: automated runtime test harness and gates*)
- `docs/adr/0010-fixed-timestep-simulation-and-runtime-performance-gate.md` (Flash Player trace runtime gate)

---

## Context & Problem Statement

Historically, Ticket #90 explored automated runtime testing gates to verify gameplay simulation behaviors (such as Captain AI stand-ground thrust zeroing or particle leaks) that compile-time gates could not detect. At that time, candidates centered around the standalone Flash Player debug binary (`flashplayer_sa_debug.exe`), `flashlog.txt` trace capture, and FFDec bytecode patching.

With **ADR 0015**, Flash ActionScript 3 was permanently sunsetted and designated as a frozen read-only reference baseline. **Unity 6.6 (6000.6.8f1) is the sole active production target**. 

Unity projects often suffer from slow automated testing cycles because launching the Unity Editor engine takes 10–30 seconds, making full engine startup unacceptable for local pre-commit hooks and rapid test-driven development (TDD). Furthermore, running tests purely inside the standalone .NET CLI runtime fails when code directly invokes native Unity engine internal calls (`ECallMethods`, such as `ScriptableObject.CreateInstance` or `NativeArray` allocations without the native Unity player library loaded).

The architecture requires a testing strategy that satisfies two competing demands:
1. **Sub-second developer inner loop and pre-commit verification**: Immediate feedback on domain math, assembly stat deltas, catalog decoding, and simulation physics integration without launching Unity.
2. **Deep engine-level verification**: Validating asset database assets, ScriptableObjects (`PartDefinitionSO`), Sprite Atlases, physics scene queries, and rendering batching inside the real Unity runtime.

---

## Decision

1. **Formal Retirement of Flash Runtime Test Harness**:
   - The Flash Player trace logging harness (`scripts/test_perf_runtime.py`) and FFDec bytecode instrumentation proposed in Ticket #90 and ADR 0010 are decommissioned and marked superseded.
   - All runtime verification efforts are migrated exclusively to Unity 6.6.

2. **Tier 1: Standalone .NET CLI Fast Loop (Pre-Commit Gate & TDD)**:
   - **Runner**: `dotnet test "A&B-U/Assets/Scripts/Tests/ABRevival.Tests.csproj" --nologo`
   - **Execution Time**: ~120–180 ms for the complete test suite.
   - **Scope**:
     - Pure C# domain entities and value objects (`CraftAssembly`, `ShipEntity`, `MountedPartData`, `ProjectileData`).
     - State machine validation and invariant enforcement (wing limits, $O(1)$ incremental stat delta tracking, assembly error codes).
     - XML catalog decoding (`PartCatalogParser`, decoding all 271 components from binary XML dumps).
     - Kinematics and weapon range formulas (ADR 0012: $d = v_0 \cdot t + 0.5 \cdot a \cdot t^2$).
     - Multi-tick simulation step verification (`CombatSimulation`, 600 ticks / 10 seconds continuous combat with 0 GC allocations).
   - **Integration Gate**: Configured as the primary Git pre-commit hook (`.githooks/pre-commit`). Any commit modifying files in `A&B-U/` must exit `0` on Tier 1 tests.

3. **Tier 2: Headless Unity Test Framework (UTF) Gate (CI / Merge Gate)**:
   - **Runner**: Headless Unity Editor batchmode CLI:
     ```bat
     Unity.exe -batchmode -projectPath "c:\Dev\A&B revival\A&B-U" -runTests -testPlatform EditMode -testResults "reports/editmode-results.xml" -logFile "reports/unity-editmode.log"
     ```
   - **Scope**:
     - ScriptableObject asset creation and disk serialization (`PartCatalogImporter` generating `.asset` files in `Assets/Data/Parts/`).
     - Asset database integrity, Sprite Atlas texture compression, and shader compilation.
     - PlayMode multi-frame physics simulation (`PhysicsCore2D` contacts, raycasts, and entity contact sweeps).
     - Burst compilation validation and native safety checks.
   - **Execution Frequency**: Run during CI workflows, before merges to `main`, and accessible locally via `scripts/run-unity-tests.bat`.

4. **Pure C# Domain Decoupling Contract**:
   - High-frequency simulation logic, algorithms, and data parsing must never directly depend on Unity native C++ `ECall` methods (such as `ScriptableObject.CreateInstance` or `UnityEngine.Object.Instantiate`).
   - Pure domain models (`PartDefinitionRecord`, `CraftAssembly`, `ShipEntity`) remain testable in Tier 1.
   - Unity-specific bridges (`PartCatalogImporter`, `MonoBehaviour` views, ScriptableObject wrappers) reside in `ABRevival.Editor` or presentation layers tested in Tier 2.

---

## Consequences

### Positive
- **Instant Inner Feedback**: Developers run the full suite in under 200 ms during TDD and commits, completely eliminating friction.
- **Zero-Error Gate Enforced**: Git pre-commit hook guarantees no broken domain models or simulation regressions reach the repository.
- **Engine-Level Fidelity**: Headless UTF guarantees that Unity assets, shaders, and physics integrate without relying on manual playtesting.
- **Clean Architecture Enforcement**: The inability to run `ECall` methods in Tier 1 acts as an architectural boundary, preventing domain leakage into Unity engine classes.

### Negative / Trade-offs
- Two test execution contexts must be maintained: `ABRevival.Tests.csproj` (for .NET SDK) and `ABRevival.Tests.asmdef` (for Unity Test Framework).
- Integration tests involving actual Unity scenes or GameObject hierarchies must run in Tier 2 (UTF) rather than Tier 1.

