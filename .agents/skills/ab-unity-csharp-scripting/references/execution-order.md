# MonoBehaviour execution order (Unity 6.6)

Depth for `ab-unity-csharp-scripting`: the event order that matters for presentation
scripts. Verified against the Unity 6.6 Manual "Event function execution order" page and the
`MonoBehaviour` scripting reference.

## Execution order (the parts that matter for presentation)

Per object, the engine calls these in this order:

| Phase | Callback | Runs | Use for |
|-------|----------|------|---------|
| Load | `Awake` | once, when the object loads — even if the script component is disabled, but not while the GameObject is inactive | cache `GetComponent`, set up self |
| Enable | `OnEnable` | each time the object/script enables | subscribe to events |
| Init | `Start` | once, before first `Update`, after every `Awake` | wiring that depends on other objects |
| Physics | `FixedUpdate` | every fixed step (default 0.02s) | engine-level fixed work; keep gameplay simulation out of it in this project |
| Physics | `OnTriggerXXX` / `OnCollisionXXX` | during the physics step | collision/trigger response |
| Frame | `Update` | once per rendered frame | presentation state, non-physics logic |
| Frame | `LateUpdate` | once per frame, after all `Update`s | camera/view follow, final pose apply |
| Disable | `OnDisable` | each time the object/script disables | unsubscribe from events |
| Teardown | `OnDestroy` | once, when destroyed | release native handles, save |

## Key consequences

- `FixedUpdate` may run zero, one, or several times per frame depending on frame rate. Never
  read raw "per frame" input there — read it in `Update`, consume it in the fixed-step layer.
- All `Awake` calls finish before any `Start`. Order *among* `Awake`s (and among `Start`s) is
  undefined unless you set a Script Execution Order in Project Settings.
- `OnEnable` runs after `Awake` on first enable, then again on every re-enable. Pair every
  `OnEnable` subscription with an `OnDisable` unsubscription to avoid duplicate handlers.
- `Update` runs once per rendered frame, not on the physics clock. Do not use it as the
  simulation step.
- `LateUpdate` runs after every `Update`, so it is the right place to apply a camera or visual
  follow that must not be overwritten by other per-frame code.
- `OnDisable` and `OnDestroy` also run on application quit; keep cleanup idempotent.
- Event callbacks are not a licence to allocate. The zero-allocation rules from
  `CODING_STANDARDS.md` section 2 apply inside every callback.

## Script execution order

Project Settings -> Script Execution Order sets a per-script offset around the default time.
Use it only when a real dependency requires it; it is a global ordering contract that is easy
to forget. Prefer explicit initialization from `Start` or an event channel.

## Related project rules

- `CODING_STANDARDS.md` section 2: no LINQ, no `foreach` over dynamic lists, no `new` class
  instances in `Update`, `FixedUpdate` or `LateUpdate`.
- ADR 0016: Tier 1 `dotnet test` and Tier 2 headless Unity Test Framework gate.
- ADR 0014: `PhysicsCore2D` and the fixed-step simulation layer.
