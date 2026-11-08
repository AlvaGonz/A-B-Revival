using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ABRevival.Core.Data;
using ABRevival.Core.Persistence;
using ABRevival.Core.Simulation;
using ABRevival.Core.Story;

namespace ABRevival.Tests
{
    [TestFixture]
    public class StoryDirectorTests
    {
        private static MissionResult Result(MissionState state)
        {
            return new MissionResult(state, MissionEndCause.WindirAct, "clock_2", 10);
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                dir = dir.Parent;
            }
            return dir?.FullName ?? @"C:\Dev\A&B revival";
        }

        private static TalkScript LoadFrozenTalk()
        {
            string path = Path.Combine(FindRepoRoot(), "src", "assets", "binaryData", "5_all.AllData_talk.bin");
            Assert.IsTrue(File.Exists(path), $"Frozen talk data missing: {path}");
            return TalkScriptParser.ParseFile(path);
        }

        [Test]
        public void ApplyMissionResult_Win_AdvancesAndAutosavesAtCompletionAndAdvance()
        {
            var save = new PlayerSaveData();
            var autosaveIndices = new List<int>();
            var director = new StoryDirector(save, payload => autosaveIndices.Add(payload.storyIndex));

            bool advanced = director.ApplyMissionResult(Result(MissionState.Win));

            Assert.IsTrue(advanced);
            Assert.AreEqual(1, director.StoryIndex);
            CollectionAssert.AreEqual(new[] { 0, 1 }, autosaveIndices,
                "Autosave must fire on mission completion, then again after STORY advances (Flash UIWin).");
        }

        [TestCase(MissionState.Lose)]
        [TestCase(MissionState.InProgress)]
        public void ApplyMissionResult_NonWin_DoesNotAdvanceOrAutosave(MissionState state)
        {
            var save = new PlayerSaveData { storyIndex = 3 };
            int autosaves = 0;
            var director = new StoryDirector(save, _ => autosaves++);

            bool advanced = director.ApplyMissionResult(Result(state));

            Assert.IsFalse(advanced);
            Assert.AreEqual(3, director.StoryIndex);
            Assert.AreEqual(0, autosaves, "A non-win mission must not touch the save.");
        }

        [Test]
        public void ApplyMissionResult_WinAtMaxStory_StopsAtMaxAndStillAutosavesCompletion()
        {
            var save = new PlayerSaveData { storyIndex = StoryDirector.MaxStoryIndex };
            var autosaveIndices = new List<int>();
            var director = new StoryDirector(save, payload => autosaveIndices.Add(payload.storyIndex));

            bool advanced = director.ApplyMissionResult(Result(MissionState.Win));

            Assert.IsFalse(advanced);
            Assert.AreEqual(StoryDirector.MaxStoryIndex, director.StoryIndex);
            CollectionAssert.AreEqual(new[] { StoryDirector.MaxStoryIndex }, autosaveIndices);
        }

        [Test]
        public void StoryDirector_ClampsTamperedStoryIndex()
        {
            Assert.AreEqual(0, new StoryDirector(new PlayerSaveData { storyIndex = -7 }).StoryIndex);
            Assert.AreEqual(
                StoryDirector.MaxStoryIndex,
                new StoryDirector(new PlayerSaveData { storyIndex = 500 }).StoryIndex);
        }

        [Test]
        public void Reload_RoundTripsStoryIndexThroughPlayerSaveCodec()
        {
            string path = Path.Combine(Path.GetTempPath(), "ab-revival-story-save-test.json");
            try
            {
                var save = new PlayerSaveData();
                var director = new StoryDirector(save, payload => PlayerSaveCodec.SaveToFile(path, payload));
                Assert.IsTrue(director.ApplyMissionResult(Result(MissionState.Win)));

                PlayerSaveData loaded = PlayerSaveCodec.LoadFromFile(path);
                var reloaded = new StoryDirector(loaded);

                Assert.AreEqual(director.StoryIndex, reloaded.StoryIndex);
                Assert.AreEqual(1, reloaded.StoryIndex);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void StoryCues_ResolveStoryZeroTalkBeforeAndAfterFromFrozenScript()
        {
            TalkScript script = LoadFrozenTalk();
            var director = new StoryDirector(new PlayerSaveData());

            TalkScript before = director.ResolveTalkBefore(script);
            TalkScript after = director.ResolveTalkAfter(script);

            Assert.IsNotNull(before);
            Assert.AreEqual(2, before.FirstIndex);
            Assert.AreEqual(10, before.LastIndex);
            Assert.AreEqual(9, before.Count);

            Assert.IsNotNull(after);
            Assert.AreEqual(11, after.FirstIndex);
            Assert.AreEqual(15, after.LastIndex);
            Assert.AreEqual(5, after.Count);

            Assert.AreEqual(script.Lines[script.IndexOfLine(2)].Text, before.Lines[0].Text);
            Assert.AreEqual(script.Lines[script.IndexOfLine(15)].Text, after.Lines[after.Count - 1].Text);
        }

        [Test]
        public void StoryCues_AreResolvedByStoryIndex_NotReusedAfterAdvance()
        {
            TalkScript script = LoadFrozenTalk();
            var save = new PlayerSaveData();
            var director = new StoryDirector(save);

            Assert.IsTrue(StoryTalkCues.TryGet(director.StoryIndex, StoryTalkCue.Before, out StoryTalkRange beforeZero));
            Assert.AreEqual(2, beforeZero.FirstLine);
            Assert.AreEqual(10, beforeZero.LastLine);

            Assert.IsTrue(director.ApplyMissionResult(Result(MissionState.Win)));

            // shortcut: story 1 has no imported cue range yet, so advancing must not replay story-0 text.
            Assert.IsFalse(StoryTalkCues.TryGet(director.StoryIndex, StoryTalkCue.Before, out _));
            Assert.IsNull(director.ResolveTalkBefore(script));
            Assert.IsNull(director.ResolveTalkAfter(script));
        }
    }
}
