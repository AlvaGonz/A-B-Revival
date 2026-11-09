using System.Collections.Generic;
using ABRevival.Core.Camera;
using ABRevival.Core.Data;
using ABRevival.Core.Persistence;
using NUnit.Framework;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 guard for the read-only Info/Records projection: stable owned-part tallies,
    /// companion occupancy and frozen labels. Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class InfoRecordsTests
    {
        [Test]
        public void StoryLabel_UsesTheFrozenBattleNumbering()
        {
            Assert.AreEqual("BATTLE-1", InfoRecords.StoryLabel(0));
            Assert.AreEqual("BATTLE-10", InfoRecords.StoryLabel(9));
            Assert.AreEqual("BATTLE-100", InfoRecords.StoryLabel(99));
        }

        [Test]
        public void TallyOwnedParts_GroupsCopiesAndSortsByPartId()
        {
            var owned = new List<int> { 173, 173, 133, 71, 173 };

            List<OwnedPartQuantity> tallies = InfoRecords.TallyOwnedParts(owned);

            Assert.AreEqual(3, tallies.Count);
            Assert.AreEqual(71, tallies[0].partId);
            Assert.AreEqual(1, tallies[0].quantity);
            Assert.AreEqual(133, tallies[1].partId);
            Assert.AreEqual(1, tallies[1].quantity);
            Assert.AreEqual(173, tallies[2].partId);
            Assert.AreEqual(3, tallies[2].quantity);
            Assert.AreEqual(5, InfoRecords.CountOwnedParts(owned));

            CollectionAssert.AreEqual(new[] { 173, 173, 133, 71, 173 }, owned,
                "Read-only tallying must not reorder the saved owned-parts list.");
        }

        [Test]
        public void TallyOwnedParts_HandlesEmptyAndMissingLists()
        {
            Assert.AreEqual(0, InfoRecords.CountOwnedParts(null));
            Assert.AreEqual(0, InfoRecords.TallyOwnedParts(null).Count);
            Assert.AreEqual(0, InfoRecords.TallyOwnedParts(new List<int>()).Count);
        }

        [Test]
        public void CountAssignedCompanions_IgnoresEmptySlots()
        {
            var companions = new List<CompanionSaveData>
            {
                new CompanionSaveData { craftIndex = -1 },
                new CompanionSaveData { craftIndex = 2 },
                new CompanionSaveData { craftIndex = 4 }
            };

            Assert.AreEqual(2, InfoRecords.CountAssignedCompanions(companions));
            Assert.AreEqual(0, InfoRecords.CountAssignedCompanions(null));
            Assert.AreEqual(InfoRecords.CompanionSlots, 5, "Center stores five companion slots.");
        }

        [Test]
        public void CameraLabel_MatchesTheAdr0005Presets()
        {
            Assert.AreEqual("Short (1.0x)", InfoRecords.CameraLabel(CameraDistancePreset.Short));
            Assert.AreEqual("Medium (1.3x)", InfoRecords.CameraLabel(CameraDistancePreset.Medium));
            Assert.AreEqual("Far (1.5x)", InfoRecords.CameraLabel(CameraDistancePreset.Far));
        }
    }
}
