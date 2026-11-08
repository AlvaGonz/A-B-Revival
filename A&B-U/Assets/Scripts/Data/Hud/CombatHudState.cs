using System;
using ABRevival.Core.Camera;
using ABRevival.Core.Data;
using ABRevival.Core.Simulation;

namespace ABRevival.Core.Hud
{
    /// <summary>
    /// Pure combat HUD state established by ADR 0004: hull health, cumulative
    /// battle-damage telemetry, Captain tactical mode code, and the ephemeral
    /// camera-distance badge. No Unity API is used here so the Tier 1 gate can
    /// exercise every transition.
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
        private CameraDistancePreset _cameraPreset = CameraDistancePreset.Short;
        private float _cameraBadgeRemaining;

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

        public CameraDistancePreset CameraPreset => _cameraPreset;
        public bool CameraBadgeVisible => _cameraBadgeRemaining > 0f;

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
                _cameraPreset,
                CameraBadgeVisible,
                CameraBadgeAlpha);
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
        public readonly CameraDistancePreset CameraPreset;
        public readonly bool CameraBadgeVisible;
        public readonly float CameraBadgeAlpha;
        public readonly string CameraBadgeText;

        internal CombatHudSnapshot(
            float hullCurrent,
            float hullMax,
            int lostGuns,
            int lostWings,
            int lostEngines,
            int lostModules,
            bool captainPresent,
            CaptainTacticalMode captainMode,
            CameraDistancePreset cameraPreset,
            bool cameraBadgeVisible,
            float cameraBadgeAlpha)
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
            CameraPreset = cameraPreset;
            CameraBadgeVisible = cameraBadgeVisible;
            CameraBadgeAlpha = cameraBadgeAlpha;
            CameraBadgeText = CameraDistancePresets.BadgeText(cameraPreset);
        }

        public bool HasLostParts => LostGuns + LostWings + LostEngines + LostModules > 0;
        public int TotalLostParts => LostGuns + LostWings + LostEngines + LostModules;
    }

    /// <summary>Two-letter tactical mode codes shown on the Captain portrait (ADR 0006).</summary>
    public static class TacticalModeCodes
    {
        private static readonly string[] Codes = { "AN", "ES", "EV", "SD", "FS", "SH" };

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
        public static string FormatHull(float current, float max)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0} / {1:0}", current, max);
        }

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
