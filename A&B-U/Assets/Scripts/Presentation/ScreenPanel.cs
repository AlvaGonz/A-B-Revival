using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Owns a <see cref="PanelRenderer"/> and exposes its UI root to controllers and tests.
    /// <see cref="PanelRenderer"/> has no root property, so the root only exists inside the
    /// versioned reload callback (ADR 0025). Visibility is applied with <c>display</c>; the
    /// renderer component is never disabled because that removes its visual elements.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [RequireComponent(typeof(PanelRenderer))]
    [DisallowMultipleComponent]
    public sealed class ScreenPanel : MonoBehaviour
    {
        private PanelRenderer _renderer;
        private VisualElement _root;
        private Action<VisualElement> _pending;
        private bool _visible;

        /// <summary>The panel root while the renderer is enabled; null before the first reload.</summary>
        public VisualElement Root => _root;

        /// <summary>Raised on every UI reload with the fresh panel root.</summary>
        public event Action<VisualElement> Reloaded;

        /// <summary>The shared panel settings assigned to the owned renderer.</summary>
        public PanelSettings PanelSettings
        {
            get
            {
                PanelRenderer renderer = RendererComponent;
                return renderer != null ? renderer.panelSettings : null;
            }
        }

        /// <summary>Per-renderer draw order. Proxies <see cref="UnityEngine.Renderer.sortingOrder"/>.</summary>
        public int SortingOrder
        {
            get
            {
                PanelRenderer renderer = RendererComponent;
                return renderer != null ? renderer.sortingOrder : 0;
            }
            set
            {
                PanelRenderer renderer = RendererComponent;
                if (renderer != null) renderer.sortingOrder = value;
            }
        }

        private PanelRenderer RendererComponent =>
            _renderer != null ? _renderer : _renderer = GetComponent<PanelRenderer>();

        private void OnEnable()
        {
            _renderer = GetComponent<PanelRenderer>();
            if (_renderer != null) _renderer.RegisterUIReloadCallback(OnUIReload);
        }

        private void OnDisable()
        {
            if (_renderer != null) _renderer.UnregisterUIReloadCallback(OnUIReload);
            _root = null;
        }

        /// <summary>Shows or hides the panel root without touching the renderer component.</summary>
        public void SetVisible(bool visible)
        {
            _visible = visible;
            ApplyVisibility();
        }

        /// <summary>
        /// Runs once the root exists: immediately when it is already available, otherwise on the
        /// first reload. Multiple callers queue in registration order.
        /// </summary>
        public void WhenReady(Action<VisualElement> action)
        {
            if (action == null) return;
            if (_root != null)
            {
                action(_root);
                return;
            }

            _pending += action;
        }

        private void OnUIReload(PanelRenderer renderer, VisualElement root, int version)
        {
            _root = root;
            ApplyVisibility();

            Action<VisualElement> pending = _pending;
            _pending = null;
            pending?.Invoke(root);

            Reloaded?.Invoke(root);
        }

        private void ApplyVisibility()
        {
            if (_root != null)
                _root.style.display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
