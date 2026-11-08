using ABRevival.Core.Camera;
using UnityEngine;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Applies ADR 0005 camera distance tiers to a 2D orthographic camera by
    /// scaling the visible world area, and exposes the inverse mouse-aim mapping
    /// so combat input stays accurate at every preset. The HUD is not scaled.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatCameraRig : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private CameraDistancePreset _preset = CameraDistancePreset.Short;

        public CameraDistancePreset Preset => _preset;
        public float ViewScale => CameraDistancePresets.ViewScale(_preset);
        public float ViewportWidth => CameraDistancePresets.ViewportWidth(_preset);
        public float ViewportHeight => CameraDistancePresets.ViewportHeight(_preset);

        private void Awake()
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();
            ApplyPreset(_preset);
        }

        /// <summary>Injects the camera dependency (used by tests and scene wiring).</summary>
        public void SetCamera(Camera camera)
        {
            _camera = camera;
            ApplyPreset(_preset);
        }

        public void ApplyPreset(CameraDistancePreset preset)
        {
            _preset = preset;

            if (_camera == null)
                return;

            _camera.orthographic = true;
            _camera.orthographicSize = CameraDistancePresets.DesignHeight * 0.5f * ViewScale;
        }

        public CameraDistancePreset CyclePreset()
        {
            ApplyPreset(CameraDistancePresets.Next(_preset));
            return _preset;
        }

        /// <summary>
        /// Centers the orthographic camera on a world point, preserving its depth. The
        /// frozen baseline follows the player craft, so mission coordinates stay 1:1.
        /// </summary>
        public void Focus(Vector2 worldPosition)
        {
            if (_camera == null) return;

            Transform cameraTransform = _camera.transform;
            Vector3 position = cameraTransform.position;
            cameraTransform.position = new Vector3(worldPosition.x, worldPosition.y, position.z);
        }

        /// <summary>Display pixels -> scaled design-space viewport pixels.</summary>
        public Vector2 ScreenToViewport(Vector2 screen, Vector2 screenSize)
        {
            ViewportPoint point = CameraDistancePresets.ScreenToViewport(
                new ViewportPoint(screen.x, screen.y),
                new ViewportPoint(screenSize.x, screenSize.y),
                _preset);
            return new Vector2(point.X, point.Y);
        }

        public Vector2 ViewportToScreen(Vector2 viewport, Vector2 screenSize)
        {
            ViewportPoint point = CameraDistancePresets.ViewportToScreen(
                new ViewportPoint(viewport.x, viewport.y),
                new ViewportPoint(screenSize.x, screenSize.y),
                _preset);
            return new Vector2(point.X, point.Y);
        }

    }
}
