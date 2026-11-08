using System;
using ABRevival.Core.Hud;
using ABRevival.Core.Simulation;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// UI Toolkit HUD presenter for ADR 0004/0005: hull health, cumulative battle
    /// damage telemetry, Captain tactical mode code, and the ephemeral camera
    /// distance badge. Elements are cached once; unchanged values are not
    /// rewritten, so the per-frame path allocates nothing.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class CombatHudPresenter
    {
        public const string HullValueElement = "hud-hull-value";
        public const string HullFillElement = "hud-hull-fill";
        public const string LostPartsElement = "hud-lost-parts";
        public const string CaptainModeElement = "hud-captain-mode";
        public const string CameraBadgeElement = "hud-camera-badge";

        private readonly Label _hullValue;
        private readonly VisualElement _hullFill;
        private readonly Label _lostParts;
        private readonly Label _captainMode;
        private readonly Label _cameraBadge;

        private bool _hasHull;
        private float _lastHullCurrent;
        private float _lastHullMax;
        private int _lastLostGuns;
        private int _lastLostWings;
        private int _lastLostEngines;
        private int _lastLostModules;
        private bool _hasCaptainState;
        private bool _lastCaptainPresent;
        private CaptainTacticalMode _lastCaptainMode;
        private string _lastCameraBadgeText;
        private bool _lastCameraBadgeVisible;

        public CombatHudPresenter(VisualElement root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            _hullValue = Require<Label>(root, HullValueElement);
            _hullFill = Require<VisualElement>(root, HullFillElement);
            _lostParts = Require<Label>(root, LostPartsElement);
            _captainMode = Require<Label>(root, CaptainModeElement);
            _cameraBadge = Require<Label>(root, CameraBadgeElement);

            _captainMode.visible = false;
            _cameraBadge.visible = false;
        }

        public void Render(in CombatHudSnapshot snapshot)
        {
            if (!_hasHull || snapshot.HullCurrent != _lastHullCurrent || snapshot.HullMax != _lastHullMax)
            {
                _hasHull = true;
                _lastHullCurrent = snapshot.HullCurrent;
                _lastHullMax = snapshot.HullMax;
                _hullValue.text = HudTextFormatter.FormatHull(snapshot.HullCurrent, snapshot.HullMax);
                _hullFill.style.width = Length.Percent(snapshot.HullFraction * 100f);
            }

            if (snapshot.LostGuns != _lastLostGuns ||
                snapshot.LostWings != _lastLostWings ||
                snapshot.LostEngines != _lastLostEngines ||
                snapshot.LostModules != _lastLostModules)
            {
                _lastLostGuns = snapshot.LostGuns;
                _lastLostWings = snapshot.LostWings;
                _lastLostEngines = snapshot.LostEngines;
                _lastLostModules = snapshot.LostModules;
                _lostParts.text = HudTextFormatter.FormatLostParts(in snapshot);
            }

            if (!_hasCaptainState ||
                snapshot.CaptainPresent != _lastCaptainPresent ||
                snapshot.CaptainMode != _lastCaptainMode)
            {
                _hasCaptainState = true;
                _lastCaptainPresent = snapshot.CaptainPresent;
                _lastCaptainMode = snapshot.CaptainMode;
                _captainMode.visible = snapshot.CaptainPresent;
                if (snapshot.CaptainPresent)
                    _captainMode.text = snapshot.CaptainModeCode;
            }

            if (!ReferenceEquals(_lastCameraBadgeText, snapshot.CameraBadgeText))
            {
                _lastCameraBadgeText = snapshot.CameraBadgeText;
                _cameraBadge.text = snapshot.CameraBadgeText;
            }

            if (snapshot.CameraBadgeVisible != _lastCameraBadgeVisible)
            {
                _lastCameraBadgeVisible = snapshot.CameraBadgeVisible;
                _cameraBadge.visible = snapshot.CameraBadgeVisible;
            }

            if (snapshot.CameraBadgeVisible)
                _cameraBadge.style.opacity = snapshot.CameraBadgeAlpha;
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            T element = root.Q<T>(name);
            if (element == null)
                throw new InvalidOperationException($"HUD element '{name}' is missing from the root.");
            return element;
        }
    }
}
