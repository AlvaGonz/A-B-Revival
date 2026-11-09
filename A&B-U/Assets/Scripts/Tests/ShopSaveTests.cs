using System.IO;
using NUnit.Framework;
using Unity.Mathematics;
using ABRevival.Core.Data;
using ABRevival.Core.Persistence;
using ABRevival.Core.Shop;

namespace ABRevival.Tests
{
    [TestFixture]
    public class ShopSaveTests
    {
        private static ShopCatalog CreateCatalog()
        {
            return new ShopCatalog(new[]
            {
                new ShopPart(0, "B.Small.01", PartCategory.Body, 500),
                new ShopPart(71, "G.25.X1.01", PartCategory.Gun, 120),
                new ShopPart(133, "E.Low.01", PartCategory.Engine, 150),
                new ShopPart(173, "W.Cls.F.01.L", PartCategory.Wing, 80)
            });
        }

        [Test]
        public void Save_RoundTrip_PreservesShopOwnedPartsAndBlueprintSelection()
        {
            ShopCatalog catalog = CreateCatalog();
            var shop = new ShopState(catalog, 1000);
            shop.Unlock(0, 1);
            shop.Unlock(71);
            shop.Unlock(173);
            shop.Buy(71, 2);
            shop.Buy(0, 1);

            var blueprint = new BlueprintSaveData
            {
                craftName = "Player Craft",
                hullName = "B.Small.01",
                hullCategory = HullCategory.Small,
                hullMass = 100f,
                hullHealth = 1300f,
                isGrandfathered = true
            };
            blueprint.mountedParts.Add(MountedPartSaveData.FromMountedPart(new MountedPartData
            {
                partId = 71,
                category = PartCategory.Gun,
                health = 400f,
                maxHealth = 425f,
                mass = 17f,
                thrust = 0f,
                energyDraw = 10f,
                localOffset = new float2(1.5f, -2.5f),
                localRotation = 0.75f
            }));

            var payload = new PlayerSaveData
            {
                shop = shop.CreateSave(),
                cameraPreset = ABRevival.Core.Camera.CameraDistancePreset.Far,
                controlMode = ABRevival.Core.Simulation.PlayerControlMode.Mouse
            };
            payload.blueprints.Add(blueprint);

            PlayerSaveData loaded = PlayerSaveCodec.FromJson(PlayerSaveCodec.ToJson(payload));
            Assert.AreEqual(ABRevival.Core.Camera.CameraDistancePreset.Far, loaded.cameraPreset,
                "The ADR 0005 camera preset must round-trip through the single save payload.");
            Assert.AreEqual(ABRevival.Core.Simulation.PlayerControlMode.Mouse, loaded.controlMode,
                "The #181 Options control mode must round-trip through the single save payload.");
            ShopState restored = ShopState.FromSave(catalog, loaded.shop);

            Assert.AreEqual(260, restored.Money);
            Assert.AreEqual(2, restored.OwnedCount(71));
            Assert.AreEqual(1, restored.OwnedCount(0));
            Assert.IsFalse(restored.IsUnlocked(0));
            Assert.IsTrue(restored.IsUnlocked(71));
            Assert.AreEqual(1, loaded.blueprints.Count);

            BlueprintSaveData savedBlueprint = loaded.blueprints[0];
            Assert.AreEqual("Player Craft", savedBlueprint.craftName);
            Assert.AreEqual("B.Small.01", savedBlueprint.hullName);
            Assert.AreEqual(HullCategory.Small, savedBlueprint.hullCategory);
            Assert.AreEqual(100f, savedBlueprint.hullMass);
            Assert.AreEqual(1300f, savedBlueprint.hullHealth);
            Assert.IsTrue(savedBlueprint.isGrandfathered);

            var parts = BlueprintSaveMapper.ToMountedParts(savedBlueprint);
            Assert.AreEqual(1, parts.Count);
            Assert.AreEqual(71, parts[0].partId);
            Assert.AreEqual(PartCategory.Gun, parts[0].category);
            Assert.AreEqual(400f, parts[0].health);
            Assert.AreEqual(425f, parts[0].maxHealth);
            Assert.AreEqual(17f, parts[0].mass);
            Assert.AreEqual(10f, parts[0].energyDraw);
            Assert.AreEqual(1.5f, parts[0].localOffset.x);
            Assert.AreEqual(-2.5f, parts[0].localOffset.y);
            Assert.AreEqual(0.75f, parts[0].localRotation);
        }

