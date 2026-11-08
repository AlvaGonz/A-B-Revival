using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using ABRevival.Core.Data;
using Unity.Mathematics;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 gate for the frozen H/F/L marker table and defFly composition. These are the
    /// values that replace the old "every AI part at the hull centre" shortcut.
    /// </summary>
    [TestFixture]
    public class PartMountTableTests
    {
        private const string SampleTable =
            "scale=2\n" +
            "S;Body_X\n" +
            "L;Body_X;0;0\n" +
            "A;Body_X;64;80\n" +
            "H;Body_X;0;0;8;2;24\n" +
            "H;Body_X;1;-12;6;0;2\n" +
            "H;Body_X;2;12;6;1;2\n" +
            "F;Body_X;0;0;20\n" +
            "S;Gun_X\n" +
            "L;Gun_X;0;4\n" +
            "A;Gun_X;16;24\n" +
            "F;Gun_X;0;0;10\n" +
            "S;Wing_L\n" +
            "L;Wing_L;8;0\n" +
            "A;Wing_L;20;8\n" +
            "H;Wing_L;0;-2;0;2;8\n" +
            "S;Engine_X\n" +
            "L;Engine_X;0;-2\n" +
            "A;Engine_X;8;12\n" +
            "T;Engine_X;0;-6\n" +
            "P;0;Body_X;2;1;PartMounts/body.png;Parts/body.png\n" +
            "P;1;Gun_X;2;32;PartMounts/gun.png;Parts/gun.png\n" +
            "P;2;Wing_L;0;1;PartMounts/wing_l.png;Parts/wing_l.png\n" +
            "P;3;Wing_L;1;1;PartMounts/wing_r.png;Parts/wing_r.png\n" +
            "P;4;Engine_X;2;1;PartMounts/engine.png;Parts/engine.png\n";

        private static string FindRepoRoot()
        {
            var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (directory != null &&
                   !Directory.Exists(Path.Combine(directory.FullName, ".git")) &&
                   !File.Exists(Path.Combine(directory.FullName, ".git")))
            {
                directory = directory.Parent;
            }
            return directory?.FullName ?? @"C:\Dev\A&B revival";
        }

        private static string DataPath(string name)
        {
            return Path.Combine(FindRepoRoot(), "A&B-U", "Assets", "Resources", "Data", name);
        }

        private static string BinaryDataPath(string name)
        {
            return Path.Combine(FindRepoRoot(), "src", "assets", "binaryData", name);
        }

        private static PartMountTable ParseCommitted()
        {
            string path = DataPath("PartMounts.txt");
            Assert.IsTrue(File.Exists(path), $"Committed mount table missing: {path}");
            return PartMountTable.Parse(File.ReadAllText(path));
        }

        private static CraftCatalog ParseCatalog()
        {
            return CraftCatalogParser.ParseFromFiles(
                BinaryDataPath("3_all.AllData_defFly.bin"),
                BinaryDataPath("8_all.AllData_defPart.bin"),
                BinaryDataPath("2_all.AllData_defFire.bin"),
                BinaryDataPath("11_all.AllData_defBullet.bin"));
        }

        [Test]
        public void MountTable_ParsesMarkersAndSeparatesMountArtFromThumbnails()
        {
            PartMountTable table = PartMountTable.Parse(SampleTable);

            Assert.AreEqual(2, table.ArtScale);
            Assert.AreEqual(5, table.PartCount);
            Assert.IsTrue(table.TryGetPart(0, out PartMountEntry body));
            Assert.AreEqual("PartMounts/body.png", body.mountFile);
            Assert.AreEqual("Parts/body.png", body.thumbnailFile);
            Assert.AreEqual(2, body.lr);

            Assert.IsTrue(table.TryGetSymbol("Body_X", out PartMountSymbol symbol));
            Assert.AreEqual(3, symbol.holes.Length);
            Assert.AreEqual(1, symbol.firePoints.Length);
            Assert.AreEqual(64f, symbol.size.x);
            Assert.AreEqual(80f, symbol.size.y);
            Assert.IsTrue(symbol.holes[0].Accepts(PartCategory.Gun));
            Assert.IsFalse(symbol.holes[0].Accepts(PartCategory.Wing));
        }

        [Test]
        public void MountTable_ResolvesLinksMirroredSocketsAndMuzzles()
        {
            PartMountTable table = PartMountTable.Parse(SampleTable);

            Assert.IsTrue(table.TryGetLink(2, out float2 leftLink));
            Assert.AreEqual(8f, leftLink.x);
            Assert.AreEqual(0f, leftLink.y);

            Assert.IsTrue(table.TryGetLink(3, out float2 rightLink));
            Assert.AreEqual(-8f, rightLink.x, "lr=1 mirrors the link point.");

            // Wing_L in Body_X hole 1 at (-12, 6): offset = hole - link = (-20, 6).
            Assert.IsTrue(table.TryResolveSocket(0, 1, 2, false, out float2 wingOffset));
            Assert.AreEqual(-20f, wingOffset.x, 0.001f);
            Assert.AreEqual(6f, wingOffset.y, 0.001f);

            // Gun_X link (0, 4) in Body_X hole 0 at (0, 8): offset = (0, 4).
            Assert.IsTrue(table.TryResolveSocket(0, 0, 1, false, out float2 gunOffset));
            Assert.AreEqual(0f, gunOffset.x, 0.001f);
            Assert.AreEqual(4f, gunOffset.y, 0.001f);

            float2 muzzle = table.MuzzleOffset(1, gunOffset);
            Assert.AreEqual(14f, muzzle.y, 0.001f, "F marker adds to the gun origin.");

            float2 exhaust = table.ExhaustPoint(4, new float2(3f, 2f));
            Assert.AreEqual(3f, exhaust.x, 0.001f);
            Assert.AreEqual(-4f, exhaust.y, 0.001f);
        }

        [Test]
        public void MountTable_FallsBackToADerivableLayoutInsteadOfCentring()
        {
            var parent = new PartMountSymbol { mcName = "B", size = new float2(100f, 80f) };
            float2 wing = PartMountTable.FallbackOffset(parent, PartCategory.Wing, 0);
            float2 engine = PartMountTable.FallbackOffset(parent, PartCategory.Engine, 1);
            float2 gun = PartMountTable.FallbackOffset(parent, PartCategory.Gun, 2);

            Assert.Less(wing.x, 0f, "A wing must sit outboard, not at the hull centre.");
            Assert.Less(engine.y, 0f, "An engine must sit aft, not at the hull centre.");
            Assert.Greater(gun.y, 0f, "A gun must sit forward, not at the hull centre.");
        }

        [Test]
        public void MountTable_RejectsMalformedRows()
        {
            Assert.Throws<ArgumentException>(() => PartMountTable.Parse(" "));
            Assert.Throws<FormatException>(() => PartMountTable.Parse("S;A\nP;0;Missing;2;1;m;t\n"));
            Assert.Throws<FormatException>(() => PartMountTable.Parse("S;A\nH;A;0;0;0;2\n"));
            Assert.Throws<FormatException>(() => PartMountTable.Parse("S;A\nP;0;A;2;1;m;t\nP;0;A;2;1;m;t\n"));
        }

        [Test]
        public void CommittedTable_CoversEveryPartAndResolvesItsMarkers()
        {
            PartMountTable table = ParseCommitted();
            List<PartDefinitionRecord> parts = PartCatalogParser.ParseCatalogFromFiles(
                BinaryDataPath("8_all.AllData_defPart.bin"),
                BinaryDataPath("2_all.AllData_defFire.bin"),
                BinaryDataPath("11_all.AllData_defBullet.bin"));

            Assert.AreEqual(271, table.PartCount);
            Assert.AreEqual(222, table.SymbolCount);
            Assert.AreEqual(2, table.ArtScale);

            for (int i = 0; i < parts.Count; i++)
            {
                PartDefinitionRecord part = parts[i];
                Assert.IsTrue(table.TryGetPart(part.partId, out PartMountEntry entry), $"Part {part.partId} missing.");
                Assert.AreEqual(part.mcName, entry.mcName);
                Assert.AreEqual(part.lr, entry.lr);
                Assert.IsTrue(table.TryGetSymbol(entry.mcName, out PartMountSymbol symbol), entry.mcName);

                for (int hole = 0; hole < symbol.holes.Length; hole++)
                    Assert.IsNotNull(symbol.holes[hole], $"{entry.mcName} hole {hole} is missing.");
            }
        }

        [Test]
        public void CraftComposition_ResolvesHostileAndCaptainFromTheirDefFlyTrees()
        {
            PartMountTable table = ParseCommitted();
            CraftCatalog catalog = ParseCatalog();
            Span<CraftPartPlacement> placements = stackalloc CraftPartPlacement[32];

            CraftStatRecord hostile = catalog.Find(0);
            int hostileCount = CraftComposition.Build(hostile, table, placements);
            Assert.AreEqual(6, hostileCount);
            Assert.AreEqual(float2.zero, placements[0].localOffset);
            Assert.Less(placements[2].localOffset.x, 0f, "Left wing must sit to port.");
            Assert.Greater(placements[4].localOffset.x, 0f, "Right wing must sit to starboard.");
            Assert.IsTrue(placements[4].mirrored, "The right wing part is the mirrored lr=1 variant.");
            Assert.AreNotEqual(placements[1].localOffset, float2.zero, "The gun must not share the hull centre.");

            var seen = new HashSet<(float, float)>();
            for (int i = 1; i < hostileCount; i++)
            {
                var key = (placements[i].localOffset.x, placements[i].localOffset.y);
                Assert.IsTrue(seen.Add(key), $"Part {i} was silently centred on another part.");
            }

            CraftStatRecord captain = catalog.Find(76);
            int captainCount = CraftComposition.Build(captain, table, placements);
            Assert.AreEqual(20, captainCount);
            Assert.AreEqual(captain.hullPartId, placements[0].partId);
            Assert.AreEqual(0f, placements[0].localOffset.x);
            Assert.AreEqual(0f, placements[0].localOffset.y);
        }

        [Test]
        public void CraftComposition_PlayerBlueprint_UsesSocketsMirroringAndLayerOrder()
        {
            PartMountTable table = ParseCommitted();
            Span<MountedPartData> parts = stackalloc MountedPartData[5];
            parts[0] = new MountedPartData { partId = 0, category = PartCategory.Body };
            parts[1] = new MountedPartData { partId = 133, category = PartCategory.Engine };
            parts[2] = new MountedPartData { partId = 173, category = PartCategory.Wing };
            parts[3] = new MountedPartData { partId = 222, category = PartCategory.Wing, mirrored = true };
            parts[4] = new MountedPartData { partId = 71, category = PartCategory.Gun };

            Span<CraftPartPlacement> placements = stackalloc CraftPartPlacement[8];
            int count = CraftComposition.BuildPlayer(parts, table, placements);
            Assert.AreEqual(5, count);

            // Engine, hull, wings, gun; the body is the second layer, not the first paint.
            Assert.AreEqual(PartCategory.Engine, placements[0].category);
            Assert.AreEqual(PartCategory.Body, placements[1].category);
            Assert.AreEqual(PartCategory.Wing, placements[2].category);
            Assert.AreEqual(PartCategory.Wing, placements[3].category);
            Assert.AreEqual(PartCategory.Gun, placements[4].category);

            Assert.Less(placements[0].localOffset.y, 0f, "The engine must sit aft.");
            Assert.Greater(placements[4].localOffset.y, 0f, "The gun must sit forward when no free socket remains.");
            Assert.IsFalse(placements[2].mirrored, "The left wing keeps its art orientation.");
            Assert.IsTrue(placements[3].mirrored, "The right wing variant is mirrored.");
        }

        [Test]
        public void FireFormula_MatchesTheFrozenDisplayValues()
        {
            PartMountTable table = ParseCommitted();
            List<PartDefinitionRecord> parts = PartCatalogParser.ParseCatalogFromFiles(
                BinaryDataPath("8_all.AllData_defPart.bin"),
                BinaryDataPath("2_all.AllData_defFire.bin"),
                BinaryDataPath("11_all.AllData_defBullet.bin"));

            PartDefinitionRecord gun1 = parts.Find(part => part.partId == 71);
            PartDefinitionRecord gun43 = parts.Find(part => part.partId == 113);
            PartDefinitionRecord gun51 = parts.Find(part => part.partId == 121);
            Assert.IsNotNull(gun1);
            Assert.IsNotNull(gun43);
            Assert.IsNotNull(gun51);

            Span<float2> muzzles = stackalloc float2[8];
            Assert.AreEqual(1, table.CopyFirePoints(71, muzzles));
            Assert.AreEqual(3, table.CopyFirePoints(113, muzzles));
            Assert.AreEqual(2, table.CopyFirePoints(121, muzzles));

            // Frozen FormShop.f4linkToArray: firePoints * firePowRate * bulletPow / 300000.
            Assert.AreEqual(340f, gun1.CalculateFirePowerRate(), 0.01f);
            Assert.AreEqual(904f, gun1.CalculateBulletPower(), 0.01f);
            Assert.AreEqual(1.0245f, gun1.CalculateDisplayFirePower(1), 0.0001f);
            Assert.AreEqual(30.081f, gun43.CalculateDisplayFirePower(3), 0.001f);
        }

        [Test]
        public void BulletTable_MapsEveryFrozenBulletToArt()
        {
            string path = DataPath("BulletSprites.txt");
            Assert.IsTrue(File.Exists(path), $"Committed bullet table missing: {path}");
            BulletSpriteTable table = BulletSpriteTable.Parse(File.ReadAllText(path));

            Assert.AreEqual(65, table.Count);
            Assert.AreEqual("bullet_normal.png", table.Get(1).symbol);
            Assert.AreEqual("bullet_Blt_1.png", table.Get(32).symbol);
            Assert.AreEqual("bullet_Blt_4.png", table.Get(42).symbol);
            string uvPath = Path.Combine(FindRepoRoot(), "A&B-U", "Assets", "Art", "Atlases", "PartsAtlasUv.txt");
            var atlas = ABRevival.Core.Rendering.SpriteUvTable.Parse(File.ReadAllText(uvPath));
            for (int id = 0; id <= 64; id++)
            {
                BulletSprite sprite = table.Get(id);
                Assert.IsNotNull(sprite.symbol, $"Bullet {id} has no art.");
                Assert.Greater(sprite.width, 0f);
                Assert.Greater(sprite.height, 0f);
                Assert.IsTrue(atlas.TryGetSymbol(sprite.symbol, out _),
                    $"Bullet {id} points at '{sprite.symbol}', which is missing from the parts atlas.");
            }
        }
    }
}
