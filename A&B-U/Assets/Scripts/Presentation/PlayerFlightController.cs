using System;
using ABRevival.Core.Camera;
using ABRevival.Core.Simulation;
using UnityEngine;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Manual player flight for the Mission state. Reads the desktop keyboard/mouse
    /// action map and writes <see cref="ShipInput"/> before every fixed simulation step.
    /// C cycles the ADR 0005 camera distance and E requests the Captain's tactical mode;
    /// firing is owned by <see cref="ICombatFireInput"/> and consumed by <see cref="WeaponSystem"/>.
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

        /// <summary>Raised when E is pressed during a mission; the game flow owns the cycle.</summary>
        public event Action TacticalModeRequested;

        /// <summary>Raised after C changes the camera preset so the flow can autosave it.</summary>
        public event Action<CameraDistancePreset> CameraDistanceChanged;

        /// <summary>Command written to the simulation on the last fixed tick.</summary>
        public ShipInput CurrentInput { get; private set; }

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
            if (!_missionActive || _simulation == null || _shipIndex < 0) return;

            ShipInput input = _deterministic || _input == null ? _deterministicInput : _input.ReadShipInput();
            CurrentInput = input;

            if (_shipIndex < _simulation.ShipCount)
                _simulation.SetInput(_shipIndex, in input);
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
