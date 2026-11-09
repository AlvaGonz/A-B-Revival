using System.Collections.Generic;
using ABRevival.Core.Camera;
using ABRevival.Core.Data;

namespace ABRevival.Core.Rendering
{
    /// <summary>
    /// Which frozen faction palette a projectile uses. The values are presentation-only and
    /// travel on <c>ProjectileData.paletteRole</c>; they never take part in targeting.
    /// </summary>
    public enum ProjectileRole
    {
        Player = 0,
        Hostile = 1,
        Captain = 2
    }

    /// <summary>
    /// The base-game projectile palette. The frozen <c>Render.changeColor(part.hue + 180)</c>
    /// evaluates HSV with the saturation/value that <c>Particle</c> leaves on the shared
    /// <c>RGBHSLHSV</c> singleton (S = 0.8, V = 1), so bullets are fully saturated faction
    /// colours rather than white cells.
    /// <para>
    /// Level data drives the hue: every friendly spawn (player, wingmen and Captain Tom) uses
    /// hue 0, which becomes 180 (cyan) after the +180 bullet shift. Hostiles use hue 180 in the
    /// first missions, which becomes 360/0 (red); later missions use hue -140, which becomes 40
    /// (orange). The port has no per-craft hue yet, so hostiles use the canonical red row. The
    /// Captain keeps the dedicated amber identity the frozen HUD already gives him
    /// (<c>UIBase.COLOR_CAPTAIN_AMBER</c>) so the flagship's fire reads apart from the player's.
    /// </para>
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class ProjectilePalette
    {
        /// <summary>Bullet hue of a friendly craft: level hue 0 + the frozen 180 shift.</summary>
        private const float FriendlyHue = 180f;

        /// <summary>Bullet hue of a red-row hostile craft: level hue 180 + the frozen 180 shift.</summary>
        private const float HostileHue = 360f;

        public const byte TrailAlpha = 110;

        /// <summary>Frozen particle saturation/value for a bullet or trail spark.</summary>
        private const float Saturation = 0.8f;
        private const float Value = 1f;

        /// <summary>Player body colour: HSV(180, 0.8, 1) = cyan.</summary>
        public static readonly uint Player = PackHsv(FriendlyHue);

        /// <summary>
        /// Captain Tom's body colour: the frozen HUD gives him a dedicated amber identity
        /// (<c>UIBase.COLOR_CAPTAIN_AMBER</c> 0xFFCC00), so the allied flagship reads apart from
        /// the player even though the frozen bullet hue rule alone would make him cyan too.
        /// </summary>
        public static readonly uint Captain = SpriteUserValue.PackTint(255, 204, 0, 255);

        /// <summary>Hostile body colour: HSV(0, 0.8, 1) = red.</summary>
        public static readonly uint Hostile = PackHsv(HostileHue);

        public static uint Body(int role)
        {
            switch ((ProjectileRole)role)
            {
                case ProjectileRole.Hostile: return Hostile;
                case ProjectileRole.Captain: return Captain;
                default: return Player;
            }
        }

        /// <summary>Trail keeps the body RGB and fades out, per the ticket's "trails follow the colour".</summary>
        public static uint Trail(int role)
        {
            uint body = Body(role);
            return (body & 0x00FFFFFFu) | ((uint)TrailAlpha << 24);
        }

        /// <summary>
        /// Missile art cannot be recoloured without destroying it, so its faction wash is the
        /// body colour blended 35% into white; the trail still carries the full colour.
        /// </summary>
        public static uint MissileBody(int role)
        {
            uint body = Body(role);
            return SpriteUserValue.PackTint(
                BlendToWhite((byte)(body & 0xFFu)),
                BlendToWhite((byte)((body >> 8) & 0xFFu)),
                BlendToWhite((byte)((body >> 16) & 0xFFu)),
                255);
        }

        /// <summary>Frozen <c>RGBHSLHSV.HSV2RGB</c> for a hue with the bullet S/V.</summary>
        private static uint PackHsv(float hue)
        {
            float h = hue % 360f;
            if (h < 0f) h += 360f;

            int sector = (int)(h / 60f) % 6;
            float fraction = (h / 60f) - (int)(h / 60f);
            float p = Value * (1f - Saturation);
            float q = Value * (1f - (fraction * Saturation));
            float t = Value * (1f - ((1f - fraction) * Saturation));

            float r;
            float g;
            float b;
            switch (sector)
            {
                case 0: r = Value; g = t; b = p; break;
                case 1: r = q; g = Value; b = p; break;
                case 2: r = p; g = Value; b = t; break;
                case 3: r = p; g = q; b = Value; break;
                case 4: r = t; g = p; b = Value; break;
                default: r = Value; g = p; b = q; break;
            }

            return SpriteUserValue.PackTint(
                (byte)((r * 255f) + 0.5f),
                (byte)((g * 255f) + 0.5f),
                (byte)((b * 255f) + 0.5f),
                255);
        }

        private static byte BlendToWhite(byte channel)
        {
            return (byte)(channel + ((255 - channel) * 0.65f));
        }
    }

