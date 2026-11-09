using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ABRevival.Core.Data;
using ABRevival.Core.Persistence;
using ABRevival.Core.Simulation;
using ABRevival.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// Boot-scene verification for the Ready/Briefing handoff (#178): the frozen
    /// UI_MapBuilding page composition, validated Player/AI loadout, talkBefore + opening
    /// cinematic launch, Back cancel, wingman deployment, full-bleed layout at three sizes
    /// and a 1920x1080 review capture. Original game IP: Vargames (2011).
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
        public IEnumerator Ready_ShowsFrozenBriefingAndLaunchesWithSelectedCraftAndCompanions()
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
            AssertFont(root, "loadout-status", "font_0002_sans");
            StringAssert.Contains("Wang is fighting", root.Q<Label>("briefing-words").text,
                "The map page must print the frozen missionWords[STORY] summary.");
            StringAssert.Contains("PLAYER " + center.CraftName(player), root.Q<Label>("loadout-status").text);
            StringAssert.Contains("AI 2/5", root.Q<Label>("loadout-status").text);
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
        public IEnumerator Ready_SpawnsTwoWingmenThatRenderAndFire()
        {
            yield return LoadBoot();

            CenterController center = Object.FindAnyObjectByType<CenterController>();
            Assert.IsTrue(center.AssignCompanion(2));
            Assert.IsTrue(center.AssignCompanion(3));
            yield return null;
            yield return OpenReady();

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            ClicksLaunchAndSkipsBriefing(flow, ActiveRoot(Object.FindAnyObjectByType<ReadyController>()));
            yield return WaitForState(flow, GameFlowState.Mission);

            Assert.AreEqual(2, flow.WingmanShipIndices.Count, "Two valid companions must spawn two wingmen.");
            int first = flow.WingmanShipIndices[0];
            int second = flow.WingmanShipIndices[1];
            Assert.AreEqual(0, flow.Simulation.GetShip(first).factionId, "Wingmen must be friendly.");
            Assert.AreEqual(0, flow.Simulation.GetShip(second).factionId);
            Assert.IsTrue(flow.Runner.IsCraftAlive(first));
            Assert.IsTrue(flow.Runner.IsCraftAlive(second));

            MissionSpriteView view = Object.FindAnyObjectByType<MissionSpriteView>();
            Assert.IsNotNull(view);
            Assert.IsTrue(view.IsActive, "The mission sprite view must be running.");
            Assert.Greater(view.InstanceCount, 0, "Wingmen must reach the existing sprite render path.");

            int firstHull = flow.HullForShip(first);
            int secondHull = flow.HullForShip(second);
            Assert.GreaterOrEqual(firstHull, 0, "Each wingman needs a contact hull for rendering and damage.");
            Assert.GreaterOrEqual(secondHull, 0);
            Assert.Greater(flow.ContactSolver.GetHullPartCount(firstHull), 0);
            Assert.Greater(flow.ContactSolver.GetHullPartCount(secondHull), 0);

            CombatHudBehaviour hud = Object.FindAnyObjectByType<CombatHudBehaviour>();
            Assert.IsNotNull(hud);
            Assert.AreEqual(2, hud.State.AiCount, "AI x N must count friendly wingmen, not hostiles.");

            int hostile = FindHostile(flow);
            Assert.GreaterOrEqual(hostile, 0, "Story 0 must spawn a hostile to fire at.");
            ShipEntity hostileShip = flow.Simulation.GetShip(hostile);
            ShipEntity wingman = flow.Simulation.GetShip(first);
            hostileShip.position = wingman.position + new Unity.Mathematics.float2(0f, 120f);
            hostileShip.velocity = Unity.Mathematics.float2.zero;
            hostileShip.thrustPower = 0f;
            hostileShip.tacticalMode = (int)CaptainTacticalMode.Anchor;
            hostileShip.spawnPosition = hostileShip.position;
            flow.Simulation.SetShip(hostile, hostileShip);

            var fireInput = flow.GetComponent<MouseCombatFireInput>();
            if (fireInput != null) fireInput.SetAutofire(false);
            int shotsBefore = flow.ShotsFiredBy(first) + flow.ShotsFiredBy(second);
            for (int i = 0; i < 90 && flow.State == GameFlowState.Mission; i++)
                flow.Tick(GameFlowController.FixedStepSeconds * 2f);
            int shotsAfter = flow.ShotsFiredBy(first) + flow.ShotsFiredBy(second);
            Assert.Greater(shotsAfter, shotsBefore, "Wingman weapons must fire through the AI weapon path.");

            // A hostile shot at a wingman must reduce its registered hull health.
            ShipEntity damagedWingman = flow.Simulation.GetShip(first);
            float healthBefore = flow.ContactSolver.GetHullHealth(firstHull);
            int projectileIndex = flow.Simulation.ProjectileCount;
            flow.Simulation.SetProjectile(projectileIndex, new ProjectileData
            {
                position = damagedWingman.position - new Unity.Mathematics.float2(5f, 0f),
                velocity = new Unity.Mathematics.float2(600f, 0f),
                lifetime = 1f,
                damage = 25f,
                factionId = 1,
                projectileType = 1,
                isAlive = true
            });
            flow.Simulation.ProjectileCount = projectileIndex + 1;
            flow.Tick(GameFlowController.FixedStepSeconds);
            float healthAfter = flow.ContactSolver.GetHullHealth(firstHull);
            Assert.Less(healthAfter, healthBefore, "Wingman hulls must take contact damage.");

            flow.Runner.NotifyCraftDestroyed(first);
            flow.Tick(GameFlowController.FixedStepSeconds);
            Assert.AreEqual(1, hud.State.AiCount,
                "AI x N must drop to the surviving friendly wingman count when one is destroyed.");
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
            PanelScaleMode previousScaleMode = settings.scaleMode;

            try
            {
                settings.scaleMode = PanelScaleMode.ConstantPixelSize;
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
                    VisualElement page = root.Q("ready-page");
                    VisualElement fill = root.Q("ready-window-fill");
                    VisualElement titlePlate = root.Q("ready-title-plate");
                    VisualElement words = root.Q("briefing-words");
                    VisualElement status = root.Q("loadout-status");
                    Button back = root.Q<Button>("back-button");
                    Button launch = root.Q<Button>("launch-button");
                    Assert.IsNotNull(readyRoot, "Ready root is missing.");
                    Assert.IsNotNull(page, "Ready page is missing.");
                    Assert.IsNotNull(fill, "Ready window fill is missing.");
                    Assert.IsNotNull(titlePlate, "Ready title plate is missing.");
                    Assert.IsNotNull(words, "Briefing words are missing.");
                    Assert.IsNotNull(status, "Loadout status is missing.");
                    Assert.IsNotNull(back, "Ready BACK is missing.");
                    Assert.IsNotNull(launch, "Ready launch button is missing.");

                    Rect surface = readyRoot.worldBound;
                    Rect pageBounds = page.worldBound;
                    Debug.Log("[Ready Layout] " + size + " surface=" + surface + " page=" + pageBounds +
                              " fill=" + fill.layout + " launch=" + launch.layout);
                    Assert.Greater(surface.width, 0f, "The root must have layout width at " + size);
                    Assert.Greater(surface.height, 0f, "The root must have layout height at " + size);
                    Assert.AreEqual(size.x, surface.width, 1f,
                        "The Ready root must track the output width at " + size);
                    Assert.AreEqual(size.y, surface.height, 1f,
                        "The Ready root must track the output height at " + size);
                    Assert.AreEqual(851f, page.layout.width, 1f,
                        "The page must keep its 851px composition width at " + size);
                    Assert.AreEqual(521f, page.layout.height, 1f,
                        "The page must keep its 521px composition height at " + size);
                    Assert.GreaterOrEqual(pageBounds.width, surface.width - 1f,
                        "The page must cover the root width at " + size);
                    Assert.GreaterOrEqual(pageBounds.height, surface.height - 1f,
                        "The page must cover the root height at " + size);
                    Assert.LessOrEqual(pageBounds.xMin, surface.xMin + 1f,
                        "The page must not letterbox horizontally at " + size);
                    Assert.LessOrEqual(pageBounds.yMin, surface.yMin + 1f,
                        "The page must not letterbox vertically at " + size);

                    Assert.AreEqual(241f, fill.resolvedStyle.left, 1.5f);
                    Assert.AreEqual(72f, fill.resolvedStyle.top, 1.5f);
                    Assert.AreEqual(374f, fill.layout.width, 1.5f);
                    Assert.AreEqual(263f, fill.layout.height, 1.5f);
                    Assert.AreEqual(345f, titlePlate.resolvedStyle.left, 1.5f);
                    Assert.AreEqual(90f, titlePlate.resolvedStyle.top, 1.5f);
                    Assert.AreEqual(290f, words.resolvedStyle.left, 1.5f);
                    Assert.AreEqual(140f, words.resolvedStyle.top, 1.5f);
                    Assert.AreEqual(270f, status.resolvedStyle.left, 1.5f);
                    Assert.AreEqual(305f, status.resolvedStyle.top, 1.5f);
                    Assert.AreEqual(354f, launch.resolvedStyle.left, 1.5f);
                    Assert.AreEqual(370f, launch.resolvedStyle.top, 1.5f);
                    Assert.GreaterOrEqual(words.layout.height, 140f, "The briefing lines must keep usable height.");
                    Assert.Greater(back.layout.width, 90f, "BACK must stay tappable at " + size);
                    Assert.Greater(launch.layout.width, 120f, "Launch must stay tappable at " + size);

                    target.Release();
                    Object.Destroy(target);
                }
            }
            finally
            {
                settings.scaleMode = previousScaleMode;
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
                StringAssert.Contains("AI 2/5", root.Q<Label>("loadout-status").text);
                AssertImageArt(root, "ready-page-art", "UI_MapBuilding");
                AssertImageArt(root, "ready-title-plate", "309_widget");
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

        private static void ClicksLaunchAndSkipsBriefing(GameFlowController flow, VisualElement root)
        {
            Click(root.Q<Button>("launch-button"));
            flow.SkipBriefing();
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
            yield return PlayModeBoot.LoadCenter();
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

        private static int FindHostile(GameFlowController flow)
        {
            var spawns = flow.Runner.Spawns;
            for (int i = 0; i < spawns.Count; i++)
            {
                if (!spawns[i].friendly && spawns[i].shipIndex >= 0) return spawns[i].shipIndex;
            }

            return -1;
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
