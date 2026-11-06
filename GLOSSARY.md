# Glossary: A&B Revival (Assembly & Battle Space)

Ubiquitous language for the decompiled codebase, game mechanics, and modding architecture.

## Core Entities
- **Ship / Craft (`SimpleFly`, `FlyBase`, `FlyData`)**: The player's or enemy's spacecraft instance, composed of attached modular components.
- **Player Craft (`PlayerCraft`, `ConfigAndStory.PLAYER`)**: The primary spacecraft directly piloted and controlled by the player during combat.
- **AI Wingman (`WingmanCraft`, `AI_P`, `ConfigAndStory.AI`)**: Generic allied craft deployed from available player hangar crafts into tactical wingman formation slots (`0..4`).
- **Allied Flagship / Captain's Ship (`CaptainShip`, `special == true`)**: A unique, non-duplicable allied capital craft piloted by Captain Tom during story missions. Customizable and armable by the player in the Factory (`UIMakePlayer`), but strictly barred from Player control and generic AI wingman slots in `UIChooseFlyAI`.
- **Part / Module (`FlyPart`, `F4PartMC`, `BaseMovePart`)**: A discrete physical component attached to a ship chassis, such as wings, weapons, thrusters, or armor plating.
- **Attachment Point / Slot (`F4DataHole`, `F4DataLinks`)**: Coordinate-anchored socket on a chassis where parts can be connected.
- **Fire Port / Hardpoint (`FirePort`, `FirePorts`, `FireMode`, `FireSingle`, `FireLoop`)**: Weapon emitter logic defining projectile spawn points, rate of fire, recoil, and cooldowns.
- **Bullet / Projectile (`F4DataBullet`, `CkBltObj`)**: Projectiles emitted during combat with assigned damage, velocity, and collision boxes.
- **Shop / Hangar (`UIShop`, `UIRepair`, `UIMakePlayer`)**: UI and state management handling player assembly, repairs, part purchases, and hangar fleet customization.
- **Infinite Stock (`shopPart`)**: Inventory model for repeatable components (weapons, wings, engines) that never deplete upon purchase, cloning bought items directly into `plPart`.
- **Finite Body Stock (`type == 0`, `Body`)**: Finite inventory rule for ship hulls/chassis, requiring discrete unit counts in `shopPart` that decrement upon purchase and restore upon sale.
- **Purchase Stack (`Sel.`, `xN`)**: Staged quantity multiplier for a single shop item in `FormShop`, represented as `x1`, `x2`, etc., scaling total transaction cost and resulting in N cloned parts added on checkout (`ACCEPT`).
- **Shop Reconciliation (`reconcileShopStock`)**: Routine synchronizing `shopPart` against `GIFT[0..STORY]`, deduplicating redundant infinite stock entries and backfilling missing historical unlocks in existing save files.
- **Mission / Director (`MissionData`, `Director`, `LevelData`, `LevelBase`)**: Campaign wave definitions, enemy spawn scripting, objective evaluation, and reward payouts.

## Tooling Vocabulary
- **JPEXS (FFDec)**: Open-source Flash decompiler and ActionScript bytecode editor used for unpacking, editing, and repacking `.swf` binaries.
- **REA (Reverse Engineer Anything)**: Agent-driven reverse engineering and static analysis toolkit connected via Model Context Protocol (MCP).
- **SWF (Shockwave Flash)**: Compiled binary container holding vector graphics, audio, symbols, and ActionScript 3 ABC (ActionScript Byte Code) tags.

