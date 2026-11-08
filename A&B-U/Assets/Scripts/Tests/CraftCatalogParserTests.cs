using System.IO;
using NUnit.Framework;
using ABRevival.Core.Data;
using ABRevival.Core.Simulation;

namespace ABRevival.Tests
{
    /// <summary>
    /// Pins the frozen defFly -> PartDefinition aggregation used to replace MissionRunner's
    /// placeholder 1000 HP frames with real craft stats.
    /// </summary>
    [TestFixture]
    public class CraftCatalogParserTests
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

        private static CraftCatalog ParseCatalog()
        {
            return CraftCatalogParser.ParseFromFiles(
                GetBinaryDataPath("3_all.AllData_defFly.bin"),
                GetBinaryDataPath("8_all.AllData_defPart.bin"),
                GetBinaryDataPath("2_all.AllData_defFire.bin"),
                GetBinaryDataPath("11_all.AllData_defBullet.bin"));
        }

        [Test]
        public void CraftCatalog_ParsesAll79FrozenCraftDefinitions()
        {
            CraftCatalog catalog = ParseCatalog();
            Assert.AreEqual(79, catalog.Count);
        }

        [Test]
        public void CraftCatalog_PinsStoryPlayerCraft_Wangs()
        {
            CraftCatalog catalog = ParseCatalog();

            // Story 0 player act: def = true, index = 77.
            CraftStatRecord craft = catalog.Find(77);
            Assert.IsNotNull(craft);
            Assert.AreEqual("wangs", craft.name);
            Assert.AreEqual(12, craft.hullPartId);
            Assert.AreEqual("B.Medium.06", craft.hullPartName);
            Assert.AreEqual(27060f, craft.health, 0.001f);
            Assert.AreEqual(803f, craft.mass, 0.001f);
            Assert.AreEqual(24.2777f, craft.thrust, 0.01f);
            Assert.AreEqual(27.8331f, craft.turn, 0.01f);
            Assert.AreEqual(1.7488f, craft.turnRate, 0.001f);

            CollectionAssert.AreEqual(
                new[] { 84, 117, 104, 84, 117, 104 },
                System.Array.ConvertAll(craft.weaponMounts, mount => mount.partId));
        }

        [Test]
        public void CraftCatalog_PinsCaptainAndHostileCraft_AreNotIdentical()
        {
            CraftCatalog catalog = ParseCatalog();

            // Story 0 Captain: defIndex 76 ("ship").
            CraftStatRecord captain = catalog.Find(76);
            Assert.IsNotNull(captain);
            Assert.AreEqual(58, captain.hullPartId);
            Assert.AreEqual("B.Mega.01", captain.hullPartName);
            Assert.AreEqual(54250f, captain.health, 0.001f);
            Assert.AreEqual(1652f, captain.mass, 0.001f);

            // Story 0 hostile wave: defIndex 0.
            CraftStatRecord hostile = catalog.Find(0);
            Assert.IsNotNull(hostile);
            Assert.AreEqual(0, hostile.hullPartId);
            Assert.AreEqual(5525f, hostile.health, 0.001f);
            Assert.AreEqual(197f, hostile.mass, 0.001f);
            Assert.AreEqual(1, hostile.weaponMounts.Length);
            Assert.AreEqual(71, hostile.weaponMounts[0].partId);

            Assert.AreNotEqual(captain.health, hostile.health);
        }

        [Test]
        public void MissionRunner_ResolvesSpawnDefIndex_ToCraftStats()
        {
            var level = new LevelRecord { storyKey = 1 };
            level.initialActs.Add(new LevelAct
            {
                name = "add",
                kind = LevelActKind.Add,
                spawn = new LevelSpawn
                {
                    kind = LevelSpawnKind.Add,
                    pilot = "Pilot XA",
                    pilots = new[] { "Pilot XA" },
                    friendly = false,
                    defIndex = 0,
                    x = 100f,
                    y = 200f
                }
            });

            using var simulation = new CombatSimulation();
            simulation.Init(maxShips: 4, maxProjectiles: 8);

            var runner = new MissionRunner(level, simulation, ParseCatalog());
            runner.Start();

            ShipEntity ship = simulation.GetShip(0);
            Assert.AreEqual(5525f, ship.maxHealth, 0.001f);
            Assert.AreEqual(5525f, ship.health, 0.001f);
            Assert.AreEqual(197f, ship.mass, 0.001f);
            Assert.AreEqual(18.7817f, ship.thrustPower / ship.mass, 0.01f);
            Assert.AreEqual(1.7223f, ship.turnSpeed, 0.001f);
            Assert.AreEqual((int)CaptainTacticalMode.SeekAndDestroy, ship.tacticalMode);
            Assert.IsTrue(runner.TryGetCraftStats(0, out CraftStatRecord craft));
            Assert.AreEqual("B.Small.01", craft.hullPartName);
        }
    }
}
