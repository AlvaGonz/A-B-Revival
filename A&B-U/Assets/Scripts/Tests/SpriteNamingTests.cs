using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using ABRevival.Core.Data;

namespace ABRevival.Tests
{
    [TestFixture]
    public class SpriteNamingTests
    {
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

        [Test]
        public void PartSpriteMap_IsDeterministic_AndCoversEveryCatalogId()
        {
            var records = ParseCatalog();
            var first = PartSpriteMap.Build(records);
            var second = PartSpriteMap.Build(records);

            Assert.AreEqual(271, first.Count);
            Assert.AreEqual(
                first.Select(mapping => mapping.fileName).ToArray(),
                second.Select(mapping => mapping.fileName).ToArray());

            for (int partId = 0; partId < 271; partId++)
            {
                Assert.IsTrue(
                    first.Any(mapping => mapping.partId == partId),
                    $"Catalog part {partId} has no sprite mapping.");
            }

            Assert.AreEqual(222, first.Select(mapping => mapping.fileName).Distinct().Count());
        }

        [Test]
        public void PartSpriteMap_IsIndependentOfRecordOrder_AndUsesTheLowestPartId()
        {
            var records = ParseCatalog();
            var forward = PartSpriteMap.Build(records);
            var reversed = PartSpriteMap.Build(Enumerable.Reverse(records).ToList());

            foreach (var mapping in forward)
            {
                var reversedMapping = reversed.Find(candidate => candidate.partId == mapping.partId);
                Assert.IsNotNull(reversedMapping);
                Assert.AreEqual(mapping.fileName, reversedMapping.fileName);
            }

            Assert.AreEqual("part_000_Body_1.png", forward[0].fileName);
            var wing = forward.Find(mapping => mapping.mcName == "Wing_1");
            Assert.AreEqual("part_173_Wing_1.png", wing.fileName);
        }

        [Test]
        public void PartSpriteMap_MatchesTheCommittedPngExport()
        {
            var records = ParseCatalog();
            var mappings = PartSpriteMap.Build(records);
            string spriteFolder = Path.Combine(FindRepoRoot(), "A&B-U", "Assets", "Art", "Sprites", "Parts");

            foreach (string fileName in mappings.Select(mapping => mapping.fileName).Distinct())
            {
                Assert.IsTrue(
                    File.Exists(Path.Combine(spriteFolder, fileName)),
                    $"Exported part sprite missing: {fileName}");
            }

            Assert.AreEqual(
                mappings.Select(mapping => mapping.fileName).Distinct().Count(),
                Directory.GetFiles(spriteFolder, "*.png").Length);
        }

        [Test]
        public void UiImageName_IsDeterministic()
        {
            Assert.AreEqual("ui_0005.png", SpriteNaming.UiImageFile(5));
            Assert.AreEqual("ui_0511.png", SpriteNaming.UiImageFile(511));
            Assert.AreEqual("ui_0511.png", SpriteNaming.UiImageFile(511));
        }
    }
}
