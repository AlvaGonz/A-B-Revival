using System.IO;
using NUnit.Framework;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 structural guard for the 1920x1080 full-bleed Factory document. The PlayMode
    /// gate asserts the live layout; this fast gate catches a fixed root or a missing binding.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class FactoryLayoutTests
    {
        public const string UxmlFile = "A&B-U/Assets/UI/Factory.uxml";
        public const string UssFile = "A&B-U/Assets/UI/Factory.uss";

        private static readonly string[] RequiredElements =
        {
            "factory-root",
            "factory-screen",
            "factory-header",
            "factory-title",
            "help-button",
            "undo-0",
            "undo-11",
            "preview-frame",
            "preview-clip",
            "preview-canvas",
            "preview-name",
            "merge-button",
            "new-button",
            "add-button",
            "zoom-in-button",
            "zoom-out-button",
            "center-view-button",
            "detail-card",
            "detail-hp",
            "detail-steering",
            "detail-speed",
            "detail-fire",
            "wing-label",
            "detail-parts",
            "stats-card",
            "stats-column",
            "stat-name",
            "stat-hp",
            "stat-fire",
            "stat-speed",
            "stat-steering",
            "stat-class",
            "stat-parts",
            "stat-value",
            "hull-label",
            "back-to-center-button",
            "parts-crafts-button",
            "assembly-error",
            "table-area",
            "table-tabs",
            "tab-all",
            "tab-body",
            "tab-wing",
            "tab-engine",
            "tab-gun",
            "table-header",
            "crafts-header",
            "owned-parts-list",
            "crafts-list",
            "page-label",
            "first-button",
            "prev-button",
            "next-button",
            "last-button",
            "factory-help",
            "help-close"
        };

        [Test]
        public void Uxml_ContainsEveryFactoryElementTheControllerBinds()
        {
            string path = Path.Combine(FindRepoRoot(), UxmlFile);
            Assert.IsTrue(File.Exists(path), "Factory document is missing: " + path);
            string uxml = File.ReadAllText(path);

            foreach (string element in RequiredElements)
                StringAssert.Contains("name=\"" + element + "\"", uxml, "Factory element missing: " + element);

            StringAssert.Contains("src=\"Factory.uss\"", uxml, "Factory must use its stylesheet.");
            StringAssert.DoesNotContain("1280px", uxml, "Factory must not keep the retired fixed-width root.");
            StringAssert.DoesNotContain("960px", uxml, "Factory must not keep the retired fixed-height root.");
        }

        [Test]
        public void Uss_FillsTheRootAndHasNoRetiredFixedRoot()
        {
            string path = Path.Combine(FindRepoRoot(), UssFile);
            Assert.IsTrue(File.Exists(path), "Factory stylesheet is missing: " + path);
            string uss = File.ReadAllText(path);

            StringAssert.Contains(".factory-root", uss);
            StringAssert.Contains("width: 100%;", uss);
            StringAssert.Contains("height: 100%;", uss);
            StringAssert.Contains(".factory-screen", uss);
            StringAssert.Contains("position: absolute;", uss);
            StringAssert.Contains(".row", uss);
            StringAssert.Contains(".undo-button", uss);
            StringAssert.Contains(".tab-button", uss);
            StringAssert.Contains(".col-fire", uss);

            StringAssert.DoesNotContain("1280", uss, "The retired 1280x960 hangar root must not survive.");
            StringAssert.DoesNotContain("960px", uss, "The retired 1280x960 hangar root must not survive.");
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

            return directory?.FullName ?? @"C:\Dev\ab-wt\175";
        }
    }
}
