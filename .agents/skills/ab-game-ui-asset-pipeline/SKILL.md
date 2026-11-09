---
name: ab-game-ui-asset-pipeline
description: Packs semantically named transparent game UI PNG files into an atlas, preserves component states and reviewed 9-slice metadata, and exports deterministic JSON handoff manifests for Godot, Unity, Cocos or generic consumers. Use after sprite-sheet splitting when users ask for atlas packing, nine-patch metadata, engine delivery, Unity sprites, Godot regions, Cocos frames or a Game UI Factory production handoff.
---

> **Project constraint (A&B Revival).** Original Flash art and fonts are the baseline (ADR 0020); layout, UX and art improvements need explicit owner approval recorded in the issue/PR. This project renders UI with Unity UI Toolkit (UXML/USS, PanelSettings). Imported workflow steps are advisory: the repository workflow (wayfinder -> to-spec -> to-tickets -> TDD -> review) prevails.

# Game UI Atlas and Engine Handoff

Convert already-approved semantic PNGs into an atlas and engine JSON. This skill does not perform pixel recognition, does not guess semantics automatically, and does not claim to generate native engine project files.

This skill describes optional, owner-approved tooling. It does not replace `scripts/export-*.bat` or the pipeline from ticket #124; use it only when the owner asks for this specific output.

## Preconditions

- `sprite-contract.yaml` has status `approved`
- All PNGs use semantic names
- Every item is linked to a `component_id`, `state` and `slice`
- `review.text_free: true`
- 9-slice margins have been manually previewed

## Slicing modes

- `9-slice`: buttons, panels, modals, progress bars
- `full`: cards, characters, badges, backgrounds
- `1:1`: icons, currency
- `tile`: repeatable textures

9-slice margins format:

```yaml
slice:
  type: 9-slice
  margins: [left, right, top, bottom]
```

## Atlas

Read `contracts/atlas-contract.yaml` and run:

```bash
python3 scripts/build_sprite_atlas.py specs/<project-id> --force
```

Built-in packer:

- Uses transparent PNGs
- Does not rotate assets
- Can pad to power-of-two dimensions
- Stores region, pivot, state, component ID and slice
- Generates the atlas PNG and JSON
- Updates the atlas contract to `packed`

After packing, inspect manually, then change the status to `approved`.

## Engine JSON

Read `contracts/export-contract.yaml` and run:

```bash
python3 scripts/export_engine_manifest.py specs/<project-id> --force
```

Supports:

- Godot JSON: texture, region, patch margins
- Unity JSON: Sprite Multiple rect, pivot, border, pixels per unit
- Cocos JSON: frame rect, original size, cap insets
- Generic JSON: atlas and sprite metadata

These files are deterministic delivery manifests. Native `.tres`, `.tscn`, Unity SpriteAtlas and Cocos project importers are outside the built-in implementation.

## Manual acceptance

- The atlas has no overlaps, clipping or transparency contamination
- Regions match the source PNG dimensions
- 9-slice assets keep a stretchable center
- All semantic names are unique
- Engine JSON references the same atlas
- Never mark native project files as generated

## Definition of done

- `atlas-contract.yaml` has status `approved`
- The atlas PNG and JSON exist
- `export-contract.yaml` has status `approved`
- Each target engine JSON exists and parses
- The project passes strict validation
