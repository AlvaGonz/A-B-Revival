using NUnit.Framework;
using Unity.Mathematics;
using ABRevival.Core.Simulation;

namespace ABRevival.Tests
{
    [TestFixture]
    public class ShipMovementTests
    {
        [Test]
        public void ShipMovement_AcceleratesShip_ForwardInFacingDirection()
        {
            var ship = new ShipEntity
            {
                position = float2.zero,
                velocity = float2.zero,
                rotation = 0f, // facing along positive Y
                mass = 100f,
                thrustPower = 500f,
                drag = 0.05f
            };

            var input = new ShipInput
            {
                throttle = 1f,
                steering = 0f
            };

            float dt = 0.016667f; // 60 Hz FixedUpdate
            ShipMovementJob.Integrate(ref ship, input, dt);

            // After 1 step, velocity should be positive forward (Y) and position should have advanced
            Assert.Greater(ship.velocity.y, 0f);
            Assert.Greater(ship.position.y, 0f);
            Assert.AreEqual(0f, ship.position.x, 0.0001f);
        }

        [Test]
        public void ShipMovement_AppliesDrag_DecayingVelocityWhenThrottleIsZero()
        {
            var ship = new ShipEntity
            {
                position = float2.zero,
                velocity = new float2(0f, 100f),
                rotation = 0f,
                mass = 100f,
                thrustPower = 500f,
                drag = 0.1f // 10% drag per second
            };

            var input = new ShipInput
            {
                throttle = 0f,
                steering = 0f
            };

            float dt = 0.016667f;
            ShipMovementJob.Integrate(ref ship, input, dt);

            // Velocity must decay due to drag
            Assert.Less(ship.velocity.y, 100f);
            Assert.Greater(ship.velocity.y, 0f);
        }

        [Test]
        public void ShipMovement_Steering_ModifiesRotationTowardsSteeringInput()
        {
            var ship = new ShipEntity
            {
                position = float2.zero,
                velocity = float2.zero,
                rotation = 0f,
                turnSpeed = math.PI // pi rad/sec (180 deg/sec)
            };

            var input = new ShipInput
            {
                throttle = 0f,
                steering = 1f
            };

            float dt = 0.1f;
            ShipMovementJob.Integrate(ref ship, input, dt);

            // Rotation should have advanced by turnSpeed * steering * dt
            Assert.Greater(ship.rotation, 0f);
            Assert.AreEqual(math.PI * 0.1f, ship.rotation, 0.001f);
        }
    }
}
