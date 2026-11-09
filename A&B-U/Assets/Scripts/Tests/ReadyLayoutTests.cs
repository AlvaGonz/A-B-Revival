using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 structural guard for the 1920x1080 full-bleed Ready/Briefing document and the
    /// committed 2011 UIMapBuild page reference (ADR 0020). Original IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class ReadyLayoutTests
    {
        public const string UxmlFile = "A&B-U/Assets/UI/Ready.uxml";
        public const string UssFile = "A&B-U/Assets/UI/Ready.uss";
        public const string ControllerFile = "A&B-U/Assets/Scripts/Presentation/ReadyController.cs";
        public const string GameFlowFile = "A&B-U/Assets/Scripts/Presentation/GameFlowController.cs";
        public const string CenterFile = "A&B-U/Assets/Scripts/Presentation/CenterController.cs";

        private static readonly string[] RequiredElements =
        {
            "ready-root",
            "ready-screen",
            "ready-backdrop",
            "ready-loading",
            "ready-window",
            "ready-title-plate",
            "ready-title",
            "ready-body",
            "loadout-panel",
            "loadout-player",
            "companion-0",
            "companion-1",
            "companion-2",
            "companion-3",
            "companion-4",
            "loadout-status",
            "briefing-panel",
            "briefing-words",
            "briefing-subtext",
            "ready-footer",
            "back-button",
            "launch-button"
        };

        [Test]
        public void Uxml_ContainsEveryReadyElementTheControllerBinds()
        {
            string uxml = File.ReadAllText(Path.Combine(FindRepoRoot(), UxmlFile));

            foreach (string element in RequiredElements)
                StringAssert.Contains("name=\"" + element + "\"", uxml, "Ready element missing: " + element);

            StringAssert.Contains("src=\"Ready.uss\"", uxml, "Ready must use its stylesheet.");
            StringAssert.DoesNotContain("1280px", uxml, "Ready must not use a fixed-width root.");
            StringAssert.DoesNotContain("960px", uxml, "Ready must not use a fixed-height root.");
        }

        [Test]
        public void Uss_FillsTheRootAndHasNoRetiredFixedRoot()
        {
            string uss = File.ReadAllText(Path.Combine(FindRepoRoot(), UssFile));

            StringAssert.Contains(".ready-root", uss);
            StringAssert.Contains("width: 100%;", uss);
            StringAssert.Contains("height: 100%;", uss);
            StringAssert.Contains(".ready-screen", uss);
            StringAssert.Contains("position: absolute;", uss);
            StringAssert.Contains(".ready-window", uss);
            StringAssert.Contains(".loadout-panel", uss);
            StringAssert.Contains(".briefing-words", uss);
            StringAssert.Contains(".launch-button", uss);

            StringAssert.DoesNotContain("1280", uss, "The retired 1280x960 hangar root must not survive.");
            StringAssert.DoesNotContain("960px", uss, "The retired 1280x960 hangar root must not survive.");
        }

        [Test]
        public void Ready_RendersSpritesThroughImageChildrenAndKeepsTheHandoffLoadout()
        {
            string controller = File.ReadAllText(Path.Combine(FindRepoRoot(), ControllerFile));
            string flow = File.ReadAllText(Path.Combine(FindRepoRoot(), GameFlowFile));
            string center = File.ReadAllText(Path.Combine(FindRepoRoot(), CenterFile));

            StringAssert.Contains("UiSpriteSkin.Apply", controller);
            StringAssert.Contains("keepBackground: false", controller);
            StringAssert.Contains("MissionLoadout.Capture", controller);
            StringAssert.Contains("CanLaunch", controller);

            StringAssert.Contains("_ready.LaunchRequested += OnReadyLaunch", flow);
            StringAssert.Contains("_center.ReadyRequested += OnReadyRequested", flow);
            StringAssert.Contains("_activeLoadout = MissionLoadout.Capture", flow);
            StringAssert.Contains("GameFlowState.Ready", flow);

            StringAssert.Contains("public event Action ReadyRequested", center);
            StringAssert.Contains("public bool CanLaunch", center);
        }

        [Test]
        public void ReferencePage_IsTheUnmodified2011Export()
        {
            string root = FindRepoRoot();
            string reference = Path.Combine(root, "docs/assets/reference/UI_MapBuilding.png");
            string source = Path.Combine(root, "A&B-U/Assets/Art/Sprites/UI/Screens/UI_MapBuilding.png");

            Assert.IsTrue(File.Exists(reference), "Ready reference page is missing.");
            Assert.IsTrue(File.Exists(source), "Ready source page export is missing.");
            Assert.AreEqual(Hash(source), Hash(reference),
                "UI_MapBuilding.png must be the unmodified 2011 page export (no crop, redraw or annotation).");
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

            return directory?.FullName ?? Directory.GetCurrentDirectory();
        }
    }
}
