using ABRevival.Core.Simulation;
using ABRevival.Core.Story;
using NUnit.Framework;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 coverage for the frozen result copy and EvtCtrl.win reward formula.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class MissionSummaryTests
    {
        [Test]
        public void LevelMoney_StoryZeroPaysNothing()
        {
            Assert.AreEqual(0, MissionSummary.LevelMoney(0, 0));
            Assert.AreEqual(0, MissionSummary.LevelMoney(0, 3));
        }

        [Test]
        public void LevelMoney_UsesTheCleanOrDamagedFrozenBonus()
        {
            Assert.AreEqual(9 * 5454, MissionSummary.LevelMoney(9, 0));
            Assert.AreEqual(9 * 2121, MissionSummary.LevelMoney(9, 2));
        }

        [Test]
        public void FromResult_PaysOnlyOnWin()
        {
            var win = new MissionResult(MissionState.Win, MissionEndCause.WindirAct, "clock_2", 120);
            var lose = new MissionResult(MissionState.Lose, MissionEndCause.PlayerDestroyed, "Michael", 120);

            MissionSummary won = MissionSummary.FromResult(win, 4, 0);
            MissionSummary lost = MissionSummary.FromResult(lose, 4, 1);

            Assert.AreEqual(4 * 5454, won.reward);
            Assert.AreEqual(0, lost.reward);
            Assert.IsTrue(won.Won);
            Assert.IsFalse(lost.Won);
        }

        [Test]
        public void CauseLabel_KeepsTheRealCauseAndObjective()
        {
            var win = new MissionResult(MissionState.Win, MissionEndCause.WindirAct, "clock_2", 10);
            var lose = new MissionResult(MissionState.Lose, MissionEndCause.PlayerDestroyed, "Michael", 10);

            string won = MissionSummary.FromResult(win, 0, 0).CauseLabel;
            string lost = MissionSummary.FromResult(lose, 0, 0).CauseLabel;

            StringAssert.Contains("Objective complete", won);
            StringAssert.Contains("clock_2", won);
            StringAssert.Contains("Craft destroyed", lost);
            StringAssert.Contains("Michael", lost);
        }

        [Test]
        public void Copy_SwitchesOnWinLoseAndFinalBattle()
        {
            var win = new MissionResult(MissionState.Win, MissionEndCause.WindirAct, "clock_2", 10);
            var lose = new MissionResult(MissionState.Lose, MissionEndCause.LoseAct, "lose", 10);

            MissionSummary clean = MissionSummary.FromResult(win, 0, 0);
            MissionSummary damaged = MissionSummary.FromResult(win, 0, 2);
            MissionSummary lost = MissionSummary.FromResult(lose, 0, 0);
            MissionSummary final = MissionSummary.FromResult(win, StoryDirector.MaxStoryIndex, 0);

            Assert.AreEqual("You won!", clean.Title);
            Assert.AreEqual("Perfect", clean.VerdictLabel);
            StringAssert.Contains("You lost 2 parts", damaged.BodyLabel);
            Assert.AreEqual("Repair needed", damaged.VerdictLabel);
            StringAssert.Contains("Unrepaired parts", damaged.WarningLabel);
            Assert.AreEqual("Battle failed", lost.Title);
            StringAssert.Contains("why not try again", lost.BodyLabel);
            Assert.AreEqual("Story Mode Completed!", final.Title);
            Assert.IsTrue(final.IsFinalBattle);
        }

        [Test]
        public void WithDamagedParts_UpdatesTheResultCopy()
        {
            var win = new MissionResult(MissionState.Win, MissionEndCause.WindirAct, "clock_2", 10);
            MissionSummary summary = MissionSummary.FromResult(win, 0, 2).WithDamagedParts(0);

            Assert.AreEqual(0, summary.damagedParts);
            Assert.AreEqual("Perfect", summary.VerdictLabel);
        }
    }
}
