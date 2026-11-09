using System.IO;
using NUnit.Framework;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 structural guard for the 1920x1080 full-bleed Center document. The PlayMode
    /// gate asserts the live layout; this fast gate catches a fixed root or a missing binding.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class CenterLayoutTests
    {
        public const string UxmlFile = "A&B-U/Assets/UI/Center.uxml";
        public const string UssFile = "A&B-U/Assets/UI/Center.uss";

        private static readonly string[] RequiredElements =
        {
            "center-root",
            "center-screen",
            "center-header",
            "center-title",
            "battle-label",
            "money-label",
            "help-button",
            "preview-frame",
            "preview-clip",
            "preview-canvas",
            "preview-name",
            "inuse-label",
            "stats-card",
            "stat-name",
            "stat-hp",
            "stat-fire",
            "stat-speed",
            "stat-steering",
            "stat-class",
            "stat-parts",
            "stat-value",
            "companion-slots",
            "grade-0",
            "grade-1",
            "grade-2",
            "grade-3",
            "grade-4",
            "reset-button",
            "best-button",
            "instruction-label",
            "info-button",
            "ready-button",
            "shop-button",
            "factory-button",
            "back-button",
            "craft-table",
            "table-header",
            "craft-list",
            "page-label",
            "first-button",
            "prev-button",
            "next-button",
            "last-button",
            "center-placeholder",
            "placeholder-title",
            "placeholder-back"
        };

        [Test]
        public void Uxml_ContainsEveryCenterElementTheControllerBinds()
        {
            string path = Path.Combine(FindRepoRoot(), UxmlFile);
            Assert.IsTrue(File.Exists(path), "Center document is missing: " + path);
            string uxml = File.ReadAllText(path);

            foreach (string element in RequiredElements)
                StringAssert.Contains("name=\"" + element + "\"", uxml, "Center element missing: " + element);

            StringAssert.Contains("src=\"Center.uss\"", uxml, "Center must use its stylesheet.");
            StringAssert.DoesNotContain("1280px", uxml, "Center must not keep the retired fixed-width root.");
            StringAssert.DoesNotContain("960px", uxml, "Center must not keep the retired fixed-height root.");
        }

        [Test]
        public void Uss_FillsTheRootAndHasNoRetiredFixedRoot()
        {
            string path = Path.Combine(FindRepoRoot(), UssFile);
            Assert.IsTrue(File.Exists(path), "Center stylesheet is missing: " + path);
            string uss = File.ReadAllText(path);

            StringAssert.Contains(".center-root", uss);
            StringAssert.Contains("width: 100%;", uss);
            StringAssert.Contains("height: 100%;", uss);
            StringAssert.Contains(".center-screen", uss);
            StringAssert.Contains("position: absolute;", uss);
            StringAssert.Contains(".craft-row", uss);
            StringAssert.Contains(".grade-button", uss);
            StringAssert.Contains(".nav-button", uss);

            StringAssert.DoesNotContain("1280", uss, "The retired 1280x960 hangar root must not survive.");
            StringAssert.DoesNotContain("960px", uss, "The retired 1280x960 hangar root must not survive.");
        }

        [Test]
        public void RetiredCombinedHangarDocument_IsGone()
        {
            string root = FindRepoRoot();
            Assert.IsFalse(File.Exists(Path.Combine(root, "A&B-U/Assets/UI/Hangar.uxml")),
                "The combined Hangar document is retired.");
            Assert.IsFalse(File.Exists(Path.Combine(root, "A&B-U/Assets/UI/Hangar.uss")),
                "The combined Hangar stylesheet is retired.");
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

            return directory?.FullName ?? @"C:\Dev\ab-wt\174";
        }
    }
}
