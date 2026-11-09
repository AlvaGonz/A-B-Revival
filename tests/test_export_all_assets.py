#!/usr/bin/env python3
"""Unit tests for scripts/export_all_assets.py and its committed manifest."""

import json
import sys
import unittest
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent / "scripts"))
import export_all_assets as pipeline  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent


class TestMarkerCleaning(unittest.TestCase):
    def test_removes_only_upper_case_marker_placements(self):
        xml = (
            '<item type="PlaceObject2Tag" name="HIT"/>'
            '<item type="PlaceObject3Tag" name="H_2_234"><matrix/></item>'
            '<item type="PlaceObject2Tag" name="F"><matrix/></item>'
            '<item type="PlaceObject2Tag" name="face"><matrix/></item>'
            '<item type="PlaceObject4Tag" name="P"><matrix/></item>'
        )
        cleaned, removed = pipeline.strip_marker_placements(xml)
        self.assertEqual(removed, 3)
        self.assertIn('name="HIT"', cleaned)
        self.assertIn('name="face"', cleaned)
        self.assertNotIn('name="F"', cleaned)
        self.assertNotIn('name="P"', cleaned)
        self.assertNotIn("H_2_234", cleaned)
        self.assertEqual(pipeline.remaining_marker_names(cleaned), [])


class TestPivotMapping(unittest.TestCase):
    def test_content_pivot_is_remapped_when_alpha_bbox_matches_native(self):
        x, y, source = pipeline.pivot_from_bbox(
            100, 100, (10, 10, 90, 90), 80.0, 80.0, [0.5, 0.5]
        )
        self.assertAlmostEqual(x, 0.5)
        self.assertAlmostEqual(y, 0.5)
        self.assertEqual(source, "alpha-bbox")

    def test_wrapper_with_invisible_rect_keeps_manifest_pivot(self):
        x, y, source = pipeline.pivot_from_bbox(
            38, 36, (9, 11, 31, 25), 37.18, 35.89, [0.5, 0.5]
        )
        self.assertEqual((x, y), (0.5, 0.5))
        self.assertEqual(source, "manifest")

    def test_adjust_false_keeps_manifest_pivot(self):
        x, y, source = pipeline.pivot_from_bbox(
            100, 100, (10, 10, 90, 90), 80.0, 80.0, [0.25, 0.75], adjust=False
        )
        self.assertEqual((x, y), (0.25, 0.75))
        self.assertEqual(source, "manifest")

    def test_ccw_pivot_rotation(self):
        self.assertEqual(pipeline.rotate_pivot_ccw((0.3, 0.5)), (0.5, 0.3))


class TestNineSliceAndStates(unittest.TestCase):
    def test_standard_button_family(self):
        row = {"symbolId": 447, "frameCount": 3, "nativeWidth": 153.7, "nativeHeight": 42.2}
        self.assertEqual(pipeline.derive_nine_slice(row, {}), ([10, 10, 10, 10], "derived"))

    def test_small_button_family(self):
        row = {"symbolId": 743, "frameCount": 2, "nativeWidth": 41.0, "nativeHeight": 19.0}
        self.assertEqual(pipeline.derive_nine_slice(row, {}), ([5, 5, 5, 5], "derived"))

    def test_static_widget_has_no_slicing(self):
        row = {"symbolId": 61, "frameCount": 1, "nativeWidth": 48.4, "nativeHeight": 51.0}
        self.assertEqual(pipeline.derive_nine_slice(row, {}), ([0, 0, 0, 0], "none"))

    def test_widget_override_wins(self):
        row = {"symbolId": 73, "frameCount": 3, "nativeWidth": 24.5, "nativeHeight": 24.3}
        self.assertEqual(pipeline.derive_nine_slice(row, {73: [0, 0, 0, 0]}), ([0, 0, 0, 0], "override"))

    def test_state_metadata_names_all_three_frames(self):
        row = {"frameCount": 3}
        self.assertEqual(
            pipeline.state_metadata(row)["exportMapping"],
            {"normal": 1, "hover": 2, "pressed": 3},
        )
        self.assertEqual(
            pipeline.state_paths(Path("x.png"), 2),
            [
                ("normal", Path("x_normal.png"), 1),
                ("pressed", Path("x_pressed.png"), 2),
            ],
        )


class TestPathHelpers(unittest.TestCase):
    def test_library_suffix_match(self):
        self.assertTrue(pipeline.is_library("9_all.AllData_ui.bin", pipeline.UI_LIB_SUFFIX))
        self.assertFalse(pipeline.is_library("4_all.AllData_data.bin", pipeline.UI_LIB_SUFFIX))

    def test_project_prefix(self):
        self.assertEqual(
            pipeline.to_project("A&B-U/Assets/Art/Fonts/font.ttf"),
            "Assets/Art/Fonts/font.ttf",
        )
        self.assertEqual(pipeline.to_project("docs/x.json"), "docs/x.json")


class TestCommittedManifest(unittest.TestCase):
    def test_counts_and_known_ambiguities(self):
        manifest = json.loads((ROOT / "docs/assets/extraction-manifest.json").read_text(encoding="utf-8"))
        counts = manifest["counts"]
        self.assertEqual(counts["manifestSymbols"], 1795)
        self.assertEqual(counts["standaloneRenderables"], 742)
        self.assertEqual(counts["previouslyExtracted"], 322)
        self.assertEqual(counts["newlyExtracted"], 420)
        self.assertEqual(counts["extractionRecords"], 742)
        self.assertEqual(counts["fonts"], 8)
        self.assertEqual(counts["textTags"], 421)
        self.assertEqual(counts["referencePages"], 32)
        self.assertEqual(len(manifest["fonts"]), 8)
        self.assertEqual(len(manifest["referencePages"]), 32)
        self.assertEqual(len({(r["library"], r["symbolId"]) for r in manifest["records"]}), 742)
        self.assertEqual(
            manifest["inventory"]["statusCounts"],
            {"composite-only": 862, "extracted": 322, "unavailable": 611},
        )

        text = json.loads((ROOT / "docs/assets/text/string-table.json").read_text(encoding="utf-8"))
        self.assertEqual(text["counts"]["uiTags"], 390)
        self.assertEqual(text["counts"]["dataTags"], 31)
        self.assertEqual(text["counts"]["uiUniqueNonEmpty"], 219)
        self.assertEqual(len(text["uniqueUiStrings"]), 219)
        self.assertEqual(text["counts"]["placeholderTags"], 4)

        ids = {item["id"] for item in manifest["ambiguities"]}
        for required in (
            "shared-shape-95-154",
            "ui-sounds-unassigned",
            "bitmap-backdrops-unassigned",
            "morph-shapes",
            "button-state-mapping",
        ):
            self.assertIn(required, ids)

    def test_unity_import_paths_are_project_relative(self):
        manifest = json.loads((ROOT / "docs/assets/extraction-manifest.json").read_text(encoding="utf-8"))
        for sprite in manifest["unityImport"]["sprites"]:
            self.assertTrue(sprite["path"].startswith("Assets/"), sprite["path"])
            self.assertTrue((ROOT / "A&B-U" / sprite["path"]).is_file(), sprite["path"])
        for atlas in manifest["unityImport"]["atlases"]:
            for asset in atlas["assets"]:
                self.assertTrue(asset.startswith("Assets/"), asset)


if __name__ == "__main__":
    unittest.main()
