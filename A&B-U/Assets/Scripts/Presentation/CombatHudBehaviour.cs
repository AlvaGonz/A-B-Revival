using ABRevival.Core.Camera;
using ABRevival.Core.Hud;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Scene entry point for the combat HUD. Owns the pure HUD state, binds the
    /// UI Toolkit presenter once, and ticks the ephemeral badge fade.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatHudBehaviour : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private CombatCameraRig _cameraRig;

        private CombatHudPresenter _presenter;

        public CombatHudState State { get; } = new CombatHudState();
        public CameraDistancePreset CurrentCameraPreset => State.CameraPreset;

        private void OnDisable()
        {
            _presenter = null;
        }

        private void Update()
        {
            State.Tick(Time.deltaTime);

            if (_presenter == null && _document != null && _document.rootVisualElement != null)
                _presenter = new CombatHudPresenter(_document.rootVisualElement);

            if (_presenter != null)
                _presenter.Render(State.Snapshot());
        }

        /// <summary>Cycles the camera preset and keeps the camera rig in sync.</summary>
        public void CycleCameraDistance()
        {
            CameraDistancePreset preset = State.CycleCameraDistance();
            if (_cameraRig != null)
                _cameraRig.ApplyPreset(preset);
        }
    }
}
