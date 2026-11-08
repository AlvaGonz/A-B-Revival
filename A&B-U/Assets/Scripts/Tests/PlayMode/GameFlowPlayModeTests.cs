using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using ABRevival.Core.Persistence;
using ABRevival.Core.Simulation;
using ABRevival.Presentation;
using ABRevival.Presentation.Audio;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// Story-0 loop smoke: boot scene loads the save, launches the reference mission through
    /// dialogue and cinematics, ticks combat at 60 Hz, forces the mission's own win objective,
    /// advances STORY, autosaves and returns to the hangar.
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

            SceneManager.LoadScene("Boot");
            yield return null;

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow);
            yield return WaitForState(flow, GameFlowState.Hangar);

            Assert.AreEqual(3, flow.StoryIndex, "Boot must restore the stored STORY index.");
            var hangar = Object.FindAnyObjectByType<HangarController>();
            Assert.IsNotNull(hangar, "Boot scene must host the hangar.");
            Assert.AreEqual(777, hangar.Money, "Boot must restore the stored shop money.");
            Assert.IsTrue(hangar.IsUnlocked(71),
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

            SceneManager.LoadScene("Boot");
            yield return null;

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow);
            yield return WaitForState(flow, GameFlowState.Hangar);

            Assert.IsTrue(File.Exists(SavePath + ".corrupt"),
                "An unreadable save must be moved aside, not silently overwritten.");
            Assert.IsFalse(File.Exists(SavePath), "A fresh campaign must not have written over the quarantined save.");
            Assert.AreEqual(0, flow.StoryIndex);
        }

        [UnityTest]
        public IEnumerator BootScene_Lose_ReturnsWithoutAdvancingStory()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);

            SceneManager.LoadScene("Boot");
            yield return null;

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow);
            yield return WaitForState(flow, GameFlowState.Hangar);

            flow.LaunchMission();
            yield return WaitForState(flow, GameFlowState.Briefing);
            flow.SkipBriefing();
            yield return WaitForState(flow, GameFlowState.Mission);

            int playerIndex = flow.Runner.PlayerShipIndex;
            Assert.GreaterOrEqual(playerIndex, 0);
            flow.Runner.NotifyCraftDestroyed(playerIndex);

            bool sawResult = false;
            for (int i = 0; i < WaitFrames && flow.State != GameFlowState.Hangar; i++)
            {
                if (flow.State == GameFlowState.Result)
                {
                    sawResult = true;
                    flow.SkipResult();
                }

                yield return null;
            }

            Assert.IsTrue(sawResult, "A loss must be observable in the Result state before returning.");
            Assert.AreEqual(GameFlowState.Hangar, flow.State);
            Assert.IsTrue(flow.LastResult.HasValue);
            Assert.AreEqual(MissionState.Lose, flow.LastResult.Value.state);
            Assert.AreEqual(0, flow.StoryIndex, "A loss must not advance STORY.");
            Assert.IsFalse(File.Exists(SavePath), "A loss must not autosave.");
        }

        [UnityTest]
        public IEnumerator BootScene_LaunchForcedWin_AdvancesStoryAutosavesAndReturns()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);

            SceneManager.LoadScene("Boot");
            yield return null;

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow, "Boot scene must contain a GameFlowController.");

            yield return WaitForState(flow, GameFlowState.Hangar);
            Assert.AreEqual(0, flow.StoryIndex, "A new campaign starts at STORY 0.");
            Assert.IsNull(flow.Runner, "No mission runs in the hangar.");

            flow.LaunchMission();
            yield return WaitForState(flow, GameFlowState.Briefing);
            flow.SkipBriefing();
            yield return WaitForState(flow, GameFlowState.Mission);

            Assert.IsNotNull(flow.Runner, "Launching must create the mission runner.");
            Assert.IsNotNull(flow.Simulation, "The mission must run on the scheduled simulation.");
            Assert.IsNotNull(flow.ContactSolver, "The projectile contact layer must be wired.");
            Assert.AreEqual(MissionState.InProgress, flow.Runner.State);

            // The shipped scene has no authored catalog, so the fallback blueprint must be
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
            Assert.AreEqual(1, flow.StoryIndex, "A win must advance STORY from 0 to 1.");

            Assert.IsFalse(flow.ForceWin(), "A consumed mission result must not be reusable.");

            int guard = 0;
            while (flow.State != GameFlowState.Hangar && guard < WaitFrames)
            {
                if (flow.State == GameFlowState.Result)
                {
                    flow.AdvanceTalk();
                    flow.SkipResult();
                }

                guard++;
                yield return null;
            }

            Assert.AreEqual(GameFlowState.Hangar, flow.State, "The flow must return to the hangar after the result.");
            Assert.IsNull(flow.Runner, "Mission state is disposed on return.");
            Assert.AreEqual(1, flow.StoryIndex, "STORY stays advanced in the hangar.");

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

            SceneManager.LoadScene("Boot");
            yield return null;

            GameFlowController flow = Object.FindAnyObjectByType<GameFlowController>();
            Assert.IsNotNull(flow);
            yield return WaitForState(flow, GameFlowState.Hangar);

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

        private static IEnumerator WaitForState(GameFlowController flow, GameFlowState expected)
        {
            for (int i = 0; i < WaitFrames; i++)
            {
                if (flow.State == expected) yield break;
                yield return null;
            }

            Assert.Fail($"Flow did not reach {expected}; current state {flow.State}.");
        }
    }
}
