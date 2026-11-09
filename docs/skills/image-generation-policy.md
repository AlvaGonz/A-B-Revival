# Image generation policy

Applies to every vendored skill that can generate or edit art (`ab-create-game-assets`, `ab-game-ui-page-generator`, `ab-game-ui-component-breakdown`, and any future generator).

## Gate

- Generation is **off by default**.
- Every use needs the owner's explicit approval in the issue or PR that requests it: what will be generated, for which asset, and why the original art cannot serve.
- Approval for one asset does not approve the next one.

## Repository hygiene

- Never commit an account name, key, token, subscription ID, billing detail or signed URL.
- Do not add generation tooling as a project dependency without a ticket.
- Generated files are not committed unless the owner approves that specific asset.
- The repository may record only the fact that "an external image-generation account is required"; nothing about the account itself.

## Relationship to ADR 0020

- Original Flash art, fonts and audio remain the baseline.
- An approved improvement records what changed, why, and which original asset it replaces or adjusts.
- Generated art must follow the original palette, proportions and UI rules; it cannot silently modernise the 2011 look.
- Fonts remain the eight original families; a generator may not introduce a new typeface.

## Provenance

Every approved generated asset records, next to the asset manifest:

- the prompt or brief,
- the tool used (generic name only),
- the date,
- the owner approval reference (issue/PR),
- the edits applied after generation.
