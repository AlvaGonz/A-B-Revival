using System;
using System.Collections.Generic;
using ABRevival.Core.Data;
using ABRevival.Core.Shop;
using UnityEngine;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Shared PartDefinitionSO catalog and PartMounts bootstrap for Center, Factory and Shop.
    /// Each screen owns its serialized catalog, but loading and shop-catalog construction are
    /// identical and used to drift between the three copies.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class PartCatalogBootstrap
    {
        public static List<PartDefinitionSO> Collect(PartDefinitionSO[] catalogParts, string owner)
        {
            var definitions = new List<PartDefinitionSO>();
            if (catalogParts != null)
            {
                for (int i = 0; i < catalogParts.Length; i++)
                {
                    if (catalogParts[i] != null) definitions.Add(catalogParts[i]);
                }
            }

            if (definitions.Count == 0)
                Debug.LogError($"[{owner}] No PartDefinitionSO catalog assigned; the part list is empty.");

            return definitions;
        }

        public static ShopCatalog BuildCatalog(IReadOnlyList<PartDefinitionSO> definitions)
        {
            var parts = new List<ShopPart>(definitions.Count);
            for (int i = 0; i < definitions.Count; i++)
            {
                PartDefinitionSO definition = definitions[i];
                if (definition != null)
                    parts.Add(new ShopPart(definition.partId, definition.partName, definition.category, definition.cost));
            }

            return new ShopCatalog(parts);
        }

        public static PartMountTable LoadMountTable(string owner)
        {
            try
            {
                TextAsset table = Resources.Load<TextAsset>("Data/PartMounts");
                return table != null ? PartMountTable.Parse(table.text) : null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[{owner}] Part mount table unavailable: {exception.Message}");
                return null;
            }
        }
    }
}
