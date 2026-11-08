using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ABRevival.Core.Data;

namespace ABRevival.Tests
{
    [TestFixture]
    public class TalkScriptParserTests
    {
        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, ".git")) && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                dir = dir.Parent;
            }
            return dir?.FullName ?? @"C:\Dev\A&B revival";
        }

        private static string FrozenTalkPath()
        {
            return Path.Combine(FindRepoRoot(), "src", "assets", "binaryData", "5_all.AllData_talk.bin");
        }

        private static string ResourcesTalkPath()
        {
            return Path.Combine(FindRepoRoot(), "A&B-U", "Assets", "Resources", "Talk", "5_all.AllData_talk.xml");
        }

        [Test]
        public void TalkScriptParser_ParsesAll388LinesAnd13Speakers_FromFrozenAS3Data()
        {
            string path = FrozenTalkPath();
            Assert.IsTrue(File.Exists(path), $"Talk data file missing: {path}");

            TalkScript script = TalkScriptParser.ParseFile(path);

            Assert.AreEqual(388, script.Count);
            Assert.AreEqual(13, script.SpeakerCount);
            Assert.AreEqual(0, script.FirstIndex);
            Assert.AreEqual(387, script.LastIndex);
        }

        [Test]
        public void TalkScriptParser_AssignsSpeakerIdsInFirstAppearanceOrder()
        {
            TalkScript script = TalkScriptParser.ParseFile(FrozenTalkPath());

            var seen = new HashSet<string>();
            for (int i = 0; i < script.Count; i++)
            {
                TalkLine line = script.Lines[i];
                Assert.AreEqual(script.Speakers[line.SpeakerId], line.Speaker, $"Speaker id mismatch at line {line.Index}.");
                if (seen.Add(line.Speaker))
                {
                    Assert.AreEqual(seen.Count - 1, line.SpeakerId, $"Speaker {line.Speaker} did not receive the next sequential id.");
                }
            }

            Assert.AreEqual(13, seen.Count);
        }

        [Test]
        public void TalkScriptParser_SpeakerOrder_MatchesFirstAppearanceInFrozenData()
        {
            TalkScript script = TalkScriptParser.ParseFile(FrozenTalkPath());

            CollectionAssert.AreEqual(
                new[]
                {
                    "Somebody", "Captain", "Wang", "Michael", "Tom", "ROBOT-S1",
                    "Officer XA", "Pilot XA", "Unknown Cf.", "Unknown Cp.",
                    "Cp.Monkey", "CommanderX", "Head Of XA"
                },
                script.Speakers);
        }

        [Test]
        public void TalkScriptParser_OrdersLinesBySourceIndex_Deterministically()
        {
            TalkScript first = TalkScriptParser.ParseFile(FrozenTalkPath());
            TalkScript second = TalkScriptParser.ParseFile(FrozenTalkPath());

            Assert.AreEqual(first.Count, second.Count);
            for (int i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first.Lines[i].Index, second.Lines[i].Index);
                Assert.AreEqual(first.Lines[i].SpeakerId, second.Lines[i].SpeakerId);
                Assert.AreEqual(first.Lines[i].Speaker, second.Lines[i].Speaker);
                Assert.AreEqual(first.Lines[i].Text, second.Lines[i].Text);
                if (i > 0)
                {
                    Assert.Less(first.Lines[i - 1].Index, first.Lines[i].Index, "Talk lines must be ordered by source index.");
                }
            }
        }

        [Test]
        public void TalkScriptParser_LoadedResourcesCopy_MatchesFrozenLineAndSpeakerCounts()
        {
            string frozenPath = FrozenTalkPath();
            string resourcesPath = ResourcesTalkPath();
            Assert.IsTrue(File.Exists(frozenPath), $"Frozen talk data missing: {frozenPath}");
            Assert.IsTrue(File.Exists(resourcesPath), $"Runtime talk data copy missing: {resourcesPath}");

            TalkScript frozen = TalkScriptParser.ParseFile(frozenPath);
            TalkScript runtime = TalkScriptParser.ParseFile(resourcesPath);

            Assert.AreEqual(frozen.Count, runtime.Count);
            Assert.AreEqual(frozen.SpeakerCount, runtime.SpeakerCount);
            Assert.AreEqual(388, runtime.Count);
            Assert.AreEqual(13, runtime.SpeakerCount);
        }

        [Test]
        public void TalkScript_Slice_KeepsGlobalSpeakerIds_ForStoryZeroTalkBeforeCue()
        {
            TalkScript script = TalkScriptParser.ParseFile(FrozenTalkPath());
            TalkScript cue = script.Slice(2, 10);

            Assert.AreEqual(9, cue.Count);
            Assert.AreEqual(2, cue.FirstIndex);
            Assert.AreEqual(10, cue.LastIndex);
            Assert.AreEqual(script.SpeakerCount, cue.SpeakerCount);
            for (int i = 0; i < cue.Count; i++)
            {
                Assert.AreEqual(script.IndexOfLine(cue.Lines[i].Index), i + script.IndexOfLine(2));
                Assert.AreEqual(script.Lines[script.IndexOfLine(cue.Lines[i].Index)].SpeakerId, cue.Lines[i].SpeakerId);
            }
        }

        [Test]
        public void TalkScriptParser_RejectsDuplicateLineIndices()
        {
            const string xml =
                "<talk>" +
                "<talkOne><index>0</index><who>Michael</who><word>One</word></talkOne>" +
                "<talkOne><index>0</index><who>Tom</who><word>Two</word></talkOne>" +
                "</talk>";

            Assert.Throws<InvalidDataException>(() => TalkScriptParser.ParseXml(xml));
        }
    }
}
