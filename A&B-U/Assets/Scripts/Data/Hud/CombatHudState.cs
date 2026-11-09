using System;
using ABRevival.Core.Camera;
using ABRevival.Core.Data;
using ABRevival.Core.Simulation;

namespace ABRevival.Core.Hud
{
    /// <summary>
    /// Pure combat HUD state established by ADR 0004: hull health, cumulative
    /// battle-damage telemetry, Captain tactical mode code, and the ephemeral
    /// camera-distance badge. The frozen HUD readouts added in ticket #168 (money,
    /// mission timer, AI count, target distance, pilot name, radar bearing, reticle)
    /// and the ticket #179 Fuel bar live here too. The third bar is deliberately inert.
    /// No Unity API is used so the Tier 1 gate can exercise every transition.
    /// </summary>
    public sealed class CombatHudState
    {
        public const float CameraBadgeDurationSeconds = 1.5f;
        public const float CameraBadgeFadeSeconds = 0.5f;

        private float _hullCurrent = 1f;
        private float _hullMax = 1f;
        private int _lostGuns;
        private int _lostWings;
        private int _lostEngines;
        private int _lostModules;
        private bool _captainPresent;
        private CaptainTacticalMode _captainMode = CaptainTacticalMode.Anchor;
        private float _captainHealthFraction = 1f;
        private CameraDistancePreset _cameraPreset = CameraDistancePreset.Short;
        private float _cameraBadgeRemaining;
        private int _money;
        private float _missionSeconds;
        private int _aiCount;
        private float _enemyDistance;
        private float _fuelFraction = 1f;
        private string _pilotName = string.Empty;
        private float _radarBearingDegrees;
        private float _reticleX = CameraDistancePresets.DesignWidth * 0.5f;
        private float _reticleY = CameraDistancePresets.DesignHeight * 0.5f;

        public float HullCurrent => _hullCurrent;
        public float HullMax => _hullMax;
        public float HullFraction => _hullMax > 0f ? _hullCurrent / _hullMax : 0f;

        public int LostGuns => _lostGuns;
        public int LostWings => _lostWings;
        public int LostEngines => _lostEngines;
        public int LostModules => _lostModules;
        public int TotalLostParts => _lostGuns + _lostWings + _lostEngines + _lostModules;

        public bool CaptainPresent => _captainPresent;
        public CaptainTacticalMode CaptainMode => _captainMode;
        public float CaptainHealthFraction => _captainHealthFraction;

        public CameraDistancePreset CameraPreset => _cameraPreset;
        public bool CameraBadgeVisible => _cameraBadgeRemaining > 0f;

        public int Money => _money;
        public float MissionSeconds => _missionSeconds;
        public int AiCount => _aiCount;
        public float EnemyDistance => _enemyDistance;

        /// <summary>Booster tank fill shown on the bar directly below hull health (ticket #179).</summary>
        public float FuelFraction => _fuelFraction;
        public string PilotName => _pilotName;
        public float RadarBearingDegrees => _radarBearingDegrees;
        public float ReticleX => _reticleX;
        public float ReticleY => _reticleY;

        public float CameraBadgeAlpha
        {
            get
            {
                if (_cameraBadgeRemaining <= 0f) return 0f;
                if (_cameraBadgeRemaining >= CameraBadgeFadeSeconds) return 1f;
                return _cameraBadgeRemaining / CameraBadgeFadeSeconds;
            }
        }

        public void SetHullHealth(float current, float max)
        {
            if (float.IsNaN(current) || float.IsNaN(max))
                throw new ArgumentException("Hull health cannot be NaN.");
            if (max <= 0f)
                throw new ArgumentOutOfRangeException(nameof(max), "Maximum hull health must be positive.");

            _hullMax = max;
            _hullCurrent = current < 0f ? 0f : current > max ? max : current;
        }

        /// <summary>Booster Fuel bar fill, already normalized by the caller.</summary>
        public void SetFuelFraction(float fraction)
        {
            _fuelFraction = ClampFraction(fraction, nameof(fraction));
        }

        public void SetCaptainHealth(float current, float max)
        {
            if (float.IsNaN(current) || float.IsNaN(max))
                throw new ArgumentException("Captain health cannot be NaN.");
            _captainHealthFraction = max > 0f ? ClampFraction(current / max, nameof(current)) : 0f;
        }

