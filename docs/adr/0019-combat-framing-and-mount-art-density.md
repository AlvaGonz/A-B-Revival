# 0019. Combat Framing and In-Game Mount Art Density

**Date:** 2026-11-08
**Status:** Accepted; decision 1 superseded by ADR 0021 (3x design viewport)
**Builds on:**
- `docs/adr/0005-configurable-combat-camera-distance.md` (Short / Medium / Far)
- `docs/adr/0013-unified-60fps-1080p-stage3d-baseline.md` (2x atlas rasterisation)
- `docs/adr/0017-world-units-and-physics-settings.md` (1 unit = 1 stage pixel)

---

## Context

The first human playtest found ships rendering as placeholder rings. Two independent causes
were present:

1. The sprite pipeline exported the frozen part wrapper symbol (the shop icon canvas), not the
   in-game `SHOW` frame, and the mission drew one body quad per hull.
2. The combat camera showed 1920x1080 world pixels while the frozen art was authored for a
   640x480 stage. A 12x20 px small fighter therefore occupied 2% of the 1080p screen, and the
   part art itself was never composed from the hull's socket markers.

ADR 0013 already requires the modern baseline to rasterise vector art at 2x into the atlas.
ADR 0017 keeps gameplay positions, velocities and ranges in frozen stage pixels. Those two
decisions do not say what the combat viewport height should be in the 1920x1080 output.

## Decision

1. **Combat viewport is 1280x720 world pixels at Short.**
   - `CameraDistancePresets.DesignWidth/Height` become 1280x720; Medium is 1664x936 and Far is
     1920x1080.
   - UI Toolkit remains screen-space at the native output resolution; only the battlefield and
     mouse-aim mapping use the combat viewport.
   - With the 2x mount-art density this reproduces the frozen 640x480 framing (2x art in a 2x
     viewport) at native 1080p output.

2. **Mount art is the frozen frame-0 raster at 2x.**
   - `SpriteNaming.MountZoom = 2` drives both the FFDec export and the H/F/L marker conversion,
     so art pixels and socket offsets share one scale.
   - The in-game sprite is the part symbol's frame 0 (the frozen `MC2BMPD` 64-rotation sheet
     frame 0), rotated a quarter turn into Unity's nose-up heading convention.
   - Shop thumbnails stay at 1x in `Assets/Art/Sprites/Parts`; mount art lives in
     `Assets/Art/Sprites/PartMounts`. `PartMounts.txt` records both files per part id.

3. **Sockets come from the `H`/`F`/`L`/`P` markers, not from the AMF3 graph.**
   - The AMF3 `defFly` graph links parts to hole indices only. `PartMountTable` carries the
     decoded marker geometry and `CraftComposition` applies the frozen
     `hole.xy - child.linkOff` rule, mirroring `lr = 1` variants.
   - A missing or undecodable socket uses the documented category fallback
     (`PartMountTable.FallbackOffset`): wings outboard, engines aft, guns forward. It never
     silently centres the child on the parent.

4. **Layering is engine, hull, wings, guns/modules.**
   - This is the ticket's readable-combat ordering. The frozen `AIURender` draws body, wings,
     then everything else (engines and guns); engines are moved behind the hull because the
     composed engine art is mostly occluded and reads better behind the chassis.

## Rejected Options

- **Keep 1920x1080 world view and upscale art 3x:** the 2048 atlas cannot hold 3x art within
  its memory budget, and 3x upscaling is not the 2x density ADR 0013 specifies.
- **Keep 1x art and shrink the camera to 480 world height:** preserves the frozen framing but
  discards the 2x high-DPI atlas decision and makes the HUD-to-aim mapping disagree with the
  visible world.
- **Read socket coordinates from the AMF3 payload:** the frozen graph does not carry them;
  only the SWF marker layers do.

## Consequences

- A small fighter hull is roughly 5% of the screen height at Short, matching the frozen
  release; large hulls and the Captain are recognisable without zooming out.
- Mouse aim, camera presets and the starfield share one viewport contract.
- Every part now draws its own in-game art at its socket offset; there is no per-hull fallback
  ring in the composed path.
