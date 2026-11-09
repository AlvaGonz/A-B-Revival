---
name: ab-game-ui-product-design
description: Writes and maintains game design docs (GDD), product requirements (PRD) and UI interaction logic before any page image generation. Use when users ask for a game design document, PRD, product requirements, UI interaction logic, interaction flows or state machines, or say to finish planning before generating UI visuals.
---

> **Project constraint (A&B Revival).** Original Flash art and fonts are the baseline (ADR 0020); layout, UX and art improvements need explicit owner approval recorded in the issue/PR. This project renders UI with Unity UI Toolkit (UXML/USS, PanelSettings). Imported workflow steps are advisory: the repository workflow (wayfinder -> to-spec -> to-tickets -> TDD -> review) prevails.
>
> **Repository artifacts.** Specs and tickets live in GitHub Issues (`docs/agents/issue-tracker.md`). The `specs/`, `plan.md`, `contracts/` and `tasks.md` paths below are upstream notation; do not create them unless the owner explicitly asks for a local artifact. Upstream Python tools and templates are not vendored.

# Game Design · PRD · UI Interaction (first generation step)

**This skill is the first step of the full UI generation pipeline.** Before invoking page image generation, style switching or bulk extension, complete this step and ask the user to approve the three documents. Without approval (and without an explicit skip by the user), do not start any later visual generation.

## Output files

Write to `specs/<project-id>/`:

| File | Name | Purpose |
|---|---|---|
| `gdd.md` | Game design document | Positioning, core loop, system inventory, economy and progression (design level) |
| `prd.md` | Product requirements document | P0/P1 requirements, information architecture, delivery scope, acceptance |
| `interaction.md` | UI interaction logic | Navigation, main flows, control behaviour, state machines, cross-page links |

Template source: the repository's `templates/spec-kit/gdd.md`, `prd.md`, `interaction.md`. If the project is already initialized, edit the existing files directly; do not start parallel documents.

## When to use

- The **first step** of any full generation pipeline (default entry)
- Creating a UI project with only a concept and no visuals yet
- The user asks to "add planning / PRD / interaction before generating"
- The three documents are missing, `draft`, or hollow before extension or image generation
- Gameplay or page scope changes and the requirements and interaction need updating

## Inputs

Prefer reading:

- `spec.md`, `research.md`, `plan.md`
- Existing `gdd.md` / `prd.md` / `interaction.md`
- If present: `contracts/screen-contract.yaml`

When information is insufficient: ask only blocking questions about genre, platform, core gameplay and first-release page scope; give adjustable defaults for the rest and label them.

## Workflow

Copy and track:

```text
Progress:
- [ ] 1. Confirm or initialize the project directory
- [ ] 2. Write/update gdd.md
- [ ] 3. Write/update prd.md
- [ ] 4. Write/update interaction.md
- [ ] 5. Cross-check the three documents for consistency
- [ ] 6. Stop at the human approval gate
```

### 1. Project directory

If `specs/<project-id>/` does not exist:

```bash
python3 scripts/init_project.py <project-id>
```

### 2. `gdd.md` (game design document)

Must cover:

- One-line positioning, target users, selling points (≤3)
- Core loop table (return → preparation → gameplay → growth → monetization)
- System inventory and priority (`must-have` / `genre-specific` / `optional`)
- Resources and economy (design-level overview; never pretend server formulas already exist)
- Progression and growth axes, minimum world-setting set, risk constraints

Keep the document-status `status` field as `draft` first; change it to `approved` after the user approves.

### 3. `prd.md` (product requirements)

Must cover:

- Product goals and verifiable success metrics
- Non-goals (explicitly out of scope)
- P0 / P1 / P2 requirement table (each row maps to a page or system)
- Information architecture and the page delivery scope of this iteration
- Data dependencies, boundary exceptions, acceptance checklist

Every P0 must be carried by a later `screen-id`.

### 4. `interaction.md` (UI interaction logic)

Must cover:

- Global interaction principles, global navigation, global states
- Every must-have page: purpose, entry/exit, main flow, control table, state machine
- Cross-page links (at least those related to the core loop)
- "Constraints on the visual": information that must appear on the first screen, areas that must not be covered, primary-action emphasis

State names should match the later `screen-contract.yaml` `states` as closely as possible (e.g. `default` / `loading` / `incomplete` / `low-currency`).

### 5. Cross-check

- The screens in the `gdd` core loop ⊆ the `prd` page scope
- The `prd` P0 pages ⊆ the per-page chapters of `interaction`
- Commercial/destructive actions have a confirmation or failure path in `interaction`
- The three documents do not contradict each other; an empty template must not be marked approved

### 6. Human gate (must stop)

When finished, output:

```text
Current step: step 1 · game design and PRD / interaction logic (first generation step)
Completion status: draft, awaiting approval
Outputs of this step: gdd.md, prd.md, interaction.md
Needs your review: positioning and loop, P0 scope, per-page main flows and states
Optional actions: edit copy / add or remove systems / approve the three documents / explicitly skip the gate
Condition to enter the next step: all three documents have status approved, or quickstart.md records the skip reason
Next-step invocation text, ready to copy:
Approve @specs/<project-id>/gdd.md, prd.md, interaction.md.
After approval, go to step 2: use ab-game-ui-workflow to generate the baseline page prototype visual.
```

**Do not** invoke `ab-game-ui-page-generator` for production image generation before the user approves.
When the user says "skip the planning gate": state the reason in `quickstart.md`; the three documents may stay draft, but never pretend they are approved.

## Approval actions

After the user approves:

1. Change the `status` of all three documents to `approved`
2. Update the corresponding `[product]` items in `tasks.md`
3. Tick the quality gate in `plan.md`
4. Suggest the next step: `ab-game-ui-specification`, or (when the style is already approved) `ab-game-ui-extension`

## Relationship to other skills

```text
Step 1  ab-game-ui-product-design    ← first generation step (must come first)
Step 2+ ab-game-ui-specification / prototype image generation / extension / …
        ab-game-ui-page-generator     # requires the three step-1 documents to be approved
```

When the orchestration skill `ab-game-ui-workflow` starts a full generation run, **it must invoke this skill first as step 1**; it must not skip straight to image generation.

## Definition of done

- The three files exist in the project root with no `{{PROJECT_ID}}` placeholders
- Each has `status: draft` or `approved`
- The content can support screen extension and single-page image generation (not an empty template)
- You have stopped at the approval gate, or completed the approval/skip record as instructed by the user
