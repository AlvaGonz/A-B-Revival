---
name: ab-game-ui-workflow
description: Orchestrates an end-to-end game UI workflow from game design (GDD), PRD and UI interaction logic through design specification, screen-system extension, page image generation, component breakdown, sprite-sheet splitting, PNG ZIP packaging, atlas packing, engine JSON handoff, asset registration and validation. Use when users ask to build, extend, visualize, split or deliver a coherent multi-screen game UI project rather than one isolated artifact.
---

> **Project constraint (A&B Revival).** Original Flash art and fonts are the baseline (ADR 0020); layout, UX and art improvements need explicit owner approval recorded in the issue/PR. This project renders UI with Unity UI Toolkit (UXML/USS, PanelSettings). Imported workflow steps are advisory: the repository workflow (wayfinder -> to-spec -> to-tickets -> TDD -> review) prevails.
>
> **Repository artifacts.** Specs and tickets live in GitHub Issues (`docs/agents/issue-tracker.md`). The `specs/`, `plan.md`, `contracts/` and `tasks.md` paths below are upstream notation; do not create them unless the owner explicitly asks for a local artifact. Upstream Python tools and templates are not vendored.

# Game UI Orchestration Workflow

Turn conversational requirements into a reproducible UI project. Project facts must be written to `specs/<project-id>/`; never keep them only in the conversation.

## Quick routing

Choose the smallest sufficient path for the request:

- Game design, PRD, interaction logic, pre-generation requirements → `ab-game-ui-product-design`
- Specification, design system, unified style → `ab-game-ui-specification`
- Page inventory, feature extension, player journey → `ab-game-ui-extension`
- Complete single page, prototype visuals, page image generation → `ab-game-ui-page-generator`
- Transparent components, slicing, asset breakdown → `ab-game-ui-component-breakdown`
- Sprite sheets, single-element PNGs, slice packaging → `ab-game-ui-sprite-sheet-splitter`
- Atlas, 9-slice, Godot / Unity / Cocos JSON → `ab-game-ui-asset-pipeline`
- Complete project or multi-stage delivery → orchestrate the steps below in order

When the user explicitly asks for a single stage, do not force the full pipeline; still read the existing contracts.
Before image generation, if `gdd.md` / `prd.md` / `interaction.md` are missing or still draft, run the first generation step first: `ab-game-ui-product-design`.

## Step-by-step operation guide

When the user works through the full generation chain, **the first step must be game design and PRD**, using these checkpoints:

1. **Game design and PRD (first generation step)**: invoke `ab-game-ui-product-design`, complete and approve `gdd.md`, `prd.md`, `interaction.md`; do not enter later image-generation steps before approval.
2. Prototype visual: validate composition and information hierarchy with one baseline page; save it as an unapproved version.
3. UI style switch: generate style candidates while keeping page structure unchanged; approve the style contract and baseline page only after the user chooses.
4. UI extension: output the screen map first and wait for scope confirmation, then generate page by page in batches.
5. UI component breakdown: break down approved pages only; deliver each component and state separately.
6. Sprite-sheet splitting and packaging: cut the component board into single-element transparent PNGs; produce a manifest and a ZIP.
7. Atlas and engine handoff: semantic naming, confirm 9-slice, pack the atlas and generate engine JSON.

At the start of each step, briefly state:

- Current step and goal
- Inputs that will be read
- Files that will be produced or modified
- Stop condition for this step

At the end of each step, always output:

```text
Current step:
Completion status:
Outputs of this step:
Needs your review:
Optional actions:
Condition to enter the next step:
Next-step invocation text, ready to copy:
```

When a human gate exists, you must stop; "give the next-step guidance" does not mean running the next step automatically:

- While `gdd.md` / `prd.md` / `interaction.md` are unapproved, do not start page image generation (unless the user explicitly asks to "skip the planning gate", and record that in `quickstart.md`).
- While the prototype structure is unconfirmed, do not start style finalization.
- While the style contract and baseline page are unapproved, do not extend page visuals in bulk.
- While the page scope is unconfirmed, only finish the screen map.
- While the source page is unapproved, do not break down components.
- While no component sprite sheet exists, do not fabricate single-element PNGs or a ZIP.
- While the semantic mapping and 9-slice are unreviewed, do not approve the atlas or engine manifest.

## Initialization

1. Generate a stable `project-id` using lowercase letters, digits and hyphens.
2. If the project directory does not exist, run:

   ```bash
   python3 scripts/init_project.py <project-id>
   ```

3. Read `spec.md`, `research.md`, `plan.md`, `gdd.md`, `prd.md`, `interaction.md` and every file in `contracts/`.
4. Record user-provided reference image paths in `spec.md`, and analysis conclusions in `research.md`.
5. Ask only blocking questions that change genre, platform, aspect ratio or delivery scope; use explicitly labelled defaults for everything else.

## Stage order

When a full project starts, **the generation chain starts with Product Design** (step 1). `spec.md` / `research.md` may be backfilled with the minimum set by product design in the same step, or completed immediately afterwards.

### 1. Product Design (first generation step)

Invoke `ab-game-ui-product-design`:

1. Write `gdd.md`: game positioning, core loop, system inventory, economy and progression (design level, no server-side formulas)
2. Write `prd.md`: P0/P1 requirements, information architecture, page delivery scope, acceptance criteria
3. Write `interaction.md`: global navigation, per-page main flows, control behaviour, state machines, cross-page links
4. If `spec.md` is still empty, also write the project positioning, platform, ratio and scope summary

