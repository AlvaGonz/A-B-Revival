# Project-local skills

Vendored skills live here and are committed with this repository. Do not install them globally.

## Layout

- Flat folders only: `.agents/skills/<skill-id>/`.
- Every vendored skill uses the `ab-` prefix, for example `ab-game-ui-ux`.
- `SKILL.md` frontmatter `name` must equal the folder name.
- Each skill carries `UPSTREAM.md`, `LICENSE`, and `NOTICE` when its `UPSTREAM.md` says `Notice: required`.
- This `README.md` is the only file at the tree root.

Both harnesses read this path: OpenCode documents `.agents/skills/` as a compatibility directory, and Antigravity defaults to it. Global user-level skills can shadow a project skill in OpenCode, so the `ab-` prefix keeps vendored IDs unique. Confirm with a smoke check when the first skill lands; if a harness rejects the path, record the fallback in `docs/skills/README.md` instead of keeping a second copy.

## Adding a skill

1. Copy the upstream skill into a new `ab-*` folder.
2. Set `SKILL.md` `name` to the folder name; keep the description meaning intact.
3. Fill `UPSTREAM.md` from `docs/skills/UPSTREAM.template.md` and add the upstream license text.
4. Add one lockfile entry in `docs/skills/vendored-skills.lock.json`.
5. Add one line to `docs/skills/ATTRIBUTION.md`.
6. Run `python scripts/validate_vendored_skills.py`.

## Precedence

Vendored skills are additions. This repository's workflow (wayfinder -> to-spec -> to-tickets -> TDD -> review) prevails; an imported workflow never replaces, reorders or overrides it. If a skill conflicts with an ADR or `CODING_STANDARDS.md`, adapt or drop the conflicting section and record it in `UPSTREAM.md`.
