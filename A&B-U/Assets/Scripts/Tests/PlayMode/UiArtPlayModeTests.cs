using System.Collections;
using System.Reflection;
using ABRevival.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// Regression guard for the sprite-atlas background bug: the visible art must come from
    /// an Image child (which honors textureRect), not the atlased background-image path.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class UiArtPlayModeTests
    {
        private const int WaitFrames = 300;

        [UnityTest]
        public IEnumerator Center_UsesTheFrozenButtonAndPanelArt()
        {
            yield return LoadBoot();
            CenterController center = Object.FindAnyObjectByType<CenterController>();
            VisualElement root = center.GetComponent<UIDocument>().rootVisualElement;

            AssertArt(root, "help-button", "ui_0073_hud_button_help");
            AssertArt(root, "ready-button", "ui_0585_hangar_button_launch");
            AssertArt(root, "info-button", "ui_0107_hangar_header", "INFO");
            AssertArt(root, "shop-button", "ui_0107_hangar_header", "SHOP");
            AssertArt(root, "factory-button", "ui_0107_hangar_header", "FACTORY");
            AssertArt(root, "back-button", "ui_0107_hangar_header", "BACK");
            AssertArt(root, "reset-button", "ui_0107_hangar_header", "Reset");
            AssertArt(root, "best-button", "ui_0107_hangar_header", "Best");
            AssertArt(root, "grade-0", "ui_0061_hud_portrait_backing");
        }

        [UnityTest]
        public IEnumerator Factory_UsesTheFrozenTabAndButtonArt()
        {
            yield return LoadBoot();
            CenterController center = Object.FindAnyObjectByType<CenterController>();
            Click(center.GetComponent<UIDocument>().rootVisualElement.Q<Button>("factory-button"));
            for (int i = 0; i < 10; i++) yield return null;

            FactoryController factory = Object.FindAnyObjectByType<FactoryController>();
            Assert.IsNotNull(factory, "Boot did not open Factory.");
            VisualElement root = factory.GetComponent<UIDocument>().rootVisualElement;

            AssertArt(root, "merge-button", "ui_0364_hangar_button_merge");
            AssertArt(root, "new-button", "ui_0361_hangar_button_new");
            AssertArt(root, "add-button", "ui_0356_hangar_button_add");
            AssertArt(root, "back-to-center-button", "ui_0324_hangar_button_back_to_center");
            AssertArt(root, "parts-crafts-button", "ui_0404_hangar_button_parts_crafts");
            AssertArt(root, "tab-all", "ui_0124_hangar_category_all");
            AssertArt(root, "tab-body", "ui_0136_hangar_category_body");
            AssertArt(root, "tab-wing", "ui_0133_hangar_category_wing");
            AssertArt(root, "tab-engine", "ui_0130_hangar_category_engine");
            AssertArt(root, "tab-gun", "ui_0127_hangar_category_gun");
        }

        [UnityTest]
        public IEnumerator Shop_UsesTheFrozenButtonAndCategoryArt()
        {
            yield return LoadBoot();
            CenterController center = Object.FindAnyObjectByType<CenterController>();
            Click(center.GetComponent<UIDocument>().rootVisualElement.Q<Button>("shop-button"));
            for (int i = 0; i < 10; i++) yield return null;

            ShopController shop = Object.FindAnyObjectByType<ShopController>();
            Assert.IsNotNull(shop, "Boot did not open Shop.");
            VisualElement root = shop.GetComponent<UIDocument>().rootVisualElement;

            AssertArt(root, "accept-button", "ui_0170_hangar_button_accept");
            AssertArt(root, "reset-button", "ui_0167_hangar_button_reset");
            AssertArt(root, "center-button", "ui_0324_hangar_button_back_to_center");

            foreach (string prefix in new[] { "buy", "sell" })
            {
                // The original category labels are baked into the tab sprites; the asset name
                // is the text contract because there is no separate Label for these tabs.
                AssertArt(root, prefix + "-filter-all", "ui_0124_hangar_category_all");
                AssertArt(root, prefix + "-filter-body", "ui_0136_hangar_category_body");
                AssertArt(root, prefix + "-filter-wing", "ui_0133_hangar_category_wing");
                AssertArt(root, prefix + "-filter-engine", "ui_0130_hangar_category_engine");
                AssertArt(root, prefix + "-filter-gun", "ui_0127_hangar_category_gun");
            }

            VisualElement icon = root.Q("buy-row-0")?.Q("shop-icon");
            Assert.IsNotNull(icon, "The shop row icon is missing.");
            Image iconArt = icon.Q<Image>("sprite-art");
            Assert.IsNotNull(iconArt, "The shop row icon has no Image art.");
            Assert.IsNotNull(iconArt.sprite, "The shop row icon has no sprite.");
        }

        private static void AssertArt(VisualElement root, string name, string spriteName, string text = null)
        {
            VisualElement element = root.Q(name);
            Assert.IsNotNull(element, name + " is missing.");

            Image art = element.Q<Image>("sprite-art");
            Assert.IsNotNull(art, name + " has no rendered sprite-art Image.");
            Assert.IsNotNull(art.sprite, name + " has no sprite on its Image.");
            StringAssert.Contains(spriteName, art.sprite.name,
                name + " must render the original " + spriteName + " art.");

            Background background = element.style.backgroundImage.value;
            Assert.IsTrue(background.sprite != null || background.texture != null,
                name + " lost its inline background contract.");

            if (text == null) return;
            Label label = element.Q<Label>("sprite-text");
            Assert.IsNotNull(label, name + " has no text overlay.");
            Assert.AreEqual(text, label.text, name + " lost its visible label.");
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

        private static void Click(Button button)
        {
            Assert.IsNotNull(button);
            MethodInfo simulate = typeof(Clickable).GetMethod(
                "SimulateSingleClick", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(simulate, "UI Toolkit Clickable.SimulateSingleClick moved.");
            simulate.Invoke(button.clickable, new object[] { null, 100 });
        }
    }
}
