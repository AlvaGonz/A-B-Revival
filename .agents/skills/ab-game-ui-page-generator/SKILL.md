---
name: ab-game-ui-page-generator
description: Designs and generates one complete game UI page at a time using an approved style and screen contract, producing a page brief, reproducible image prompt, negative constraints, an actual visual asset when image generation is approved and available, and a manifest entry. Use for home, task, ranking, shop, inventory, character, event, reward, settlement, login, onboarding, HUD or other game screens.
---

> **Project constraint (A&B Revival).** Original Flash art and fonts are the baseline (ADR 0020); layout, UX and art improvements need explicit owner approval recorded in the issue/PR. This project renders UI with Unity UI Toolkit (UXML/USS, PanelSettings). Imported workflow steps are advisory: the repository workflow (wayfinder -> to-spec -> to-tickets -> TDD -> review) prevails.

# Game UI Single-Page Visual Generation

Finish one page at a time, inherit the project contracts, and do not turn consecutive pages into unrelated style drafts.

## Preconditions

Read:

- The three planning documents must already be completed by `ab-game-ui-product-design`: `gdd.md`, `prd.md`, `interaction.md` (`status: approved`, or `quickstart.md` records that the planning gate was skipped)
- `spec.md` and `research.md`
- `contracts/style-contract.yaml`
- The target page's entry in `contracts/screen-contract.yaml`
- The approved baseline page and `asset-manifest.yaml`

If the three planning documents are missing or still `draft` and the user has not explicitly skipped, stop image generation and invoke the first generation step `ab-game-ui-product-design` instead.
If the style contract is still `draft`, you may generate exploratory drafts, but assets must not be marked approved.

## Image generation gate

Image generation is **off by default**. Run it only when the owner has explicitly approved that specific page in the issue/PR. Approval for one page does not approve the next one. Never record an account, key, token or subscription detail in the repository or in generated files.

## Page steps

1. Confirm the page ID, purpose, primary action, states and ratio; check that page's main flow in `interaction.md`.
2. Establish the information hierarchy: key visual, primary action, resource state, navigation, secondary entries.
3. Describe the top, middle, bottom and floating regions concretely, not with generic layout names only.
4. List buttons, cards, icons, characters, status bars, navigation, bubbles and decoration.
5. Inject color values, material, light direction, corner radius, outlines and typography strategy from the style contract.
6. Produce the complete positive prompt and the negative constraints.
7. When image generation is approved and a tool is available, generate the image; otherwise keep the prompt and degrade gracefully.
8. Save the prompt and image, and update the page contract and asset manifest.

## Prompt file

Save as `prompts/pages/<screen-id>.md`; the structure must be:

```markdown
# <Page name>

## Page positioning
## Visual constraints
## Layout and components
## States
## Image generation prompt
## Negative constraints
## Acceptance record
```

The positive prompt must include:

- Page name, game genre, platform, ratio and reference dimensions
- Concrete content of the top, middle, bottom and auxiliary regions
- Main character, scene or core gameplay focus
- Color, material, light source, corner radius, outline and icon language from the contract
- Clickable texture, information hierarchy, CJK text strategy, image quality
- A requirement to stay consistent with the approved baseline page

The negative constraints must include at least:

- Garbled text, wrong text, trademarks and unauthorized characters
- Button deformation, wrong perspective, elements merging, subject occlusion
- Confused information hierarchy, excessive complexity, low clarity
- Color tone, light direction, material or corner radius that violates the contract

## Image generation

When image generation is approved and a tool is available:

1. Use the closest ratio the tool supports.
2. Name files `<screen-id>-v<number>.<ext>`.
3. Save the result to `assets/pages/`.
4. Record the real path, dimensions, status and version source in the manifest.
5. If text in the generated image is unreadable, it fails acceptance; generate a text-free base or plan for later typesetting.

When generation is not approved or no tool is available:

- Do not create fake images
- `status: pending-generation`
- `approved: false`
- The prompt path must be valid

## Page approval

Set `approved` to `true` only when all of the following hold:

- The primary action is clear and the page purpose holds
- Ratio, safe areas and navigation are reasonable
- The visual language from the contract is consistent
- Key states have a corresponding design
- Text is readable, or later typesetting has been explicitly adopted
- The user confirms, or a clearly defined project acceptance rule passes

## Output

- Page brief and prompt file
- The actual visual draft, or an explicit pending-generation status
- Updated `screen-contract.yaml`
- Updated `asset-manifest.yaml`
- Suggestions for the following component breakdown
