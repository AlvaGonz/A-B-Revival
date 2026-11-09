---
name: ab-test-helpers
description: Unity Test Framework helper patterns for the whole A&B Revival project - fixtures, teardown, Boot scene loading, deterministic 60 Hz time, save and audio reset, and shared setup for the EditMode suites in Assets/Scripts/Tests and the PlayMode suites in Assets/Scripts/Tests/PlayMode. Use when the same setup is repeated across suites, when a new suite needs scene loading, or when tests leak state into each other.
---

> **Project constraint (A&B Revival).** Original Flash art, fonts and audio are the baseline (ADR 0020); improvements need explicit owner approval recorded in the issue/PR. The project has two test tiers (ADR 0016): Tier 1 runs the EditMode project through `dotnet test` in about 150 ms, and Tier 2 runs the Unity Test Framework gates in headless Unity. The repository workflow (wayfinder -> to-spec -> to-tickets -> TDD -> review) prevails over any step in this skill.

# Test Helpers

Helpers exist so a suite states its own scenario instead of repeating setup. They are small
plain classes and fixtures in the existing test assemblies, not a framework. The project
already has one shared PlayMode driver (`PlayModeBoot`) and several copied helpers; extend
the shared one instead of adding a third copy.

## Where helpers live

| Suite | Root | Assembly | Runs in |
|---|---|---|---|
| EditMode / Tier 1 | `A&B-U/Assets/Scripts/Tests/` | `ABRevival.Tests.asmdef` | `dotnet test` and Unity EditMode |
| PlayMode / Tier 2 | `A&B-U/Assets/Scripts/Tests/PlayMode/` | `ABRevival.Tests.PlayMode.asmdef` | headless Unity PlayMode |
| Input PlayMode | `A&B-U/Assets/Scripts/Tests/PlayMode/Input/` | `ABRevival.Tests.PlayMode.Input.asmdef` | headless Unity PlayMode |

A helper is a `.cs` file in the suite root or a `Helpers/` subfolder. The test assemblies
compile recursively, so there is no separate helper assembly and no new `.asmdef`.
`PlayMode/Input` already uses the Input System package's own `InputTestFixture`; do not
wrap it.

## Rules

- **Add a helper only when at least two suites repeat the setup.** One caller: keep it
  inline. Two suites: one small static method. Three or more: consolidate the copies.
- **Plain `static class` or a normal fixture.** A helper class must not carry
  `[TestFixture]` or extend the suite base class: NUnit would discover it as an empty
  suite. Only real test files are fixtures.
- **Assert through NUnit.** Use `Assert.That`, `Assert.IsNotNull`, `Assert.AreEqual` with a
  message. A helper never calls `Debug.Assert` or halts the run.
- **Tier 1 stays pure C#.** A helper used by the `ABRevival.Tests` project must not call
  Unity engine ECall: no `UnityEngine.Object`, `ScriptableObject.CreateInstance`,
  `Instantiate`, `GameObject` or `SceneManager`. That boundary is what keeps the fast loop
  alive (ADR 0016). Engine helpers live under `PlayMode/` or in a Tier-2-only EditMode file.
- **No framework.** No new assembly, no dependency injection container, no mocking library,
  no universal base fixture, no code generation. A helper is a few static methods or one
  fixture, and it is deleted when it stops earning its place.
- **Keep project conventions.** Namespace `ABRevival.Tests` or `ABRevival.Tests.PlayMode`,
  class name ending in `Tests` for fixtures, and the
  `Original game IP: Vargames (2011).` line in the fixture summary.

## EditMode patterns

### Repository paths and test data

The layout suites each carry a private `FindRepoRoot` (`MainMenuLayoutTests`,
`CenterLayoutTests`, `FactoryLayoutTests`, `InfoLayoutTests`, `ShopLayoutTests`,
`ReadyLayoutTests`, `CombatHudLayoutTests`, `TalkPlaybackTests`). When you touch the next
one, move the lookup into one small `TestPaths` static class. Do not refactor every suite
in the same change.