        /// <summary>Records a destroyed modular part from its legacy domain category.</summary>
        public void RecordLostPart(PartCategory category)
        {
            switch (category)
            {
                case PartCategory.Body:
                    return; // Hull loss destroys the craft; ADR 0004 ignores it.
                case PartCategory.Wing:
                    ++_lostWings;
                    return;
                case PartCategory.Engine:
                    ++_lostEngines;
                    return;
                case PartCategory.Gun:
                    ++_lostGuns;
                    return;
                default:
                    ++_lostModules;
                    return;
            }
        }

        public void RecordLostPart(BattleDamageCategory category)
        {
            switch (category)
            {
                case BattleDamageCategory.Gun:
                    ++_lostGuns;
                    return;
                case BattleDamageCategory.Wing:
                    ++_lostWings;
                    return;
                case BattleDamageCategory.Engine:
                    ++_lostEngines;
                    return;
                case BattleDamageCategory.Module:
                    ++_lostModules;
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown battle damage category.");
            }
        }

        public void SetCaptainPresence(bool present)
        {
            _captainPresent = present;
        }

        public void SetCaptainMode(CaptainTacticalMode mode)
        {
            if (!IsKnownMode(mode))
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown Captain tactical mode.");
            _captainMode = mode;
        }

        /// <summary>Save-file credits shown on the top-left money plate.</summary>
        public void SetMoney(int money)
        {
            _money = money < 0 ? 0 : money;
        }

        /// <summary>Elapsed mission time in seconds, fed from the mission tick count.</summary>
        public void SetMissionSeconds(float seconds)
        {
            if (float.IsNaN(seconds))
                throw new ArgumentException("Mission time cannot be NaN.", nameof(seconds));
            _missionSeconds = seconds < 0f ? 0f : seconds;
        }

        /// <summary>Living hostile count shown on the AI x N plate.</summary>
        public void SetAiCount(int count)
        {
            _aiCount = count < 0 ? 0 : count;
        }

        /// <summary>Distance to the nearest living hostile, in world pixels.</summary>
        public void SetEnemyDistance(float distance)
        {
            if (float.IsNaN(distance))
                throw new ArgumentException("Distance cannot be NaN.", nameof(distance));
            _enemyDistance = distance < 0f ? 0f : distance;
        }

        public void SetPilotName(string name)
        {
            _pilotName = name ?? string.Empty;
        }

        /// <summary>Player heading used to rotate the radar arrow; degrees, clockwise.</summary>
        public void SetRadarBearing(float degrees)
        {
            if (float.IsNaN(degrees))
                throw new ArgumentException("Radar bearing cannot be NaN.", nameof(degrees));
            _radarBearingDegrees = degrees;
        }

        /// <summary>Reticle position in HUD design pixels (top-left origin).</summary>
        public void SetReticlePosition(float x, float y)
        {
            if (float.IsNaN(x) || float.IsNaN(y))
                throw new ArgumentException("Reticle position cannot be NaN.");
            _reticleX = x;
            _reticleY = y;
        }

        /// <summary>
        /// Applies a persisted preset without showing the ephemeral badge. Used at
        /// mission start when CONFIG[6] is restored (ADR 0005).
        /// </summary>
        public void SetCameraDistance(CameraDistancePreset preset)
        {
            _cameraPreset = ValidatePreset(preset);
        }

        /// <summary>Cycles Short -> Medium -> Far -> Short and shows the badge.</summary>
        public CameraDistancePreset CycleCameraDistance()
        {
            _cameraPreset = CameraDistancePresets.Next(_cameraPreset);
            ShowCameraDistanceBadge();
            return _cameraPreset;
        }

        public void ShowCameraDistanceBadge()
        {
            _cameraBadgeRemaining = CameraBadgeDurationSeconds;
        }

