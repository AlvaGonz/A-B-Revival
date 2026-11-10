using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ABRevival.Core.Data;
using ABRevival.Core.Persistence;
using ABRevival.Core.Shop;
using ABRevival.Core.Simulation;
using ABRevival.Core.Story;
using ABRevival.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// Boot-scene verification for Results/Repair (#180): the real mission cause and reward,
    /// Next/Retry/Back persistence, the frozen repair ledger, full-bleed layout at three sizes
    /// and 1920x1080 review captures. Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class ResultsPlayModeTests
    {
        private const int WaitFrameLimit = 900;
        private const float SeededPartHealth = 1000000f;

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
            DestroyGameFlow();
            DeleteSave();
            if (ABRevival.Presentation.Audio.AudioService.Instance != null)
                Object.Destroy(ABRevival.Presentation.Audio.AudioService.Instance.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Results_Win_ShowsRealCauseAndRepairsTheDamagedPart()
        {
            SeedCraft();
            yield return LoadBoot();
            yield return LaunchAndWin(withDamage: true);

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            ResultsController results = Object.FindAnyObjectByType<ResultsController>();
            Assert.IsNotNull(results, "Boot must host the dedicated Results screen.");
            VisualElement root = ActiveRoot(results);

            Assert.AreEqual(GameFlowState.Result, flow.State);
            Assert.AreEqual(DisplayStyle.Flex, root.resolvedStyle.display);
            Assert.AreEqual("You won!", root.Q<Label>("results-title").text);
            Assert.AreEqual("BATTLE-1", root.Q<Label>("results-battle").text);
            StringAssert.Contains("Objective complete", root.Q<Label>("results-cause").text);
            StringAssert.Contains("clock_2", root.Q<Label>("results-cause").text);
            Assert.AreEqual("Money: +0G", root.Q<Label>("results-money").text);
            Assert.AreEqual("Parts: 1", root.Q<Label>("results-parts").text);
            Assert.AreEqual("Repair needed", root.Q<Label>("results-verdict").text);
            Assert.AreEqual(DisplayStyle.Flex, root.Q<Button>("results-repair").resolvedStyle.display);
            Assert.AreEqual(1, flow.CurrentRepair.Items.Count);
            Assert.AreEqual(1438, flow.CurrentRepair.Items[0].RepairCost,
                "G.25.X1.01 costs 2055; FormRepair charges 70%.");

            Click(root.Q<Button>("results-repair"));
            for (int i = 0; i < 6; i++) yield return null;
            Assert.AreEqual(GameFlowState.Repair, flow.State);

            RepairController repair = Object.FindAnyObjectByType<RepairController>();
            Assert.IsNotNull(repair);
            VisualElement repairRoot = ActiveRoot(repair);
            Assert.AreEqual(DisplayStyle.Flex, repairRoot.resolvedStyle.display);
            Assert.AreEqual(1, repair.VisibleRowCount);
            Assert.IsNotNull(repairRoot.Q("repair-row-0"), "The damaged gun must be listed.");
            StringAssert.Contains("Michael's", repairRoot.Q<Label>("repair-craft-list").text);
            Assert.AreEqual("0 G", repairRoot.Q<Label>("repair-cost").text);
            Assert.AreEqual("100,000 G", repairRoot.Q<Label>("repair-now").text);

            Click(repairRoot.Q<Button>("repair-all"));
            yield return null;
            Assert.AreEqual("1,438 G", repairRoot.Q<Label>("repair-cost").text);
            Assert.AreEqual("98,562 G", repairRoot.Q<Label>("repair-now").text);

            Click(repairRoot.Q<Button>("repair-accept"));
            for (int i = 0; i < 6; i++) yield return null;

            Assert.AreEqual(GameFlowState.Result, flow.State, "ACCEPT must return to the result screen.");
            Assert.AreEqual(0, flow.CurrentRepair.Items.Count);
            Assert.AreEqual("Parts: 0", root.Q<Label>("results-parts").text);
            Assert.AreEqual("Perfect", root.Q<Label>("results-verdict").text);

            PlayerSaveData saved = PlayerSaveCodec.LoadFromFile(SavePath);
            Assert.AreEqual(98562, saved.shop.money, "Accepting the repair must charge the frozen price.");
            Assert.AreEqual(SeededPartHealth, saved.blueprints[0].mountedParts[1].health,
                "The repaired gun must be back at full health.");
            Assert.AreEqual(0, saved.storyIndex, "Repairing must not advance STORY.");

            Click(root.Q<Button>("results-right"));
            yield return WaitForState(flow, GameFlowState.Center);

            Assert.AreEqual(1, flow.StoryIndex, "Next advances STORY.");
            saved = PlayerSaveCodec.LoadFromFile(SavePath);
            Assert.AreEqual(1, saved.storyIndex, "Next persists the advanced STORY.");
            Assert.AreEqual(4, saved.blueprints[0].mountedParts.Count,
                "The repaired gun must stay mounted on the craft.");
        }

        [UnityTest]
        public IEnumerator Results_NextWithoutRepair_ReturnsThePartToShopStock()
        {
            SeedCraft();
            yield return LoadBoot();
            yield return LaunchAndWin(withDamage: true);

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            ResultsController results = Object.FindAnyObjectByType<ResultsController>();
            VisualElement root = ActiveRoot(results);

            Click(root.Q<Button>("results-right"));
            yield return WaitForState(flow, GameFlowState.Center);

            Assert.AreEqual(1, flow.StoryIndex);
            PlayerSaveData saved = PlayerSaveCodec.LoadFromFile(SavePath);
            Assert.AreEqual(1, saved.storyIndex);
            Assert.AreEqual(3, saved.blueprints[0].mountedParts.Count,
                "UIWin.toNext removes only the unrepaired part from the craft.");
            Assert.AreEqual(0, saved.blueprints[0].mountedParts[0].partId);

            int index = -1;
            for (int i = 0; i < saved.shop.stock.Count; i++)
            {
                if (saved.shop.stock[i].partId == 71) index = i;
            }

            Assert.GreaterOrEqual(index, 0, "The lost gun must return as a shop buy-back row.");
            Assert.AreEqual(0, saved.shop.stock[index].quantity,
                "Non-body buy-back rows are infinite (quantity 0).");
        }

        [UnityTest]
        public IEnumerator Results_Fail_ShowsTheLossAndDoesNotAdvanceOrAutosave()
        {
            SeedCraft();
            yield return LoadBoot();
            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();

            string saveBefore = File.ReadAllText(SavePath);
            flow.LaunchMission();
            yield return WaitForState(flow, GameFlowState.Briefing);
            flow.SkipBriefing();
            yield return WaitForState(flow, GameFlowState.Mission);

            int playerIndex = flow.Runner.PlayerShipIndex;
            Assert.GreaterOrEqual(playerIndex, 0);
            flow.Runner.NotifyCraftDestroyed(playerIndex);
            flow.Tick(GameFlowController.FixedStepSeconds);
            yield return WaitForState(flow, GameFlowState.Fail);

            ResultsController results = Object.FindAnyObjectByType<ResultsController>();
            VisualElement root = ActiveRoot(results);
            Assert.AreEqual("Battle failed", root.Q<Label>("results-title").text);
            StringAssert.Contains("Craft destroyed", root.Q<Label>("results-cause").text);
            StringAssert.Contains("everything has gone", root.Q<Label>("results-body").text);
            Assert.AreEqual("Money: +0G", root.Q<Label>("results-money").text);
            Assert.AreEqual("Parts: 0", root.Q<Label>("results-parts").text);
            Assert.AreEqual("Craft destroyed", root.Q<Label>("results-verdict").text);
            Assert.AreEqual(DisplayStyle.None, root.Q<Button>("results-repair").resolvedStyle.display,
                "UIFail has no REPAIR action.");

            Click(root.Q<Button>("results-right"));
            yield return WaitForState(flow, GameFlowState.Center);

            Assert.AreEqual(0, flow.StoryIndex, "A loss must not advance STORY.");
            Assert.AreEqual(saveBefore, File.ReadAllText(SavePath), "A loss must not autosave.");
        }

        [UnityTest]
        public IEnumerator Results_Win_PersistsANonzeroRewardThroughTheMissionEndSave()
        {
            SeedCraft();
            yield return LoadBoot();
            yield return LaunchMission();

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            // Story 0 pays 0; raise the live campaign index so EvtCtrl.win pays one battle.
            SetStoryIndex(flow, 1);
            Assert.IsTrue(flow.ForceWin(), "The reference mission must reach its own win objective.");
            Assert.AreEqual(5454, flow.LastSummary.reward);
            Assert.AreEqual(GameFlowState.Result, flow.State);

            PlayerSaveData saved = PlayerSaveCodec.LoadFromFile(SavePath);
            Assert.AreEqual(105454, saved.shop.money,
                "The mission-end autosave must keep the reward; Center's pre-mission shop snapshot must not overwrite it.");
            Assert.AreEqual(1, saved.storyIndex, "The reward save still belongs to the current battle.");

            Click(ActiveRoot(Object.FindAnyObjectByType<ResultsController>()).Q<Button>("results-right"));
            yield return WaitForState(flow, GameFlowState.Center);

            CenterController center = Object.FindAnyObjectByType<CenterController>();
            Assert.AreEqual(105454, center.Money, "Center must show the rewarded purse after the result.");
            saved = PlayerSaveCodec.LoadFromFile(SavePath);
            Assert.AreEqual(105454, saved.shop.money);
        }

        [UnityTest]
        public IEnumerator Results_RetryRefundsTheRewardAndDoesNotAdvance()
        {
            SeedCraft();
            yield return LoadBoot();
            yield return LaunchMission();

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            SetStoryIndex(flow, 1);
            Assert.IsTrue(flow.ForceWin());
            Assert.AreEqual(105454, PlayerSaveCodec.LoadFromFile(SavePath).shop.money);

            Click(ActiveRoot(Object.FindAnyObjectByType<ResultsController>()).Q<Button>("results-left"));
            yield return WaitForState(flow, GameFlowState.Center);

            Assert.AreEqual(1, flow.StoryIndex, "Retry must not advance STORY.");
            Assert.AreEqual(100000, Object.FindAnyObjectByType<CenterController>().Money,
                "Retry must return the purse captured before the reward.");
            Assert.AreEqual(100000, PlayerSaveCodec.LoadFromFile(SavePath).shop.money,
                "The retry refund must be persisted, not only shown in memory.");
        }

        [UnityTest]
        public IEnumerator Repair_PartialAccept_ClampsThePageIndex()
        {
            SeedCraft();
            yield return LoadBoot();

            RepairController repair = Object.FindAnyObjectByType<RepairController>();
            PlayerSaveData save = PlayerSaveCodec.LoadFromFile(SavePath);
            BlueprintSaveData blueprint = save.blueprints[0];
            while (blueprint.mountedParts.Count < 12)
            {
                blueprint.mountedParts.Add(new MountedPartSaveData
                {
                    partId = 71,
                    category = PartCategory.Gun,
                    health = 1f,
                    maxHealth = SeededPartHealth,
                    mass = 17f
                });
            }

            var damage = new List<DamagedPart>();
            for (int i = 0; i < 11; i++)
                damage.Add(new DamagedPart(0, i, blueprint.mountedParts[i].partId, blueprint.mountedParts[i].category));

            RepairState state = repair.CreateRepairState(save, damage);
            repair.Initialize(save, state);
            yield return null;

            VisualElement root = ActiveRoot(repair);
            Click(root.Q<Button>("repair-next"));
            yield return null;
            Assert.AreEqual(1, repair.PageIndex, "11 damaged parts occupy two repair pages.");

            Click(root.Q<Button>("repair-all"));
            yield return null;
            Click(root.Q<Button>("repair-accept"));
            for (int i = 0; i < 6; i++) yield return null;

            Assert.AreEqual(0, state.Items.Count, "ACCEPT must repair every selected part.");
            Assert.AreEqual(0, repair.PageIndex, "The empty ledger must clamp the page index back to the first page.");
            Assert.AreEqual(0, repair.VisibleRowCount);
        }

        [UnityTest]
        public IEnumerator Results_FillsTheRootAt1080p720pAnd16By10()
        {
            SeedCraft();
            yield return LoadBoot();

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            ResultsController results = Object.FindAnyObjectByType<ResultsController>();
            RepairController repair = Object.FindAnyObjectByType<RepairController>();
            var resultsPanel = results.GetComponent<ScreenPanel>();
            var repairPanel = repair.GetComponent<ScreenPanel>();
            Assert.IsNotNull(resultsPanel);
            Assert.IsNotNull(repairPanel);

            PanelSettings settings = resultsPanel.PanelSettings;
            PanelScaleMode previousScaleMode = settings.scaleMode;
            RenderTexture previous = settings.targetTexture;

            PlayerSaveData save = PlayerSaveCodec.LoadFromFile(SavePath);
            RepairState state = repair.CreateRepairState(save, new List<DamagedPart>
            {
                new DamagedPart(0, 1, 71, PartCategory.Gun),
                new DamagedPart(0, 2, 173, PartCategory.Wing),
                new DamagedPart(0, 3, 133, PartCategory.Engine)
            });

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
                    settings.scaleMode = PanelScaleMode.ConstantPixelSize;
                    settings.targetTexture = target;

                    results.Initialize(WinSummary(damaged: 2));
                    yield return WaitFrames(8);

                    VisualElement root = resultsPanel.Root;
                    VisualElement page = root.Q("results-page");
                    VisualElement panel = root.Q("results-panel");
                    VisualElement actions = root.Q("results-actions");
                    VisualElement repairRow = root.Q("results-repair-row");
                    Assert.IsNotNull(page);
                    Assert.IsNotNull(panel);
                    Assert.IsNotNull(actions);
                    Assert.IsNotNull(repairRow);

                    Rect surface = resultsPanel.Root.Q("results-root").worldBound;
                    Rect pageBounds = page.worldBound;
                    Debug.Log("[Results Layout] " + size + " surface=" + surface + " page=" + pageBounds +
                              " panel=" + panel.worldBound);
                    Assert.AreEqual(size.x, surface.width, 1f, "Results root width at " + size);
                    Assert.AreEqual(size.y, surface.height, 1f, "Results root height at " + size);
                    Assert.GreaterOrEqual(pageBounds.width, surface.width - 1f, "Results page covers width at " + size);
                    Assert.GreaterOrEqual(pageBounds.height, surface.height - 1f, "Results page covers height at " + size);
                    Assert.LessOrEqual(pageBounds.xMin, surface.xMin + 1f, "No horizontal letterbox at " + size);
                    Assert.LessOrEqual(pageBounds.yMin, surface.yMin + 1f, "No vertical letterbox at " + size);
                    Assert.Greater(panel.worldBound.width, 500f, "Results panel must stay readable at " + size);
                    Assert.LessOrEqual(panel.worldBound.yMin, actions.worldBound.yMin, "Panel above actions at " + size);
                    Assert.LessOrEqual(actions.worldBound.yMax, repairRow.worldBound.yMin + 1f,
                        "Result actions above the repair row at " + size);
                    Assert.LessOrEqual(repairRow.worldBound.yMax, pageBounds.yMax + 1f,
                        "Repair row inside the page at " + size);

                    results.SetVisible(false);
                    repair.Initialize(save, state);
                    yield return WaitFrames(8);

                    VisualElement repairRoot = repairPanel.Root;
                    VisualElement repairPage = repairRoot.Q("repair-page");
                    VisualElement table = repairRoot.Q("repair-table");
                    VisualElement header = repairRoot.Q("repair-header");
                    VisualElement detail = repairRoot.Q("repair-detail");
                    VisualElement repairActions = repairRoot.Q("repair-actions");
                    Assert.IsNotNull(table);
                    Assert.IsNotNull(header);
                    Assert.IsNotNull(detail);
                    Assert.IsNotNull(repairActions);

                    Rect repairSurface = repairRoot.Q("repair-root").worldBound;
                    Rect repairPageBounds = repairPage.worldBound;
                    Debug.Log("[Repair Layout] " + size + " surface=" + repairSurface + " page=" + repairPageBounds +
                              " table=" + table.worldBound + " actions=" + repairActions.worldBound);
                    Assert.AreEqual(size.x, repairSurface.width, 1f, "Repair root width at " + size);
                    Assert.AreEqual(size.y, repairSurface.height, 1f, "Repair root height at " + size);
                    Assert.GreaterOrEqual(repairPageBounds.width, repairSurface.width - 1f,
                        "Repair page covers width at " + size);
                    Assert.GreaterOrEqual(repairPageBounds.height, repairSurface.height - 1f,
                        "Repair page covers height at " + size);
                    Assert.LessOrEqual(repairPageBounds.xMin, repairSurface.xMin + 1f,
                        "No horizontal letterbox at " + size);
                    Assert.LessOrEqual(repairPageBounds.yMin, repairSurface.yMin + 1f,
                        "No vertical letterbox at " + size);
                    Assert.LessOrEqual(header.worldBound.yMin, table.worldBound.yMin, "Header above the table at " + size);
                    Assert.LessOrEqual(table.worldBound.yMax, detail.worldBound.yMin + 1f,
                        "Table above the detail card at " + size);
                    Assert.LessOrEqual(detail.worldBound.yMax, repairActions.worldBound.yMin + 1f,
                        "Detail above the action row at " + size);
                    Assert.Greater(repairActions.Q<Button>("repair-accept").worldBound.width, 120f,
                        "ACCEPT must stay tappable at " + size);

                    repair.SetVisible(false);
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
        public IEnumerator Results_Renders1920x1080CapturesOutsideTheProject()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Capture requires a graphics device; run the PlayMode gate without -nographics.");

            SeedCraft();
            yield return LoadBoot();

            ResultsController results = Object.FindAnyObjectByType<ResultsController>();
            RepairController repair = Object.FindAnyObjectByType<RepairController>();
            var panel = results.GetComponent<ScreenPanel>();
            PanelSettings settings = panel.PanelSettings;
            RenderTexture previous = settings.targetTexture;
            var target = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
            target.Create();

            PlayerSaveData save = PlayerSaveCodec.LoadFromFile(SavePath);
            RepairState state = repair.CreateRepairState(save, new List<DamagedPart>
            {
                new DamagedPart(0, 1, 71, PartCategory.Gun),
                new DamagedPart(0, 2, 173, PartCategory.Wing),
                new DamagedPart(0, 3, 133, PartCategory.Engine)
            });

            string folder = Path.Combine(Path.GetTempPath(), "opencode");
            Directory.CreateDirectory(folder);

            try
            {
                settings.targetTexture = target;
                yield return WaitFrames(12);

                results.Initialize(WinSummary(damaged: 2));
                yield return WaitFrames(12);
                AssertResultArt(results, "ui_0005");
                AssertFont(results, "results-title", "font_0007_SF_TransRobotics");
                AssertFont(results, "results-tip", "font_0002_sans");
                string winPath = Path.Combine(folder, "results-win-1920x1080.png");
                WriteCapture(target, winPath);

                results.SetVisible(false);
                repair.Initialize(save, state);
                yield return WaitFrames(12);
                VisualElement repairRoot = repair.GetComponent<ScreenPanel>().Root;
                Assert.IsNotNull(repairRoot.Q("repair-row-0"));
                Image exitArt = repairRoot.Q<Button>("repair-exit").Q<Image>("sprite-art");
                Assert.IsNotNull(exitArt, "Repair EXIT must render through an Image child.");
                StringAssert.Contains("215_widget", exitArt.sprite.name);
                AssertFont(repair, "repair-title", "font_0007_SF_TransRobotics");
                string repairPath = Path.Combine(folder, "repair-damaged-1920x1080.png");
                WriteCapture(target, repairPath);

                repair.SetVisible(false);
                results.Initialize(FailSummary());
                yield return WaitFrames(12);
                AssertResultArt(results, "ui_0511");
                string failPath = Path.Combine(folder, "results-fail-1920x1080.png");
                WriteCapture(target, failPath);

                results.Initialize(WinSummary(damaged: 0, storyIndex: StoryDirector.MaxStoryIndex));
                yield return WaitFrames(12);
                Assert.AreEqual("Story Mode Completed!", results.GetComponent<ScreenPanel>()
                    .Root.Q<Label>("results-title").text);
                Image endingArt = results.GetComponent<ScreenPanel>().Root
                    .Q<Button>("results-left").Q<Image>("sprite-art");
                Assert.IsNotNull(endingArt);
                StringAssert.Contains("614_widget", endingArt.sprite.name);
                string allWinPath = Path.Combine(folder, "results-allwin-1920x1080.png");
                WriteCapture(target, allWinPath);

                AssertCaptureHasContent(winPath, "Results win");
                AssertCaptureHasContent(repairPath, "Repair");
                AssertCaptureHasContent(failPath, "Results fail");
                AssertCaptureHasContent(allWinPath, "Results all-win");
                Debug.Log("[Results Capture] win=" + winPath + " repair=" + repairPath + " fail=" + failPath +
                          " allwin=" + allWinPath);
            }
            finally
            {
                settings.targetTexture = previous;
                RenderTexture.active = null;
                target.Release();
                Object.Destroy(target);
            }
        }

        // ---------------- Helpers ----------------

        private static MissionSummary WinSummary(int damaged, int storyIndex = 0)
        {
            return new MissionSummary(
                MissionState.Win,
                MissionEndCause.WindirAct,
                "clock_2",
                storyIndex,
                MissionSummary.LevelMoney(storyIndex, damaged),
                damaged);
        }

        private static MissionSummary FailSummary()
        {
            return new MissionSummary(
                MissionState.Lose,
                MissionEndCause.PlayerDestroyed,
                "Michael",
                0,
                0,
                1);
        }

        private static IEnumerator LaunchMission()
        {
            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow);
            flow.LaunchMission();
            yield return WaitForState(flow, GameFlowState.Briefing);
            flow.SkipBriefing();
            yield return WaitForState(flow, GameFlowState.Mission);
        }

        private static void SetStoryIndex(GameFlowController flow, int storyIndex)
        {
            FieldInfo field = typeof(GameFlowController).GetField(
                "_save", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "GameFlowController._save moved.");
            ((PlayerSaveData)field.GetValue(flow)).storyIndex = storyIndex;
        }

        private static IEnumerator LaunchAndWin(bool withDamage)
        {
            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow);
            flow.LaunchMission();
            yield return WaitForState(flow, GameFlowState.Briefing);
            flow.SkipBriefing();
            yield return WaitForState(flow, GameFlowState.Mission);

            if (withDamage)
            {
                Assert.IsTrue(flow.MarkDamagedPartForVerification(0, 1),
                    "The seeded G.25.X1.01 gun must be damageable.");
            }

            // Remove every spawned hostile first so ForceWin completes on the win objective
            // without giving the AI a window to detach unrelated parts.
            var spawns = flow.Runner.Spawns;
            for (int i = 0; i < spawns.Count; i++)
            {
                MissionSpawnRecord spawn = spawns[i];
                if (!spawn.friendly && spawn.shipIndex >= 0)
                    flow.Runner.NotifyCraftDestroyed(spawn.shipIndex);
            }

            Assert.IsTrue(flow.ForceWin(), "The reference mission must reach its own win objective.");
            Assert.AreEqual(GameFlowState.Result, flow.State);
        }

        private static IEnumerator LoadBoot()
        {
            yield return PlayModeBoot.LoadCenter();
        }

        private static IEnumerator WaitForState(GameFlowController flow, GameFlowState expected)
        {
            for (int i = 0; i < WaitFrameLimit; i++)
            {
                if (flow.State == expected) yield break;
                yield return null;
            }

            Assert.Fail($"Flow did not reach {expected}; current state {flow.State}.");
        }

        private static IEnumerator WaitFrames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        private static VisualElement ActiveRoot(Component component)
        {
            return component != null ? component.GetComponent<ScreenPanel>()?.Root : null;
        }

        private static void AssertResultArt(ResultsController results, string spriteName)
        {
            VisualElement root = ActiveRoot(results);
            Image art = root.Q("results-background").Q<Image>("sprite-art");
            Assert.IsNotNull(art, "The result backdrop must render through an Image child.");
            Assert.IsNotNull(art.sprite);
            StringAssert.Contains(spriteName, art.sprite.name);

            Image right = root.Q<Button>("results-right").Q<Image>("sprite-art");
            Assert.IsNotNull(right, "The result action must render through an Image child.");
            Assert.IsNotNull(right.sprite);
        }

        private static void AssertFont(Component screen, string name, string expected)
        {
            Label label = ActiveRoot(screen).Q<Label>(name);
            Assert.IsNotNull(label, name + " is missing.");
            FontDefinition definition = label.resolvedStyle.unityFontDefinition;
            string font = definition.font != null
                ? definition.font.name
                : definition.fontAsset != null ? definition.fontAsset.name : null;
            Assert.IsNotNull(font, name + " resolved to the Unity default font.");
            StringAssert.Contains(expected, font, name + " must use the original " + expected + " face.");
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

        private static void Click(Button button)
        {
            Assert.IsNotNull(button);
            MethodInfo simulate = typeof(Clickable).GetMethod(
                "SimulateSingleClick", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(simulate, "UI Toolkit Clickable.SimulateSingleClick moved.");
            simulate.Invoke(button.clickable, new object[] { null, 100 });
        }

        private static void DestroyGameFlow()
        {
            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            if (flow != null) Object.Destroy(flow.gameObject);
        }

        private static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(SavePath + ".corrupt")) File.Delete(SavePath + ".corrupt");
            if (File.Exists(SavePath + ".tmp")) File.Delete(SavePath + ".tmp");
        }

        private static void SeedCraft()
        {
            var save = new PlayerSaveData { shop = new ShopSaveData { money = 100000 } };
            var blueprint = new BlueprintSaveData
            {
                craftName = "Michael's",
                hullName = "B.Small.01",
                hullCategory = HullCategory.Small,
                hullMass = 100f,
                hullHealth = 1300f
            };
            blueprint.mountedParts.Add(new MountedPartSaveData
            {
                partId = 0,
                category = PartCategory.Body,
                health = SeededPartHealth,
                maxHealth = SeededPartHealth,
                mass = 100f,
                thrust = 5f,
                localRotation = 0f
            });
            blueprint.mountedParts.Add(new MountedPartSaveData
            {
                partId = 71,
                category = PartCategory.Gun,
                health = SeededPartHealth,
                maxHealth = SeededPartHealth,
                mass = 17f,
                localRotation = 0f
            });
            blueprint.mountedParts.Add(new MountedPartSaveData
            {
                partId = 173,
                category = PartCategory.Wing,
                health = SeededPartHealth,
                maxHealth = SeededPartHealth,
                mass = 30f,
                localRotation = 0f
            });
            blueprint.mountedParts.Add(new MountedPartSaveData
            {
                partId = 133,
                category = PartCategory.Engine,
                health = SeededPartHealth,
                maxHealth = SeededPartHealth,
                mass = 20f,
                thrust = 100f,
                localRotation = 0f
            });
            save.blueprints.Add(blueprint);
            save.playerBlueprintIndex = 0;
            PlayerSaveCodec.SaveToFile(SavePath, save);
        }
    }
}
