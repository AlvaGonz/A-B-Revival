using System;

namespace ABRevival.Core.Camera
{
    /// <summary>
    /// Combat camera distance tiers from ADR 0005, rebased onto the ticket #179 single
    /// 3x design scale: 640x480 frozen stage -> 1920x1080 design pixels. One Unity unit
    /// stays one frozen stage pixel (ADR 0017); the 2x mount art is displayed at 1.5x
    /// so world art and HUD art share the same on-screen 3x density at 1080p.
    /// </summary>
    public enum CameraDistancePreset
    {
        Short = 0,
        Medium = 1,
        Far = 2
    }

    /// <summary>
    /// Pure mapping between the combat viewport and the scaled battlefield. The HUD is
    /// screen-space UI Toolkit and uses the same 1920x1080 design canvas; only the world
    /// viewport and the mouse-aim mapping scale with the distance preset.
    /// </summary>
    public static class CameraDistancePresets
    {
        /// <summary>Frozen 640x360 16:9 safe area multiplied by the ticket #179 design scale.</summary>
        public const float DesignScale = 3f;

        public const float DesignWidth = 640f * DesignScale;
        public const float DesignHeight = 360f * DesignScale;

        /// <summary>Draw scale for the 2x mount atlas so world art is 3x, matching the HUD.</summary>
        public const float MountArtScale = DesignScale / 2f;

        public const int Count = 3;

        private static readonly float[] ViewScales = { 1.0f, 1.3f, 1.5f };

        private static readonly string[] BadgeTexts =
        {
            "[ CAM: SHORT (1.0x) ]",
            "[ CAM: MEDIUM (1.3x) ]",
            "[ CAM: FAR (1.5x) ]"
        };

        public static CameraDistancePreset Next(CameraDistancePreset preset)
        {
            return (CameraDistancePreset)((ToIndex(preset) + 1) % Count);
        }

        public static float ViewScale(CameraDistancePreset preset)
        {
            return ViewScales[ToIndex(preset)];
        }

        public static float ViewportWidth(CameraDistancePreset preset)
        {
            return DesignWidth * ViewScale(preset);
        }

        public static float ViewportHeight(CameraDistancePreset preset)
        {
            return DesignHeight * ViewScale(preset);
        }

        /// <summary>Ephemeral HUD badge text, matching the frozen baseline wording.</summary>
        public static string BadgeText(CameraDistancePreset preset)
        {
            return BadgeTexts[ToIndex(preset)];
        }

        /// <summary>
        /// Display pixels -> scaled viewport pixels (ADR 0005 inverse scaling).
        /// The HUD itself always uses <see cref="DesignWidth"/> x <see cref="DesignHeight"/>.
        /// </summary>
        public static ViewportPoint ScreenToViewport(ViewportPoint screen, ViewportPoint screenSize, CameraDistancePreset preset)
        {
            if (screenSize.X <= 0f || screenSize.Y <= 0f)
                throw new ArgumentOutOfRangeException(nameof(screenSize), "Screen size must be positive.");

            return new ViewportPoint(
                screen.X / screenSize.X * ViewportWidth(preset),
                screen.Y / screenSize.Y * ViewportHeight(preset));
        }

        /// <summary>Scaled viewport pixels -> display pixels.</summary>
        public static ViewportPoint ViewportToScreen(ViewportPoint viewport, ViewportPoint screenSize, CameraDistancePreset preset)
        {
            if (screenSize.X <= 0f || screenSize.Y <= 0f)
                throw new ArgumentOutOfRangeException(nameof(screenSize), "Screen size must be positive.");

            return new ViewportPoint(
                viewport.X / ViewportWidth(preset) * screenSize.X,
                viewport.Y / ViewportHeight(preset) * screenSize.Y);
        }

        /// <summary>Viewport pixels -> world offset from the camera centre.</summary>
        public static ViewportPoint ViewportToWorldCentered(ViewportPoint viewport, CameraDistancePreset preset)
        {
            return new ViewportPoint(
                viewport.X - ViewportWidth(preset) * 0.5f,
                viewport.Y - ViewportHeight(preset) * 0.5f);
        }

        /// <summary>World offset from the camera centre -> viewport pixels.</summary>
        public static ViewportPoint WorldToViewportCentered(ViewportPoint world, CameraDistancePreset preset)
        {
            return new ViewportPoint(
                world.X + ViewportWidth(preset) * 0.5f,
                world.Y + ViewportHeight(preset) * 0.5f);
        }

        /// <summary>Mouse display position -> world aim offset from the camera centre.</summary>
        public static ViewportPoint ScreenToWorldCentered(ViewportPoint screen, ViewportPoint screenSize, CameraDistancePreset preset)
        {
            return ViewportToWorldCentered(ScreenToViewport(screen, screenSize, preset), preset);
        }

        private static int ToIndex(CameraDistancePreset preset)
        {
            int index = (int)preset;
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(preset), preset, "Unknown camera distance preset.");
            return index;
        }
    }

    public readonly struct ViewportPoint
    {
        public readonly float X;
        public readonly float Y;

        public ViewportPoint(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
