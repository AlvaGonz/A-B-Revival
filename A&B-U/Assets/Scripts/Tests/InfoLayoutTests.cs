using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 structural guard for the 1920x1080 full-bleed Info/Records document, its
    /// controls reference and the committed 2011 page references (ADR 0020).
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class InfoLayoutTests
    {
        public const string UxmlFile = "A&B-U/Assets/UI/Info.uxml";
        public const string UssFile = "A&B-U/Assets/UI/Info.uss";
        public const string ControllerFile = "A&B-U/Assets/Scripts/Presentation/InfoController.cs";

        private static readonly string[] RequiredElements =
        {
            "info-root",
            "info-screen",
            "info-header",
            "info-title",
            "money-label",
            "help-tab",
            "records-tab",
            "info-body",
            "help-page",
            "help-sheet",
            "help-art",
            "help-prev",
            "help-page-label",
            "help-next",
            "controls-card",
            "control-mouse",
            "control-keyboard",
            "control-switch",
            "control-turret",
            "control-boost",
            "control-camera",
            "control-tactical",
            "control-autofire",
            "records-page",
            "records-title",
            "records-summary",
            "records-story",
            "records-money",
            "records-player",
            "records-fleet",
            "records-companions",
            "records-parts-total",
            "records-camera",
            "records-note",
            "parts-panel",
            "parts-header",
            "parts-list",
            "parts-count",
            "back-button"
        };

        private static readonly string[][] ReferencePages =
        {
            new[] { "docs/assets/reference/UI_HelpGame.png", "A&B-U/Assets/Art/Sprites/UI/Help/UI_HelpGame.png" },
            new[] { "docs/assets/reference/UI_HelpGame2.png", "A&B-U/Assets/Art/Sprites/UI/Help/UI_HelpGame2.png" },
            new[] { "docs/assets/reference/UI_Data.png", "A&B-U/Assets/Art/Sprites/UI/Screens/UI_Data.png" },
            new[] { "docs/assets/reference/UI_Mission.png", "A&B-U/Assets/Art/Sprites/UI/Screens/UI_Mission.png" }
        };

        [Test]
        public void Uxml_ContainsEveryInfoElementTheControllerBinds()
        {
            string path = Path.Combine(FindRepoRoot(), UxmlFile);
            Assert.IsTrue(File.Exists(path), "Info document is missing: " + path);
            string uxml = File.ReadAllText(path);

            foreach (string element in RequiredElements)
                StringAssert.Contains("name=\"" + element + "\"", uxml, "Info element missing: " + element);

            StringAssert.Contains("src=\"Info.uss\"", uxml, "Info must use its stylesheet.");
            StringAssert.DoesNotContain("1280px", uxml, "Info must not use a fixed-width root.");
            StringAssert.DoesNotContain("960px", uxml, "Info must not use a fixed-height root.");
        }

        [Test]
        public void ControlsReference_CoversBothModesAndEveryRevivalBinding()
        {
            string uxml = File.ReadAllText(Path.Combine(FindRepoRoot(), UxmlFile));

            StringAssert.Contains("Mouse", uxml, "The reference must describe Mouse control mode.");
            StringAssert.Contains("Keyboard", uxml, "The reference must describe Keyboard control mode.");
            StringAssert.Contains("Q switches", uxml, "Q must switch control modes.");
            StringAssert.Contains("Turret aim", uxml, "The reference must describe turret aim.");
            StringAssert.Contains("rotate independently", uxml, "Turrets must be described as hull-independent.");
            StringAssert.Contains("Boost / fuel", uxml, "The reference must describe booster fuel.");
            StringAssert.Contains("Space burns", uxml, "Space must be documented as the boost key.");
            StringAssert.Contains("C cycles", uxml, "C must document camera distance cycling.");
            StringAssert.Contains("E cycles the Captain", uxml, "E must document tactical mode cycling.");
            StringAssert.Contains("X toggles", uxml, "X must document autofire.");
        }

        [Test]
        public void Uss_FillsTheRootAndHasNoRetiredFixedRoot()
        {
            string path = Path.Combine(FindRepoRoot(), UssFile);
            Assert.IsTrue(File.Exists(path), "Info stylesheet is missing: " + path);
            string uss = File.ReadAllText(path);

            StringAssert.Contains(".info-root", uss);
            StringAssert.Contains("width: 100%;", uss);
            StringAssert.Contains("height: 100%;", uss);
            StringAssert.Contains(".info-screen", uss);
            StringAssert.Contains("position: absolute;", uss);
            StringAssert.Contains(".help-art", uss);
            StringAssert.Contains(".controls-card", uss);
            StringAssert.Contains(".record-row", uss);
            StringAssert.Contains(".part-row", uss);
            StringAssert.Contains(".part-cell", uss);
            StringAssert.Contains(".back-button", uss);

            StringAssert.DoesNotContain("1280", uss, "The retired 1280x960 hangar root must not survive.");
            StringAssert.DoesNotContain("960px", uss, "The retired 1280x960 hangar root must not survive.");
        }

        [Test]
        public void Info_IsReadOnlyAndNeverCapturesTheSave()
        {
            string controller = File.ReadAllText(Path.Combine(FindRepoRoot(), ControllerFile));

            StringAssert.DoesNotContain("CaptureSave", controller,
                "Info/Records must not capture or mutate the shared payload.");
            StringAssert.DoesNotContain("Autosave", controller,
                "Info/Records must not write the save.");
            StringAssert.DoesNotContain("SaveToFile", controller,
                "Info/Records must not write the save.");
        }

        [Test]
        public void ReferencePages_AreTheUnmodified2011Exports()
        {
            string root = FindRepoRoot();
            foreach (string[] pair in ReferencePages)
            {
                string reference = Path.Combine(root, pair[0]);
                string source = Path.Combine(root, pair[1]);
                Assert.IsTrue(File.Exists(reference), "Reference page is missing: " + pair[0]);
                Assert.IsTrue(File.Exists(source), "Source page export is missing: " + pair[1]);
                Assert.AreEqual(Hash(source), Hash(reference),
                    pair[0] + " must be the unmodified 2011 page export (no crop, redraw or annotation).");
            }
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

            return directory?.FullName ?? @"C:\Dev\ab-wt\177";
        }
    }
}
