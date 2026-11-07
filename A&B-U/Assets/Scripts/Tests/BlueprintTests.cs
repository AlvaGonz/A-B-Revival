using NUnit.Framework;
using Unity.Mathematics;
using ABRevival.Core.Data;

namespace ABRevival.Tests
{
    [TestFixture]
    public class BlueprintTests
    {
        [Test]
        public void PartCategory_Values_MatchLegacyAS3Constants()
        {
            Assert.AreEqual(0, (int)PartCategory.Body);
            Assert.AreEqual(1, (int)PartCategory.Wing);
            Assert.AreEqual(2, (int)PartCategory.Engine);
            Assert.AreEqual(3, (int)PartCategory.Gun);
        }

        [Test]
        public void HullCategory_Deduction_MatchesADR0007Rules()
        {
            Assert.AreEqual(HullCategory.Small, CraftBlueprintSO.GetHullCategoryForName("B.Sml.01"));
            Assert.AreEqual(HullCategory.Medium, CraftBlueprintSO.GetHullCategoryForName("B.Med.02"));
            Assert.AreEqual(HullCategory.Medium, CraftBlueprintSO.GetHullCategoryForName("B.Med++Heavy"));
            Assert.AreEqual(HullCategory.Large, CraftBlueprintSO.GetHullCategoryForName("B.Lrg.03"));
            Assert.AreEqual(HullCategory.Mega, CraftBlueprintSO.GetHullCategoryForName("B.Mega.Carrier"));
            Assert.AreEqual(HullCategory.Medium, CraftBlueprintSO.GetHullCategoryForName("CustomUnknown"));
        }

        [Test]
        public void WingCapacity_Limits_StrictlyEnforced()
        {
            Assert.AreEqual(4, CraftBlueprintSO.GetMaxWingsForCategory(HullCategory.Small));
            Assert.AreEqual(6, CraftBlueprintSO.GetMaxWingsForCategory(HullCategory.Medium));
            Assert.AreEqual(8, CraftBlueprintSO.GetMaxWingsForCategory(HullCategory.Large));
            Assert.AreEqual(12, CraftBlueprintSO.GetMaxWingsForCategory(HullCategory.Mega));
        }

        [Test]
        public void ValidateWingCapacity_RejectsExcessWings_UnlessGrandfathered()
        {
            var smallHull = HullCategory.Small; // max 4
            var fiveWings = new MountedPartData[5];
            for (int i = 0; i < 5; i++)
            {
                fiveWings[i] = new MountedPartData { category = PartCategory.Wing };
            }

            // Non-grandfathered craft with 5 wings on small hull must fail
            Assert.IsFalse(CraftBlueprintSO.ValidateWingCapacity(smallHull, fiveWings, isGrandfathered: false, out string error));
            StringAssert.Contains("exceeds capacity", error);

            // Grandfathered craft with 5 wings on small hull must pass
            Assert.IsTrue(CraftBlueprintSO.ValidateWingCapacity(smallHull, fiveWings, isGrandfathered: true, out _));

            // Small hull with 4 wings must pass regardless of grandfathering
            var fourWings = new MountedPartData[4];
            for (int i = 0; i < 4; i++)
            {
                fourWings[i] = new MountedPartData { category = PartCategory.Wing };
            }
            Assert.IsTrue(CraftBlueprintSO.ValidateWingCapacity(smallHull, fourWings, isGrandfathered: false, out _));
        }

        [Test]
        public void CalculateAggregates_SumsMassHealthAndThrustAccurately()
        {
            float hullMass = 100f;
            float hullHealth = 500f;

            var parts = new MountedPartData[]
            {
                new MountedPartData { category = PartCategory.Engine, mass = 25f, health = 50f, thrust = 150f, energyDraw = 10f },
                new MountedPartData { category = PartCategory.Wing, mass = 15f, health = 30f, thrust = 0f, energyDraw = 0f },
                new MountedPartData { category = PartCategory.Gun, mass = 20f, health = 40f, thrust = 0f, energyDraw = 25f }
            };

            var stats = CraftBlueprintSO.CalculateAggregateStats(hullMass, hullHealth, parts);
            Assert.AreEqual(160f, stats.totalMass, 0.001f);
            Assert.AreEqual(620f, stats.totalHealth, 0.001f);
            Assert.AreEqual(150f, stats.totalThrust, 0.001f);
            Assert.AreEqual(35f, stats.totalEnergyDraw, 0.001f);
        }
    }
}
