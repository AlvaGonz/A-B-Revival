using System;
using NUnit.Framework;
using Unity.Mathematics;
using ABRevival.Core.Simulation;
using ABRevival.Presentation;

namespace ABRevival.Tests.PlayMode
{
    [TestFixture]
    public class FleetSimulationPlayModeTests
    {
        private const float DeltaTime = 1f / 60f;

        [Test]
        public void ScheduledCombatSimulation_600Ticks_AcrossSixTacticalModes_ZeroAllocations()
        {
            using var sim = new ScheduledCombatSimulation();
            sim.Init(maxShips: 8, maxProjectiles: 16);
            sim.PlayerIndex = 0;
            sim.ShipCount = 6;

            sim.SetShip(0, new ShipEntity
            {
                position = float2.zero,
                velocity = float2.zero,
                rotation = 0f,
                mass = 100f,
                thrustPower = 500f,
                turnSpeed = 2f,
                drag = 0.05f,
                factionId = 0
            });

            sim.SetShip(1, new ShipEntity
            {
                position = new float2(0f, -200f),
                spawnPosition = new float2(0f, -200f),
                rotation = 0f,
                mass = 120f,
                thrustPower = 300f,
                turnSpeed = 2f,
                drag = 0.05f,
                factionId = 0,
                tacticalMode = (int)CaptainTacticalMode.Anchor
            });

            sim.SetShip(2, new ShipEntity
            {
                position = new float2(-100f, -250f),
                spawnPosition = new float2(-100f, -250f),
                rotation = 0f,
                mass = 90f,
                thrustPower = 250f,
                turnSpeed = 2f,
                drag = 0.05f,
                factionId = 0,
                tacticalMode = (int)CaptainTacticalMode.Escort,
                formationSlot = -1
            });

            sim.SetShip(3, new ShipEntity
            {
                position = new float2(100f, -250f),
                spawnPosition = new float2(100f, -250f),
                rotation = 0f,
                mass = 90f,
                thrustPower = 250f,
                turnSpeed = 2f,
                drag = 0.05f,
                factionId = 0,
                tacticalMode = (int)CaptainTacticalMode.Escort,
                formationSlot = 1
            });

            sim.SetShip(4, new ShipEntity
            {
                position = new float2(0f, 400f),
                spawnPosition = new float2(0f, 400f),
                rotation = math.PI,
                mass = 150f,
                thrustPower = 200f,
                turnSpeed = 1.5f,
                drag = 0.05f,
                factionId = 1,
                tacticalMode = (int)CaptainTacticalMode.SeekAndDestroy
            });

            sim.SetShip(5, new ShipEntity
            {
                position = new float2(300f, 500f),
                spawnPosition = new float2(300f, 500f),
                rotation = math.PI,
                mass = 150f,
                thrustPower = 200f,
                turnSpeed = 1.5f,
                drag = 0.05f,
                factionId = 1,
                tacticalMode = (int)CaptainTacticalMode.Anchor
            });

            // Player full throttle forward; the AI job must never overwrite this input.
            sim.SetInput(0, new ShipInput { throttle = 1f, steering = 0f });

            sim.ProjectileCount = 1;
            sim.SetProjectile(0, new ProjectileData
            {
                position = new float2(0f, 10f),
                velocity = new float2(0f, 600f),
                acceleration = float2.zero,
                lifetime = 2f,
                damage = 10f,
                isAlive = true,
                factionId = 0
            });

            // Pre-warm job reflection data and Burst compilation before measuring.
            for (int i = 0; i < 30; i++)
            {
                sim.Tick(DeltaTime);
            }

            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

            // 600 ticks = 10 seconds at 60 Hz, cycling the Captain through all six modes.
            for (int phase = 0; phase < 6; phase++)
            {
                var captain = sim.GetShip(1);
                captain.tacticalMode = phase;
                sim.SetShip(1, captain);

                for (int tick = 0; tick < 100; tick++)
                {
                    sim.Tick(DeltaTime);
                }
            }

            long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            long allocatedBytes = allocatedAfter - allocatedBefore;

            Assert.AreEqual(0, allocatedBytes,
                $"Scheduled job graph allocated {allocatedBytes} bytes over 600 ticks. Zero-alloc contract violated!");

            var player = sim.GetShip(0);
            Assert.Greater(player.position.y, 0f, "Player ship must advance along its forward heading.");
            Assert.Greater(player.velocity.y, 0f, "Player throttle input must not be overwritten by tactical AI.");

            var projectile = sim.GetProjectile(0);
            Assert.Greater(projectile.distanceTraveled, 0f, "Projectile must accumulate distance traveled.");
            Assert.IsFalse(projectile.isAlive, "Projectile must expire after its lifetime.");

            var captainFinal = sim.GetShip(1);
            Assert.IsTrue(float.IsFinite(captainFinal.position.x) && float.IsFinite(captainFinal.position.y),
                "Captain must remain in a finite position through all tactical modes.");
        }
    }
}
