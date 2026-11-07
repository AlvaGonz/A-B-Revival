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
6. **Assembly UI Counter & Limit Line (`UIMakePlayer.wingLimitSprite`, `txtWingLimit`):**
   - Preserves original assembly layout: `txtWingHint` remains a single-line hotkey reminder (`[R] Rotate Wing | [F] Flip Wing`) without layout disruption.
   - On the left 12-slot parts tray (`$chooseu0..11`), a dynamic horizontal capacity line is drawn at the boundary of the hull's allowable wings (e.g. at slot 4 for Small, slot 6 for Medium, slot 8 for Large, slot 12 for Mega).
   - Centered directly on top of the limit line, a compact `x/limit` text indicator is rendered in a dark translucent badge with glow filter.
   - Dynamic coloring: cyan (`0x00ccff`) when within limit, warning gold (`0xffcc00`) when cap is reached, and alert red (`0xff4444`) on over-cap legacy crafts.
   - Refreshed in `uiloop` per frame, maintaining fast performance and zero layout shift.

## Consequences
- Balances craft creation: light interceptors remain agile while mega ships have capacity for heavier configurations.
- Preserves save compatibility without breaking player hangar crafts.
- Zero audio or UI asset overhead.

