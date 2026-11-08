using System.Collections;
using System.IO;
using ABRevival.Core.Data;
using ABRevival.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace ABRevival.Tests.PlayMode
{
    [TestFixture]
    public class TalkPlaybackTests
    {
        private sealed class CountingTalkAudio : ITalkAudio
        {
            public int Count;

            public void PlayAdvanceClick()
            {
                Count++;
            }
        }

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(Application.dataPath);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, ".git")) && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                dir = dir.Parent;
            }
            return dir != null ? dir.FullName : Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        }

        private static TalkScript LoadFrozenScript()
        {
            string path = Path.Combine(FindRepoRoot(), "src", "assets", "binaryData", "5_all.AllData_talk.bin");
            Assert.IsTrue(File.Exists(path), $"Frozen talk data missing: {path}");
            return TalkScriptParser.ParseFile(path);
        }

        private static TalkPortraitCatalog LoadPortraitCatalog()
        {
            var catalog = Resources.Load<TalkPortraitCatalog>("Talk/TalkPortraitCatalog");
            Assert.IsNotNull(catalog, "Talk portrait catalog missing from Resources/Talk.");
            return catalog;
        }

        [UnityTest]
        public IEnumerator StoryZeroTalkBefore_PlaysToCompletion_WithTextPortraitAndClickPerLine()
        {
            TalkScript script = LoadFrozenScript();
            TalkScript cue = script.Slice(2, 10);
            TalkPortraitCatalog catalog = LoadPortraitCatalog();

            Assert.AreEqual(9, cue.Count, "Story-0 talkBefore cue is expected to span lines 2..10.");

            var root = new VisualElement();
            var view = new TalkView(root, catalog);
            var audio = new CountingTalkAudio();
            bool completed = false;
            var presenter = new TalkPresenter(cue, view, audio, () => completed = true);

            presenter.Advance();
            AssertDisplayedLine(view, cue.Lines[0], catalog);
            Assert.AreEqual(1, audio.Count);

            for (int i = 1; i < cue.Count; i++)
            {
                presenter.Advance();
                AssertDisplayedLine(view, cue.Lines[i], catalog);
                Assert.AreEqual(i + 1, audio.Count);
                yield return null;
            }

            presenter.Advance();

            Assert.IsTrue(presenter.IsComplete);
            Assert.IsTrue(completed);
            Assert.AreEqual(cue.Count, audio.Count, "One click sound per displayed line, none on completion.");
            Assert.AreEqual(string.Empty, view.Text.text);
            Assert.AreEqual(string.Empty, view.SpeakerName.text);
            Assert.IsNull(view.Portrait.sprite);
        }

        [Test]
        public void PortraitCatalog_MapsAll13SpeakerIdsToExtractedPortraits()
        {
            TalkScript script = LoadFrozenScript();
            TalkPortraitCatalog catalog = LoadPortraitCatalog();

            Assert.AreEqual(13, script.SpeakerCount);
            Assert.AreEqual(script.SpeakerCount, catalog.Count);
            for (int speakerId = 0; speakerId < script.SpeakerCount; speakerId++)
            {
                Assert.IsNotNull(catalog.GetPortrait(speakerId), $"Missing portrait for speaker id {speakerId} ({script.Speakers[speakerId]}).");
            }
        }

        private static void AssertDisplayedLine(TalkView view, TalkLine line, TalkPortraitCatalog catalog)
        {
            Assert.AreEqual(line.Text, view.Text.text);
            Assert.AreEqual(line.Speaker, view.SpeakerName.text);
            Assert.AreSame(catalog.GetPortrait(line.SpeakerId), view.Portrait.sprite);
        }
    }
}
