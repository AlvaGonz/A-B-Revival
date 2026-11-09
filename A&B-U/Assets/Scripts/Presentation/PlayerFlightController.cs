using System;
using ABRevival.Core.Camera;
using ABRevival.Core.Simulation;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Manual player flight for the Mission state. Reads the desktop keyboard/mouse
    /// action map and writes <see cref="ShipInput"/> before every fixed simulation step.
    /// Q switches keyboard/mouse control mode, C cycles the ADR 0005 camera distance,
    /// Space is the afterburner and E requests the Captain's tactical mode. The mouse
    /// world target is published for independent turret aim; firing is owned by
    /// <see cref="ICombatFireInput"/> and consumed by <see cref="WeaponSystem"/>.
    /// A deterministic override remains for the headless win hook; normal play never uses it.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerFlightController : MonoBehaviour
    {
        private PlayerFlightInput _input;
        private ScheduledCombatSimulation _simulation;
        private CombatHudBehaviour _hud;
        private CombatCameraRig _cameraRig;
        private int _shipIndex = -1;
        private bool _missionActive;
        private bool _deterministic;
        private ShipInput _deterministicInput;
        private PlayerControlMode _controlMode = PlayerControlMode.Keyboard;
        private Vector2 _pointerScreen;
        private Vector2 _screenSize = new Vector2(CameraDistancePresets.DesignWidth, CameraDistancePresets.DesignHeight);
        private bool _hasPointer;

        /// <summary>Raised when E is pressed during a mission; the game flow owns the cycle.</summary>
        public event Action TacticalModeRequested;

        /// <summary>Raised after C changes the camera preset so the flow can autosave it.</summary>
        public event Action<CameraDistancePreset> CameraDistanceChanged;

        /// <summary>Raised after Q (or Options, #181) changes the battle control scheme.</summary>
        public event Action<PlayerControlMode> ControlModeChanged;

        /// <summary>Command written to the simulation on the last fixed tick.</summary>
        public ShipInput CurrentInput { get; private set; }

        /// <summary>Mouse target in world pixels used by the player's turrets.</summary>
        public float2 CurrentAimPoint { get; private set; }

        public PlayerControlMode ControlMode => _controlMode;

        public bool HasMission => _missionActive;

        private void Awake()
        {
            _input = new PlayerFlightInput();
        }

        private void OnDestroy()
        {
            _input?.Dispose();
            _input = null;
        }

        private void OnEnable()
        {
            if (_missionActive) _input?.Enable();
        }

        private void OnDisable()
        {
            _input?.Disable();
        }

        private void Update()
        {
            UpdatePointer();
            PollActions();
        }

        /// <summary>
        /// Consumes edge-triggered actions once per rendered frame. Called by Update in
        /// play and directly by tests; allocation-free.
        /// </summary>
        public void PollActions()
        {
            if (!_missionActive || _deterministic || _input == null) return;

            if (_input.TacticalModePressedThisFrame())
                TacticalModeRequested?.Invoke();

            if (_input.CameraDistancePressedThisFrame())
                CycleCameraDistance();

            if (_input.ControlModePressedThisFrame())
                SetControlMode(_controlMode == PlayerControlMode.Keyboard
                    ? PlayerControlMode.Mouse
                    : PlayerControlMode.Keyboard);
        }

        /// <summary>Sets the battle control scheme; the game flow persists it (#181 Options).</summary>
        public void SetControlMode(PlayerControlMode mode)
        {
            if (mode != PlayerControlMode.Keyboard && mode != PlayerControlMode.Mouse)
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown player control mode.");
            if (_controlMode == mode) return;

            _controlMode = mode;
            ControlModeChanged?.Invoke(mode);
        }

        /// <summary>Test and scripted-input hook: caches the pointer without a physical mouse.</summary>
        public void SetPointer(Vector2 screenPosition, Vector2 screenSize)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f) return;
            _pointerScreen = screenPosition;
            _screenSize = screenSize;
            _hasPointer = true;
        }

        private void UpdatePointer()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            _pointerScreen = mouse.position.ReadValue();
            _screenSize = new Vector2(Screen.width, Screen.height);
            _hasPointer = true;
        }

        /// <summary>Starts manual control for the current mission. Enables the action map.</summary>
        public void BeginMission(
            ScheduledCombatSimulation simulation,
            int shipIndex,
            CombatHudBehaviour hud,
            CombatCameraRig cameraRig)
        {
            _simulation = simulation;
            _shipIndex = shipIndex;
            _hud = hud;
            _cameraRig = cameraRig;
            _missionActive = simulation != null;
            _deterministic = false;
            _deterministicInput = default;
            CurrentInput = default;

            if (_missionActive) _input?.Enable();
        }

        /// <summary>Stops writing input; the next mission starts with autofire off.</summary>
        public void EndMission()
        {
            _missionActive = false;
            _deterministic = false;
            _deterministicInput = default;
            _simulation = null;
            _shipIndex = -1;
            _hud = null;
            _cameraRig = null;
            CurrentInput = default;
            _input?.Disable();
        }

        /// <summary>
        /// Deterministic test hook used by <see cref="GameFlowController.ForceWin"/>:
        /// freezes live input and writes <paramref name="input"/> every fixed tick.
        /// </summary>
        public void BeginDeterministicFlight(in ShipInput input)
        {
            _deterministic = true;
            _deterministicInput = input;
        }

        public void EndDeterministicFlight()
        {
            _deterministic = false;
            _deterministicInput = default;
        }

        /// <summary>Writes the player ship's input for one fixed simulation step. Allocation-free.</summary>
        public void Tick(float deltaTime)
        {
            if (!_missionActive || _simulation == null || _shipIndex < 0 || _shipIndex >= _simulation.ShipCount)
                return;

            ShipEntity ship = _simulation.GetShip(_shipIndex);
            CurrentAimPoint = ResolveAimPoint(in ship);

            ShipInput input;
            if (_deterministic || _input == null)
            {
                input = _deterministicInput;
            }
            else if (_controlMode == PlayerControlMode.Mouse)
            {
                input = FlightControlMath.FromCursor(in ship, CurrentAimPoint, _input.ReadShipInput().boost);
            }
            else
            {
                input = _input.ReadShipInput();
            }

            CurrentInput = input;
            _simulation.SetInput(_shipIndex, in input);
        }

        private float2 ResolveAimPoint(in ShipEntity ship)
        {
            if (_cameraRig != null && _hasPointer)
            {
                Vector2 world = _cameraRig.ScreenToWorld(_pointerScreen, _screenSize);
                return new float2(world.x, world.y);
            }

            if (_hasPointer && _screenSize.x > 0f && _screenSize.y > 0f)
            {
                ViewportPoint centred = CameraDistancePresets.ScreenToWorldCentered(
                    new ViewportPoint(_pointerScreen.x, _pointerScreen.y),
                    new ViewportPoint(_screenSize.x, _screenSize.y),
                    CameraDistancePreset.Short);
                return ship.position + new float2(centred.X, centred.Y);
            }

            // No pointer (headless or a missing mouse): aim straight ahead.
            return ship.position + (ShipEntity.Forward(ship.rotation) * 1000f);
        }

        private void CycleCameraDistance()
        {
            if (_hud != null)
            {
                _hud.CycleCameraDistance();
                _cameraRig?.ApplyPreset(_hud.State.CameraPreset);
                CameraDistanceChanged?.Invoke(_hud.State.CameraPreset);
                return;
            }

            if (_cameraRig != null)
                CameraDistanceChanged?.Invoke(_cameraRig.CyclePreset());
        }
    }
}
