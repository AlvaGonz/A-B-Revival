using ABRevival.Core.Simulation;
using UnityEngine;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Weapon firing seam consumed by player input. Ticket #163 replaces the
    /// no-op usage with assembled-part fire ports and projectile spawning.
    /// </summary>
    public interface IWeaponFireControl
    {
        /// <summary>Attempts to fire the player's weapons for this fixed step.</summary>
        void Fire(int shipIndex, in ShipInput input, float deltaTime);
    }

    /// <summary>
    /// Manual player flight for the Mission state. Reads the desktop keyboard/mouse
    /// action map, writes <see cref="ShipInput"/> before every fixed simulation step,
    /// toggles autofire on X, and cycles camera presets on C. A deterministic override
    /// remains for the headless win hook; normal play never uses it.
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

        /// <summary>Read by gameplay and HUD; X flips it.</summary>
        public bool AutofireEnabled { get; private set; }

        /// <summary>Command written to the simulation on the last fixed tick.</summary>
        public ShipInput CurrentInput { get; private set; }

        /// <summary>Set by the game flow when ticket #163 lands; null means no weapons.</summary>
        public IWeaponFireControl WeaponFireControl { get; set; }

        public bool HasMission => _missionActive;

        public bool IsFiring => AutofireEnabled || (_input != null && _input.FireHeld);

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

            if (_input.AutofirePressedThisFrame())
                AutofireEnabled = !AutofireEnabled;

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
            AutofireEnabled = false;
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
            AutofireEnabled = false;
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

        /// <summary>
        /// Writes the player ship's input for one fixed simulation step, then hands the
        /// fire request to the weapon seam when autofire or the mouse trigger is active.
        /// Allocation-free.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_missionActive || _simulation == null || _shipIndex < 0) return;

            ShipInput input = _deterministic || _input == null ? _deterministicInput : _input.ReadShipInput();
            CurrentInput = input;

            if (_shipIndex < _simulation.ShipCount)
                _simulation.SetInput(_shipIndex, in input);

            if (IsFiring)
                WeaponFireControl?.Fire(_shipIndex, in input, deltaTime);
        }

        private void CycleCameraDistance()
        {
            if (_hud != null)
            {
                _hud.CycleCameraDistance();
                _cameraRig?.ApplyPreset(_hud.State.CameraPreset);
                return;
            }

            _cameraRig?.CyclePreset();
        }
    }
}
