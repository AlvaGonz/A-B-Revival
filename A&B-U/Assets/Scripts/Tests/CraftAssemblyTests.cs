using NUnit.Framework;
using Unity.Mathematics;
using ABRevival.Core.Data;

namespace ABRevival.Tests
{
    [TestFixture]
    public class CraftAssemblyTests
    {
        [Test]
        public void CraftAssembly_TryMountPart_AcceptsBody_UpdatesStats()
        {
            var assembly = new CraftAssembly("B.Med.01", hullMass: 100f, hullHealth: 500f);
            var bodyPart = new MountedPartData
            {
                partId = 1,
                category = PartCategory.Body,
                health = 100f,
                mass = 20f,
                thrust = 0f,
                energyDraw = 10f
            };

            var error = assembly.TryMountPart(bodyPart, out int slotIndex);
            Assert.AreEqual(AssemblyError.None, error);
            Assert.AreEqual(0, slotIndex);
            Assert.AreEqual(1, assembly.PartCount);

            var stats = assembly.CurrentStats;
            Assert.AreEqual(120f, stats.totalMass, 0.001f);
            Assert.AreEqual(600f, stats.totalHealth, 0.001f);
            Assert.AreEqual(0f, stats.totalThrust, 0.001f);
            Assert.AreEqual(10f, stats.totalEnergyDraw, 0.001f);
        }

        [Test]
        public void CraftAssembly_TryMountPart_RejectsWhenFull()
        {
            var assembly = new CraftAssembly("B.Mega.Carrier", hullMass: 200f, hullHealth: 1000f, isGrandfathered: true);
            var dummyPart = new MountedPartData { category = PartCategory.Engine, health = 10f, mass = 1f };

            for (int i = 0; i < CraftAssembly.MaxCapacity; i++)
            {
                var err = assembly.TryMountPart(dummyPart, out int slot);
                Assert.AreEqual(AssemblyError.None, err);
                Assert.AreEqual(i, slot);
            }

            Assert.AreEqual(CraftAssembly.MaxCapacity, assembly.PartCount);

            // 33rd part must be rejected with AssemblyFull
            var overflowErr = assembly.TryMountPart(dummyPart, out int overflowSlot);
            Assert.AreEqual(AssemblyError.AssemblyFull, overflowErr);
            Assert.AreEqual(-1, overflowSlot);
        }

        [Test]
        public void CraftAssembly_TryMountPart_RejectsASecondBody()
        {
            // ADR 0007: the Body part is the chassis; a craft cannot have two hulls.
            var assembly = new CraftAssembly("B.Med.01");
            var body = new MountedPartData { partId = 0, category = PartCategory.Body, mass = 20f };

            Assert.AreEqual(AssemblyError.None, assembly.TryMountPart(body, out _));
            Assert.AreEqual(AssemblyError.MultipleHulls, assembly.TryMountPart(body, out _));
            Assert.AreEqual(1, assembly.BodyCount);
        }

        [Test]
        public void CraftAssembly_TryMountPart_RejectsExcessWings_SmallHull()
        {
            // Small hull limit = 4 wings (ADR 0007)
            var assembly = new CraftAssembly("B.Sml.Fighter", isGrandfathered: false);
            Assert.AreEqual(4, assembly.MaxWingCapacity);
            var wing = new MountedPartData { category = PartCategory.Wing, mass = 5f, health = 20f };

            for (int i = 0; i < 4; i++)
            {
                var err = assembly.TryMountPart(wing, out _);
                Assert.AreEqual(AssemblyError.None, err);
            }

            // 5th wing must fail
            var excessErr = assembly.TryMountPart(wing, out _);
            Assert.AreEqual(AssemblyError.ExceedsWingCapacity, excessErr);
            Assert.AreEqual(4, assembly.PartCount);
        }

        [Test]
        public void CraftAssembly_TryMountPart_AllowsExcessWings_WhenGrandfathered()
        {
            // Small hull limit = 4 wings, but grandfathered craft allows exceeding it
            var assembly = new CraftAssembly("B.Sml.Fighter", isGrandfathered: true);
            var wing = new MountedPartData { category = PartCategory.Wing, mass = 5f, health = 20f };

            for (int i = 0; i < 6; i++)
            {
                var err = assembly.TryMountPart(wing, out _);
                Assert.AreEqual(AssemblyError.None, err);
            }

            Assert.AreEqual(6, assembly.PartCount);
        }

