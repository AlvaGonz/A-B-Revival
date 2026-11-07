using NUnit.Framework;
using Unity.Mathematics;
using ABRevival.Core.Simulation;

namespace ABRevival.Tests
{
    [TestFixture]
    public class TacticalAITests
    {
        [Test]
        public void TacticalMode_Enum_MatchesADR0006Definitions()
        {
            Assert.AreEqual(0, (int)CaptainTacticalMode.Anchor);
            Assert.AreEqual(1, (int)CaptainTacticalMode.Escort);
            Assert.AreEqual(2, (int)CaptainTacticalMode.Evade);
            Assert.AreEqual(3, (int)CaptainTacticalMode.SeekAndDestroy);
            Assert.AreEqual(4, (int)CaptainTacticalMode.FireSupport);
            Assert.AreEqual(5, (int)CaptainTacticalMode.Shield);
        }

        [Test]
        public void AnchorMode_HoldsPosition_ThrottleZero()
        {
            var captain = new ShipEntity { position = new float2(100f, 100f), rotation = 0f };
            var player = new ShipEntity { position = new float2(500f, 500f) };
            var hostile = new ShipEntity { position = new float2(200f, 200f) };

            TacticalAIJob.EvaluateTacticalSteering(captain, player, hostile, CaptainTacticalMode.Anchor, out ShipInput input);

            Assert.AreEqual(0f, input.throttle, 0.001f);
        }

        [Test]
        public void EscortMode_TargetsPositionBehindPlayer()
        {
            // Player at (0, 0) facing up (rotation = 0 rad, forward = (0, 1))
            var player = new ShipEntity { position = float2.zero, rotation = 0f };
            // Captain at (0, -200)
            var captain = new ShipEntity { position = new float2(0f, -200f), rotation = 0f };
            var hostile = new ShipEntity { position = new float2(1000f, 1000f) };

            TacticalAIJob.EvaluateTacticalSteering(captain, player, hostile, CaptainTacticalMode.Escort, out ShipInput input);

            // Escort target is ~80px behind player (0, -80).
            // Captain is at (0, -200), so captain should throttle forward towards (0, -80).
            Assert.Greater(input.throttle, 0f);
        }

        [Test]
        public void EvadeMode_FleesFromNearestHostile()
        {
            var captain = new ShipEntity { position = float2.zero, rotation = 0f };
            var player = new ShipEntity { position = new float2(-500f, 0f) };
            // Hostile is directly above at (0, 100)
            var hostile = new ShipEntity { position = new float2(0f, 100f) };

            TacticalAIJob.EvaluateTacticalSteering(captain, player, hostile, CaptainTacticalMode.Evade, out ShipInput input);

            // Flee vector is (0, -100). Throttle must be 1.0 (full evasion)
            Assert.AreEqual(1f, input.throttle, 0.001f);
        }

        [Test]
        public void SeekAndDestroyMode_PursuesNearestHostile()
        {
            var captain = new ShipEntity { position = float2.zero, rotation = 0f };
            var player = new ShipEntity { position = new float2(-500f, 0f) };
            var hostile = new ShipEntity { position = new float2(0f, 400f) };

            TacticalAIJob.EvaluateTacticalSteering(captain, player, hostile, CaptainTacticalMode.SeekAndDestroy, out ShipInput input);

            // Hostile is straight ahead, throttle should be 1.0 and steering ~0
            Assert.AreEqual(1f, input.throttle, 0.001f);
            Assert.AreEqual(0f, input.steering, 0.1f);
        }
    }
}
