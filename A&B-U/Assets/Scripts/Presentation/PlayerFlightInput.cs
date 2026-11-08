using System;
using ABRevival.Core.Simulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Desktop keyboard/mouse action map for direct flight. Bindings are built in code
    /// (no imported asset) so they are deterministic in tests, and every read touches
    /// preallocated Input System state only: no managed allocation per frame.
    /// X toggles autofire, C cycles the ADR 0005 camera distance, left mouse fires.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class PlayerFlightInput : IDisposable
    {
        public const string MapName = "PlayerFlight";
        public const string ThrustAction = "Thrust";
        public const string SteeringAction = "Steering";
        public const string AutofireAction = "Autofire";
        public const string CameraDistanceAction = "CameraDistance";
        public const string FireAction = "Fire";

        private readonly InputActionMap _map;
        private readonly InputAction _thrust;
        private readonly InputAction _steering;
        private readonly InputAction _autofire;
        private readonly InputAction _cameraDistance;
        private readonly InputAction _fire;

        public PlayerFlightInput()
        {
            _map = new InputActionMap(MapName);

            _thrust = _map.AddAction(ThrustAction, InputActionType.Value, "<Keyboard>/w", expectedControlLayout: "Axis");
            _thrust.AddBinding("<Keyboard>/upArrow");

            // Positive steering is counter-clockwise, matching ShipInput and the frozen
            // baseline's A / Left turn direction.
            _steering = _map.AddAction(SteeringAction, InputActionType.Value, expectedControlLayout: "Axis");
            _steering.AddCompositeBinding("1DAxis")
                .With("positive", "<Keyboard>/a")
                .With("positive", "<Keyboard>/leftArrow")
                .With("negative", "<Keyboard>/d")
                .With("negative", "<Keyboard>/rightArrow");

            _autofire = _map.AddAction(AutofireAction, InputActionType.Button, "<Keyboard>/x");
            _cameraDistance = _map.AddAction(CameraDistanceAction, InputActionType.Button, "<Keyboard>/c");
            _fire = _map.AddAction(FireAction, InputActionType.Button, "<Mouse>/leftButton");
        }

        public bool FireHeld => _fire.IsPressed();

        public bool AutofirePressedThisFrame() => _autofire.WasPressedThisFrame();

        public bool CameraDistancePressedThisFrame() => _cameraDistance.WasPressedThisFrame();

        /// <summary>Reads the fixed-tick flight command. Allocation-free.</summary>
        public ShipInput ReadShipInput()
        {
            return new ShipInput
            {
                throttle = Mathf.Clamp01(_thrust.ReadValue<float>()),
                steering = Mathf.Clamp(_steering.ReadValue<float>(), -1f, 1f)
            };
        }

        public void Enable() => _map.Enable();

        public void Disable() => _map.Disable();

        public void Dispose()
        {
            _map.Disable();
            _map.Dispose();
        }
    }
}
