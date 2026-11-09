using ABRevival.Core.Camera;
using ABRevival.Core.Hud;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Scene entry point for the combat HUD. Owns the pure HUD state, binds the
    /// UI Toolkit presenter once with the generated <see cref="HudSpriteLibrary"/>, maps the
    /// mouse through the <see cref="CombatCameraRig"/> inverse mapping for the reticle, and
    /// ticks the ephemeral badge fade.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatHudBehaviour : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private CombatCameraRig _cameraRig;
        [SerializeField] private HudSpriteLibrary _sprites;

        private CombatHudPresenter _presenter;

        public CombatHudState State { get; } = new CombatHudState();
        public CameraDistancePreset CurrentCameraPreset => State.CameraPreset;
        public HudSpriteLibrary Sprites => _sprites;

        private void OnDisable()
        {
            _presenter = null;
        }

        private void Update()
        {
            State.Tick(Time.deltaTime);

            if (_document == null || _document.rootVisualElement == null) return;

            if (_presenter == null)
            {
                if (_sprites == null)
                    _sprites = Resources.Load<HudSpriteLibrary>(HudSpriteLibrary.ResourcePath);
                if (_sprites == null)
                    Debug.LogWarning("[CombatHud] No HUD sprite library at Resources/UI; rendering without art.");

                _presenter = new CombatHudPresenter(_document.rootVisualElement, _sprites);
            }

            UpdatePointer(_document.rootVisualElement);
            _presenter.Render(State.Snapshot());
        }

        private void UpdatePointer(VisualElement root)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 panelSize = root.layout.size;
            if (panelSize.x <= 0f || panelSize.y <= 0f)
                panelSize = new Vector2(CameraDistancePresets.DesignWidth, CameraDistancePresets.DesignHeight);

            SetPointer(mouse.position.ReadValue(), new Vector2(Screen.width, Screen.height), panelSize);
        }

        /// <summary>
        /// Projects a bottom-left screen point through the camera rig inverse mapping and
        /// stores the reticle position in HUD design pixels (top-left origin). Public so the
        /// PlayMode gate can drive it without a physical mouse.
        /// </summary>
        public void SetPointer(Vector2 screenPosition, Vector2 screenSize, Vector2 panelSize)
        {
            if (screenSize.x <= 0f || screenSize.y <= 0f || panelSize.x <= 0f || panelSize.y <= 0f)
                return;

            CameraDistancePreset preset = _cameraRig != null ? _cameraRig.Preset : State.CameraPreset;
            float viewportWidth = _cameraRig != null
                ? _cameraRig.WorldViewportWidth
                : CameraDistancePresets.ViewportWidth(preset);
            float viewportHeight = _cameraRig != null
                ? _cameraRig.WorldViewportHeight
                : CameraDistancePresets.ViewportHeight(preset);

            Vector2 viewport = _cameraRig != null
                ? _cameraRig.ScreenToWorldViewport(screenPosition, screenSize)
                : new Vector2(
                    screenPosition.x / screenSize.x * viewportWidth,
                    screenPosition.y / screenSize.y * viewportHeight);

            float worldX = viewport.x - viewportWidth * 0.5f;
            float worldY = viewport.y - viewportHeight * 0.5f;

            State.SetReticlePosition(
                panelSize.x * 0.5f + worldX * panelSize.x / viewportWidth,
                panelSize.y * 0.5f - worldY * panelSize.y / viewportHeight);
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