```csharp
using System.IO;
using NUnit.Framework;

public static class TestPaths
{
    public static string RepoRoot
    {
        get
        {
            for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
                 dir is not null;
                 dir = dir.Parent)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, ".git")) ||
                    File.Exists(Path.Combine(dir.FullName, ".git")))
                {
                    return dir.FullName;
                }
            }

            throw new DirectoryNotFoundException(
                "Repository root not found above " + TestContext.CurrentContext.TestDirectory);
        }
    }

    public static string FromRepo(params string[] parts) =>
        Path.Combine(RepoRoot, Path.Combine(parts));
}
```

Never keep a hardcoded fallback such as `C:\Dev\ab-wt\181`: it passes on one machine and
fails everywhere else. Fail with the searched directory instead.

Reading UXML, USS, controller source and catalog XML as text is the project's structural
guard pattern (for example `MainMenuLayoutTests`, `ShopLayoutTests`, `FontAssetTests`).
The helper resolves the path; the assertions stay in the suite so the failure message names
the screen.

### Fixtures, temp files and ScriptableObjects

- Use `[OneTimeSetUp]` for read-only data shared by the fixture and `[SetUp]` for mutable
  state. Keep both cheap; Tier 1 has a 150 ms budget for the whole suite.
- Write temp files under `TestContext.CurrentContext.WorkDirectory` or `Path.GetTempPath()`
  with a unique name, and delete them in `[TearDown]`. Do not write into the repository.
- In EditMode-only tests, destroy `ScriptableObject.CreateInstance` results with
  `Object.DestroyImmediate` in `[TearDown]` or `[OneTimeTearDown]`. A Tier 1 dotnet run
  cannot create those engine objects at all, so such tests belong to the Tier 2 gate.
- Build data with a factory only when several suites need the same valid object. A single
  `MakePart(...)` used once is noise; inline it.

## PlayMode patterns

### Scene loading

`Boot` is the only entry scene (`SampleScene` is the empty template). Reuse
`PlayModeBoot.LoadMainMenu`, `LoadCenter`, `WaitForMainMenu`, `WaitForCenter`, `StartGame`
and `Click` instead of copying them. The pattern is: load the scene, yield one frame, then
poll for the controller and its `UIDocument` root with a frame budget, and fail with the
state reached.

Add a `PlayModeBoot` method when another screen suite needs the same wait. Do not add a
second `WaitForState` copy: `GameFlowPlayModeTests`, `ReadyPlayModeTests` and
`ResultsPlayModeTests` already hold three identical private versions, which is the signal
to move one copy into `PlayModeBoot`.

Never wait for readiness with `Thread.Sleep`, `WaitForSeconds` or a fixed number of frames
without a condition. Those make the suite slow when it passes and flaky when the machine is
loaded.

### Teardown that actually resets state

`[UnitySetUp]` and `[UnityTearDown]` return `IEnumerator`; destroy objects and then
`yield return null` so Unity completes the destruction. Reset every piece of global state
the test can touch:

- Delete the save and its `.corrupt` / `.tmp` siblings from
  `Application.persistentDataPath` before and after the test
  (`MainMenuPlayModeTests`, `GameFlowPlayModeTests`).
- Destroy `AudioService.Instance` and any `DontDestroyOnLoad` object the test created.
- Unload additive scenes and return to a known state. `Boot` reloads cleanly; a test that
  leaves a mission running poisons the next fixture.
- Restore `Time.timeScale`, `Time.fixedDeltaTime`, `Application.targetFrameRate`,
  `Screen.SetResolution` and `PanelSettings.targetTexture`. Use `try/finally` or the
  teardown; `CombatHudResolutionPlayModeTests` shows the target-texture restore.
- Release every `RenderTexture` the test created (`Release()` then `Destroy`).
- Dispose every `NativeArray` / `NativeList` the test allocated, including on the failure
  path. A leaked persistent allocation fails the next leak check, not this test.

