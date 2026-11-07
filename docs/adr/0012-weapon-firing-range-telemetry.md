# 12. Weapon Firing Range Telemetry Across Inventory & Inspection Views

Date: 2026-11-07

## Status
Accepted

## Context
In the base game, weapons display properties such as cost, mass, hit points, and firepower index (`fire`), but players have no in-game visibility into a weapon's effective firing range before purchasing, equipping, or modifying it. Firing range is derived from projectile physics and emitter definitions (`F4DataBullet` and `F4DataFire` linked to `F4DataOrg`). Multiple UI views present weapons to the player:
- Shop purchase list (`FormShop` / `UIShop`)
- Assembly part selection list (`FormPlayerPart` / `UIMakePlayer`)
- Hangar repair & loadout list (`FormRepair` / `UIRepair`)
- Part inspection & hover card (`ShowPart`)

We need a canonical, domain-encapsulated range calculation that conforms to `CODING_STANDARDS.md` and displays cleanly across all four weapon views without UI layout corruption.

## Decision

1. **Canonical Domain Range Formula (`F4DataOrg.getEffectiveRange`)**:
   Encapsulate the effective weapon range calculation on `F4DataOrg`:
   \[
   d = v_0 \cdot t + \frac{1}{2} a \cdot t^2
   \]
   where:
   - \(v_0 = \text{fire.spdS} + \text{fire.spdAdd} \cdot \text{fire.fireNum} \cdot 0.5\) (average muzzle velocity across burst)
   - \(a = \text{fire.accS} + \text{fire.accAdd} \cdot \text{fire.fireNum} \cdot 0.5\) (average projectile acceleration)
   - \(t = \text{bullet.life}\) (projectile lifetime in simulation frames)
   - If `type != TYPE_GUN`, `firePoints.length == 0`, or either `fire` or `bullet` is null, returns `0`.
   - Returns \(\max(0, d)\).

2. **Domain Boundary Encapsulation**:
   UI classes (`FormShop`, `FormPlayerPart`, `FormRepair`, `ShowPart`) must never parse XML, touch `F4DataBullet`/`F4DataFire` physics variables directly, or calculate kinematic formulas. They query `F4DataOrg.getEffectiveRange()` exclusively.

3. **Part Inspection Card Seam (`ShowPart.addPart`)**:
   In `ShowPart.addPart`, when inspecting a Gun (`param1.type == F4DataOrg.TYPE_GUN`):
   - The `pow` textfield (which displays engine thrust `0.0` for guns) displays the effective range: `int(param1.getEffectiveRange()) + ""`.
   - The `turn` textfield (which displays steering `0.0` for guns) displays `"-"`.
   - For non-gun parts, `pow` displays `param1.powFly.toFixed(1)` and `turn` displays turning agility as before.

4. **Inventory Table Seam (`FormShop`, `FormPlayerPart`, `FormRepair`)**:
   In inventory rows, engines use `powfly` for thrust and wings use `turnfly` for steering, while guns leave both at `0.0`.
   - When a row represents a Gun (`param2[2] == "Gun"` or `param1.type == F4DataOrg.TYPE_GUN`), the `powfly` textfield displays the weapon's effective range `int(_range) + ""`.
   - In `f4linkToArray`, the effective range is stored in the row data array, enabling clean column sorting.

5. **Backwards Compatibility & Zero Asset Overhead**:
   - Reuses existing DefineEditText fields and MovieClip symbols (`powfly`, `pow`, `fire`).
   - Requires zero modifications to binary assets or SWF shape definitions.
   - Preserves 100% compatibility with saved craft files and shop inventories.

## Consequences
- Players gain complete tactical visibility over weapon engagement envelopes directly in all hangar and shop interfaces.
- Ballistic physics remain strictly encapsulated within the domain layer.
- Zero performance impact during combat or menu transitions.
