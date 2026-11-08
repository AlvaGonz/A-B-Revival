using System;
using Unity.Mathematics;

namespace ABRevival.Core.Simulation
{
    /// <summary>
    /// One candidate contact along a projectile sweep. Blittable so the contact layer can
    /// keep candidates in native memory and order them without managed allocations.
    /// </summary>
    public struct BallisticHit
    {
        public int targetIndex;
        public float fraction;
        public float2 point;
        public float2 normal;
    }

    /// <summary>
    /// Pure ballistic math shared by the domain range formula, the Burst integration jobs,
    /// and the PhysicsCore2D contact layer (ADR 0012).
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class Ballistics
    {
        /// <summary>
        /// ADR 0012 effective range: d = v0 * t + 0.5 * a * t^2, clamped to zero.
        /// This is the single source of truth for weapon range; contact resolution must
        /// never modify a projectile's lifetime or accumulated distance.
        /// </summary>
        public static float EffectiveRange(float muzzleSpeed, float acceleration, float lifetime)
        {
            if (lifetime <= 0f) return 0f;

            float range = (muzzleSpeed * lifetime) + (0.5f * acceleration * lifetime * lifetime);
            return math.max(0f, range);
        }

        /// <summary>
        /// Swept segment of a projectile that has already been integrated this step.
        /// <see cref="ProjectileMoveJob.Integrate"/> advances position by velocity * deltaTime,
        /// so subtracting that displacement recovers the previous position exactly.
        /// </summary>
        public static void SweepSegment(in ProjectileData projectile, float deltaTime, out float2 from, out float2 to)
        {
            to = projectile.position;
            from = projectile.position - (projectile.velocity * deltaTime);
        }

        /// <summary>
        /// Stable insertion sort by ascending sweep fraction (nearest first).
        /// Hit lists are small (one hull's parts), so this avoids comparer and LINQ allocations.
        /// </summary>
        public static void OrderByFraction(Span<BallisticHit> hits)
        {
            for (int i = 1; i < hits.Length; i++)
            {
                var hit = hits[i];
                int j = i - 1;
                while (j >= 0 && hits[j].fraction > hit.fraction)
                {
                    hits[j + 1] = hits[j];
                    j--;
                }

                hits[j + 1] = hit;
            }
        }
    }
}