    /// <summary>
    /// Frozen procedural bullet geometry. <c>F4DataCPX.updateBltByMc</c> gives a
    /// <c>normal</c> bullet <c>hit = (btwh, btwh)</c> (2 px when btwh is absent), and
    /// <c>Render.render</c> draws a Bresenham stroke of <c>hit.width / 2</c> buffer pixels,
    /// which is <c>max(2, btwh)</c> stage pixels after the 2x buffer upscale. The stroke is
    /// one simulation frame long, so fast, low-btwh weapons read as long thin railgun lines.
    /// Sizes are multiplied by <see cref="CameraDistancePresets.DesignScale"/> because the
    /// world art is drawn at 3x stage density (ADR 0021).
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class ProjectileGeometry
    {
        /// <summary>BresenhamLine.lineRect32 clamps the stroke to two buffer pixels.</summary>
        public const float MinimumThickness = 2f;

        /// <summary>One 60 Hz frame, the frozen render step that produced the streak.</summary>
        private const float ReferenceFrameSeconds = 1f / 60f;

        public static float Thickness(float btwh)
        {
            float stagePixels = btwh > MinimumThickness ? btwh : MinimumThickness;
            return stagePixels * CameraDistancePresets.DesignScale;
        }

        /// <summary>Streak length: one frame of travel, never shorter than the square body.</summary>
        public static float Length(float speed, float thickness)
        {
            float length = speed * ReferenceFrameSeconds * CameraDistancePresets.DesignScale;
            return length > thickness ? length : thickness;
        }
    }

    /// <summary>One bullet id resolved to its frozen body: a procedural streak or missile art.</summary>
    public readonly struct ProjectileVisual
    {
        public readonly bool isMissile;
        public readonly float thickness;
        public readonly string symbol;
        public readonly float spriteWidth;
        public readonly float spriteHeight;

        public ProjectileVisual(bool isMissile, float thickness, string symbol, float spriteWidth, float spriteHeight)
        {
            this.isMissile = isMissile;
            this.thickness = thickness;
            this.symbol = symbol;
            this.spriteWidth = spriteWidth;
            this.spriteHeight = spriteHeight;
        }
    }

    /// <summary>
    /// Bullet id -> visual rule table built once at mission start. A bullet is a missile only
    /// when the frozen <c>F4DataBullet.mcName</c> is not <c>normal</c>; every other id is the
    /// procedural square stroke. Ids without a row fall back to the 2 px normal body.
    /// </summary>
    public sealed class ProjectileVisualTable
    {
        public const int MaxBulletId = 255;

        private const string ProceduralSymbol = "normal";

        private static readonly ProjectileVisual Fallback =
            new ProjectileVisual(false, ProjectileGeometry.MinimumThickness, null, 0f, 0f);

        private readonly ProjectileVisual?[] _byId;
        private readonly int _count;

        private ProjectileVisualTable(ProjectileVisual?[] byId, int count)
        {
            _byId = byId;
            _count = count;
        }

        public int Count => _count;

        public ProjectileVisual Get(int bulletId)
        {
            return (uint)bulletId < (uint)_byId.Length && _byId[bulletId].HasValue
                ? _byId[bulletId].Value
                : Fallback;
        }

        /// <summary>Joins the frozen bullet XML (btwh/mcName) with the extracted art table.</summary>
        public static ProjectileVisualTable Build(IReadOnlyList<BulletArtRecord> bullets, BulletSpriteTable sprites)
        {
            var byId = new ProjectileVisual?[MaxBulletId + 1];
            int count = 0;

            if (bullets != null)
            {
                for (int i = 0; i < bullets.Count; i++)
                {
                    BulletArtRecord record = bullets[i];
                    if ((uint)record.bulletId > MaxBulletId) continue;
                    if (!byId[record.bulletId].HasValue) count++;
                    byId[record.bulletId] = Resolve(record, sprites);
                }
            }

            return new ProjectileVisualTable(byId, count);
        }

        private static ProjectileVisual Resolve(BulletArtRecord record, BulletSpriteTable sprites)
        {
            float thickness = record.btwh > 0f ? record.btwh : ProjectileGeometry.MinimumThickness;
            bool missile = !string.IsNullOrEmpty(record.mcName) && record.mcName != ProceduralSymbol;

            if (missile && sprites != null && sprites.TryGet(record.bulletId, out BulletSprite sprite) &&
                !string.IsNullOrEmpty(sprite.symbol))
            {
                return new ProjectileVisual(true, thickness, sprite.symbol, sprite.width, sprite.height);
            }

            return new ProjectileVisual(false, thickness, null, 0f, 0f);
        }
    }
}
