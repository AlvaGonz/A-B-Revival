using System.Collections.Generic;
using ABRevival.Core.Data;
using ABRevival.Core.Persistence;
using ABRevival.Core.Shop;
using NUnit.Framework;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 coverage for the frozen FormRepair ledger: 70% value, all-or-nothing purse
    /// check, in-place health restore and the UIWin.toNext shop buy-back.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [TestFixture]
    public class RepairStateTests
    {
        private static PlayerSaveData SaveWith(params (int partId, PartCategory category, float maxHealth)[] parts)
        {
            var save = new PlayerSaveData { shop = new ShopSaveData { money = 1000 } };
            var blueprint = new BlueprintSaveData { craftName = "Michael's" };
            for (int i = 0; i < parts.Length; i++)
            {
                blueprint.mountedParts.Add(new MountedPartSaveData
                {
                    partId = parts[i].partId,
                    category = parts[i].category,
                    health = 0f,
                    maxHealth = parts[i].maxHealth
                });
            }

            save.blueprints.Add(blueprint);
            return save;
        }

        private static RepairPartInfo Info(int partId, PartCategory category, int cost)
        {
            return new RepairPartInfo("Part " + partId, category, 0, 10f, 1f, 2f, 0.5f, 0f, cost);
        }

        private static RepairState Create(PlayerSaveData save, params DamagedPart[] damage)
        {
            return RepairState.Create(
                save,
                new List<DamagedPart>(damage),
                partId =>
                {
                    switch (partId)
                    {
                        case 10: return Info(partId, PartCategory.Body, 1000);
                        case 11: return Info(partId, PartCategory.Gun, 333);
                        case 12: return Info(partId, PartCategory.Wing, 500);
                        default: return Info(partId, PartCategory.Engine, 100);
                    }
                });
        }

        [Test]
        public void Create_ComputesSeventyPercentOfCatalogValue()
        {
            PlayerSaveData save = SaveWith(
                (10, PartCategory.Body, 100f),
                (11, PartCategory.Gun, 50f));
            RepairState repair = Create(save,
                new DamagedPart(0, 0, 10, PartCategory.Body),
                new DamagedPart(0, 1, 11, PartCategory.Gun));

            Assert.AreEqual(2, repair.Items.Count);
            Assert.AreEqual(700, repair.Items[0].RepairCost);
            Assert.AreEqual(233, repair.Items[1].RepairCost);
            Assert.AreEqual(1000, repair.Money);
            Assert.IsTrue(repair.HasDamage);
        }

        [Test]
        public void ApplySelected_RestoresHealthAndChargesTheFrozenPrice()
        {
            PlayerSaveData save = SaveWith(
                (10, PartCategory.Body, 100f),
                (11, PartCategory.Gun, 50f));
            RepairState repair = Create(save,
                new DamagedPart(0, 0, 10, PartCategory.Body),
                new DamagedPart(0, 1, 11, PartCategory.Gun));
            repair.SelectAll();

            int repaired = repair.ApplySelected();

            Assert.AreEqual(2, repaired);
            Assert.AreEqual(67, save.shop.money);
            Assert.AreEqual(100f, save.blueprints[0].mountedParts[0].health);
            Assert.AreEqual(50f, save.blueprints[0].mountedParts[1].health);
            Assert.AreEqual(50f, save.blueprints[0].mountedParts[1].maxHealth);
            Assert.AreEqual(0, repair.Items.Count);
            Assert.AreEqual(2, repair.RepairedCount);
        }

        [Test]
        public void ApplySelected_IsAllOrNothingWhenThePurseCannotCoverTheSelection()
        {
            PlayerSaveData save = SaveWith(
                (10, PartCategory.Body, 100f),
                (11, PartCategory.Gun, 50f));
            save.shop.money = 100;
            RepairState repair = Create(save,
                new DamagedPart(0, 0, 10, PartCategory.Body),
                new DamagedPart(0, 1, 11, PartCategory.Gun));
            repair.SelectAll();

            Assert.IsFalse(repair.CanAfford);
            Assert.IsFalse(repair.CanApply);
            Assert.AreEqual(0, repair.ApplySelected());
            Assert.AreEqual(100, save.shop.money);
            Assert.AreEqual(0f, save.blueprints[0].mountedParts[0].health);
        }

        [Test]
        public void SkipRemaining_RemovesPartsAndReturnsThemToShopStock()
        {
            PlayerSaveData save = SaveWith(
                (10, PartCategory.Body, 100f),
                (11, PartCategory.Gun, 50f));
            save.shop.money = 0;
            RepairState repair = Create(save,
                new DamagedPart(0, 0, 10, PartCategory.Body),
                new DamagedPart(0, 1, 11, PartCategory.Gun));

            int removed = repair.SkipRemaining();

            Assert.AreEqual(2, removed);
            Assert.AreEqual(0, save.blueprints[0].mountedParts.Count);
            Assert.AreEqual(2, save.shop.stock.Count);

            int bodyIndex = -1;
            int gunIndex = -1;
            for (int i = 0; i < save.shop.stock.Count; i++)
            {
                if (save.shop.stock[i].partId == 10) bodyIndex = i;
                if (save.shop.stock[i].partId == 11) gunIndex = i;
            }

            Assert.GreaterOrEqual(bodyIndex, 0, "The lost body must return as a finite hull row.");
            Assert.GreaterOrEqual(gunIndex, 0, "The lost gun must return as a module row.");
            Assert.AreEqual(1, save.shop.stock[bodyIndex].quantity);
            Assert.AreEqual(0, save.shop.stock[gunIndex].quantity);
            Assert.AreEqual(0, repair.Items.Count);
        }

        [Test]
        public void SkipRemaining_KeepsRepairedPartsOnTheCraft()
        {
            PlayerSaveData save = SaveWith(
                (10, PartCategory.Body, 100f),
                (11, PartCategory.Gun, 50f));
            RepairState repair = Create(save,
                new DamagedPart(0, 0, 10, PartCategory.Body),
                new DamagedPart(0, 1, 11, PartCategory.Gun));

            repair.Toggle(0);
            repair.ApplySelected();
            repair.SkipRemaining();

            Assert.AreEqual(1, save.blueprints[0].mountedParts.Count);
            Assert.AreEqual(10, save.blueprints[0].mountedParts[0].partId);
            Assert.AreEqual(100f, save.blueprints[0].mountedParts[0].health);
        }

        [Test]
        public void SelectAll_RespectsTheCategoryFilter_AndResetClearsIt()
        {
            PlayerSaveData save = SaveWith(
                (10, PartCategory.Body, 100f),
                (11, PartCategory.Gun, 50f),
                (12, PartCategory.Wing, 80f));
            RepairState repair = Create(save,
                new DamagedPart(0, 0, 10, PartCategory.Body),
                new DamagedPart(0, 1, 11, PartCategory.Gun),
                new DamagedPart(0, 2, 12, PartCategory.Wing));

            int selected = repair.SelectAll(PartCategory.Gun);

            Assert.AreEqual(233, selected);
            Assert.AreEqual(1, repair.SelectedCount);
            Assert.AreEqual(233, repair.SelectedCost);

            repair.Reset();
            Assert.AreEqual(0, repair.SelectedCount);
        }

        [Test]
        public void Create_IgnoresStaleDamageRecords()
        {
            PlayerSaveData save = SaveWith((10, PartCategory.Body, 100f));
            RepairState repair = Create(save,
                new DamagedPart(0, 0, 99, PartCategory.Body),
                new DamagedPart(4, 0, 10, PartCategory.Body),
                new DamagedPart(0, 9, 10, PartCategory.Body));

            Assert.AreEqual(0, repair.Items.Count);
            Assert.IsFalse(repair.HasDamage);
        }
    }
}
