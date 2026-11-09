---
name: ab-create-game-assets
description: Plan, source, normalize and validate cohesive game art for this project — art direction, manifests, provenance, sprite/UI normalization and acceptance gates. Image generation is off by default and needs explicit owner approval. Use when defining an art brief, auditing extracted assets, normalizing sprites, or planning a new asset family.
---

# Create game assets (owner-gated)

> **Project constraint (A&B Revival).** Original Flash art, fonts and audio are the baseline (ADR 0020). Do not replace or restyle them without the owner's explicit approval recorded in the issue/PR. Image generation is **off by default** and needs owner approval for each use; never commit credentials, account names, subscription details or generated art unless the owner approves that asset. The repository workflow (wayfinder -> to-spec -> to-tickets -> TDD -> review) prevails.

Turn visual intent into a consistent, engine-ready asset set. Generation is one optional production tool behind an approval gate, never proof that an asset is shippable.

## Core workflow

1. **Inspect before inventing.** Find the extracted sprites, reference pages, fonts, import settings, camera framing, target resolution and naming conventions. Prefer the existing direction; this project already has a full extraction manifest and reference renders.
2. **Lock the technical frame.** Record engine (Unity 6000.6.8f1), 2D, camera/view, native display sizes (1920x1080 baseline; phone/tablet profiles), asset dimensions, world scale (1 unit = 1 pixel), transparency, palette, filtering, frames, texture budget and target platforms.
3. **Name the visual system.** Shape language, silhouette priorities, value structure, palette roles, materials, lighting, detail density, edge treatment, motion character. Use concrete properties, not an artist's name.
4. **Make an asset manifest.** List every required asset, state, variant, size, pivot, collision role, source, license and approval status. Separate production assets from greybox placeholders. Reuse `docs/assets/extraction-manifest.json` where the asset already exists.
5. **Approve one visual target.** Pick a representative hero asset or small style board before producing a set. Judge it at game scale over a real background. Record the owner's decision.
6. **Produce related assets as families.** Reuse the approved target as reference; keep palette, view, proportions, lighting, outline and texture density invariant. Small coherent batches, not unrelated one-offs.
7. **Normalize deterministically.** Crop, size, anchor, slice, name, compress and check alpha with project tooling. Never trust generated grids, transparency, seams, pivots or dimensions without inspection.
8. **Import with engine-native settings.** Filtering, mipmaps, pixels-per-unit, color space, compression, sprite slicing, texture types and collision. Read the project's importer code before changing asset settings.
9. **Validate in context.** Contact sheet plus the actual game at native resolution: silhouette, scale, animation stability, seams, legibility, palette, collision fit, memory and compression.
10. **Record provenance.** Source URL/tool, license or generation note, edit history and restrictions beside the manifest. Approved improvements state which original asset they replace or adjust.

## Choose the production path

| Need | Default path |
|---|---|
| Existing asset needs a controlled change | Edit the original/reference; state what must remain unchanged; owner approval |
| New 2D visual family | Approve seed -> source or generate with owner approval -> normalize -> preview -> import |
| True pixel art | Use generated work as a draft; enforce grid, palette, clusters and frames with pixel tools |
| Tileable surface or tileset | Small family; repair seams; test 3x3 repetition before approval |
| UI art or icons | Keep text and interaction code-native; prefer SVG/vector for simple symbols |
| 3D model or material | Concepts as reference; author/clean in a DCC; validate topology, UVs, scale, pivots and LODs |
| No generation tool available | Build the brief and manifest; source licensed assets or keep explicit greybox placeholders |
| Audio asset | Route implementation to the audio pipeline; still track source/license in the manifest |

## Image-generation gate

Generation runs only when the owner approves the specific use in the issue/PR. The brief stills owns the constraints; the generation call happens outside this repository.

Build prompts from these blocks:

```text
ROLE/PURPOSE: production asset for [gameplay role]
SUBJECT: [specific object/character and action]
VIEW: [orthographic/top-down/side/three-quarter], [camera and facing]
ART DIRECTION: [shape language], [palette roles], [materials], [edge treatment]
GAME-SCALE READ: [silhouette and focal details that must survive at WxH]
TECHNICAL OUTPUT: [dimensions/aspect], [transparent or scene background], [frame/slot count]
LOCKS: preserve [identity, proportions, palette, costume, lighting, line weight]
EXCLUDE: text, labels, mockup frames, scenery, duplicate objects, cropped edges, signatures
```

For edits, say both what changes and what stays fixed. Ask for transparency through the tool's native option, then verify the alpha channel. A generated file is not committed until the owner approves that specific asset.

## Normalization and QA

Use project tooling instead of the upstream scripts (they are not vendored here):

- `python scripts/export_all_assets.py` — extraction, manifest and Unity import settings.
- `scripts/export-ui-sprites.bat` — UI sprite regeneration and contact sheet.
- Pillow is already used by the export pipeline; do not add a second image stack.

Inspect constraints and keep a machine-readable report next to the asset manifest. Check size, alpha, palette count and texture budget before import.

## Quality gates

- **Cohesion:** related assets share palette roles, line/edge treatment, view, light direction, scale and detail density.
- **Gameplay read:** silhouettes and state changes stay clear at native resolution, in motion, and over real backgrounds.
- **Technical fit:** exact size/frame count, usable alpha, stable pivots, correct color space/filtering, no clipped content, no accidental labels or baked mockup chrome.
- **Animation:** identity, volume, proportions, costume, facing and baseline do not drift.
- **Tiles/backgrounds:** edges tile without seams; parallax layers have intentional depth and no baked collision cues.
- **3D:** transforms, scale, pivot, normals, UVs, materials, topology, rig, collision proxies, LODs and runtime format are checked, not inferred.
- **Rights:** every shipped file has recorded provenance and terms compatible with the project.

Do not call an asset production-ready from a prompt result alone. Approval requires the technical checks plus an in-engine or native-scale inspection.

## References

- `references/art-direction.md` — visual-system decisions and consistency.
- `references/raster-pipeline.md` — sprites, animation strips, tiles, backgrounds, UI art and import settings.
- `references/three-d-pipeline.md` — concept-to-mesh, textures, glTF/GLB, LOD, collision and runtime validation.
- `references/provenance.md` — licenses, generated-media records and provenance.

## Related skills

- `ab-game-ui-ux` — layout, navigation, readability and accessible interaction.
- `ab-performance-optimization` — texture, atlas and draw-call budgets.
- The project's extraction pipeline: `scripts/export_all_assets.py`, `docs/assets/extraction-manifest.json`.
