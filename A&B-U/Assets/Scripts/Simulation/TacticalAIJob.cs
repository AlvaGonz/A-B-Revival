using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace ABRevival.Core.Simulation
{
    /// <summary>
    /// Burst-compiled parallel job evaluating tactical AI modes for fleet ships and Captain Tom.
    /// Strictly adheres to ADR 0006 behavior contracts with zero GC allocations.
    /// </summary>
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    public struct TacticalAIJob : IJobParallelFor
    {
        public NativeArray<ShipEntity> fleetShips;
        [WriteOnly] public NativeArray<ShipInput> outputs;
        [ReadOnly] public ShipEntity player;
        [ReadOnly] public NativeArray<ShipEntity> hostiles;

        /// <summary>
        /// Pure static tactical evaluation function.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EvaluateTacticalSteering(
            in ShipEntity captain,
            in ShipEntity player,
            in ShipEntity nearestHostile,
            CaptainTacticalMode mode,
            out ShipInput input)
        {
            input = new ShipInput { throttle = 0f, steering = 0f };

            float2 targetPos = captain.position;
            bool calculateSteering = false;

            switch (mode)
            {
                case CaptainTacticalMode.Anchor:
                    // Hold mission spawn position, fire if targets in range
                    input.throttle = 0f;
                    input.steering = 0f;
                    return;

                case CaptainTacticalMode.Escort:
                    // Trail ~80px behind player craft
                    float2 playerForward = ShipEntity.Forward(player.rotation);
                    targetPos = player.position - (playerForward * 80f);
                    float distToEscort = math.distance(captain.position, targetPos);
                    input.throttle = math.clamp(distToEscort / 40f, 0f, 1f);
                    calculateSteering = true;
                    break;

                case CaptainTacticalMode.Evade:
                    // Flee from nearest hostile threat vector
                    float2 awayVector = captain.position - nearestHostile.position;
                    if (math.lengthsq(awayVector) < 0.001f)
                    {
                        awayVector = new float2(0f, -1f);
                    }
                    targetPos = captain.position + awayVector;
                    input.throttle = 1f;
                    calculateSteering = true;
                    break;

                case CaptainTacticalMode.SeekAndDestroy:
                    // Aggressively pursue nearest hostile
                    targetPos = nearestHostile.position;
                    input.throttle = 1f;
                    calculateSteering = true;
                    break;

                case CaptainTacticalMode.FireSupport:
                    // Maintain ~250px engagement perimeter
                    float distToTarget = math.distance(captain.position, nearestHostile.position);
                    targetPos = nearestHostile.position;
                    input.throttle = distToTarget > 250f ? 1f : (distToTarget < 200f ? 0f : 0.5f);
                    calculateSteering = true;
                    break;

                case CaptainTacticalMode.Shield:
                    // Position intercept midpoint between player and hostile threat
                    targetPos = (player.position + nearestHostile.position) * 0.5f;
                    float distToShield = math.distance(captain.position, targetPos);
                    input.throttle = math.clamp(distToShield / 40f, 0f, 1f);
                    calculateSteering = true;
                    break;
            }

            if (calculateSteering)
            {
                float2 toTarget = targetPos - captain.position;
                if (math.lengthsq(toTarget) > 0.001f)
                {
                    toTarget = math.normalize(toTarget);
                    float2 forward = ShipEntity.Forward(captain.rotation);
                    float2 right = new float2(forward.y, -forward.x);

                    // Cross product (right dot toTarget) determines steering angle
                    float turnFactor = math.dot(right, toTarget);
                    input.steering = math.clamp(turnFactor, -1f, 1f);
                }
            }
        }

        public void Execute(int index)
        {
            var ship = fleetShips[index];
            var mode = (CaptainTacticalMode)ship.tacticalMode;

            // Find nearest hostile
            ShipEntity nearestHostile = default;
            float shortestDistSq = float.MaxValue;
            for (int h = 0; h < hostiles.Length; h++)
            {
                float dSq = math.distancesq(ship.position, hostiles[h].position);
                if (dSq < shortestDistSq)
                {
                    shortestDistSq = dSq;
                    nearestHostile = hostiles[h];
                }
            }

            EvaluateTacticalSteering(ship, player, nearestHostile, mode, out ShipInput input);
            outputs[index] = input;
        }
    }
}

