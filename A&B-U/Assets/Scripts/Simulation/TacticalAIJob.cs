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
    /// The player ship is skipped so its input stays under player control.
    /// </summary>
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    public struct TacticalAIJob : IJobParallelFor
    {
        public NativeArray<ShipEntity> ships;
        [WriteOnly] public NativeArray<ShipInput> outputs;
        [ReadOnly] public int shipCount;
        [ReadOnly] public int playerIndex;

        private const float EscortTrailDistance = 80f;
        private const float FormationSpacing = 60f;
        private const float EvadeSafeRadius = 300f;
        private const float StandoffRange = 250f;
        private const float ThrottleFullDistance = 40f;

        /// <summary>
        /// Pure static tactical evaluation function.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EvaluateTacticalSteering(
            in ShipEntity captain,
            in ShipEntity player,
            in ShipEntity hostile,
            CaptainTacticalMode mode,
            out ShipInput input)
        {
            EvaluateTacticalSteering(captain, player, hostile, true, true, mode, out input);
        }

        /// <summary>
        /// Pure static tactical evaluation function with explicit target/player validity.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EvaluateTacticalSteering(
            in ShipEntity captain,
            in ShipEntity player,
            in ShipEntity hostile,
            bool hasHostile,
            bool hasPlayer,
            CaptainTacticalMode mode,
            out ShipInput input)
        {
            input = default;

            float2 targetPos = captain.position;
            bool steerToTarget = false;

            switch (mode)
            {
                case CaptainTacticalMode.Anchor:
                    // Hold the mission spawn point (ADR 0006), firing is handled elsewhere.
                    targetPos = captain.spawnPosition;
                    input.throttle = ThrottleTo(captain.position, targetPos);
                    steerToTarget = true;
                    break;

                case CaptainTacticalMode.Escort:
                    // Trail ~80px behind the player in formation.
                    if (!hasPlayer) break;
                    float2 playerForward = ShipEntity.Forward(player.rotation);
                    targetPos = player.position
                                - (playerForward * EscortTrailDistance)
                                + SlotOffset(playerForward, captain.formationSlot);
                    input.throttle = ThrottleTo(captain.position, targetPos);
                    steerToTarget = true;
                    break;

                case CaptainTacticalMode.Evade:
                    // Flee the nearest threat, then hold position once safe.
                    if (!hasHostile) break;
                    float2 awayVector = captain.position - hostile.position;
                    float threatDistanceSq = math.lengthsq(awayVector);
                    if (threatDistanceSq >= EvadeSafeRadius * EvadeSafeRadius) break;
                    if (threatDistanceSq < 0.001f)
                    {
                        awayVector = new float2(0f, -1f);
                    }
                    targetPos = captain.position + awayVector;
                    input.throttle = 1f;
                    steerToTarget = true;
                    break;

                case CaptainTacticalMode.SeekAndDestroy:
                    // Aggressively pursue the closest hostile.
                    if (!hasHostile) break;
                    float2 seekAxis = SafeNormalize(hostile.position - captain.position, new float2(0f, 1f));
                    targetPos = hostile.position + SlotOffset(seekAxis, captain.formationSlot);
                    input.throttle = 1f;
                    steerToTarget = true;
                    break;

                case CaptainTacticalMode.FireSupport:
                    // Close to a ~250px engagement perimeter, then hold position and fire.
                    if (!hasHostile) break;
                    float2 supportAxis = hostile.position - captain.position;
                    float distanceToTarget = math.length(supportAxis);
                    if (distanceToTarget <= StandoffRange) break;
                    supportAxis = SafeNormalize(supportAxis, new float2(0f, 1f));
                    targetPos = hostile.position
                                - (supportAxis * StandoffRange)
                                + SlotOffset(supportAxis, captain.formationSlot);
                    input.throttle = distanceToTarget > StandoffRange + 50f ? 1f : 0.5f;
                    steerToTarget = true;
                    break;

                case CaptainTacticalMode.Shield:
                    // Position between the player and the nearest hostile threat.
                    if (!hasPlayer || !hasHostile) break;
                    float2 interceptAxis = SafeNormalize(hostile.position - player.position, new float2(0f, 1f));
                    targetPos = ((player.position + hostile.position) * 0.5f)
                                + SlotOffset(interceptAxis, captain.formationSlot);
                    input.throttle = ThrottleTo(captain.position, targetPos);
                    steerToTarget = true;
                    break;
            }

            if (!steerToTarget) return;

            float2 toTarget = targetPos - captain.position;
            if (math.lengthsq(toTarget) <= 0.001f) return;

            toTarget = math.normalize(toTarget);
            float2 forward = ShipEntity.Forward(captain.rotation);
            float2 right = new float2(forward.y, -forward.x);

            // Positive steering increases rotation (counter-clockwise), so invert the
            // starboard dot product to turn toward the target instead of away from it.
            input.steering = math.clamp(-math.dot(right, toTarget), -1f, 1f);
        }

        public void Execute(int index)
        {
            if (index >= shipCount || index == playerIndex) return;

            var ship = ships[index];
            var mode = (CaptainTacticalMode)ship.tacticalMode;

            bool hasPlayer = playerIndex >= 0 && playerIndex < shipCount;
            ShipEntity player = hasPlayer ? ships[playerIndex] : default;

            bool hasHostile = TryFindNearestHostile(
                ships, shipCount, ship.position, ship.factionId, out ShipEntity hostile);

            // Fire Support focuses the player's target, which is the hostile nearest to
            // the player; it falls back to the ship's own nearest hostile.
            if (mode == CaptainTacticalMode.FireSupport && hasPlayer)
            {
                hasHostile = TryFindNearestHostile(
                    ships, shipCount, player.position, ship.factionId, out hostile);
            }

            EvaluateTacticalSteering(ship, player, hostile, hasHostile, hasPlayer, mode, out ShipInput input);
            outputs[index] = input;
        }

        private static bool TryFindNearestHostile(
            in NativeArray<ShipEntity> ships,
            int count,
            float2 from,
            int factionId,
            out ShipEntity nearest)
        {
            nearest = default;
            float shortestDistSq = float.MaxValue;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                var candidate = ships[i];
                if (candidate.factionId == factionId) continue;

                float distSq = math.distancesq(from, candidate.position);
                if (distSq >= shortestDistSq) continue;

                shortestDistSq = distSq;
                nearest = candidate;
                found = true;
            }

            return found;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float ThrottleTo(float2 from, float2 to)
        {
            return math.clamp(math.distance(from, to) / ThrottleFullDistance, 0f, 1f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float2 SafeNormalize(float2 value, float2 fallback)
        {
            float lengthSq = math.lengthsq(value);
            return lengthSq > 0.001f ? value * math.rsqrt(lengthSq) : fallback;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float2 SlotOffset(float2 axis, int slot)
        {
            if (slot == 0) return float2.zero;
            float2 right = new float2(axis.y, -axis.x);
            return right * (slot * FormationSpacing);
        }
    }
}
