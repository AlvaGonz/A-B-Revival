using System.Collections;
using System.IO;
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
    /// Frozen-hangar parity guard: the boot scene must render the extracted 2011 frame,
    /// panel, table and button sprites, draw the assembled craft, keep a camera, and
    /// still raise the launch seam.
    /// </summary>
    [TestFixture]
    public class HangarVisualPlayModeTests
    {
        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, GameFlowController.DefaultSaveFileName);

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (AudioService.Instance != null) Object.Destroy(AudioService.Instance.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BootScene_HangarUsesOriginalSpritesAndRaisesLaunch()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);

            SceneManager.LoadScene("Boot");
            yield return null;

            Assert.GreaterOrEqual(SceneManager.sceneCountInBuildSettings, 1);
            StringAssert.EndsWith("Boot.unity", SceneUtility.GetScenePathByBuildIndex(0));
            for (int i = 1; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                StringAssert.DoesNotContain("Hangar.unity", SceneUtility.GetScenePathByBuildIndex(i),
                    "The superseded Hangar scene must not be a build target.");
            }

            Assert.IsNotNull(Camera.main, "Boot must keep its main camera; no 'No cameras rendering'.");

            HangarController hangar = null;
            VisualElement root = null;
            for (int i = 0; i < 300; i++)
            {
                hangar = Object.FindAnyObjectByType<HangarController>();
                if (hangar != null)
                {
                    var document = hangar.GetComponent<UIDocument>();
                    root = document != null ? document.rootVisualElement : null;
                    if (root != null && root.Q("launch-button") != null) break;
                }

                yield return null;
            }

            Assert.IsNotNull(hangar, "Boot scene must host the hangar.");
            Assert.IsNotNull(root, "Hangar UIDocument has no root element.");
            yield return null;

            AssertSprite(root.Q("hangar-screen"), "hangar-screen");
            AssertSprite(root.Q("hangar-header"), "hangar-header");
            AssertSprite(root.Q("hangar-frame"), "hangar-frame");
            AssertSprite(root.Q("stats-card"), "stats-card");
            AssertSprite(root.Q("table-area"), "table-area");
            AssertSprite(root.Q("table-header"), "table-header");
            AssertSprite(root.Q("launch-button"), "launch-button");
            AssertSprite(root.Q("merge-button"), "merge-button");
            AssertSprite(root.Q("new-button"), "new-button");
            AssertSprite(root.Q("back-to-center-button"), "back-to-center-button");
            AssertSprite(root.Q("parts-crafts-button"), "parts-crafts-button");
            AssertSprite(root.Q("checkout-button"), "checkout-button");
            AssertSprite(root.Q("clear-stack-button"), "clear-stack-button");
            AssertSprite(root.Q("tab-all"), "tab-all");
            AssertSprite(root.Q("tab-body"), "tab-body");
            AssertSprite(root.Q("tab-wing"), "tab-wing");
            AssertSprite(root.Q("tab-engine"), "tab-engine");
            AssertSprite(root.Q("tab-gun"), "tab-gun");

            var preview = root.Q<VisualElement>("preview-canvas");
            Assert.IsNotNull(preview, "Hangar preview canvas is missing.");
            Assert.Greater(preview.childCount, 0, "The assembled craft preview rendered no parts.");
            for (int i = 0; i < preview.childCount; i++)
            {
                AssertSprite(preview[i], "preview part " + i);
            }

            AssertPreviewFitsInsideTheClip(root.Q<VisualElement>("preview-clip"), preview);

            // Frozen F4ShowData stats: summed turnFly in degrees and the FormShop fire formula.
            Assert.AreEqual("540.0", root.Q<Label>("stat-strength").text, "Str. must be the summed turn rate.");
            Assert.AreEqual("1.02", root.Q<Label>("stat-fire").text, "Fire must use the frozen display formula.");

            // Exactly one category tab is active: All by default, then the selected category.
            AssertSingleActiveTab(root, "tab-all");
            hangar.Assembly.SelectCategory(ABRevival.Core.Data.PartCategory.Wing);
            AssertSingleActiveTab(root, "tab-wing");
            hangar.Assembly.SelectCategory(null);
            AssertSingleActiveTab(root, "tab-all");

            var parts = root.Q<ScrollView>("owned-parts-list");
            Assert.IsNotNull(parts, "Parts table is missing.");
            Assert.Greater(RowCount(parts), 0, "The parts table has no mounted or owned rows.");

            var shop = root.Q<ScrollView>("shop-stock-list");
            Assert.IsNotNull(shop, "Shop stock list is missing.");
            Assert.Greater(RowCount(shop), 0, "The shop has no stock rows.");
            VisualElement firstShopRow = FirstRow(shop);
            Assert.IsNotNull(firstShopRow, "Shop row is missing.");
            Assert.IsNotNull(firstShopRow.Q<Label>("shop-name"), "Shop row has no name.");
            Assert.IsNotNull(firstShopRow.Q<Label>("shop-price"), "Shop row has no price.");
            Assert.IsNotNull(firstShopRow.Q<Label>("shop-stock"), "Shop row has no stock.");
            Assert.IsNotNull(firstShopRow.Q<Label>("shop-stack"), "Shop row has no xN stack.");
            AssertSprite(firstShopRow.Q("shop-icon"), "shop icon");

            VisualElement icon = firstShopRow.Q("shop-icon");
            var name = firstShopRow.Q<Label>("shop-name");
            var price = firstShopRow.Q<Label>("shop-price");
            var stock = firstShopRow.Q<Label>("shop-stock");
            var stack = firstShopRow.Q<Label>("shop-stack");
            Assert.LessOrEqual(icon.worldBound.xMax, name.worldBound.xMin + 0.5f, "Shop icon overlaps the name.");
            Assert.LessOrEqual(name.worldBound.xMax, price.worldBound.xMin + 0.5f, "Shop name overlaps the price.");
            Assert.LessOrEqual(price.worldBound.xMax, stock.worldBound.xMin + 0.5f, "Shop price overlaps the stock.");
            Assert.LessOrEqual(stock.worldBound.xMax, stack.worldBound.xMin + 0.5f, "Shop stock overlaps the xN stack.");

            var stackTotal = root.Q<Label>("stack-total-label");
            Assert.IsNotNull(stackTotal, "Shop stack total is missing.");
            Assert.LessOrEqual(stackTotal.worldBound.yMax, root.Q("checkout-button").worldBound.yMin + 0.5f,
                "The stack total collides with the ACCEPT/RESET row.");

            bool launched = false;
            hangar.LaunchRequested += () => launched = true;
            hangar.RequestLaunch();
            Assert.IsTrue(launched, "Launch must still raise HangarController.LaunchRequested.");
        }

        private static void AssertSprite(VisualElement element, string label)
        {
            Assert.IsNotNull(element, label + " is missing.");
            Background background = element.style.backgroundImage.value;
            Assert.IsTrue(background.sprite != null || background.texture != null,
                label + " has no extracted sprite background.");
        }

        private static void AssertPreviewFitsInsideTheClip(VisualElement clip, VisualElement preview)
        {
            Assert.IsNotNull(clip, "Hangar preview clip is missing.");
            Rect clipBounds = clip.worldBound;
            Assert.Greater(clipBounds.width, 0f, "The preview clip has no layout width.");
            Assert.Greater(clipBounds.height, 0f, "The preview clip has no layout height.");

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            for (int i = 0; i < preview.childCount; i++)
            {
                Rect bounds = preview[i].worldBound;
                Assert.Greater(bounds.width, 0f, "Preview part " + i + " has no width.");
                Assert.Greater(bounds.height, 0f, "Preview part " + i + " has no height.");
                Assert.GreaterOrEqual(bounds.xMin, clipBounds.xMin - 1f, "Preview part " + i + " clips on the left.");
                Assert.LessOrEqual(bounds.xMax, clipBounds.xMax + 1f, "Preview part " + i + " clips on the right.");
                Assert.GreaterOrEqual(bounds.yMin, clipBounds.yMin - 1f, "Preview part " + i + " clips on the top.");
                Assert.LessOrEqual(bounds.yMax, clipBounds.yMax + 1f, "Preview part " + i + " clips on the bottom.");
                minX = Mathf.Min(minX, bounds.xMin);
                maxX = Mathf.Max(maxX, bounds.xMax);
                minY = Mathf.Min(minY, bounds.yMin);
                maxY = Mathf.Max(maxY, bounds.yMax);
            }

            float unionArea = Mathf.Max(0f, maxX - minX) * Mathf.Max(0f, maxY - minY);
            Assert.Greater(unionArea, clipBounds.width * clipBounds.height * 0.2f,
                "The assembled craft must use the preview area, not render as a tiny thumbnail.");
        }

        private static void AssertSingleActiveTab(VisualElement root, string expected)
        {
            string[] tabs = { "tab-all", "tab-body", "tab-wing", "tab-engine", "tab-gun" };
            for (int i = 0; i < tabs.Length; i++)
            {
                var tab = root.Q<Button>(tabs[i]);
                Assert.IsNotNull(tab, tabs[i] + " is missing.");
                Assert.AreEqual(tabs[i] == expected, tab.ClassListContains("tab-active"),
                    tabs[i] + " active state is wrong; only " + expected + " may be active.");
            }
        }

        private static int RowCount(ScrollView list)
        {
            int count = 0;
            var children = list.contentContainer.Children();
            foreach (VisualElement child in children)
            {
                if (child.ClassListContains("row") || child.ClassListContains("shop-row")) count++;
            }

            return count;
        }

        private static VisualElement FirstRow(ScrollView list)
        {
            foreach (VisualElement child in list.contentContainer.Children())
            {
                if (child.ClassListContains("shop-row")) return child;
            }

            return null;
        }
    }
}
