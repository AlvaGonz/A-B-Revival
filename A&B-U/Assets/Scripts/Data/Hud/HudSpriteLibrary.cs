using UnityEngine;

namespace ABRevival.Core.Hud
{
    /// <summary>
    /// Named references to the original HUD art extracted from the frozen UI library by
    /// ticket #166. The generated asset lives in <c>Resources/UI</c> so the runtime HUD loads
    /// it without scene wiring; regenerate with Tools/A&amp;B Revival/Build HUD Sprite Library.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [CreateAssetMenu(menuName = "A&B Revival/HUD Sprite Library", fileName = "HudSpriteLibrary")]
    public sealed class HudSpriteLibrary : ScriptableObject
    {
        public const string ResourcePath = "UI/HudSpriteLibrary";

        [SerializeField] private Sprite _moneyPanel;
        [SerializeField] private Sprite _pauseButton;
        [SerializeField] private Sprite _helpButton;
        [SerializeField] private Sprite _radar;
        [SerializeField] private Sprite _radarArrow;
        // Blank 493 plate: the exported hud_ai_count sprite has "AI x 10" baked in.
        [SerializeField] private Sprite _aiCount;
        [SerializeField] private Sprite _minimap;
        [SerializeField] private Sprite _pilotPortrait;
        [SerializeField] private Sprite _captainPortrait;
        [SerializeField] private Sprite _portraitBacking;
        [SerializeField] private Sprite _healthBar;
        [SerializeField] private Sprite _energyBar;
        [SerializeField] private Sprite _shieldBar;
        [SerializeField] private Sprite _barFrame;
        [SerializeField] private Sprite _reticle;
        [SerializeField] private Sprite _modeBadge;
        [SerializeField] private Sprite _cameraBadge;
        [SerializeField] private Sprite _damageTelemetryRow;
        [SerializeField] private Sprite _portraitHealthPip;

        public Sprite MoneyPanel => _moneyPanel;
        public Sprite PauseButton => _pauseButton;
        public Sprite HelpButton => _helpButton;
        public Sprite Radar => _radar;
        public Sprite RadarArrow => _radarArrow;
        public Sprite AiCount => _aiCount;
        public Sprite Minimap => _minimap;
        public Sprite PilotPortrait => _pilotPortrait;
        public Sprite CaptainPortrait => _captainPortrait;
        public Sprite PortraitBacking => _portraitBacking;
        public Sprite HealthBar => _healthBar;

        /// <summary>Blue Fuel fill (frozen <c>pow</c> bar art).</summary>
        public Sprite EnergyBar => _energyBar;

        /// <summary>Green reserved third bar fill (frozen <c>powb</c> art); never driven.</summary>
        public Sprite ShieldBar => _shieldBar;
        public Sprite BarFrame => _barFrame;
        public Sprite Reticle => _reticle;
        public Sprite ModeBadge => _modeBadge;
        public Sprite CameraBadge => _cameraBadge;
        public Sprite DamageTelemetryRow => _damageTelemetryRow;
        public Sprite PortraitHealthPip => _portraitHealthPip;
    }
}
