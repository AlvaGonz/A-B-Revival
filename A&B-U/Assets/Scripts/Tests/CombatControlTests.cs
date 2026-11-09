using ABRevival.Core.Camera;
using ABRevival.Core.Simulation;
using NUnit.Framework;
using Unity.Mathematics;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 guard for ticket #179: mouse-mode throttle/steering and the Space afterburner
    /// fuel contract. Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class CombatControlTests
    {
        private const float DeltaTime = 1f / 60f;

        [Test]
        public void MouseCursor_InsideDeadZone_HoldsThrottleZero()
        {
            var ship = new ShipEntity { position = float2.zero, rotation = 0f };
            ShipInput input = FlightControlMath.FromCursor(
                in ship, new float2(FlightControlMath.ThrottleDeadZone * 0.5f, 0f), false);

            Assert.AreEqual(0f, input.throttle, 0.0001f);
        }

        [Test]
        public void MouseCursor_DistanceDrivesThrottle_AndSaturatesAtFullRadius()
        {
            var ship = new ShipEntity { position = float2.zero, rotation = 0f };
            float middle = (FlightControlMath.ThrottleDeadZone + FlightControlMath.ThrottleFullDistance) * 0.5f;

            ShipInput half = FlightControlMath.FromCursor(in ship, new float2(middle, 0f), false);
            ShipInput full = FlightControlMath.FromCursor(
                in ship, new float2(FlightControlMath.ThrottleFullDistance * 2f, 0f), false);

            Assert.AreEqual(0.5f, half.throttle, 0.01f);
            Assert.AreEqual(1f, full.throttle, 0.0001f);
        }

        [Test]
        public void MouseCursor_TurnsHullTowardCursorInBothDirections()
        {
            var ship = new ShipEntity { position = float2.zero, rotation = 0f };
            ShipInput right = FlightControlMath.FromCursor(in ship, new float2(500f, 0f), false);
            ShipInput left = FlightControlMath.FromCursor(in ship, new float2(-500f, 0f), false);

            Assert.Less(right.steering, 0f, "A cursor to starboard turns clockwise.");
            Assert.Greater(left.steering, 0f, "A cursor to port turns counter-clockwise.");
            Assert.AreEqual(0f, FlightControlMath.WrapRadians(math.PI * 2f), 0.0001f);
        }

        [Test]
        public void Boost_MultipliesThrust_AndDrainsFuel()
        {
            var plain = new ShipEntity
            {
                position = float2.zero,
                mass = 100f,
                thrustPower = 500f,
                boosterFuel = 500f,
                maxBoosterFuel = 500f
            };
            var boosted = plain;

            ShipMovementJob.Integrate(ref plain, new ShipInput { throttle = 1f }, DeltaTime);
            ShipMovementJob.Integrate(ref boosted, new ShipInput { throttle = 1f, boost = true }, DeltaTime);

            Assert.AreEqual(
                ShipEntity.BoosterThrustMultiplier * plain.velocity.y,
                boosted.velocity.y,
                0.01f);
            Assert.AreEqual(500f - (ShipEntity.BoosterFuelDrainPerSecond * DeltaTime), boosted.boosterFuel, 0.001f);
            Assert.AreEqual(500f, plain.boosterFuel, 0.001f, "Without boost the tank stays full.");
        }

        [Test]
        public void Boost_WithoutFuel_IsIgnored()
        {
            var ship = new ShipEntity
            {
                position = float2.zero,
                mass = 100f,
                thrustPower = 500f,
                boosterFuel = 0f,
                maxBoosterFuel = 500f
            };

            ShipMovementJob.Integrate(ref ship, new ShipInput { throttle = 1f, boost = true }, DeltaTime);

            Assert.AreEqual(500f / 100f * DeltaTime, ship.velocity.y, 0.001f);
            Assert.AreEqual(0f, ship.boosterFuel, 0.001f);
        }

        [Test]
        public void FuelPickup_RefillsAndClampsAtCapacity()
        {
            var ship = new ShipEntity { boosterFuel = 100f, maxBoosterFuel = 500f };

            ship.RefuelBooster(ShipEntity.FuelPickupAmount);
            Assert.AreEqual(350f, ship.boosterFuel, 0.001f);

            ship.RefuelBooster(ShipEntity.FuelPickupAmount * 4f);
            Assert.AreEqual(500f, ship.boosterFuel, 0.001f);
            Assert.AreEqual(1f, ship.BoosterFraction, 0.001f);
        }

        [Test]
        public void DesignScale_IsTheSingleThreeTimesStageScale()
        {
            Assert.AreEqual(1920f, CameraDistancePresets.DesignWidth, 0.001f);
            Assert.AreEqual(1080f, CameraDistancePresets.DesignHeight, 0.001f);
            Assert.AreEqual(3f, CameraDistancePresets.DesignScale, 0.001f);
            Assert.AreEqual(1.5f, CameraDistancePresets.MountArtScale, 0.001f);
        }
    }
}
