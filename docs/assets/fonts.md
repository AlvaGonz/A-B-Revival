# Original Fonts (ADR 0020)

The port uses **only** the eight fonts embedded in the frozen Flash build. No system, open-substitute or Unity-default font is permitted anywhere in the game UI.

Canonical mapping and licensing notes live in
`docs/assets/reference/README.md#original-font-mapping-adr-0020`. This page is the quick allowlist.

| # | Asset (`A&B-U/Assets/Art/Fonts/`) | Family | SWF symbol | Frozen usage | Shared class |
|---|---|---|---|---|---:|
| 1 | `font_0007_SF_TransRobotics.ttf` | SF TransRobotics | 7 | Menu chrome, buttons, help pages, HUD labels | theme `:root` / `.font-ui` |
| 2 | `font_0081_Arial.ttf` | Arial | 81 | Center/Factory/Shop tables, stats, detail values | `.font-data` |
| 3 | `font_0002_sans.ttf` | `_sans` | 2 | Options/Data labels, Factory undo slots, page counters, Center instruction | `.font-sans` |
| 4 | `font_0478_sans.ttf` | `_sans` | 478 | Companion grade letters, HUD pilot name | `.font-grade` |
| 5 | `font_0627_SF_TransRobotics.ttf` | SF TransRobotics | 627 | `Mov1`–`Mov9` cinematic captions | `.font-cinematic` |
| 6 | `font_0701_SF_TransRobotics.ttf` | SF TransRobotics | 701 | `BGWord` credits | `.font-credits` |
| 7 | `font_0595_Times_New_Roman.ttf` | Times New Roman | 595 | "35" version plate — **only the `3` and `5` glyphs** | `.font-version` |
| 8 | `font_0496_SF_Zero_Gravity.ttf` | SF Zero Gravity | 496 | HUD `Q` logo mark — **only the `Q` glyph** | `.font-logo` |

## Rules

- Apply fonts only through the shared theme classes in `A&B-U/Assets/UI/UnityDefaultRuntimeTheme.tss`; a screen must not declare its own font URL.
- The two glyph-subset fonts are plate-only. If a surface needs arbitrary text, use the closest **original** family: Arial for the numeric version plate, SF TransRobotics for logo text.
- Licensing is accepted-risk under ADR 0020: Arial and Times New Roman are commercial subsets; SF TransRobotics/SF Zero Gravity have undocumented ShyFonts-era terms; `_sans` is an unidentified generic subset. Keep them for this non-commercial preservation build; revisit before any distribution-policy change.
- Do not "fix" text metrics by swapping fonts. Fix layout spacing instead.
