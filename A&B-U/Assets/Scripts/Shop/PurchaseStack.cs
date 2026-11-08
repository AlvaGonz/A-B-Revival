using System.Collections.Generic;

namespace ABRevival.Core.Shop
{
    public struct PurchaseStackLine
    {
        public readonly int PartId;
        public readonly int Quantity;

        public PurchaseStackLine(int partId, int quantity)
        {
            PartId = partId;
            Quantity = quantity;
        }
    }

    /// <summary>
    /// Staged purchase quantities for the shop checkout loop (ADR 0003).
    /// Transient UI state: it is never persisted.
    /// </summary>
    public sealed class PurchaseStack
    {
        public const int MaxQuantityPerLine = 99;

        private readonly List<PurchaseStackLine> _lines = new List<PurchaseStackLine>();

        public IReadOnlyList<PurchaseStackLine> Lines => _lines;

        public bool IsEmpty => _lines.Count == 0;

        public int GetQuantity(int partId)
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].PartId == partId) return _lines[i].Quantity;
            }

            return 0;
        }

        public void SetQuantity(int partId, int quantity)
        {
            if (quantity <= 0)
            {
                Remove(partId);
                return;
            }

            if (quantity > MaxQuantityPerLine) quantity = MaxQuantityPerLine;

            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].PartId == partId)
                {
                    _lines[i] = new PurchaseStackLine(partId, quantity);
                    return;
                }
            }

            _lines.Add(new PurchaseStackLine(partId, quantity));
        }

        /// <summary>Click behaviour from FormShop: 0 -> 1 -> ... -> 99 -> 0.</summary>
        public void Increment(int partId)
        {
            int next = GetQuantity(partId);
            next = next >= MaxQuantityPerLine ? 0 : next + 1;
            SetQuantity(partId, next);
        }

        public void Clear() => _lines.Clear();

        public int TotalCost(ShopCatalog catalog)
        {
            long total = 0;
            for (int i = 0; i < _lines.Count; i++)
            {
                PurchaseStackLine line = _lines[i];
                if (catalog != null && catalog.TryGet(line.PartId, out ShopPart part))
                {
                    total += (long)part.Cost * line.Quantity;
                }
            }

            return total > int.MaxValue ? int.MaxValue : (int)total;
        }

        private void Remove(int partId)
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                if (_lines[i].PartId == partId)
                {
                    _lines.RemoveAt(i);
                    return;
                }
            }
        }
    }
}
