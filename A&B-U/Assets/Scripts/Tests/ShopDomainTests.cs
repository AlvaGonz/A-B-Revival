using NUnit.Framework;
using ABRevival.Core.Data;
using ABRevival.Core.Persistence;
using ABRevival.Core.Shop;

namespace ABRevival.Tests
{
    [TestFixture]
    public class ShopDomainTests
    {
        private static ShopCatalog CreateCatalog()
        {
            return new ShopCatalog(new[]
            {
                new ShopPart(0, "B.Small.01", PartCategory.Body, 500),
                new ShopPart(1, "B.Small.02", PartCategory.Body, 600),
                new ShopPart(2, "B.Med.01", PartCategory.Body, 800),
                new ShopPart(71, "G.25.X1.01", PartCategory.Gun, 120),
                new ShopPart(72, "G.25.X1.02", PartCategory.Gun, 140),
                new ShopPart(133, "E.Low.01", PartCategory.Engine, 150),
                new ShopPart(173, "W.Cls.F.01.L", PartCategory.Wing, 80),
                new ShopPart(222, "W.Cls.F.01.R", PartCategory.Wing, 80)
            });
        }

        [Test]
        public void Buy_InfiniteStockPart_KeepsShopRowAndClonesOwnedCopies()
        {
            var shop = new ShopState(CreateCatalog(), 500);
            shop.Unlock(71);

            ShopResult result = shop.Buy(71, 3);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(360, result.Amount);
            Assert.AreEqual(140, shop.Money);
            Assert.AreEqual(3, shop.OwnedCount(71));
            Assert.IsTrue(shop.IsUnlocked(71));
            Assert.AreEqual(0, shop.GetBodyStock(71));
        }

        [Test]
        public void Buy_FiniteBody_ConsumesStockAndSellRestoresIt()
        {
            var shop = new ShopState(CreateCatalog(), 500);
            shop.Unlock(0, 1);

            ShopResult bought = shop.Buy(0, 1);

            Assert.IsTrue(bought.Success);
            Assert.AreEqual(500, bought.Amount);
            Assert.AreEqual(0, shop.Money);
            Assert.AreEqual(1, shop.OwnedCount(0));
            Assert.IsFalse(shop.IsUnlocked(0));

            ShopResult secondBuy = shop.Buy(0, 1);
            Assert.AreEqual(ShopError.OutOfStock, secondBuy.Error);
            Assert.AreEqual(0, shop.Money);

            ShopResult sold = shop.Sell(0);

            Assert.IsTrue(sold.Success);
            Assert.AreEqual(350, sold.Amount);
            Assert.AreEqual(350, shop.Money);
            Assert.AreEqual(0, shop.OwnedCount(0));
            Assert.AreEqual(1, shop.GetBodyStock(0));
        }

        [Test]
        public void Buy_BodyQuantityBeyondStock_IsRejectedWithoutMutation()
        {
            var shop = new ShopState(CreateCatalog(), 2000);
            shop.Unlock(0, 1);

            ShopResult result = shop.Buy(0, 2);

            Assert.AreEqual(ShopError.OutOfStock, result.Error);
            Assert.AreEqual(2000, shop.Money);
            Assert.AreEqual(0, shop.OwnedCount(0));
            Assert.AreEqual(1, shop.GetBodyStock(0));
        }

        [Test]
        public void Buy_InsufficientFunds_DoesNotMutateInventory()
        {
            var shop = new ShopState(CreateCatalog(), 100);
            shop.Unlock(71);

            ShopResult result = shop.Buy(71, 1);

            Assert.AreEqual(ShopError.InsufficientFunds, result.Error);
            Assert.AreEqual(100, shop.Money);
            Assert.AreEqual(0, shop.OwnedCount(71));
            Assert.IsTrue(shop.IsUnlocked(71));
        }

        [Test]
        public void Sell_InfiniteStockPart_CreditsSellRateAndKeepsShopRow()
        {
            var shop = new ShopState(CreateCatalog(), 500);
            shop.Unlock(71);
            shop.Buy(71, 2);

            ShopResult result = shop.Sell(71);

            Assert.IsTrue(result.Success);
            Assert.AreEqual(84, result.Amount);
            Assert.AreEqual(1, shop.OwnedCount(71));
            Assert.IsTrue(shop.IsUnlocked(71));
        }

        [Test]
        public void PurchaseStack_TotalCost_MultipliesUnitPricesAndWrapsAt99()
        {
            ShopCatalog catalog = CreateCatalog();
            var stack = new PurchaseStack();

            stack.Increment(71);
            stack.Increment(71);
            stack.Increment(133);

            Assert.AreEqual(2, stack.GetQuantity(71));
            Assert.AreEqual(1, stack.GetQuantity(133));
            Assert.AreEqual(390, stack.TotalCost(catalog));

            for (int i = 2; i < PurchaseStack.MaxQuantityPerLine; i++)
            {
                stack.Increment(71);
            }

            Assert.AreEqual(PurchaseStack.MaxQuantityPerLine, stack.GetQuantity(71));
            stack.Increment(71);
            Assert.AreEqual(0, stack.GetQuantity(71));
        }

