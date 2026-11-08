# Research: 2D Unity Runtime E2E Testing Frameworks & Headless Automation

**Date:** 2026-11-08  
**Ticket:** #145 (Research: survey 2D Unity runtime E2E testing frameworks & headless automation)  
**Parent:** Wayfinder Map #122  
**Status:** Complete  
**Related:** ADR 0016 (Two-Tier Automated Testing Gate), Ticket #90

---

## 1. Executive Summary

This spike asks the "Playwright equivalent for Unity" question: can *A&B Revival* launch the game, inject player input, inspect runtime state, assert gameplay transitions, and capture failure evidence unattended in CI?

The survey covers the five candidates named in the ticket: AltTester, Unity Automated QA, Poco/Airtest, the Input System `InputTestFixture`, and headless Unity Test Framework (UTF) PlayMode tests with direct state assertions.

**Recommendation: adopt native `InputTestFixture` + UTF PlayMode as the Tier 2 E2E lane. Do not add a third-party driver to the merge gate.**

The reasoning is specific to this project, not a generic preference:

1. **ADR 0016 already defines Tier 2 as headless UTF PlayMode.** The native option fits the accepted gate; AltTester would add a second runner, a server process, and a commercial license contract outside the ADR.
2. **The game state that matters is DOTS/blittable, not a GameObject hierarchy.** `CombatSimulation`, `ShipEntity`, `ShipInput`, and `ShipMovementJob` are plain C# arrays/`NativeArray` buffers of blittable structs. A hierarchy-driven driver cannot see them without custom proxy commands. A C# test can assert them directly.
3. **Determinism is a gate requirement.** The native path can step `CombatSimulation.Tick(1f / 60f)` for an exact number of ticks. AltTester/Poco/Automated QA drive a real-time player and depend on waits/timeouts.
4. **Cost and licensing.** AltTester's free Lite tier is GUI-only and ships the GPL-3.0 Unity SDK; CI batch mode and the non-GPL SDK require Pro at €75 + VAT per seat per month. The native stack is already paid for by Unity and already present in the project (`com.unity.inputsystem` 1.20.1, `com.unity.test-framework` 1.8.0).
5. **Compatibility risk.** AltTester 2.4.0 (5 Nov 2026) lists Unity 6.5 support; this project is Unity 6.6 (6000.6.8f1). Unity Automated QA has been on hold since December 2021. Poco's official Unity SDK still documents Unity 4/5.

AltTester remains the leading candidate **if** the project later needs true black-box automation of a built player on real devices, with screenshots and multi-language bindings. That should be a separate nightly/release lane and a separate ADR, not a replacement for the Tier 2 state gate.

---

## 2. Scope, Baseline & Method

**Project baseline verified in-repo:**

| Item | Value |
|---|---|
| Unity Editor | `6000.6.8f1` (`A&B-U/ProjectSettings/ProjectVersion.txt`) |
| Render pipeline | URP 17.6.0 |
| Input | `com.unity.inputsystem` 1.20.1 |
| Test framework | `com.unity.test-framework` 1.8.0 |
| Physics | `com.unity.modules.physicscore2d` 1.0.0 |
| Tier 1 | `dotnet test A&B-U/Assets/Scripts/Tests/ABRevival.Tests.csproj` (~150 ms) |
| Tier 2 | Headless UTF, currently EditMode only via `scripts/run-unity-tests.bat --unity` |
| PlayMode test assembly | `A&B-U/Assets/Scripts/Tests/PlayMode/ABRevival.Tests.PlayMode.asmdef` (empty — no PlayMode tests yet) |
| Presentation layer | `ABRevival.Presentation.asmdef` only — no `MonoBehaviour` gameplay host yet |
| Scenes | `SampleScene.unity` only — no battle scene yet |

**Evaluation criteria** (from the ticket): Unity 6.6 compatibility, headless CI execution, determinism/clock control, maintenance/licensing, authoring ergonomics. I added two project-specific lenses: DOTS entity inspection and true black-box/built-player coverage.

**Method:** current vendor documentation, release notes, repository metadata, and the project's own source/ADR. Prices and versions are point-in-time facts (8 Nov 2026) and must be re-checked before purchase.

---

## 3. Candidate Technical Survey

### 3.1 AltTester (formerly AltUnity Tester)

**What it is.** AltTester instruments a Unity app with an SDK. The SDK opens a WebSocket client inside the app and talks to the AltTester Server in the AltTester Desktop application. Drivers in C#, Python, Java, and Robot Framework connect to that server and use an Appium/Selenium-style `AltDriver` to find objects, read component properties, click/tap/swipe, call methods, load scenes, and take PNG screenshots ([AltTester overview](https://alttester.com/docs/sdk/latest/pages/overview.html)).

