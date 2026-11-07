# 3. Hybrid Shop Stock Model, Purchase Stacking, and Retroactive Inventory Reconciliation

Date: 2026-11-06

## Status
Accepted

## Context
With the introduction of infinite stock for unlocked parts in the shop (`UIShop` / `FormShop`), items cloned upon purchase rather than moving from `shopPart` to `plPart`. However, two major design and UX problems arose:

1. **Redundant Duplicate Rows**: Original reward tables (`ConfigAndStory.GIFT`) injected up to 4 duplicate copies of each weapon and wing into `shopPart` to allow multiple individual purchases. Under an infinite stock model, this produces 4 identical rows in the shop table. In addition, existing developer/player savegames contain both duplicate rows and missing unlocks from past story missions.
2. **Infinite Ship Hull Exploitation**: Ship hulls/chassis (`Body`, `type == 0`) represent complete craft platforms. Allowing infinite hulls trivializes fleet expansion and decouples hangar fleet management from resource pacing.
3. **Single-Unit Click Fatigue**: Buying multiple units of high-volume modules (e.g., 4x guns or 2x wings) required repeatedly opening the shop, clicking, and confirming individual transactions or having multiple rows on screen.

## Decision
1. **Hybrid Stock Model**:
   - **Infinite Stock Modules (Weapons, Wings, Engines, Others)**: Non-body items (`type != 0`) possess infinite stock. They are cloned into `plPart` on purchase and never removed from `shopPart`.
   - **Finite Stock Bodies (`type == 0`)**: Ship chassis/hulls possess strictly finite stock (1 unit per unlocked model). Purchasing a `Body` removes it from `shopPart` into `plPart`. Selling a `Body` returns it to `shopPart`. Stacking `Body` items beyond available stock (1) is barred.
2. **Purchase Stacking UI (`FormShop`)**:
   - In `FormShop` (table `form1`), clicking a row increments its purchase stack (`sel`: `0 -> 1 -> 2 -> ... -> 99 -> 0`).
   - Holding down the mouse button over an item row activates rapid auto-increment acceleration (initial hold delay ~300ms, ramping to fast repeat intervals), preventing repetitive click fatigue.
   - The `Sel.` column renders the staged quantity: `N` for 0, `x1`, `x2`, etc.
   - Total transaction cost (`Cost=`) computes dynamically as `sum(unit_price * quantity)`.
   - On `ACCEPT`, the checkout loop clones `quantity` copies of each stacked item into `plPart`.
   - Player selling (`form2`) remains strictly unit-based (`Y / N`).
3. **Clean Reward Definitions (`ConfigAndStory.GIFT`)**:
   - `GIFT` definitions are pruned to register exactly one entry per unlocked item (and 1 L + 1 R for wing pairs).
4. **Retroactive Inventory Reconciliation (`reconcileShopStock`)**:
   - On shop initialization (`UIShop`) and save loading (`F4DataCPX.readData`), a reconciliation routine executes:
     - Deduplicates existing `shopPart` rows for non-body items.
     - Iterates historical story rewards (`GIFT[0..STORY]`), inserting any missing unlocks that should have been granted in past missions.
     - Prunes duplicate `Body` entries while ensuring missing unlocked `Body` models (not in `shopPart`, `plPart`, or active fleet) are available.

## Consequences
- The shop UI is clean, compact, and free of redundant rows.
- Players can conveniently stage and buy bulk quantities of weapons and thrusters in a single transaction.
- Ship hulls maintain economic scarcity and progression value.
- Existing savegames are automatically upgraded and repaired without loss of progression or inventory.

