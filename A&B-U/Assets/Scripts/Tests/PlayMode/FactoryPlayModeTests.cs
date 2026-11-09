using System.Collections;
using System.Collections.Generic;
using System.Globalization;
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
    /// Boot-scene verification for Factory (UIMakePlayer): real fleet blueprint, 2x mount-art
    /// preview, live F4ShowData stats, wing counter, category filters, mount/unmount/new/merge
    /// against the shared save, undo, zoom, full-bleed layout at three sizes and a capture.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class FactoryPlayModeTests
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
        public IEnumerator Factory_OpensEditsMountsPersistsAndReturnsToCenter()
        {
            yield return LoadBoot();

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            CenterController center = Object.FindAnyObjectByType<CenterController>();
            FactoryController factory = Object.FindAnyObjectByType<FactoryController>();
            Assert.IsNotNull(flow, "Boot must host the game flow.");
            Assert.IsNotNull(center, "Boot must host Center.");
            Assert.IsNotNull(factory, "Boot must host Factory.");

            VisualElement centerRoot = ActiveRoot(center);
            Assert.AreEqual(GameFlowState.Center, flow.State);
            Click(centerRoot.Q<Button>("factory-button"));
            yield return null;

            Assert.AreEqual(GameFlowState.Factory, flow.State, "Center FACTORY must open the Factory screen.");
            VisualElement root = ActiveRoot(factory);
            Assert.IsNotNull(root);
            Assert.AreEqual(DisplayStyle.Flex, root.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, centerRoot.resolvedStyle.display, "Center must hide behind Factory.");
            Assert.AreEqual(DisplayStyle.Flex, root.Q("factory-screen").resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.Flex, root.Q("preview-clip").resolvedStyle.display);

            Assert.Greater(factory.FleetCount, 0, "Factory must load the real saved fleet.");
            Assert.GreaterOrEqual(factory.ActivePartCount, 4, "The player craft must arrive assembled.");
            Assert.Greater(root.Q("preview-canvas").childCount, 0, "The Factory preview rendered no parts.");
            Assert.IsTrue(factory.Presenter.PreviewUsesMountArt,
                "Every Factory preview part must resolve to 2x PartMounts art.");
            VisualElement bodyPart = root.Q("preview-part-0");
            Assert.IsNotNull(bodyPart, "The body preview part is missing.");
            Image bodyArt = bodyPart.Q<Image>("sprite-art");
            Assert.IsNotNull(bodyArt, "The body preview must render through an Image child.");
            Sprite bodySprite = bodyArt.sprite;
            Assert.IsNotNull(bodySprite);
            Assert.Greater(bodySprite.rect.width, 50f,
                "Factory must use the 2x in-game mount art, not the 1x shop thumbnail.");
            Assert.IsTrue(bodyPart.style.backgroundImage.value.sprite == null,
                "Factory must not sample its atlas sprite through background-image.");
            Assert.Greater(factory.Presenter.PreviewFillFraction, 0.35f,
                "The assembled craft must use the preview frame.");

            CraftShowStats stats = factory.ActiveStats;
            Assert.AreEqual(factory.ActiveCraftName, root.Q<Label>("stat-name").text);
            Assert.AreEqual(stats.hp.ToString(CultureInfo.InvariantCulture), root.Q<Label>("stat-hp").text);
            Assert.AreEqual(stats.fire.ToString("0.00", CultureInfo.InvariantCulture), root.Q<Label>("stat-fire").text);
            Assert.AreEqual(stats.speed.ToString("0.0", CultureInfo.InvariantCulture), root.Q<Label>("stat-speed").text);
            Assert.AreEqual(stats.steering.ToString("0.0", CultureInfo.InvariantCulture), root.Q<Label>("stat-steering").text);
            Assert.AreEqual(stats.cls, root.Q<Label>("stat-class").text);
            Assert.AreEqual(stats.parts.ToString(CultureInfo.InvariantCulture), root.Q<Label>("stat-parts").text);
            StringAssert.StartsWith("Wings ", root.Q<Label>("wing-label").text,
                "The wing counter must render in the craft stats card.");

            int assembled = factory.ActivePartCount;
            VisualElement breakRow = FirstBreakableRow(root);
            Assert.IsNotNull(breakRow, "The mounted parts table has no non-chassis BREAK row.");
            Click(breakRow.Q<Button>("row-break"));
            yield return null;
            Assert.AreEqual(assembled - 1, factory.ActivePartCount, "BREAK must unmount the part.");
            PlayerSaveData saved = PlayerSaveCodec.LoadFromFile(SavePath);
            Assert.AreEqual(assembled - 1, saved.blueprints[factory.ActiveCraftIndex].mountedParts.Count,
                "Unmount must persist through the existing save payload.");

            Button undo = root.Q<Button>("undo-0");
            Assert.IsNotNull(undo);
            Assert.IsTrue(undo.enabledSelf, "The removed part must be re-addable from the undo strip.");
            Click(undo);
            yield return null;
            Assert.AreEqual(assembled, factory.ActivePartCount, "Undo must re-add the removed part.");

            Click(root.Q<Button>("new-button"));
            yield return null;
            Assert.AreEqual(1, factory.ActivePartCount, "NEW must break every non-chassis part to inventory.");

            VisualElement mergeRow = FirstRowWithButton(root, "row-merge");
            Assert.IsNotNull(mergeRow, "NEW must return the broken parts to the inventory.");
            Click(mergeRow.Q<Button>("row-merge"));
            yield return null;
            Assert.AreEqual(2, factory.ActivePartCount, "MERGE on an owned part must mount it.");

            float zoom = factory.Presenter.PreviewZoom;
            Click(root.Q<Button>("zoom-in-button"));
            yield return null;
            Assert.Greater(factory.Presenter.PreviewZoom, zoom, "Zoom must change the preview scale.");
            Click(root.Q<Button>("center-view-button"));
            yield return null;
            Assert.AreEqual(1f, factory.Presenter.PreviewZoom, 0.001f, "CENTER must restore the preview zoom.");

            Click(root.Q<Button>("tab-wing"));
            yield return null;
            Assert.Greater(RowCount(root, "owned-parts-list"), 0, "The Wing filter returned no owned or mounted rows.");
            Assert.IsFalse(root.Q<Button>("tab-all").ClassListContains("tab-active"));
            Assert.IsTrue(root.Q<Button>("tab-wing").ClassListContains("tab-active"));
            foreach (VisualElement row in Rows(root, "owned-parts-list"))
            {
                Label type = row.Query<Label>(className: "col-type").First();
                Assert.AreEqual("Wing", type.text, "The Wing filter leaked another category.");
            }

            Click(root.Q<Button>("merge-button"));
            yield return null;
            Assert.IsTrue(factory.ShowingCrafts, "MERGE must store the craft and show the craft list.");
            Assert.AreEqual(DisplayStyle.Flex, root.Q<ScrollView>("crafts-list").resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, root.Q<ScrollView>("owned-parts-list").resolvedStyle.display);

            VisualElement craftRow = root.Q("craft-row-" + factory.ActiveCraftIndex);
            Assert.IsNotNull(craftRow, "The merged craft must appear in the craft list.");
            Click(craftRow.Q<Button>("craft-edit"));
            yield return null;
            Assert.IsFalse(factory.ShowingCrafts, "EDIT must reopen the craft for assembly.");

            saved = PlayerSaveCodec.LoadFromFile(SavePath);
            int persistedParts = saved.blueprints[factory.ActiveCraftIndex].mountedParts.Count;
            Assert.AreEqual(2, persistedParts, "Factory edits must be saved before leaving.");

            Click(root.Q<Button>("back-to-center-button"));
            yield return null;
            Assert.AreEqual(GameFlowState.Center, flow.State, "Back to Center must restore the dashboard.");
            Assert.AreEqual(DisplayStyle.None, root.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.Flex, centerRoot.resolvedStyle.display);
            Assert.AreEqual(2, center.StatsFor(center.SelectedCraftIndex).parts,
                "Center must reload the edited blueprint.");

            yield return ReloadBoot();
            center = Object.FindAnyObjectByType<CenterController>();
            Assert.AreEqual(2, center.StatsFor(center.SelectedCraftIndex).parts,
                "Factory edits must survive a full boot reload.");
        }

        [UnityTest]
        public IEnumerator Factory_FillsTheRootAt1080p720pAnd16By10()
        {
            yield return OpenFactory();

            FactoryController factory = Object.FindAnyObjectByType<FactoryController>();
            var document = factory.GetComponent<UIDocument>();
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
                    VisualElement factoryRoot = root.Q("factory-root");
                    VisualElement screen = root.Q("factory-screen");
                    VisualElement header = root.Q("factory-header");
                    VisualElement undo = root.Q("undo-strip");
                    VisualElement table = root.Q("table-area");
                    VisualElement stats = root.Q("stats-column");
                    VisualElement back = root.Q<Button>("back-to-center-button");
                    Assert.IsNotNull(factoryRoot, "Factory root is missing.");
                    Assert.IsNotNull(screen, "Factory screen is missing.");
                    Assert.IsNotNull(header, "Factory header is missing.");
                    Assert.IsNotNull(undo, "Factory undo strip is missing.");
                    Assert.IsNotNull(table, "Factory table is missing.");
                    Assert.IsNotNull(stats, "Factory stats column is missing.");
                    Assert.IsNotNull(back, "Factory Back to Center is missing.");

                    Rect surface = factoryRoot.worldBound;
                    Assert.Greater(surface.width, 0f, "The root must have layout width at " + size);
                    Assert.Greater(surface.height, 0f, "The root must have layout height at " + size);
                    Assert.AreEqual(surface.width, screen.worldBound.width, 1f,
                        "factory-screen must fill the root width at " + size);
                    Assert.AreEqual(surface.height, screen.worldBound.height, 1f,
                        "factory-screen must fill the root height at " + size);

                    float paddingTop = screen.resolvedStyle.paddingTop;
                    float paddingBottom = screen.resolvedStyle.paddingBottom;
                    float paddingRight = screen.resolvedStyle.paddingRight;
                    float paddingLeft = screen.resolvedStyle.paddingLeft;
                    Assert.LessOrEqual(Mathf.Abs(header.worldBound.yMin - (surface.yMin + paddingTop)), 3f,
                        "The header must anchor to the top at " + size);
                    Assert.LessOrEqual(Mathf.Abs(table.worldBound.yMax - (surface.yMax - paddingBottom)), 3f,
                        "The table must anchor to the bottom at " + size);
                    Assert.LessOrEqual(Mathf.Abs(undo.worldBound.xMin - (surface.xMin + paddingLeft)), 3f,
                        "The undo strip must anchor to the left at " + size);
                    Assert.LessOrEqual(Mathf.Abs(stats.worldBound.xMax - (surface.xMax - paddingRight)), 3f,
                        "The action column must anchor to the right at " + size);
                    Assert.Greater(table.worldBound.height, 80f, "The table must keep usable height at " + size);
                    Assert.Greater(root.Q("preview-frame").worldBound.height, 100f,
                        "The preview must keep usable height at " + size);

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
        public IEnumerator Factory_RendersA1920x1080CaptureOutsideTheProject()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Capture requires a graphics device; run the PlayMode gate without -nographics.");

            yield return OpenFactory();

            FactoryController factory = Object.FindAnyObjectByType<FactoryController>();
            var document = factory.GetComponent<UIDocument>();
            Assert.IsNotNull(document);

            // The 2011 page reference is the craft list (Index/Name/EDIT/BREAK); capture that view.
            factory.ShowCrafts(true);
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, document.rootVisualElement.Q<ScrollView>("crafts-list").resolvedStyle.display);
            Assert.Greater(RowCount(document.rootVisualElement, "crafts-list"), 0, "The craft list is empty.");

            string folder = Path.Combine(Path.GetTempPath(), "opencode");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "factory-1920x1080.png");
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
                AssertSprite(root.Q("factory-root"), "factory-root");
                AssertSprite(root.Q("factory-header"), "factory-header");
                AssertSprite(root.Q("preview-frame"), "preview-frame");
                AssertSprite(root.Q("merge-button"), "merge-button");
                AssertSprite(root.Q("new-button"), "new-button");
                AssertSprite(root.Q("add-button"), "add-button");
                AssertSprite(root.Q("back-to-center-button"), "back-to-center-button");
                AssertSprite(root.Q("parts-crafts-button"), "parts-crafts-button");
                AssertSprite(root.Q("tab-all"), "tab-all");
                Assert.Greater(root.Q("preview-canvas").childCount, 0, "The capture frame must show the real craft.");

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
                var colors = new HashSet<int>();
                for (int i = 0; i < pixels.Length; i++)
                {
                    Color32 pixel = pixels[i];
                    if (pixel.r > 16 || pixel.g > 16 || pixel.b > 16) nonBlack++;
                    if (colors.Count < 4096) colors.Add((pixel.r << 16) | (pixel.g << 8) | pixel.b);
                }

                Assert.Greater(nonBlack, pixels.Length / 200, "Factory rendered almost black.");
                Assert.Greater(colors.Count, 32, "Factory rendered no extracted UI palette.");
                Debug.Log("[Factory Capture] path=" + path + " nonBlack=" +
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

        private static IEnumerator LoadBoot()
        {
            yield return PlayModeBoot.LoadCenter();
        }

        private static IEnumerator ReloadBoot()
        {
            yield return PlayModeBoot.LoadCenter();
        }

        private static IEnumerator OpenFactory()
        {
            yield return LoadBoot();

            CenterController center = Object.FindAnyObjectByType<CenterController>();
            VisualElement centerRoot = ActiveRoot(center);
            Click(centerRoot.Q<Button>("factory-button"));
            for (int i = 0; i < 8; i++) yield return null;

            FactoryController factory = Object.FindAnyObjectByType<FactoryController>();
            Assert.IsNotNull(factory, "Boot did not open Factory.");
            Assert.AreEqual(GameFlowState.Factory, Object.FindAnyObjectByType<GameFlowController>().State);
        }

        private static VisualElement ActiveRoot(Component component)
        {
            var document = component != null ? component.GetComponent<UIDocument>() : null;
            return document != null ? document.rootVisualElement : null;
        }

        private static IEnumerable<VisualElement> Rows(VisualElement root, string listName)
        {
            ScrollView list = root.Q<ScrollView>(listName);
            if (list == null) yield break;
            foreach (VisualElement child in list.contentContainer.Children())
            {
                if (child.ClassListContains("row")) yield return child;
            }
        }

        private static int RowCount(VisualElement root, string listName)
        {
            int count = 0;
            foreach (VisualElement _ in Rows(root, listName)) count++;
            return count;
        }

        private static VisualElement FirstRowWithButton(VisualElement root, string buttonName)
        {
            foreach (VisualElement row in Rows(root, "owned-parts-list"))
            {
                if (row.Q<Button>(buttonName) != null) return row;
            }

            return null;
        }

        /// <summary>First unmountable non-chassis row; breaking the hull would invalidate the craft.</summary>
        private static VisualElement FirstBreakableRow(VisualElement root)
        {
            foreach (VisualElement row in Rows(root, "owned-parts-list"))
            {
                if (row.Q<Button>("row-break") == null) continue;
                Label type = row.Query<Label>(className: "col-type").First();
                if (type != null && type.text == "Body") continue;
                return row;
            }

            return null;
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

        private static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(SavePath + ".corrupt")) File.Delete(SavePath + ".corrupt");
            if (File.Exists(SavePath + ".tmp")) File.Delete(SavePath + ".tmp");
        }
    }
}
