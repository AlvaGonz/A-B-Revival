using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using ABRevival.Core.Data;
using ABRevival.Core.Simulation;
using ABRevival.Presentation;

namespace ABRevival.Tests.PlayMode
{
    [TestFixture]
    public class ProjectileContactPlayModeTests
    {
        private const float DeltaTime = 1f / 60f;
        private const float FastVelocity = 12000f;

        [Test]
        public void ProjectileSweep_CrossesPhysicsCore2DBody_DetectsContact()
        {
            using var solver = new ProjectileContactSolver(maxHulls: 2, maxPartsPerHull: 2, maxProjectiles: 4);
            TestContext.WriteLine($"PhysicsCore2D lengthUnitsPerMeter={solver.LengthUnitsPerMeter}; expected 100 when the ticket #123 settings asset is assigned.");
            if (!solver.UsesProjectWorldScale)
                TestContext.WriteLine("WARNING: PhysicsCoreSettings2D asset is missing; PhysicsCore2D is running on Unity defaults (ticket #123 owns the asset).");

            int hull = solver.RegisterHull(float2.zero, 0f, factionId: 1);
            solver.SetHullParts(hull, SinglePart(health: 100f));

            Span<ProjectileData> projectiles = stackalloc ProjectileData[1];
            projectiles[0] = new ProjectileData
            {
                // Start 100 px below the hull and jump 200 px in one step: discrete contacts would tunnel.
                position = new float2(0f, -100f),
                velocity = new float2(0f, FastVelocity),
                lifetime = 1f,
                distanceTraveled = 42f,
                damage = 10f,
                factionId = 0,
                isAlive = true
            };

            ProjectileMoveJob.Integrate(ref projectiles[0], DeltaTime);
            Assert.Greater(projectiles[0].position.y, 20f, "The projectile must jump past the hull in a single step.");

            float lifetimeBeforeContact = projectiles[0].lifetime;
            float distanceBeforeContact = projectiles[0].distanceTraveled;

            var result = solver.Step(projectiles, DeltaTime);

            Assert.AreEqual(1, result.hitCount);
            Assert.AreEqual(0, result.detachedPartCount);
            Assert.AreEqual(0, result.destroyedHullCount);

            Assert.AreEqual(1, solver.HitCount);
            var hit = solver.Hits[0];
            Assert.AreEqual(0, hit.projectileIndex);
            Assert.AreEqual(hull, hit.hullIndex);
            Assert.AreEqual(0, hit.partIndex);
            Assert.AreEqual(10f, hit.damage, 0f);
            Assert.AreEqual(0.4f, hit.fraction, 0.05f, "The sweep must stop at the near face of the hull.");
            Assert.AreEqual(-20f, hit.point.y, 1f);
            Assert.IsFalse(hit.partDetached);
            Assert.IsFalse(hit.hullDestroyed);

            Assert.AreEqual(90f, solver.GetPart(hull, 0).health, 0.001f);
            Assert.IsTrue(solver.IsPartMounted(hull, 0));
            Assert.IsFalse(projectiles[0].isAlive, "A contact consumes the projectile.");

            // Collision outcome must not rewrite the ADR 0012 range state.
            Assert.AreEqual(lifetimeBeforeContact, projectiles[0].lifetime, 0f);
            Assert.AreEqual(distanceBeforeContact, projectiles[0].distanceTraveled, 0f);
        }

        [Test]
        public void ProjectileSweep_HitsNearestPartFirst()
        {
            using var solver = new ProjectileContactSolver(maxHulls: 1, maxPartsPerHull: 2, maxProjectiles: 1);
            int hull = solver.RegisterHull(float2.zero, 0f, factionId: 1);

            Span<HullPartDefinition> parts = stackalloc HullPartDefinition[2];
            parts[0] = new HullPartDefinition
            {
                part = Part(health: 100f, offset: new float2(0f, -10f)),
                hitRadius = 20f
            };
            parts[1] = new HullPartDefinition
            {
                part = Part(health: 100f, offset: new float2(0f, 10f)),
                hitRadius = 20f
            };
            solver.SetHullParts(hull, parts);

            Span<ProjectileData> projectiles = stackalloc ProjectileData[1];
            projectiles[0] = FastProjectile(damage: 1f, factionId: 0);
            ProjectileMoveJob.Integrate(ref projectiles[0], DeltaTime);

            solver.Step(projectiles, DeltaTime);

            Assert.AreEqual(1, solver.HitCount);
            Assert.AreEqual(0, solver.Hits[0].partIndex, "The nearer part must absorb the hit.");
        }

        [Test]
        public void ProjectileSweep_IgnoresSameFactionHull()
        {
            using var solver = new ProjectileContactSolver(maxHulls: 1, maxPartsPerHull: 1, maxProjectiles: 1);
            int hull = solver.RegisterHull(float2.zero, 0f, factionId: 1);
            solver.SetHullParts(hull, SinglePart(health: 100f));

            Span<ProjectileData> projectiles = stackalloc ProjectileData[1];
            projectiles[0] = FastProjectile(damage: 10f, factionId: 1);
            ProjectileMoveJob.Integrate(ref projectiles[0], DeltaTime);

            var result = solver.Step(projectiles, DeltaTime);

            Assert.AreEqual(0, result.hitCount);
            Assert.AreEqual(100f, solver.GetPart(hull, 0).health, 0f);
            Assert.IsTrue(projectiles[0].isAlive);
        }

