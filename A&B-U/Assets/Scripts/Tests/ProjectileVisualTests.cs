using System.IO;
using ABRevival.Core.Camera;
using ABRevival.Core.Data;
using ABRevival.Core.Rendering;
using NUnit.Framework;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 gate for the frozen projectile visuals (ticket #184): the procedural square body
    /// grows with <c>F4DataBullet.btwh</c>, fast low-calibre guns read as long thin lines, only
    /// non-<c>normal</c> bullets use the extracted missile art, and the palette comes from the
    /// frozen hue rules rather than synthetic white cells.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class ProjectileVisualTests
    {
        private const int WeakPlayerBullet = 1;     // G.25, btwh 1
        private const int MediumHostileBullet = 14; // G.30, btwh 3
        private const int HeavyBullet = 26;         // G.70, btwh 7.5
        private const int RailgunBullet = 58;       // G.L, btwh 6, spdS 80
        private const int MissileBullet = 32;       // G.R, mcName Blt_1

        [Test]
        public void BulletCatalog_CarriesTheFrozenBodyWidth()
        {
            var bullets = BulletCatalog.Parse(File.ReadAllText(DataPath("11_all.AllData_defBullet.bytes")));

            BulletArtRecord weak = Find(bullets, WeakPlayerBullet);
            BulletArtRecord heavy = Find(bullets, HeavyBullet);
            BulletArtRecord missile = Find(bullets, MissileBullet);

            Assert.AreEqual(1f, weak.btwh, 0.001f);
            Assert.AreEqual("normal", weak.mcName);
            Assert.AreEqual(7.5f, heavy.btwh, 0.001f);
            Assert.AreEqual("Blt_1", missile.mcName);
            Assert.AreEqual(7.5f, missile.btwh, 0.001f);
        }

        [Test]
        public void VisualTable_MapsFamiliesAndKeepsMissilesOnExtractedArt()
        {
            ProjectileVisualTable table = LoadVisualTable();

            Assert.AreEqual(65, table.Count);

            ProjectileVisual weak = table.Get(WeakPlayerBullet);
            ProjectileVisual medium = table.Get(MediumHostileBullet);
            ProjectileVisual heavy = table.Get(HeavyBullet);
            ProjectileVisual railgun = table.Get(RailgunBullet);
            ProjectileVisual missile = table.Get(MissileBullet);

            Assert.IsFalse(weak.isMissile);
            Assert.IsFalse(medium.isMissile);
            Assert.IsFalse(heavy.isMissile);
            Assert.IsFalse(railgun.isMissile);

            Assert.AreEqual(1f, weak.thickness, 0.001f);
            Assert.AreEqual(3f, medium.thickness, 0.001f);
            Assert.AreEqual(7.5f, heavy.thickness, 0.001f);
            Assert.AreEqual(6f, railgun.thickness, 0.001f);

            Assert.IsTrue(missile.isMissile, "Blt_1 bullets must use the extracted missile art.");
            Assert.AreEqual("bullet_Blt_1.png", missile.symbol);
            Assert.AreEqual(72f, missile.spriteWidth, 0.001f);
            Assert.AreEqual(75f, missile.spriteHeight, 0.001f);

            // Unknown ids never invent art; they fall back to the frozen 2 px normal body.
            ProjectileVisual unknown = table.Get(999);
            Assert.IsFalse(unknown.isMissile);
            Assert.AreEqual(ProjectileGeometry.MinimumThickness, unknown.thickness, 0.001f);
        }

        [Test]
        public void Geometry_GrowsWithWeaponStrengthAndStretchesFastGunsIntoLines()
        {
            float weakThickness = ProjectileGeometry.Thickness(1f);
            float mediumThickness = ProjectileGeometry.Thickness(3f);
            float heavyThickness = ProjectileGeometry.Thickness(7.5f);
            float railgunThickness = ProjectileGeometry.Thickness(6f);

            Assert.AreEqual(2f * CameraDistancePresets.DesignScale, weakThickness, 0.001f,
                "BresenhamLine clamps strokes to two stage pixels before the 3x design scale.");
            Assert.Greater(mediumThickness, weakThickness);
            Assert.Greater(heavyThickness, mediumThickness);

            float weakLength = ProjectileGeometry.Length(360f, weakThickness);
            float heavyLength = ProjectileGeometry.Length(540f, heavyThickness);
            float railgunLength = ProjectileGeometry.Length(2520f, railgunThickness);

            Assert.Greater(heavyLength, weakLength, "Heavier rounds must read larger than weak ones.");
            Assert.Greater(railgunLength, railgunThickness * 6f,
                "A G.L round must be a long thin line, not a dot.");
            Assert.Less(railgunThickness, heavyThickness,
                "The railgun is the thin-line family; G.70 is the chunky one.");
        }

        [Test]
        public void Palette_MatchesTheFrozenFactionHues()
        {
            Assert.AreEqual(SpriteUserValue.PackTint(51, 255, 255, 255), ProjectilePalette.Player);
            Assert.AreEqual(SpriteUserValue.PackTint(255, 51, 51, 255), ProjectilePalette.Hostile);
            Assert.AreEqual(SpriteUserValue.PackTint(255, 204, 0, 255), ProjectilePalette.Captain,
                "The Captain keeps the frozen HUD's COLOR_CAPTAIN_AMBER identity.");
            Assert.AreNotEqual(ProjectilePalette.Player, ProjectilePalette.Captain);

            uint trail = ProjectilePalette.Trail((int)ProjectileRole.Hostile);
            Assert.AreEqual(ProjectilePalette.Hostile & 0x00FFFFFFu, trail & 0x00FFFFFFu,
                "The trail must inherit the body colour.");
            Assert.AreEqual(ProjectilePalette.TrailAlpha, (byte)(trail >> 24));

            uint missileBody = ProjectilePalette.MissileBody((int)ProjectileRole.Player);
            Assert.Greater((byte)(missileBody & 0xFFu), 100, "Missile art keeps a bright faction wash.");
            Assert.AreNotEqual(ProjectilePalette.Player, missileBody);
        }

        private static ProjectileVisualTable LoadVisualTable()
        {
            var bullets = BulletCatalog.Parse(File.ReadAllText(DataPath("11_all.AllData_defBullet.bytes")));
            BulletSpriteTable sprites = BulletSpriteTable.Parse(File.ReadAllText(DataPath("BulletSprites.txt")));
            return ProjectileVisualTable.Build(bullets, sprites);
        }

        private static BulletArtRecord Find(System.Collections.Generic.List<BulletArtRecord> bullets, int id)
        {
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                if (bullets[i].bulletId == id) return bullets[i];
            }

            Assert.Fail($"Bullet {id} is missing from the frozen catalog.");
            return null;
        }

        private static string DataPath(string name)
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null &&
                   !Directory.Exists(Path.Combine(directory.FullName, ".git")) &&
                   !File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                directory = directory.Parent;
            }

            string root = directory?.FullName ?? @"C:\Dev\A&B revival";
            return Path.Combine(root, "A&B-U", "Assets", "Resources", "Data", name);
        }
    }
}
