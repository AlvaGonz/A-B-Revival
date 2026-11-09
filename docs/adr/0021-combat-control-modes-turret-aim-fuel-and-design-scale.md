# 0021. Combat Control Modes, Independent Turret Aim, Booster Fuel, and One Design Scale

**Date:** 2026-11-09
**Status:** Accepted
**Builds on:** ADR 0005, ADR 0012, ADR 0017, ADR 0019, ADR 0020
**Implements:** ticket #179

---

## Context

The frozen release had two control schemes (`OpOneCore.playerCtrlMode`): the cursor-distance
scheme and the WASD scheme. The Unity port only had WASD hull control with no turret aim, kept
an inert `ShipEntity.energy` field, and mixed a 2.25x HUD canvas with a 2x world viewport. The
2011 HUD has three stacked bars (health, `pow`, `powb`), of which only health was live.

## Decision

1. **Keyboard is the default control mode.**
   - `WASD` drives the hull; the mouse defines the turret target and left click fires.
   - `Q` toggles between keyboard and mouse mode during battle.
   - The choice is stored in `PlayerSaveData.controlMode`; the Options screen (#181) reads and
     writes the same setting through `PlayerFlightController.ControlMode` /
     `ControlModeChanged`.
2. **Mouse mode follows the frozen cursor mapping.**
   - Cursor distance from the craft sets throttle, with the frozen 100/200 px dead zone and
     saturation scaled to the design width.
   - The hull turns toward the cursor; all turrets auto-target the closest hostile inside the
     highest effective range among the mounted weapons.
3. **Gun mounts aim independently.**
   - Each `WeaponMount` carries its hull pivot, part-local muzzle marker, and a full-360 world
     aim heading. `MissionSpriteView` rotates only `PartCategory.Gun` sprites; wings, engines,
     armour, and hull stay hull-fixed. Projectiles spawn at the rotated muzzle.
4. **`energy` becomes `boosterFuel`.**
   - `ShipInput.boost` (`Space`) multiplies applied thrust by the frozen `aiPowMut = 3` and
     drains 100 fuel/s from a 500 default tank. Empty fuel cancels the multiplier.
   - `fuel` pickups refill through `ShipEntity.RefuelBooster`; the HUD bar below health is Fuel.
   - The third bar stays visible with an inert, reserved fill. It is never bound to data.
5. **One 3x design scale.**
   - `CameraDistancePresets.DesignScale = 3`: the 640x360 16:9 safe area maps to 1920x1080
     design pixels at Short. Medium and Far keep the ADR 0005 1.3x/1.5x multipliers.
   - The HUD stylesheet is authored in the same 3x coordinates.
   - The 2x mount atlas is rendered at `MountArtScale = 1.5` and part socket/muzzle offsets are
     multiplied by the same amount, so world and HUD art share one on-screen scale at 1080p.
   - Gameplay positions and velocities remain frozen stage pixels (ADR 0017).

## Consequences

- The HUD and world no longer disagree about mouse coordinates or art density.
- Turrets can target anything around the craft; the hull no longer needs to point at the target
  in keyboard mode.
- Fuel is a real resource with a visible drain/refill loop instead of an unused field.
- The third bar is intentionally blank until a later ticket defines its resource; it must not be
  filled with derived or invented values.
- ADR 0019's 1280x720 Short viewport is superseded by the 3x design viewport. Its 2x atlas
  density decision still stands and is compensated at draw time.
