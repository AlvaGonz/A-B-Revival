---
name: ab-game-ui-extension
description: Extends a game concept or partial interface into a coherent screen system, player journey, feature flow, prioritized screen inventory, states, edge cases and implementation-ready screen contracts. Use when users ask to expand an existing game UI, add related pages, audit missing screens, or plan a complete UI ecosystem.
---

> **Project constraint (A&B Revival).** Original Flash art and fonts are the baseline (ADR 0020); layout, UX and art improvements need explicit owner approval recorded in the issue/PR. This project renders UI with Unity UI Toolkit (UXML/USS, PanelSettings). Imported workflow steps are advisory: the repository workflow (wayfinder -> to-spec -> to-tickets -> TDD -> review) prevails.
>
> **Repository artifacts.** Specs and tickets live in GitHub Issues (`docs/agents/issue-tracker.md`). The `specs/`, `plan.md`, `contracts/` and `tasks.md` paths below are upstream notation; do not create them unless the owner explicitly asks for a local artifact. Upstream Python tools and templates are not vendored.

# Game UI Extension

Choose the necessary pages from gameplay and the player journey; do not default to stuffing in every common system.

## Inputs

Read `gdd.md`, `prd.md`, `interaction.md`, `spec.md`, `research.md`, `contracts/style-contract.yaml`, `plan.md` and `contracts/screen-contract.yaml`. If the user gives only one image, first judge where it sits in the core loop. When the three planning documents are missing, invoke `ab-game-ui-product-design` first, then extend the screens.

For a full taxonomy audit read [references/screen-taxonomy.md](references/screen-taxonomy.md); an ordinary extension does not need the full taxonomy loaded.

## Player journey

Check by applicability:

1. Launch and first entry: splash, login, announcement, server select, character creation, tutorial.
2. Core gameplay: home, quest, preparation, stage, battle HUD, pause, settlement.
3. Growth management: character, equipment, skills, ascension, inventory, codex, achievements.
4. Economy and monetization: shop, gacha, battle pass, subscription, exchange, ad rewards.
5. Live ops and return: sign-in, daily, event hub, limited-time challenge, returner.
6. Social and competition: friends, guild, chat, mail, profile, leaderboard, season.
7. Strategic expression: formation, deck, presets, talents, appearance, housing.
8. Support and settings: settings, controls, accessibility, account, customer support, redeem codes.

Select only pages that serve the genre, platform, core loop, retention and business model.

## Screen priority

- `must-have`: without it the core loop cannot be completed or delivered safely.
- `genre-specific`: determined by gameplay, e.g. card formation or shooter loadout.
- `optional`: improves retention, expression or operations but does not block the first release.

## Contract per page

Write to `screen-contract.yaml`:

- `id`: stable lowercase hyphenated ID
- `purpose`: the player problem it solves
- `entry_points`: where the player enters from
- `primary_actions`: the 1–3 most important actions
- `dependencies`: prerequisite pages or systems
- `layout`: top, middle, bottom and floating regions
- `components`: reusable and page-specific components
- `states`: default, loading, empty, locked, claimable, cooldown, error, etc.
- `edge_cases`: inventory full, event expired, network loss, insufficient resources, etc.
- `data_needs`: currency, timers, level, inventory, social state, etc.

## Extension steps

1. Write the core loop and the path from first entry to daily return.
2. Place the user's existing pages into the journey and find the real breakpoints.
3. Select pages and label their priority; merge entries with duplicate functions.
4. Complete the contract for each page, ensuring the primary action is identifiable within three seconds.
5. Order `plan.md` by dependency and value, not by page name.
6. Plan page-generation batches: baseline page first, then high-reuse system pages, then live-ops pages.

## Design constraints

- The home screen keeps only a limited set of high-priority entries; secondary events go into the event hub.
- High-frequency actions support one-tap claim, batch operations, filters, presets or jumping to the source.
- Shop, gacha and leaderboards clearly show price, probability, settlement time and rules.
- The HUD does not cover the core gameplay area and adapts to the input method.
- Every page follows the style contract; extension changes functional emphasis, not the visual system.

## Output

- Screen route, generation batches and dependencies in `plan.md`
- A complete `screen-contract.yaml`
- Tasks tagged `[extension]` and `[page]` in `tasks.md`
- A summary of gaps, merge rationale and risks

## Checks

- The path from first entry to repeated return is closed
- Settlement can return to growth or the next match
- No page is an island without entry or exit
- Key systems include empty, locked, error and insufficient-resource states
- Page count matches the project stage
