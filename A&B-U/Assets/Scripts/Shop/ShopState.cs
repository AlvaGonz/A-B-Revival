using System;
using System.Collections.Generic;
using ABRevival.Core.Persistence;
using ABRevival.Core.Story;

namespace ABRevival.Core.Shop
{
    public enum ShopError
    {
        None = 0,
        UnknownPart,
        OutOfStock,
        InsufficientFunds,
        NotOwned,
        InvalidQuantity,
        EmptyPurchase
    }

    public readonly struct ShopResult
    {
        public readonly ShopError Error;
        public readonly int Amount;

        public ShopResult(ShopError error, int amount = 0)
        {
            Error = error;
            Amount = amount;
        }

        public bool Success => Error == ShopError.None;

        public static ShopResult Ok(int amount = 0) => new ShopResult(ShopError.None, amount);
    }

    public struct ShopStockEntry
    {
        public readonly int PartId;
        public readonly int Quantity;

        /// <summary>False while the row still carries the frozen NEW badge.</summary>
        public readonly bool Seen;

        public ShopStockEntry(int partId, int quantity, bool seen = false)
        {
            PartId = partId;
            Quantity = quantity;
            Seen = seen;
        }
    }

    /// <summary>
    /// Pure C# shop inventory implementing ADR 0003: infinite stock for
    /// non-body parts, finite stock for hulls, unit selling, purchase stacking
    /// and retroactive stock reconciliation.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class ShopState
    {
        public const float SellRate = 0.7f;
        public const int MaxStoryIndex = StoryDirector.MaxStoryIndex;

        private readonly ShopCatalog _catalog;
        private readonly List<ShopStockEntry> _stock = new List<ShopStockEntry>();
        private readonly List<int> _ownedParts = new List<int>();

        private int _money;

        public ShopState(ShopCatalog catalog, int money = 0)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _money = Math.Max(0, money);
        }

        public ShopCatalog Catalog => _catalog;

        public int Money => _money;

        public IReadOnlyList<ShopStockEntry> Stock => _stock;

        public IReadOnlyList<int> OwnedParts => _ownedParts;

        public PurchaseStack PurchaseStack { get; } = new PurchaseStack();

        public bool IsUnlocked(int partId) => FindStockIndex(partId) >= 0;

        public int GetBodyStock(int partId)
        {
            int index = FindStockIndex(partId);
            if (index < 0) return 0;
            return _catalog.TryGet(partId, out ShopPart part) && part.IsBody ? _stock[index].Quantity : 0;
        }

        public int OwnedCount(int partId)
        {
            int count = 0;
            for (int i = 0; i < _ownedParts.Count; i++)
            {
                if (_ownedParts[i] == partId) count++;
            }

            return count;
        }

        public int GetSellValue(int partId)
        {
            if (!_catalog.TryGet(partId, out ShopPart part)) return 0;
            // Integer math: float truncation made 500*0.7 sell for 349 under Mono.
            return (part.Cost * 7) / 10;
        }

        /// <summary>
        /// Adds a shop row. Non-body rows are infinite and never duplicated.
        /// Body quantity is only meaningful for finite hull stock.
        /// </summary>
        public bool Unlock(int partId, int bodyQuantity = 1)
        {
            if (!_catalog.TryGet(partId, out ShopPart part)) return false;

            int index = FindStockIndex(partId);
            if (index >= 0)
            {
                return true;
            }

            _stock.Add(new ShopStockEntry(partId, part.IsBody ? Math.Max(0, bodyQuantity) : 0));
            return true;        }

        public ShopResult Buy(int partId, int quantity)
        {
            ShopResult validation = ValidatePurchase(partId, quantity, out ShopPart part, out int totalCost);
            if (!validation.Success) return validation;

            ApplyPurchase(part, quantity);
            _money -= totalCost;
            return ShopResult.Ok(totalCost);
        }

