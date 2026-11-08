using System;
using NUnit.Framework;
using Unity.Mathematics;
using ABRevival.Core.Simulation;

namespace ABRevival.Tests
{
    [TestFixture]
    public class BallisticsTests
    {
        private const float DeltaTime = 1f / 60f;

        [Test]
        public void EffectiveRange_MatchesADR0012Formula()
        {
            // d = v0 * t + 0.5 * a * t^2 = 300 * 2 + 0.5 * 100 * 4 = 800
            Assert.AreEqual(800f, Ballistics.EffectiveRange(300f, 100f, 2f), 0.001f);
        }

        [Test]
        public void EffectiveRange_NegativeAcceleration_ClampsToZero()
        {
            // Decelerating projectile whose kinematic solution is negative must report zero range.
            Assert.AreEqual(0f, Ballistics.EffectiveRange(10f, -100f, 1f), 0.001f);
        }

        [Test]
        public void EffectiveRange_ZeroOrNegativeLifetime_ReturnsZero()
        {
            Assert.AreEqual(0f, Ballistics.EffectiveRange(300f, 100f, 0f), 0.001f);
            Assert.AreEqual(0f, Ballistics.EffectiveRange(300f, 100f, -1f), 0.001f);
        }

        [Test]
        public void Integrate_LifetimeExpiry_TravelsTheADR0012Range()
        {
            const float muzzleSpeed = 300f;
            const float acceleration = 100f;
            const float lifetime = 2f;

            var projectile = new ProjectileData
            {
                position = float2.zero,
                velocity = new float2(0f, muzzleSpeed),
                acceleration = new float2(0f, acceleration),
                lifetime = lifetime,
                damage = 25f,
                isAlive = true
            };

            for (int step = 0; step < 10000 && projectile.isAlive; step++)
            {
                ProjectileMoveJob.Integrate(ref projectile, math.min(DeltaTime, projectile.lifetime));
            }

            float expectedRange = Ballistics.EffectiveRange(muzzleSpeed, acceleration, lifetime);
            Assert.IsFalse(projectile.isAlive, "Projectile must expire when lifetime reaches zero.");
            Assert.AreEqual(0f, projectile.lifetime, 1e-4f);
            Assert.AreEqual(expectedRange, projectile.distanceTraveled, expectedRange * 0.005f);
        }

        [Test]
        public void SweepSegment_RecoversPreviousPosition_WithoutTouchingRangeState()
        {
            var projectile = new ProjectileData
            {
                position = new float2(0f, 10f),
                velocity = new float2(0f, 600f),
                acceleration = new float2(0f, 100f),
                lifetime = 2f,
                distanceTraveled = 123f,
                damage = 7f,
                isAlive = true
            };

            ProjectileMoveJob.Integrate(ref projectile, DeltaTime);
            var integrated = projectile;

            Ballistics.SweepSegment(in projectile, DeltaTime, out float2 from, out float2 to);

            Assert.AreEqual(integrated.position.x, to.x, 1e-4f);
            Assert.AreEqual(integrated.position.y, to.y, 1e-4f);
            Assert.AreEqual(integrated.position.x - (integrated.velocity.x * DeltaTime), from.x, 1e-4f);
            Assert.AreEqual(integrated.position.y - (integrated.velocity.y * DeltaTime), from.y, 1e-4f);

            // Collision never owns lifetime or range; the ADR 0012 integration keeps them.
            Assert.AreEqual(integrated.lifetime, projectile.lifetime, 0f);
            Assert.AreEqual(integrated.distanceTraveled, projectile.distanceTraveled, 0f);
        }

        [Test]
        public void OrderByFraction_SortsNearestFirst_AndKeepsTiesStable()
        {
            Span<BallisticHit> hits = stackalloc BallisticHit[4];
            hits[0] = Hit(targetIndex: 7, fraction: 0.8f);
            hits[1] = Hit(targetIndex: 1, fraction: 0.5f);
            hits[2] = Hit(targetIndex: 2, fraction: 0.1f);
            hits[3] = Hit(targetIndex: 3, fraction: 0.5f);

            Ballistics.OrderByFraction(hits);

            Assert.AreEqual(2, hits[0].targetIndex);
            Assert.AreEqual(1, hits[1].targetIndex);
            Assert.AreEqual(3, hits[2].targetIndex, "Equal fractions must keep insertion order.");
            Assert.AreEqual(7, hits[3].targetIndex);
        }

        private static BallisticHit Hit(int targetIndex, float fraction)
        {
            return new BallisticHit
            {
                targetIndex = targetIndex,
                fraction = fraction
            };
        }
    }
}
