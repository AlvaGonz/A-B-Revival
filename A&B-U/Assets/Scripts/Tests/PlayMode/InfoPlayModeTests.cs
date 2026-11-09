using System.Collections;
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
    /// Boot-scene verification for Info/Records (#177): both original How-to-play sheets,
    /// the control reference for both modes and the revival bindings, real read-only save
    /// records, full-bleed layout at three sizes and 1920x1080 captures.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class InfoPlayModeTests
    {
        private const int WaitFrames = 600;

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
        public IEnumerator Info_ShowsBothHelpPagesAndRealReadOnlyRecords()
        {
            SeedSave(12345, 173, 173, 133, 71);
            yield return LoadBoot();

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            CenterController center = Object.FindAnyObjectByType<CenterController>();
            Assert.IsNotNull(flow);
            Assert.IsNotNull(center);
            VisualElement centerRoot = ActiveRoot(center);
            int fleet = center.FleetCount;
            string player = center.CraftName(center.PlayerCraftIndex);
            string saveBefore = File.ReadAllText(SavePath);

            Click(centerRoot.Q<Button>("info-button"));
            for (int i = 0; i < 8; i++) yield return null;

            InfoController info = Object.FindAnyObjectByType<InfoController>();
            Assert.IsNotNull(info, "Boot must host the dedicated Info screen.");
            Assert.AreEqual(GameFlowState.Info, flow.State, "Center INFO must open the Info screen.");
            Assert.AreEqual(DisplayStyle.None, centerRoot.resolvedStyle.display, "Center must hide behind Info.");
            VisualElement root = ActiveRoot(info);
            Assert.IsNotNull(root);
            Assert.AreEqual(DisplayStyle.Flex, root.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.Flex, root.Q("help-page").resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, root.Q("records-page").resolvedStyle.display);

            Assert.AreEqual("Info", root.Q<Label>("info-title").text);
            Assert.AreEqual("12,345 G", root.Q<Label>("money-label").text);
            Assert.AreEqual("1 / 2", root.Q<Label>("help-page-label").text);
            AssertHelpArt(root, "UI_HelpGame");
            StringAssert.Contains("turrets", root.Q<Label>("control-mouse").text.ToLowerInvariant(),
                "The mouse mode must describe turret auto-targeting.");
            StringAssert.Contains("mouse aims the turrets", root.Q<Label>("control-keyboard").text.ToLowerInvariant(),
                "The keyboard mode must describe mouse turret aim.");
            StringAssert.Contains("q switches", root.Q<Label>("control-switch").text.ToLowerInvariant());
            StringAssert.Contains("rotate independently", root.Q<Label>("control-turret").text.ToLowerInvariant());
            StringAssert.Contains("space burns", root.Q<Label>("control-boost").text.ToLowerInvariant());
            StringAssert.Contains("c cycles", root.Q<Label>("control-camera").text.ToLowerInvariant());
            StringAssert.Contains("e cycles the captain", root.Q<Label>("control-tactical").text.ToLowerInvariant());
            StringAssert.Contains("x toggles", root.Q<Label>("control-autofire").text.ToLowerInvariant());

            Click(root.Q<Button>("help-next"));
            yield return null;
            Assert.AreEqual(1, info.HelpPageIndex);
            Assert.AreEqual("2 / 2", root.Q<Label>("help-page-label").text);
            AssertHelpArt(root, "UI_HelpGame2");
            Assert.IsFalse(root.Q<Button>("help-next").enabledSelf, "The last page must disable the next arrow.");
            Click(root.Q<Button>("help-prev"));
            yield return null;
            Assert.AreEqual(0, info.HelpPageIndex);
            Assert.IsFalse(root.Q<Button>("help-prev").enabledSelf, "The first page must disable the previous arrow.");

            Click(root.Q<Button>("records-tab"));
            yield return null;
            Assert.IsTrue(info.ShowingRecords);
            Assert.AreEqual(DisplayStyle.None, root.Q("help-page").resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.Flex, root.Q("records-page").resolvedStyle.display);
            Assert.AreEqual("Data", root.Q<Label>("records-title").text);
            Assert.AreEqual("BATTLE-1", root.Q<Label>("records-story").text);
            Assert.AreEqual("12,345 G", root.Q<Label>("records-money").text);
            Assert.AreEqual(player, root.Q<Label>("records-player").text);
            Assert.AreEqual(fleet + " crafts", root.Q<Label>("records-fleet").text);
            Assert.AreEqual("0 / 5", root.Q<Label>("records-companions").text);
            Assert.AreEqual("4 units", root.Q<Label>("records-parts-total").text);
            Assert.AreEqual("Short (1.0x)", root.Q<Label>("records-camera").text);
            StringAssert.Contains("Read-only", root.Q<Label>("records-note").text);

            Assert.AreEqual(3, info.OwnedPartTypes);
            Assert.AreEqual("3 types", root.Q<Label>("parts-count").text);
            Assert.AreEqual(3, root.Query(className: "part-row").ToList().Count);
            VisualElement wingRow = root.Q("part-row-173");
            Assert.IsNotNull(wingRow, "The owned Wing_1 copies must be listed.");
            Assert.AreEqual("W.Cls.F.01.L", Cell(wingRow, "col-part-name").text,
                "Owned parts must resolve their real catalog names.");
            Assert.AreEqual("Wing", Cell(wingRow, "col-part-type").text);
            Assert.AreEqual("2", Cell(wingRow, "col-part-qty").text);

            Click(root.Q<Button>("help-tab"));
            yield return null;
            Assert.IsFalse(info.ShowingRecords);
            Assert.AreEqual(DisplayStyle.Flex, root.Q("help-page").resolvedStyle.display);

            Click(root.Q<Button>("back-button"));
            for (int i = 0; i < 4; i++) yield return null;
            Assert.AreEqual(GameFlowState.Center, flow.State, "Info BACK must return to Center.");
            Assert.AreEqual(DisplayStyle.Flex, centerRoot.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, root.resolvedStyle.display);
            Assert.AreEqual(saveBefore, File.ReadAllText(SavePath),
                "Info/Records is read-only: the save file must not change.");
        }

        [UnityTest]
        public IEnumerator Info_FillsTheRootAt1080p720pAnd16By10()
        {
            yield return LoadBoot();
            yield return OpenInfo();

            InfoController info = Object.FindAnyObjectByType<InfoController>();
            var document = info.GetComponent<UIDocument>();
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
                    Click(root.Q<Button>("help-tab"));
                    yield return null;
                    VisualElement infoRoot = root.Q("info-root");
                    VisualElement screen = root.Q("info-screen");
                    VisualElement header = root.Q("info-header");
                    VisualElement footer = root.Q("info-footer");
                    VisualElement sheet = root.Q("help-sheet");
                    VisualElement art = root.Q("help-art");
                    VisualElement controls = root.Q("controls-card");
                    VisualElement back = root.Q<Button>("back-button");
                    Assert.IsNotNull(infoRoot, "Info root is missing.");
                    Assert.IsNotNull(screen, "Info screen is missing.");
                    Assert.IsNotNull(header, "Info header is missing.");
                    Assert.IsNotNull(footer, "Info footer is missing.");
                    Assert.IsNotNull(sheet, "Help sheet is missing.");
                    Assert.IsNotNull(art, "Help art is missing.");
                    Assert.IsNotNull(controls, "Controls card is missing.");
                    Assert.IsNotNull(back, "Info BACK is missing.");

                    Rect surface = infoRoot.worldBound;
                    Debug.Log("[Info Layout] " + size + " surface=" + surface + " screen=" + screen.worldBound +
                              " sheet=" + sheet.worldBound + " controls=" + controls.worldBound +
                              " art=" + art.worldBound + " back=" + back.worldBound);
                    Assert.Greater(surface.width, 0f, "The root must have layout width at " + size);
                    Assert.Greater(surface.height, 0f, "The root must have layout height at " + size);
                    Assert.AreEqual(surface.width, screen.worldBound.width, 1f,
                        "info-screen must fill the root width at " + size);
                    Assert.AreEqual(surface.height, screen.worldBound.height, 1f,
                        "info-screen must fill the root height at " + size);

                    float paddingTop = screen.resolvedStyle.paddingTop;
                    float paddingBottom = screen.resolvedStyle.paddingBottom;
                    float paddingRight = screen.resolvedStyle.paddingRight;
                    float paddingLeft = screen.resolvedStyle.paddingLeft;
                    Assert.LessOrEqual(Mathf.Abs(header.worldBound.yMin - (surface.yMin + paddingTop)), 3f,
                        "The header must anchor to the top at " + size);
                    Assert.LessOrEqual(Mathf.Abs(footer.worldBound.yMax - (surface.yMax - paddingBottom)), 3f,
                        "The footer must anchor to the bottom at " + size);
                    Assert.LessOrEqual(Mathf.Abs(sheet.worldBound.xMin - (surface.xMin + paddingLeft)), 3f,
                        "The help sheet must anchor to the left at " + size);
                    Assert.LessOrEqual(Mathf.Abs(controls.worldBound.xMax - (surface.xMax - paddingRight)), 3f,
                        "The controls card must anchor to the right at " + size);
                    Assert.Greater(art.worldBound.height, 150f, "The help page art must keep usable height at " + size);
                    Assert.Greater(controls.worldBound.height, 240f, "The controls card must keep usable height at " + size);
                    Assert.Greater(back.worldBound.width, 200f, "The BACK plate must stay tappable at " + size);

                    Click(root.Q<Button>("records-tab"));
                    yield return null;
                    VisualElement records = root.Q("records-page");
                    VisualElement summary = root.Q("records-summary");
                    VisualElement parts = root.Q("parts-panel");
                    Assert.AreEqual(DisplayStyle.Flex, records.resolvedStyle.display);
                    Assert.LessOrEqual(Mathf.Abs(summary.worldBound.xMin - (surface.xMin + paddingLeft)), 3f,
                        "The records summary must anchor to the left at " + size);
                    Assert.LessOrEqual(Mathf.Abs(parts.worldBound.xMax - (surface.xMax - paddingRight)), 3f,
                        "The owned-parts table must anchor to the right at " + size);
                    Assert.Greater(summary.worldBound.height, 200f,
                        "The records summary must keep usable height at " + size);
                    Assert.Greater(parts.worldBound.height, 200f,
                        "The owned-parts table must keep usable height at " + size);

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
        public IEnumerator Info_Renders1920x1080CapturesOutsideTheProject()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Capture requires a graphics device; run the PlayMode gate without -nographics.");

            SeedSave(12345, 173, 173, 133, 71);
            yield return LoadBoot();
            yield return OpenInfo();

            InfoController info = Object.FindAnyObjectByType<InfoController>();
            var document = info.GetComponent<UIDocument>();
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

                AssertHelpArt(root, "UI_HelpGame");
                AssertSprite(root.Q("info-title-plate"), "info-title-plate");
                AssertSprite(root.Q("back-button"), "back-button");
                Assert.AreEqual("1 / 2", root.Q<Label>("help-page-label").text);

                string folder = Path.Combine(Path.GetTempPath(), "opencode");
                Directory.CreateDirectory(folder);
                string helpPath = Path.Combine(folder, "info-help-1920x1080.png");
                WriteCapture(target, helpPath);

                Click(root.Q<Button>("help-next"));
                for (int i = 0; i < 12; i++) yield return null;
                AssertHelpArt(root, "UI_HelpGame2");

                Click(root.Q<Button>("records-tab"));
                for (int i = 0; i < 12; i++) yield return null;
                Assert.AreEqual(DisplayStyle.Flex, root.Q("records-page").resolvedStyle.display);
                Assert.AreEqual("BATTLE-1", root.Q<Label>("records-story").text);
                Assert.IsNotNull(root.Q("part-row-173"), "The records capture must show real owned parts.");
                AssertSprite(root.Q("records-title-plate"), "records-title-plate");

                string recordsPath = Path.Combine(folder, "info-records-1920x1080.png");
                WriteCapture(target, recordsPath);

                AssertCaptureHasContent(helpPath, "Info help");
                AssertCaptureHasContent(recordsPath, "Info records");
                Debug.Log("[Info Capture] help=" + helpPath + " records=" + recordsPath);
            }
            finally
            {
                settings.targetTexture = previous;
                RenderTexture.active = null;
                target.Release();
                Object.Destroy(target);
            }
        }

        private static void WriteCapture(RenderTexture target, string path)
        {
            if (File.Exists(path)) File.Delete(path);
            RenderTexture.active = target;
            var image = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
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
            var colors = new System.Collections.Generic.HashSet<int>();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 pixel = pixels[i];
                if (pixel.r > 16 || pixel.g > 16 || pixel.b > 16) nonBlack++;
                if (colors.Count < 4096) colors.Add((pixel.r << 16) | (pixel.g << 8) | pixel.b);
            }

            Assert.Greater(nonBlack, pixels.Length / 200, label + " rendered almost black.");
            Assert.Greater(colors.Count, 32, label + " rendered no extracted UI palette.");
        }

        private static IEnumerator OpenInfo()
        {
            CenterController center = Object.FindAnyObjectByType<CenterController>();
            Assert.IsNotNull(center);
            Click(ActiveRoot(center).Q<Button>("info-button"));
            for (int i = 0; i < 8; i++) yield return null;
            Assert.AreEqual(GameFlowState.Info, Object.FindAnyObjectByType<GameFlowController>().State);
            Assert.IsNotNull(Object.FindAnyObjectByType<InfoController>());
        }

        private static IEnumerator LoadBoot()
        {
            yield return PlayModeBoot.LoadCenter();
        }

        private static VisualElement ActiveRoot(Component component)
        {
            var document = component != null ? component.GetComponent<UIDocument>() : null;
            return document != null ? document.rootVisualElement : null;
        }

        private static Label Cell(VisualElement row, string className)
        {
            return row.Query<Label>(className: className).First();
        }

        private static void AssertHelpArt(VisualElement root, string spriteName)
        {
            VisualElement art = root.Q("help-art");
            Assert.IsNotNull(art, "The help art surface is missing.");
            Image image = art.Q<Image>("sprite-art");
            Assert.IsNotNull(image, "The help page must render through an Image child.");
            Assert.IsNotNull(image.sprite, "The help page has no sprite.");
            StringAssert.Contains(spriteName, image.sprite.name,
                "The help page must render the original " + spriteName + " export.");
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

        private static void SeedSave(int money, params int[] ownedParts)
        {
            var payload = new PlayerSaveData();
            payload.shop.money = money;
            payload.shop.ownedParts.AddRange(ownedParts);
            PlayerSaveCodec.SaveToFile(SavePath, payload);
        }
    }
}
