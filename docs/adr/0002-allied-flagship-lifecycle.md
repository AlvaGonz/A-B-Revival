# 2. Allied Flagship (Captain's Ship) Lifecycle and Deployment Boundary

Date: 2026-11-06

## Status
Accepted

## Context
From Battle 4 onward, the player unlocks Captain Tom's Ship in the Factory for customization, weapon refitting, and structural upgrades (Spec #21). Because the ship is stored in `ConfigAndStory.ALLPART.plFly` to allow the Factory to load and edit its parts, it also appeared in the combat craft chooser (`UIChooseFlyAI` / `FormChooseFlyAI`).

This permitted players to assign the Captain's Ship to a generic AI wingman slot (`ConfigAndStory.AI`) via `[ADD AI]` or select it as the player's personal craft (`ConfigAndStory.PLAYER`). However, the story battle engine (`LevelBase` / `EvtCtrl`) automatically deploys the canonical Captain's Ship as a scripted story ally in missions. When assigned to AI slots, two identical Captain's Ships were spawned on the battlefield—one with story dialogue and hero AI, and another with generic wingman AI ("Michael" / `AI_P`).

## Decision
1. **Single Flagship Invariant**: The game enforces a strict invariant that exactly one Captain's Ship can exist in the fleet.
2. **Factory Customization Only**: The player may view, equip, disarm, and reconfigure the Captain's Ship in the Factory (`UIMakePlayer`). It cannot be deleted or scrapped (`isSpecialProtected`).
3. **Barred from Generic Wingman Slots**: In `FormChooseFlyAI.as`, `ckAIShow()` returns `false` for any craft where `special == true` or `name == "Captain's Ship"`, hiding the `[ADD AI]` button. `addAI()` and `fillBestAI()` in `UIChooseFlyAI.as` explicitly reject the Captain's Ship.
4. **Barred from Direct Player Piloting**: In `UIChooseFlyAI.as`, clicking the Captain's Ship row displays its statistics in the detail preview, but does not allow assigning it to `ConfigAndStory.PLAYER`. The player always pilots their personal craft while Captain Tom pilots the flagship.
5. **UI Status Badge**: In `FormChooseFlyAI.as`, the Captain's Ship row displays a dedicated read-only `[CAPTAIN]` status indicator instead of `[ADD AI]`.
6. **State Sanitization**: On `UIChooseFlyAI` load, any stale references to the Captain's Ship in `ConfigAndStory.AI` or `ConfigAndStory.PLAYER` are sanitized to prevent invalid states in older save files.

## Consequences
- Prevents duplicate Captain ships and behavioral desynchronization in combat.
- Preserves full player agency to customize and upgrade the allied flagship in the Factory.
- Establishes a clear domain separation between Player Craft, AI Wingmen, and Allied Flagships.

