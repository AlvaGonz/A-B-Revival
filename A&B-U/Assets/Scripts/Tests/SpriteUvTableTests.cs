using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ABRevival.Core.Data;
using ABRevival.Core.Rendering;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 coverage and stability gate for the deterministic part atlas UV table.
    /// Rebuilds the table from the real catalog binaries and the committed PNGs, so a
    /// new or re-exported sprite fails the fast loop until the table is regenerated.
    /// </summary>
    [TestFixture]
    public class SpriteUvTableTests
    {
        private const int CatalogPartCount = 271;
        private const int DistinctSymbolCount = 222;
        public const int BulletSymbolCount = 5;

        private static string FindRepoRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null && !Directory.Exists(Path.Combine(directory.FullName, ".git")) && !File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                directory = directory.Parent;
            }
            return directory?.FullName ?? @"C:\Dev\A&B revival";
        }

        private static List<PartDefinitionRecord> ParseCatalog()
        {
            string binaryData = Path.Combine(FindRepoRoot(), "src", "assets", "binaryData");
            return PartCatalogParser.ParseCatalogFromFiles(
                Path.Combine(binaryData, "8_all.AllData_defPart.bin"),
                Path.Combine(binaryData, "2_all.AllData_defFire.bin"),
                Path.Combine(binaryData, "11_all.AllData_defBullet.bin"));
        }

        private static string PartsFolder()
        {
            return Path.Combine(FindRepoRoot(), "A&B-U", "Assets", "Art", "Sprites", "PartMounts");
        }

        private static string BulletsFolder()
        {
            return Path.Combine(FindRepoRoot(), "A&B-U", "Assets", "Art", "Sprites", "Bullets");
        }

        private static Dictionary<string, SpritePixelSize> ReadSizes()
        {
            Dictionary<string, SpritePixelSize> sizes = PngSize.ReadFolder(PartsFolder());
            foreach (KeyValuePair<string, SpritePixelSize> entry in PngSize.ReadFolder(BulletsFolder()))
                sizes[entry.Key] = entry.Value;
            return sizes;
        }

        private static List<string> BulletSymbols()
        {
            var names = new List<string>();
            foreach (string file in Directory.GetFiles(BulletsFolder(), "*.png"))
                names.Add(Path.GetFileName(file));
            names.Sort(StringComparer.Ordinal);
            return names;
        }

        private static string UvTablePath()
        {
            return Path.Combine(FindRepoRoot(), "A&B-U", "Assets", "Art", "Atlases", "PartsAtlasUv.txt");
        }

        [Test]
        public void UvTable_IsRegeneratedExactlyFromTheCommittedSprites()
        {
            var records = ParseCatalog();
            var mappings = PartSpriteMap.Build(records);
            var sizes = ReadSizes();

            string expected = SpriteUvTableBuilder.Build(mappings, sizes, extraSymbols: BulletSymbols());
            Assert.IsTrue(File.Exists(UvTablePath()), $"Committed UV table missing: {UvTablePath()}");
            string committed = File.ReadAllText(UvTablePath());

            Assert.AreEqual(Normalize(expected), Normalize(committed),
                "PartsAtlasUv.txt is stale. Run Tools/A&B Revival/Rebuild Parts Atlas UV Table.");
        }

        [Test]
        public void UvTable_CoversEveryCatalogPartAndEveryExportedSymbol()
        {
            var records = ParseCatalog();
            var mappings = PartSpriteMap.Build(records);
            var sizes = ReadSizes();
            var table = SpriteUvTable.Parse(File.ReadAllText(UvTablePath()));

            Assert.AreEqual(CatalogPartCount, mappings.Count);
            Assert.AreEqual(DistinctSymbolCount + BulletSymbolCount, sizes.Count);
            Assert.AreEqual(CatalogPartCount, table.PartCount);
            Assert.AreEqual(
                DistinctSymbolCount + 1 + BulletSymbolCount,
                table.SymbolCount,
                "222 mount sprites, the projectile white cell and 5 bullet symbols.");
            Assert.IsTrue(table.HasWhite, "Projectiles need the synthetic white atlas cell.");
            Assert.AreEqual(SpriteAtlasLayout.DefaultWidth, table.AtlasWidth);
            Assert.AreEqual(SpriteAtlasLayout.DefaultHeight, table.AtlasHeight);

            var partIds = new HashSet<int>();
            var symbolRects = new Dictionary<string, SpriteUvRect>(StringComparer.Ordinal);
            foreach (PartSpriteMapping mapping in mappings)
            {
                Assert.IsTrue(partIds.Add(mapping.partId), $"Duplicate part id {mapping.partId}.");
                Assert.IsTrue(table.TryGetPart(mapping.partId, out SpriteUvRect rect),
                    $"Catalog part {mapping.partId} ({mapping.fileName}) has no atlas rect.");

                if (symbolRects.TryGetValue(mapping.fileName, out SpriteUvRect existing))
                {
                    Assert.AreEqual(existing.X, rect.X, mapping.fileName);
                    Assert.AreEqual(existing.Y, rect.Y, mapping.fileName);
                }
                else
                {
                    symbolRects.Add(mapping.fileName, rect);
                }
            }

            Assert.AreEqual(DistinctSymbolCount, symbolRects.Count);
            foreach (SpriteUvRect rect in symbolRects.Values)
            {
                Assert.Greater(rect.Width, 0);
                Assert.Greater(rect.Height, 0);
                Assert.GreaterOrEqual(rect.X, 0);
                Assert.GreaterOrEqual(rect.Y, 0);
                Assert.LessOrEqual(rect.X + rect.Width, table.AtlasWidth);
                Assert.LessOrEqual(rect.Y + rect.Height, table.AtlasHeight);
            }
        }

        [Test]
        public void UvTable_IsStableAcrossReparsesAndInputOrder()
        {
            var records = ParseCatalog();
            var mappings = PartSpriteMap.Build(records);
            var sizes = ReadSizes();
            List<string> extras = BulletSymbols();

            // The packer sorts symbols, so a reversed catalog order yields identical rects.
            var reversed = new List<PartDefinitionRecord>(records);
            reversed.Reverse();
            var reversedMappings = PartSpriteMap.Build(reversed);

            string forward = SpriteUvTableBuilder.Build(mappings, sizes, extraSymbols: extras);
            string backward = SpriteUvTableBuilder.Build(reversedMappings, sizes, extraSymbols: extras);
            Assert.AreEqual(Normalize(forward), Normalize(backward), "UV rects must not depend on catalog order.");

            var first = SpriteUvTable.Parse(forward);
            var second = SpriteUvTable.Parse(forward);
            Assert.AreEqual(first.PartCount, second.PartCount);
            Assert.AreEqual(first.SymbolCount, second.SymbolCount);
            Assert.AreEqual(first.White.X, second.White.X);

            for (int partId = 0; partId < first.PartCount; partId++)
            {
                Assert.IsTrue(first.TryGetPart(partId, out SpriteUvRect a));
                Assert.IsTrue(second.TryGetPart(partId, out SpriteUvRect b));
                Assert.AreEqual(a.X, b.X);
                Assert.AreEqual(a.Y, b.Y);
                Assert.AreEqual(a.Width, b.Width);
                Assert.AreEqual(a.Height, b.Height);
                Assert.AreEqual(a.UvX, b.UvX, 0f);
                Assert.AreEqual(a.UvY, b.UvY, 0f);
            }
        }

        [Test]
        public void UvTable_ConvertsTopLeftPixelsToBottomLeftUvs()
        {
            var table = SpriteUvTable.Parse(File.ReadAllText(UvTablePath()));
            SpriteUvRect white = table.White;

            // The white cell is the first packed symbol at the top-left corner.
            Assert.AreEqual(0, white.X);
            Assert.AreEqual(0, white.Y);
            Assert.AreEqual(0f, white.UvX, 0f);
            Assert.AreEqual(1f - (white.Height / (float)table.AtlasHeight), white.UvY, 0.000001f);
            Assert.AreEqual(white.Width / (float)table.AtlasWidth, white.UvWidth, 0.000001f);
            Assert.AreEqual(white.Height / (float)table.AtlasHeight, white.UvHeight, 0.000001f);
        }

        [Test]
        public void UvTable_ExposesBulletAndEffectSymbols()
        {
            var table = SpriteUvTable.Parse(File.ReadAllText(UvTablePath()));

            Assert.IsTrue(table.TryGetSymbol("bullet_normal.png", out SpriteUvRect normal));
            Assert.IsTrue(table.TryGetSymbol("bullet_Blt_1.png", out SpriteUvRect blt1));
            Assert.AreEqual(32, normal.Width);
            Assert.AreEqual(32, normal.Height);
            Assert.Less(normal.X, table.AtlasWidth);
            Assert.Less(blt1.Y, table.AtlasHeight);
            Assert.IsFalse(table.TryGetSymbol("missing.png", out _));
        }

        [Test]
        public void UvTable_RejectsMalformedPayloads()
        {
            Assert.Throws<ArgumentException>(() => SpriteUvTable.Parse(" "));
            Assert.Throws<FormatException>(() => SpriteUvTable.Parse("0;part_000_Body_1.png;0;0;1;1\n"));
            Assert.Throws<FormatException>(() => SpriteUvTable.Parse("atlas=2048x2048\n0;part_000_Body_1.png;0;0;1;1\n"));
            Assert.Throws<FormatException>(() => SpriteUvTable.Parse("atlas=8x8\n-1;__white;0;0;8;8\n0;part_000_Body_1.png;4;4;8;8\n"));
            Assert.Throws<FormatException>(() => SpriteUvTable.Parse(
                "atlas=8x8\n-1;__white;0;0;8;8\n4097;part_000_Body_1.png;0;0;1;1\n"),
                "Part ids above the 4096 cap must be rejected.");
        }

        private static string Normalize(string value)
        {
            return value.Replace("\r\n", "\n").Replace('\r', '\n');
        }
    }
}