        [Test]
        public void Checkout_CommitsEveryStackedLineAtomically()
        {
            var shop = new ShopState(CreateCatalog(), 500);
            shop.Unlock(71);
            shop.Unlock(133);

            shop.PurchaseStack.SetQuantity(71, 2);
            shop.PurchaseStack.SetQuantity(133, 1);

            ShopResult result = shop.Checkout();

            Assert.IsTrue(result.Success);
            Assert.AreEqual(390, result.Amount);
            Assert.AreEqual(110, shop.Money);
            Assert.AreEqual(2, shop.OwnedCount(71));
            Assert.AreEqual(1, shop.OwnedCount(133));
            Assert.IsTrue(shop.PurchaseStack.IsEmpty);
        }

        [Test]
        public void Checkout_WithInsufficientFunds_LeavesStackAndInventoryUntouched()
        {
            var shop = new ShopState(CreateCatalog(), 200);
            shop.Unlock(71);
            shop.PurchaseStack.SetQuantity(71, 2);

            ShopResult result = shop.Checkout();

            Assert.AreEqual(ShopError.InsufficientFunds, result.Error);
            Assert.AreEqual(200, shop.Money);
            Assert.AreEqual(0, shop.OwnedCount(71));
            Assert.AreEqual(2, shop.PurchaseStack.GetQuantity(71));
        }

        [Test]
        public void ReconcileStock_DeduplicatesRowsAndBackfillsHistoricalGifts()
        {
            ShopCatalog catalog = CreateCatalog();
            var shop = new ShopState(catalog);
            var save = new ShopSaveData();
            save.stock.Add(new ShopStockSave { partId = 71, quantity = 0 });
            save.stock.Add(new ShopStockSave { partId = 71, quantity = 0 });
            save.stock.Add(new ShopStockSave { partId = 0, quantity = 1 });
            save.stock.Add(new ShopStockSave { partId = 0, quantity = 1 });
            shop.LoadFrom(save);

            int[][] gifts =
            {
                new[] { 0, 71, 133, 173, 222 },
                new[] { 1, 72 }
            };

            shop.ReconcileStock(gifts, 1);

            Assert.AreEqual(1, CountStock(shop, 71));
            Assert.AreEqual(1, CountStock(shop, 0));
            Assert.IsTrue(shop.IsUnlocked(133));
            Assert.IsTrue(shop.IsUnlocked(173));
            Assert.IsTrue(shop.IsUnlocked(222));
            Assert.IsTrue(shop.IsUnlocked(72));
            Assert.AreEqual(1, shop.GetBodyStock(0));
        }

        [Test]
        public void ReconcileStock_IsIdempotent()
        {
            var shop = new ShopState(CreateCatalog(), 777);
            shop.Unlock(71);
            shop.Unlock(71);
            shop.AddOwnedPart(71);

            int[][] gifts =
            {
                new[] { 0, 71, 133, 173 },
                new[] { 1, 72, 222 }
            };

            shop.ReconcileStock(gifts, 1);
            string first = Snapshot(shop);

            shop.ReconcileStock(gifts, 1);
            string second = Snapshot(shop);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void ReconcileStock_DoesNotBackfillBodyWhenOwnedOrInActiveFleet()
        {
            ShopCatalog catalog = CreateCatalog();
            var shop = new ShopState(catalog);
            shop.Unlock(71);
            shop.AddOwnedPart(0);

            shop.ReconcileStock(
                new[] { new[] { 0, 1, 2, 71 } },
                0,
                new[] { "B.Small.02" });

            Assert.IsFalse(shop.IsUnlocked(0));
            Assert.IsFalse(shop.IsUnlocked(1));
            Assert.AreEqual(1, shop.GetBodyStock(2));
        }

        [Test]
        public void ReconcileStock_ClampsStoryToHighestGiftTable()
        {
            var shop = new ShopState(CreateCatalog());
            var gifts = new int[40][];

            for (int i = 0; i < gifts.Length; i++)
            {
                gifts[i] = System.Array.Empty<int>();
            }

            gifts[34] = new[] { 71 };
            gifts[35] = new[] { 72 };

            shop.ReconcileStock(gifts, 39);

            Assert.IsTrue(shop.IsUnlocked(71));
            Assert.IsFalse(shop.IsUnlocked(72));
        }

        private static int CountStock(ShopState shop, int partId)
        {
            int count = 0;
            for (int i = 0; i < shop.Stock.Count; i++)
            {
                if (shop.Stock[i].PartId == partId) count++;
            }

            return count;
        }

        private static string Snapshot(ShopState shop)
        {
            var payload = new ABRevival.Core.Persistence.PlayerSaveData { shop = shop.CreateSave() };
            return ABRevival.Core.Persistence.PlayerSaveCodec.ToJson(payload);
        }
    }
}