        public ShopResult Sell(int partId)
        {
            if (!_catalog.TryGet(partId, out ShopPart part)) return new ShopResult(ShopError.UnknownPart);

            int ownedIndex = _ownedParts.IndexOf(partId);
            if (ownedIndex < 0) return new ShopResult(ShopError.NotOwned);

            _ownedParts.RemoveAt(ownedIndex);
            int credit = GetSellValue(partId);
            _money += credit;

            if (part.IsBody)
            {
                int stockIndex = FindStockIndex(partId);
                if (stockIndex < 0)
                {
                    _stock.Add(new ShopStockEntry(partId, 1, seen: true));
                }
                else
                {
                    _stock[stockIndex] = new ShopStockEntry(partId, _stock[stockIndex].Quantity + 1, seen: true);
                }
            }
            else if (!IsUnlocked(partId))
            {
                _stock.Add(new ShopStockEntry(partId, 0, seen: true));
            }

            return ShopResult.Ok(credit);
        }

        /// <summary>
        /// Atomic ACCEPT: validates every stacked line, then commits all of them.
        /// </summary>
        public ShopResult Checkout()
        {
            if (PurchaseStack.IsEmpty) return new ShopResult(ShopError.EmptyPurchase);

            IReadOnlyList<PurchaseStackLine> lines = PurchaseStack.Lines;
            long total = 0;

            for (int i = 0; i < lines.Count; i++)
            {
                ShopResult validation = ValidatePurchase(lines[i].PartId, lines[i].Quantity, out _, out int lineCost);
                if (!validation.Success) return validation;
                total += lineCost;
            }

            if (total > _money) return new ShopResult(ShopError.InsufficientFunds);

            for (int i = 0; i < lines.Count; i++)
            {
                _catalog.TryGet(lines[i].PartId, out ShopPart part);
                ApplyPurchase(part, lines[i].Quantity);
            }

            _money -= (int)total;
            PurchaseStack.Clear();
            return ShopResult.Ok((int)total);
        }

        public bool TryTakeOwnedPart(int partId)
        {
            int index = _ownedParts.IndexOf(partId);
            if (index < 0) return false;

            _ownedParts.RemoveAt(index);
            return true;
        }

        public void AddOwnedPart(int partId)
        {
            if (_catalog.Contains(partId)) _ownedParts.Add(partId);
        }

        /// <summary>
        /// ADR 0003 reconciliation: dedupe rows, backfill historical gift unlocks,
        /// and restore missing hulls that are neither owned nor in the active fleet.
        /// Deterministic and idempotent.
        /// </summary>
        public void ReconcileStock(
            IReadOnlyList<int[]> gifts,
            int currentStory,
            IReadOnlyCollection<string> activeHullNames = null)
        {
            currentStory = Math.Clamp(currentStory, 0, MaxStoryIndex);

            DedupeStock();

            if (gifts != null)
            {
                int limit = Math.Min(currentStory + 1, gifts.Count);
                for (int story = 0; story < limit; story++)
                {
                    int[] giftList = gifts[story];
                    if (giftList == null) continue;

                    for (int i = 0; i < giftList.Length; i++)
                    {
                        BackfillGift(giftList[i], activeHullNames);
                    }
                }
            }

            _stock.Sort((a, b) => a.PartId.CompareTo(b.PartId));
        }

        public ShopSaveData CreateSave()
        {
            var save = new ShopSaveData { money = _money };

            for (int i = 0; i < _stock.Count; i++)
            {
                save.stock.Add(new ShopStockSave
                {
                    partId = _stock[i].PartId,
                    quantity = _stock[i].Quantity,
                    seen = _stock[i].Seen
                });
            }

            save.ownedParts.AddRange(_ownedParts);
            return save;
        }

        public void LoadFrom(ShopSaveData save)
        {
            _money = Math.Max(0, save?.money ?? 0);
            _stock.Clear();
            _ownedParts.Clear();
            PurchaseStack.Clear();

            if (save?.stock != null)
            {
                for (int i = 0; i < save.stock.Count; i++)
                {
                    ShopStockSave entry = save.stock[i];
                    if (_catalog.Contains(entry.partId))
                        _stock.Add(new ShopStockEntry(entry.partId, entry.quantity, entry.seen));
                }
            }

            if (save?.ownedParts != null)
            {
                for (int i = 0; i < save.ownedParts.Count; i++)
                {
                    int partId = save.ownedParts[i];
                    if (_catalog.Contains(partId)) _ownedParts.Add(partId);
                }
            }
        }

