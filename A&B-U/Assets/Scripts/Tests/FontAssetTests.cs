using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 guard for ADR 0020: the shared theme is the only place that declares a game
    /// UI font, and every declaration points at one of the eight frozen font assets.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class FontAssetTests
    {
        public const string ThemeFile = "A&B-U/Assets/UI/UnityDefaultRuntimeTheme.tss";
        public const string FontFolder = "A&B-U/Assets/Art/Fonts";
        public const string ReferenceDoc = "docs/assets/reference/README.md";

        private static readonly (string File, string Family, int Symbol)[] Fonts =
        {
            ("font_0007_SF_TransRobotics.ttf", "SF TransRobotics", 7),
            ("font_0081_Arial.ttf", "Arial", 81),
            ("font_0002_sans.ttf", "_sans", 2),
            ("font_0478_sans.ttf", "_sans", 478),
            ("font_0627_SF_TransRobotics.ttf", "SF TransRobotics", 627),
            ("font_0701_SF_TransRobotics.ttf", "SF TransRobotics", 701),
            ("font_0595_Times_New_Roman.ttf", "Times New Roman", 595),
            ("font_0496_SF_Zero_Gravity.ttf", "SF Zero Gravity", 496)
        };

        [Test]
        public void Theme_IsTheOnlyFontSource_AndDeclaresEveryOriginalFace()
        {
            string root = FindRepoRoot();
            string path = Path.Combine(root, ThemeFile);
            Assert.IsTrue(File.Exists(path), "Shared theme is missing: " + path);
            string theme = File.ReadAllText(path);

            StringAssert.Contains(":root", theme, "The inherited root font is missing.");
            StringAssert.Contains("font_0007_SF_TransRobotics.ttf", theme,
                "SF TransRobotics must be the inherited UI font.");

            foreach ((string file, string family, int symbol) in Fonts)
            {
                StringAssert.Contains(file, theme, "Theme does not declare " + family + " (symbol " + symbol + ").");
                Assert.IsTrue(File.Exists(Path.Combine(root, FontFolder, file)),
                    "Font asset is missing: " + file);
            }

            StringAssert.DoesNotContain("system://", theme, "A system font must never be referenced.");
            foreach (string line in theme.Split('\n'))
            {
                int index = line.IndexOf("-unity-font-definition:", StringComparison.Ordinal);
                if (index < 0) continue;
                StringAssert.Contains("../Art/Fonts/font_", line,
                    "Every font declaration must point at an extracted original face: " + line.Trim());
            }
        }

        [Test]
        public void FontImporters_EmbedTheOriginalBytes()
        {
            string root = FindRepoRoot();
            foreach ((string file, string family, int symbol) in Fonts)
            {
                string metaPath = Path.Combine(root, FontFolder, file + ".meta");
                Assert.IsTrue(File.Exists(metaPath), "Font meta is missing: " + metaPath);
                string meta = File.ReadAllText(metaPath);

                StringAssert.Contains("includeFontData: 1", meta, file + " must embed its font data.");
                StringAssert.Contains("fontNames:", meta, file + " has no family name.");
                StringAssert.Contains(family, meta, file + " does not declare family " + family + ".");
            }
        }

        [Test]
        public void ReferenceTable_DocumentsEveryFamilyAndSourceSymbol()
        {
            string root = FindRepoRoot();
            string path = Path.Combine(root, ReferenceDoc);
            Assert.IsTrue(File.Exists(path), "Reference README is missing: " + path);
            string doc = File.ReadAllText(path);

            foreach ((string file, string family, int symbol) in Fonts)
            {
                StringAssert.Contains(file, doc, "Reference table is missing " + file + ".");
                StringAssert.Contains(family, doc, "Reference table is missing family " + family + ".");
            }

            foreach (int symbol in new[] { 2, 7, 81, 478, 496, 595, 627, 701 })
                StringAssert.Contains("| " + symbol + " |", doc, "Reference table is missing symbol " + symbol + ".");
        }

        private static string FindRepoRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null &&
                   !Directory.Exists(Path.Combine(directory.FullName, ".git")) &&
                   !File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? @"C:\Dev\ab-wt\185";
        }
    }
}
