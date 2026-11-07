# 7. Hull Category Wing Capacity & Assembly HUD Counter

Date: 2026-11-06

## Status
Accepted

## Context
Ship assembly previously enforced a single global cap of 10 wings regardless of hull size in `F4DataHole.acceptableOrg`. This allowed small interceptors to mount as many wings as dreadnoughts, compromising ship class visual identity, balance, and progression (Spec #52, Tickets #78 and #79). We wanted to replace this with a hull-category capacity rule, display a dynamic counter in the Factory UI, provide immediate audio feedback upon rejection, and preserve existing player craft saves (grandfathering).

## Decision
1. **Capacity Table:** Small (`4`), Medium (`6`), Large (`8`), Mega (`12`). Fallback for legacy/unknown hulls is `10`.
2. **Category Classification (`F4DataOrg.wingCategoryForHull`):** Resolved by hull name prefix:
   - `B.Small*` -> Small
   - `B.Med*` (including `B.Med++` hulls which do not start with `"B.Medium"`) -> Medium
   - `B.Large*` -> Large
   - `B.Mega*` -> Mega
3. **Domain Encapsulation:**
   - Policy logic lives in the domain layer (`F4DataOrg.maxWingsForHull` and `F4DataOrg.wingCategoryForHull`), not in UI controllers.
   - Hull discovery (`F4DataHole.getAssemblyHull`) and wing counting (`F4DataHole.countMountedWings`) are encapsulated in `F4DataHole` to avoid UI message chains or deep traversal of display lists.
   - Note on SWF Bytecode Import: FFDec `-importScript` silently ignores newly introduced class files not present in the original SWF ABC tags. Therefore, policy and counting logic are placed directly on established domain classes `F4DataOrg` and `F4DataHole`.
4. **Grandfathering & Editing (`F4DataHole.isMountedWing`):**
   - Saved crafts are loaded without validation stripping.
   - In `F4DataHole.acceptableOrg`, already-mounted wings (`isMountedWing(org)`) bypass the capacity check. This ensures players can reposition or edit existing wings on over-cap legacy crafts, while strictly blocking the addition of new wings beyond the category cap.
5. **Rejection Feedback:**
   - Wing addition past the limit returns rejection code `2` in `acceptableOrg`. This seamlessly feeds existing sound triggers in `addPart` (`soundArray[13]`) and drag-drop `linkAct` (`soundArray[18]`). No new sound assets or hooks required.
6. **Assembly Stats Table Counter (`UIMakePlayer.initWingsInDtTemp`, `txtWings`):**
   - Preserves original assembly layout: `txtWingHint` remains a clean single-line hotkey reminder (`[R] Rotate Wing | [F] Flip Wing`) without layout disruption.
   - The wing counter is integrated directly into the bottom-right craft statistics table (`dtTemp`) as an additional row immediately above `Parts` (`Hp`, `Steering`, `Speed`, `Fire Pow.`, `Wings`, `Parts`).
   - The `dtTemp` container is vertically scaled (`scaleY = 0.835`) to compact all 6 rows into the original 80.9px bounding box, preventing any overlap with surrounding controls or the `CLR GUNS` / `CLR WINGS` buttons below.
   - Styled to match existing table rows: background shape with rounded corners, silver label (`Wings`), and right-aligned counter (`x/limit`).
   - Dynamic coloring: standard gray (`0xbfbfbf`) within limit, warning gold (`0xffcc00`) at limit, and alert red (`0xff4444`) on over-cap legacy crafts.
   - Refreshed in `uiloop` and `addDtLTempRun`, maintaining fast performance and zero layout shift.

## Consequences
- Balances craft creation: light interceptors remain agile while mega ships have capacity for heavier configurations.
- Preserves save compatibility without breaking player hangar crafts.
- Zero audio or UI asset overhead.

