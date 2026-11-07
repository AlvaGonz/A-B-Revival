using System;
using NUnit.Framework;
using Unity.Mathematics;
using ABRevival.Core.Simulation;

namespace ABRevival.Tests
{
    [TestFixture]
    public class CombatSimulationTests
    {
        [Test]
        public void CombatSimulation_Init_AllocatesBuffers()
        {
            using var sim = new CombatSimulation();
            sim.Init(maxShips: 16, maxProjectiles: 64);

            Assert.IsTrue(sim.IsInitialized);
            Assert.AreEqual(16, sim.MaxShips);
            Assert.AreEqual(64, sim.MaxProjectiles);
            Assert.AreEqual(0, sim.ShipCount);
            Assert.AreEqual(0, sim.ProjectileCount);
        }

        [Test]
        public void CombatSimulation_Tick_ThrowsIfNotInitialized()
        {
            using var sim = new CombatSimulation();
            Assert.Throws<InvalidOperationException>(() => sim.Tick(0.016667f));
        }

        [Test]
        public void CombatSimulation_Tick_CompletesWithoutException()
        {
            using var sim = new CombatSimulation();
            sim.Init(maxShips: 8, maxProjectiles: 16);

            // Tick an empty simulation
            Assert.DoesNotThrow(() => sim.Tick(0.016667f));
        }

        [Test]
        public void CombatSimulation_ShipCount_ReflectsActiveEntities()
        {
            using var sim = new CombatSimulation();
            sim.Init(maxShips: 10, maxProjectiles: 20);

            sim.ShipCount = 3;
            Assert.AreEqual(3, sim.ShipCount);

            sim.ProjectileCount = 7;
            Assert.AreEqual(7, sim.ProjectileCount);

            // Setting beyond max should clamp or throw
            Assert.Throws<ArgumentOutOfRangeException>(() => sim.ShipCount = 15);
            Assert.Throws<ArgumentOutOfRangeException>(() => sim.ProjectileCount = 25);
        }

        [Test]
        public void CombatSimulation_MultiTick_ProducesMotion()
        {
            using var sim = new CombatSimulation();
            sim.Init(maxShips: 4, maxProjectiles: 8);

            var ship = new ShipEntity
            {
                position = float2.zero,
                velocity = float2.zero,
                rotation = 0f,
                mass = 100f,
                thrustPower = 500f,
                drag = 0.05f
            };

            var input = new ShipInput
            {
                throttle = 1f,
                steering = 0f
            };

            sim.ShipCount = 1;
            sim.SetShip(0, ship);
            sim.SetInput(0, input);

            float dt = 0.016667f; // 60 Hz step
            for (int i = 0; i < 10; i++)
            {
                sim.Tick(dt);
            }

            var updatedShip = sim.GetShip(0);
            Assert.Greater(updatedShip.position.y, 0f);
            Assert.Greater(updatedShip.velocity.y, 0f);
        }

        [Test]
        public void CombatSimulation_Dispose_CanBeCalledSafely()
        {
            var sim = new CombatSimulation();
            sim.Init(maxShips: 4, maxProjectiles: 8);
            sim.Dispose();

            Assert.IsTrue(sim.IsDisposed);
            // Calling Dispose again should not throw
            Assert.DoesNotThrow(() => sim.Dispose());
        }

        [Test]
        public void CombatSimulation_AfterDispose_ThrowsObjectDisposedException()
        {
            var sim = new CombatSimulation();
            sim.Init(maxShips: 4, maxProjectiles: 8);
            sim.Dispose();

            Assert.Throws<ObjectDisposedException>(() => sim.Tick(0.016667f));
        }
    }
}

