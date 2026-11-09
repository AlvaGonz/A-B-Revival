using System.Collections;
using System.IO;
using System.Reflection;
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
    /// Boot-scene verification for Center (UIChooseFlyAI): real blueprint preview, F4ShowData
    /// stats, Player/AI assignment, companion grades, Reset/Best, pagination, persistence,
    /// navigation placeholders and full-bleed layout at three output sizes.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class CenterPlayModeTests
    {
        private const int WaitFrames = 600;

        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, GameFlowController.DefaultSaveFileName);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(SavePath + ".corrupt")) File.Delete(SavePath + ".corrupt");
            if (File.Exists(SavePath + ".tmp")) File.Delete(SavePath + ".tmp");
            if (ABRevival.Presentation.Audio.AudioService.Instance != null)
                Object.Destroy(ABRevival.Presentation.Audio.AudioService.Instance.gameObject);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(SavePath + ".corrupt")) File.Delete(SavePath + ".corrupt");
            if (File.Exists(SavePath + ".tmp")) File.Delete(SavePath + ".tmp");
            if (ABRevival.Presentation.Audio.AudioService.Instance != null)
                Object.Destroy(ABRevival.Presentation.Audio.AudioService.Instance.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Center_SelectsAssignsGradesResetsBestsAndPersists()
        {
            yield return LoadCenter();

            CenterController center = Object.FindAnyObjectByType<CenterController>();
            VisualElement root = ActiveRoot(center);
            Assert.IsNotNull(center);
            Assert.IsNotNull(root);

            Assert.AreEqual("Center", root.Q<Label>("center-title").text);
            Assert.AreEqual("BATTLE-1", root.Q<Label>("battle-label").text);
            Assert.AreEqual("Michael's", root.Q<Label>("stat-name").text);
            Assert.AreEqual("Michael's", root.Q<Label>("preview-name").text);
            Assert.GreaterOrEqual(center.PreviewPartCount, 4, "The player blueprint preview must render body, wings, engine and gun.");
            VisualElement bodyPart = root.Q("preview-part-0");
            Assert.IsNotNull(bodyPart, "The body preview part is missing.");
            Image bodyArt = bodyPart.Q<Image>("sprite-art");
            Assert.IsNotNull(bodyArt, "The body preview must render through an Image child.");
            Sprite bodySprite = bodyArt.sprite;
            Assert.IsNotNull(bodySprite, "The body preview has no sprite.");
            Assert.Greater(bodySprite.rect.width, 50f,
                "The preview must use the 2x in-game mount art, not the 1x shop thumbnail.");
            Assert.IsTrue(bodyPart.style.backgroundImage.value.sprite == null,
                "The preview must not sample its atlas sprite through background-image.");
            Assert.IsTrue(center.PreviewUsesMountArt,
                "Every mounted preview part must resolve to PartMounts art; no thumbnail mixing.");
            for (int i = 0; i < bodyPart.parent.childCount; i++)
            {
                Image partArt = bodyPart.parent[i].Q<Image>("sprite-art");
                Assert.IsNotNull(partArt, "Preview part " + bodyPart.parent[i].name + " has no Image art.");
                Assert.IsNotNull(partArt.sprite);
                Assert.Greater(partArt.sprite.rect.height, 30f,
                    "Preview part " + bodyPart.parent[i].name + " is a 1x thumbnail.");
            }

            Assert.GreaterOrEqual(center.PreviewFillFraction, 0.60f,
                "The assembled craft must fill at least 60% of the usable preview area.");
            Assert.LessOrEqual(center.PreviewFillFraction, 0.72f,
                "The assembled craft must not overshoot the 2011 preview density.");
            Debug.Log($"[Center Preview] bounds={center.PreviewBoundsSize.x:0.0}x{center.PreviewBoundsSize.y:0.0} " +
                      $"fill={center.PreviewFillFraction:P1}");
            Assert.IsTrue(center.IsInUse(center.SelectedCraftIndex), "The Player craft starts selected and in use.");
            Assert.AreEqual(DisplayStyle.Flex, root.Q("inuse-label").resolvedStyle.display);

            Assert.Greater(center.FleetCount, 8, "The seeded fleet must exercise pagination.");
            Assert.AreEqual("1 / 2", root.Q<Label>("page-label").text);
            center.NextPage();
            yield return null;
            Assert.AreEqual(1, center.PageIndex);
            Assert.AreEqual("2 / 2", root.Q<Label>("page-label").text);
            center.FirstPage();
            yield return null;
            Assert.AreEqual(0, center.PageIndex);

            center.SelectCraft(1);
            yield return null;
            Assert.AreEqual(center.CraftName(1), root.Q<Label>("preview-name").text);
            Assert.AreEqual(center.StatsFor(1).cls, root.Q<Label>("stat-class").text);
            Assert.AreEqual(DisplayStyle.None, root.Q("inuse-label").resolvedStyle.display,
                "A spare craft is not in use.");

            Assert.IsTrue(center.AssignPlayer(1));
            yield return null;
            Assert.AreEqual(1, center.PlayerCraftIndex);
            Assert.AreEqual(DisplayStyle.Flex, root.Q("inuse-label").resolvedStyle.display);
            Assert.AreEqual("1", root.Q("craft-row-1").Q<Button>("player-cell").text);
            Assert.AreEqual("0", root.Q("craft-row-0").Q<Button>("player-cell").text);

            Assert.IsTrue(center.AssignCompanion(2));
            Assert.IsTrue(center.AssignCompanion(2));
            yield return null;
            Assert.AreEqual(2, center.Companions.CountOf(2));
            Assert.AreEqual("2", root.Q("craft-row-2").Q<Button>("ai-cell").text,
                "The AI column is the numeric assignment count, not a label.");
            Assert.AreEqual(DisplayStyle.Flex, root.Q("companion-slots").resolvedStyle.display);
            Assert.AreEqual(center.StatsFor(2).cls, root.Q<Button>("grade-0").text,
                "The round grade button must show the companion's ship quality.");
            Assert.AreEqual(center.StatsFor(2).cls, root.Q<Button>("grade-1").text,
                "A duplicated companion occupies a second grade slot.");

            Assert.IsTrue(File.Exists(SavePath), "Assignments must autosave through the existing payload.");
            PlayerSaveData saved = PlayerSaveCodec.LoadFromFile(SavePath);
            Assert.AreEqual(1, saved.playerBlueprintIndex);
            Assert.AreEqual(2, CountCompanionCopies(saved, 2));

            yield return ReloadCenter();
            center = Object.FindAnyObjectByType<CenterController>();
            root = ActiveRoot(center);
            Assert.AreEqual(1, center.PlayerCraftIndex, "The Player pick must survive a reload.");
            Assert.AreEqual(2, center.Companions.CountOf(2), "The duplicate AI count must survive a reload.");
            Assert.AreEqual(DisplayStyle.Flex, root.Q("companion-slots").resolvedStyle.display);
            Assert.AreEqual("2", root.Q("craft-row-2").Q<Button>("ai-cell").text);
            Assert.AreEqual(center.StatsFor(2).cls, root.Q<Button>("grade-0").text);
            Assert.AreEqual(center.StatsFor(2).cls, root.Q<Button>("grade-1").text);

            center.ResetCompanions();
            yield return null;
            Assert.AreEqual(0, center.Companions.AssignedCount);
            Assert.AreEqual(DisplayStyle.None, root.Q("companion-slots").resolvedStyle.display,
                "Grade buttons hide when no AI companion is selected.");

            center.FillBestCompanions();
            yield return null;
            Assert.AreEqual(5, center.Companions.AssignedCount, "Best must fill all five slots.");
            for (int slot = 0; slot < 5; slot++)
            {
                Assert.IsTrue(center.Companions.IsAssigned(slot));
                int craft = center.Companions.CraftIndex(slot);
                Assert.AreEqual(center.StatsFor(craft).cls, root.Q<Button>("grade-" + slot).text);
            }
        }

        [UnityTest]
        public IEnumerator Center_NavigationButtonsOpenPlaceholdersAndReturn()
        {
            yield return LoadCenter();

            CenterController center = Object.FindAnyObjectByType<CenterController>();
            VisualElement root = ActiveRoot(center);
            Assert.IsNotNull(center);
            Assert.IsNotNull(root);

            // BACK is the real main-menu return (#181); HELP still uses the placeholder shell.
            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Click(root.Q<Button>("back-button"));
            yield return null;
            Assert.AreEqual(GameFlowState.MainMenu, flow.State, "Center BACK must return to the main menu.");
            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            Assert.IsNotNull(menu);
            Assert.AreEqual(DisplayStyle.Flex, menu.GetComponent<UIDocument>().rootVisualElement.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, root.resolvedStyle.display);

            PlayModeBoot.StartGame();
            yield return PlayModeBoot.WaitForCenter();

            Click(root.Q<Button>("help-button"));
            yield return null;
            Assert.AreEqual("HELP", center.PlaceholderTitle, "HELP must reach its placeholder target.");
            Assert.AreEqual(DisplayStyle.None, root.Q("center-screen").resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.Flex, root.Q("center-placeholder").resolvedStyle.display);

            Click(root.Q<Button>("placeholder-back"));
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, root.Q("center-screen").resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, root.Q("center-placeholder").resolvedStyle.display);

            // READY is a real handoff screen (#178), not a placeholder.
            ReadyController ready = Object.FindAnyObjectByType<ReadyController>();
            Assert.IsNotNull(flow);
            Assert.IsNotNull(ready, "Boot must host the dedicated Ready screen.");
            Assert.IsTrue(center.CanLaunch, "The seeded Player craft must be launchable.");
            Assert.IsTrue(root.Q<Button>("ready-button").enabledSelf,
                "READY must be enabled when the Player craft is valid.");
            Click(root.Q<Button>("ready-button"));
            yield return null;
            Assert.AreEqual(GameFlowState.Ready, flow.State);
            Assert.AreEqual(DisplayStyle.None, root.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.Flex, ready.GetComponent<UIDocument>().rootVisualElement.resolvedStyle.display);

            Click(ready.GetComponent<UIDocument>().rootVisualElement.Q<Button>("back-button"));
            yield return null;
            Assert.AreEqual(GameFlowState.Center, flow.State);
            Assert.AreEqual(DisplayStyle.Flex, root.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, ready.GetComponent<UIDocument>().rootVisualElement.resolvedStyle.display);

            // INFO is a real read-only screen (#177), not a placeholder.
            InfoController info = Object.FindAnyObjectByType<InfoController>();
            Assert.IsNotNull(flow);
            Assert.IsNotNull(info, "Boot must host the dedicated Info screen.");
            Click(root.Q<Button>("info-button"));
            yield return null;
            Assert.AreEqual(GameFlowState.Info, flow.State);
            Assert.AreEqual(DisplayStyle.None, root.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.Flex, info.GetComponent<UIDocument>().rootVisualElement.resolvedStyle.display);

            Click(info.GetComponent<UIDocument>().rootVisualElement.Q<Button>("back-button"));
            yield return null;
            Assert.AreEqual(GameFlowState.Center, flow.State);
            Assert.AreEqual(DisplayStyle.Flex, root.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, info.GetComponent<UIDocument>().rootVisualElement.resolvedStyle.display);

            // FACTORY is a real screen (#175), not a placeholder.
            FactoryController factory = Object.FindAnyObjectByType<FactoryController>();
            Assert.IsNotNull(flow);
            Assert.IsNotNull(factory, "Boot must host the dedicated Factory screen.");
            Click(root.Q<Button>("factory-button"));
            yield return null;
            Assert.AreEqual(GameFlowState.Factory, flow.State);
            Assert.AreEqual(DisplayStyle.None, root.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.Flex, factory.GetComponent<UIDocument>().rootVisualElement.resolvedStyle.display);

            Click(factory.GetComponent<UIDocument>().rootVisualElement.Q<Button>("back-to-center-button"));
            yield return null;
            Assert.AreEqual(GameFlowState.Center, flow.State);
            Assert.AreEqual(DisplayStyle.Flex, root.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, factory.GetComponent<UIDocument>().rootVisualElement.resolvedStyle.display);

            // SHOP is a real screen (#176), not a placeholder.
            ShopController shop = Object.FindAnyObjectByType<ShopController>();
            Assert.IsNotNull(shop, "Boot must host the dedicated Shop screen.");
            Click(root.Q<Button>("shop-button"));
            yield return null;
            Assert.AreEqual(GameFlowState.Shop, flow.State);
            Assert.AreEqual(DisplayStyle.None, root.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.Flex, shop.GetComponent<UIDocument>().rootVisualElement.resolvedStyle.display);

            Click(shop.GetComponent<UIDocument>().rootVisualElement.Q<Button>("center-button"));
            yield return null;
            Assert.AreEqual(GameFlowState.Center, flow.State);
            Assert.AreEqual(DisplayStyle.Flex, root.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, shop.GetComponent<UIDocument>().rootVisualElement.resolvedStyle.display);
        }

        [UnityTest]
        public IEnumerator Center_FillsTheRootAt1080p720pAnd16By10()
        {
            yield return LoadCenter();

            CenterController center = Object.FindAnyObjectByType<CenterController>();
            var document = center.GetComponent<UIDocument>();
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
                    VisualElement centerRoot = root.Q("center-root");
                    VisualElement screen = root.Q("center-screen");
                    VisualElement header = root.Q("center-header");
                    VisualElement table = root.Q("craft-table");
                    VisualElement actions = root.Q("factory-button");
                    Assert.IsNotNull(centerRoot, "Center root is missing.");
                    Assert.IsNotNull(screen, "Center screen is missing.");
                    Assert.IsNotNull(header, "Center header is missing.");
                    Assert.IsNotNull(table, "Center table is missing.");
                    Assert.IsNotNull(actions, "Center actions column is missing.");

                    Rect surface = centerRoot.worldBound;
                    Assert.Greater(surface.width, 0f, "The root must have layout width at " + size);
                    Assert.Greater(surface.height, 0f, "The root must have layout height at " + size);
                    Assert.AreEqual(surface.width, screen.worldBound.width, 1f,
                        "center-screen must fill the root width at " + size);
                    Assert.AreEqual(surface.height, screen.worldBound.height, 1f,
                        "center-screen must fill the root height at " + size);
                    float paddingTop = screen.resolvedStyle.paddingTop;
                    float paddingBottom = screen.resolvedStyle.paddingBottom;
                    float paddingRight = screen.resolvedStyle.paddingRight;
                    Assert.LessOrEqual(Mathf.Abs(header.worldBound.yMin - (surface.yMin + paddingTop)), 3f,
                        "The header must anchor to the top at " + size);
                    Assert.LessOrEqual(Mathf.Abs(table.worldBound.yMax - (surface.yMax - paddingBottom)), 3f,
                        "The craft table must anchor to the bottom at " + size);
                    Assert.LessOrEqual(Mathf.Abs(actions.worldBound.xMax - (surface.xMax - paddingRight)), 3f,
                        "The action column must anchor to the right at " + size);
                    Assert.Greater(table.worldBound.height, 80f, "The table must keep usable height at " + size);

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
        public IEnumerator Center_RendersA1920x1080CaptureOutsideTheProject()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Capture requires a graphics device; run the PlayMode gate without -nographics.");

            yield return LoadCenter();

            CenterController center = Object.FindAnyObjectByType<CenterController>();
            var document = center.GetComponent<UIDocument>();
            Assert.IsNotNull(document);

            // Show the five companion grade buttons and a duplicate AI count in the review frame.
            Assert.IsTrue(center.AssignCompanion(2));
            Assert.IsTrue(center.AssignCompanion(2));
            yield return null;
            Assert.AreEqual("2", ActiveRoot(center).Q("craft-row-2").Q<Button>("ai-cell").text);

            string folder = Path.Combine(Path.GetTempPath(), "opencode");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "center-1920x1080.png");
            if (File.Exists(path)) File.Delete(path);

            PanelSettings settings = document.panelSettings;
            RenderTexture previous = settings.targetTexture;
            var target = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            target.Create();

            try
            {
                settings.targetTexture = target;
                for (int i = 0; i < 12; i++) yield return null;

                VisualElement root = document.rootVisualElement;
                AssertSprite(root.Q("center-root"), "center-root");
                AssertSprite(root.Q("center-header"), "center-header");
                AssertSprite(root.Q("preview-frame"), "preview-frame");
                AssertSprite(root.Q("craft-table"), "craft-table");
                AssertSprite(root.Q("help-button"), "help-button");
                AssertSprite(root.Q("ready-button"), "ready-button");
                AssertSprite(root.Q("factory-button"), "factory-button");
                Assert.AreEqual("Center", root.Q<Label>("center-title").text);
                Assert.AreEqual(DisplayStyle.Flex, root.Q("companion-slots").resolvedStyle.display,
                    "The review frame must show the five companion grade buttons.");
                Assert.Greater(root.Q("preview-canvas").childCount, 0, "The capture frame must show the real blueprint.");

                RenderTexture.active = target;
                var image = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
                image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                image.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(path, image.EncodeToPNG());
                Object.Destroy(image);

                var check = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Assert.IsTrue(check.LoadImage(File.ReadAllBytes(path)));
                Color32[] pixels = check.GetPixels32();
                Object.Destroy(check);

                int nonBlack = 0;
                var colors = new System.Collections.Generic.HashSet<int>();
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 pixel = pixels[i];
                    if (pixel.r > 16 || pixel.g > 16 || pixel.b > 16) nonBlack++;
                    if (colors.Count < 4096) colors.Add((pixel.r << 16) | (pixel.g << 8) | pixel.b);
                }

                Assert.Greater(nonBlack, pixels.Length / 200, "Center rendered almost black.");
                Assert.Greater(colors.Count, 32, "Center rendered no extracted UI palette.");
                Debug.Log("[Center Capture] path=" + path + " nonBlack=" +
                          ((float)nonBlack / pixels.Length).ToString("P2") + " colors=" + colors.Count);
            }
            finally
            {
                settings.targetTexture = previous;
                RenderTexture.active = null;
                target.Release();
                Object.Destroy(target);
            }
        }

        private static IEnumerator LoadCenter()
        {
            yield return PlayModeBoot.LoadCenter();
        }

        private static IEnumerator ReloadCenter()
        {
            yield return PlayModeBoot.LoadCenter();
        }

        private static VisualElement ActiveRoot(CenterController center)
        {
            var document = center != null ? center.GetComponent<UIDocument>() : null;
            return document != null ? document.rootVisualElement : null;
        }

        private static int CountCompanionCopies(PlayerSaveData save, int craftIndex)
        {
            int count = 0;
            for (int i = 0; i < save.companions.Count; i++)
            {
                if (save.companions[i].craftIndex == craftIndex) count++;
            }

            return count;
        }

        private static void Click(Button button)
        {
            Assert.IsNotNull(button);
            MethodInfo simulate = typeof(Clickable).GetMethod(
                "SimulateSingleClick", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(simulate, "UI Toolkit Clickable.SimulateSingleClick moved.");
            simulate.Invoke(button.clickable, new object[] { null, 100 });
        }

        private static void AssertSprite(VisualElement element, string label)
        {
            Assert.IsNotNull(element, label + " is missing.");
            Image image = element.Q<Image>("sprite-art");
            Assert.IsNotNull(image, label + " has no rendered sprite-art Image.");
            Assert.IsNotNull(image.sprite, label + " has no sprite.");
            Background background = element.style.backgroundImage.value;
            Assert.IsTrue(background.sprite == null && background.texture == null,
                label + " must not use the atlased background-image path.");
        }
    }
}