Only after the `status` of all three documents is `approved` may you enter prototype image generation and later steps. When the user explicitly skips, record the reason in `quickstart.md`.

Gate: never pass off an empty template or a title-only edit as an approved planning document; at the end of this stage you must stop and wait for approval.

### 2. Specify

Complete `spec.md` (if step 1 did not finish it):

- Project positioning, core gameplay, target platform, screen ratio and language
- User stories, scope, first page and deliverables
- Verifiable visual, functional and copyright acceptance criteria

Gate: do not freeze the contract while blocking open items remain.

### 3. Research

Abstract reference images into reusable language; do not copy protected characters, trademarks or complete compositions:

- Color, material, light source, corner radius, outline, density
- Navigation, cards, buttons, icons and feedback patterns
- Player journey, core loop, platform constraints and risks

### 4. Specification

Invoke `ab-game-ui-specification` and write `contracts/style-contract.yaml`. Before changing the status from `draft` to `approved`, confirm that color, typography, geometry, effects and consistency rules are all executable.

Gate: page skills must not silently introduce a primary color, material, light direction or corner-radius system outside the contract.

### 5. Extension

Invoke `ab-game-ui-extension`:

- Use `gdd.md` / `prd.md` / `interaction.md` as input
- Build a player-journey-ordered screen route in `plan.md`
- Record page purpose, entry points, primary actions, components, states, edge cases and data in `contracts/screen-contract.yaml`
- Select only pages that serve gameplay and the business model; label them `must-have`, `genre-specific` or `optional`

### 6. Page generation

Invoke `ab-game-ui-page-generator`, one page at a time:

1. Confirm the first generation step (the three planning documents) is approved (or a skip is recorded).
2. Read the style and screen contracts and that page's main flow/states in `interaction.md`.
3. Save the page brief and final prompt to `prompts/pages/<screen-id>.md`.
4. When an image tool is available and the owner has approved that specific use, actually generate the image and save it to `assets/pages/<screen-id>.<ext>`.
5. Update `asset-manifest.yaml`; wait for user approval or an explicit acceptance rule.

Do not invent images because a tool is unavailable. In that case use asset status `pending-generation` and keep the prompt ready to run.

### 7. Component breakdown

Invoke `ab-game-ui-component-breakdown` only for pages with `approved: true`:

- Update `component-contract.yaml` first
- One prompt, one file and one manifest entry per component
- Transparent by default except backgrounds; no text by default
- Component states, dimensions, naming and source page are traceable

### 8. Sprite-sheet splitting

When component breakdown outputs a single sprite sheet, invoke `ab-game-ui-sprite-sheet-splitter`:

1. Read the actual sprite sheet and the component contract.
2. Check for and remove titles, button copy, number labels, watermarks and text ghosts.
3. Detect separate elements via alpha or solid background.
4. Export single-element transparent PNGs.
5. Generate a manifest of coordinates, dimensions and paths.
6. Package the PNGs and manifest into a ZIP.
7. Manually inspect text, clipping, merging, shadows and transparent edges.

When automatic detection is unreliable, adjust thresholds or regenerate a sprite sheet with spacing; do not mark wrong slices as approved.

### 9. Atlas and engine handoff

Invoke `ab-game-ui-asset-pipeline` when a development handoff is needed:

1. Map detection indices to stable semantic names.
2. Register each component as `full`, `1:1`, `tile` or a manually reviewed `9-slice`.
3. Generate a non-rotated atlas PNG and JSON.
4. Generate Godot, Unity, Cocos or generic JSON manifests.
5. Clearly distinguish JSON handoff from native engine project files.

### 10. Validate

Run:

```bash
python3 scripts/validate_project.py specs/<project-id>
```

Fix every error. Warnings must be explained or resolved in `quickstart.md`.

## Visual generation rules

Image generation is off by default; run it only with explicit owner approval for that specific use.

When an image generation tool is available:

1. Page images use the contract's screen ratio.
2. If the tool does not support exact dimensions, choose the closest ratio and record the actual dimensions in the manifest.
3. When CJK text generation is unreliable, prefer generating a text-free base and note the later typesetting areas; never claim garbled text is directly deliverable.
4. Generate components one at a time; never paste multiple components into one image.
5. Move or copy tool-returned files into the project asset directory, then update the real paths.

## Change control

- When the user changes visual direction, update `spec.md` and `style-contract.yaml` first, then mark affected pages and components as `stale`.
- When page structure changes, update `screen-contract.yaml` before generating images.
- While a page is unapproved, do not start component breakdown.
- Do not overwrite approved assets; generate a new version and keep the source relationship in the manifest.

## Definition of done

- The six types of Spec-Kit artifacts are complete, including approved (or documented-skip) `gdd.md` / `prd.md` / `interaction.md`
- IDs and paths agree across the four core contracts; add the split contract when sprite sheets are used
- Page and component images exist, or are explicitly marked pending generation
- Visuals and components both have reproducible prompts
- When sprite sheets are used, single-element PNGs, split manifest and ZIP package are traceable
- When a development handoff is used, atlas regions, 9-slice and engine JSON agree
- `tasks.md` reflects the real state
- The validation script reports no errors
