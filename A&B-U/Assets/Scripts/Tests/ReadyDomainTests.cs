using System.Collections.Generic;
using ABRevival.Core.Data;
using ABRevival.Core.Persistence;
using NUnit.Framework;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 coverage for the Ready/Briefing data: the frozen missionWords table and the
    /// Center-to-mission loadout snapshot. Original IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class ReadyDomainTests
    {
        [Test]
        public void MissionBriefing_UsesTheFrozenMissionWords()
        {
            Assert.AreEqual("Wang is fighting. \n\nMichael is waiting.", MissionBriefing.Words(0));
            Assert.AreEqual("Protect the ship for 1 minute.", MissionBriefing.Words(2));
            Assert.AreEqual("Protect the ship, as you can.", MissionBriefing.Words(34));
            Assert.AreEqual("BATTLES Never end...", MissionBriefing.Words(99));
            Assert.AreEqual(string.Empty, MissionBriefing.Words(35));
            Assert.AreEqual(string.Empty, MissionBriefing.Words(-1));
        }

        [Test]
        public void MissionLoadout_KeepsSelectedCraftAndValidCompanions()
        {
            var save = new PlayerSaveData { playerBlueprintIndex = 2 };
            save.companions.Add(new CompanionSaveData { craftIndex = 1 });
            save.companions.Add(CompanionSaveData.Empty);
            save.companions.Add(new CompanionSaveData { craftIndex = 2 });
            save.companions.Add(new CompanionSaveData { craftIndex = 99 });
            save.companions.Add(new CompanionSaveData { craftIndex = 1 });

            MissionLoadout loadout = MissionLoadout.Capture(save, 5);

            Assert.AreEqual(2, loadout.PlayerCraftIndex);
            CollectionAssert.AreEqual(new[] { 1, 1 }, loadout.CompanionCraftIndices,
                "Companions keep slot order and duplicates; empty, Player and out-of-range slots are dropped.");
            Assert.AreEqual(2, loadout.CompanionCount);
        }

        [Test]
        public void MissionLoadout_SkipsAndCountsInvalidCompanions()
        {
            var save = new PlayerSaveData { playerBlueprintIndex = 0 };
            save.companions.Add(new CompanionSaveData { craftIndex = 1 });
            save.companions.Add(new CompanionSaveData { craftIndex = 2 });
            save.companions.Add(new CompanionSaveData { craftIndex = 3 });

            // Craft 2 has no hull; 3 is out of range and must not count as invalid.
            MissionLoadout loadout = MissionLoadout.Capture(
                save, 3, craft => craft != 2);

            CollectionAssert.AreEqual(new[] { 1 }, loadout.CompanionCraftIndices);
            Assert.AreEqual(1, loadout.InvalidCompanionCount,
                "The invalid companion must be excluded from the loadout and flagged.");
        }

        [Test]
        public void MissionLoadout_ClampsThePlayerPickAndHandlesAnEmptyFleet()
        {
            var save = new PlayerSaveData { playerBlueprintIndex = 9 };
            save.blueprints.Add(new BlueprintSaveData());

            MissionLoadout loadout = MissionLoadout.Capture(save, 1);
            Assert.AreEqual(0, loadout.PlayerCraftIndex);
            Assert.AreEqual(0, loadout.CompanionCount);

            MissionLoadout empty = MissionLoadout.Capture(new PlayerSaveData(), 0);
            Assert.AreEqual(-1, empty.PlayerCraftIndex);
            Assert.IsEmpty(empty.CompanionCraftIndices);
        }
    }
}
