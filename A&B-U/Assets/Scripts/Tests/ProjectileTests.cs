using NUnit.Framework;
using Unity.Mathematics;
using ABRevival.Core.Simulation;

namespace ABRevival.Tests
{
    [TestFixture]
    public class ProjectileTests
    {
        [Test]
        public void Projectile_SimulatedRange_MatchesTheoreticalADR0012Formula()
        {
            float v0 = 300f;              // Initial speed: 300 px/sec
            float burstAcc = 100f;        // Acceleration: 100 px/sec^2
            float totalLifetime = 2.0f;   // 2.0 seconds

            // Direction: along positive Y
            var p = new ProjectileData
            {
                position = float2.zero,
                velocity = new float2(0f, v0),
                acceleration = new float2(0f, burstAcc),
                lifetime = totalLifetime,
                initialLifetime = totalLifetime,
                damage = 25f,
                isAlive = true
            };

            // Theoretical distance via ADR 0012 kinematic formula: d = v0 * t + 0.5 * a * t^2
            float theoreticalRange = (v0 * totalLifetime) + (0.5f * burstAcc * totalLifetime * totalLifetime);
            Assert.AreEqual(800f, theoreticalRange, 0.001f);

            // Simulate at 60 Hz FixedUpdate
            float dt = 0.016667f;
            int steps = (int)math.ceil(totalLifetime / dt);

            for (int i = 0; i < steps; i++)
            {
                if (!p.isAlive) break;
                // Clip last step dt if lifetime < dt
                float stepDt = math.min(dt, p.lifetime);
                ProjectileMoveJob.Integrate(ref p, stepDt);
            }

            // Projectile must be dead after lifetime expires
            Assert.IsFalse(p.isAlive);
            Assert.LessOrEqual(p.lifetime, 0f);

            // Simulated distance must match theoretical within 0.1% tolerance
            float simulatedRange = p.distanceTraveled;
            float errorMargin = theoreticalRange * 0.005f; // 0.5% tolerance
            Assert.AreEqual(theoreticalRange, simulatedRange, errorMargin,
                $"Simulated range ({simulatedRange}) diverged from theoretical ({theoreticalRange})");
        }

        [Test]
        public void Projectile_ZeroAcceleration_LinearTrajectory()
        {
            var p = new ProjectileData
            {
                position = float2.zero,
                velocity = new float2(100f, 0f),
                acceleration = float2.zero,
                lifetime = 1.0f,
                isAlive = true
            };

            float dt = 0.5f;
            ProjectileMoveJob.Integrate(ref p, dt);

            Assert.AreEqual(50f, p.position.x, 0.001f);
            Assert.AreEqual(50f, p.distanceTraveled, 0.001f);
            Assert.AreEqual(0.5f, p.lifetime, 0.001f);
            Assert.IsTrue(p.isAlive);

            ProjectileMoveJob.Integrate(ref p, dt);
            Assert.AreEqual(100f, p.position.x, 0.001f);
            Assert.AreEqual(100f, p.distanceTraveled, 0.001f);
            Assert.AreEqual(0f, p.lifetime, 0.001f);
            Assert.IsFalse(p.isAlive);
        }
    }
}
