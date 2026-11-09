# 0020. Original Fonts and Asset Fidelity

**Date:** 2026-11-08
**Status:** Accepted (owner decision)
**Related:** ADR 0013, ADR 0015, ADR 0019, research #173

## Context

The exhaustive asset inventory (#173) found that the 2011 build embeds subsets of Arial, Times New Roman and several SF faces. The Arial/Times subsets are commercial fonts, and the SF licensing is unclear. A metric-compatible open-font replacement was proposed.

The owner rejected replacement: the project is a non-commercial preservation and modding effort, and the UI must match the frozen release. Substituting fonts would change glyph widths and break the 1:1 layout target.

## Decision

1. **Use the original embedded fonts.** Extract them from the frozen SWF and embed them exactly as the baseline does.
2. **Do not substitute** open fonts, system fonts, or Unity defaults anywhere in the game UI.
3. **Document the source**: each font asset records its SWF symbol, family and usage. The extraction manifest is the source of truth.
4. **Record the licensing risk** here instead of silently shipping a substitute. This is a non-commercial fan preservation build; if distribution policy changes, this ADR must be revisited before any public release.
5. **Original art has the same precedence**: frames, icons, portraits, ships, effects and audio are reproduced from the frozen assets, not redesigned. Where an asset does not exist (procedural bullets/VFX), reimplement the behaviour with the original palette and rules rather than inventing art.

## Consequences

- Positive: glyph metrics, layout and visual fidelity match the 2011 release.
- Negative: commercial font subsets are redistributed with the project; legal risk remains until a distribution decision changes it.
- Constraint: any agent touching UI must use the extracted fonts. Do not "fix" text metrics by swapping fonts.

## Enforcement

- `AGENTS.md` carries the rule as a durable agent instruction.
- The extraction ticket must commit the original font assets and their manifest entries.
- A UI section is not accepted if it renders with a substitute font.
