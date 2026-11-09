---
name: ab-soak-test
description: Long-run soak test protocol for the Unity 6.6 target - timed checkpoints for slow memory leaks, frame-time drift, late-appearing edge cases and fun fatigue, using the Unity Memory Profiler when installed or the built-in Profiler Memory module, and the ADR 0008 lifecycle invariants. Use after a leak fix, when extended play degrades, or before a release soak.
---

> **Project constraint (A&B Revival).** Original Flash art, fonts and audio are the baseline (ADR 0020); improvements need explicit owner approval recorded in the issue/PR. The simulation runs at a 60 Hz fixed step (`GameFlowController.FixedStepSeconds`, 16.666 ms frame budget). The repository workflow (wayfinder -> to-spec -> to-tickets -> TDD -> review) prevails over any step in this skill.

# Soak Test

A soak test (also called an endurance test) is an extended play session run with specific
observation goals. A smoke check covers the broad critical path in about 10 minutes and a
single-feature playtest runs about 30 minutes; a soak runs for **30 minutes to several
hours** to surface:

- **Memory leaks** - gradual growth that only appears after mission and scene cycles.
- **Performance drift** - frame time that worsens over time.
- **State accumulation bugs** - issues that appear only after N repetitions of a mechanic
  (inventory full, score overflow, AI state corruption, pooled objects never returned).
- **Fun fatigue** - mechanics that feel good in a first session but grow repetitive.
- **Content exhaustion** - the point where the player runs out of novel content.

This skill defines the protocol and the analysis. A human runs the session; the observations
require a human observer.

**When to run:**

- Before a release or milestone soak.
- After fixing a memory or stability issue (regression soak).
- When extended play has not been formally tracked.
- When a long-run regression is reported but shorter playtests are clean.

---

## 1. Duration and focus

**Duration** (default `1h`):

- `30m` - short soak; one mechanic or one screen.
- `1h` - standard soak; covers the common leak categories.
- `2h` - extended soak; recommended for the first full soak.
- `4h` - deep soak; only for a game with long-session design.

**Focus** (default `all`):

- `memory` - reserved memory, managed heap, object count, native and graphics memory.
- `stability` - crash, freeze and hang detection.
- `balance` - fun fatigue, content exhaustion, difficulty perception.
- `all` - all of the above.

The first soak uses `all`. A memory-only soak is a regression check after a specific fix.

---

## 2. Load project context

Read only what the run needs:

- `docs/adr/0008-60fps-resolution-modernization-and-leak-prevention.md` - the lifecycle
  invariants the soak is checking: entity teardown, sound retirement, particle capping,
  list draining and rendering-resource disposal.
- `docs/adr/0014-unity-6000-dots-physics-core-2d-migration.md` - Unity 6.6 target,
  zero-allocation policy and `NativeArray` ownership.
- `A&B-U/ProjectSettings/TimeManager.asset` - confirm `Fixed Timestep: 0.0166667`
  (60 Hz) and `Maximum Allowed Timestep: 0.33333334`.
- `A&B-U/Assets/Scripts/Presentation/GameFlowController.cs` - `FixedStepSeconds`,
  `MaxSubstepsPerFrame` and the `GameFlowState` loop (MainMenu -> Center -> Briefing ->
  Mission -> Result -> Return -> Center).
- `A&B-U/Assets/Scenes/Boot.unity` - the entry scene for every run. `SampleScene.unity`
  is the empty template and is not a soak target.
- `A&B-U/Assets/Scripts/Tests/` and `A&B-U/Assets/Scripts/Tests/PlayMode/` - what the
  automated gates already cover, so the soak does not re-document it.
- Open issues and the most recent soak, playtest or PR notes in the tracker - known
  issues to watch for.

Budget targets for this project:

- **Frame rate:** 60 FPS, 16.666 ms frame budget (ADR 0013, ADR 0014).
- **Memory ceiling:** none published. Record what the build actually uses. The ADR 0008
  Flash baseline stabilized at 40-55 MB; Unity numbers are not comparable to it.
- **Fixed step:** 1/60 s, at most 4 substeps per frame (`GameFlowController`).

Use a **development build** for a release soak. Editor numbers include editor overhead;
if only the editor is available, record that in the protocol and treat absolute values as
relative to this run only.

---

## 3. Define observation checkpoints

Based on duration, generate timed checkpoints:

- **30m soak:** T+0, T+10, T+20, T+30.
- **1h soak:** T+0, T+15, T+30, T+45, T+60.
- **2h soak:** T+0, T+20, T+40, T+60, T+80, T+100, T+120.
- **4h soak:** T+0, T+30, T+60, T+90, T+120, T+180, T+240.

