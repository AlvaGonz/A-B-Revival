using UnityEngine;
using UnityEngine.InputSystem;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Desktop input seam: left mouse fires, 'X' toggles autofire (glossary
    /// `OpOneCore.autoFire`). Input System devices are optional so headless
    /// batchmode runs read a neutral state instead of throwing.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MouseCombatFireInput : MonoBehaviour, ICombatFireInput
    {
        private bool _autofire;

        public bool FireHeld => Mouse.current != null && Mouse.current.leftButton.isPressed;
        public bool Autofire => _autofire;

        /// <summary>Sets the autofire toggle directly, for tests and scripted runs.</summary>
        public void SetAutofire(bool enabled)
        {
            _autofire = enabled;
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.xKey.wasPressedThisFrame)
                _autofire = !_autofire;
        }
    }
}
