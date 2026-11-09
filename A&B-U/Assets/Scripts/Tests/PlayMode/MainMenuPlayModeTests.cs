using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ABRevival.Core.Persistence;
using ABRevival.Core.Simulation;
using ABRevival.Presentation;
using ABRevival.Presentation.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// Boot-scene verification for the frozen UIMain/UI_Options pages (#181): the main-menu
    /// layer composition and entries, PLAY to Center, Options control-mode/resolution/sound
    /// persistence, the Data handoff, explicit deferred reasons, full-bleed layout at three
    /// output sizes and reference captures for landscape, phone and tablet profiles.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class MainMenuPlayModeTests
    {
        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, GameFlowController.DefaultSaveFileName);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            DeleteSave();
            if (AudioService.Instance != null) Object.Destroy(AudioService.Instance.gameObject);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            DeleteSave();
            if (AudioService.Instance != null) Object.Destroy(AudioService.Instance.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator MainMenu_BootsIntoUIMainAndPlayReachesCenter()
        {
            yield return PlayModeBoot.LoadMainMenu();

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            CenterController center = Object.FindAnyObjectByType<CenterController>();
            Assert.IsNotNull(flow);
            Assert.IsNotNull(menu, "Boot must host the dedicated main menu.");
            Assert.IsNotNull(center);
            Assert.AreEqual(GameFlowState.MainMenu, flow.State, "Boot must enter the frozen UIMain page first.");

            VisualElement root = ActiveRoot(menu);
            Assert.AreEqual(DisplayStyle.Flex, root.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, ActiveRoot(center).resolvedStyle.display,
                "Center must stay hidden behind the main menu.");

            AssertMainMenuLayers(root);
            AssertButtonArt(root, "play-button", "447_widget");
            AssertButtonArt(root, "data-button", "450_widget");
            AssertButtonArt(root, "options-button", "242_widget");
            AssertButtonArt(root, "credits-button", "453_widget");
            AssertButtonArt(root, "link-button", "20_widget");
            AssertButtonArt(root, "player-button", "458_widget");
            foreach (string topButton in new[] { "link-button", "player-button" })
                Assert.AreEqual(1f, root.Q(topButton).resolvedStyle.opacity, 0.001f,
                    "The live top control must be visible: " + topButton);

            PlayModeBoot.Click(root.Q<Button>("play-button"));
            yield return PlayModeBoot.WaitForCenter();
            Assert.AreEqual(GameFlowState.Center, flow.State, "PLAY must open Center.");
            Assert.AreEqual(DisplayStyle.None, root.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.Flex, ActiveRoot(center).resolvedStyle.display);

            PlayModeBoot.Click(ActiveRoot(center).Q<Button>("back-button"));
            for (int i = 0; i < 6; i++) yield return null;
            Assert.AreEqual(GameFlowState.MainMenu, flow.State, "Center BACK must return to the main menu.");
            Assert.AreEqual(DisplayStyle.Flex, root.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, ActiveRoot(center).resolvedStyle.display);
        }

        [UnityTest]
        public IEnumerator Options_ChangesControlResolutionAndSoundAndPersists()
        {
            yield return PlayModeBoot.LoadMainMenu();

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            PlayerFlightController flight = Object.FindAnyObjectByType<PlayerFlightController>();
            VisualElement mainRoot = ActiveRoot(menu);
            PlayModeBoot.Click(mainRoot.Q<Button>("options-button"));
            for (int i = 0; i < 10; i++) yield return null;

            OptionsController options = Object.FindAnyObjectByType<OptionsController>();
            Assert.IsNotNull(options, "Boot must host the dedicated Options screen.");
            Assert.AreEqual(GameFlowState.Options, flow.State, "OPTIONS must open the Options screen.");
            Assert.AreEqual(DisplayStyle.None, mainRoot.resolvedStyle.display);

            VisualElement root = ActiveRoot(options);
            Assert.AreEqual(DisplayStyle.Flex, root.resolvedStyle.display);
            AssertSprite(root.Q("options-art"), "UI_Options");
            AssertButtonArt(root, "ok-button", "276_widget");
            AssertButtonArt(root, "cancel-button", "273_widget");
            Assert.AreEqual("1920×1080", root.Q<Label>("resolution-state").text);
            Assert.AreEqual("On", root.Q<Label>("sound-state").text);
            Assert.AreEqual("Keyboard", root.Q<Label>("control-state").text);

            PlayModeBoot.Click(root.Q<Button>("resolution-row"));
            yield return null;
            Assert.AreEqual("Fullscreen", root.Q<Label>("resolution-state").text,
                "The resolution row must cycle through the frozen display modes.");

            PlayModeBoot.Click(root.Q<Button>("sound-row"));
            yield return null;
            Assert.AreEqual("Off", root.Q<Label>("sound-state").text);
            Assert.IsFalse(options.SoundEnabled);
            Assert.IsFalse(AudioService.Instance.SfxEnabled, "The Sound row must mute the SFX bus immediately.");
            StringAssert.Contains("Q switches", root.Q<Label>("control-hint").text);

            PlayModeBoot.Click(root.Q<Button>("control-row"));
            yield return null;
            Assert.AreEqual("Mouse", root.Q<Label>("control-state").text);
            Assert.AreEqual(PlayerControlMode.Mouse, options.ControlMode);

            PlayModeBoot.Click(root.Q<Button>("ok-button"));
            for (int i = 0; i < 8; i++) yield return null;
            Assert.AreEqual(GameFlowState.MainMenu, flow.State, "OK must apply and return to the main menu.");
            Assert.AreEqual(DisplayStyle.Flex, mainRoot.resolvedStyle.display);
            Assert.AreEqual(PlayerControlMode.Mouse, flight.ControlMode,
                "OK must push the control mode into PlayerFlightController.");

            PlayerSaveData saved = PlayerSaveCodec.LoadFromFile(SavePath);
            Assert.AreEqual(3, saved.displayMode, "OK must persist the display mode.");
            Assert.IsFalse(saved.soundEnabled, "OK must persist the Sound row.");
            Assert.AreEqual(PlayerControlMode.Mouse, saved.controlMode, "OK must persist the control mode.");

            SceneManager.LoadScene("Boot");
            yield return null;
            yield return PlayModeBoot.WaitForMainMenu();
            GameFlowController reloaded = Object.FindAnyObjectByType<GameFlowController>();
            PlayerFlightController reloadedFlight = Object.FindAnyObjectByType<PlayerFlightController>();
            Assert.AreEqual(GameFlowState.MainMenu, reloaded.State, "A returning player also boots into UIMain.");
            Assert.AreEqual(PlayerControlMode.Mouse, reloadedFlight.ControlMode,
                "The persisted control mode must be applied at boot.");
            Assert.IsFalse(AudioService.Instance.SfxEnabled, "The persisted Sound setting must be applied at boot.");
        }

        [UnityTest]
        public IEnumerator DataReachesRecordsAndCreditsPlayerAndLinkExplainTheirDeferral()
        {
            yield return PlayModeBoot.LoadMainMenu();

            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            VisualElement root = ActiveRoot(menu);

            PlayModeBoot.Click(root.Q<Button>("data-button"));
            for (int i = 0; i < 8; i++) yield return null;
            InfoController info = Object.FindAnyObjectByType<InfoController>();
            Assert.IsNotNull(info);
            Assert.AreEqual(GameFlowState.Info, flow.State, "DATA must open the delivered Info/Records screen.");
            Assert.IsTrue(info.ShowingRecords, "DATA must open the records tab.");
            Assert.AreEqual(DisplayStyle.Flex, info.GetComponent<UIDocument>().rootVisualElement
                .Q("records-page").resolvedStyle.display);

            PlayModeBoot.Click(info.GetComponent<UIDocument>().rootVisualElement.Q<Button>("back-button"));
            for (int i = 0; i < 6; i++) yield return null;
            Assert.AreEqual(GameFlowState.MainMenu, flow.State, "Info BACK must return to the main menu when opened from it.");
            Assert.AreEqual(DisplayStyle.Flex, root.resolvedStyle.display);

            PlayModeBoot.Click(root.Q<Button>("credits-button"));
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, root.Q("main-deferred").resolvedStyle.display);
            Assert.AreEqual("CREDITS", root.Q<Label>("deferred-title").text);
            StringAssert.Contains("ticket", root.Q<Label>("deferred-body").text.ToLowerInvariant());
            PlayModeBoot.Click(root.Q<Button>("deferred-back"));
            yield return null;
            Assert.AreEqual(DisplayStyle.None, root.Q("main-deferred").resolvedStyle.display);

            PlayModeBoot.Click(root.Q<Button>("player-button"));
            yield return null;
            Assert.AreEqual("PLAYER", root.Q<Label>("deferred-title").text);
            StringAssert.Contains("uiplayer", root.Q<Label>("deferred-body").text.ToLowerInvariant());

            PlayModeBoot.Click(root.Q<Button>("link-button"));
            yield return null;
            Assert.AreEqual("VARGAMES", root.Q<Label>("deferred-title").text);
            StringAssert.Contains("external", root.Q<Label>("deferred-body").text.ToLowerInvariant());
        }

        [UnityTest]
        public IEnumerator MainMenuAndOptions_FillTheRootAt1080p720pAnd16By10()
        {
            yield return PlayModeBoot.LoadMainMenu();

            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            var document = menu.GetComponent<UIDocument>();
            Assert.IsNotNull(document);
            PanelSettings settings = document.panelSettings;
            RenderTexture previous = settings.targetTexture;

            try
            {
                foreach (Vector2Int size in new[]
                         {
                             new Vector2Int(1920, 1080),
                             new Vector2Int(1280, 720),
                             new Vector2Int(1680, 1050)
                         })
                {
                    var target = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.ARGB32);
                    target.Create();
                    settings.targetTexture = target;

                    for (int i = 0; i < 8; i++) yield return null;

                    VisualElement root = document.rootVisualElement;
                    VisualElement screen = root.Q("main-root");
                    VisualElement backdrop = root.Q("main-backdrop");
                    VisualElement title = root.Q("main-title");
                    VisualElement ship = root.Q("main-ship");
                    Assert.IsNotNull(screen);
                    Assert.IsNotNull(backdrop);
                    Assert.IsNotNull(title);
                    Assert.IsNotNull(ship);
                    Assert.IsNull(root.Q("main-page"), "The baked UI_Main page wrapper must be retired at " + size);
                    Assert.IsNull(root.Q("main-art"), "The baked UI_Main_Hero crop must be retired at " + size);

                    Rect surface = screen.worldBound;
                    Rect rootBound = root.worldBound;
                    Debug.Log("[Main Layout] " + size + " surface=" + surface + " root=" + rootBound +
                              " title=" + title.worldBound + " ship=" + ship.worldBound);
                    Assert.AreEqual(rootBound.width, surface.width, 1f,
                        "The main root must fill the panel width at " + size);
                    Assert.AreEqual(rootBound.height, surface.height, 1f,
                        "The main root must fill the panel height at " + size);
                    Assert.Greater(surface.width, 1000f, "The main menu must use the full canvas at " + size);
                    Assert.Greater(surface.height, 700f, "The main menu must use the full canvas at " + size);
                    Assert.AreEqual(surface.width, backdrop.worldBound.width, 1f,
                        "The single starfield must cover the full width at " + size);
                    Assert.AreEqual(surface.height, backdrop.worldBound.height, 1f,
                        "The single starfield must cover the full height at " + size);
                    Assert.Greater(title.worldBound.width, 0f,
                        "The title must render at " + size);
                    Assert.Greater(title.worldBound.height, 0f,
                        "The title must render at " + size);
                    Assert.LessOrEqual(title.worldBound.yMin, surface.yMin + surface.height * 0.10f,
                        "The title must sit at the top at " + size);
                    Assert.AreEqual(surface.center.x, ship.worldBound.center.x, 2f,
                        "The ship must be centred horizontally at " + size);

                    float bottomControlsTop = float.MaxValue;
                    foreach (string button in new[] { "play-button", "data-button", "options-button", "credits-button" })
                        bottomControlsTop = Mathf.Min(bottomControlsTop, root.Q(button).worldBound.yMin);

                    Assert.GreaterOrEqual(ship.worldBound.yMin, title.worldBound.yMax,
                        "The ship must start below the title at " + size);
                    Assert.LessOrEqual(ship.worldBound.yMax, bottomControlsTop,
                        "The ship must end above the bottom controls at " + size);
                    Assert.GreaterOrEqual(ship.worldBound.center.y, title.worldBound.yMax,
                        "The ship centre must be below the title at " + size);
                    Assert.LessOrEqual(ship.worldBound.center.y, bottomControlsTop,
                        "The ship centre must be above the bottom controls at " + size);

                    foreach (string button in new[]
                             {
                                 "play-button", "data-button", "options-button",
                                 "credits-button", "link-button", "player-button"
                             })
                    {
                        Rect rect = root.Q(button).worldBound;
                        Assert.Greater(rect.width, 100f, button + " must keep a usable size at " + size);
                        Assert.Greater(rect.height, 30f, button + " must keep a usable size at " + size);
                        Assert.GreaterOrEqual(rect.xMin, surface.xMin, button + " must stay inside the root at " + size);
                        Assert.LessOrEqual(rect.xMax, surface.xMax + 1f, button + " must stay inside the root at " + size);
                        Assert.GreaterOrEqual(rect.yMin, surface.yMin, button + " must stay inside the root at " + size);
                        Assert.LessOrEqual(rect.yMax, surface.yMax + 1f, button + " must stay inside the root at " + size);
                    }

                    foreach (string topButton in new[] { "link-button", "player-button" })
                        Assert.AreEqual(1f, root.Q(topButton).resolvedStyle.opacity, 0.001f,
                            "The live top control must be visible at " + size + ": " + topButton);

                    AssertMainMenuGeometry(root, size.ToString());

                    PlayModeBoot.Click(root.Q<Button>("options-button"));
                    for (int i = 0; i < 8; i++) yield return null;
                    OptionsController options = Object.FindAnyObjectByType<OptionsController>();
                    VisualElement optionsRoot = ActiveRoot(options).Q("options-root");
                    VisualElement optionsPage = ActiveRoot(options).Q("options-page");
                    Rect optionsSurface = optionsRoot.worldBound;
                    Rect optionsPanel = ActiveRoot(options).worldBound;
                    Assert.AreEqual(optionsPanel.width, optionsSurface.width, 1f,
                        "Options must fill the panel width at " + size);
                    Assert.AreEqual(optionsPanel.height, optionsSurface.height, 1f,
                        "Options must fill the panel height at " + size);
                    Assert.GreaterOrEqual(optionsPage.worldBound.height, optionsSurface.height - 2f,
                        "The options page must cover the output height at " + size);
                    Assert.GreaterOrEqual(optionsPage.worldBound.width, optionsSurface.width * 0.99f,
                        "The options page must cover the output width at " + size);
                    Assert.LessOrEqual(optionsPage.worldBound.xMin, optionsSurface.xMin + 2f,
                        "Options must not letterbox horizontally at " + size);
                    Assert.LessOrEqual(optionsPage.worldBound.yMin, optionsSurface.yMin + 2f,
                        "Options must not letterbox vertically at " + size);

                    PlayModeBoot.Click(ActiveRoot(options).Q<Button>("cancel-button"));
                    for (int i = 0; i < 8; i++) yield return null;

                    target.Release();
                    Object.Destroy(target);
                }
            }
            finally
            {
                settings.targetTexture = previous;
            }
        }

        [UnityTest]
        public IEnumerator MainMenuAndOptions_RenderReferenceCapturesOutsideTheProject()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Capture requires a graphics device; run the PlayMode gate without -nographics.");

            yield return PlayModeBoot.LoadMainMenu();

            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            var document = menu.GetComponent<UIDocument>();
            Assert.IsNotNull(document);
            PanelSettings settings = document.panelSettings;
            RenderTexture previous = settings.targetTexture;
            string folder = Path.Combine(Path.GetTempPath(), "opencode");
            Directory.CreateDirectory(folder);
            RenderTexture target = null;

            try
            {
                foreach (Vector2Int size in new[]
                         {
                             new Vector2Int(1920, 1080),
                             new Vector2Int(750, 1334),
                             new Vector2Int(1170, 2532),
                             new Vector2Int(2048, 2732),
                             new Vector2Int(1024, 768)
                         })
                {
                    target = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.ARGB32);
                    target.Create();
                    settings.targetTexture = target;
                    for (int i = 0; i < 12; i++) yield return null;

                    VisualElement root = document.rootVisualElement;
                    AssertMainMenuLayers(root);
                    AssertMainMenuGeometry(root, size.ToString());
                    string mainPath = Path.Combine(folder, "main-menu-" + size.x + "x" + size.y + ".png");
                    WriteCapture(target, mainPath);
                    AssertCaptureHasContent(mainPath, "Main menu " + size);

                    target.Release();
                    Object.Destroy(target);
                    target = null;
                }

                target = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
                target.Create();
                settings.targetTexture = target;
                for (int i = 0; i < 12; i++) yield return null;

                VisualElement menuRoot = document.rootVisualElement;
                PlayModeBoot.Click(menuRoot.Q<Button>("options-button"));
                for (int i = 0; i < 12; i++) yield return null;
                OptionsController options = Object.FindAnyObjectByType<OptionsController>();
                VisualElement optionsRoot = ActiveRoot(options);
                AssertSprite(optionsRoot.Q("options-art"), "UI_Options");
                string optionsPath = Path.Combine(folder, "options-1920x1080.png");
                WriteCapture(target, optionsPath);
                AssertCaptureHasContent(optionsPath, "Options");

                target.Release();
                Object.Destroy(target);
                target = null;
                Debug.Log("[Main Menu Capture] folder=" + folder + " options=" + optionsPath);
            }
            finally
            {
                if (target != null)
                {
                    target.Release();
                    Object.Destroy(target);
                }

                settings.targetTexture = previous;
                RenderTexture.active = null;
            }
        }

        private static void WriteCapture(RenderTexture target, string path)
        {
            if (File.Exists(path)) File.Delete(path);
            RenderTexture.active = target;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            RenderTexture.active = null;
            File.WriteAllBytes(path, image.EncodeToPNG());
            Object.Destroy(image);
        }

        private static void AssertCaptureHasContent(string path, string label)
        {
            var check = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(check.LoadImage(File.ReadAllBytes(path)));
            Color32[] pixels = check.GetPixels32();
            Object.Destroy(check);

            int nonBlack = 0;
            var colors = new HashSet<int>();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 pixel = pixels[i];
                if (pixel.r > 16 || pixel.g > 16 || pixel.b > 16) nonBlack++;
                if (colors.Count < 4096) colors.Add((pixel.r << 16) | (pixel.g << 8) | pixel.b);
            }

            Assert.Greater(nonBlack, pixels.Length / 200, label + " rendered almost black.");
            Assert.Greater(colors.Count, 32, label + " rendered no extracted UI palette.");
        }

        private static VisualElement ActiveRoot(Component component)
        {
            var document = component != null ? component.GetComponent<UIDocument>() : null;
            return document != null ? document.rootVisualElement : null;
        }

        private static void AssertMainMenuLayers(VisualElement root)
        {
            AssertSprite(root.Q("main-backdrop"), "ui_0005");
            AssertSprite(root.Q("main-title"), "UI_Main_Title");
            AssertSprite(root.Q("main-ship"), "UI_Main_Ship");
            Assert.IsNull(root.Q("main-page"), "The baked UI_Main page wrapper is retired.");
            Assert.IsNull(root.Q("main-art"), "The baked UI_Main_Hero crop is retired.");
            foreach (Image image in root.Query<Image>("sprite-art").ToList())
                Assert.IsFalse(image.sprite != null && image.sprite.name.Contains("UI_Main_Hero"),
                    "UI_Main_Hero must not be drawn by the main menu.");
        }

        private static void AssertMainMenuGeometry(VisualElement root, string label)
        {
            VisualElement surface = root.Q("main-root");
            VisualElement title = root.Q("main-title");
            VisualElement ship = root.Q("main-ship");
            Assert.IsNotNull(surface, "Main root is missing at " + label);
            Assert.IsNotNull(title, "Main title is missing at " + label);
            Assert.IsNotNull(ship, "Main ship is missing at " + label);

            Assert.Greater(ship.worldBound.width, 0f, "Ship width must be positive at " + label);
            Assert.Greater(ship.worldBound.height, 0f, "Ship height must be positive at " + label);
            Assert.GreaterOrEqual(title.worldBound.xMin, surface.worldBound.xMin - 1f,
                "The title must stay inside the root at " + label);
            Assert.LessOrEqual(title.worldBound.xMax, surface.worldBound.xMax + 1f,
                "The title must stay inside the root at " + label);

            float bottomControlsTop = float.MaxValue;
            foreach (string button in new[] { "play-button", "data-button", "options-button", "credits-button" })
                bottomControlsTop = Mathf.Min(bottomControlsTop, root.Q(button).worldBound.yMin);

            float bandHeight = bottomControlsTop - title.worldBound.yMax;
            float gapTop = ship.worldBound.yMin - title.worldBound.yMax;
            float gapBottom = bottomControlsTop - ship.worldBound.yMax;
            Assert.AreEqual(gapTop, gapBottom, bandHeight * 0.01f,
                "Ship top and bottom gaps must match within 1% of the band at " + label);

            Rect shipBounds = ship.worldBound;
            shipBounds.width = Mathf.Max(0f, shipBounds.width - 0.01f);
            foreach (string topButton in new[] { "link-button", "player-button" })
                Assert.IsFalse(shipBounds.Overlaps(root.Q(topButton).worldBound),
                    "The ship must not overlap " + topButton + " at " + label);
        }

        private static void AssertSprite(VisualElement element, string spriteName)
        {
            Assert.IsNotNull(element, "The art surface is missing: " + spriteName);
            Image image = element.Q<Image>("sprite-art");
            Assert.IsNotNull(image, "The art surface must render through an Image child.");
            Assert.IsNotNull(image.sprite, "The art surface has no sprite.");
            StringAssert.Contains(spriteName, image.sprite.name,
                "The surface must render the original " + spriteName + " export.");
            Background background = element.style.backgroundImage.value;
            Assert.IsTrue(background.sprite == null && background.texture == null,
                "The art surface must not use the atlased background-image path.");
        }

        private static void AssertButtonArt(VisualElement root, string name, string spriteName)
        {
            VisualElement element = root.Q(name);
            Assert.IsNotNull(element, name + " is missing.");
            Image image = element.Q<Image>("sprite-art");
            Assert.IsNotNull(image, name + " has no rendered sprite-art Image.");
            Assert.IsNotNull(image.sprite, name + " has no sprite.");
            StringAssert.Contains(spriteName, image.sprite.name,
                name + " must render the original " + spriteName + " art.");
        }

        private static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(SavePath + ".corrupt")) File.Delete(SavePath + ".corrupt");
            if (File.Exists(SavePath + ".tmp")) File.Delete(SavePath + ".tmp");
        }
    }
}