        [Test]
        public void CraftAssembly_UnmountPart_RemovesAndUpdatesStatsDelta()
        {
            var assembly = new CraftAssembly("B.Med.01", hullMass: 100f, hullHealth: 500f);
            var engine = new MountedPartData { category = PartCategory.Engine, mass = 30f, health = 50f, thrust = 200f, energyDraw = 40f };

            assembly.TryMountPart(engine, out int slot);
            Assert.AreEqual(130f, assembly.CurrentStats.totalMass, 0.001f);
            Assert.AreEqual(200f, assembly.CurrentStats.totalThrust, 0.001f);

            bool unmounted = assembly.UnmountPart(slot, out var removed);
            Assert.IsTrue(unmounted);
            Assert.AreEqual(PartCategory.Engine, removed.category);
            Assert.AreEqual(0, assembly.PartCount);

            // Stats should return cleanly to hull base stats
            Assert.AreEqual(100f, assembly.CurrentStats.totalMass, 0.001f);
            Assert.AreEqual(500f, assembly.CurrentStats.totalHealth, 0.001f);
            Assert.AreEqual(0f, assembly.CurrentStats.totalThrust, 0.001f);
            Assert.AreEqual(0f, assembly.CurrentStats.totalEnergyDraw, 0.001f);

            // Unmounting empty slot returns false
            Assert.IsFalse(assembly.UnmountPart(slot, out _));
        }

        [Test]
        public void CraftAssembly_IsValid_FalseWithNoBody()
        {
            var assembly = new CraftAssembly("B.Med.01");
            var engine = new MountedPartData { category = PartCategory.Engine, mass = 10f };

            assembly.TryMountPart(engine, out _);
            Assert.IsFalse(assembly.IsValid);
            Assert.IsFalse(assembly.Validate(out var error));
            Assert.AreEqual(AssemblyError.MissingHull, error);
        }

        [Test]
        public void CraftAssembly_IsValid_TrueWithBodyAndValidWings()
        {
            var assembly = new CraftAssembly("B.Med.01");
            var body = new MountedPartData { category = PartCategory.Body, mass = 20f };
            var wing = new MountedPartData { category = PartCategory.Wing, mass = 5f };

            assembly.TryMountPart(body, out _);
            assembly.TryMountPart(wing, out _);

            Assert.IsTrue(assembly.IsValid);
            Assert.IsTrue(assembly.Validate(out var error));
            Assert.AreEqual(AssemblyError.None, error);
        }

        [Test]
        public void CraftAssembly_CurrentStats_IncrementalAccumulation()
        {
            var assembly = new CraftAssembly("B.Med.01", hullMass: 50f, hullHealth: 200f);
            var partA = new MountedPartData { category = PartCategory.Engine, mass = 10f, health = 20f, thrust = 50f, energyDraw = 5f };
            var partB = new MountedPartData { category = PartCategory.Gun, mass = 15f, health = 30f, thrust = 100f, energyDraw = 15f };

            assembly.TryMountPart(partA, out int slotA);
            assembly.TryMountPart(partB, out int slotB);

            Assert.AreEqual(75f, assembly.CurrentStats.totalMass, 0.001f);
            Assert.AreEqual(250f, assembly.CurrentStats.totalHealth, 0.001f);
            Assert.AreEqual(150f, assembly.CurrentStats.totalThrust, 0.001f);
            Assert.AreEqual(20f, assembly.CurrentStats.totalEnergyDraw, 0.001f);

            assembly.UnmountPart(slotA);
            Assert.AreEqual(65f, assembly.CurrentStats.totalMass, 0.001f);
            Assert.AreEqual(230f, assembly.CurrentStats.totalHealth, 0.001f);
            Assert.AreEqual(100f, assembly.CurrentStats.totalThrust, 0.001f);
            Assert.AreEqual(15f, assembly.CurrentStats.totalEnergyDraw, 0.001f);
        }

        [Test]
        public void CraftAssembly_TryMountPart_RejectsWhenEnergyExceeds999()
        {
            var assembly = new CraftAssembly("B.Med.01");
            var highEnergyPart = new MountedPartData { category = PartCategory.Gun, energyDraw = 1000f };

            var error = assembly.TryMountPart(highEnergyPart, out _);
            Assert.AreEqual(AssemblyError.InsufficientEnergy, error);
            Assert.AreEqual(0, assembly.PartCount);
        }

        [Test]
        public void CraftAssembly_TryMountPart_ExplicitSocket_RejectsOccupiedAndOutOfRange()
        {
            var assembly = new CraftAssembly("B.Med.01");
            var part1 = new MountedPartData { category = PartCategory.Body, mass = 10f };
            var part2 = new MountedPartData { category = PartCategory.Wing, mass = 5f };

            // Out of range socket
            bool mountedNeg = assembly.TryMountPart(-1, part1, out var errNeg);
            Assert.IsFalse(mountedNeg);
            Assert.AreEqual(AssemblyError.SocketOutOfRange, errNeg);

            bool mountedLarge = assembly.TryMountPart(CraftAssembly.MaxCapacity, part1, out var errLarge);
            Assert.IsFalse(mountedLarge);
            Assert.AreEqual(AssemblyError.SocketOutOfRange, errLarge);

            // Mount at socket 5
            bool mounted5 = assembly.TryMountPart(5, part1, out var err5);
            Assert.IsTrue(mounted5);
            Assert.AreEqual(AssemblyError.None, err5);

            // Mount at socket 5 again -> SocketOccupied
            bool mounted5Again = assembly.TryMountPart(5, part2, out var err5Again);
            Assert.IsFalse(mounted5Again);
            Assert.AreEqual(AssemblyError.SocketOccupied, err5Again);
        }
    }
}
