using System.Collections;
using System.IO;
using ABRevival.Core.Data;
using ABRevival.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// ADR 0020 evidence for ticket #185: every live Center, Factory, Shop, HUD, Help and
    /// placeholder label resolves to one of the eight frozen fonts, and the 1920x1080,
    /// 1280x720 and 16:10 frames are captured for review. Original IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class FontPlayModeTests
    {
        private const int WaitFrames = 300;
        private const string Sf = "font_0007_SF_TransRobotics";
        private const string Arial = "font_0081_Arial";
        private const string Sans = "font_0002_sans";
        private const string Grade = "font_0478_sans";

        private static readonly string[] OriginalFonts =
        {
            Sf, Arial, Sans, Grade,
            "font_0627_SF_TransRobotics",
            "font_0701_SF_TransRobotics",
            "font_0595_Times_New_Roman",
            "font_0496_SF_Zero_Gravity"
        };

        [UnityTest]
        public IEnumerator Center_UsesOriginalFontsAtEveryResolution()
        {
            yield return LoadBoot();

            CenterController center = Object.FindAnyObjectByType<CenterController>();
            UIDocument document = center.GetComponent<UIDocument>();
            yield return BeginReferenceResolution(document);

            AssertCenterFonts(document.rootVisualElement);

            TalkScreen talk = Object.FindAnyObjectByType<TalkScreen>();
            Assert.IsNotNull(talk, "Boot scene must host the talk screen.");
            UIDocument talkDocument = talk.GetComponent<UIDocument>();
            StringAssert.Contains(Sf, ResolvedFont(talkDocument.rootVisualElement),
                "Talk panel labels must inherit the original SF TransRobotics face.");

            Click(document.rootVisualElement.Q<Button>("info-button"));
            for (int i = 0; i < 8; i++) yield return null;
            InfoController info = Object.FindAnyObjectByType<InfoController>();
            Assert.IsNotNull(info, "Boot must host the Info screen for the font check.");
            VisualElement infoRoot = info.GetComponent<UIDocument>().rootVisualElement;
            Assert.AreEqual(DisplayStyle.Flex, infoRoot.resolvedStyle.display,
                "The Info screen must be visible for the font check.");
            AssertFont(infoRoot, "info-title", Sf);
            AssertFont(infoRoot, "help-tab", Sf);
            AssertFont(infoRoot, "records-tab", Sf);
            AssertFont(infoRoot, "help-page-label", Sans);
            AssertFont(infoRoot, "control-mouse", Arial);
            AssertFont(infoRoot, "records-note", Sans);
            AssertEveryTextUsesOriginalFont(infoRoot, "Info");
            if (infoRoot.layout.width >= 1900f) AssertTextMetrics(infoRoot, "Info", false);
            Click(infoRoot.Q<Button>("back-button"));
            for (int i = 0; i < 8; i++) yield return null;

            Click(document.rootVisualElement.Q<Button>("back-button"));
            for (int i = 0; i < 8; i++) yield return null;
            MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
            Assert.IsNotNull(menu, "Boot must host the main menu for the font check.");
            VisualElement menuRoot = menu.GetComponent<ScreenPanel>().Root;
            Assert.AreEqual(DisplayStyle.Flex, menuRoot.resolvedStyle.display,
                "The main menu must be visible after Center BACK.");
            Click(menuRoot.Q<Button>("credits-button"));
            yield return null;
            AssertFont(menuRoot, "deferred-title", Sf);
            AssertFont(menuRoot, "deferred-body", Sans);
            AssertFont(menuRoot, "deferred-back", Sf);
            Click(menuRoot.Q<Button>("deferred-back"));
            yield return null;

            PlayModeBoot.StartGame();
            yield return PlayModeBoot.WaitForCenter();
            document = Object.FindAnyObjectByType<CenterController>().GetComponent<UIDocument>();

            yield return EndReferenceResolution(document);
            yield return CaptureAtEveryResolution(document, "center", AssertCenterFonts);
        }

        [UnityTest]
        public IEnumerator Factory_UsesOriginalFontsAtEveryResolution()
        {
            yield return LoadBoot();

            Click(Object.FindAnyObjectByType<CenterController>()
                .GetComponent<UIDocument>().rootVisualElement.Q<Button>("factory-button"));
            for (int i = 0; i < 10; i++) yield return null;

            FactoryController factory = Object.FindAnyObjectByType<FactoryController>();
            Assert.IsNotNull(factory, "Boot did not open Factory.");
            UIDocument document = factory.GetComponent<UIDocument>();
            yield return BeginReferenceResolution(document);

            AssertFactoryFonts(document.rootVisualElement);

            Click(document.rootVisualElement.Q<Button>("help-button"));
            for (int i = 0; i < 8; i++) yield return null;
            Assert.AreEqual(DisplayStyle.Flex, document.rootVisualElement.Q("factory-help").resolvedStyle.display,
                "The Factory help panel must be visible for the font check.");
            AssertFont(document.rootVisualElement, "help-title", Sf);
            AssertFont(document.rootVisualElement, "help-body", Sf);
            AssertFont(document.rootVisualElement, "help-close", Sf);
            Click(document.rootVisualElement.Q<Button>("help-close"));
            for (int i = 0; i < 8; i++) yield return null;

            yield return EndReferenceResolution(document);
            yield return CaptureAtEveryResolution(document, "factory", AssertFactoryFonts);
        }

        [UnityTest]
        public IEnumerator Shop_UsesOriginalFontsAtEveryResolution()
        {
            yield return LoadBoot();

            Click(Object.FindAnyObjectByType<CenterController>()
                .GetComponent<UIDocument>().rootVisualElement.Q<Button>("shop-button"));
            for (int i = 0; i < 10; i++) yield return null;

            ShopController shop = Object.FindAnyObjectByType<ShopController>();
            Assert.IsNotNull(shop, "Boot did not open Shop.");
            UIDocument document = shop.GetComponent<UIDocument>();
            Assert.Greater(shop.Presenter.BuyRowCount, 0, "The Shop needs live rows for the font check.");
            yield return BeginReferenceResolution(document);

            AssertShopFonts(document.rootVisualElement);
            yield return EndReferenceResolution(document);
            yield return CaptureAtEveryResolution(document, "shop", AssertShopFonts);
        }

        [UnityTest]
        public IEnumerator Hud_UsesOriginalFontsAtEveryResolution()
        {
            yield return PlayModeBoot.LoadCenter();

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow, "Boot scene must host the game flow.");
            flow.LaunchMission();
            yield return null;
            flow.SkipBriefing();

            for (int i = 0; i < WaitFrames && flow.State != GameFlowState.Mission; i++)
                yield return null;
            Assert.AreEqual(GameFlowState.Mission, flow.State, "The HUD font check needs the mission state.");

            CombatHudBehaviour hud = Object.FindAnyObjectByType<CombatHudBehaviour>();
            Assert.IsNotNull(hud, "Boot scene must host the combat HUD.");
            hud.State.RecordLostPart(PartCategory.Gun);
            hud.CycleCameraDistance();
            for (int i = 0; i < 4; i++) yield return null;

            UIDocument document = hud.GetComponent<UIDocument>();
            yield return BeginReferenceResolution(document);
            AssertHudFonts(document.rootVisualElement);
            yield return EndReferenceResolution(document);
            yield return CaptureAtEveryResolution(document, "hud", AssertHudFonts);
        }

        private static void AssertCenterFonts(VisualElement root)
        {
            AssertFont(root, "center-title", Sf);
            AssertFont(root, "battle-label", Sf);
            AssertFont(root, "money-label", Sf);
            AssertFont(root, "preview-name", Sf);
            AssertFont(root, "inuse-label", Sf);
            AssertFont(root, "instruction-label", Sans);
            AssertFont(root, "page-label", Sans);
            AssertFont(root, "reset-button", Arial);
            AssertFont(root, "best-button", Arial);
            AssertFont(root, "grade-0", Grade);
            AssertFont(root, "grade-4", Grade);
            AssertFont(root, "stat-name", Arial);
            AssertFont(root, "stat-hp", Arial);

            AssertFirstTextFont(root.Q("table-header"), Arial, "Center table header");
            AssertFirstTextFont(root.Q("craft-row-0"), Arial, "Center craft row");

            AssertEveryTextUsesOriginalFont(root, "Center");
            if (root.layout.width >= 1900f) AssertTextMetrics(root, "Center", true);
        }

        private static void AssertFactoryFonts(VisualElement root)
        {
            AssertFont(root, "factory-title", Sf);
            AssertFont(root, "preview-name", Sf);
            AssertFont(root, "undo-0", Sans);
            AssertFont(root, "page-label", Sans);
            AssertFont(root, "detail-hp", Arial);
            AssertFont(root, "stat-name", Arial);

            AssertFirstTextFont(root.Q("table-header"), Arial, "Factory table header");
            AssertFirstTextFont(root.Q("crafts-header"), Arial, "Factory crafts header");
            AssertFirstTextFont(root.Q("craft-row-0"), Arial, "Factory craft row");

            AssertEveryTextUsesOriginalFont(root, "Factory");
            if (root.layout.width >= 1900f) AssertTextMetrics(root, "Factory", false);
        }

        private static void AssertShopFonts(VisualElement root)
        {
            AssertFont(root, "shop-title", Sf);
            AssertFont(root, "money-label", Sf);
            AssertFont(root, "cost-label", Sf);
            AssertFont(root, "now-label", Sf);
            AssertFont(root, "player-label", Sf);
            AssertFont(root, "buy-page-label", Sans);
            AssertFont(root, "sell-page-label", Sans);
            AssertFont(root, "buy-detail-type", Arial);
            AssertFont(root, "sell-detail-value", Arial);

            AssertFirstTextFont(root.Q("buy-header"), Arial, "Shop buy header");
            AssertFirstTextFont(root.Q("sell-header"), Arial, "Shop sell header");
            AssertFirstTextFont(root.Q("buy-row-0"), Arial, "Shop buy row");

            AssertEveryTextUsesOriginalFont(root, "Shop");
            if (root.layout.width >= 1900f) AssertTextMetrics(root, "Shop", false);
        }

        private static void AssertHudFonts(VisualElement root)
        {
            AssertFont(root, "hud-money", Sf);
            AssertFont(root, "hud-radar-distance", Sf);
            AssertFont(root, "hud-mission-timer", Sf);
            AssertFont(root, "hud-ai-count-value", Sf);
            AssertFont(root, "hud-hull-value", Sf);
            AssertFont(root, "hud-captain-mode", Sf);
            AssertFont(root, "hud-mode-badge-text", Sf);
            AssertFont(root, "hud-camera-badge-text", Sf);
            AssertFont(root, "hud-pilot-name", Grade);

            AssertEveryTextUsesOriginalFont(root, "HUD");
            if (root.layout.width >= 1900f) AssertTextMetrics(root, "HUD", true);
        }

        private static IEnumerator BeginReferenceResolution(UIDocument document)
        {
            var target = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            target.Create();
            document.panelSettings.targetTexture = target;
            for (int i = 0; i < 10; i++) yield return null;
            VisualElement root = document.rootVisualElement;
            Debug.Log("[Font Resolution] target=" + target.width + "x" + target.height +
                      " root=" + root.layout.width + "x" + root.layout.height +
                      " panelScale=" + document.panelSettings.scale);
        }

        private static IEnumerator EndReferenceResolution(UIDocument document)
        {
            PanelSettings settings = document.panelSettings;
            RenderTexture target = settings.targetTexture;
            settings.targetTexture = null;
            if (target != null)
            {
                target.Release();
                Object.Destroy(target);
            }

            for (int i = 0; i < 4; i++) yield return null;
        }

        private static IEnumerator CaptureAtEveryResolution(
            UIDocument document, string surface, System.Action<VisualElement> assertFonts)
        {
            PanelSettings settings = document.panelSettings;
            RenderTexture previous = settings.targetTexture;
            string folder = Path.Combine(Path.GetTempPath(), "opencode");
            Directory.CreateDirectory(folder);

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

                    for (int i = 0; i < 10; i++) yield return null;

                    VisualElement root = document.rootVisualElement;
                    Assert.IsNotNull(root, surface + " lost its live root at " + size);
                    Debug.Log("[Font Resolution] " + surface + " target=" + size.x + "x" + size.y +
                              " root=" + root.layout.width + "x" + root.layout.height +
                              " panelScale=" + settings.scale);
                    assertFonts(root);

                    string path = Path.Combine(folder,
                        "fonts-" + surface + "-" + size.x + "x" + size.y + ".png");
                    if (File.Exists(path)) File.Delete(path);

                    RenderTexture.active = target;
                    var image = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
                    image.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                    image.Apply();
                    RenderTexture.active = null;
                    File.WriteAllBytes(path, image.EncodeToPNG());
                    Object.Destroy(image);

                    Assert.IsTrue(new FileInfo(path).Length > 1024,
                        surface + " capture is empty at " + size + ": " + path);
                    Debug.Log("[Font Capture] path=" + path + " bytes=" + new FileInfo(path).Length);

                    settings.targetTexture = previous;
                    target.Release();
                    Object.Destroy(target);
                    for (int i = 0; i < 4; i++) yield return null;
                }
            }
            finally
            {
                settings.targetTexture = previous;
                RenderTexture.active = null;
            }
        }

        private static void AssertEveryTextUsesOriginalFont(VisualElement root, string surface)
        {
            int checkedCount = 0;
            foreach (TextElement element in root.Query<TextElement>().Build())
            {
                if (string.IsNullOrEmpty(element.text)) continue;
                checkedCount++;

                string font = ResolvedFont(element);
                Assert.IsNotNull(font, surface + " text '" + element.text + "' (" + Describe(element) +
                    ") resolved to the Unity default font.");
                bool original = false;
                foreach (string candidate in OriginalFonts)
                {
                    if (font.Contains(candidate))
                    {
                        original = true;
                        break;
                    }
                }

                Assert.IsTrue(original, surface + " text '" + element.text + "' (" + Describe(element) +
                    ") resolved to a non-original font: " + font);
            }

            Assert.Greater(checkedCount, 0, surface + " has no text to verify.");
            Debug.Log("[Font Check] " + surface + " labels=" + checkedCount);
        }

        private static void AssertFont(VisualElement root, string name, string expected)
        {
            TextElement element = root.Q<TextElement>(name);
            Assert.IsNotNull(element, name + " is missing.");
            string font = ResolvedFont(element);
            Assert.IsNotNull(font, name + " resolved to the Unity default font.");
            StringAssert.Contains(expected, font, name + " must use the original " + expected + " face.");
        }

        private static void AssertFirstTextFont(VisualElement container, string expected, string label)
        {
            Assert.IsNotNull(container, label + " is missing.");
            TextElement first = null;
            foreach (TextElement element in container.Query<TextElement>().Build())
            {
                if (string.IsNullOrEmpty(element.text)) continue;
                first = element;
                break;
            }

            Assert.IsNotNull(first, label + " has no text.");
            StringAssert.Contains(expected, ResolvedFont(first), label + " must use the original " + expected + " face.");
        }

        private static string ResolvedFont(VisualElement element)
        {
            FontDefinition definition = element.resolvedStyle.unityFontDefinition;
            if (definition.font != null) return definition.font.name;
            if (definition.fontAsset != null) return definition.fontAsset.name;
            return null;
        }

        private static string Describe(VisualElement element)
        {
            var path = new System.Collections.Generic.List<string>();
            for (VisualElement current = element; current != null && path.Count < 4; current = current.parent)
                path.Add(string.IsNullOrEmpty(current.name)
                    ? current.GetType().Name
                    : current.name + "[" + current.GetType().Name + "]");
            return string.Join("/", path);
        }

        private static void AssertTextMetrics(VisualElement root, string surface, bool checkWidth)
        {
            foreach (TextElement element in root.Query<TextElement>().Build())
            {
                if (string.IsNullOrEmpty(element.text)) continue;
                if (element.resolvedStyle.whiteSpace == WhiteSpace.Normal) continue;

                Vector2 natural = element.MeasureTextSize(
                    element.text, 0, VisualElement.MeasureMode.Undefined, 0, VisualElement.MeasureMode.Undefined);
                Rect box = element.layout;
                if (!float.IsNaN(element.resolvedStyle.height) && box.height > 0f)
                {
                    Assert.LessOrEqual(natural.y, box.height + 2f,
                        surface + "/" + Describe(element) + " line height " + natural.y +
                        " overflows its " + box.height + "px box (style height " +
                        element.resolvedStyle.height + ") at fontSize " + element.resolvedStyle.fontSize);
                }

                if (checkWidth && !float.IsNaN(element.resolvedStyle.width) && box.width > 0f &&
                    element.resolvedStyle.textOverflow == TextOverflow.Clip)
                {
                    Assert.LessOrEqual(natural.x, box.width + 2f,
                        surface + "/" + Describe(element) + " text width " + natural.x +
                        " overflows its " + box.width + "px box: '" + element.text + "'");
                }
            }
        }

        private static IEnumerator LoadBoot()
        {
            yield return PlayModeBoot.LoadCenter();
        }

        private static void Click(Button button)
        {
            Assert.IsNotNull(button);
            var simulate = typeof(Clickable).GetMethod(
                "SimulateSingleClick",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(simulate, "UI Toolkit Clickable.SimulateSingleClick moved.");
            simulate.Invoke(button.clickable, new object[] { null, 100 });
        }
    }
}
