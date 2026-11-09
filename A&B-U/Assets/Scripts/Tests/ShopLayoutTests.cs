using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 structural guard for the 1920x1080 full-bleed Shop document and its frozen
    /// UI_Shop page reference. The PlayMode gate asserts the live layout and transaction flow.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class ShopLayoutTests
    {
        public const string UxmlFile = "A&B-U/Assets/UI/Shop.uxml";
        public const string UssFile = "A&B-U/Assets/UI/Shop.uss";
        public const string ReferenceFile = "docs/assets/reference/shop-page.png";
        public const string SourceReferenceFile = "A&B-U/Assets/Art/Sprites/UI/ui_0205_reference_shop_page.png";

        private static readonly string[] RequiredElements =
        {
            "shop-root",
            "shop-screen",
            "shop-header",
            "shop-title",
            "money-label",
            "buy-panel",
            "buy-header",
            "buy-rows",
            "buy-page-label",
            "buy-first",
            "buy-prev",
            "buy-next",
            "buy-last",
            "buy-filter-all",
            "buy-filter-body",
            "buy-filter-wing",
            "buy-filter-engine",
            "buy-filter-gun",
            "cost-label",
            "now-label",
            "accept-button",
            "reset-button",
            "center-button",
            "buy-detail-name",
            "buy-detail-value",
            "sell-detail-name",
            "sell-detail-value",
            "sell-panel",
            "sell-header",
            "sell-rows",
            "sell-page-label",
            "sell-first",
            "sell-prev",
            "sell-next",
            "sell-last",
            "sell-filter-all",
            "sell-filter-body",
            "sell-filter-wing",
            "sell-filter-engine",
            "sell-filter-gun",
            "player-label",
            "shop-error"
        };

        [Test]
        public void Uxml_ContainsEveryShopElementThePresenterBinds()
        {
            string path = Path.Combine(FindRepoRoot(), UxmlFile);
            Assert.IsTrue(File.Exists(path), "Shop document is missing: " + path);
            string uxml = File.ReadAllText(path);

            foreach (string element in RequiredElements)
                StringAssert.Contains("name=\"" + element + "\"", uxml, "Shop element missing: " + element);

            StringAssert.Contains("src=\"Shop.uss\"", uxml, "Shop must use its stylesheet.");
            StringAssert.DoesNotContain("1280px", uxml, "Shop must not use a fixed-width root.");
            StringAssert.DoesNotContain("960px", uxml, "Shop must not use a fixed-height root.");
        }

        [Test]
        public void Uss_FillsTheRootAndHasNoRetiredFixedRoot()
        {
            string path = Path.Combine(FindRepoRoot(), UssFile);
            Assert.IsTrue(File.Exists(path), "Shop stylesheet is missing: " + path);
            string uss = File.ReadAllText(path);

            StringAssert.Contains(".shop-root", uss);
            StringAssert.Contains("width: 100%;", uss);
            StringAssert.Contains("height: 100%;", uss);
            StringAssert.Contains(".shop-screen", uss);
            StringAssert.Contains("position: absolute;", uss);
            StringAssert.Contains(".shop-row", uss);
            StringAssert.Contains(".shop-icon", uss);
            StringAssert.Contains(".filter-button", uss);
            StringAssert.Contains(".detail-card", uss);

            StringAssert.DoesNotContain("1280", uss, "The retired 1280x960 hangar root must not survive.");
            StringAssert.DoesNotContain("960px", uss, "The retired 1280x960 hangar root must not survive.");
        }

        [Test]
        public void ReferencePage_IsTheUnmodifiedUiShopExport()
        {
            string root = FindRepoRoot();
            string reference = Path.Combine(root, ReferenceFile);
            string source = Path.Combine(root, SourceReferenceFile);
            Assert.IsTrue(File.Exists(reference), "Shop page reference is missing: " + ReferenceFile);
            Assert.IsTrue(File.Exists(source), "Source UI_Shop export is missing: " + SourceReferenceFile);
            Assert.AreEqual(Hash(source), Hash(reference),
                "shop-page.png must be the unmodified UI_Shop export (no crop, redraw or annotation).");
        }

        private static string Hash(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream stream = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(stream));
            }
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

            return directory?.FullName ?? @"C:\Dev\ab-wt\176";
        }
    }
}