        /// <summary>
        /// Save loading path from ADR 0003: restore then reconcile against the
        /// historical gift table so old saves gain missing unlocks.
        /// </summary>
        public void LoadFrom(
            ShopSaveData save,
            IReadOnlyList<int[]> gifts,
            int currentStory,
            IReadOnlyCollection<string> activeHullNames = null)
        {
            LoadFrom(save);
            ReconcileStock(gifts, currentStory, activeHullNames);
        }

        public static ShopState FromSave(ShopCatalog catalog, ShopSaveData save)
        {
            var state = new ShopState(catalog);
            state.LoadFrom(save);
            return state;
        }

        private ShopResult ValidatePurchase(int partId, int quantity, out ShopPart part, out int totalCost)
        {
            part = default;
            totalCost = 0;

            if (quantity <= 0) return new ShopResult(ShopError.InvalidQuantity);
            if (!_catalog.TryGet(partId, out part)) return new ShopResult(ShopError.UnknownPart);

            int index = FindStockIndex(partId);
            if (index < 0) return new ShopResult(ShopError.OutOfStock);
            if (part.IsBody && _stock[index].Quantity < quantity) return new ShopResult(ShopError.OutOfStock);

            long cost = (long)part.Cost * quantity;
            if (cost > int.MaxValue) return new ShopResult(ShopError.InsufficientFunds);
            totalCost = (int)cost;

            if (totalCost > _money) return new ShopResult(ShopError.InsufficientFunds);

            return ShopResult.Ok(totalCost);
        }

        private void ApplyPurchase(ShopPart part, int quantity)
        {
            int index = FindStockIndex(part.PartId);
            if (part.IsBody)
            {
                int remaining = _stock[index].Quantity - quantity;
                if (remaining <= 0)
                {
                    _stock.RemoveAt(index);
                }
                else
                {
                    _stock[index] = new ShopStockEntry(part.PartId, remaining, seen: true);
                }
            }
            else
            {
                // Buying clears the NEW badge for this stock row.
                _stock[index] = new ShopStockEntry(part.PartId, _stock[index].Quantity, seen: true);
            }

            for (int i = 0; i < quantity; i++)
            {
                _ownedParts.Add(part.PartId);
            }
        }

        private void DedupeStock()
        {
            var seen = new HashSet<int>();
            for (int i = 0; i < _stock.Count; i++)
            {
                ShopStockEntry entry = _stock[i];
                if (!_catalog.TryGet(entry.PartId, out ShopPart part) || !seen.Add(entry.PartId))
                {
                    _stock.RemoveAt(i);
                    i--;
                    continue;
                }

                int quantity = part.IsBody ? Math.Max(1, entry.Quantity) : 0;
                _stock[i] = new ShopStockEntry(entry.PartId, quantity, entry.Seen);
            }
        }

        private void BackfillGift(int partId, IReadOnlyCollection<string> activeHullNames)
        {
            if (!_catalog.TryGet(partId, out ShopPart part)) return;
            if (IsUnlocked(partId)) return;

            if (!part.IsBody)
            {
                _stock.Add(new ShopStockEntry(partId, 0));
                return;
            }

            if (OwnedCount(partId) > 0) return;
            if (activeHullNames != null && ContainsName(activeHullNames, part.Name)) return;

            _stock.Add(new ShopStockEntry(partId, 1));
        }

        private int FindStockIndex(int partId)
        {
            for (int i = 0; i < _stock.Count; i++)
            {
                if (_stock[i].PartId == partId) return i;
            }

            return -1;
        }

        private static bool ContainsName(IReadOnlyCollection<string> names, string name)
        {
            foreach (string candidate in names)
            {
                if (string.Equals(candidate, name, StringComparison.Ordinal)) return true;
            }

            return false;
        }
    }
}
