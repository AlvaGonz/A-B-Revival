using Unity.Mathematics;

namespace ABRevival.Core.Simulation
{
    /// <summary>
    /// Blittable unmanaged struct representing an active projectile in flight.
    /// Strictly zero heap allocations.
    /// </summary>
    public struct ProjectileData
    {
        public float2 position;
        public float2 velocity;
        public float2 acceleration;
        public float lifetime;
        public float distanceTraveled;
        public float damage;
        public int factionId;
        /// <summary>
        /// Projectile type discriminator (e.g. bullet, missile, beam).
        /// Stored as int for Burst blittable compatibility.
        /// </summary>
        public int projectileType;
        public bool isAlive;
    }
}

