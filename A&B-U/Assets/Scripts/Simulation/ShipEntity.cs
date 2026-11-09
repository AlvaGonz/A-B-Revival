using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace ABRevival.Core.Simulation
{
    /// <summary>
    /// Unmanaged blittable value type representing a dynamic ship simulation entity in contiguous memory.
    /// Zero heap references.
    /// </summary>
    public struct ShipEntity
    {
        /// <summary>Frozen <c>AIUnity.aiPowMut</c>: the afterburner multiplies applied thrust by three.</summary>
        public const float BoosterThrustMultiplier = 3f;

        /// <summary>Booster fuel consumed per second of afterburner; 5 s from a full default tank.</summary>
        public const float BoosterFuelDrainPerSecond = 100f;

        /// <summary>Frozen <c>fuel</c> pickup refill amount, enough for 2.5 s of afterburner.</summary>
        public const float FuelPickupAmount = 250f;

        public float2 position;
        public float2 velocity;
        public float rotation;         // Heading angle in radians (0 = Vector2.up)
        public float angularVelocity;
        public float mass;
        public float health;
        public float maxHealth;
        /// <summary>Booster fuel (renamed from the inert frozen <c>energy</c> pool).</summary>
        public float boosterFuel;
        public float maxBoosterFuel;
        public float thrustPower;
        public float turnSpeed;        // Radians per second
        public float drag;             // Linear dampening coefficient per second
        public int factionId;          // 0 = Player/Allies, 1 = Hostile
        /// <summary>
        /// Stored as int (not CaptainTacticalMode enum) for Burst blittable compatibility.
        /// Cast via (CaptainTacticalMode)tacticalMode at evaluation time.
        /// </summary>
        public int tacticalMode;
        /// <summary>
        /// Mission spawn point used by Anchor mode (ADR 0006), not the activation position.
        /// </summary>
        public float2 spawnPosition;
        /// <summary>
        /// Fleet formation slot; 0 is the leader, positive slots fan out to starboard,
        /// negative slots to port.
        /// </summary>
        public int formationSlot;

        /// <summary>
        /// Unit forward vector from heading angle (0 rad = up / positive Y).
        /// Shared across ShipMovementJob and TacticalAIJob to eliminate duplication.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Forward(float rotation)
        {
            return new float2(-math.sin(rotation), math.cos(rotation));
        }

        /// <summary>0..1 booster tank fraction shown on the Fuel bar.</summary>
        public float BoosterFraction => maxBoosterFuel > 0f ? math.saturate(boosterFuel / maxBoosterFuel) : 0f;

        /// <summary>Refills the booster tank, clamping at capacity. Frozen <c>fuel</c> pickup path.</summary>
        public void RefuelBooster(float amount)
        {
            if (float.IsNaN(amount) || amount <= 0f) return;
            boosterFuel = math.min(maxBoosterFuel, boosterFuel + amount);
        }
    }

    /// <summary>
    /// Control inputs feeding the ShipMovementJob.
    /// </summary>
    public struct ShipInput
    {
        public float throttle; // 0..1
        public float steering; // -1..1 (positive increases rotation: left / counter-clockwise)
        public bool boost;     // Space: afterburner while boosterFuel remains
    }
}

