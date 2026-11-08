using System.Collections;
using System.IO;
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
            if (AudioService.Instance != null) Object.Destroy(AudioService.Instance.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BootScene_LoadsSavedStoryAndShopState()
        {
            var seeded = new PlayerSaveData { storyIndex = 3 };
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

            yield return WaitForState(flow, GameFlowState.Hangar);

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
                if (flow.State == GameFlowState.Result) flow.AdvanceTalk();
                guard++;
                yield return null;
            }

            Assert.AreEqual(GameFlowState.Hangar, flow.State, "The flow must return to the hangar after the result.");
            Assert.IsNull(flow.Runner, "Mission state is disposed on return.");
            Assert.AreEqual(1, flow.StoryIndex, "STORY stays advanced in the hangar.");

            Assert.IsTrue(File.Exists(SavePath), "A win must autosave the player payload.");
            PlayerSaveData saved = PlayerSaveCodec.LoadFromFile(SavePath);
            Assert.AreEqual(1, saved.storyIndex, "The autosave must persist the advanced STORY index.");
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
