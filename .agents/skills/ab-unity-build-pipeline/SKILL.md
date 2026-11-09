---
name: ab-unity-build-pipeline
description: >
  Build and ship Unity 6.6 (6000.6.8f1) players: build profiles and scenes, player/quality
  settings, the IL2CPP vs Mono scripting backend, managed code stripping, scripted
  BuildPipeline.BuildPlayer, mobile profiles and CI/headless builds. Use when configuring or
  automating a build, choosing a scripting backend, shrinking build size, or when the user
  mentions Unity build, player settings, IL2CPP, code stripping or headless builds.
---

> **Project constraint (A&B Revival).** Original Flash art, fonts and audio are the baseline
> (ADR 0020); improvements need the owner's explicit approval recorded in the issue/PR. This
> project pins Unity 6000.6.8f1; verify any API before use. Build verification follows
> ADR 0016: Tier 1 `dotnet test` and the headless Unity Test Framework gate. This skill does
> not require an extra content package. The repository workflow (wayfinder -> to-spec ->
> to-tickets -> TDD -> review) prevails; imported steps are advisory. APIs below were
> verified against Unity 6000.6.8f1.

# Unity build pipeline

Configure, script and automate Unity 6.6 player builds: scenes, platform target, scripting
backend, stripping, mobile profiles and headless/CI builds. Targets **Unity 6.6
(6000.6.8f1)**.

## When to use

- Use when setting up Build Profiles, choosing a platform and scripting backend (Mono vs
  IL2CPP), reducing build size with managed stripping, scripting a repeatable build with
  `BuildPipeline.BuildPlayer`, or wiring a CI/headless build.
- Use when the project has `ProjectSettings/EditorBuildSettings.asset` or a CI build script.

**When *not* to use:** authoring a CI service config end-to-end is DevOps; this skill covers
the Unity-side build API and settings. Console/platform certification specifics are
platform-NDA territory. Storefront submission is out of scope.

## Core workflow

1. **List the scenes to build** (File -> Build Profiles -> Scene List, or
   `EditorBuildSettings.scenes`). Only listed, enabled scenes ship; scene 0 is the start
   scene. A Build Profile asset can carry its own Scene List (Add Settings -> Scene List);
   when it does, that profile builds its list, not the global one.
2. **Pick the platform target** and switch the active build target if needed
   (`BuildTarget` / `EditorUserBuildSettings`). In batch mode, pass `-buildTarget <name>` or
   `-activeBuildProfile <path>` on the command line instead: `SwitchActiveBuildTarget` is not
   available there.
3. **Choose the scripting backend** (Player Settings): **Mono** (fast iteration, desktop) vs
   **IL2CPP** (AOT C++; required for many platforms, better runtime performance, harder to
   reverse). IL2CPP needs the platform's C++ toolchain installed.
4. **Set the mobile profile** before building Android or iOS; see the table below.
5. **Tune size/perf:** set Managed Stripping Level (Disabled -> Minimal -> Low -> Medium ->
   High) and protect reflection-only code with a `link.xml`. Set Quality Settings per
   platform.
6. **Script the build** with `BuildPipeline.BuildPlayer(BuildPlayerOptions)` or
   `BuildPlayerWithProfileOptions`, and **inspect the returned `BuildReport`** — a non-
   `Succeeded` result must fail your pipeline.
7. **Run headless** for CI with `-batchmode -quit -nographics -executeMethod`, and check the
   exit code. Use `EditorApplication.Exit(code)` inside the build method for a reliable
   result.
8. **Verify** the actual output runs (launch the player), not just that the build returned
   without throwing. Then run the ADR 0016 gates: Tier 1 `dotnet test` before commit and the
   Tier 2 headless Unity Test Framework suite before merge.

## Mobile profiles

| Target | Backend | Architecture | Output | Profile notes |
|---|---|---|---|---|
| Android | IL2CPP | ARM64 | AAB for Google Play, APK for device testing | The ARM64 target requires IL2CPP; 32-bit ARMv7 can stay for local iteration. Use ASTC texture compression and Vulkan. Install the Android SDK/NDK and the Unity Android module. |
| iOS | IL2CPP | ARM64 only | Xcode project | iOS uses AOT, so Mono/JIT is not an option. Build in Unity, then sign and run from Xcode on macOS. Use Metal and ASTC. |
| Windows | Mono or IL2CPP | x86_64 | `.exe` | Mono for fast iteration; IL2CPP for release builds. Keep toolchain and crash reporting consistent with the shipping build. |

Mobile checklist:

- Build Profiles -> select the platform -> Player Settings -> Other Settings -> Configuration:
  set **Scripting Backend** to IL2CPP and enable **ARM64** (Android).
