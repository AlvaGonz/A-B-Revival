# Research: Automated Runtime Test Harness & Gates for Unity 6.6 (DOTS / PhysicsCore2D)

**Date**: 2026-11-07  
**Context**: Ticket #90 — automated runtime test harness and gates, updated for Unity 6.6 target (ADR 0015).  
**Primary Source**: Official Unity Documentation ([Unity Test Framework](https://docs.unity.com/en-us/packages/com.unity.test-framework/1.8/manual/index.html)), Unity 6000.6.8f1 Editor CLI documentation.

---

## 1. Problem Statement & Historical Context

Ticket #90 was originally scoped when Flash / AS3 Stage3D was the primary development target. At that time, compile-time checks (`lint_domain_leakage.py`, FFDec recompile) were the only gates, and runtime behavior (e.g. Captain AI stand-ground thrust zeroing) had to be verified by manual playtesting in standalone Flash Player.

With **ADR 0015**, Flash AS3 is sunsetted and frozen as a read-only reference baseline. **Unity 6.6 (6000.6.8f1) is the sole active production target**. Therefore, the automated runtime test harness must be implemented using **Unity Test Framework (UTF)** and Unity-native automation.

---

## 2. Primary Source Survey: Unity Test Architecture

### 2.1 Two Testing Modalities: EditMode vs PlayMode

| Feature | EditMode (`Editor`) | PlayMode (`Standalone/Player`) | Fast-Path CLI (`dotnet test`) |
|---|---|---|---|
| **Host Process** | Unity Editor (`-batchmode`) | Unity Engine Runtime Player | .NET SDK CLI (`dotnet`) |
| **Execution Speed** | 5 – 15 seconds (Editor startup) | 10 – 30 seconds (Player boot) | **100 – 200 ms** (instant JIT) |
| **Unity Native ECalls** | Full support | Full support | Mocked / Pure C# fallback |
| **Coroutines / Frames** | Single frame / async | Multi-frame `[UnityTest]` | N/A (synchronous) |
| **PhysicsCore2D Engine** | Available via Physics scene | Full multithreaded simulation | Pure integration methods |
| **Burst Compilation** | Supported in Editor | Fully compiled native code | Supported via reference |
| **Target Use Case** | Pre-merge CI, asset verification | Physics contacts, wave director | **Pre-commit hook, developer inner loop** |

### 2.2 Unity Test Framework CLI Automation (Primary Source Reference)

Unity provides standalone headless execution via command-line arguments:

```bat
"C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe" ^
  -batchmode ^
  -projectPath "c:\Dev\A&B revival\A&B-U" ^
  -runTests ^
  -testPlatform EditMode ^
  -testResults "reports/editmode-results.xml" ^
  -logFile "reports/unity-editmode.log"
```

Exit codes:
- `0`: All tests passed.
- `2`: One or more tests failed.
- `3`: Compilation error or startup failure.

### 2.3 Tiered Testing Strategy (Fast Loop + Deep Loop)

1. **Tier 1 (Fast Loop — Pre-Commit & TDD)**:
   - Tool: `dotnet test A&B-U/Assets/Scripts/Tests/ABRevival.Tests.csproj`
   - Scope: Pure C# domain logic, math, kinematics, delta accumulation (`CraftAssembly`), job integration loops (`CombatSimulation`), catalog parsing (`PartCatalogParser`).
   - Duration: **~120 ms**.
   - Gate: Blocks Git commit if tests fail.

2. **Tier 2 (Deep Loop — Pre-Merge & CI Gate)**:
   - Tool: Headless Unity Editor `-batchmode -runTests -testPlatform EditMode`
   - Scope: Asset database verification, ScriptableObject instantiation, `PartCatalogImporter` asset creation in `Assets/Data/Parts/`, Burst safety analysis.
   - Duration: **~10 seconds**.

3. **Tier 3 (Runtime PlayMode — Nightly / Release Gate)**:
   - Tool: Unity PlayMode runner with `PhysicsCore2D` scene.
   - Scope: Multi-frame collision solver, projectile contact sweeps, wave spawn intervals, HUD BatchRendererGroup instancing.
   - Duration: **~25 seconds**.

---

## 3. Retiring Obsolete Flash Gates

The current pre-commit hook (`scripts/install-hooks.bat`) runs:
1. `scripts/lint_domain_leakage.py --staged`
2. `scripts/recompile.bat` (FFDec SWF recompilation)

Since AS3 is now frozen and read-only (ADR 0015):
- If no AS3 files are modified, FFDec recompile is completely dead weight.
- The pre-commit hook should be updated to execute the **Tier 1 Fast Loop** (`dotnet test`) on staged C# changes.

---

## 4. Recommendations for Ticket #90 Resolution

1. Record **ADR 0016: Two-Tier Automated Testing Gate (Fast CLI .NET + Headless Unity UTF)**.
2. Update pre-commit hook to execute `dotnet test` whenever `A&B-U/Assets/Scripts/` files are modified.
3. Wire headless Unity CLI runner script (`scripts/run-unity-tests.bat`) for Tier 2 EditMode / PlayMode sweeps.
4. Close Ticket #90 as resolved and superseded by the Unity test architecture.

