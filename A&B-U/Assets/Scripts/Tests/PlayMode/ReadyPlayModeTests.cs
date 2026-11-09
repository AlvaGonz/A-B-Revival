using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ABRevival.Core.Data;
using ABRevival.Core.Persistence;
using ABRevival.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// Boot-scene verification for the Ready/Briefing handoff (#178): validated Player/AI
    /// loadout, frozen missionWords, talkBefore + opening cinematic launch, Back cancel,
    /// full-bleed layout at three sizes and a 1920x1080 review capture.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class ReadyPlayModeTests
    {
        private const int WaitFrames = 900;

        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, GameFlowController.DefaultSaveFileName);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            DeleteSave();
            if (ABRevival.Presentation.Audio.AudioService.Instance != null)
                Object.Destroy(ABRevival.Presentation.Audio.AudioService.Instance.gameObject);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            DeleteSave();
            if (ABRevival.Presentation.Audio.AudioService.Instance != null)
                Object.Destroy(ABRevival.Presentation.Audio.AudioService.Instance.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Ready_ShowsBriefingAndLaunchesWithSelectedCraftAndCompanions()
        {
            yield return LoadBoot();

            CenterController center = Object.FindAnyObjectByType<CenterController>();
            VisualElement centerRoot = ActiveRoot(center);
            Assert.IsTrue(center.AssignCompanion(2));
            Assert.IsTrue(center.AssignCompanion(3));
            yield return null;

            int player = center.PlayerCraftIndex;
            int companion0 = center.Companions.CraftIndex(0);
            int companion1 = center.Companions.CraftIndex(1);
            Assert.AreEqual(2, companion0);
            Assert.AreEqual(3, companion1);

            yield return OpenReady();

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            ReadyController ready = Object.FindAnyObjectByType<ReadyController>();
            VisualElement root = ActiveRoot(ready);
            Assert.AreEqual(GameFlowState.Ready, flow.State);
            Assert.AreEqual(DisplayStyle.None, centerRoot.resolvedStyle.display, "Center must hide behind Ready.");
            Assert.AreEqual(DisplayStyle.Flex, root.resolvedStyle.display);

            Assert.AreEqual("BATTLE-1", root.Q<Label>("ready-title").text);
            AssertFont(root, "ready-title", "font_0007_SF_TransRobotics");
            AssertFont(root, "briefing-words", "font_0007_SF_TransRobotics");
            AssertFont(root, "loadout-player", "font_0081_Arial");
            AssertFont(root, "loadout-status", "font_0002_sans");
            StringAssert.Contains("Wang is fighting", root.Q<Label>("briefing-words").text,
                "The map page must print the frozen missionWords[STORY] summary.");
            Assert.AreEqual(center.CraftName(player) + "  [" + center.StatsFor(player).cls + "]",
                root.Q<Label>("loadout-player").text);
            StringAssert.Contains(center.CraftName(2), ready.CompanionLabel(0));
            StringAssert.Contains(center.CraftName(3), ready.CompanionLabel(1));
            Assert.AreEqual("-", root.Q<Label>("companion-2").text);
            Assert.AreEqual("-", root.Q<Label>("companion-3").text);
            Assert.AreEqual("-", root.Q<Label>("companion-4").text);
            Assert.IsTrue(ready.CanLaunch);
            Assert.IsTrue(root.Q<Button>("launch-button").enabledSelf);

            Click(root.Q<Button>("launch-button"));
            for (int i = 0; i < 8; i++) yield return null;

            Assert.AreEqual(GameFlowState.Briefing, flow.State,
                "Launch must start talkBefore and the opening cinematic before combat.");
            Assert.AreEqual(DisplayStyle.None, root.resolvedStyle.display, "Ready must hide during the briefing.");
            Assert.IsNotNull(flow.ActiveLoadout, "The launch must carry the Ready loadout.");
            Assert.AreEqual(player, flow.ActiveLoadout.PlayerCraftIndex,
                "The mission must receive the selected Player craft.");
            CollectionAssert.AreEqual(new[] { companion0, companion1 },
                new List<int>(flow.ActiveLoadout.CompanionCraftIndices),
                "The mission must receive the assigned AI companions.");

            flow.SkipBriefing();
            yield return WaitForState(flow, GameFlowState.Mission);
            Assert.IsNotNull(flow.Runner, "The mission runner must start after the briefing.");
            Assert.AreEqual(player, flow.ActiveLoadout.PlayerCraftIndex);
            CollectionAssert.AreEqual(new[] { companion0, companion1 },
                new List<int>(flow.ActiveLoadout.CompanionCraftIndices));
        }

        [UnityTest]
        public IEnumerator Ready_BackCancelsToCenterWithoutLaunching()
        {
            yield return LoadBoot();

            CenterController center = Object.FindAnyObjectByType<CenterController>();
            Assert.IsTrue(center.AssignCompanion(2));
            yield return null;
            string saveBefore = File.ReadAllText(SavePath);

            yield return OpenReady();
            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            ReadyController ready = Object.FindAnyObjectByType<ReadyController>();
            Click(ActiveRoot(ready).Q<Button>("back-button"));
            for (int i = 0; i < 6; i++) yield return null;

            Assert.AreEqual(GameFlowState.Center, flow.State, "Ready BACK must return to Center.");
            Assert.IsNull(flow.Runner, "Cancelling the handoff must not start a mission.");
            Assert.IsNull(flow.ActiveLoadout, "Cancelling the handoff must not publish a loadout.");
            Assert.AreEqual(DisplayStyle.Flex, ActiveRoot(center).resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, ActiveRoot(ready).resolvedStyle.display);
            Assert.AreEqual(saveBefore, File.ReadAllText(SavePath),
                "Ready is a handoff only; Back must not mutate the save.");
        }

        [UnityTest]
        public IEnumerator Ready_IsDisabledWithoutAValidPlayerCraft()
        {
            SeedGunOnlyCraft();
            yield return LoadBoot();

            CenterController center = Object.FindAnyObjectByType<CenterController>();
            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            VisualElement root = ActiveRoot(center);
            Assert.IsFalse(center.CanLaunch, "A hull-less blueprint must not be launchable.");
            Button readyButton = root.Q<Button>("ready-button");
            Assert.IsNotNull(readyButton);
            Assert.IsFalse(readyButton.enabledSelf, "READY must be disabled when no valid Player craft exists.");

            Click(readyButton);
            for (int i = 0; i < 6; i++) yield return null;
            Assert.AreEqual(GameFlowState.Center, flow.State,
                "A disabled READY must not open the briefing, matching UIChooseFlyAI.afterFadeIn.");
        }

        [UnityTest]
        public IEnumerator Ready_FillsTheRootAt1080p720pAnd16By10()
        {
            yield return LoadBoot();
            yield return OpenReady();

            ReadyController ready = Object.FindAnyObjectByType<ReadyController>();
            var document = ready.GetComponent<UIDocument>();
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
                    VisualElement readyRoot = root.Q("ready-root");
                    VisualElement screen = root.Q("ready-screen");
                    VisualElement window = root.Q("ready-window");
                    VisualElement header = root.Q("ready-header");
                    VisualElement footer = root.Q("ready-footer");
                    VisualElement loadout = root.Q("loadout-panel");
                    VisualElement briefing = root.Q("briefing-panel");
                    VisualElement words = root.Q("briefing-words");
                    Button back = root.Q<Button>("back-button");
                    Button launch = root.Q<Button>("launch-button");
                    Assert.IsNotNull(readyRoot, "Ready root is missing.");
                    Assert.IsNotNull(screen, "Ready screen is missing.");
                    Assert.IsNotNull(window, "Ready window is missing.");
                    Assert.IsNotNull(header, "Ready header is missing.");
                    Assert.IsNotNull(footer, "Ready footer is missing.");
                    Assert.IsNotNull(loadout, "Loadout panel is missing.");
                    Assert.IsNotNull(briefing, "Briefing panel is missing.");
                    Assert.IsNotNull(words, "Briefing words are missing.");
                    Assert.IsNotNull(back, "Ready BACK is missing.");
                    Assert.IsNotNull(launch, "Ready launch button is missing.");

                    Rect surface = readyRoot.worldBound;
                    Debug.Log("[Ready Layout] " + size + " surface=" + surface + " window=" + window.worldBound +
                              " loadout=" + loadout.worldBound + " briefing=" + briefing.worldBound +
                              " back=" + back.worldBound + " launch=" + launch.worldBound);
                    Assert.Greater(surface.width, 0f, "The root must have layout width at " + size);
                    Assert.Greater(surface.height, 0f, "The root must have layout height at " + size);
                    Assert.AreEqual(surface.width, screen.worldBound.width, 1f,
                        "ready-screen must fill the root width at " + size);
                    Assert.AreEqual(surface.height, screen.worldBound.height, 1f,
                        "ready-screen must fill the root height at " + size);
                    Assert.AreEqual(surface.width * 0.72f, window.worldBound.width, 4f,
                        "The briefing window must keep the full-bleed proportion at " + size);
                    Assert.AreEqual(surface.height * 0.73f, window.worldBound.height, 4f,
                        "The briefing window must keep the full-bleed proportion at " + size);
                    Assert.LessOrEqual(Mathf.Abs(window.worldBound.xMin - (surface.xMin + surface.width * 0.14f)), 4f,
                        "The window must stay centered at " + size);
                    Assert.LessOrEqual(Mathf.Abs(header.worldBound.yMin - (window.worldBound.yMin + 14f)), 4f,
                        "The header must anchor to the window top at " + size);
                    Assert.LessOrEqual(Mathf.Abs(footer.worldBound.yMax - (window.worldBound.yMax - 16f)), 4f,
                        "The footer must anchor to the window bottom at " + size);
                    Assert.LessOrEqual(Mathf.Abs(loadout.worldBound.xMin - (window.worldBound.xMin + 22f)), 4f,
                        "The loadout must anchor to the left at " + size);
                    Assert.LessOrEqual(Mathf.Abs(briefing.worldBound.xMax - (window.worldBound.xMax - 22f)), 4f,
                        "The briefing must anchor to the right at " + size);
                    Assert.Greater(words.worldBound.height, 90f, "The briefing text must keep usable height at " + size);
                    Assert.Greater(back.worldBound.width, 220f, "BACK must stay tappable at " + size);
                    Assert.Greater(launch.worldBound.width, 140f, "Launch must stay tappable at " + size);

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
        public IEnumerator Ready_Renders1920x1080CaptureOutsideTheProject()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Capture requires a graphics device; run the PlayMode gate without -nographics.");

            yield return LoadBoot();
            CenterController center = Object.FindAnyObjectByType<CenterController>();
            Assert.IsTrue(center.AssignCompanion(2));
            Assert.IsTrue(center.AssignCompanion(3));
            yield return null;
            yield return OpenReady();

            ReadyController ready = Object.FindAnyObjectByType<ReadyController>();
            var document = ready.GetComponent<UIDocument>();
            Assert.IsNotNull(document);
            VisualElement root = document.rootVisualElement;
            PanelSettings settings = document.panelSettings;
            RenderTexture previous = settings.targetTexture;
            var target = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            target.Create();

            try
            {
                settings.targetTexture = target;
                for (int i = 0; i < 12; i++) yield return null;

                Assert.AreEqual("BATTLE-1", root.Q<Label>("ready-title").text);
                StringAssert.Contains("Wang is fighting", root.Q<Label>("briefing-words").text);
                AssertImageArt(root, "ready-backdrop", "ui_0312_hangar_background");
                AssertImageArt(root, "ready-loading", "306_widget");
                AssertImageArt(root, "ready-title-plate", "ui_0107_hangar_header");
                AssertImageArt(root, "launch-button", "310_widget");
                AssertImageArt(root, "back-button", "ui_0324_hangar_button_back_to_center");

                string folder = Path.Combine(Path.GetTempPath(), "opencode");
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, "ready-briefing-1920x1080.png");
                if (File.Exists(path)) File.Delete(path);

                RenderTexture.active = target;
                var image = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                image.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.Destroy(image);

                AssertCaptureHasContent(path);
                Debug.Log("[Ready Capture] path=" + path);
            }
            finally
            {
                settings.targetTexture = previous;
                RenderTexture.active = null;
                target.Release();
                Object.Destroy(target);
            }
        }

        private static IEnumerator OpenReady()
        {
            CenterController center = Object.FindAnyObjectByType<CenterController>();
            Assert.IsNotNull(center);
            Click(ActiveRoot(center).Q<Button>("ready-button"));
            for (int i = 0; i < 8; i++) yield return null;

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow);
            Assert.AreEqual(GameFlowState.Ready, flow.State, "Center READY must open the handoff screen.");
            Assert.IsNotNull(Object.FindAnyObjectByType<ReadyController>(), "Boot must host the Ready screen.");
        }

        private static IEnumerator LoadBoot()
        {
            SceneManager.LoadScene("Boot");
            yield return null;

            for (int i = 0; i < WaitFrames; i++)
            {
                CenterController center = Object.FindAnyObjectByType<CenterController>();
                if (center != null && center.FleetCount > 0)
                {
                    UIDocument document = center.GetComponent<UIDocument>();
                    if (document != null && document.rootVisualElement != null &&
                        document.rootVisualElement.Q("center-root") != null)
                    {
                        yield return null;
                        yield return null;
                        yield break;
                    }
                }

                yield return null;
            }

            Assert.Fail("Boot did not reach the Center screen.");
        }

        private static IEnumerator WaitForState(GameFlowController flow, GameFlowState expected)
        {
            for (int i = 0; i < WaitFrames; i++)
            {
                if (flow.State == expected) yield break;
                yield return null;
            }

            Assert.Fail($"Flow did not reach {expected}; current state {flow.State}.");
        }

        private static VisualElement ActiveRoot(Component component)
        {
            var document = component != null ? component.GetComponent<UIDocument>() : null;
            return document != null ? document.rootVisualElement : null;
        }

        private static void AssertImageArt(VisualElement root, string name, string spriteName)
        {
            VisualElement element = root.Q(name);
            Assert.IsNotNull(element, name + " is missing.");
            Image image = element.Q<Image>("sprite-art");
            Assert.IsNotNull(image, name + " must render through an Image child.");
            Assert.IsNotNull(image.sprite, name + " has no sprite.");
            StringAssert.Contains(spriteName, image.sprite.name,
                name + " must render the original " + spriteName + " export.");

            Background background = element.style.backgroundImage.value;
            Assert.IsTrue(background.sprite == null && background.texture == null,
                name + " must not use the background-image path.");
        }

        private static void AssertFont(VisualElement root, string name, string expected)
        {
            Label label = root.Q<Label>(name);
            Assert.IsNotNull(label, name + " is missing.");
            FontDefinition definition = label.resolvedStyle.unityFontDefinition;
            string font = definition.font != null
                ? definition.font.name
                : definition.fontAsset != null ? definition.fontAsset.name : null;
            Assert.IsNotNull(font, name + " resolved to the Unity default font.");
            StringAssert.Contains(expected, font, name + " must use the original " + expected + " face.");
        }

        private static void AssertCaptureHasContent(string path)
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

            Assert.Greater(nonBlack, pixels.Length / 200, "Ready rendered almost black.");
            Assert.Greater(colors.Count, 32, "Ready rendered no extracted UI palette.");
        }

        private static void Click(Button button)
        {
            Assert.IsNotNull(button);
            MethodInfo simulate = typeof(Clickable).GetMethod(
                "SimulateSingleClick", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(simulate, "UI Toolkit Clickable.SimulateSingleClick moved.");
            simulate.Invoke(button.clickable, new object[] { null, 100 });
        }

        private static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(SavePath + ".corrupt")) File.Delete(SavePath + ".corrupt");
            if (File.Exists(SavePath + ".tmp")) File.Delete(SavePath + ".tmp");
        }

        private static void SeedGunOnlyCraft()
        {
            var payload = new PlayerSaveData();
            var blueprint = new BlueprintSaveData { craftName = "No Hull", hullName = string.Empty };
            blueprint.mountedParts.Add(new MountedPartSaveData
            {
                partId = 71,
                category = PartCategory.Gun,
                health = 10f,
                maxHealth = 10f
            });
            payload.blueprints.Add(blueprint);
            PlayerSaveCodec.SaveToFile(SavePath, payload);
        }
    }
}
