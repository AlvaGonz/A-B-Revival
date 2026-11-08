using System;
using System.IO;
using NUnit.Framework;
using Unity.Mathematics;
using ABRevival.Core.Data;
using ABRevival.Core.Simulation;

namespace ABRevival.Tests
{
    [TestFixture]
    public class CombatIntegrationTests
    {
        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git")) && !File.Exists(Path.Combine(dir.FullName, ".git")))
            {
                dir = dir.Parent;
            }
            return dir?.FullName ?? @"C:\Dev\A&B revival";
        }

        private static string GetBinaryDataPath(string filename)
        {
            return Path.Combine(FindRepoRoot(), "src", "assets", "binaryData", filename);
        }

        [Test]
        public void CombatIntegration_EndToEnd_CraftAssembly_And_600Tick_ZeroAlloc_Combat()
        {
            // 1. Load real component definitions from frozen AS3 XML files
            string partPath = GetBinaryDataPath("8_all.AllData_defPart.bin");
            string firePath = GetBinaryDataPath("2_all.AllData_defFire.bin");
            string bulletPath = GetBinaryDataPath("11_all.AllData_defBullet.bin");

            var catalog = PartCatalogParser.ParseCatalogFromFiles(partPath, firePath, bulletPath);
            Assert.AreEqual(271, catalog.Count);

            var bodyDef = catalog.Find(p => p.partName == "B.Small.01");
            var wingDef = catalog.Find(p => p.category == PartCategory.Wing);
            var engineDef = catalog.Find(p => p.partName == "E.Low.01");
            var gunDef = catalog.Find(p => p.partName == "G.25.X1.01");

            Assert.IsNotNull(bodyDef);
            Assert.IsNotNull(wingDef);
            Assert.IsNotNull(engineDef);
            Assert.IsNotNull(gunDef);

            // 2. Assemble a multi-part ship using CraftAssembly domain model
            using var assembly = new CraftAssembly(bodyDef.partName, hullMass: bodyDef.mass, hullHealth: bodyDef.health);

            var bodyPart = new MountedPartData
            {
                partId = bodyDef.partId,
                category = bodyDef.category,
                mass = bodyDef.mass,
                health = bodyDef.health
            };
            var wingPart = new MountedPartData
            {
                partId = wingDef.partId,
                category = wingDef.category,
                mass = wingDef.mass,
                health = wingDef.health
            };
            var enginePart = new MountedPartData
            {
                partId = engineDef.partId,
                category = engineDef.category,
                mass = engineDef.mass,
                health = engineDef.health,
                thrust = engineDef.thrust
            };
            var gunPart = new MountedPartData
            {
                partId = gunDef.partId,
                category = gunDef.category,
                mass = gunDef.mass,
                health = gunDef.health
            };

            Assert.AreEqual(AssemblyError.None, assembly.TryMountPart(bodyPart, out _));
            Assert.AreEqual(AssemblyError.None, assembly.TryMountPart(wingPart, out _));
            Assert.AreEqual(AssemblyError.None, assembly.TryMountPart(enginePart, out _));
            Assert.AreEqual(AssemblyError.None, assembly.TryMountPart(gunPart, out _));

            Assert.IsTrue(assembly.IsValid);
            var stats = assembly.CurrentStats;
            Assert.Greater(stats.totalMass, 0f);
            Assert.Greater(stats.totalHealth, 0f);
            Assert.Greater(stats.totalThrust, 0f);

            // 3. Translate CraftAssembly stats into dynamic combatant ShipEntity
            var playerShip = new ShipEntity
            {
                position = float2.zero,
                velocity = float2.zero,
                rotation = 0f, // facing north / positive Y
                angularVelocity = 0f,
                mass = stats.totalMass,
                health = stats.totalHealth,
                maxHealth = stats.totalHealth,
                thrustPower = stats.totalThrust,
                turnSpeed = 2.0f,
                drag = 0.05f,
                factionId = 0, // Player
                tacticalMode = 0
            };

            var hostileShip = new ShipEntity
            {
                position = new float2(0f, 300f),
                velocity = float2.zero,
                rotation = math.PI, // facing south towards player
                angularVelocity = 0f,
                mass = 150f,
                health = 500f,
                maxHealth = 500f,
                thrustPower = 200f,
                turnSpeed = 1.5f,
                drag = 0.05f,
                factionId = 1, // Hostile
                tacticalMode = (int)CaptainTacticalMode.SeekAndDestroy
            };

            // Spawn projectile from gun
            var projectile = new ProjectileData
            {
                position = new float2(0f, 10f),
                velocity = new float2(0f, gunDef.projectileVelocity),
                acceleration = new float2(0f, gunDef.burstAcceleration),
                lifetime = gunDef.bulletLifetime,
                damage = gunDef.bulletDamage,
                distanceTraveled = 0f,
                isAlive = true,
                factionId = 0,
                projectileType = 1
            };

            // 4. Initialize CombatSimulation runner
            using var sim = new CombatSimulation();
            sim.Init(maxShips: 8, maxProjectiles: 32);

            sim.ShipCount = 2;
            sim.SetShip(0, playerShip);
            sim.SetShip(1, hostileShip);

            // Player full throttle forward
            sim.SetInput(0, new ShipInput { throttle = 1f, steering = 0f });
            sim.SetInput(1, new ShipInput { throttle = 0.5f, steering = 0f });

            sim.ProjectileCount = 1;
            sim.SetProjectile(0, projectile);

            float dt = 0.016667f; // 60 Hz FixedUpdate step

            // 5. Pre-warm JIT to avoid one-off compilation allocations
            for (int p = 0; p < 10; p++)
            {
                sim.Tick(dt);
            }

            // 6. Zero-Alloc Verification across 600 continuous ticks (10 seconds of combat)
            long bytesAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();

            for (int tick = 0; tick < 600; tick++)
            {
                sim.Tick(dt);
            }

            long bytesAllocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            long totalAllocatedInLoop = bytesAllocatedAfter - bytesAllocatedBefore;

            // Zero heap allocations in simulation loop
            Assert.AreEqual(0, totalAllocatedInLoop, $"Simulation loop allocated {totalAllocatedInLoop} bytes over 600 ticks. Zero-alloc contract violated!");

            // 7. Verify physical advancement and kinematic accuracy
            var finalPlayer = sim.GetShip(0);
            Assert.Greater(finalPlayer.position.y, 0f, "Player ship must advance along forward heading.");
            Assert.Greater(finalPlayer.velocity.y, 0f, "Player velocity must increase from engine thrust.");

            var finalProjectile = sim.GetProjectile(0);
            Assert.Greater(finalProjectile.distanceTraveled, 0f, "Projectile must accumulate distance traveled.");

            // 8. Clean disposal test
            sim.Dispose();
            Assert.IsTrue(sim.IsDisposed);
        }
    }
}
