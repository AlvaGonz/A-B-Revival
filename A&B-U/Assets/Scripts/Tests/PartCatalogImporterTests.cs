using System.IO;
using NUnit.Framework;
using ABRevival.Core.Data;

namespace ABRevival.Tests
{
    [TestFixture]
    public class PartCatalogImporterTests
    {
        private const string PartBinPath = "../../../../src/assets/binaryData/8_all.AllData_defPart.bin";
        private const string FireBinPath = "../../../../src/assets/binaryData/2_all.AllData_defFire.bin";
        private const string BulletBinPath = "../../../../src/assets/binaryData/11_all.AllData_defBullet.bin";

        [Test]
        public void PartCatalogParser_ParsesAll271Parts_FromFrozenAS3Data()
        {
            // Resolve relative path to root repo
            string resolvedPartPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, PartBinPath));
            string resolvedFirePath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, FireBinPath));
            string resolvedBulletPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, BulletBinPath));

            Assert.IsTrue(File.Exists(resolvedPartPath), $"Part data file missing: {resolvedPartPath}");
            Assert.IsTrue(File.Exists(resolvedFirePath), $"Fire data file missing: {resolvedFirePath}");
            Assert.IsTrue(File.Exists(resolvedBulletPath), $"Bullet data file missing: {resolvedBulletPath}");

            var records = PartCatalogParser.ParseCatalogFromFiles(resolvedPartPath, resolvedFirePath, resolvedBulletPath);
            Assert.AreEqual(271, records.Count);
        }

        [Test]
        public void PartCatalogParser_ExtractsGunAttributes_AndCalculatesADR0012Range()
        {
            string resolvedPartPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, PartBinPath));
            string resolvedFirePath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, FireBinPath));
            string resolvedBulletPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, BulletBinPath));

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
        }

        [Test]
        public void PartCatalogParser_ExtractsEngineAttributes_ThrustPower()
        {
            string resolvedPartPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, PartBinPath));
            string resolvedFirePath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, FireBinPath));
            string resolvedBulletPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, BulletBinPath));

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
