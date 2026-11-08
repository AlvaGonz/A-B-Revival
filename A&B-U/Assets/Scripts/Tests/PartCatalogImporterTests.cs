using System.IO;
using NUnit.Framework;
using ABRevival.Core.Data;

namespace ABRevival.Tests
{
    [TestFixture]
    public class PartCatalogImporterTests
    {
        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, ".git")) && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                dir = dir.Parent;
            }
            return dir?.FullName ?? @"C:\Dev\A&B revival";
        }

        private static string GetBinaryDataPath(string filename)
        {
            return Path.Combine(FindRepoRoot(), "src", "assets", "binaryData", filename);
        }

        [Test]
        public void PartCatalogParser_ParsesAll271Parts_FromFrozenAS3Data()
        {
            string resolvedPartPath = GetBinaryDataPath("8_all.AllData_defPart.bin");
            string resolvedFirePath = GetBinaryDataPath("2_all.AllData_defFire.bin");
            string resolvedBulletPath = GetBinaryDataPath("11_all.AllData_defBullet.bin");

            Assert.IsTrue(File.Exists(resolvedPartPath), $"Part data file missing: {resolvedPartPath}");
            Assert.IsTrue(File.Exists(resolvedFirePath), $"Fire data file missing: {resolvedFirePath}");
            Assert.IsTrue(File.Exists(resolvedBulletPath), $"Bullet data file missing: {resolvedBulletPath}");

            var records = PartCatalogParser.ParseCatalogFromFiles(resolvedPartPath, resolvedFirePath, resolvedBulletPath);
            Assert.AreEqual(271, records.Count);
        }

        [Test]
        public void PartCatalogParser_ExtractsGunAttributes_AndCalculatesADR0012Range()
        {
            string resolvedPartPath = GetBinaryDataPath("8_all.AllData_defPart.bin");
            string resolvedFirePath = GetBinaryDataPath("2_all.AllData_defFire.bin");
            string resolvedBulletPath = GetBinaryDataPath("11_all.AllData_defBullet.bin");

            var records = PartCatalogParser.ParseCatalogFromFiles(resolvedPartPath, resolvedFirePath, resolvedBulletPath);

            // G.25.X1.01 has index 71, fire 1, bullet 1
            // fire 1: spdS = 10, accS = 0
            // bullet 1: dmg = 13, life = 40
            var gun = records.Find(p => p.partName == "G.25.X1.01");
            Assert.IsNotNull(gun);
            Assert.AreEqual(PartCategory.Gun, gun.category);
            Assert.AreEqual(17f, gun.mass, 0.001f);
            Assert.AreEqual(425f, gun.health, 0.001f);
            Assert.AreEqual(10f, gun.projectileVelocity, 0.001f);
            Assert.AreEqual(0f, gun.burstAcceleration, 0.001f);
            Assert.AreEqual(40f, gun.bulletLifetime, 0.001f);
            Assert.AreEqual(13f, gun.bulletDamage, 0.001f);

            // ADR 0012 Range: d = v0 * t + 0.5 * a * t^2 = 10 * 40 + 0 = 400
            Assert.AreEqual(400f, gun.CalculateEffectiveRange(), 0.001f);

            // F4DataOrg.makeCost: the shop must not sell every part for free.
            Assert.AreEqual(2055, gun.cost);
        }

        [Test]
        public void PartCatalogParser_AssignsNonZeroLegacyCosts_ToEveryPart()
        {
            var records = PartCatalogParser.ParseCatalogFromFiles(
                GetBinaryDataPath("8_all.AllData_defPart.bin"),
                GetBinaryDataPath("2_all.AllData_defFire.bin"),
                GetBinaryDataPath("11_all.AllData_defBullet.bin"));

            Assert.AreEqual(271, records.Count);
            for (int i = 0; i < records.Count; i++)
            {
                Assert.Greater(records[i].cost, 0, $"{records[i].partName} must have a legacy cost.");
            }
        }

        [Test]
        public void PartCatalogParser_ExtractsEngineAttributes_ThrustPower()
        {
            string resolvedPartPath = GetBinaryDataPath("8_all.AllData_defPart.bin");
            string resolvedFirePath = GetBinaryDataPath("2_all.AllData_defFire.bin");
            string resolvedBulletPath = GetBinaryDataPath("11_all.AllData_defBullet.bin");

            var records = PartCatalogParser.ParseCatalogFromFiles(resolvedPartPath, resolvedFirePath, resolvedBulletPath);

            // E.Low.01 has powFly = 16, mass = 20, maxhp = 600
            var engine = records.Find(p => p.partName == "E.Low.01");
            Assert.IsNotNull(engine);
            Assert.AreEqual(PartCategory.Engine, engine.category);
            Assert.AreEqual(20f, engine.mass, 0.001f);
            Assert.AreEqual(600f, engine.health, 0.001f);
            Assert.AreEqual(16f, engine.thrust, 0.001f);
        }
    }
}