At each checkpoint the observer records the items in section 4. Between checkpoints, repeat
the cycle under test: start a mission, play, reach Result, return to Center, reload Boot or
restart the run. Growth across cycles is the signal; a single load spike is not.

---

## 4. Observation items

### 4.1 Memory and frame time

**Prerequisite: the memory tool.** `com.unity.memoryprofiler` is **not** in this
project's `Packages/manifest.json`. Do not add it as part of a soak. Pick one
option and record it:

1. The owner approves an **editor-only** install of the Unity Memory Profiler
   (`Window > Analysis > Memory Profiler`). Confirm the menu path in the
   installed Unity version at T+0.
2. Otherwise use the built-in **Profiler** window's **Memory** module
   (`Window > Analysis > Profiler`), which ships with the editor.
3. If neither is available (a player-only run with no profiler), mark the memory
   checkpoint `NOT ASSESSED` and say so in the verdict.

Record the tool and menu path you actually used at T+0. The two tools do not
report the same numbers and must not be mixed within one run.

> **Record the unit the tool displays; never convert it.** A soak looks for growth, so
> every threshold is a ratio or a delta against this run's own T+0 baseline. Write the unit
> down at T+0 and use it for the rest of the run.

At T+0 and at every checkpoint:

- Take a snapshot in the chosen tool: `Capture New Snapshot` in the Memory
  Profiler, or capture a frame and open the Memory module in the Profiler.
- Record: Total Reserved Memory, Total Allocated, GC Allocated / Managed Heap, Object
  Count, and native and graphics totals **as displayed**.
- Compare the chosen tool's own snapshots (or frames) between T+0 and the current
  checkpoint, and note the types whose count or size only rises.

Alert thresholds:

- Total Reserved Memory growth **greater than 20% from T+0** after the first 15 minutes.
- **GC Allocated growing monotonically across 3 or more checkpoints.** Monotonicity is
  the leak signal and is deliberately unit-free.
- **Object Count that does not return to its T+0 floor** after Result -> Center, after
  reloading Boot, or after a mission restart.
- **Native or graphics memory that only rises** across mission cycles.

ADR 0008 invariants to check in the Unity target:

- **Entity teardown:** destroyed ships, projectiles, drops and debris release references to
  parents, cameras, collision state and audio. Static registries and event subscriptions
  do not retain destroyed objects.
- **Audio retirement:** destroyed entities stop and release their sources; the audio
  service does not accumulate voices across missions.
- **Particle cap:** effects stay within the renderer's fixed capacity
  (`MissionSpriteView.MaxParticles`, currently 512) and never allocate a GameObject per hit.
  ADR 0008 set the original ceiling at 400 active particles.
- **Battle-list draining:** every battle element is removed at mission end. Fleet,
  projectile, drop and debris counts return to baseline after Result -> Center.
- **Rendering resources:** `RenderTexture.Release()`, destruction of runtime `Texture2D`,
  `Sprite` and `Material` instances, and `NativeArray.Dispose()` on teardown.
- **Pools:** pooled objects are returned, not abandoned. Pool counts stay flat across
  cycles.

Frame-time checks at each checkpoint:

- Record CPU main-thread, render-thread and GPU frame times from the Profiler.
- Watch the 95th percentile and the spikes, not only average FPS. Degradation usually
  appears as a rising tail before it moves the average.
- Confirm the simulation is still on the 60 Hz fixed step. If `Time.fixedDeltaTime` or
  `Time.timeScale` changed during the run, record when and why, and restore it.

### 4.2 Stability

At each checkpoint, note:

- [ ] No crash, hang or freeze since the last checkpoint.
- [ ] Frame rate still within the 60 FPS budget.
- [ ] Audio still plays correctly, with no desync or silence.
- [ ] Every HUD element still renders correctly.
- [ ] Input still responds, with no input loss or lag spike.
- [ ] Mission Result -> Center still completes without a stale screen.

### 4.3 Balance and fatigue

Collect subjective observations at each checkpoint:

- [ ] Core mechanic still feels rewarding (Y/N).
- [ ] Perceived difficulty: too easy / appropriate / too hard.
- [ ] Any "I have seen this before" moment since the last checkpoint (content exhaustion).
- [ ] Any moment of frustration since the last checkpoint. Note the cause.
- [ ] Any moment of peak engagement since the last checkpoint. Note the cause.

---

## 5. Protocol document

Present the protocol in conversation, then publish the completed record where the owner
asks: an issue comment or attachment in the tracker, or a path the owner names. The issue
tracker is the canonical home for project records.

