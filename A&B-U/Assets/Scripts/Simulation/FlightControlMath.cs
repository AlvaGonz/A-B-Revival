using ABRevival.Core.Camera;
using Unity.Mathematics;

namespace ABRevival.Core.Simulation
{
    /// <summary>
    /// Pure cursor-to-flight mapping for mouse mode (frozen <c>OpOneCore.playerCtrlMode == 0</c>).
    /// Cursor distance sets throttle (100..200 px in the 640 stage, scaled to the 1920 design
    /// width) and the hull turns toward the cursor, exactly like <c>linkUnityTargetA</c>.
    /// No Unity API is used so Tier 1 covers the whole mapping.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class FlightControlMath
    {
        /// <summary>Frozen 640-stage cursor radius where throttle starts, scaled to design pixels.</summary>
        public const float ThrottleDeadZone = CameraDistancePresets.DesignWidth * (100f / 640f);

        /// <summary>Frozen 640-stage cursor radius where throttle saturates, scaled to design pixels.</summary>
        public const float ThrottleFullDistance = CameraDistancePresets.DesignWidth * (200f / 640f);

        /// <summary>Proportional hull turn gain; one design-screen radius turns at full rate.</summary>
        public const float SteeringGain = 3f;

        /// <summary>Maps the cursor into throttle and hull steering, preserving the boost edge.</summary>
        public static ShipInput FromCursor(
            in ShipEntity ship,
            float2 cursor,
            bool boost)
        {
            float2 offset = cursor - ship.position;
            float distance = math.length(offset);

            float throttle = 0f;
            if (distance > ThrottleDeadZone)
            {
                throttle = math.saturate(
                    (distance - ThrottleDeadZone) / (ThrottleFullDistance - ThrottleDeadZone));
            }

            float steering = 0f;
            if (distance > 0.001f)
            {
                float desired = math.atan2(-offset.x, offset.y);
                steering = math.clamp(WrapRadians(desired - ship.rotation) * SteeringGain, -1f, 1f);
            }

            return new ShipInput
            {
                throttle = throttle,
                steering = steering,
                boost = boost
            };
        }

        /// <summary>Wraps an angle into [-PI, PI] so steering never spins the long way round.</summary>
        public static float WrapRadians(float radians)
        {
            return math.atan2(math.sin(radians), math.cos(radians));
        }
    }
}