- Set the package identifier, version and bundle version before the build.
- Set Texture Compression to ASTC for Android/iOS; see `CODING_STANDARDS.md` section 2.
- Confirm the platform module and toolchain are installed on the build machine; a late
  toolchain error is not a code failure.
- Build the release variant (no `BuildOptions.Development`) and install the artifact on a
  real device before calling it done.

## Patterns

### 1. Scripted build with a result check

```csharp
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    [MenuItem("Build/Windows x64")]
    public static void BuildWindows()
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = "Builds/Windows/Game.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,   // BuildOptions.Development for a dev build
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        if (summary.result != BuildResult.Succeeded)
            throw new System.Exception($"Build failed: {summary.totalErrors} errors");
        Debug.Log($"Build OK: {summary.totalSize} bytes in {summary.totalTime}");
    }
}
```

### 2. Headless / CI invocation

```bat
REM Exit code is 0 on success; -quit closes the Editor; -nographics suits build servers.
REM -buildTarget makes the Editor start on that platform, so scripts compile with its defines.
"%UNITY_EXE%" -batchmode -quit -nographics ^
  -projectPath "%PROJECT_DIR%" ^
  -buildTarget win64 ^
  -executeMethod BuildScript.BuildWindows ^
  -logFile -
```

### 3. Build a saved Build Profile

```csharp
using UnityEditor;
using UnityEditor.Build.Profile;
using UnityEditor.Build.Reporting;

public static class ProfileBuild
{
    [MenuItem("Build/Windows Release Profile")]
    public static void Build()
    {
        var profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(
            "Assets/Settings/Build Profiles/WindowsRelease.asset");
        var options = new BuildPlayerWithProfileOptions
        {
            buildProfile = profile,                 // platform, scene list, defines from the asset
            locationPathName = "Builds/Windows/Game.exe",
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new System.Exception($"Build failed: {report.summary.totalErrors} errors");
    }
}
```

### 4. Protect stripped code with `link.xml`

```xml
<!-- Assets/link.xml — keep types the linker cannot see are used (reflection, JSON, plugins). -->
<linker>
  <assembly fullname="MyGameRuntime" preserve="all"/>
</linker>
```

## Pitfalls

- **A scene loads in the Editor but is missing in the build** — it is not in the scene list
  (File -> Build Profiles -> Scene List, or the built profile's own list) or is disabled.
  `SceneManager.LoadScene` only sees listed scenes.
- **IL2CPP build fails on a fresh machine** — the platform C++ toolchain (for example Windows
  build tools or the Android NDK) is not installed. Mono has no such requirement.
- **`MissingMethodException`/`TypeLoadException` only in the build** — managed stripping
  removed reflection-only code. Lower the stripping level or add a `link.xml` preserve entry.
- **Treating "BuildPlayer returned" as success** — always check `BuildReport.summary.result`;
  it can return with errors.
- **The build platform's code did not compile in** — a scripted `BuildPlayer` for a target
  other than the active one keeps the active platform's `UNITY_*` defines, and define changes
  only apply after a domain reload. Switch platform first (in the Editor) or launch with
  `-buildTarget` (batch mode).
- **Build ignores scenes you added to the global list** — the Build Profile you build has its
  own Scene List. Edit that list; a profile without a Scene List uses the global one.
- **Shipping a Development build** — `BuildOptions.Development` enables the profiler and
  debugging and is slower; use `BuildOptions.None` for release.
- **`-quit` after a `Debug.LogError` can still exit 0** — use `EditorApplication.Exit(code)`
  inside the build method so CI fails on a failed build.
- **A Build Profile path is project-relative** — `-activeBuildProfile` takes a path relative
  to the project root, not an absolute path.

## References

- For a complete **multi-platform CI build script** (`-buildTarget` per platform, version
  stamping, argument parsing, exit codes) and building a Build Profile from the command line,
  read `references/ci-build-script.md`.
- Primary docs: `ScriptReference/BuildPipeline.BuildPlayer`,
  `ScriptReference/BuildPlayerWithProfileOptions`, the 6000.6 Manual pages "Create a build
  from the command line", "Create and manage build profiles", "IL2CPP scripting back end",
  "Configure managed code stripping", "Android build settings reference" and "iOS build
  settings reference".
- Project rules: ADR 0016 (Tier 1 `dotnet test`, Tier 2 headless UTF) and
  `CODING_STANDARDS.md` section 2 (ASTC/BC7, Vulkan/Metal).

## Related skills

- `ab-unity-csharp-scripting` — editor scripting conventions used by build scripts.
- `ab-physics-tuning` — the `PhysicsCore2D` settings a build must preserve.
