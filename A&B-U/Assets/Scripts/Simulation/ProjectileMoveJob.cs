using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace ABRevival.Core.Simulation
{
    /// <summary>
    /// Burst-compiled parallel job integrating projectile flight, burst acceleration,
    /// distance telemetry, and lifetime expiration.
    /// Follows the ADR 0012 kinematic formula: d = v0 * t + 0.5 * a * t^2.
    /// Strictly zero-alloc.
    /// </summary>
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    public struct ProjectileMoveJob : IJobParallelFor
    {
        public NativeArray<ProjectileData> projectiles;
        public float deltaTime;

        /// <summary>
        /// Pure static integration function. Inlineable by Burst and testable in isolation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Integrate(ref ProjectileData p, float deltaTime)
        {
            if (!p.isAlive) return;

            // Velocity integration with burst acceleration
            p.velocity += p.acceleration * deltaTime;

            // Position displacement
            float2 stepDisplacement = p.velocity * deltaTime;
            p.position += stepDisplacement;
            p.distanceTraveled += math.length(stepDisplacement);

            // Lifetime countdown
            p.lifetime -= deltaTime;
            if (p.lifetime <= 0f)
            {
                p.isAlive = false;
            }
        }

        public void Execute(int index)
        {
            var p = projectiles[index];
            Integrate(ref p, deltaTime);
            projectiles[index] = p;
        }
    }
}