```csharp
private static string SavePath =>
    Path.Combine(Application.persistentDataPath, GameFlowController.DefaultSaveFileName);

[UnityTearDown]
public IEnumerator TearDown()
{
    if (File.Exists(SavePath)) File.Delete(SavePath);
    if (File.Exists(SavePath + ".corrupt")) File.Delete(SavePath + ".corrupt");
    if (File.Exists(SavePath + ".tmp")) File.Delete(SavePath + ".tmp");
    if (AudioService.Instance is not null) Object.Destroy(AudioService.Instance.gameObject);
    yield return null;
}
```

### Deterministic 60 Hz time

The simulation runs at a 60 Hz fixed step:

- `MissionRunner.DefaultTickSeconds = 1f / 60f`.
- `GameFlowController.FixedStepSeconds` aliases it.
- `ProjectSettings/TimeManager.asset` sets `Fixed Timestep: 0.0166667`.
- `GameFlowController.TickMission` accumulates and allows at most 4 substeps per frame.

Drive simulation by ticks, not by wall-clock waits:

```csharp
for (int tick = 0; tick < 240; tick++)
{
    flow.Tick(GameFlowController.FixedStepSeconds);
}
```

`CombatHudResolutionPlayModeTests` uses `flow.Tick(GameFlowController.FixedStepSeconds *
4f)` for the same reason. A `[UnityTest]` yields frames, so frame time is not deterministic;
assert on simulation state and exact tick counts, and yield only for presentation, UI layout
and scene activation.

For PhysicsCore2D, use a scripted world (`PhysicsWorld.SimulationType.Script`) and step it
explicitly, as `WorldUnitPhysicsTests` does. Do not let the automatic world run while
asserting positions.

Do not change `Time.timeScale` to speed a test up unless the test is about time scale.
If it must change, restore it in teardown even when the test fails.

## Shared setup for the whole project

The names below are suggestions; the rule is one shared copy, not these exact classes.

| Shared need | Where it repeats | Small helper |
|---|---|---|
| Repository root and data paths | all layout suites, `TalkPlaybackTests`, catalog tests | `TestPaths` in the EditMode root |
| Reaching a `GameFlowState` | `GameFlowPlayModeTests`, `ReadyPlayModeTests`, `ResultsPlayModeTests` | one `PlayModeBoot.WaitForState` |
| Boot, main menu, center | every screen PlayMode suite | `PlayModeBoot.LoadMainMenu` / `LoadCenter` |
| Save reset | main menu, game flow, shop, results | `TestSave.Delete` or a `PlayModeBoot` method |
| Audio singleton reset | main menu, game flow, shop, results | `TestAudio.Reset` or a `PlayModeBoot` method |
| UI document root | every screen suite | `PlayModeBoot.Document(controller)` |
| Fixed-step driving | combat HUD, fleet simulation, game flow | `PlayModeBoot.Step(flow, ticks)` |
| Scripted physics world | `WorldUnitPhysicsTests`, projectile contact | keep local until a third suite needs it |
| Input actions | `PlayerFlightInputPlayModeTests` | use `InputTestFixture` directly |

The menu is one consumer of these helpers, not the reason they exist. A new combat, shop,
result or input suite should reuse the same `TestPaths`, `PlayModeBoot` and teardown
patterns rather than start its own copies.

## When a helper is the wrong answer

- The setup appears once. Inline it; a helper used by one test hides the scenario.
- The helper would assert for the test. Assertions belong in the suite, where the failure
  message names the behavior.
- The helper needs a new assembly, a package or a mocking library. The project already has
  NUnit, the Input System test fixture and Unity Test Framework; that is enough.
- The helper hides a real dependency. Construct the domain object directly.
- The helper only exists to shorten a name. Keep the descriptive name.

## Verify

- Tier 1: `dotnet test "A&B-U/Assets/Scripts/Tests/ABRevival.Tests.csproj" --nologo`.
- Tier 2: `scripts/run-unity-tests.bat --unity`.
- A helper change must keep both gates green. If a helper only compiles or runs in one
  tier, it belongs in that tier's root, not in the shared path.
- After adding or moving a helper, run the suite that used it at least twice in a row. State
  that leaks between runs shows up on the second run, not the first.
