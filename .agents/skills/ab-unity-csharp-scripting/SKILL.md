---
name: ab-unity-csharp-scripting
description: >
  Write Unity 6.6 (6000.6.8f1) presentation C# scripts: the MonoBehaviour lifecycle
  (Awake/OnEnable/Start/Update/FixedUpdate/LateUpdate), component access, execution order
  and Inspector serialization. Use when creating or editing presentation .cs scripts, or
  when the user mentions MonoBehaviour, Start/Update, GetComponent, SerializeField or
  "Unity script".
---

> **Project constraint (A&B Revival).** Original Flash art, fonts and audio are the baseline
> (ADR 0020); improvements need the owner's explicit approval recorded in the issue/PR. This
> skill covers presentation-layer MonoBehaviours only. Gameplay simulation runs in the
> fixed-step simulation layer (ADR 0014/0016); do not move simulation into `Update`, and do
> not read this skill as ECS guidance. The repository workflow (wayfinder -> to-spec ->
> to-tickets -> TDD -> review) prevails; imported steps are advisory. APIs below were
> verified against Unity 6000.6.8f1.

# Unity C# scripting (MonoBehaviour)

Write correct presentation scripts in Unity 6.6. Get the lifecycle, component access,
serialization and execution order right so behaviour is deterministic and the Inspector
stays useful. Targets **Unity 6.6 (6000.6.8f1)**, C# / .NET Standard 2.1.

## When to use

- Use when authoring or fixing a presentation `MonoBehaviour`: choosing the right lifecycle
  callback, caching component references, exposing fields to the Inspector, or wiring view
  updates.
- Use when the project has `*.cs` files, an `Assembly-CSharp` or `*.asmdef`, and a
  `ProjectSettings/` folder.

**When *not* to use:** simulation, jobs, `NativeArray` or Burst work -> `CODING_STANDARDS.md`
section 2 and the simulation project; physics tuning -> `ab-physics-tuning`; player input ->
`ab-unity-input-system`; shared data/config -> `ab-unity-scriptableobjects`.

## Zero-allocation rules first

`CODING_STANDARDS.md` section 2 governs every script this skill touches:

- No LINQ, no `foreach` over dynamic lists, and no `new` class instances in `Update`,
  `FixedUpdate` or `LateUpdate`.
- Cache component and child references in `Awake` or `Start`; never call `GetComponent` per
  frame.
- Simulation uses `NativeArray<T>`, structs, `IJobParallelFor` and `[BurstCompile]`. This
  skill does not replace that layer and does not imply ECS for presentation scripts.
- Do not drive gameplay or per-frame loops with coroutines; update views from the existing
  simulation tick or event path instead.
- Do not allocate per frame: no per-frame lists, string concatenation or boxing.

## Core workflow

1. **Pick the callback by purpose, not habit.** `Awake` (cache references, runs once on
   load), `OnEnable` (subscribe to events), `Start` (init that depends on other objects'
   `Awake`), `Update` (per-frame presentation state), `LateUpdate` (camera/view follow after
   movement), `OnDisable`/`OnDestroy` (unsubscribe/cleanup). The simulation layer owns fixed
   steps; do not treat `FixedUpdate` as the game simulation.
2. **Cache component lookups in `Awake`** — never call `GetComponent` every frame.
3. **Expose tunables with `[SerializeField] private`**, not public fields, so other code
   cannot mutate them but designers can edit them in the Inspector.
4. **Scale per-frame values by `Time.deltaTime`** in `Update`. In `LateUpdate`, apply the
   latest pose/state after all `Update` calls.
5. **Keep the update path allocation-free** — read cached fields, write structs, and avoid
   per-frame event subscriptions or string building.
6. **Verify in Play mode**: check the Console for null-reference exceptions, confirm values
   in the Inspector update as expected, and watch the Profiler if `Update` is hot.
7. **Move `.meta` files with their assets.** When you create, move, rename or delete scripts
   or assets outside the Editor, always move the file together with its `.meta` (a new
   `.meta` means a new GUID and broken references). Never delete or rewrite a `.meta` by hand.

