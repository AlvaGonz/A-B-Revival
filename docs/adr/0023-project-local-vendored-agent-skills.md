# 0023. Project-Local Vendored Agent Skills

**Date:** 2026-11-09
**Status:** Accepted
**Related:** ADR 0020, tickets #188, #189, #191-#195, #122

## Context

This project is worked on through OpenCode and Antigravity. Both harnesses can read project-local skills; neither needs a global install. Global skills drift per machine, are invisible to reviewers, and can shadow project behavior.

Three external collections were inventoried in #188: `awesome-gamedev-agent-skills`, `game-ui-design-workflow` and `Claude-Code-Game-Studios`. Most of their 160 skills target other engines, other workflows or generated art. Only a subset fits this Unity 6.6 project, and some of that subset conflicts with the repository workflow unless it is adapted.

Vendoring third-party Markdown also brings licensing, provenance and update obligations. A loose copy would be untraceable within a month.

## Decision

1. **One committed tree.** Vendored skills live in `.agents/skills/`, flat, one folder per skill, prefixed `ab-`. Both harnesses read this path; `.opencode/skills/` is not used as a second copy.
2. **Additive, never authoritative.** Imported skills never replace, reorder or override the repository workflow (wayfinder -> to-spec -> to-tickets -> TDD -> review). A conflicting instruction is adapted or dropped.
3. **Provenance per skill.** Every folder carries `SKILL.md`, `UPSTREAM.md` (source, path, pinned commit, license), `LICENSE`, and `NOTICE` when the upstream requires one. The lockfile and `ATTRIBUTION.md` mirror the same records.
4. **Agents are harness-specific.** OpenCode reads `.opencode/agents/`; Antigravity reads `.agents/agents/`. Different frontmatter, two files per agent, no shared copy.
5. **Image generation is owner-gated.** Generation is off by default and needs explicit owner approval per use. No credential, account or subscription detail enters the repository.
6. **Automated drift control.** `scripts/validate_vendored_skills.py` runs from pre-commit when the tree or lockfile changes and refuses partially staged changes. Upstream updates are diffed quarterly or when a release matters; nothing auto-updates.
7. **Rejected stays rejected.** Rejected skills remain in `docs/research/game-dev-skills-manifest.json` with a reason. Reopening one requires a ticket that names the need.

## Consequences

- Positive: reproducible across machines, reviewable in PRs, auditable licensing, no global pollution, one place to check before adding a skill.
- Negative: the repository grows with vendored Markdown and licenses; translations and adaptations need upkeep; the lockfile is a second record to maintain.
- Constraint: editing a vendored skill requires updating `UPSTREAM.md` and the lockfile entry in the same commit.
- Constraint: a new harness that does not read `.agents/skills/` needs its own documented bridge, not a silent duplicate.

## Enforcement

- `scripts/validate_vendored_skills.py` + `.githooks/pre-commit`.
- `docs/skills/README.md`, `.agents/skills/README.md`, `docs/skills/ATTRIBUTION.md`.
- The `AGENTS.md` project-local skills rule.
- `docs/skills/image-generation-policy.md` for generation.
