---
name: ab-game-ui-sprite-sheet-splitter
description: Splits a generated game UI sprite sheet or component board into individual transparent PNG elements, writes crop metadata, validates dimensions and alpha channels, and packages all PNG files into a ZIP archive. Use after component breakdown when users ask to cut a combined sprite sheet, isolate every button/icon/card/effect, export single elements, remove a solid background, or deliver a downloadable PNG asset pack.
---

> **Project constraint (A&B Revival).** Original Flash art and fonts are the baseline (ADR 0020); layout, UX and art improvements need explicit owner approval recorded in the issue/PR. This project renders UI with Unity UI Toolkit (UXML/USS, PanelSettings). Imported workflow steps are advisory: the repository workflow (wayfinder -> to-spec -> to-tickets -> TDD -> review) prevails.
>
> **Upstream tooling.** The Python scripts referenced below are not vendored; run them from an upstream checkout or use project tooling. They are optional and do not replace `scripts/export-*.bat` or ticket #124.

# Game UI Sprite-Sheet Splitting and PNG Packaging

Further cut the whole sprite sheet produced during component breakdown into separate transparent PNGs, and package them as a downloadable ZIP. Do not mistake "generating a component list" for "having completed pixel-level slicing".

This skill describes optional, owner-approved tooling. It does not replace `scripts/export-*.bat` or the pipeline from ticket #124; use it only when the owner asks for this specific output.

## Preconditions

Read:

- The approved component breakdown result
- `contracts/style-contract.yaml`
- `contracts/component-contract.yaml`
- `contracts/asset-manifest.yaml`
- `contracts/sprite-contract.yaml` (when present)

The input sprite sheet must be an actual image file. If there are only prompts or a component list, finish generating the component images first.

Final single-element PNGs must have all text removed, including titles, button copy, number labels, placeholder text, watermarks and annotation numbering. Text is not part of a component's graphic.

## Suitable inputs

- Transparent-background sprite sheets: prefer detection via the alpha channel.
- Solid or near-solid backgrounds: sample the surrounding background color and remove it.
- Buttons, icons, cards, decorations, effects and characters on one image.

Not suitable:

- Complete UI pages where elements occlude each other or are severely merged.
- Composite images with complex scene backgrounds and no spacing between elements.
- Assets that require reconstructing occluded pixels.

In these cases, regenerate a component sprite sheet with spacing instead of forcing a cut.

## Input sprite-sheet specification

Prefer requesting the following when generating component images:

- All elements complete, not touching, with whitespace on all sides.
- A uniform solid background or a transparent background.
- No text, numbering, watermarks or annotation lines.
- Shadows must not spill onto neighboring components.
- Similar states arranged in a fixed order, but without drawn grid lines.
- Do not split one component into several distant fragments.

## Automatic splitting

Run:

```bash
python3 scripts/split_sprite_sheet.py <source.png> \
  --output-dir <project>/assets/sprites/<pack-id>/items \
  --zip <project>/packages/<pack-id>-png.zip \
  --mode auto \
  --prefix <pack-id>
```

Default behavior:

1. Detect via alpha when the input has a transparent channel.
2. Sample the edge background color when the input is opaque.
3. Merge pixel regions that are close together.
4. Filter out overly small noise.
5. Add a safety margin around each element.
6. Export standalone transparent PNGs.
7. Generate `sprite-manifest.yaml`.
8. Generate a ZIP containing the manifest and all PNGs.

## Semantic mapping

Automatic indices cannot be final asset names. Prepare a YAML ordered by detection sequence:

```yaml
items:
  - semantic_name: shop-buy-button-normal
    component_id: shop-buy-button
    category: button
    state: normal
    slice: {type: 9-slice, margins: [36, 36, 28, 28]}
```

Add this when running:

```bash
--mapping mappings/shop-components.yaml
```

The tool uses `semantic_name` as the PNG file name and writes the component ID, category, state and slice into the manifest. Naming and slicing follow the component skill's semantic naming conventions and the [slice rules](references/slice-rules.md) respectively.

Automatic cutting does not automatically recognize or repair text. When the source sprite sheet still contains text, do one of the following before cutting:

1. Prefer regenerating a text-free sprite sheet.
2. If regeneration is impossible, mask-repair or content-fill the text regions, restoring the base texture.
3. Re-check after repair; do not simply crop out the text and leave transparent holes, color blocks or ghosts.

Never mark slices that still contain text as approved.

## Parameter tuning

Elements split too finely:

```bash
--connect-gap 6
```

Multiple elements merged into one:

```bash
--connect-gap 0
```

Background residue:

```bash
--mode background --background-tolerance 16
```

Light-colored components mistakenly removed:

```bash
--background-tolerance 8
```

Small icons filtered out:

```bash
--min-area 16
```

Need more crop padding:

```bash
--padding 8
```

Use `--force` explicitly when re-outputting; never silently overwrite an approved package.

## Project contract

Write the result to `contracts/sprite-contract.yaml`:

- Source sprite sheet and source page
- Detection mode and thresholds
- Output directory and ZIP path
- Single-element IDs, paths, original-image coordinates, dimensions and transparency state
- Manual review status

Register at least the following in `asset-manifest.yaml`:

- The original sprite sheet: `kind: sprite-sheet`
- The final archive: `kind: package`

Single-element details are managed by `sprite-contract.yaml`, to keep large component packs from bloating the global manifest.

## Manual acceptance

After automatic cutting, check each item:

- Each file contains exactly one complete element.
- Elements are not cut off.
- Adjacent elements are not merged.
- Shadows, glows and semi-transparent edges are complete.
- The background is transparent, with no white fringe, color fringe or dirty specks.
- No titles, button copy, number labels, watermarks or text ghosts.
- File names, coordinates and dimensions match the manifest.
- The ZIP can be extracted and contains all PNGs and the manifest.

When a cut is wrong, adjust the thresholds and regenerate first; if that still fails, record manual crop boxes in `sprite-contract.yaml` rather than marking the wrong result as approved.

## File naming

```text
<pack-id>-001.png
<pack-id>-002.png
<pack-id>-003.png
...
<pack-id>-png.zip
```

Once the component semantics are known, rename to:

```text
button-primary-default.png
nav-home-selected.png
currency-gold.png
effect-crescent-glow.png
```

Update the manifest after renaming; never change only the file names.

## Definition of done

- The original sprite sheet is traceable
- Each element is a standalone transparent PNG
- Automatic coordinates and actual dimensions are recorded
- Manual acceptance passed
- `review.text_free: true`
- The ZIP package exists and can be extracted
- Final PNGs use semantic names and are linked to component IDs
- The project passes strict validation
- `quickstart.md` records how to extract and use the package

When an atlas and engine JSON are needed next, invoke `ab-game-ui-asset-pipeline`.
