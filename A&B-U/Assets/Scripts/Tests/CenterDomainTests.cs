using ABRevival.Core.Data;
using ABRevival.Core.Persistence;
using NUnit.Framework;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 guard for the Center (UIChooseFlyAI) domain: frozen F4ShowData stat formulas,
    /// companion slot rules and the single-save persistence of the Player pick and AI slots.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class CenterDomainTests
    {
        [Test]
        public void CraftShowData_MatchesTheFrozenF4ShowDataCard()
        {
            var parts = new[]
            {
                new CraftPartStat(PartCategory.Body, 100f, 1300f, 5f, 1.7453293f, 1013f),
                new CraftPartStat(PartCategory.Wing, 20f, 1300f, 0f, 3.8397243f, 3084f),
                new CraftPartStat(PartCategory.Engine, 20f, 600f, 16f, 0f, 4138f),
                new CraftPartStat(PartCategory.Gun, 17f, 425f, 0f, 0f, 2055f, 1.02f)
            };

            CraftShowStats stats = CraftShowData.Evaluate(
                "Michael's", "B.Small.01", 100f, 1300f, parts);

            Assert.AreEqual("Michael's", stats.name);
            Assert.AreEqual(3625, stats.hp);
            Assert.AreEqual(13.4f, stats.speed, 0.05f);
            Assert.AreEqual(20.4f, stats.steering, 0.05f);
            Assert.AreEqual(1.02f, stats.fire, 0.001f);
            Assert.AreEqual(4, stats.parts);
            Assert.AreEqual(10290, stats.value);
            Assert.AreEqual("F", stats.cls, "The frozen class thresholds must drive the grade.");
        }

        [Test]
        public void CraftShowData_UsesTheHullHeaderWhenNoBodyPartIsMounted()
        {
            var parts = new[]
            {
                new CraftPartStat(PartCategory.Gun, 17f, 425f, 0f, 0f, 2055f, 1.02f)
            };

            CraftShowStats stats = CraftShowData.Evaluate("Spare", "B.Small.01", 100f, 1300f, parts);

            Assert.AreEqual(1725, stats.hp);
            Assert.AreEqual(2, stats.parts);
            Assert.AreEqual(117f, stats.mass, 0.001f);
        }

        [Test]
        public void ClassFor_MatchesEveryFrozenThreshold()
        {
            Assert.AreEqual("F", CraftShowData.ClassFor(0.49f));
            Assert.AreEqual("E", CraftShowData.ClassFor(0.50f));
            Assert.AreEqual("D", CraftShowData.ClassFor(1.00f));
            Assert.AreEqual("C", CraftShowData.ClassFor(1.50f));
            Assert.AreEqual("B", CraftShowData.ClassFor(2.30f));
            Assert.AreEqual("A", CraftShowData.ClassFor(4.00f));
            Assert.AreEqual("S", CraftShowData.ClassFor(5.80f));
        }

        [Test]
        public void CompanionRoster_AssignsFiveSlotsRejectsDuplicatesAndRemoves()
        {
            var roster = new CompanionRoster();

            Assert.AreEqual(5, CompanionRoster.SlotCount);
            Assert.AreEqual(0, roster.FirstFreeSlot());
            Assert.AreEqual(0, roster.AssignedCount);

            Assert.IsTrue(roster.AssignFirstFree(2));
            Assert.IsTrue(roster.Assign(3, 7));
            Assert.IsFalse(roster.Assign(4, 2), "A craft cannot occupy two slots.");
            Assert.AreEqual(2, roster.AssignedCount);
            Assert.AreEqual(3, roster.SlotOf(7));
            Assert.AreEqual(7, roster.CraftIndex(3));

            Assert.IsTrue(roster.RemoveAt(3));
            Assert.AreEqual(-1, roster.CraftIndex(3));
            Assert.IsFalse(roster.Remove(7));

            roster.Clear();
            Assert.AreEqual(0, roster.AssignedCount);
        }

        [Test]
        public void CompanionRoster_LoadSaveRoundTripsAndDropsInvalidSlots()
        {
            var roster = new CompanionRoster();
            roster.Assign(0, 1);
            roster.Assign(2, 4);

            var saved = roster.ToSave();
            Assert.AreEqual(CompanionRoster.SlotCount, saved.Count);

            var restored = new CompanionRoster();
            restored.Load(saved, playerCraftIndex: -1, fleetCount: 5);
            Assert.AreEqual(1, restored.CraftIndex(0));
            Assert.AreEqual(4, restored.CraftIndex(2));

            var clamped = new CompanionRoster();
            clamped.Load(saved, playerCraftIndex: 4, fleetCount: 5);
            Assert.AreEqual(1, clamped.AssignedCount, "Player-owned and out-of-range craft must be dropped.");
            Assert.AreEqual(1, clamped.CraftIndex(0));
        }

        [Test]
        public void PlayerSave_RoundTripsPlayerPickAndCompanions()
        {
            var payload = new PlayerSaveData { playerBlueprintIndex = 3 };
            payload.companions.Add(new CompanionSaveData { craftIndex = 1, role = 3, leader = 1 });
            payload.companions.Add(new CompanionSaveData { craftIndex = 4, role = 1, leader = 0 });

            PlayerSaveData loaded = PlayerSaveCodec.FromJson(PlayerSaveCodec.ToJson(payload));

            Assert.AreEqual(3, loaded.playerBlueprintIndex);
            Assert.AreEqual(2, loaded.companions.Count);
            Assert.AreEqual(1, loaded.companions[0].craftIndex);
            Assert.AreEqual(3, loaded.companions[0].role);
            Assert.AreEqual(1, loaded.companions[0].leader);
            Assert.AreEqual(4, loaded.companions[1].craftIndex);
        }

        [Test]
        public void PlayerSave_OldPayloadWithoutCenterFieldsLoadsWithDefaults()
        {
            PlayerSaveData loaded = PlayerSaveCodec.FromJson("{\"version\":1}");

            Assert.AreEqual(0, loaded.playerBlueprintIndex);
            Assert.IsNotNull(loaded.companions);
            Assert.AreEqual(0, loaded.companions.Count);
            Assert.IsNotNull(loaded.blueprints);
        }
    }
}
