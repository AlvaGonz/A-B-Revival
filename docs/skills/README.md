# Vendored skills policy

Project-local skill acquisition for OpenCode and Antigravity. Nothing is installed globally.

## Location

- Canonical tree: `.agents/skills/` (flat, `ab-` prefix, committed).
- Both harnesses read it natively; see `.agents/skills/README.md`. OpenCode ranks user-level skills above `.agents/skills/`, so the `ab-` prefix also prevents shadowing. Antigravity's scope ordering is undocumented.
- OpenCode agents: `.opencode/agents/ab-*.md`. Antigravity agents: `.agents/agents/ab-*.md`. Agent files are per harness because their frontmatter differs.
- Verified 2026-11-09 against each vendor's docs: OpenCode uses `description`/`mode`/`permissions` (no `name`); Antigravity uses `name`/`description`/`tools`/`subagent`/`mainAgent`.
- No skill, agent or config goes to a user-level directory.

## Adoption rules

- Imported skills are **additions**. The repository workflow (wayfinder -> to-spec -> to-tickets -> TDD -> review) always prevails.
- Each skill is vendored at a pinned commit; never track a moving branch.
- No credential, account, subscription or key is committed. Generation skills run only with owner approval; see `docs/skills/image-generation-policy.md`.
- Original art and fonts remain the baseline (ADR 0020); improvements need explicit owner approval in the issue/PR.
- Bundled scripts are reviewed before copying. Prefer Markdown-only skills.

## Lockfile schema

```json
{
  "schemaVersion": 1,
  "skills": [
    {
      "id": "ab-demo",
      "sourceRepo": "https://github.com/owner/repo",
      "sourcePath": "skills/demo",
      "commit": "<40-character hex SHA>",
      "license": "MIT",
      "localPath": ".agents/skills/ab-demo",
      "modifications": "none",
      "adoptedAt": "2026-11-09"
    }
  ]
}
```

`id`, `sourceRepo`, `sourcePath`, `commit`, `license` and `localPath` are required. `modifications` and `adoptedAt` are informational. `commit` must be a pinned hex SHA, not `HEAD` or a branch name.

## Routing

`AGENTS.md` carries the task -> skill/agent table under **Project-local skills**. Routing verified: 2026-11-09. Update the table in the same PR that adds, removes or renames a skill; no automatic check, because matching prose against the lockfile is brittle. The quarterly update policy below is the reminder.

## Update policy

- Quarterly, or when an upstream release matters: diff the vendored skill against its pinned upstream commit, open a ticket, and apply the change with a fresh lockfile entry.
- Never auto-update on agent startup or commit.
- Removal: delete the folder, the lockfile entry and the attribution line in one commit.

## Validation

`python scripts/validate_vendored_skills.py` checks:

1. Every `.agents/skills/ab-*/` has `SKILL.md` with a matching `name`.
2. `UPSTREAM.md` records source, path, commit and license.
3. `LICENSE` exists; `NOTICE` exists when `Notice: required`.
4. Each folder appears exactly once in `docs/skills/vendored-skills.lock.json`, with the expected `localPath`.
5. No stale lock entries and no folder outside the `ab-` prefix.

The pre-commit hook runs it whenever `.agents/skills/` or the lockfile is staged. Self-checks: `python scripts/test_validate_vendored_skills.py`.

## Current state

Vendored: 21 skills across gameplay, UI, art, Unity runtime and testing, plus 3 agents in both harness formats. Phase 1 (#191-#195) and phase 2 (#198-#201). The lockfile is the source of truth for what is installed; `docs/skills/ATTRIBUTION.md` mirrors it for humans.