        /// <summary>Advances the ephemeral badge fade; call once per frame.</summary>
        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime))
                throw new ArgumentException("deltaTime cannot be NaN.", nameof(deltaTime));
            if (deltaTime <= 0f || _cameraBadgeRemaining <= 0f)
                return;

            _cameraBadgeRemaining -= deltaTime;
            if (_cameraBadgeRemaining < 0f)
                _cameraBadgeRemaining = 0f;
        }

        public CombatHudSnapshot Snapshot()
        {
            return new CombatHudSnapshot(
                _hullCurrent,
                _hullMax,
                _lostGuns,
                _lostWings,
                _lostEngines,
                _lostModules,
                _captainPresent,
                _captainMode,
                _captainHealthFraction,
                _cameraPreset,
                CameraBadgeVisible,
                CameraBadgeAlpha,
                _money,
                _missionSeconds,
                _aiCount,
                _enemyDistance,
                _fuelFraction,
                _pilotName,
                _radarBearingDegrees,
                _reticleX,
                _reticleY);
        }

        private static float ClampFraction(float value, string parameter)
        {
            if (float.IsNaN(value))
                throw new ArgumentException("Fraction cannot be NaN.", parameter);
            return value < 0f ? 0f : value > 1f ? 1f : value;
        }

        private static bool IsKnownMode(CaptainTacticalMode mode)
        {
            return (int)mode >= (int)CaptainTacticalMode.Anchor &&
                   (int)mode <= (int)CaptainTacticalMode.Shield;
        }

        private static CameraDistancePreset ValidatePreset(CameraDistancePreset preset)
        {
            if ((int)preset < 0 || (int)preset >= CameraDistancePresets.Count)
                throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unknown camera distance preset.");
            return preset;
        }
    }

    public readonly struct CombatHudSnapshot
    {
        public readonly float HullCurrent;
        public readonly float HullMax;
        public readonly float HullFraction;
        public readonly int LostGuns;
        public readonly int LostWings;
        public readonly int LostEngines;
        public readonly int LostModules;
        public readonly bool CaptainPresent;
        public readonly CaptainTacticalMode CaptainMode;
        public readonly string CaptainModeCode;
        public readonly float CaptainHealthFraction;
        public readonly CameraDistancePreset CameraPreset;
        public readonly bool CameraBadgeVisible;
        public readonly float CameraBadgeAlpha;
        public readonly string CameraBadgeText;
        public readonly int Money;
        public readonly float MissionSeconds;
        public readonly int AiCount;
        public readonly float EnemyDistance;
        public readonly float FuelFraction;
        public readonly string PilotName;
        public readonly float RadarBearingDegrees;
        public readonly float ReticleX;
        public readonly float ReticleY;

        internal CombatHudSnapshot(
            float hullCurrent,
            float hullMax,
            int lostGuns,
            int lostWings,
            int lostEngines,
            int lostModules,
            bool captainPresent,
            CaptainTacticalMode captainMode,
            float captainHealthFraction,
            CameraDistancePreset cameraPreset,
            bool cameraBadgeVisible,
            float cameraBadgeAlpha,
            int money,
            float missionSeconds,
            int aiCount,
            float enemyDistance,
            float fuelFraction,
            string pilotName,
            float radarBearingDegrees,
            float reticleX,
            float reticleY)
        {
            HullCurrent = hullCurrent;
            HullMax = hullMax;
            HullFraction = hullMax > 0f ? hullCurrent / hullMax : 0f;
            LostGuns = lostGuns;
            LostWings = lostWings;
            LostEngines = lostEngines;
            LostModules = lostModules;
            CaptainPresent = captainPresent;
            CaptainMode = captainMode;
            CaptainModeCode = TacticalModeCodes.Code(captainMode);
            CaptainHealthFraction = captainHealthFraction;
            CameraPreset = cameraPreset;
            CameraBadgeVisible = cameraBadgeVisible;
            CameraBadgeAlpha = cameraBadgeAlpha;
            CameraBadgeText = CameraDistancePresets.BadgeText(cameraPreset);
            Money = money;
            MissionSeconds = missionSeconds;
            AiCount = aiCount;
            EnemyDistance = enemyDistance;
            FuelFraction = fuelFraction;
            PilotName = pilotName;
            RadarBearingDegrees = radarBearingDegrees;
            ReticleX = reticleX;
            ReticleY = reticleY;
        }

        public bool HasLostParts => LostGuns + LostWings + LostEngines + LostModules > 0;
        public int TotalLostParts => LostGuns + LostWings + LostEngines + LostModules;
    }

    /// <summary>Two-letter tactical mode codes shown on the Captain portrait (ADR 0006).</summary>
    public static class TacticalModeCodes
    {
        private static readonly string[] Codes = { "AN", "ES", "EV", "SD", "FS", "SH" };

        public static int Count => Codes.Length;

        public static string Code(CaptainTacticalMode mode)
        {
            int index = (int)mode;
            if (index < 0 || index >= Codes.Length)
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown Captain tactical mode.");
            return Codes[index];
        }
    }

    /// <summary>
    /// HUD text formatting kept out of the presenter so the exact baseline
    /// wording is Tier 1 testable. Callers invoke these only when a value changes.
    /// </summary>
    public static class HudTextFormatter
    {
        private static readonly System.Globalization.CultureInfo Invariant =
            System.Globalization.CultureInfo.InvariantCulture;

        public static string FormatHull(float current, float max)
        {
            return string.Format(Invariant, "{0:0} / {1:0}", current, max);
        }

        public static string FormatMoney(int money)
        {
            return (money < 0 ? 0 : money).ToString("N0", Invariant) + " G";
        }

        // shortcut: cached to 99:59; longer missions fall back to one format per second.
        private const int MissionTimeCacheSeconds = 100 * 60;
        private static readonly string[] MissionTimeCache = BuildMissionTimeCache();

        /// <summary>Mission clock as mm:ss; hours fold into minutes like the frozen timer.</summary>
        public static string FormatMissionTime(float seconds)
        {
            return FormatMissionTime(seconds > 0f ? (int)Math.Floor(seconds) : 0);
        }

        /// <summary>Mission clock as mm:ss from a cached table so the HUD never allocates in Update.</summary>
        public static string FormatMissionTime(int totalSeconds)
        {
            if (totalSeconds < 0) totalSeconds = 0;
            if (totalSeconds < MissionTimeCache.Length) return MissionTimeCache[totalSeconds];
            return (totalSeconds / 60).ToString("00", Invariant) + ":" +
                   (totalSeconds % 60).ToString("00", Invariant);
        }

        private static string[] BuildMissionTimeCache()
        {
            var cache = new string[MissionTimeCacheSeconds];
            for (int second = 0; second < cache.Length; second++)
            {
                cache[second] = (second / 60).ToString("00", Invariant) + ":" +
                                (second % 60).ToString("00", Invariant);
            }

            return cache;
        }

        public static string FormatAiCount(int count)
        {
            return "AI x " + (count < 0 ? 0 : count).ToString(Invariant);
        }

        public static string FormatDistance(float distance)
        {
            return FormatDistance(RoundedDistance(distance));
        }

        public static string FormatDistance(int distance)
        {
            return (distance < 0 ? 0 : distance).ToString(Invariant);
        }

        public static int RoundedDistance(float distance)
        {
            double rounded = Math.Round(distance > 0f ? distance : 0f, MidpointRounding.AwayFromZero);
            return (int)rounded;
        }

        public static string FormatModeBadge(bool captainPresent, CaptainTacticalMode mode)
        {
            if (!captainPresent) return "TACTICAL --";
            int index = (int)mode;
            if (index < 0 || index >= TacticalModeCodes.Count)
                throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown Captain tactical mode.");
            return ModeBadges[index];
        }

        private static readonly string[] ModeBadges =
        {
            "TACTICAL AN", "TACTICAL ES", "TACTICAL EV",
            "TACTICAL SD", "TACTICAL FS", "TACTICAL SH"
        };

        public static string FormatLostParts(in CombatHudSnapshot snapshot)
        {
            if (!snapshot.HasLostParts)
                return string.Empty;

            var builder = new System.Text.StringBuilder(64);
            builder.Append("LOST: ");
            AppendCategory(builder, "Gun", snapshot.LostGuns);
            AppendCategory(builder, "Wing", snapshot.LostWings);
            AppendCategory(builder, "Engine", snapshot.LostEngines);
            AppendCategory(builder, "Module", snapshot.LostModules);

            // Remove the trailing space between badges.
            if (builder.Length > 0)
                builder.Length--;
            return builder.ToString();
        }

        private static void AppendCategory(System.Text.StringBuilder builder, string label, int count)
        {
            if (count <= 0) return;
            builder.Append('[').Append(label).Append(" x").Append(count).Append("] ");
        }
    }
}