**Strengths.**

- True black-box-ish E2E: tests can drive a built player or the Editor, on PC, mobile, and WebGL.
- Rich object API: `FindObject`, `FindObjects`, hierarchy paths, `AltId`, component properties, `CallComponentMethod`, scene commands, time scale, PlayerPrefs, screenshots ([commands reference](https://alttester.com/docs/sdk/latest/pages/commands.html)).
- Page Object pattern and generated scripts; bindings for C#, Python, Java, Robot.
- Active release cadence: 2.3.0 (Jan 2026) added Unity 6 support and updated New Input System support; 2.4.0 (5 Nov 2026) added Unity 6.5 support and fixed input/stripping issues ([2.3.0 notes](https://github.com/alttester/AltTester-Unity-SDK/releases/tag/2.3.0), [2.4.0 notes](https://github.com/alttester/AltTester-Unity-SDK/releases/tag/2.4.0)).

**Project-specific limits.**

- **Unity 6.6 is not listed.** The latest release supports Unity 6.5. 6.6 may work, but it is outside the vendor's tested matrix.
- **New Input System caveat is material.** AltTester's own known-issues page states that "Player Input is not working when connected to AltTester Unity SDK/Desktop" and that it affects "all input actions created with the New Input System for app objects" ([known issues](https://alttester.com/docs/sdk/latest/pages/known-issues.html#player-input-is-not-working-when-connected-to-alttester-unity-sdkdesktop)). This project uses Input System 1.20.1 as its input path. The page also lists `wasPressedThisFrame`/`wasReleasedThisFrame` problems with `PressKey`. These are exactly the APIs a gameplay E2E test would exercise.
- **DOTS invisibility.** The API is built around Unity's GameObject/Component hierarchy. `ShipEntity` is a blittable struct in a `NativeArray`/array buffer, not a GameObject. AltTester cannot query it without custom commands or proxy GameObjects, which defeats the purpose.
- **Real-time by design.** It drives a running player over a socket, with command delays, implicit timeouts, and `SetTimeScale`. Frame-exact deterministic stepping is not its model.
- **CI overhead.** Headless CI requires an instrumented build, the AltTester Server, and the driver process. Pricing explicitly puts "CI/CD Pipeline support for batchmode on MacOS, Windows and Linux" in Pro, not Lite.

**Licensing.** The public Unity SDK repository is GPL-3.0 ([GitHub metadata](https://github.com/alttester/AltTester-Unity-SDK)). The free Lite tier includes the GPL-3.0 Unity SDK, one connection, GUI mode only, and is limited to studios under €500k annual revenue. Pro is €75 + VAT per seat per month, includes the non-GPL SDK and 2 parallel connections, and is the only tier with batchmode CI support ([AltTester pricing](https://alttester.com/pricing/)). For a proprietary fan project that must preserve Vargames' IP and cannot relicense the game under GPL, Lite is not a viable CI option.

**Verdict:** Best third-party option, but wrong tool for the current Tier 2 gate. Revisit only for a black-box/device lane with a funded Pro license and a verified Unity 6.6 + Input System 1.20.1 spike.

---

### 3.2 Unity Automated QA (`com.unity.automated-testing`)

**What it is.** An experimental Unity package for recording and replaying touch/click/drag interactions on Unity UI, generating UTF tests from recordings, and running tests on local or Unity-hosted iOS/Android devices ([package manual](https://docs.unity3d.com/Packages/com.unity.automated-testing@0.8/manual/index.html)).

**Naming correction.** The ticket calls it `com.unity.automated-qa`. The actual package is **`com.unity.automated-testing`**; the docs and changelog use that name.

**Status.** The package is effectively abandoned. The 0.8.1 changelog entry (2021-12-06) says: "Automated QA Development has been put on hold until further notice" ([changelog](https://docs.unity3d.com/Packages/com.unity.automated-testing@0.8/changelog/CHANGELOG.html)). The docs still say Unity 2019.4+ and were generated in October 2023. No Unity 6 validation or release exists.

**Strengths.**

- No-code record/replay; useful for quick UI smoke tests.
- Generates C# UTF tests with Page Objects; supports custom automators and a game crawler.
- Cloud device farm integration for iOS/Android (subject to Unity entitlements).

**Limits.**

- Recording is limited to clicks/touches/drags on **Unity UI** elements; the docs explicitly say keyboard input is not supported by the recorder ([Recorded Playback limitations](https://docs.unity3d.com/Packages/com.unity.automated-testing@0.6/manual/RecordedPlayback.html)).
- The recorder does not capture keyboard or gamepad input; there is no gameplay simulation and no DOTS entity inspection.
- No Unity 6 support; the package predates the project's entire stack (URP 17, PhysicsCore2D, Input System 1.20).
- The cloud device farm is not a Linux headless CI runner; it is a separate Unity service.
- Experimental API with known breaking changes across 0.x versions.

**Verdict:** Reject. It is stale, UI-only, and does not cover thrust/combat/physics state. Keeping it in the project would be a maintenance liability.

---

### 3.3 Poco / Airtest Project (NetEase)

**What it is.** Airtest is an image-recognition automation framework; Poco is its UI-inspection companion. A Poco SDK embedded in the game exposes a runtime UI hierarchy over an RPC socket; a Python client (`pocoui`) selects nodes by name/type and performs clicks, swipes, drags, and waits. Airtest can also fall back to image matching. The Unity3D driver is one of several engine drivers ([Poco README](https://github.com/AirtestProject/Poco), [Poco SDK integration guide](https://airtest.doc.io.netease.com/en/IDEdocs/poco_framework/6_poco_sdk/)).

**Strengths.**

- Free and open source (Apache-2.0).
- Cross-engine: Unity, Cocos2d-x, Android/iOS native.
- Simple Python selectors and a hierarchy inspector; image recognition can handle non-UI elements.
- Widely used in mobile game QA.

**Limits.**

- **Stale Unity support.** The official integration guide says PocoSDK supports "Unity3D version 4 and above" and the older ReadTheDocs page says "Unity3D version 4 & 5" ([integration guide](https://poco.readthedocs.io/en/latest/source/doc/integration.html)). No Unity 6 validation was found.
- **Low maintenance activity.** The main Poco repository's last commit was 2024-01-08; the Poco-SDK repository's last commit was 2024-06-24 (GitHub API metadata). The README advertises Python 2.7 and 3.3–3.6 compatibility, a strong signal of its era.
- **UI hierarchy only.** Poco sees GameObjects/UI nodes, not DOTS entities. Combat and physics state are invisible without custom SDK extensions.
- **Not CI-native for Unity.** It requires a running app plus a Python environment and, for mobile, ADB/device setup. There is no headless Unity runner integration comparable to UTF.
- **Real-time waits.** Selector waits and image matching are wall-clock based; deterministic frame stepping is not supported.

**Verdict:** Reject for this project. Apache-2.0 is attractive, but the Unity SDK is unmaintained for modern Unity and blind to the simulation state that matters.

---

### 3.4 Unity Input System `InputTestFixture`

**What it is.** An official test fixture shipped with `com.unity.inputsystem` (the project pins 1.20.1). `InputTestFixture` puts the Input System into a known state with no devices, lets the test add virtual devices, inject state events, and restores the previous Input System state on teardown. It works in EditMode and PlayMode tests ([Set up test fixtures](https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/set-up-test-fixtures.md), [API reference](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/api/UnityEngine.InputSystem.InputTestFixture.html)).

**Capabilities relevant to E2E.**

- Add virtual `Gamepad`, `Keyboard`, `Mouse`, `Touchscreen`, or custom devices.
- `Press`, `Release`, `PressAndRelease`, `Set`, `Trigger`, `Click`, `BeginTouch`/`MoveTouch`/`EndTouch`, `Move` for pointer controls.
- Explicit event timestamps and `currentTime`, so input timing can be controlled rather than inherited from wall-clock.
- Sever the Input System from platform input for the duration of the test, so a developer's real keyboard/mouse cannot interfere.
- Assert action state with NUnit or subscribe to `InputActionTrace`.

**Setup requirements (easy to miss).**

1. `com.unity.inputsystem` must be listed in `"testables"` in `A&B-U/Packages/manifest.json`; otherwise the package's test framework assembly is not included ([Set up test assemblies](https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/set-up-test-assemblies.md)).
2. The test asmdef must reference `Unity.InputSystem` and `Unity.InputSystem.TestFramework` in addition to `UnityEngine.TestRunner`/`UnityEditor.TestRunner` and `nunit.framework.dll`.
3. Do not add `[SetUp]`/`[TearDown]` methods in a class deriving from `InputTestFixture`; override `Setup()`/`TearDown()` instead, or the fixture lifecycle will not run.

**PlayMode nuance.** In a `[UnityTest]`, `queueEventOnly` is ignored and multi-frame PlayMode tests process input as part of the Unity player loop. A queued `Set`/`Press` needs a `yield return null` before the action/control is read ([`Set` API remarks](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/api/UnityEngine.InputSystem.InputTestFixture.html)). In a single-frame `[Test]`, `Set` processes the event immediately.

**Limits.**

- It is an input fixture, not a driver. It cannot inspect a remote built player, take screenshots, record video, or query a scene hierarchy from outside the process.
- It does not provide a simulation clock; deterministic stepping comes from your test code (fixed `deltaTime`, manual ticks, or `Time.captureDeltaTime`).
- It only exercises what the test code calls. A true scene-load/menu-navigation test still needs UTF scene management and a presentation host.

**Verdict:** Recommended input layer. It is official, dependency-free beyond the Input System package already in the project, and it drives the exact input path the game uses.

---

### 3.5 Headless Unity UTF PlayMode with State Assertions

**What it is.** Unity Test Framework PlayMode tests run in the Editor's play mode (or in a built Player) and can use `[UnityTest]` coroutines to span frames, load scenes, query GameObjects, step physics, and assert domain state. The Tier 2 command already exists in ADR 0016 for EditMode; PlayMode is the same runner with `-testPlatform PlayMode` ([UTF command-line reference](https://docs.unity.com/en-us/engine/6000.6/manual/programming-environment/test-framework-introduction/reference-command-line)).

**Command line.**

```bat
Unity.exe -batchmode -projectPath "A&B-U" ^
  -runTests -testPlatform PlayMode ^
  -testResults "reports/playmode-results.xml" ^
  -logFile "reports/playmode.log"
```

- `-testPlatform PlayMode` runs PlayMode tests **in the Editor**, not in a built player ([UTF command-line reference](https://docs.unity.com/en-us/engine/6000.6/manual/programming-environment/test-framework-introduction/reference-command-line)).
- Passing a `BuildTarget` (for example `StandaloneLinux64`) builds and runs a Player instead. That is the path to true built-player E2E, at the cost of a build per run.
- `-nographics` runs batch mode without initializing a graphics device, which is what makes GPU-less Linux/Windows runners viable; output logs are disabled unless `-logFile` is supplied ([Editor command-line reference](https://docs.unity.com/en-us/engine/6000.6/manual/unity-editor/command-line-arguments/editor)).
- The Editor's `-quit` argument must not be used while tests are running.

**Headless CI reality.**

- Unity license activation is required. GameCI's `unity-test-runner` action documents `testMode: playmode` on `ubuntu-latest` with `UNITY_LICENSE`/`UNITY_EMAIL`/`UNITY_PASSWORD` secrets ([GameCI test runner](https://game.ci/docs/github/test-runner/)).
- Editor startup is tens of seconds, so PlayMode is a merge/CI gate, not a pre-commit gate. That matches ADR 0016's two-tier split.
- `-nographics` means no rendering, so visual assertions and screenshots are not meaningful. If a future test needs them, drop `-nographics` and run on a GPU/headed runner (or a virtual display with software GL).
- The `-nographics` docs warn that simulated input workflows need a focused window. That warning applies to OS-level input simulation; `InputTestFixture` injects Input System state events inside the process, so it is not subject to window focus.

**Determinism options.**

- Manual fixed-step: call `CombatSimulation.Tick(1f / 60f)` a fixed number of times. No `Time.deltaTime`, no `WaitForSeconds`, no frame-rate coupling.
- Engine step: set `Time.captureDeltaTime` or `Time.fixedDeltaTime` and yield fixed frames; use `Physics2D.Simulate`/PhysicsCore2D stepping for physics queries.
- Prefer manual ticks for domain logic; use engine stepping only where the real Unity player loop is part of the behavior under test (scene load, `MonoBehaviour.Update`, physics contacts).

**Failure evidence.** UTF writes an NUnit XML result file and a full Editor log (`-testResults`, `-logFile`). With `-nographics`, screenshots are blank, so the useful failure artifact is a state dump: tick count, input values, ship/projectile positions, velocities, and health. Attach that text to the test result or log it before the assertion. Screenshots or video require a graphics-enabled runner and belong to a separate visual lane.

**Limits.**

- In-process and white-box: it can cheat by calling domain methods directly. That is acceptable for a state gate, but it is not a substitute for a built-player smoke test.
- No remote device farm, no OS-level input, no video recording.
- PlayMode test assemblies add compile time and require a licensed Editor in CI.

**Verdict:** Recommended execution layer. Pair it with `InputTestFixture` for the input boundary and direct domain assertions for the state boundary.

---

## 4. Comparative Matrix

Legend: ✅ strong / ⚠️ usable with caveat / ❌ weak or unsuitable.

| Criterion | AltTester | Unity Automated QA | Poco / Airtest | `InputTestFixture` | Headless UTF PlayMode |
|---|---|---|---|---|---|
| **Unity 6.6 compatibility** | ⚠️ 2.4.0 lists Unity 6.5; 6.6 untested | ❌ abandoned 2021; docs say 2019.4+ | ❌ official Unity SDK docs target Unity 4/5 | ✅ shipped with Input System 1.20.1 (project pin) | ✅ UTF 1.8.0 on 6000.6.8f1 |
| **Headless CI** | ⚠️ Pro only: instrumented build + server + driver; batchmode supported | ❌ no Linux headless; cloud device farm is separate | ❌ running app + Python/ADB; no UTF integration | ✅ in-process; no extra service | ✅ `-batchmode -nographics -runTests -testPlatform PlayMode` |
| **Determinism / clock control** | ❌ real-time socket; command delays/timeouts; `SetTimeScale` only | ❌ timestamped replay with dynamic waits | ❌ real-time selector/image waits | ✅ explicit state events, timestamps, `currentTime` | ✅ fixed `Tick(dt)`, manual frame stepping, scene control |
| **Maintenance / licensing** | ⚠️ GPL-3.0 Lite; Pro €75+VAT/seat/month for non-GPL + CI | ❌ experimental, on hold since 2021 | ✅ Apache-2.0, but last commits 2024 and Python 2.7-era | ✅ Unity package, maintained with Input System | ✅ Unity package, maintained with Editor |
| **Authoring ergonomics** | ✅ C#/Python/Java/Robot; Page Objects; recorder | ⚠️ no-code recorder; generated tests; UI-only | ⚠️ Python external scripts; simple selectors | ✅ C# NUnit in the same repo; IDE debugging | ✅ C# NUnit `[UnityTest]`; same assertions as Tier 1 |
| **DOTS / blittable state inspection** | ❌ GameObject/Component hierarchy only | ❌ Unity UI elements only | ❌ UI hierarchy only | ✅ direct C# access to `ShipEntity`/`NativeArray` | ✅ direct C# access; can assert `CombatSimulation` state |
| **True black-box / built player** | ✅ built player + real devices | ⚠️ device farm only, stale | ✅ real devices | ❌ in-process only | ⚠️ Editor batchmode; Player via `-testPlatform <BuildTarget>` |
| **Screenshots / visual evidence** | ✅ `GetPNGScreenshot` (needs graphics) | ✅ device screenshots | ✅ image capture | ❌ | ⚠️ only without `-nographics` |
| **CI overhead** | High: instrumented build, server, socket, driver | High / external cloud | High: device + Python environment | Low: no extra process | Low–medium: Editor boot (tens of seconds) |
| **Current fit for this project** | 2/5 (future black-box lane) | 1/5 | 1/5 | 5/5 (input layer) | 5/5 (execution layer) |

**Reading the matrix.** The third-party tools win on true black-box coverage and screenshots. The native tools win on everything ADR 0016's Tier 2 gate actually needs: Unity 6.6 compatibility, zero additional licensing, deterministic simulation stepping, and direct access to DOTS state.

---

## 5. Architectural Recommendation

### 5.1 Decision

**Use `InputTestFixture` + UTF PlayMode as the standard Tier 2 E2E approach.** Do not adopt AltTester, Unity Automated QA, or Poco for the merge gate.

```
Tier 1 (pre-commit, ~150 ms)
  dotnet test ABRevival.Tests.csproj
  └─ pure C# domain, math, catalog, simulation ticks

Tier 2 (CI / merge, tens of seconds)
  Unity -batchmode -runTests -testPlatform PlayMode
  └─ ABRevival.Tests.PlayMode
       ├─ InputTestFixture: virtual gamepad/keyboard/touch
       ├─ scene load + presentation host (when it exists)
       ├─ fixed-step CombatSimulation / PhysicsCore2D
       └─ direct assertions on ShipEntity, CombatSimulation, projectiles
```

### 5.2 Why this is the right call for ADR 0016

ADR 0016 accepted two gates: a fast pure-.NET Tier 1 and a headless Unity UTF Tier 2. The native approach is the only candidate that is *already* Tier 2. It needs no new ADR, no new server, no new license, and no new CI job type. It also preserves the architectural contract that domain logic stays Unity-free: the PlayMode test references `ABRevival.Core` and asserts its state, while the domain assembly remains testable in Tier 1.

AltTester would be a **third** execution context: Tier 1 .NET, Tier 2 UTF, plus an instrumented player driven by AltTester Server. That is a significant increase in surface area for a gate that currently needs to answer questions like "does full thrust move the ship forward?" and "does the Captain AI zero thrust on stand-ground?" — questions that are answered by reading `ShipEntity`, not by clicking a UI hierarchy.

### 5.3 Why the third-party drivers lose on this project's actual bugs

The regressions the gate must catch are simulation regressions:

- thrust zeroing in `CaptainTacticalMode` / `TacticalAIJob`
- ship displacement, drag, and steering math in `ShipMovementJob`
- projectile lifetime and contact sweeps in `ProjectileMoveJob` / PhysicsCore2D
- zero-GC and multi-tick stability in `CombatSimulation`

Every one of these is a value-level assertion on a struct or a `NativeArray`. AltTester's `FindObject`/`GetComponentProperty` cannot see a `ShipEntity`; Poco cannot see it; Automated QA cannot see it. Making them see it would require proxy GameObjects or custom commands, which reintroduces the coupling the test is supposed to avoid. The native test reads the state directly, with no adapter layer.

### 5.4 Why the licensing and compatibility math is decisive

- **AltTester Lite is not a CI option.** It is GUI-only, one connection, and GPL-3.0. CI batchmode is a Pro feature. Pro is €75 + VAT/seat/month (€900+/seat/year at list price). For a preservation/fan project, that is a recurring cost for a capability the native stack already provides.
- **GPL-3.0 is a poor fit.** The project's own tooling is MIT, and the game IP belongs to Vargames. Shipping a GPL-3.0 SDK inside the game build creates obligations the project cannot satisfy. The non-GPL SDK is Pro-only.
- **Unity 6.6 is ahead of AltTester's tested matrix.** 2.4.0 lists 6.5. The project would be an early adopter on a third-party instrumentation layer, with the Input System known-issue risk above.
- **The alternatives are staler.** Automated QA has been on hold since 2021; Poco's official Unity SDK targets Unity 4/5.

### 5.5 What native does *not* cover (and when to revisit)

Be honest about the gap: `InputTestFixture` + Editor PlayMode is an **in-process, white-box** test. It does not prove that a packaged build launches, that a real gamepad works on a device, that a menu is clickable at a given resolution, or that a shader renders correctly. Those are legitimate E2E concerns.

Revisit AltTester (or a custom Player harness) only when at least one of these becomes a real requirement:

1. Store/device certification requires black-box tests on a built player.
2. A QA/community team needs Python/Robot tests or page-object authoring.
3. Multi-device/cloud coverage becomes part of the release process.
4. A concrete Unity 6.6 + Input System 1.20.1 AltTester spike passes, including the "Player Input not working" known issue.

If that happens, add it as a **nightly/release lane** and record a new ADR. Do not replace the Tier 2 state gate; the native tests are faster, deterministic, and better at pinpointing simulation regressions.

---

## 6. Working E2E-Style Test Snippet

This is a complete PlayMode test against the existing domain. It injects a virtual gamepad trigger through the real Input System action path, runs the actual `CombatSimulation` for one simulated second at a fixed 60 Hz, and asserts displacement.

**Prerequisites (not yet in the repo):**

1. Add `com.unity.inputsystem` to `"testables"` in `A&B-U/Packages/manifest.json`:

   ```json
   "testables": [
     "com.unity.inputsystem"
   ]
   ```

   Add it as a sibling of `"dependencies"`, not inside it.

2. In `A&B-U/Assets/Scripts/Tests/PlayMode/ABRevival.Tests.PlayMode.asmdef`, add `Unity.InputSystem` and `Unity.InputSystem.TestFramework` to `references` (keep `ABRevival.Core`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`).

3. Place the test in `A&B-U/Assets/Scripts/Tests/PlayMode/ThrustE2ETests.cs`.

```csharp
using System.Collections;
using ABRevival.Core.Simulation;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// Tier 2 E2E-style test: virtual gamepad -> InputAction -> CombatSimulation -> state assertion.
    /// The simulation is stepped at a fixed 60 Hz; no wall-clock waits and no scene dependency yet.
    /// </summary>
    public class ThrustE2ETests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator GamepadThrust_DisplacesShip_ForwardAlongHeading()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var throttle = new InputAction("Throttle", binding: "<Gamepad>/rightTrigger");
            throttle.Enable();

            using var sim = new CombatSimulation();
            sim.Init(maxShips: 1, maxProjectiles: 1);
            sim.ShipCount = 1;
            sim.SetShip(0, new ShipEntity
            {
                position = float2.zero,
                velocity = float2.zero,
                rotation = 0f,          // heading = +Y
                mass = 100f,
                thrustPower = 500f,
                drag = 0.05f
            });

            try
            {
                // InputTestFixture: queue a state event. A [UnityTest] processes it in the player
                // loop, so yield one frame before reading the action (queueEventOnly is ignored).
                Set(gamepad.rightTrigger, 1f);
                yield return null;

                Assert.AreEqual(1f, throttle.ReadValue<float>(), 1e-4f,
                    "The virtual gamepad trigger should drive the throttle action.");

                const float dt = 1f / 60f;
                for (int i = 0; i < 60; i++)
                {
                    sim.SetInput(0, new ShipInput
                    {
                        throttle = throttle.ReadValue<float>(),
                        steering = 0f
                    });
                    sim.Tick(dt);
                }

                var ship = sim.GetShip(0);
                Assert.Greater(ship.position.y, 1f,
                    "One second of full thrust should produce forward displacement.");
                Assert.Greater(ship.velocity.y, 0f,
                    "Forward velocity should be positive after thrust.");
                Assert.AreEqual(0f, ship.position.x, 1e-4f,
                    "No steering input should produce no lateral drift.");
            }
            finally
            {
                throttle.Disable();
                throttle.Dispose();
            }
        }
    }
}
```

**Why this is E2E-shaped, not just another unit test:**

- The input enters through a real `Gamepad` device and a real `InputAction` binding, not by calling `ShipMovementJob.Integrate` directly.
- The test runs the real `CombatSimulation.Tick` loop — the entry point the future presentation host will call.
- The assertion reads the same `ShipEntity` state the renderer/AI would read.
- Time is controlled by the test (`dt = 1/60`, exactly 60 ticks), so the result is deterministic.

**When the presentation layer lands**, replace the direct `new CombatSimulation()` with:

```csharp
// SceneManager.LoadScene("Battle");
// var host = Object.FindAnyObjectByType<BattleSimulationHost>();
// yield return null; // let the host initialize
// ... inject input, advance host.FixedUpdate / host.Step(dt), assert host.Simulation.GetShip(0)
```

Keep the same input-injection and assertion shape. The `BattleSimulationHost` should expose the simulation state (or a read-only snapshot) rather than duplicating logic in the test.

---

## 7. Implementation Notes for the Follow-Up Ticket

1. **Wire PlayMode into the Tier 2 runner.** `scripts/run-unity-tests.bat` currently runs only `-testPlatform EditMode`. Add a `--playmode` switch (or run both) with `-testResults reports/playmode-results.xml`.
2. **Keep `-nographics` for state tests.** It is the difference between a GPU-less Linux runner and a self-hosted GPU runner. Do not add visual assertions to this lane.
3. **Separate screenshot/visual tests.** If needed later, run them in a dedicated job without `-nographics`, and never make them part of the pre-merge gate.
4. **Pin the Input System test setup.** `testables` is easy to lose in a manifest merge; add a test that fails if the fixture assembly is missing, or document it in the PlayMode asmdef.
5. **CI license.** Use a licensed self-hosted runner or GameCI's activation flow; a missing Unity license looks like a test failure and is the most common headless-CI trap.
6. **Do not add a third-party driver until the revisit triggers in §5.5 are met.** If AltTester is evaluated later, budget a spike against Unity 6.6 and Input System 1.20.1, and test gamepad `PlayerInput` specifically because of the vendor's known issue.

---

## 8. Risks & Open Questions

| Risk | Impact | Mitigation |
|---|---|---|
| No presentation host/scene yet | The snippet tests the simulation, not a real scene boot | Add `BattleSimulationHost` when presentation lands; keep the same test shape |
| `InputTestFixture` setup lost in manifest/asmdef changes | Compile errors or "type not found" in CI | Document `testables` + asmdef refs; add a smoke test or CI check |
| Unity 6.6 + Input System 1.20.1 PlayMode quirks | Unexpected input timing in `[UnityTest]` | Always `yield return null` after `Set`/`Press`; read actions, not raw controls, where possible |
| Editor batchmode cost | Tier 2 takes tens of seconds | Keep Tier 1 as the fast gate; run PlayMode only on CI/merge |
| Visual regressions uncovered | Native lane cannot see rendering | Defer to a separate graphics-enabled job; do not weaken `-nographics` |
| AltTester becomes necessary later | New license/infrastructure | Treat as a new ADR + nightly lane, not a Tier 2 replacement |
| Prices/versions change | Recommendation drift | Re-check AltTester pricing and Unity support before any purchase |

**Open question for the parent map:** should Tier 2 eventually include a `StandaloneLinux64` built-player smoke test (one test, one build) to prove the packaged player boots? That is still UTF and still native, but it adds a build step. It is worth a separate ticket once a battle scene and presentation host exist.

---

## 9. Sources

All accessed 2026-11-08.

**AltTester**
- Overview / architecture: <https://alttester.com/docs/sdk/latest/pages/overview.html>
- Commands / object API: <https://alttester.com/docs/sdk/latest/pages/commands.html>
- Known issues (New Input System): <https://alttester.com/docs/sdk/latest/pages/known-issues.html>
- Pricing & tiers: <https://alttester.com/pricing/>
- License metadata (GPL-3.0): <https://github.com/alttester/AltTester-Unity-SDK>
- Release 2.3.0 (Unity 6 support): <https://github.com/alttester/AltTester-Unity-SDK/releases/tag/2.3.0>
- Release 2.4.0 (Unity 6.5 support): <https://github.com/alttester/AltTester-Unity-SDK/releases/tag/2.4.0>

**Unity Automated QA**
- Manual index (development on hold): <https://docs.unity3d.com/Packages/com.unity.automated-testing@0.8/manual/index.html>
- Changelog 0.8.1 / hold notice: <https://docs.unity3d.com/Packages/com.unity.automated-testing@0.8/changelog/CHANGELOG.html>
- Recorded Playback limitations: <https://docs.unity3d.com/Packages/com.unity.automated-testing@0.6/manual/RecordedPlayback.html>

**Poco / Airtest**
- Poco repository (Apache-2.0): <https://github.com/AirtestProject/Poco>
- Poco SDK repository: <https://github.com/AirtestProject/Poco-SDK>
- Poco SDK integration guide (Unity 4+): <https://airtest.doc.io.netease.com/en/IDEdocs/poco_framework/6_poco_sdk/>
- Poco integration (Unity 4 & 5): <https://poco.readthedocs.io/en/latest/source/doc/integration.html>

**Unity Input System**
- Set up test assemblies (`testables` + asmdef refs): <https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/set-up-test-assemblies.md>
- Set up test fixtures: <https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/set-up-test-fixtures.md>
- `InputTestFixture` API: <https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/api/UnityEngine.InputSystem.InputTestFixture.html>
- Writing tests: <https://github.com/Unity-Technologies/InputSystem/blob/develop/Packages/com.unity.inputsystem/Documentation~/write-tests.md>
- Package manifest in this repo: `A&B-U/Packages/manifest.json` (Input System 1.20.1)

**Unity Test Framework / headless CI**
- UTF command-line reference (PlayMode vs Player): <https://docs.unity.com/en-us/engine/6000.6/manual/programming-environment/test-framework-introduction/reference-command-line>
- Run tests from the command line: <https://docs.unity.com/en-us/engine/6000.6/manual/programming-environment/test-framework-introduction/running-tests/run-tests-from-command-line>
- Run PlayMode tests in a Player: <https://docs.unity.com/en-us/engine/6000.6/manual/programming-environment/test-framework-introduction/running-tests/workflow-run-playmode-test-standalone>
- Editor command-line reference (`-batchmode`, `-nographics`, `-logFile`): <https://docs.unity.com/en-us/engine/6000.6/manual/unity-editor/command-line-arguments/editor>
- GameCI Unity Test Runner (playmode on Ubuntu, license secrets): <https://game.ci/docs/github/test-runner/>

**Project sources**
- `docs/adr/0016-two-tier-automated-testing-gates-unity-utf.md`
- `A&B-U/ProjectSettings/ProjectVersion.txt`
- `A&B-U/Packages/manifest.json`
- `A&B-U/Assets/Scripts/Simulation/CombatSimulation.cs`
- `A&B-U/Assets/Scripts/Simulation/ShipEntity.cs`
- `A&B-U/Assets/Scripts/Simulation/ShipMovementJob.cs`
- `A&B-U/Assets/Scripts/Tests/PlayMode/ABRevival.Tests.PlayMode.asmdef`
- `scripts/run-unity-tests.bat`
