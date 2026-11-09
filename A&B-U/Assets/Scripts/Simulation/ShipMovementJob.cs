using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace ABRevival.Core.Simulation
{
    /// <summary>
    /// Burst-compiled parallel job executing 60 Hz physics integration, thrust, drag,
    /// and steering over contiguous ship entity buffers.
    /// Strictly zero-alloc.
    /// </summary>
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    public struct ShipMovementJob : IJobParallelFor
    {
        public NativeArray<ShipEntity> ships;
        [ReadOnly] public NativeArray<ShipInput> inputs;
        public float deltaTime;

        /// <summary>
        /// Pure static integration function. Inlineable by Burst and testable in isolation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Integrate(ref ShipEntity ship, in ShipInput input, float deltaTime)
        {
            // 1. Angular Steering
            ship.rotation += input.steering * ship.turnSpeed * deltaTime;

            // Normalize rotation angle within [-2*PI, 2*PI]
            if (math.abs(ship.rotation) > math.PI * 2f)
            {
                ship.rotation = math.fmod(ship.rotation, math.PI * 2f);
            }

            // 2. Forward vector from heading angle (0 rad = up / positive Y)
            float2 forward = ShipEntity.Forward(ship.rotation);

            // 3. Acceleration & Thrust, with the frozen Space afterburner while fuel remains.
            float throttle = math.clamp(input.throttle, 0f, 1f);
            bool boosting = input.boost && throttle > 0f && ship.boosterFuel > 0f;
            if (boosting)
            {
                throttle *= ShipEntity.BoosterThrustMultiplier;
                ship.boosterFuel = math.max(
                    0f, ship.boosterFuel - (ShipEntity.BoosterFuelDrainPerSecond * deltaTime));
            }

            float safeMass = math.max(1f, ship.mass);
            float appliedThrust = throttle * ship.thrustPower;
            float2 acceleration = (forward * appliedThrust) / safeMass;

            ship.velocity += acceleration * deltaTime;

            // 4. Space Drag Damping
            float damping = math.clamp(1f - (ship.drag * deltaTime), 0f, 1f);
            ship.velocity *= damping;

            // 5. Position Integration
            ship.position += ship.velocity * deltaTime;
        }

        public void Execute(int index)
        {
            var ship = ships[index];
            Integrate(ref ship, inputs[index], deltaTime);
            ships[index] = ship;
        }
    }
}
