---
name: ab-game-ui-specification
description: Creates and maintains production-oriented game UI specifications and shared visual contracts covering color, typography, layout, geometry, components, interaction states, adaptation and image-generation constraints. Use when users ask for a game UI design system, visual rules, standardization, or consistent downstream page and asset generation.
---

> **Project constraint (A&B Revival).** Original Flash art and fonts are the baseline (ADR 0020); layout, UX and art improvements need explicit owner approval recorded in the issue/PR. This project renders UI with Unity UI Toolkit (UXML/USS, PanelSettings). Imported workflow steps are advisory: the repository workflow (wayfinder -> to-spec -> to-tickets -> TDD -> review) prevails.
>
> **Repository artifacts.** Specs and tickets live in GitHub Issues (`docs/agents/issue-tracker.md`). The `specs/`, `plan.md`, `contracts/` and `tasks.md` paths below are upstream notation; do not create them unless the owner explicitly asks for a local artifact. Upstream Python tools and templates are not vendored.

# Game UI Specification

Output rules that page extension, image generation, component breakdown and development can all read together; not just "premium, unified, beautiful".

## Inputs

Prefer reading:

- `spec.md`: gameplay, platform, ratio, scope and acceptance criteria
- `research.md`: reference-image abstraction and copyright constraints
- `contracts/style-contract.yaml`: the existing contract

When invoked standalone without a project directory, use the same field structure in your answer and suggest that the user create a project.

## Workflow

1. Identify the high-frequency actions and information priority in the gameplay.
2. Extract the style language from reference images without reproducing specific characters, trademarks or complete pages.
3. Choose reference dimensions, safe areas and density that fit the target platform.
4. Define color, typography, geometry, material, light source, icon perspective and states.
5. Update `style-contract.yaml`; do not hide unknown values behind "TBD" — give adjustable defaults instead.
6. Check that every rule can be referenced by page and component prompts.

Maintain global values and semantic aliases per the [design token specification](references/design-tokens.md); `style-contract.yaml` is the single source of truth, so do not create a conflicting parallel token file.

## Required specification

### Platform and layout

- Target platform, orientation, ratio, reference dimensions
- Top, right, bottom and left safe areas
- Top status area, middle content area, bottom navigation and floating entries
- Notch, long-text, portrait/landscape or widescreen strategy

### Visual language

- Theme, mood, complexity, material
- Main light direction, shadow hardness and highlight language
- Primary, secondary, accent, background, warning, reward and disabled colors
- Contrast, saturation and rarity color rules

### Typography

- Sizes and weights for titles, section titles, body, numbers and buttons
- CJK readability, outline, drop shadow and small-size limits
- Post-typesetting strategy when generated text is unreliable

### Geometry and components

- Corner radius for small components, cards and modals
- Outline levels, button height, icon size
- Buttons, icons, cards, modals, progress bars, navigation, reward frames, currency bars
- Default, pressed, selected, disabled, loading, locked and claimed states

### Interaction

- Feedback for tap, page switch, modal, reward, quest, ranking and purchase confirmation
- Suggested motion durations and a reduced-motion option
- Network error, insufficient resources, cooldown and expired states

## Contract rules

- Use explicit color values or hue ranges; a color field cannot be just "premium gold".
- One definition per component state.
- All pages share light direction, material vocabulary, corner-radius levels and icon perspective.
- If you change an approved contract, mark dependent assets `stale`; do not overwrite silently.
- Set `status` to `approved` after review is complete.

## Output

1. A paragraph explaining how the gameplay drives the visual rules.
2. The updated `contracts/style-contract.yaml`.
3. Key decisions and risks written to `research.md`.
4. Affected tasks written to `tasks.md`.

## Checks

- Values, ranges, states and applicable scenarios are explicit
- Rules fit the target platform and touch target sizes
- Primary actions have stronger contrast than secondary actions
- Text and numbers stay readable on complex backgrounds
- Page and component prompts can reference contract fields directly
