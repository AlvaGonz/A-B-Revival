---
name: ab-game-ui-component-breakdown
description: Breaks an approved game UI page or visual style into reusable, individually generated component assets with clean edges, transparent backgrounds where appropriate, stable naming, states, dimensions, prompts and traceable manifest records. Use for UI slicing, transparent buttons, icons, cards, modals, navigation, characters, props, frames, labels, effects or development handoff.
---

> **Project constraint (A&B Revival).** Original Flash art and fonts are the baseline (ADR 0020); layout, UX and art improvements need explicit owner approval recorded in the issue/PR. This project renders UI with Unity UI Toolkit (UXML/USS, PanelSettings). Imported workflow steps are advisory: the repository workflow (wayfinder -> to-spec -> to-tickets -> TDD -> review) prevails.
>
> **Upstream tooling.** The Python scripts referenced below are not vendored; run them from an upstream checkout or use project tooling. Generation is off by default and needs explicit owner approval for each use.

# Game UI Component Breakdown

Output standalone assets, or a component sprite sheet intended for later automatic cutting, plus their contracts; do not regenerate the complete page.

## Preconditions

Must read:

- `contracts/style-contract.yaml`
- `contracts/screen-contract.yaml`
- `contracts/component-contract.yaml`
- `contracts/asset-manifest.yaml`
- The approved page visual

The source page's manifest entry must be `approved: true`. If it is not approved, stop the breakdown and explain that page acceptance must be completed first.

## Image generation gate

Image generation is **off by default**. Run it only when the owner has explicitly approved that specific component in the issue/PR. Approval for one component does not approve the next one. Never record an account, key, token or subscription detail in the repository or in generated files.

## Categories

- `background`: full scene backdrop, with no text, buttons, navigation or character occlusion
- `control`: primary button, secondary button, close, back, claim, navigation buttons
- `container`: cards, text boxes, modal backplates, currency bars, progress bars
- `icon`: resource, function, state and navigation icons
- `character`: characters, NPCs, pets, avatars
- `decoration`: borders, corner marks, light effects, bases, ribbons and particles

## Breakdown steps

1. Decide whether to break down the full set or a specified scope.
2. Establish component IDs, categories, purpose, reusability, dimensions, states and source-page relationships.
3. Separate page-specific components from cross-page base components.
4. Write an independently executable prompt file for each component.
5. When generation is approved and a tool is available, generate one at a time; do not arrange multiple components into an asset board.
6. Check edges, alpha channels, cropping, light direction and proportions.
7. Update the component contract and asset manifest.

## Sprite-sheet mode

When the user later needs "sprite-sheet splitting, single-element PNGs, ZIP packaging", you may first generate one component sprite sheet as an intermediate artifact:

- All elements are complete, do not touch each other, and keep clearly visible spacing.
- Use a transparent background or a single solid background.
- No text, numbering, grid lines, watermarks or annotation arrows.
- Shadows and glows must not cross into neighboring elements.
- Different states of the same component keep a fixed order and a consistent silhouette.

A sprite sheet is not the final slice. After generating it you must invoke `ab-game-ui-sprite-sheet-splitter` to export standalone PNGs and complete manual acceptance.

Final file names follow the [component semantic naming conventions](references/naming-conventions.md). `element-001.png` is allowed only as an intermediate detection result and cannot be an approved deliverable.

## Default rules

- Use a transparent background except for backgrounds.
- Buttons, cards, modals and labels should have no text by default; keep typesetting whitespace.
- Icons keep the same perspective, light source, outline and small-size recognizability.
- Characters are complete and unobstructed, in a pose suitable for compositing into the original layout.
- Decorations are stackable and have no opaque rectangular base.
- Keep the source page's visual weight; do not generate secondary buttons stronger than primary buttons.
- Fill in `slice` metadata for buttons, panels, modals and progress bars; 9-slice margins need manual preview confirmation.

## Single-component prompt file

Save as `prompts/components/<component-id>.md`:

```markdown
# <Component name>

## Source and purpose
## Dimensions and states
## Contract fields that must be inherited
## Image generation prompt
## Negative constraints
## Acceptance record
```

The prompt must make explicit:

- "standalone game UI component asset"
- Component category, purpose, target dimensions and state
- Color tone, material, light direction, outline, thickness and corner radius
- Whether it is transparent, whether it has no text, centered composition and safe margins
- High resolution, complete silhouette, suitable for compositing and development
- Do not generate a complete page; do not merge other components

## Special rules

### Background

No UI controls, text, buttons, navigation or foreground characters; when extension is needed, require naturally continuous edges.

### Buttons and containers

Blank text area, complete border, realistic clickable texture; generate a separate file for each state, never a multi-state board in one image.

### Icons

Recognizable at small size, no text, consistent perspective; no base plate unless the contract requires one.

### Characters

Complete silhouette, no occlusion, transparent background; never reproduce an unauthorized protected character.

## Acceptance

- File paths, component IDs and the source page are traceable
- Transparency requirements are correct for non-background components
- No text, garbled text, dirty edges, background residue or accidental cropping
- Silhouettes stay stable across states, and visual differences follow the contract
- File names use `<component-id>-<state>-v<number>.<ext>`
- Each asset is registered separately in the manifest

When a tool is unavailable, keep the prompt, set the status to `pending-generation`, and never fabricate transparent assets.
