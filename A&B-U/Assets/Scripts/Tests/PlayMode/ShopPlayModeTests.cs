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
    /// Boot-scene verification for Shop (UIShop): the two stock/owned tables, category filters,
    /// xN purchase stack and hold-to-stack cadence, buy/sell checkout, ADR 0003 stock rules,
    /// persistence through the single save, full-bleed layout at three sizes and a capture.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class ShopPlayModeTests
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
        public IEnumerator Shop_StacksHoldsBuysSellsAndPersists()
        {
            SeedSave(20000);
            yield return OpenShop();

            ShopController shop = Object.FindAnyObjectByType<ShopController>();
            VisualElement root = ActiveRoot(shop);
            Assert.Greater(shop.Presenter.BuyRowCount, 0, "The shop stock table is empty.");

            VisualElement buyRow = FindAffordableModule(root, shop.Presenter, shop.Money);
            Assert.IsNotNull(buyRow, "The shop has no affordable module row to stage.");
            Assert.IsNotNull(buyRow.Q("shop-new-badge"), "A newly unlocked row must show the frozen NEW badge.");
            int partId = PartIdOf(buyRow);
            int price = shop.Shop.Catalog.TryGet(partId, out var part) ? part.Cost : 0;
            int moneyBefore = shop.Money;
            int ownedBefore = shop.Shop.OwnedCount(partId);

            shop.Presenter.AddToStack(partId);
            yield return null;
            buyRow = FindBuyRow(root, partId);
            Assert.IsNotNull(buyRow);
            Assert.AreEqual("x1", Cell(buyRow, "col-sel").text, "A click must stage x1.");
            StringAssert.Contains(price.ToString(CultureInfo.InvariantCulture), root.Q<Label>("cost-label").text);

            shop.Presenter.BeginHold(partId);
            for (int i = 0; i < 5; i++) shop.Presenter.AdvanceHoldTick();
            Assert.AreEqual(1, shop.Shop.PurchaseStack.GetQuantity(partId),
                "The frozen hold delay is six 50 ms ticks (300 ms).");
            shop.Presenter.AdvanceHoldTick();
            Assert.AreEqual(2, shop.Shop.PurchaseStack.GetQuantity(partId),
                "The sixth hold tick must increment the stack.");
            shop.Presenter.AdvanceHoldTick();
            Assert.AreEqual(2, shop.Shop.PurchaseStack.GetQuantity(partId),
                "Ticks 7-15 repeat every other tick.");
            shop.Presenter.AdvanceHoldTick();
            Assert.AreEqual(3, shop.Shop.PurchaseStack.GetQuantity(partId));
            shop.Presenter.EndHold();

            shop.Presenter.Accept();
            yield return null;
            Assert.AreEqual(moneyBefore - price * 3, shop.Money, "ACCEPT must charge the stacked quantity.");
            Assert.AreEqual(ownedBefore + 3, shop.Shop.OwnedCount(partId));
            Assert.IsTrue(shop.Shop.IsUnlocked(partId), "Infinite stock must keep the shop row.");
            buyRow = FindBuyRow(root, partId);
            Assert.IsNotNull(buyRow);
            Assert.AreEqual("INF", Cell(buyRow, "col-stock").text);
            Assert.AreEqual("N", Cell(buyRow, "col-sel").text, "ACCEPT must clear the purchase stack.");
            Assert.IsNull(buyRow.Q("shop-new-badge"), "Buying must clear the row's NEW badge.");

            PlayerSaveData saved = PlayerSaveCodec.LoadFromFile(SavePath);
            Assert.AreEqual(shop.Money, saved.shop.money, "ACCEPT must persist the money through the single save.");

            VisualElement sellRow = FindSellRow(root, partId);
            Assert.IsNotNull(sellRow, "Bought parts must appear in the sell table.");
            Assert.AreEqual(3, CountSellRows(root, partId), "The frozen form lists one row per owned copy.");
            shop.Presenter.ToggleSell(partId);
            yield return null;
            Assert.AreEqual("Y", Cell(FindSellRow(root, partId), "col-sel").text, "Selling is unit-based Y/N.");
            int sellValue = shop.Shop.GetSellValue(partId);
            int moneyAfterBuy = shop.Money;
            shop.Presenter.Accept();
            yield return null;

            Assert.AreEqual(moneyAfterBuy + sellValue, shop.Money);
            Assert.AreEqual(ownedBefore + 2, shop.Shop.OwnedCount(partId));
            Assert.AreEqual(2, CountSellRows(root, partId), "Selling one copy must leave the other copy's row.");
            saved = PlayerSaveCodec.LoadFromFile(SavePath);
            Assert.AreEqual(shop.Money, saved.shop.money);

            int persistedOwned = 0;
            for (int i = 0; i < saved.shop.ownedParts.Count; i++)
            {
                if (saved.shop.ownedParts[i] == partId) persistedOwned++;
            }

            Assert.AreEqual(ownedBefore + 2, persistedOwned);

            int expectedMoney = shop.Money;
            yield return ReloadBoot();
            CenterController center = Object.FindAnyObjectByType<CenterController>();
            Assert.AreEqual(expectedMoney, center.Money, "The shop money must survive a full boot reload.");
            Assert.IsTrue(center.IsUnlocked(partId), "Infinite stock rows must survive a full boot reload.");

            Click(ActiveRoot(center).Q<Button>("shop-button"));
            for (int i = 0; i < 8; i++) yield return null;
            ShopController reopened = Object.FindAnyObjectByType<ShopController>();
            Assert.IsNotNull(reopened);
            VisualElement persistedSellRow = FindSellRow(ActiveRoot(reopened), partId);
            Assert.IsNotNull(persistedSellRow, "Owned parts must survive a full boot reload.");
            Assert.AreEqual(2, CountSellRows(ActiveRoot(reopened), partId));
        }

        [UnityTest]
        public IEnumerator Shop_FiniteHullStockDepletesOnBuyAndReturnsOnSell()
        {
            yield return OpenShop();

            ShopController shop = Object.FindAnyObjectByType<ShopController>();
            VisualElement root = ActiveRoot(shop);

            VisualElement bodyRow = FindAffordableBody(root, shop.Presenter, shop.Money);
            Assert.IsNotNull(bodyRow, "The shop has no affordable finite-stock hull row.");
            int bodyId = PartIdOf(bodyRow);
            int price = shop.Shop.Catalog.TryGet(bodyId, out var bodyPart) ? bodyPart.Cost : 0;
            int moneyBefore = shop.Money;
            Assert.AreEqual(1, shop.Shop.GetBodyStock(bodyId));

            shop.Presenter.AddToStack(bodyId);
            yield return null;
            Assert.AreEqual("x1", Cell(FindBuyRow(root, bodyId), "col-sel").text);
            shop.Presenter.AdvanceHoldTick();
            Assert.AreEqual(1, shop.Shop.PurchaseStack.GetQuantity(bodyId),
                "Hulls never auto-stack or exceed their single-unit stock.");

            shop.Presenter.Accept();
            yield return null;
            Assert.AreEqual(moneyBefore - price, shop.Money);
            Assert.IsFalse(shop.Shop.IsUnlocked(bodyId), "A bought hull must leave the shop stock.");
            Assert.AreEqual(0, shop.Shop.GetBodyStock(bodyId));
            Assert.AreEqual(1, shop.Shop.OwnedCount(bodyId));
            Assert.IsNull(FindAcrossPages(root, shop.Presenter, row => PartIdOf(row) == bodyId));

            VisualElement sellRow = FindSellRow(root, bodyId);
            Assert.IsNotNull(sellRow, "The bought hull must be sellable.");
            shop.Presenter.ToggleSell(bodyId);
            int sellValue = shop.Shop.GetSellValue(bodyId);
            int moneyAfterBuy = shop.Money;
            shop.Presenter.Accept();
            yield return null;

            Assert.AreEqual(moneyAfterBuy + sellValue, shop.Money);
            Assert.AreEqual(0, shop.Shop.OwnedCount(bodyId));
            Assert.IsTrue(shop.Shop.IsUnlocked(bodyId), "Selling a hull must return it to shop stock.");
            Assert.AreEqual(1, shop.Shop.GetBodyStock(bodyId));
            bodyRow = FindAcrossPages(root, shop.Presenter, row => PartIdOf(row) == bodyId);
            Assert.IsNotNull(bodyRow, "The returned hull must be back in the shop table.");
            Assert.AreEqual("x1", Cell(bodyRow, "col-stock").text);
        }

        [UnityTest]
        public IEnumerator Shop_InvertFlipsEveryOwnedCopyAndHelpOverlayToggles()
        {
            SeedSave(20000);
            yield return OpenShop();

            ShopController shop = Object.FindAnyObjectByType<ShopController>();
            VisualElement root = ActiveRoot(shop);
            VisualElement buyRow = FindAffordableModule(root, shop.Presenter, shop.Money);
            Assert.IsNotNull(buyRow, "The invert test needs an affordable module.");
            int partId = PartIdOf(buyRow);
            int sellValue = shop.Shop.GetSellValue(partId);

            shop.Presenter.AddToStack(partId);
            shop.Presenter.AddToStack(partId);
            shop.Presenter.AddToStack(partId);
            shop.Presenter.Accept();
            yield return null;
            Assert.AreEqual(3, CountSellRows(root, partId), "Three bought copies must produce three sell rows.");

            Click(root.Q<Button>("invert-button"));
            yield return null;
            Assert.AreEqual(3, shop.Presenter.SellSelectionCount,
                "INVERT must flip every filtered sell row from N to Y (FormShop.selInvert).");
            foreach (VisualElement row in RealRows(root, "sell-rows"))
            {
                Label sel = Cell(row, "col-sel");
                if (PartIdOf(row) >= 0 && sel != null) Assert.AreEqual("Y", sel.text);
            }

            StringAssert.Contains((sellValue * 3).ToString(CultureInfo.InvariantCulture),
                root.Q<Label>("now-label").text);

            Click(root.Q<Button>("invert-button"));
            yield return null;
            Assert.AreEqual(0, shop.Presenter.SellSelectionCount, "A second INVERT must clear every row.");
            foreach (VisualElement row in RealRows(root, "sell-rows"))
            {
                Label sel = Cell(row, "col-sel");
                if (PartIdOf(row) >= 0 && sel != null) Assert.AreEqual("N", sel.text);
            }

            VisualElement help = root.Q("shop-help");
            Assert.IsNotNull(help, "The UI_HelpShop overlay is missing.");
            Assert.AreEqual(DisplayStyle.None, help.resolvedStyle.display);
            Click(root.Q<Button>("help-button"));
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, help.resolvedStyle.display, "The help button must open UI_HelpShop.");
            AssertSprite(root.Q("shop-help-page"), "shop-help-page");

            Click(root.Q<Button>("shop-help-close"));
            yield return null;
            Assert.AreEqual(DisplayStyle.None, help.resolvedStyle.display, "The SKIP/close control must close the help page.");

            Click(root.Q<Button>("help-button"));
            yield return null;
            Assert.AreEqual(DisplayStyle.Flex, help.resolvedStyle.display);
            Click(root.Q<Button>("help-button"));
            yield return null;
            Assert.AreEqual(DisplayStyle.None, help.resolvedStyle.display, "The help button must toggle the overlay closed.");
        }

        [UnityTest]
        public IEnumerator Shop_BackToCenterPreservesCenterFleetSelectionAndCompanions()
        {
            SeedSave(20000);
            yield return LoadBoot();

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            CenterController center = Object.FindAnyObjectByType<CenterController>();
            VisualElement centerRoot = ActiveRoot(center);
            Assert.Greater(center.FleetCount, 2, "The boot fleet must have craft to select.");

            center.SelectCraft(2);
            int selected = center.SelectedCraftIndex;
            int player = center.PlayerCraftIndex;
            int fleet = center.FleetCount;
            int companions = center.Companions.AssignedCount;

            Click(centerRoot.Q<Button>("shop-button"));
            yield return null;
            Assert.AreEqual(GameFlowState.Shop, flow.State, "Center's SHOP must open the Shop screen.");

            ShopController shop = Object.FindAnyObjectByType<ShopController>();
            VisualElement shopRoot = ActiveRoot(shop);
            Assert.AreEqual(DisplayStyle.None, centerRoot.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.Flex, shopRoot.resolvedStyle.display);

            VisualElement buyRow = FindAffordableModule(shopRoot, shop.Presenter, shop.Money);
            Assert.IsNotNull(buyRow, "The shop has no affordable module row to buy.");
            int partId = PartIdOf(buyRow);
            shop.Presenter.AddToStack(partId);
            shop.Presenter.Accept();
            yield return null;
            int moneyAfterBuy = shop.Money;

            Click(shopRoot.Q<Button>("center-button"));
            yield return null;

            Assert.AreEqual(GameFlowState.Center, flow.State);
            Assert.AreEqual(DisplayStyle.Flex, centerRoot.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, shopRoot.resolvedStyle.display);
            Assert.AreEqual(selected, center.SelectedCraftIndex, "The Shop visit must preserve the Center selection.");
            Assert.AreEqual(player, center.PlayerCraftIndex, "The Shop visit must preserve the Player craft.");
            Assert.AreEqual(fleet, center.FleetCount, "The Shop visit must preserve the fleet.");
            Assert.AreEqual(companions, center.Companions.AssignedCount, "The Shop visit must preserve companions.");
            Assert.AreEqual(moneyAfterBuy, center.Money, "Center must show the money spent in the Shop.");
        }

        [UnityTest]
        public IEnumerator Shop_FillsTheRootAt1080p720pAnd16By10()
        {
            yield return OpenShop();

            ShopController shop = Object.FindAnyObjectByType<ShopController>();
            var document = shop.GetComponent<UIDocument>();
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
                    VisualElement shopRoot = root.Q("shop-root");
                    VisualElement screen = root.Q("shop-screen");
                    VisualElement header = root.Q("shop-header");
                    VisualElement buy = root.Q("buy-panel");
                    VisualElement middle = root.Q("shop-middle");
                    VisualElement sell = root.Q("sell-panel");
                    VisualElement center = root.Q<Button>("center-button");
                    VisualElement invert = root.Q<Button>("invert-button");
                    VisualElement help = root.Q<Button>("help-button");
                    VisualElement filters = root.Q("buy-filters");
                    Assert.IsNotNull(shopRoot, "Shop root is missing.");
                    Assert.IsNotNull(screen, "Shop screen is missing.");
                    Assert.IsNotNull(header, "Shop header is missing.");
                    Assert.IsNotNull(buy, "Shop buy table is missing.");
                    Assert.IsNotNull(middle, "Shop trade strip is missing.");
                    Assert.IsNotNull(sell, "Shop sell table is missing.");
                    Assert.IsNotNull(center, "Shop Back to Center is missing.");
                    Assert.IsNotNull(invert, "Shop INVERT is missing.");
                    Assert.IsNotNull(help, "Shop help button is missing.");
                    Assert.IsNotNull(filters, "Shop category filters are missing.");

                    Rect surface = shopRoot.worldBound;
                    Assert.Greater(surface.width, 0f, "The root must have layout width at " + size);
                    Assert.Greater(surface.height, 0f, "The root must have layout height at " + size);
                    Assert.AreEqual(surface.width, screen.worldBound.width, 1f,
                        "shop-screen must fill the root width at " + size);
                    Assert.AreEqual(surface.height, screen.worldBound.height, 1f,
                        "shop-screen must fill the root height at " + size);

                    float paddingTop = screen.resolvedStyle.paddingTop;
                    float paddingBottom = screen.resolvedStyle.paddingBottom;
                    float paddingRight = screen.resolvedStyle.paddingRight;
                    float paddingLeft = screen.resolvedStyle.paddingLeft;
                    Assert.LessOrEqual(Mathf.Abs(header.worldBound.yMin - (surface.yMin + paddingTop)), 3f,
                        "The header must anchor to the top at " + size);
                    Assert.LessOrEqual(Mathf.Abs(sell.worldBound.yMax - (surface.yMax - paddingBottom)), 3f,
                        "The sell table must anchor to the bottom at " + size);
                    Assert.LessOrEqual(Mathf.Abs(buy.worldBound.xMin - (surface.xMin + paddingLeft)), 3f,
                        "The buy table must anchor to the left at " + size);
                    Assert.LessOrEqual(Mathf.Abs(buy.worldBound.xMax - (surface.xMax - paddingRight)), 3f,
                        "The buy table must anchor to the right at " + size);
                    Assert.Greater(buy.worldBound.height, 120f, "The buy table must keep usable height at " + size);
                    Assert.Greater(sell.worldBound.height, 120f, "The sell table must keep usable height at " + size);
                    Assert.Greater(middle.worldBound.height, 80f, "The trade strip must keep usable height at " + size);

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
        public IEnumerator Shop_RendersA1920x1080CaptureOutsideTheProject()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Capture requires a graphics device; run the PlayMode gate without -nographics.");

            SeedSave(20000);
            yield return OpenShop();

            ShopController shop = Object.FindAnyObjectByType<ShopController>();
            var document = shop.GetComponent<UIDocument>();
            Assert.IsNotNull(document);
            VisualElement root = document.rootVisualElement;

            // Show a completed purchase (INF row kept), a staged sell unit and the net cost.
            VisualElement buyRow = FindAffordableModule(root, shop.Presenter, shop.Money);
            Assert.IsNotNull(buyRow, "The capture needs an affordable module row.");
            int partId = PartIdOf(buyRow);
            shop.Presenter.AddToStack(partId);
            shop.Presenter.Accept();
            yield return null;
            Assert.IsNotNull(FindBuyRow(root, partId), "Infinite stock must keep the row after buying.");
            Assert.IsTrue(shop.Presenter.ToggleSell(partId), "The bought part must be selectable for selling.");
            yield return null;

            string folder = Path.Combine(Path.GetTempPath(), "opencode");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "shop-1920x1080.png");
            if (File.Exists(path)) File.Delete(path);

            PanelSettings settings = document.panelSettings;
            RenderTexture previous = settings.targetTexture;
            var target = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            target.Create();

            try
            {
                settings.targetTexture = target;
                for (int i = 0; i < 12; i++) yield return null;

                AssertSprite(root.Q("shop-root"), "shop-root");
                AssertSprite(root.Q("shop-title-plate"), "shop-title-plate");
                AssertSprite(root.Q("buy-panel"), "buy-panel");
                AssertSprite(root.Q("sell-panel"), "sell-panel");
                AssertSprite(root.Q("accept-button"), "accept-button");
                AssertSprite(root.Q("reset-button"), "reset-button");
                AssertSprite(root.Q("center-button"), "center-button");
                AssertSprite(root.Q("invert-button"), "invert-button");
                AssertSprite(root.Q("help-button"), "help-button");
                AssertSprite(root.Q("buy-filter-all"), "buy-filter-all");
                Assert.Greater(shop.Presenter.BuyRowCount, 0, "The capture frame must show stock rows.");
                Assert.IsNotNull(root.Q("shop-new-badge"), "The capture frame must show a NEW badge.");
                VisualElement firstRow = FirstAnyRow(root);
                Assert.IsNotNull(firstRow, "The capture frame must show a stock row.");
                VisualElement icon = firstRow.Q("shop-icon");
                Assert.IsNotNull(icon, "The capture frame must show the part icon.");
                Background iconBackground = icon.style.backgroundImage.value;
                Assert.IsTrue(iconBackground.sprite != null || iconBackground.texture != null,
                    "The part icon must use the extracted part sprite.");

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

                Assert.Greater(nonBlack, pixels.Length / 200, "Shop rendered almost black.");
                Assert.Greater(colors.Count, 32, "Shop rendered no extracted UI palette.");
                Debug.Log("[Shop Capture] path=" + path + " nonBlack=" +
                          ((float)nonBlack / pixels.Length).ToString("P2") + " colors=" + colors.Count);

                // Second frame: the exact UI_HelpShop overlay.
                Click(root.Q<Button>("help-button"));
                for (int i = 0; i < 12; i++) yield return null;
                VisualElement help = root.Q("shop-help");
                Assert.AreEqual(DisplayStyle.Flex, help.resolvedStyle.display);
                AssertSprite(root.Q("shop-help-page"), "shop-help-page");

                string helpPath = Path.Combine(folder, "shop-help-1920x1080.png");
                if (File.Exists(helpPath)) File.Delete(helpPath);
                RenderTexture.active = target;
                var helpImage = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
                helpImage.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                helpImage.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(helpPath, helpImage.EncodeToPNG());
                Object.Destroy(helpImage);

                var helpCheck = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Assert.IsTrue(helpCheck.LoadImage(File.ReadAllBytes(helpPath)));
                Color32[] helpPixels = helpCheck.GetPixels32();
                Object.Destroy(helpCheck);
                int helpNonBlack = 0;
                for (int i = 0; i < helpPixels.Length; i++)
                {
                    Color32 pixel = helpPixels[i];
                    if (pixel.r > 16 || pixel.g > 16 || pixel.b > 16) helpNonBlack++;
                }

                Assert.Greater(helpNonBlack, helpPixels.Length / 200, "The help overlay rendered almost black.");
                Debug.Log("[Shop Help Capture] path=" + helpPath + " nonBlack=" +
                          ((float)helpNonBlack / helpPixels.Length).ToString("P2"));
                Click(root.Q<Button>("shop-help-close"));
            }
            finally
            {
                settings.targetTexture = previous;
                RenderTexture.active = null;
                target.Release();
                Object.Destroy(target);
            }
        }

        private static IEnumerator OpenShop()
        {
            yield return LoadBoot();

            CenterController center = Object.FindAnyObjectByType<CenterController>();
            VisualElement centerRoot = ActiveRoot(center);
            Click(centerRoot.Q<Button>("shop-button"));
            for (int i = 0; i < 8; i++) yield return null;

            ShopController shop = Object.FindAnyObjectByType<ShopController>();
            Assert.IsNotNull(shop, "Boot did not open Shop.");
            Assert.AreEqual(GameFlowState.Shop, Object.FindAnyObjectByType<GameFlowController>().State);
            Assert.IsNotNull(ActiveRoot(shop));
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
                    var document = center.GetComponent<UIDocument>();
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

        private static IEnumerator ReloadBoot()
        {
            SceneManager.LoadScene("Boot");
            yield return null;

            for (int i = 0; i < WaitFrames; i++)
            {
                CenterController center = Object.FindAnyObjectByType<CenterController>();
                if (center != null && center.FleetCount > 0)
                {
                    var document = center.GetComponent<UIDocument>();
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

            Assert.Fail("Boot did not restore Center.");
        }

        private static VisualElement ActiveRoot(Component component)
        {
            var document = component != null ? component.GetComponent<UIDocument>() : null;
            return document != null ? document.rootVisualElement : null;
        }

        private static IEnumerable<VisualElement> RealRows(VisualElement root, string listName)
        {
            VisualElement list = root.Q(listName);
            if (list == null) yield break;
            foreach (VisualElement child in list.Children())
            {
                if (child.ClassListContains("shop-row") && !child.ClassListContains("shop-row-empty"))
                    yield return child;
            }
        }

        private static VisualElement FirstAnyRow(VisualElement root)
        {
            foreach (VisualElement row in RealRows(root, "buy-rows")) return row;
            return null;
        }

        private static VisualElement FindAffordableModule(VisualElement root, ShopPresenter presenter, int money)
        {
            int[] filters = { (int)PartCategory.Wing, (int)PartCategory.Engine, (int)PartCategory.Gun };
            for (int i = 0; i < filters.Length; i++)
            {
                presenter.SetBuyFilter(filters[i]);
                VisualElement row = FindAcrossPages(root, presenter, candidate => PriceOf(candidate) <= money);
                if (row != null) return row;
            }

            return null;
        }

        private static VisualElement FindAffordableBody(VisualElement root, ShopPresenter presenter, int money)
        {
            presenter.SetBuyFilter((int)PartCategory.Body);
            return FindAcrossPages(root, presenter, row => PriceOf(row) <= money);
        }

        private static VisualElement FindAcrossPages(
            VisualElement root, ShopPresenter presenter, System.Func<VisualElement, bool> match)
        {
            int pages = presenter.BuyPageCount;
            for (int page = 0; page < pages; page++)
            {
                presenter.GoToBuyPage(page);
                foreach (VisualElement row in RealRows(root, "buy-rows"))
                {
                    if (match(row)) return row;
                }
            }

            return null;
        }

        private static int PriceOf(VisualElement row)
        {
            Label label = Cell(row, "col-value");
            return label != null && int.TryParse(label.text, out int price) ? price : int.MaxValue;
        }

        private static VisualElement FindBuyRow(VisualElement root, int partId)
        {
            foreach (VisualElement row in RealRows(root, "buy-rows"))
            {
                if (PartIdOf(row) == partId) return row;
            }

            return null;
        }

        private static VisualElement FindSellRow(VisualElement root, int partId)
        {
            foreach (VisualElement row in RealRows(root, "sell-rows"))
            {
                if (PartIdOf(row) == partId) return row;
            }

            return null;
        }

        private static int CountSellRows(VisualElement root, int partId)
        {
            int count = 0;
            foreach (VisualElement row in RealRows(root, "sell-rows"))
            {
                if (PartIdOf(row) == partId) count++;
            }

            return count;
        }

        private static int PartIdOf(VisualElement row)
        {
            if (row.userData is int stored) return stored;

            Label label = Cell(row, "col-index");
            return label != null && int.TryParse(label.text, out int id) ? id : -1;
        }

        private static Label Cell(VisualElement row, string className)
        {
            return row.Query<Label>(className: className).First();
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
            Background background = element.style.backgroundImage.value;
            Assert.IsTrue(background.sprite != null || background.texture != null,
                label + " has no extracted sprite background.");
        }

        private static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(SavePath + ".corrupt")) File.Delete(SavePath + ".corrupt");
            if (File.Exists(SavePath + ".tmp")) File.Delete(SavePath + ".tmp");
        }

        private static void SeedSave(int money)
        {
            var payload = new PlayerSaveData();
            payload.shop.money = money;
            PlayerSaveCodec.SaveToFile(SavePath, payload);
        }
    }
}
