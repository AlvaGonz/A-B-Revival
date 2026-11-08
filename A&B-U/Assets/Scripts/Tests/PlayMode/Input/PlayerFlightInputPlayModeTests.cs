using ABRevival.Core.Camera;
using ABRevival.Core.Simulation;
using ABRevival.Presentation;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// Desktop flight input through the real Input System: W thrusts, A/D steer,
    /// E requests the Captain tactical mode, C cycles the ADR 0005 camera preset
    /// and HUD badge, and the fixed-tick path allocates nothing.
    /// </summary>
    public class PlayerFlightInputPlayModeTests : InputTestFixture
    {
        private const float Step = 1f / 60f;

        private GameObject _host;
        private ScheduledCombatSimulation _simulation;
        private PlayerFlightController _player;
        private CombatCameraRig _cameraRig;
        private CombatHudBehaviour _hud;
        private Keyboard _keyboard;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            _keyboard = InputSystem.AddDevice<Keyboard>();
            InputSystem.AddDevice<Mouse>();

            _host = new GameObject("player-flight-input-test");
            _cameraRig = _host.AddComponent<CombatCameraRig>();
            _hud = _host.AddComponent<CombatHudBehaviour>();
            _player = _host.AddComponent<PlayerFlightController>();

            _simulation = new ScheduledCombatSimulation();
            _simulation.Init(maxShips: 4, maxProjectiles: 4);
            _simulation.PlayerIndex = 0;
            _simulation.ShipCount = 1;
            _simulation.SetShip(0, new ShipEntity
            {
                position = float2.zero,
                velocity = float2.zero,
                rotation = 0f,
                mass = 100f,
                health = 1000f,
                maxHealth = 1000f,
                thrustPower = 1000f,
                turnSpeed = 2f,
                drag = 0.05f,
                factionId = 0
            });

            _player.BeginMission(_simulation, 0, _hud, _cameraRig);
        }

        [TearDown]
        public override void TearDown()
        {
            Object.DestroyImmediate(_host);
            _simulation.Dispose();
            base.TearDown();
        }

        [Test]
        public void ThrustInput_MovesForward_AndReleaseCoastsWithZeroThrottle()
        {
            ShipEntity before = _simulation.GetShip(0);

            Press(_keyboard.wKey);
            for (int i = 0; i < 10; i++)
            {
                _player.Tick(Step);
                _simulation.Tick(Step);
            }

            ShipEntity moving = _simulation.GetShip(0);
            Assert.Greater(moving.position.y, before.position.y, "W must produce forward displacement.");
            Assert.Greater(moving.velocity.y, 0f, "W must produce forward velocity.");
            Assert.AreEqual(0f, moving.rotation, 1e-6f, "Thrust alone must not steer.");

            Release(_keyboard.wKey);
            _player.Tick(Step);
            Assert.AreEqual(0f, _player.CurrentInput.throttle, 1e-6f, "Released thrust writes zero throttle.");

            float speedBeforeCoast = moving.velocity.y;
            _simulation.Tick(Step);
            ShipEntity coasting = _simulation.GetShip(0);
            Assert.Greater(coasting.velocity.y, 0f, "No input must leave the craft drifting, not stopped.");
            Assert.Less(coasting.velocity.y, speedBeforeCoast, "Drag must decay the coasting velocity.");
        }

        [Test]
        public void SteeringInput_ChangesRotationInBothDirections()
        {
            float start = _simulation.GetShip(0).rotation;

            Press(_keyboard.aKey);
            for (int i = 0; i < 5; i++)
            {
                _player.Tick(Step);
                _simulation.Tick(Step);
            }

            float afterLeft = _simulation.GetShip(0).rotation;
            Assert.Greater(afterLeft, start, "A must steer counter-clockwise (positive rotation).");
            Release(_keyboard.aKey);

            Press(_keyboard.dKey);
            for (int i = 0; i < 10; i++)
            {
                _player.Tick(Step);
                _simulation.Tick(Step);
            }

            Assert.Less(_simulation.GetShip(0).rotation, afterLeft, "D must steer clockwise (negative rotation).");
            Release(_keyboard.dKey);
        }

        [Test]
        public void TacticalModeKey_RaisesOneRequestPerPress()
        {
            int requests = 0;
            _player.TacticalModeRequested += () => requests++;

            Press(_keyboard.eKey);
            _player.PollActions();
            Release(_keyboard.eKey);

            Assert.AreEqual(1, requests, "E must raise exactly one tactical-mode request.");
        }

        [Test]
        public void CameraDistanceKey_CyclesRigPresetAndHudBadge()
        {
            Assert.AreEqual(CameraDistancePreset.Short, _cameraRig.Preset);

            Press(_keyboard.cKey);
            _player.PollActions();

            Assert.AreEqual(CameraDistancePreset.Medium, _cameraRig.Preset, "C must advance the camera preset.");
            Assert.AreEqual(CameraDistancePreset.Medium, _hud.State.CameraPreset, "The HUD state must follow the camera.");
            Assert.IsTrue(_hud.State.CameraBadgeVisible, "C must show the ADR 0005 distance badge.");
            Release(_keyboard.cKey);

            Press(_keyboard.cKey);
            _player.PollActions();
            Assert.AreEqual(CameraDistancePreset.Far, _cameraRig.Preset);
            Release(_keyboard.cKey);
        }

        [Test]
        public void FixedTickInputPath_AllocatesNoManagedMemory()
        {
            Press(_keyboard.wKey);
            for (int i = 0; i < 30; i++)
            {
                _player.Tick(Step);
                _simulation.Tick(Step);
            }

            long allocatedBefore = System.GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 600; i++)
            {
                _player.Tick(Step);
            }
            long allocatedBytes = System.GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            Assert.AreEqual(0, allocatedBytes,
                $"Fixed-tick input allocated {allocatedBytes} bytes over 600 ticks.");
            Release(_keyboard.wKey);
        }
    }
}