        [Test]
        public void ProjectileSweep_LethalHit_DetachesPart_AndReportsHullDestruction()
        {
            using var solver = new ProjectileContactSolver(maxHulls: 1, maxPartsPerHull: 1, maxProjectiles: 2);
            int hull = solver.RegisterHull(float2.zero, 0f, factionId: 1);
            solver.SetHullParts(hull, SinglePart(health: 10f));

            Span<ProjectileData> projectiles = stackalloc ProjectileData[1];
            projectiles[0] = FastProjectile(damage: 25f, factionId: 0);
            ProjectileMoveJob.Integrate(ref projectiles[0], DeltaTime);

            var result = solver.Step(projectiles, DeltaTime);

            Assert.AreEqual(1, result.hitCount);
            Assert.AreEqual(1, result.detachedPartCount);
            Assert.AreEqual(1, result.destroyedHullCount);
            Assert.IsTrue(solver.Hits[0].partDetached);
            Assert.IsTrue(solver.Hits[0].hullDestroyed);
            Assert.IsFalse(solver.IsPartMounted(hull, 0));
            Assert.IsTrue(solver.IsHullDestroyed(hull), "Hull destruction must be recorded.");
            Assert.AreEqual(0f, solver.GetPart(hull, 0).health, 0f);
            Assert.AreEqual(0f, solver.GetHullHealth(hull), 0f);

            Assert.AreEqual(1, solver.DestroyedHullCount);
            Assert.AreEqual(hull, solver.DestroyedHulls[0].hullIndex);
            Assert.AreEqual(1, solver.DestroyedHulls[0].factionId);

            // A destroyed hull must no longer absorb projectiles.
            projectiles[0] = FastProjectile(damage: 25f, factionId: 0);
            ProjectileMoveJob.Integrate(ref projectiles[0], DeltaTime);
            var second = solver.Step(projectiles, DeltaTime);
            Assert.AreEqual(0, second.hitCount);
        }

        [Test]
        public void SetHullPose_MovesHitboxes()
        {
            using var solver = new ProjectileContactSolver(maxHulls: 1, maxPartsPerHull: 1, maxProjectiles: 1);
            int hull = solver.RegisterHull(float2.zero, 0f, factionId: 1);
            solver.SetHullParts(hull, SinglePart(health: 100f));

            Span<ProjectileData> projectiles = stackalloc ProjectileData[1];
            projectiles[0] = FastProjectile(damage: 10f, factionId: 0);
            ProjectileMoveJob.Integrate(ref projectiles[0], DeltaTime);
            Assert.AreEqual(1, solver.Step(projectiles, DeltaTime).hitCount);

            solver.SetHullPose(hull, new float2(0f, 500f), 0f);

            // The old firing line no longer crosses the hull.
            projectiles[0] = FastProjectile(damage: 10f, factionId: 0);
            ProjectileMoveJob.Integrate(ref projectiles[0], DeltaTime);
            Assert.AreEqual(0, solver.Step(projectiles, DeltaTime).hitCount);

            // The same sweep shifted 500 px up does.
            projectiles[0] = new ProjectileData
            {
                position = new float2(0f, 400f),
                velocity = new float2(0f, FastVelocity),
                lifetime = 1f,
                damage = 10f,
                factionId = 0,
                isAlive = true
            };
            ProjectileMoveJob.Integrate(ref projectiles[0], DeltaTime);
            Assert.AreEqual(1, solver.Step(projectiles, DeltaTime).hitCount);
        }

        [Test]
        public void ProjectileSweep_120Steps_ZeroAllocations()
        {
            using var solver = new ProjectileContactSolver(maxHulls: 1, maxPartsPerHull: 1, maxProjectiles: 1);
            int hull = solver.RegisterHull(float2.zero, 0f, factionId: 1);
            solver.SetHullParts(hull, SinglePart(health: 1_000_000f));

            var projectiles = new NativeArray<ProjectileData>(1, Allocator.Persistent);
            try
            {
                long allocatedBefore = 0;
                int hitSteps = 0;

                // 10 warm-up steps absorb JIT, safety-handle and Temp-allocator first-touch costs.
                for (int step = 0; step < 130; step++)
                {
                    if (step == 10) allocatedBefore = GC.GetAllocatedBytesForCurrentThread();

                    var projectile = FastProjectile(damage: 1f, factionId: 0);
                    ProjectileMoveJob.Integrate(ref projectile, DeltaTime);
                    projectiles[0] = projectile;

                    var result = solver.Step(projectiles.AsSpan(), DeltaTime);
                    if (step >= 10 && result.hitCount == 1) hitSteps++;
                }

                long allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
                long allocatedBytes = allocatedAfter - allocatedBefore;

                Assert.AreEqual(0, allocatedBytes,
                    $"Contact step allocated {allocatedBytes} bytes over 120 steps. Zero-alloc contract violated!");
                Assert.AreEqual(120, hitSteps, "Every measured step must resolve a contact.");
            }
            finally
            {
                projectiles.Dispose();
            }
        }

        private static ProjectileData FastProjectile(float damage, int factionId)
        {
            return new ProjectileData
            {
                position = new float2(0f, -100f),
                velocity = new float2(0f, FastVelocity),
                lifetime = 1f,
                damage = damage,
                factionId = factionId,
                isAlive = true
            };
        }

        private static HullPartDefinition[] SinglePart(float health)
        {
            return new[]
            {
                new HullPartDefinition
                {
                    part = Part(health, float2.zero),
                    hitRadius = 20f
                }
            };
        }

        private static MountedPartData Part(float health, float2 offset)
        {
            return new MountedPartData
            {
                partId = 1,
                category = PartCategory.Engine,
                health = health,
                maxHealth = health,
                localOffset = offset
            };
        }
    }
}