## Patterns

### 1. Lifecycle + cached references (the canonical skeleton)

```csharp
using UnityEngine;

public sealed class CraftPresenter : MonoBehaviour
{
    [SerializeField] private SpriteRenderer hull;
    private Transform _cachedTransform;

    private void Awake()
    {
        _cachedTransform = transform;
        if (hull == null)
            hull = GetComponentInChildren<SpriteRenderer>();
    }

    // Called by the presentation/view layer after the simulation tick.
    public void ApplyPose(in Vector3 position, in Quaternion rotation)
    {
        _cachedTransform.SetPositionAndRotation(position, rotation);
    }
}
```

### 2. Safe component access with `TryGetComponent`

```csharp
// Avoids a null-result lookup and is clearer than GetComponent + null check.
if (other.TryGetComponent(out CraftPresenter presenter))
    presenter.ApplyPose(transform.position, transform.rotation);
```

### 3. Serialization that shows up correctly in the Inspector

```csharp
[SerializeField, Range(0f, 1f)] private float volume = 0.8f;  // slider
[SerializeField] private string playerName = "Hero";          // private but serialized

[System.Serializable]                 // REQUIRED for a plain class to serialize/show
public sealed class Stats { public int hp = 100; public int mana = 50; }

[SerializeField] private Stats stats = new();  // nested data in the Inspector
```

### 4. Cache scene lookups once, not per frame

```csharp
private CombatHudBehaviour _hud;

private void Awake()
{
    // FindFirstObjectByType is obsolete in Unity 6.6; FindAnyObjectByType does not
    // depend on instance-ID ordering.
    _hud = FindAnyObjectByType<CombatHudBehaviour>();
}
```

## Pitfalls

- **`GetComponent` in `Update`** — it searches every frame and tanks performance. Cache the
  reference in `Awake`/`Start`.
- **Physics in `Update`** — moving a physics body with forces or `MovePosition` outside the
  fixed step causes jitter and timestep-dependent behaviour. This project simulates physics
  in the `PhysicsCore2D` layer; presentation scripts only read its results.
- **Relying on `Start` order across objects** — `Start` runs after *all* `Awake`s, but order
  among `Start`s is undefined. Do cross-object wiring in `Start`, self-setup in `Awake`.
- **`public` fields just to show them in the Inspector** — that also lets any script mutate
  them. Use `[SerializeField] private` instead.
- **`gameObject.tag == "Enemy"`** — prefer `gameObject.CompareTag("Enemy")`; it compares the
  tag natively and is faster than reading the tag string.
- **`FindFirstObjectByType` is obsolete in Unity 6.6** — it relied on instance-ID ordering.
  Use `FindAnyObjectByType` or `FindObjectsByType(FindObjectsSortMode.None)`; both are slow,
  so cache the result.
- **`OnEnable` without `OnDisable`** — every subscription must be paired with an
  unsubscription or duplicate handlers accumulate on re-enable.
- **`Update` never runs before `Start`, but the first `Update` can run on the same frame as
  `Start`** — guard against not-yet-initialised fields if you split setup oddly.
- **A missing `.meta`** breaks references for every asset that pointed at the old GUID. Move
  the pair together and let Unity re-import before committing.

## References

- For the full event-execution-order table and the consequences for presentation scripts,
  read `references/execution-order.md`.
- Primary docs: Unity 6.6 Manual "Event function execution order"
  (`https://docs.unity3d.com/6000.6/Documentation/Manual/execution-order.html`) and
  `ScriptReference/MonoBehaviour`.
- Project rules: `CODING_STANDARDS.md` section 2 (zero-alloc, `NativeArray`, Burst) and
  ADR 0016 (Tier 1 `dotnet test`, Tier 2 headless UTF).

## Related skills

- `ab-physics-tuning` — `PhysicsCore2D`, fixed stepping and motion.
- `ab-unity-input-system` — reading player input into presentation scripts.
- `ab-unity-scriptableobjects` — sharing data/config between scripts without singletons.
