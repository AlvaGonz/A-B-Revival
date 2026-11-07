using Unity.Mathematics;

namespace ABRevival.Core.Simulation
{
    /// <summary>
    /// Unmanaged blittable value type representing a dynamic ship simulation entity in contiguous memory.
    /// Zero heap references.
    /// </summary>
    public struct ShipEntity
    {
        public float2 position;
        public float2 velocity;
        public float rotation;         // Heading angle in radians (0 = Vector2.up)
        public float angularVelocity;
        public float mass;
        public float health;
        public float maxHealth;
        public float thrustPower;
        public float turnSpeed;        // Radians per second
        public float drag;             // Linear dampening coefficient per second
        public int factionId;          // 0 = Player/Allies, 1 = Hostile
        public int tacticalMode;       // ADR 0006 CaptainTacticalMode
    }

    /// <summary>
    /// Control inputs feeding the ShipMovementJob.
    /// </summary>
    public struct ShipInput
    {
        public float throttle; // 0..1
        public float steering; // -1..1 (positive = right / clockwise)
    }
}
