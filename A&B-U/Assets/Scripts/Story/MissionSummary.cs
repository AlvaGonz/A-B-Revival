using System;
using System.Globalization;
using ABRevival.Core.Simulation;

namespace ABRevival.Core.Story
{
    /// <summary>
    /// Presentation summary of one finished mission: the frozen result cause, the campaign
    /// reward from EvtCtrl.win and the damaged-part count. UIWin/UIFail/UIAllWin render it.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public readonly struct MissionSummary
    {
        /// <summary>Frozen clear bonus per story index (EvtCtrl.win, no damaged parts).</summary>
        public const int CleanBonusPerStory = 5454;

        /// <summary>Frozen damaged-craft bonus per story index (EvtCtrl.win).</summary>
        public const int DamagedBonusPerStory = 2121;

        public readonly MissionState state;
        public readonly MissionEndCause cause;
        public readonly string objective;
        public readonly int storyIndex;
        public readonly int reward;
        public readonly int damagedParts;

        public MissionSummary(
            MissionState state,
            MissionEndCause cause,
            string objective,
            int storyIndex,
            int reward,
            int damagedParts)
        {
            this.state = state;
            this.cause = cause;
            this.objective = objective ?? string.Empty;
            this.storyIndex = Math.Max(0, storyIndex);
            this.reward = Math.Max(0, reward);
            this.damagedParts = Math.Max(0, damagedParts);
        }

        public bool Won => state == MissionState.Win;

        /// <summary>UIAllWin takes over when the last campaign battle is won.</summary>
        public bool IsFinalBattle => Won && storyIndex >= StoryDirector.MaxStoryIndex;

        public string BattleLabel => "BATTLE-" + (storyIndex + 1).ToString(CultureInfo.InvariantCulture);

        public string Title => IsFinalBattle ? "Story Mode Completed!" : Won ? "You won!" : "Battle failed";

        /// <summary>
        /// EvtCtrl.win: STORY * 5454 on a clean win, STORY * 2121 with damaged parts.
        /// shortcut: the mission item layer does not spawn money pickups yet, so only the
        /// story bonus is paid; add the Item "money" payload to the result when items land.
        /// </summary>
        public static int LevelMoney(int storyIndex, int damagedPartCount)
        {
            int story = Math.Clamp(storyIndex, 0, StoryDirector.MaxStoryIndex);
            return story * (damagedPartCount > 0 ? DamagedBonusPerStory : CleanBonusPerStory);
        }

        public static MissionSummary FromResult(in MissionResult result, int storyIndex, int damagedParts)
        {
            return new MissionSummary(
                result.state,
                result.cause,
                result.objective,
                storyIndex,
                result.state == MissionState.Win ? LevelMoney(storyIndex, damagedParts) : 0,
                damagedParts);
        }

        public MissionSummary WithDamagedParts(int count)
        {
            return new MissionSummary(state, cause, objective, storyIndex, reward, count);
        }

        public string CauseLabel
        {
            get
            {
                string text = cause switch
                {
                    MissionEndCause.WindirAct => "Objective complete",
                    MissionEndCause.LoseAct => "Objective failed",
                    MissionEndCause.DelLoseAct => "Objective failed",
                    MissionEndCause.PlayerDestroyed => "Craft destroyed",
                    _ => "Unknown outcome"
                };

                return string.IsNullOrEmpty(objective) ? text : text + " (" + objective + ")";
            }
        }

        public string BodyLabel
        {
            get
            {
                if (IsFinalBattle) return "Enjoy the ending music.\nDon't leave, it's only the beginning!";
                if (!Won) return "Hum, seems everything has gone.\nYes? No? ... Has it? why not try again?";
                if (damagedParts == 0) return "You lost no parts.";
                return damagedParts == 1
                    ? "You lost one part."
                    : "You lost " + damagedParts.ToString(CultureInfo.InvariantCulture) + " parts.";
            }
        }

        public string RewardLabel => "Money: +" + reward.ToString(CultureInfo.InvariantCulture) + "G";

        public string PartsLabel => "Parts: " + damagedParts.ToString(CultureInfo.InvariantCulture);

        public string VerdictLabel
        {
            get
            {
                if (!Won) return cause == MissionEndCause.PlayerDestroyed ? "Craft destroyed" : "Battle failed";
                return damagedParts == 0 ? "Perfect" : "Repair needed";
            }
        }

        public string WarningLabel => Won && damagedParts > 0
            ? "Unrepaired parts leave the craft and return to the shop."
            : string.Empty;
    }
}
