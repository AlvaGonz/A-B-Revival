using System;
using System.IO;
using System.Security.Cryptography;
using ABRevival.Core.Persistence;
using ABRevival.Core.Simulation;
using NUnit.Framework;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 structural guard for the 1920x1080 Main menu and Options documents, their
    /// controls/settings wiring and the committed 2011 page references (ADR 0020).
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class MainMenuLayoutTests
    {
        public const string MainUxmlFile = "A&B-U/Assets/UI/Main.uxml";
        public const string MainUssFile = "A&B-U/Assets/UI/Main.uss";
        public const string MainControllerFile = "A&B-U/Assets/Scripts/Presentation/MainMenuController.cs";
        public const string OptionsUxmlFile = "A&B-U/Assets/UI/Options.uxml";
        public const string OptionsUssFile = "A&B-U/Assets/UI/Options.uss";
        public const string OptionsControllerFile = "A&B-U/Assets/Scripts/Presentation/OptionsController.cs";

        private static readonly string[] MainElements =
        {
            "main-root",
            "main-backdrop",
            "main-page",
            "main-art",
            "play-button",
            "data-button",
            "options-button",
            "credits-button",
            "link-button",
            "player-button",
            "main-deferred",
            "deferred-title",
            "deferred-body",
            "deferred-back"
        };

        private static readonly string[] OptionsElements =
        {
            "options-root",
            "options-page",
            "options-art",
            "bloom-row",
            "bloom-state",
            "candy-row",
            "candy-state",
            "background-row",
            "background-state",
            "resolution-row",
            "resolution-state",
            "sound-row",
            "sound-state",
            "music-row",
            "music-state",
            "control-row",
            "control-state",
            "control-hint",
            "ok-button",
            "cancel-button"
        };

        private static readonly string[][] ReferencePages =
        {
            new[] { "docs/assets/reference/UI_Main.png", "A&B-U/Assets/Art/Sprites/UI/Screens/UI_Main.png" },
            new[] { "docs/assets/reference/UI_Options.png", "A&B-U/Assets/Art/Sprites/UI/Screens/UI_Options.png" }
        };

        [Test]
        public void MainMenu_UxmlContainsEveryElementTheControllerBinds()
        {
            string uxml = File.ReadAllText(Path.Combine(FindRepoRoot(), MainUxmlFile));

            foreach (string element in MainElements)
                StringAssert.Contains("name=\"" + element + "\"", uxml, "Main menu element missing: " + element);

            StringAssert.Contains("src=\"Main.uss\"", uxml, "The main menu must use its stylesheet.");
            StringAssert.DoesNotContain("1280px", uxml, "The main menu must not use a fixed-width root.");
            StringAssert.DoesNotContain("960px", uxml, "The main menu must not use a fixed-height root.");
        }

        [Test]
        public void Options_UxmlContainsEveryRowAndBothPageButtons()
        {
            string uxml = File.ReadAllText(Path.Combine(FindRepoRoot(), OptionsUxmlFile));

            foreach (string element in OptionsElements)
                StringAssert.Contains("name=\"" + element + "\"", uxml, "Options element missing: " + element);

            StringAssert.Contains("src=\"Options.uss\"", uxml, "Options must use its stylesheet.");
            StringAssert.DoesNotContain("1280px", uxml, "Options must not use a fixed-width root.");
            StringAssert.DoesNotContain("960px", uxml, "Options must not use a fixed-height root.");
        }

        [Test]
        public void Controllers_WireControlModeResolutionSoundAndPersistence()
        {
            string options = File.ReadAllText(Path.Combine(FindRepoRoot(), OptionsControllerFile));
            StringAssert.Contains("PlayerFlightController", options, "Options must reach the flight controller.");
            StringAssert.Contains("SetControlMode", options, "Options must set PlayerFlightController.ControlMode.");
            StringAssert.Contains("SetSfxEnabled", options, "The Sound row must drive the SFX bus.");
            StringAssert.Contains("SetMusicEnabled", options, "The Music row must drive the music bus.");
            StringAssert.Contains("ApplyDisplayMode", options, "Resolution must reach the platform player.");
            StringAssert.Contains("Applied?.Invoke()", options, "OK must let GameFlowController persist the settings.");

            string flow = File.ReadAllText(
                Path.Combine(FindRepoRoot(), "A&B-U/Assets/Scripts/Presentation/GameFlowController.cs"));
            StringAssert.Contains("GameFlowState.MainMenu", flow, "Boot must enter the frozen main menu.");
            StringAssert.Contains("EnterMainMenu()", flow, "The flow must own the main-menu transition.");
            StringAssert.Contains("OnOptionsApplied", flow, "Options changes must persist through the flow.");
        }

        [Test]
        public void UssFiles_FillTheRootAndUseTheFrozenPageScale()
        {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), MainUssFile));
            StringAssert.Contains(".main-root", main);
            StringAssert.Contains("width: 100%;", main);
            StringAssert.Contains("height: 100%;", main);
            StringAssert.Contains(".main-page", main);
            StringAssert.Contains("width: 667px;", main);
            StringAssert.Contains("height: 535px;", main);
            StringAssert.Contains(".menu-button", main);
            StringAssert.DoesNotContain("1280", main, "The retired fixed hangar root must not survive.");

            string options = File.ReadAllText(Path.Combine(FindRepoRoot(), OptionsUssFile));
            StringAssert.Contains(".options-root", options);
            StringAssert.Contains("width: 100%;", options);
            StringAssert.Contains("height: 100%;", options);
            StringAssert.Contains(".options-page", options);
            StringAssert.Contains("width: 850px;", options);
            StringAssert.Contains("height: 490px;", options);
            StringAssert.Contains(".option-row", options);
            StringAssert.Contains(".option-row-state", options);
            StringAssert.DoesNotContain("1280", options, "The retired fixed hangar root must not survive.");
        }

        [Test]
        public void FrozenOptions_DefaultsMatchConfigAndStory()
        {
            var save = new PlayerSaveData();
            Assert.AreEqual(2, save.displayMode, "The frozen default resolution is 1920x1080.");
            Assert.AreEqual(PlayerControlMode.Keyboard, save.controlMode, "Keyboard is the #179 default.");
            Assert.IsTrue(save.bloomEnabled, "CONFIG[0] defaults to 1 (Bloom on).");
            Assert.IsFalse(save.candyEnabled, "CONFIG[1] defaults to 0 (Candy! off).");
            Assert.IsTrue(save.backgroundEnabled, "CONFIG[5] defaults to 1 (Background on).");
            Assert.IsTrue(save.soundEnabled, "CONFIG[3] defaults to 1 (Sound on).");
            Assert.IsTrue(save.musicEnabled, "CONFIG[4] defaults to 1 (Music on).");
        }

        [Test]
        public void PlayerSave_RoundTripsEveryOptionsValue()
        {
            var save = new PlayerSaveData
            {
                controlMode = PlayerControlMode.Mouse,
                displayMode = 3,
                bloomEnabled = false,
                candyEnabled = true,
                backgroundEnabled = false,
                soundEnabled = false,
                musicEnabled = false
            };

            PlayerSaveData loaded = PlayerSaveCodec.FromJson(PlayerSaveCodec.ToJson(save));
            Assert.AreEqual(PlayerControlMode.Mouse, loaded.controlMode);
            Assert.AreEqual(3, loaded.displayMode);
            Assert.IsFalse(loaded.bloomEnabled);
            Assert.IsTrue(loaded.candyEnabled);
            Assert.IsFalse(loaded.backgroundEnabled);
            Assert.IsFalse(loaded.soundEnabled);
            Assert.IsFalse(loaded.musicEnabled);
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

            return directory?.FullName ?? @"C:\Dev\ab-wt\181";
        }
    }
}
