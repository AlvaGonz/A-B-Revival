using System.IO;
using NUnit.Framework;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 guard that the frozen-layout HUD document still carries every element the
    /// presenter binds, and that the generated sprite library asset was committed.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class CombatHudLayoutTests
    {
        public const string UxmlFile = "A&B-U/Assets/UI/CombatHud.uxml";
        public const string UssFile = "A&B-U/Assets/UI/CombatHud.uss";
        public const string SpriteLibraryFile = "A&B-U/Assets/Resources/UI/HudSpriteLibrary.asset";

        private static readonly string[] RequiredElements =
        {
            "hud-root",
            "hud-money-panel",
            "hud-money",
            "hud-pause-button",
            "hud-help-button",
            "hud-radar",
            "hud-radar-arrow",
            "hud-radar-distance",
            "hud-mission-timer",
            "hud-ai-count",
            "hud-ai-count-value",
            "hud-telemetry",
            "hud-lost-parts",
            "hud-bars",
            "hud-health-bar",
            "hud-hull-fill",
            "hud-fuel-bar",
            "hud-fuel-fill",
            "hud-reserved-bar",
            "hud-reserved-fill",
            "hud-hull-value",
            "hud-pilot-backing",
            "hud-pilot-portrait",
            "hud-pilot-health-pip",
            "hud-pilot-name",
            "hud-minimap",
            "hud-captain-tile",
            "hud-captain-backing",
            "hud-captain-portrait",
            "hud-captain-health",
            "hud-captain-mode",
            "hud-mode-badge",
            "hud-mode-badge-text",
            "hud-camera-badge",
            "hud-camera-badge-text",
            "hud-reticle"
        };

        [Test]
        public void Uxml_ContainsEveryHudElementThePresenterBinds()
        {
            string path = Path.Combine(FindRepoRoot(), UxmlFile);
            Assert.IsTrue(File.Exists(path), "Combat HUD document is missing: " + path);
            string uxml = File.ReadAllText(path);

            foreach (string element in RequiredElements)
                StringAssert.Contains("name=\"" + element + "\"", uxml, "HUD element missing: " + element);

            StringAssert.Contains("src=\"CombatHud.uss\"", uxml, "The HUD must use its stylesheet.");
        }

        [Test]
        public void Uss_StylesTheFrozenAnchors()
        {
            string path = Path.Combine(FindRepoRoot(), UssFile);
            Assert.IsTrue(File.Exists(path), "Combat HUD stylesheet is missing: " + path);
            string uss = File.ReadAllText(path);

            foreach (string selector in new[]
                     {
                         ".hud-money-panel", ".hud-radar", ".hud-ai-count", ".hud-telemetry",
                         ".hud-bars", ".hud-fuel-fill", ".hud-reserved-fill",
                         ".hud-pilot-tile", ".hud-minimap", ".hud-captain-tile", ".hud-reticle"
                     })
            {
                StringAssert.Contains(selector, uss, "HUD selector missing: " + selector);
            }
        }

        [Test]
        public void GeneratedSpriteLibraryAsset_IsCommitted()
        {
            string path = Path.Combine(FindRepoRoot(), SpriteLibraryFile);
            Assert.IsTrue(File.Exists(path), "HUD sprite library is missing: " + path);
            Assert.Greater(new FileInfo(path).Length, 1000, "HUD sprite library has no sprite references.");

            string meta = path + ".meta";
            Assert.IsTrue(File.Exists(meta), "HUD sprite library has no importer meta.");
            StringAssert.Contains("NativeFormatImporter", File.ReadAllText(meta));
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

            return directory?.FullName ?? @"C:\Dev\A&B revival";
        }
    }
}
