# 0025. Section Prefabs and UI Builder Authoring

**Date:** 2026-11-09
**Status:** Accepted (owner decision, #215)
**Related:** ADR 0014, ADR 0016, ADR 0020, ADR 0024, #190, #215

## Context

`Boot.unity` is the only built scene. It hosts 16 flat roots; 11 of them are screens with
`UIDocument` + controller. `BootSceneBuilder` rebuilds the scene from an empty scene and
reassigns every sprite/catalog reference, so manual edits are discarded. The owner wants to
open each game section, see its assets, move them, and keep the change.

The current UI is code-driven: UXML declares empty containers, `MainMenuController.FitPage`
positions the main-menu layers, and `UiSpriteSkin` applies sprites through an `Image` child
because UI Toolkit's `background-image` samples atlased sprites incorrectly. UI Builder
therefore shows empty boxes, not the real screen.

Unity 6.6 facts that constrain the choice:

- Prefabs enter a player build only through a built scene, another included prefab,
  `Resources`, or Addressables. Editor-only references do not count.
- Textures, audio and animations drive build size; prefab count is not a documented size
  driver.
- Direct references are the documented route for fixed content. `Resources` always ships its
  contents, and Addressables targets updatable content.
- `UIDocument` is obsolete in 6000.6 and cannot be added to new GameObjects from the editor.
  `PanelRenderer` supersedes it and preserves UI content when disabled.
- `MonoBehaviour`/`ScriptableObject` types used by prefabs survive managed stripping; plain C#
  types referenced only by serialized fields may not at Medium/High.

## Decision

1. **11 section prefabs.** MainMenu, Options, Center, Factory, Shop, Info, Ready, Results,
   Repair, Talk and Combat become prefabs under a `Screens` root in Boot. GameFlow, Flight,
   Cinematics, Audio and EventSystem stay in Boot.
2. **The prefab owns what its root owns today.** UI document + controller; Combat also camera,
   AudioListener, renderer and HUD; Talk its portrait catalog and talk data. No shared service
   is duplicated.
3. **`ScreenCatalog` ScriptableObject.** It holds direct prefab references. Boot instantiates
   all sections at startup; controllers keep hiding with `display` rather than `SetActive`, so
   `UIDocument` content is not cleared. No `Resources`, no Addressables.
4. **UI Builder is the authoring surface.** A custom `SpriteElement` with a serialized
   `Sprite` UXML attribute renders an `Image` child, so atlased art is visible and movable in
   UI Builder. `UiSpriteSkin` remains for hover/state changes until each screen is migrated.
5. **Hybrid layout for the MainMenu pilot.** The title, backdrop and buttons are positioned in
   USS/UXML and the layout fields are removed from `MainMenuController`. `shipScale`,
   `bottomMargin`, `anchorBottomRow` and safe-area handling stay in C# (ADR 0024).
6. **Pilot scope: MainMenu only.** Success means: open `Main.uxml` in UI Builder, see the real
   art, move the title/backdrop/buttons, save, run Boot and see the change; PlayMode 5/5 and
   Tier 1 237/237.
7. **Retire `BootSceneBuilder` after one migration.** A one-time editor migration extracts the
   11 roots into prefabs, creates the `ScreenCatalog`, saves Boot, and the builder is removed.
   After that, the scene and prefabs are the source of truth.
8. **Use `PanelRenderer` from the pilot onward.** #216 migrates MainMenu; #217 migrates the
   remaining 10 screens and the section prefabs. A shared `ScreenPanel` wrapper owns the
   `PanelRenderer`, exposes the root through the 3-arg reload callback, and provides
   `SetVisible`/sorting order. `UIDocument` is removed from production by #217; #190 is
   superseded by this decision.

## Consequences

- Positive: the owner can edit each section manually; sections are isolated; the PlayMode
  contract (`LoadScene("Boot")` + find by type) survives because all prefabs are instantiated
  at startup.
- Positive: build impact is negligible; prefabs are included by the catalog reference and
  their referenced art.
- Negative: one-time migration cost; 11 prefabs to maintain; `SpriteElement` and USS layout
  migration; Talk has no UXML and needs one during rollout.
- Negative: `PanelRenderer` has no `rootVisualElement`; every controller and test must obtain
  the root through `ScreenPanel` and the 3-arg reload callback. Prefab support is undocumented
  and must be validated in #216/#217.
- Constraint: do not re-run `BootSceneBuilder` after migration; its deletion is the guard.
- Constraint: use `display` toggling, not `SetActive` or component disable, while `PanelRenderer`
  is in use. Disabling the component removes its visual elements.
- Constraint: `PanelRenderer` has no `sourceAsset`; use `visualTreeAsset`, the 3-arg
  `VersionedUIReloadCallback`, and `Renderer.sortingOrder` for per-renderer ordering.
- Constraint: Talk has no UXML; give it a blank `Talk.uxml` so `PanelRenderer` has a stable root
  to build into.
- Constraint: section siblings need unique `sortingOrder` values and share
  `HangarPanelSettings` for focus navigation.
- Constraint: avoid `Resources` and Addressables for section prefabs; add `[Preserve]` or
  `link.xml` if a plain C# type is only referenced by serialized fields.

## Enforcement

- #215 records the grilling; implementation tickets carry the pilot, migration and rollout.
- `ScreenCatalog` gets a validation test: 11 entries, no nulls, no duplicates.
- ADR 0024 still governs the main-menu composition; ADR 0020 still governs art fidelity.
- A section prefab is not accepted if its UI is invisible in UI Builder or if manual layout
  edits are overwritten at runtime.
- The PanelRenderer migration is complete when `rg "UIDocument|rootVisualElement" A&B-U/Assets/Scripts`
  returns no production references (tests may use `ScreenPanel.Root`).
