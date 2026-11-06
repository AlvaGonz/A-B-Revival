# 4. Combat Damage Telemetry, Ally Health Alerts, Critical Distressing, and Autofire System

Date: 2026-11-06

## Status
Accepted

## Context
During space battles, players need immediate, non-intrusive situational awareness regarding:
1. Which specific modular parts on the player's craft have been destroyed, accumulated throughout the engagement.
2. The survivability status of the Allied Flagship (Captain Tom), whose loss can cause mission failure or narrative consequence.
3. High-urgency visual distress feedback for both the Player (Michael) and Allied Commander when operating under critical damage ($\le 30\%$ total health).
4. Ergonomic relief from continuous left-mouse holding through an automatic continuous firing toggle.

## Decision
1. **Cumulative Player Damage Telemetry**:
   - Monitored at `AIUnity.as` upon part destruction (`hp <= 0`) for `this == InitData.CORE.playerFly`.
   - Ignores ship chassis/body (`type == 0`) since body loss triggers general ship destruction.
   - Categorizes lost components into canonical types: `Gun`, `Wing`, `Engine`, `Module`.
   - Rendered persistently above the player's lifebar in `UIGame.as` as a compact badge display (e.g. `LOST: [Gun x2] [Wing x1]`).
   - Accompanied by a low-volume audio cue (`S_Cant`, vol 0.35) and a brief flash when increments occur.
2. **Allied Flagship Health Threshold Alerts**:
   - Monitored continuously in `UIGame.as` against Captain Tom's total health bar (`pf1.unity.udata.hp.x / hp.y * 100`).
   - Dispatches single-shot tactical alerts upon downward crossing of four discrete thresholds:
     - `70%`: `[ CAPTAIN TOM: 70% HEALTH ]`
     - `50%`: `[ CAPTAIN TOM: 50% HEALTH ]`
     - `30%`: `[ WARNING: CAPTAIN TOM CRITICAL - 30% ]`
     - `10%`: `[ EMERGENCY: CAPTAIN TOM FAILING - 10%! ]`
   - Re-arms thresholds if the Captain recovers health above 70%.
3. **Critical State Portrait Distressing ($\le 30\%$ Total Health)**:
   - Evaluated for both Michael (`lifeBar.face`) and Captain Tom (`f1.face`).
   - While health $\le 30\%$, the portrait frame applies a rhythmic red pulsing `ColorTransform` (heartbeat/alarm modulation).
   - Upon taking direct bullet/collision impacts while critical, the portrait frame applies a transient micro-jitter/shake ($\pm 2-3$ px).
   - Distressing automatically ceases when health restores above 30%.
4. **Mouse-Anchored Autofire System ("X" Hotkey)**:
   - In `OpOneCore.as`, pressing key 'X' toggles `autoFire` (`true / false`).
   - Weapon discharge condition evaluates as `isFiring = this.autoFire || this._mouse.mouseIsDown`.
   - Rendered in `UIGame.as` as a small green circular dot anchored adjacent to the mouse cursor (`mouse`), visible strictly when `autoFire` is active and hidden otherwise.

## Consequences
- Provides clear, continuous awareness of ship degradation without obstructing combat view.
- Enhances player emotional connection to Allied Flagship survival through dramatic health thresholds.
- Delivers strong visual urgency when ships enter life-or-death health zones.
- Drastically reduces player finger strain during extended wave battles.

