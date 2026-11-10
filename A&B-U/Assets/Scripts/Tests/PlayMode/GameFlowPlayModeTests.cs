using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using ABRevival.Core.Persistence;
using ABRevival.Core.Simulation;
using ABRevival.Presentation;
using ABRevival.Presentation.Audio;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// Story-0 loop smoke: boot scene loads the save, launches the reference mission through
    /// dialogue and cinematics, ticks combat at 60 Hz, forces the mission's own win objective,
    /// advances STORY, autosaves and returns to the center.
    /// </summary>
    [TestFixture]
    public class GameFlowPlayModeTests
    {
        private const int WaitFrames = 900;

        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, GameFlowController.DefaultSaveFileName);

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(SavePath + ".corrupt")) File.Delete(SavePath + ".corrupt");
            if (File.Exists(SavePath + ".tmp")) File.Delete(SavePath + ".tmp");
            if (AudioService.Instance != null) Object.Destroy(AudioService.Instance.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BootScene_LoadsSavedStoryAndShopState()
        {
            var seeded = new PlayerSaveData
            {
                storyIndex = 3,
                cameraPreset = ABRevival.Core.Camera.CameraDistancePreset.Far
            };
            seeded.shop.money = 777;
            seeded.shop.ownedParts.Add(71);
            PlayerSaveCodec.SaveToFile(SavePath, seeded);

            yield return PlayModeBoot.LoadBoot();
            yield return null;

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow);
            yield return WaitForState(flow, GameFlowState.Center);

            Assert.AreEqual(3, flow.StoryIndex, "Boot must restore the stored STORY index.");
            var center = Object.FindAnyObjectByType<CenterController>();
            Assert.IsNotNull(center, "Boot scene must host the center.");
            Assert.AreEqual(777, center.Money, "Boot must restore the stored shop money.");
            Assert.IsTrue(center.IsUnlocked(71),
                "Loading a save must reconcile missing catalog rows (part 71 had no stock row).");

            var cameraRig = Object.FindAnyObjectByType<CombatCameraRig>();
            Assert.IsNotNull(cameraRig);
            Assert.AreEqual(ABRevival.Core.Camera.CameraDistancePreset.Far, cameraRig.Preset,
                "Boot must apply the saved ADR 0005 camera preset.");
        }

        [UnityTest]
        public IEnumerator BootScene_CorruptSave_IsQuarantinedBeforeStartingFresh()
        {
            File.WriteAllText(SavePath, "{ not valid json");
            LogAssert.Expect(LogType.Error, new Regex("Save was corrupt.*"));

            yield return PlayModeBoot.LoadBoot();
            yield return null;

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow);
            yield return WaitForState(flow, GameFlowState.Center);

            Assert.IsTrue(File.Exists(SavePath + ".corrupt"),
                "An unreadable save must be moved aside, not silently overwritten.");
            Assert.IsFalse(File.Exists(SavePath), "A fresh campaign must not have written over the quarantined save.");
            Assert.AreEqual(0, flow.StoryIndex);
        }

        [UnityTest]
        public IEnumerator FuelPickup_RefillsTheTankAndTheHudBar()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);

            yield return PlayModeBoot.LoadBoot();
            yield return null;

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow);
            yield return WaitForState(flow, GameFlowState.Center);

            flow.LaunchMission();
            yield return WaitForState(flow, GameFlowState.Briefing);
            flow.SkipBriefing();
            yield return WaitForState(flow, GameFlowState.Mission);

            int playerIndex = flow.Runner.PlayerShipIndex;
            Assert.GreaterOrEqual(playerIndex, 0);

            ShipEntity ship = flow.Simulation.GetShip(playerIndex);
            ship.boosterFuel = 100f;
            flow.Simulation.SetShip(playerIndex, ship);

            var hud = Object.FindAnyObjectByType<CombatHudBehaviour>();
            Assert.IsNotNull(hud);
            hud.State.SetFuelFraction(100f / 500f);
            float fuelBefore = hud.State.FuelFraction;

            Assert.IsTrue(flow.CollectFuelPickup(), "The frozen fuel pickup must refill the player tank.");
            Assert.Greater(hud.State.FuelFraction, fuelBefore, "The fuel pickup must update the HUD bar.");
            Assert.AreEqual(350f / 500f, hud.State.FuelFraction, 0.001f);

            Assert.IsTrue(flow.CollectFuelPickup(10000f), "A large pickup must clamp at capacity.");
            Assert.AreEqual(1f, hud.State.FuelFraction, 0.001f);
            Assert.IsFalse(flow.CollectFuelPickup(), "A full tank must not report a pickup.");
        }

        [UnityTest]
        public IEnumerator BootScene_Lose_ReturnsWithoutAdvancingStory()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);

            yield return PlayModeBoot.LoadBoot();
            yield return null;

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow);
            yield return WaitForState(flow, GameFlowState.Center);

            flow.LaunchMission();
            yield return WaitForState(flow, GameFlowState.Briefing);
            flow.SkipBriefing();
            yield return WaitForState(flow, GameFlowState.Mission);

            int playerIndex = flow.Runner.PlayerShipIndex;
            Assert.GreaterOrEqual(playerIndex, 0);
            flow.Runner.NotifyCraftDestroyed(playerIndex);

            bool sawResult = false;
            for (int i = 0; i < WaitFrames && flow.State != GameFlowState.Center; i++)
            {
                if (flow.State == GameFlowState.Fail)
                {
                    sawResult = true;
                    flow.SkipResult();
                }

                yield return null;
            }

            Assert.IsTrue(sawResult, "A loss must be observable in the Fail state before returning.");
            Assert.AreEqual(GameFlowState.Center, flow.State);
            Assert.IsTrue(flow.LastResult.HasValue);
            Assert.AreEqual(MissionState.Lose, flow.LastResult.Value.state);
            Assert.AreEqual(0, flow.StoryIndex, "A loss must not advance STORY.");
            Assert.IsFalse(File.Exists(SavePath), "A loss must not autosave.");
        }

        [UnityTest]
        public IEnumerator BootScene_LaunchForcedWin_AdvancesStoryAutosavesAndReturns()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);

            yield return PlayModeBoot.LoadBoot();
            yield return null;

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow, "Boot scene must contain a GameFlowController.");

            yield return WaitForState(flow, GameFlowState.Center);
            Assert.AreEqual(0, flow.StoryIndex, "A new campaign starts at STORY 0.");
            Assert.IsNull(flow.Runner, "No mission runs in the center.");

            flow.LaunchMission();
            yield return WaitForState(flow, GameFlowState.Briefing);
            flow.SkipBriefing();
            yield return WaitForState(flow, GameFlowState.Mission);

            Assert.IsNotNull(flow.Runner, "Launching must create the mission runner.");
            Assert.IsNotNull(flow.Simulation, "The mission must run on the scheduled simulation.");
            Assert.IsNotNull(flow.ContactSolver, "The projectile contact layer must be wired.");
            Assert.AreEqual(MissionState.InProgress, flow.Runner.State);

            // The shipped scene assigns the imported part catalog; the default blueprint must be
            // armed and reach WeaponSystem through the real launch path.
            var fireInput = flow.GetComponent<MouseCombatFireInput>();
            Assert.IsNotNull(fireInput, "The flow must own the single combat fire input.");
            fireInput.SetAutofire(true);
            for (int i = 0; i < 20; i++) flow.Tick(GameFlowController.FixedStepSeconds);
            Assert.Greater(flow.ShotsFired, 0,
                "The default loadout must mount a gun; the real setup path produced no projectiles.");
            fireInput.SetAutofire(false);

            // Warm up Burst/job reflection before measuring the 60 Hz flow path.
            for (int i = 0; i < 30; i++) flow.Tick(GameFlowController.FixedStepSeconds);

            long allocatedBefore = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 60; i++) flow.Tick(GameFlowController.FixedStepSeconds);
            long allocatedBytes = System.GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            Assert.AreEqual(0, allocatedBytes,
                $"Game flow allocated {allocatedBytes} bytes over 60 mission ticks. Per-frame allocation contract violated.");

            Assert.IsTrue(flow.ForceWin(), "The reference mission must reach its own win objective.");
            Assert.AreEqual(GameFlowState.Result, flow.State);
            Assert.IsTrue(flow.LastResult.HasValue);
            Assert.AreEqual(MissionState.Win, flow.LastResult.Value.state);
            Assert.AreEqual(0, flow.StoryIndex,
                "UIWin keeps STORY at the current battle until Next (toNext/nextGame).");

            Assert.IsFalse(flow.ForceWin(), "A consumed mission result must not be reusable.");

            int guard = 0;
            while (flow.State != GameFlowState.Center && guard < WaitFrames)
            {
                if (flow.State == GameFlowState.Result)
                {
                    flow.AdvanceTalk();
                    flow.SkipResult();
                }

                guard++;
                yield return null;
            }

            Assert.AreEqual(GameFlowState.Center, flow.State, "The flow must return to the center after the result.");
            Assert.IsNull(flow.Runner, "Mission state is disposed on return.");
            Assert.AreEqual(1, flow.StoryIndex, "Next advances STORY before returning to Center.");

            Assert.IsTrue(File.Exists(SavePath), "A win must autosave the player payload.");
            PlayerSaveData saved = PlayerSaveCodec.LoadFromFile(SavePath);
            Assert.AreEqual(1, saved.storyIndex, "The autosave must persist the advanced STORY index.");

            // A second launch must resolve the level from the advanced STORY, not replay story 0.
            flow.LaunchMission();
            yield return WaitForState(flow, GameFlowState.Briefing);
            flow.SkipBriefing();
            yield return WaitForState(flow, GameFlowState.Mission);

            Assert.AreEqual(1, flow.StoryIndex);
            Assert.AreEqual(2, flow.Runner.Level.storyKey,
                "The new launch must load the level for the saved STORY index.");
        }

        [UnityTest]
        public IEnumerator Launch_ECyclesTheLiveCaptainAndIgnoresADestroyedCaptain()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);

            yield return PlayModeBoot.LoadBoot();
            yield return null;

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow);
            yield return WaitForState(flow, GameFlowState.Center);

            flow.LaunchMission();
            yield return WaitForState(flow, GameFlowState.Briefing);
            flow.SkipBriefing();
            yield return WaitForState(flow, GameFlowState.Mission);

            int playerIndex = flow.Runner.PlayerShipIndex;
            int captainIndex = -1;
            var spawns = flow.Runner.Spawns;
            for (int i = 0; i < spawns.Count; i++)
            {
                if (spawns[i].friendly && spawns[i].shipIndex >= 0 && spawns[i].shipIndex != playerIndex)
                {
                    captainIndex = spawns[i].shipIndex;
                    break;
                }
            }

            Assert.GreaterOrEqual(captainIndex, 0, "Story 0 must deploy Captain Tom.");
            Assert.AreEqual(CaptainTacticalMode.Anchor, (CaptainTacticalMode)flow.Simulation.GetShip(captainIndex).tacticalMode);

            Assert.IsTrue(flow.CycleCaptainTacticalMode());
            Assert.AreEqual(CaptainTacticalMode.Escort,
                (CaptainTacticalMode)flow.Simulation.GetShip(captainIndex).tacticalMode);
            var hud = Object.FindAnyObjectByType<CombatHudBehaviour>();
            Assert.IsNotNull(hud);
            Assert.AreEqual(CaptainTacticalMode.Escort, hud.State.CaptainMode);

            flow.Runner.NotifyCraftDestroyed(captainIndex);
            Assert.IsFalse(flow.CycleCaptainTacticalMode(), "E must do nothing once the Captain is gone.");
            Assert.AreEqual(CaptainTacticalMode.Escort,
                (CaptainTacticalMode)flow.Simulation.GetShip(captainIndex).tacticalMode);
        }

        [UnityTest]
        public IEnumerator BootScene_HostileFire_DamagesAndDestroysPlayer_ReachesLose()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);

            yield return PlayModeBoot.LoadBoot();
            yield return null;

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow);
            yield return WaitForState(flow, GameFlowState.Center);

            flow.LaunchMission();
            yield return WaitForState(flow, GameFlowState.Briefing);
            flow.SkipBriefing();
            yield return WaitForState(flow, GameFlowState.Mission);

            int playerIndex = flow.Runner.PlayerShipIndex;
            Assert.GreaterOrEqual(playerIndex, 0);

            // Move the Captain out of the firing line. The player must die from hostile
            // projectiles, not from a direct NotifyCraftDestroyed call in this test.
            int captainIndex = FindSpawn(flow, friendly: true, excludeShip: playerIndex);
            if (captainIndex >= 0)
            {
                flow.Runner.NotifyCraftDestroyed(captainIndex);
                ShipEntity captain = flow.Simulation.GetShip(captainIndex);
                captain.position += new float2(6000f, 0f);
                flow.Simulation.SetShip(captainIndex, captain);
            }

            int hostileIndex = FindSpawn(flow, friendly: false, excludeShip: -1);
            Assert.GreaterOrEqual(hostileIndex, 0, "Story 0 must spawn a hostile craft.");

            ShipEntity player = flow.Simulation.GetShip(playerIndex);
            ShipEntity hostile = flow.Simulation.GetShip(hostileIndex);
            hostile.position = player.position + new float2(0f, 300f);
            hostile.velocity = float2.zero;
            hostile.thrustPower = 0f;
            hostile.tacticalMode = (int)CaptainTacticalMode.SeekAndDestroy;
            flow.Simulation.SetShip(hostileIndex, hostile);

            var hud = Object.FindAnyObjectByType<CombatHudBehaviour>();
            Assert.IsNotNull(hud);
            float hullBefore = hud.State.HullCurrent;
            Assert.Greater(hullBefore, 0f);

            bool sawDamage = false;
            for (int i = 0; i < 3200 && flow.State == GameFlowState.Mission; i++)
            {
                flow.Tick(GameFlowController.FixedStepSeconds * 4f);
                if (hud.State.HullCurrent < hullBefore) sawDamage = true;
            }

            Assert.IsTrue(sawDamage, "Hostile projectiles must damage the player hull through the contact solver.");
            Assert.AreEqual(GameFlowState.Fail, flow.State, "Destroying the player must end the mission.");
            Assert.IsTrue(flow.LastResult.HasValue);
            Assert.AreEqual(MissionState.Lose, flow.LastResult.Value.state);
            Assert.AreEqual(MissionEndCause.PlayerDestroyed, flow.LastResult.Value.cause,
                "The loss must be caused by the player craft being destroyed, not a scripted objective.");
        }

        private static int FindSpawn(GameFlowController flow, bool friendly, int excludeShip)
        {
            var spawns = flow.Runner.Spawns;
            for (int i = 0; i < spawns.Count; i++)
            {
                MissionSpawnRecord spawn = spawns[i];
                if (spawn.shipIndex < 0 || spawn.shipIndex == excludeShip) continue;
                if (spawn.friendly == friendly) return spawn.shipIndex;
            }

            return -1;
        }

        private static IEnumerator WaitForState(GameFlowController flow, GameFlowState expected)
        {
            for (int i = 0; i < WaitFrames; i++)
            {
                if (flow.State == expected) yield break;

                // The frozen boot order is UIMain -> PLAY -> Center (#181).
                if (expected == GameFlowState.Center && flow.State == GameFlowState.MainMenu)
                {
                    MainMenuController menu = Object.FindAnyObjectByType<MainMenuController>();
                    ScreenPanel panel = menu != null ? menu.GetComponent<ScreenPanel>() : null;
                    Button play = panel?.Root?.Q<Button>("play-button");
                    if (play != null) PlayModeBoot.Click(play);
                }

                yield return null;
            }

            Assert.Fail($"Flow did not reach {expected}; current state {flow.State}.");
        }
    }
}