        [Test]
        public void Save_ToJson_IsDeterministic()
        {
            ShopCatalog catalog = CreateCatalog();
            var shop = new ShopState(catalog, 321);
            shop.Unlock(71);
            shop.Unlock(133);
            shop.Buy(71, 1);
            shop.Buy(133, 1);

            var payload = new PlayerSaveData { shop = shop.CreateSave() };

            string first = PlayerSaveCodec.ToJson(payload);
            string second = PlayerSaveCodec.ToJson(payload);

            Assert.AreEqual(first, second);
            Assert.AreEqual(first, PlayerSaveCodec.ToJson(PlayerSaveCodec.FromJson(first)));
        }

        [Test]
        public void Save_FileRoundTrip_LoadsSamePayload()
        {
            var payload = new PlayerSaveData
            {
                shop = new ShopSaveData
                {
                    money = 42,
                    ownedParts = { 71, 71, 133 }
                }
            };
            payload.blueprints.Add(new BlueprintSaveData
            {
                craftName = "Round Trip",
                hullName = "B.Small.01",
                hullMass = 100f,
                hullHealth = 1300f
            });

            string path = Path.Combine(Path.GetTempPath(), "ab-revival-save-test.json");
            try
            {
                PlayerSaveCodec.SaveToFile(path, payload);
                PlayerSaveData loaded = PlayerSaveCodec.LoadFromFile(path);

                Assert.AreEqual(42, loaded.shop.money);
                Assert.AreEqual(3, loaded.shop.ownedParts.Count);
                Assert.AreEqual("Round Trip", loaded.blueprints[0].craftName);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void Save_ReplacesTheExistingFileAtomically_AndLeavesNoTempFile()
        {
            string path = Path.Combine(Path.GetTempPath(), "ab-revival-atomic-save-test.json");
            try
            {
                PlayerSaveCodec.SaveToFile(path, new PlayerSaveData { storyIndex = 3 });
                PlayerSaveCodec.SaveToFile(path, new PlayerSaveData { storyIndex = 7 });

                Assert.AreEqual(7, PlayerSaveCodec.LoadFromFile(path).storyIndex);
                Assert.IsFalse(File.Exists(path + ".tmp"), "The staging file must be consumed by the swap.");
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
            }
        }

        [Test]
        public void Save_InvalidCameraPreset_FallsBackToShort()
        {
            var payload = new PlayerSaveData
            {
                cameraPreset = (ABRevival.Core.Camera.CameraDistancePreset)99,
                controlMode = (ABRevival.Core.Simulation.PlayerControlMode)99
            };
            PlayerSaveData loaded = PlayerSaveCodec.FromJson(PlayerSaveCodec.ToJson(payload));

            Assert.AreEqual(ABRevival.Core.Camera.CameraDistancePreset.Short, loaded.cameraPreset);
            Assert.AreEqual(ABRevival.Core.Simulation.PlayerControlMode.Keyboard, loaded.controlMode,
                "Keyboard must be the default battle control mode.");
        }

        [Test]
        public void Save_LoadWithGiftTable_BackfillsHistoricalUnlocks()
        {
            ShopCatalog catalog = CreateCatalog();
            var shop = new ShopState(catalog);
            var save = new ShopSaveData { money = 10 };
            save.stock.Add(new ShopStockSave { partId = 71, quantity = 0 });

            shop.LoadFrom(save, new[] { new[] { 0, 71, 133 } }, 0);

            Assert.AreEqual(10, shop.Money);
            Assert.IsTrue(shop.IsUnlocked(71));
            Assert.AreEqual(1, shop.GetBodyStock(0));
            Assert.IsTrue(shop.IsUnlocked(133));
        }

        [Test]
        public void Save_RejectsUnsupportedVersion()
        {
            var payload = new PlayerSaveData { version = 99 };
            string json = PlayerSaveCodec.ToJson(payload);

            Assert.Throws<InvalidDataException>(() => PlayerSaveCodec.FromJson(json));
        }
    }
}
