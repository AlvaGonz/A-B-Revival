# Coding Standards & Architecture Guardrails

This document establishes the coding conventions, domain encapsulation rules, and architectural standards for *Assembly & Battle Revival*.

---

## 1. Domain Model Encapsulation

1. **No Domain String Matching in UI Classes**:
   - UI views and controllers (classes in `src/as3/scripts/ui/pages/` and `src/as3/scripts/ui/unit/`) must never parse or inspect domain object identity strings directly (e.g. `name == "Captain's Ship"` or `name.indexOf("Captain")`).
   - Domain invariants must be encapsulated on the model entity itself:
     - Use `F4DataLinks.isCaptainShip()` to test if a craft is an allied flagship.
     - Use `F4DataLinks.isSpecialProtected` to determine scrap/delete eligibility.
   - Enforced by: `scripts/lint_domain_leakage.py` in the pre-commit hook.

2. **Primitive Obsession & Part Types**:
   - Never use raw magic numbers (`0`, `1`, `2`, `3`) to identify component categories in business or builder logic.
   - Always use the canonical constants defined on `F4DataOrg`:
     - `F4DataOrg.TYPE_BODY` (`0`): Ship hull / chassis.
     - `F4DataOrg.TYPE_WING` (`1`): Wing aerofoil and wing mounted points.
     - `F4DataOrg.TYPE_ENGINE` (`2`): Thruster / propulsion engine.
     - `F4DataOrg.TYPE_GUN` (`3`): Weapons, cannons, and projectile emitters.

3. **Demeter & Message Chains**:
   - Avoid deep traversal walks through DisplayObjects to read model data (e.g. `part.f4DataLink.f4dataorg.type == targetType && part.f4DataLink.f4dataorg.useAs != 2`).
   - Encapsulate composite queries on the immediate wrapper (e.g. `part.isRemovableType(targetType)`).

4. **IP & Attribution Integrity**:
   - Never generate code comments, commit logs, or documentation claiming ownership of original game code, assets, or mechanics. Vargames is always acknowledged as the sole creator and copyright owner of *Assembly & Battle*.

## 2. Reglas de Rendimiento Unity 6.6 (6000.6.8f1 - CPU Multicore & GPU TBDR)

- **Zero-Alloc:** Prohibido usar `LINQ`, `foreach` sobre listas dinámicas o instanciar clases (`new`) en `Update`, `FixedUpdate` o `LateUpdate`.
- **Concurrencia:** Simula flotas, proyectiles e IA táctica (ADR 0006) con `NativeArray<T>`, `struct` y `IJobParallelFor` decorados con `[BurstCompile]`.
- **Física 2D:** Reemplaza colisiones heredadas por la API multihilo `Unity.U2D.Physics` (`PhysicsCore2D`) basada en structs.
- **Batching & Shaders:** Agrupa piezas modulares en Sprite Atlases (ASTC en móvil, BC7 en PC), ordena con Z-Buffer y usa `unity_RendererUserValue` para daño o facción por pieza sin romper el batching; prioriza Vulkan en Android/PC y Metal en iOS.

---

## 3. Pre-Commit Verification Pipeline

- All commits modifying ActionScript 3 sources (`src/as3/**/*.as`) must pass pre-commit verification.
- Active hooks live in `.githooks/` and are configured via:
  ```bash
  scripts/install-hooks.bat   # Windows
  sh scripts/install-hooks.sh # Unix / macOS
  ```
- Checks executed on commit:
  1. `scripts/lint_domain_leakage.py --staged`: Static check against domain leakage.
  2. `scripts/recompile.bat`: FFDec compilation check ensuring `game/a&b_space_mod.swf` compiles with exit code `0`.

---

## 3. Fowler Code Smell Baseline

All code reviews evaluate against Fowler's 12 baseline code smells:
- *Mysterious Name*: Descriptive function and variable names revealing intent.
- *Duplicated Code*: Common shapes extracted to shared helpers.
- *Feature Envy*: Methods reside on the data they manipulate.
- *Data Clumps*: Bunched parameters bundled into a dedicated type.
- *Primitive Obsession*: Domain concepts given types/constants.
- *Repeated Switches*: Cascades replaced with polymorphism or maps.
- *Shotgun Surgery*: Changes grouped by module cohesion.
- *Divergent Change*: One module changes for one reason.
- *Speculative Generality*: No unused scaffolding or YAGNI abstractions.
- *Message Chains*: Navigation hidden behind delegating methods.
- *Middle Man*: Pointless 1-line forwarding delegates removed.
- *Refused Bequest*: Favor composition over empty subclass inheritance.

