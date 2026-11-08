using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ABRevival.Core.Data;

namespace ABRevival.Tests
{
    [TestFixture]
    public class UiSpriteManifestTests
    {
        private static readonly string[] RequiredLogicalNames =
        {
            // Hangar frame, header, panels, table and shop rows.
            "hangar_frame",
            "hangar_header",
            "hangar_panel",
            "hangar_stats_card",
            "hangar_table_header",
            "hangar_table_row_even",
            "hangar_table_row_odd",
            "hangar_shop_row_even",
            "hangar_shop_row_odd",

            // Required buttons and category filters.
            "hangar_button_merge",
            "hangar_button_new",
            "hangar_button_back_to_center",
            "hangar_button_parts_crafts",
            "hangar_button_launch",
            "hangar_category_all",
            "hangar_category_body",
            "hangar_category_wing",
            "hangar_category_engine",
            "hangar_category_gun",

            // Part shop thumbnails.
            "hangar_shop_thumbnail_body",
            "hangar_shop_thumbnail_wing",
            "hangar_shop_thumbnail_engine",
            "hangar_shop_thumbnail_gun",
            "hangar_shop_thumbnail_module",

            // HUD frame, buttons, radar, minimap and portraits.
            "hud_frame",
            "hud_money_panel",
            "hud_button_pause",
            "hud_button_help",
            "hud_radar_ring",
            "hud_minimap_frame",
            "hud_pilot_portrait_frame",
            "hud_captain_portrait_frame",

            // HUD bars, reticle and badges.
            "hud_health_bar",
            "hud_energy_bar",
            "hud_shield_bar",
            "hud_bar_frame",
            "hud_reticle",
            "hud_mode_badge",
            "hud_camera_badge",
            "hud_damage_telemetry_row"
        };

        private static string FindRepoRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null &&
                   !Directory.Exists(Path.Combine(directory.FullName, ".git")) &&
                   !File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? @"C:\Dev\A&B revival";
        }

        private sealed class ManifestRow
        {
            public int index;
            public string logicalName;
            public int symbolId;
            public string symbolClass;
            public string kind;
            public int frame;
            public string file;
            public string rect;
            public string note;
        }

        private static List<ManifestRow> ReadManifest()
        {
            string path = Path.Combine(FindRepoRoot(), UiSpriteCatalog.ManifestFile);
            Assert.IsTrue(File.Exists(path), "UI sprite manifest is missing: " + path);

            var rows = new List<ManifestRow>();
            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = rawLine.Trim();
                if (!line.StartsWith("|", StringComparison.Ordinal)) continue;

                string[] cells = line.Split('|').Select(cell => cell.Trim()).ToArray();
                if (cells.Length < 11) continue;
                if (cells[1] == "#" || cells[1].StartsWith("---", StringComparison.Ordinal)) continue;

                rows.Add(new ManifestRow
                {
                    index = int.Parse(cells[1]),
                    logicalName = cells[2].Trim('`'),
                    symbolId = int.Parse(cells[3]),
                    symbolClass = cells[4],
                    kind = cells[5],
                    frame = int.Parse(cells[6]),
                    file = cells[7].Trim('`'),
                    rect = cells[8],
                    note = cells[9]
                });
            }

            Assert.Greater(rows.Count, 0, "UI sprite manifest has no symbol rows.");
            return rows;
        }

        [Test]
        public void Manifest_CoversEveryRequiredUiElement()
        {
            List<ManifestRow> rows = ReadManifest();
            var names = new HashSet<string>(rows.Select(row => row.logicalName), StringComparer.Ordinal);
            var catalogRequired = new HashSet<string>(
                UiSpriteCatalog.Sprites.Where(spec => spec.required).Select(spec => spec.logicalName),
                StringComparer.Ordinal);

            foreach (string required in RequiredLogicalNames)
            {
                Assert.IsTrue(catalogRequired.Contains(required), $"Catalog does not mark '{required}' as required.");
                Assert.IsTrue(names.Contains(required), $"Required UI element '{required}' is missing from the manifest.");
            }
        }

        [Test]
        public void Manifest_IsDeterministicAndMatchesTheCatalog()
        {
            List<ManifestRow> rows = ReadManifest();
            Assert.AreEqual(UiSpriteCatalog.Sprites.Count, rows.Count);

            var names = new HashSet<string>(StringComparer.Ordinal);
            var files = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < rows.Count; i++)
            {
                ManifestRow row = rows[i];
                UiSpriteSpec spec = UiSpriteCatalog.Sprites[i];

                Assert.AreEqual(i + 1, row.index, "Manifest indexes must be contiguous and ordered.");
                Assert.AreEqual(spec.logicalName, row.logicalName, "Manifest order must follow the catalog order.");
                Assert.AreEqual(spec.symbolId, row.symbolId);
                Assert.AreEqual(spec.frame, row.frame);
                Assert.AreEqual(UiSpriteCatalog.FileName(spec), row.file);
                Assert.IsTrue(names.Add(row.logicalName), "Duplicate logical name: " + row.logicalName);
                Assert.IsTrue(files.Add(row.file), "Duplicate PNG path: " + row.file);
            }

            Assert.AreEqual(
                UiSpriteCatalog.Sprites.Select(spec => spec.logicalName).OrderBy(name => name, StringComparer.Ordinal),
                UiSpriteCatalog.Sprites.Select(spec => spec.logicalName));
        }

        [Test]
        public void Manifest_PngFilesExistAndUseTheWorldImportStandard()
        {
            string repoRoot = FindRepoRoot();
            foreach (ManifestRow row in ReadManifest())
            {
                string pngPath = Path.Combine(repoRoot, row.file.Replace('/', Path.DirectorySeparatorChar));
                Assert.IsTrue(File.Exists(pngPath), $"Manifest PNG is missing: {row.file}");
                Assert.Greater(new FileInfo(pngPath).Length, 0, "Manifest PNG is empty: " + row.file);

                if (!row.file.StartsWith(UiSpriteCatalog.OutputFolder + "/", StringComparison.Ordinal)) continue;

                string metaPath = pngPath + ".meta";
                Assert.IsTrue(File.Exists(metaPath), "Generated sprite has no importer meta: " + row.file);
                string meta = File.ReadAllText(metaPath);
                StringAssert.Contains("spritePixelsToUnits: 1", meta, "PPU must be 1: " + row.file);
                StringAssert.Contains("textureType: 8", meta, "Texture must import as a Sprite: " + row.file);
                StringAssert.Contains("spriteMode: 1", meta, "Sprite must import as Single: " + row.file);
                StringAssert.Contains("textureCompression: 0", meta, "UI sprites must be uncompressed: " + row.file);
            }
        }

        [Test]
        public void ContactSheet_Exists()
        {
            string path = Path.Combine(FindRepoRoot(), UiSpriteCatalog.ContactSheetFile);
            Assert.IsTrue(File.Exists(path), "UI sprite contact sheet is missing: " + path);
            Assert.Greater(new FileInfo(path).Length, 10_000, "UI sprite contact sheet is suspiciously small.");
        }
    }
}
