using System;
using System.Collections.Generic;
using ABRevival.Core.Data;

namespace ABRevival.Core.Shop
{
    /// <summary>
    /// Immutable shop-facing view of a legacy <c>F4DataOrg</c> part definition.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public readonly struct ShopPart
    {
        public readonly int PartId;
        public readonly string Name;
        public readonly PartCategory Category;
        public readonly int Cost;

        public ShopPart(int partId, string name, PartCategory category, int cost)
        {
            PartId = partId;
            Name = name ?? string.Empty;
            Category = category;
            Cost = cost < 0 ? 0 : cost;
        }

        public bool IsBody => Category == PartCategory.Body;
    }

    /// <summary>
    /// Read-only part lookup used by the shop domain. Kept separate from
    /// <see cref="PartDefinitionSO"/> so Tier 1 tests never touch Unity assets.
    /// </summary>
    public sealed class ShopCatalog
    {
        private readonly Dictionary<int, ShopPart> _parts;

        public ShopCatalog(IEnumerable<ShopPart> parts)
        {
            _parts = new Dictionary<int, ShopPart>();
            if (parts == null) return;

            foreach (ShopPart part in parts)
            {
                _parts[part.PartId] = part;
            }
        }

        public IReadOnlyCollection<ShopPart> Parts => _parts.Values;

        public bool TryGet(int partId, out ShopPart part) => _parts.TryGetValue(partId, out part);

        public bool Contains(int partId) => _parts.ContainsKey(partId);
    }
}