```markdown
# Soak Test Protocol

> **Date**: [date]
> **Duration**: [duration]
> **Focus**: [memory | stability | balance | all]
> **Build**: [development build | editor] - [version or commit]
> **Scene**: Boot
> **Run by**: [name]

## Pre-Session Setup

- [ ] Fresh launch, not resumed from a previous session.
- [ ] Background applications closed to reduce OS memory interference.
- [ ] Memory tool open and able to capture at T+0: [Memory Profiler | built-in Profiler Memory module | none -> NOT ASSESSED]
- [ ] Target 60 FPS confirmed; `TimeManager.asset` fixed timestep is 0.0166667.
- [ ] Prior known issues to watch: [from tracker]

## Baseline (T+0) - Record Before Playing

| Metric | Baseline Value (unit as displayed) |
|---|---|
| Total Reserved Memory | |
| Total Allocated | |
| GC Allocated / Managed Heap | |
| Object Count | |
| Native / Graphics memory | |
| FPS and frame time (first 30 s) | |
| Memory snapshot / frame label | |

## Checkpoint Log

### T+[N] minutes

| Metric | Value | Delta from T+0 | Alert? |
|---|---|---|---|
| Total Reserved Memory | | | |
| GC Allocated / Managed Heap | | | |
| Object Count | | | |
| Native / Graphics memory | | | |
| CPU / render / GPU frame time | | | |
| Crashes / hangs | | | |
| Snapshot / frame label | | | |

Stability checks:

- [ ] No crash or hang since the last checkpoint.
- [ ] Frame rate within budget.
- [ ] Audio correct.
- [ ] HUD rendering correctly.
- [ ] Input responding correctly.
- [ ] Result -> Center completed.

Balance and fatigue:

- Core mechanic still rewarding: Y / N
- Difficulty: too easy / appropriate / too hard
- Notable moments: [peak engagement or frustration]
- Content exhaustion signs: Y / N - [describe]

Free observations:

[Anything unexpected since the last checkpoint]

## Post-Session Analysis

### Memory Trend

| Checkpoint | Reserved Memory | Managed Heap | Object Count |
|---|---|---|---|
| T+0 | | | |
| [T+N] | | | |

**Leak detected?** Y / N
**Estimated time to OOM at current rate:** [N hours / not applicable]

### Frame Time

Worst frame time: [N ms] at [checkpoint]
Degradation: stable / mild / severe
Fixed step and time scale unchanged: Y / N - [if not, why]

### Stability Summary

Total crashes: [N]
Total hangs: [N]
Worst FPS observed: [N] at [checkpoint]

### Balance and Fatigue Summary

Fun curve: [engaged throughout / fatigue at T+N / repetitive from start]
Content exhaustion point: [never / at T+N / early]
Difficulty arc: [appropriate / too easy throughout / spike at T+N]

### Issues Found

| ID | Severity | Checkpoint | Description |
|---|---|---|---|
| SOAK-001 | S[1-4] | T+[N] | [description] |

## Verdict: PASS / PASS WITH CONCERNS / NOT ASSESSED / FAIL

**PASS**: no leak, stability maintained, fun factor consistent.
**PASS WITH CONCERNS**: minor drift or fatigue, addressable in the next milestone.
**NOT ASSESSED**: the run did not reach the checkpoints it was built around; say how far
it got and which checkpoints were never reached.
**FAIL**: leak confirmed, stability breach, or severe fun fatigue.

A short soak cannot return PASS. Everything this protocol detects - slow leaks, fatigue,
late edge cases - is invisible early, so a run that ended before its checkpoints has not
shown stability; it has shown nothing yet. Record `NOT ASSESSED - reached T+[N] of
[duration]; checkpoints [list] not reached`. NOT ASSESSED ranks above both pass values and
below FAIL: a crash at T+20 is a real finding no matter how short the run, and must not be
demoted because of the run's length. The same applies when no memory instrumentation was
available (nothing was measured) or when the protocol was written but never executed (a
protocol document is not a result).

## Sign-Off

- **Tester**: [name] - [date]
- **Owner / reviewer**: [name] - [date]
```

---

## 6. Collaborative protocol

- **This skill defines the protocol; a human runs it.** Never claim a soak ran without an
  observer and real checkpoint data.
- **Duration matches the game's session design.** The current mission loop is short, so a
  first `1h` soak with several mission cycles is the useful default; `4h` is only justified
  by a long-session mode.
- **First soak uses `all` focus.** Narrow focus is for regression soaks after a specific fix.
- **Ask before publishing.** Confirm where the record goes before writing it.
- **File findings in the tracker.** Link each issue back to the soak record and fix leaks
  through the normal workflow; the soak never patches code or bypasses the gates.
