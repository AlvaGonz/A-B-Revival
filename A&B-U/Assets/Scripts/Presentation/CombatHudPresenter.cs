using System;
using ABRevival.Core.Camera;
using ABRevival.Core.Hud;
using ABRevival.Core.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// UI Toolkit HUD presenter for ADR 0004/0005 rebuilt on the frozen 2011 layout
    /// (ticket #168): money plate and pause/help, radar with distance and mission timer,
    /// AI x N, telemetry, pilot portrait with health/shield/energy and name, minimap with
    /// the Captain portrait, mouse reticle, and the tactical/camera badges.
    /// Elements and sprites are cached once; unchanged values are not rewritten, so the
    /// steady-state per-frame path allocates nothing.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class CombatHudPresenter
    {
        public const string MoneyPanelElement = "hud-money-panel";
        public const string MoneyElement = "hud-money";
        public const string PauseButtonElement = "hud-pause-button";
        public const string HelpButtonElement = "hud-help-button";
        public const string RadarElement = "hud-radar";
        public const string RadarArrowElement = "hud-radar-arrow";
        public const string DistanceElement = "hud-radar-distance";
        public const string MissionTimerElement = "hud-mission-timer";
        public const string AiCountPanelElement = "hud-ai-count";
        public const string AiCountElement = "hud-ai-count-value";
        public const string TelemetryElement = "hud-telemetry";
        public const string LostPartsElement = "hud-lost-parts";
        public const string HullValueElement = "hud-hull-value";
        public const string HullFillElement = "hud-hull-fill";
        public const string ShieldFillElement = "hud-shield-fill";
        public const string EnergyFillElement = "hud-energy-fill";
        public const string PilotPortraitElement = "hud-pilot-portrait";
        public const string PilotNameElement = "hud-pilot-name";
        public const string MinimapElement = "hud-minimap";
        public const string CaptainTileElement = "hud-captain-tile";
        public const string CaptainPortraitElement = "hud-captain-portrait";
        public const string CaptainModeElement = "hud-captain-mode";
        public const string CaptainHealthElement = "hud-captain-health";
        public const string ModeBadgeElement = "hud-mode-badge";
        public const string ModeBadgeTextElement = "hud-mode-badge-text";
        public const string CameraBadgeElement = "hud-camera-badge";
        public const string CameraBadgeTextElement = "hud-camera-badge-text";
        public const string ReticleElement = "hud-reticle";

        private readonly Label _money;
        private readonly Label _distance;
        private readonly Label _missionTimer;
        private readonly Label _aiCount;
        private readonly Label _hullValue;
        private readonly Label _lostParts;
        private readonly Label _pilotName;
        private readonly Label _captainMode;
        private readonly Label _modeBadgeText;
        private readonly Label _cameraBadgeText;
        private readonly VisualElement _hullFill;
        private readonly VisualElement _shieldFill;
        private readonly VisualElement _energyFill;
        private readonly VisualElement _captainTile;
        private readonly VisualElement _captainHealth;
        private readonly VisualElement _cameraBadge;
        private readonly VisualElement _radarArrow;
        private readonly VisualElement _reticle;

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
        private float _lastCaptainHealth = -1f;
        private string _lastCameraBadgeText;
        private bool _lastCameraBadgeVisible;
        private int _lastMoney = int.MinValue;
        private int _lastMissionSecond = -1;
        private int _lastAiCount = int.MinValue;
        private int _lastDistance = int.MinValue;
        private float _lastEnergy = -1f;
        private float _lastShield = -1f;
        private string _lastPilotName;
        private float _lastRadarBearing = float.NaN;
        private bool _hasReticle;
        private float _lastReticleX;
        private float _lastReticleY;
        private string _lastModeBadge;
        private CameraDistancePreset _lastCameraBadgePreset = (CameraDistancePreset)(-1);

        public CombatHudPresenter(VisualElement root, HudSpriteLibrary sprites = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            _money = Require<Label>(root, MoneyElement);
            _distance = Require<Label>(root, DistanceElement);
            _missionTimer = Require<Label>(root, MissionTimerElement);
            _aiCount = Require<Label>(root, AiCountElement);
            _hullValue = Require<Label>(root, HullValueElement);
            _lostParts = Require<Label>(root, LostPartsElement);
            _pilotName = Require<Label>(root, PilotNameElement);
            _captainMode = Require<Label>(root, CaptainModeElement);
            _modeBadgeText = Require<Label>(root, ModeBadgeTextElement);
            _cameraBadgeText = Require<Label>(root, CameraBadgeTextElement);
            _hullFill = Require<VisualElement>(root, HullFillElement);
            _shieldFill = Require<VisualElement>(root, ShieldFillElement);
            _energyFill = Require<VisualElement>(root, EnergyFillElement);
            _captainTile = Require<VisualElement>(root, CaptainTileElement);
            _captainHealth = Require<VisualElement>(root, CaptainHealthElement);
            _cameraBadge = Require<VisualElement>(root, CameraBadgeElement);
            _radarArrow = Require<VisualElement>(root, RadarArrowElement);
            _reticle = Require<VisualElement>(root, ReticleElement);

            _captainTile.visible = false;
            _cameraBadge.visible = false;

            if (sprites != null)
                ApplySprites(root, sprites);
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
                _captainTile.visible = snapshot.CaptainPresent;
                if (snapshot.CaptainPresent)
                    _captainMode.text = snapshot.CaptainModeCode;
            }

            if (snapshot.CaptainHealthFraction != _lastCaptainHealth)
            {
                _lastCaptainHealth = snapshot.CaptainHealthFraction;
                _captainHealth.style.width = Length.Percent(snapshot.CaptainHealthFraction * 100f);
            }

            if (snapshot.Money != _lastMoney)
            {
                _lastMoney = snapshot.Money;
                _money.text = HudTextFormatter.FormatMoney(snapshot.Money);
            }

            int missionSecond = (int)snapshot.MissionSeconds;
            if (missionSecond != _lastMissionSecond)
            {
                _lastMissionSecond = missionSecond;
                _missionTimer.text = HudTextFormatter.FormatMissionTime(missionSecond);
            }

            if (snapshot.AiCount != _lastAiCount)
            {
                _lastAiCount = snapshot.AiCount;
                _aiCount.text = HudTextFormatter.FormatAiCount(snapshot.AiCount);
            }

            int distance = HudTextFormatter.RoundedDistance(snapshot.EnemyDistance);
            if (distance != _lastDistance)
            {
                _lastDistance = distance;
                _distance.text = HudTextFormatter.FormatDistance(distance);
            }

            if (snapshot.EnergyFraction != _lastEnergy)
            {
                _lastEnergy = snapshot.EnergyFraction;
                _energyFill.style.width = Length.Percent(snapshot.EnergyFraction * 100f);
            }

            if (snapshot.ShieldFraction != _lastShield)
            {
                _lastShield = snapshot.ShieldFraction;
                _shieldFill.style.width = Length.Percent(snapshot.ShieldFraction * 100f);
            }

            if (!string.Equals(snapshot.PilotName, _lastPilotName, StringComparison.Ordinal))
            {
                _lastPilotName = snapshot.PilotName;
                _pilotName.text = snapshot.PilotName;
            }

            if (snapshot.RadarBearingDegrees != _lastRadarBearing)
            {
                _lastRadarBearing = snapshot.RadarBearingDegrees;
                _radarArrow.style.rotate = new Rotate(Angle.Degrees(snapshot.RadarBearingDegrees));
            }

            string modeBadge = HudTextFormatter.FormatModeBadge(snapshot.CaptainPresent, snapshot.CaptainMode);
            if (!string.Equals(modeBadge, _lastModeBadge, StringComparison.Ordinal))
            {
                _lastModeBadge = modeBadge;
                _modeBadgeText.text = modeBadge;
            }

            if (snapshot.CameraPreset != _lastCameraBadgePreset)
            {
                _lastCameraBadgePreset = snapshot.CameraPreset;
                _cameraBadgeText.text = snapshot.CameraBadgeText;
            }

            if (snapshot.CameraBadgeVisible != _lastCameraBadgeVisible)
            {
                _lastCameraBadgeVisible = snapshot.CameraBadgeVisible;
                _cameraBadge.visible = snapshot.CameraBadgeVisible;
            }

            if (snapshot.CameraBadgeVisible)
                _cameraBadge.style.opacity = snapshot.CameraBadgeAlpha;

            if (!_hasReticle || snapshot.ReticleX != _lastReticleX || snapshot.ReticleY != _lastReticleY)
            {
                _hasReticle = true;
                _lastReticleX = snapshot.ReticleX;
                _lastReticleY = snapshot.ReticleY;
                _reticle.style.left = snapshot.ReticleX;
                _reticle.style.top = snapshot.ReticleY;
            }
        }

        private static void ApplySprites(VisualElement root, HudSpriteLibrary sprites)
        {
            Assign(root, MoneyPanelElement, sprites.MoneyPanel);
            Assign(root, PauseButtonElement, sprites.PauseButton);
            Assign(root, HelpButtonElement, sprites.HelpButton);
            Assign(root, RadarElement, sprites.Radar);
            Assign(root, RadarArrowElement, sprites.RadarArrow);
            Assign(root, AiCountPanelElement, sprites.AiCount);
            Assign(root, MinimapElement, sprites.Minimap);
            Assign(root, PilotPortraitElement, sprites.PilotPortrait);
            Assign(root, CaptainPortraitElement, sprites.CaptainPortrait);
            Assign(root, HullFillElement, sprites.HealthBar);
            Assign(root, ShieldFillElement, sprites.ShieldBar);
            Assign(root, EnergyFillElement, sprites.EnergyBar);
            Assign(root, ReticleElement, sprites.Reticle);
            Assign(root, ModeBadgeElement, sprites.ModeBadge);
            Assign(root, CameraBadgeElement, sprites.CameraBadge);
            Assign(root, TelemetryElement, sprites.DamageTelemetryRow);
        }

        private static void Assign(VisualElement root, string name, Sprite sprite)
        {
            var image = root.Q<Image>(name);
            if (image == null)
                throw new InvalidOperationException($"HUD image '{name}' is missing from the root.");
            image.sprite = sprite;
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
