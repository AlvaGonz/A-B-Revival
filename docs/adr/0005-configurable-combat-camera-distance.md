# 5. Configurable Combat Camera Distance and Dynamic FOV Scaling

Date: 2026-11-06

## Status
Accepted

## Context
During space battles, players need tactical flexibility regarding battlefield situational awareness:
1. The default combat camera runs at a fixed $640 \times 480$ viewport (Short / 1.0x), which offers close-up detail but restricts tactical vision during fast engagements, long-range sniper fire, and multi-enemy flanking maneuvers.
2. Players require the option to zoom out the field-of-view (FOV) by 30% (Medium) or 50% (Far) to observe incoming projectile swarms, enemy carrier placements, and wingman positioning across a broader sector.
3. The camera distance toggle must be actionable on the fly during combat without interrupting the battle flow, with clear visual HUD feedback.
4. The user's chosen camera distance preference must persist across missions and application restarts.

## Decision
1. **Camera FOV & Buffer Scaling Model**:
   - Camera distance is modeled as a 3-tier preset index `0..2`:
     - Index `0` (**Short / Base**): $1.0\times$ view, $640 \times 480$ viewport buffer.
     - Index `1` (**Medium**): $1.3\times$ view ($+30\%$ wider vision), $832 \times 624$ viewport buffer.
     - Index `2` (**Far**): $1.5\times$ view ($+50\%$ wider vision), $960 \times 720$ viewport buffer.
   - Handled via `RenderData.setCameraDistance(level:int)`:
     - Updates `this.W` and `this.H` to preset dimensions.
     - Updates `cam.rect.width = this.W` and `cam.rect.height = this.H`.
     - Reallocates `CANVAS`, `BMPD`, `BMPDFX`, `BMPDBLTFX`, `BMPDBG`, and glow filter `sm` to match new buffer bounds.
     - Reassigns `this.bmp.bitmapData = this.CANVAS` and scales the container bitmap via `this.bmp.scaleX = 640.0 / this.W` and `this.bmp.scaleY = 480.0 / this.H`.
     - Preserves mouse aim accuracy in `camCtrl()` via `stage.mouseX / bmp.scaleX` and `stage.mouseY / bmp.scaleY`.
   - The UI and HUD layer remains unscaled at native $640 \times 480$ resolution.
2. **In-Combat Hotkey Toggle ("C")**:
   - In `OpOneCore.as`, key 'C' (`keyCode 67`) is intercepted via `this._key.isKeyDownOnce("C") || this._key.isKeyDownOnce("c")`.
   - Cycles cyclically: `Short (0) -> Medium (1) -> Far (2) -> Short (0)`.
   - Invokes `InitData.RENDERDATA.setCameraDistance(newLevel)`.
   - Synchronizes `ConfigAndStory.CONFIG[6] = newLevel` and dispatches `ConfigAndStory.save()`.
3. **Ephemeral HUD Distance Indicator**:
   - In `UIGame.as`, a dedicated HUD badge text (`camDistanceTxt`) is instantiated.
   - Upon pressing 'C', updates text to `[ CAM: SHORT (1.0x) ]`, `[ CAM: MEDIUM (1.3x) ]`, or `[ CAM: FAR (1.5x) ]`.
   - Resets alpha to `1.0` and smoothly fades out over ~1.5 seconds (45 frames at 30 FPS).
4. **Configuration Persistence**:
   - `ConfigAndStory.CONFIG[6]` stores the active camera distance index (`0`, `1`, or `2`).
   - Default is `0` (Short) if unset.
   - Initialized during game startup and loaded whenever entering combat.

## Consequences
- Players gain full control over situational combat awareness with immediate in-flight toggling.
- Combat rendering dynamically adapts FOV without distorting HUD or mouse aim coordinate spaces.
- User preference seamlessly persists across missions and game sessions without manual re-configuration.

