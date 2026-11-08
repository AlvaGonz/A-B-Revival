using System;
using ABRevival.Core.Data;
using ABRevival.Core.Persistence;
using ABRevival.Core.Simulation;

namespace ABRevival.Core.Story
{
    /// <summary>Story-level dialogue cue resolved from the frozen TalkIndex table.</summary>
    public enum StoryTalkCue
    {
        Before = 0,
        After = 1
    }

    /// <summary>Inclusive source-line range of one story cue.</summary>
    public readonly struct StoryTalkRange
    {
        public readonly int FirstLine;
        public readonly int LastLine;

        public StoryTalkRange(int firstLine, int lastLine)
        {
            FirstLine = firstLine;
            LastLine = lastLine;
        }
    }

    /// <summary>
    /// Port of TalkIndex.talkBefore/talkAfter (src/as3/scripts/f4data/talk/TalkIndex.as).
    /// </summary>
    public static class StoryTalkCues
    {
        // shortcut: only the story-0 range is wired because only that mission is playable;
        // add the remaining TalkIndex.as ranges when later missions become playable.
        public static bool TryGet(int storyIndex, StoryTalkCue cue, out StoryTalkRange range)
        {
            if (storyIndex == 0)
            {
                range = cue == StoryTalkCue.Before
                    ? new StoryTalkRange(2, 10)
                    : new StoryTalkRange(11, 15);
                return true;
            }

            range = default;
            return false;
        }
    }

    /// <summary>
    /// Owns STORY progression on the single player save payload.
    /// STORY advances only when a mission ends in Win, matching UIWin.toNext in the frozen
    /// release; a loss leaves the campaign index untouched. Autosave fires at the two Flash
    /// points: mission completion before the advance (UIWin ctor -> autoSaveMission) and
    /// story advance after it (UIWin.nextGame).
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class StoryDirector
    {
        /// <summary>Highest numbered story; the frozen release's MAP1 boundary.</summary>
        public const int MaxStoryIndex = 34;

        private readonly PlayerSaveData _save;
        private readonly Action<PlayerSaveData> _autosave;

        public StoryDirector(PlayerSaveData save, Action<PlayerSaveData> autosave = null)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _autosave = autosave;
            _save.storyIndex = Clamp(_save.storyIndex);
        }

        public int StoryIndex => _save.storyIndex;

        /// <summary>
        /// Applies a terminal mission result. Returns true only when STORY advanced.
        /// Non-win results neither advance nor autosave, matching UIFail.
        /// </summary>
        public bool ApplyMissionResult(MissionResult result)
        {
            if (result.state != MissionState.Win) return false;

            Autosave();
            if (_save.storyIndex >= MaxStoryIndex) return false;

            _save.storyIndex++;
            Autosave();
            return true;
        }

        /// <summary>Slices the story's talkBefore cue from the imported script, or null when none is wired.</summary>
        public TalkScript ResolveTalkBefore(TalkScript script) => ResolveCue(script, StoryTalkCue.Before);

        /// <summary>Slices the story's talkAfter cue from the imported script, or null when none is wired.</summary>
        public TalkScript ResolveTalkAfter(TalkScript script) => ResolveCue(script, StoryTalkCue.After);

        private TalkScript ResolveCue(TalkScript script, StoryTalkCue cue)
        {
            if (script == null) throw new ArgumentNullException(nameof(script));
            if (!StoryTalkCues.TryGet(_save.storyIndex, cue, out StoryTalkRange range)) return null;
            return script.Slice(range.FirstLine, range.LastLine);
        }

        private void Autosave()
        {
            _autosave?.Invoke(_save);
        }

        private static int Clamp(int storyIndex) => Math.Clamp(storyIndex, 0, MaxStoryIndex);
    }
}
