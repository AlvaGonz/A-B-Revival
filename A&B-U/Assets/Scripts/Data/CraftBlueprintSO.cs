using System;
using UnityEngine;

namespace ABRevival.Core.Data
{
    [CreateAssetMenu(fileName = "NewCraftBlueprint", menuName = "ABRevival/Craft Blueprint")]
    public class CraftBlueprintSO : ScriptableObject
    {
        public string craftName;
        public string hullName;
        public HullCategory hullCategory;
        public float hullMass = 100f;
        public float hullHealth = 500f;
        public bool isGrandfathered;

        public MountedPartData[] mountedParts = Array.Empty<MountedPartData>();

        /// <summary>
        /// Deduce Hull Category from hull prefix according to ADR 0007:
        /// Small: B.Sml, Medium: B.Med (including B.Med++), Large: B.Lrg, Mega: B.Mega.
        /// </summary>
        public static HullCategory GetHullCategoryForName(string name)
        {
            if (string.IsNullOrEmpty(name)) return HullCategory.Medium;

            if (name.StartsWith("B.Sml", StringComparison.OrdinalIgnoreCase))
                return HullCategory.Small;
            if (name.StartsWith("B.Med", StringComparison.OrdinalIgnoreCase))
                return HullCategory.Medium;
            if (name.StartsWith("B.Lrg", StringComparison.OrdinalIgnoreCase))
                return HullCategory.Large;
            if (name.StartsWith("B.Mega", StringComparison.OrdinalIgnoreCase))
                return HullCategory.Mega;

            return HullCategory.Medium; // default fallback
        }

        /// <summary>
        /// Wing limits per hull category established in ADR 0007:
        /// Small = 4, Medium = 6, Large = 8, Mega = 12.
        /// </summary>
        public static int GetMaxWingsForCategory(HullCategory category)
        {
            return category switch
            {
                HullCategory.Small => 4,
                HullCategory.Medium => 6,
                HullCategory.Large => 8,
                HullCategory.Mega => 12,
                _ => 6
            };
        }

        /// <summary>
        /// Pure domain validation of wing capacity limit against ADR 0007.
        /// Bypasses capacity ceiling if isGrandfathered == true (ADR 0007 grandfathering rule).
        /// Zero-alloc, pure execution.
        /// </summary>
        public static bool ValidateWingCapacity(HullCategory category, MountedPartData[] parts, bool isGrandfathered, out string errorMessage)
        {
            int wingCount = 0;
            if (parts != null)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i].category == PartCategory.Wing)
                    {
                        wingCount++;
                    }
                }
            }

            int maxAllowed = GetMaxWingsForCategory(category);
            if (wingCount > maxAllowed && !isGrandfathered)
            {
                errorMessage = $"Wing count ({wingCount}) exceeds capacity limit ({maxAllowed}) for {category} hull.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Instance overload for ScriptableObject asset inspection.
        /// </summary>
        public bool ValidateWingCapacity(out string errorMessage)
        {
            return ValidateWingCapacity(hullCategory, mountedParts, isGrandfathered, out errorMessage);
        }

        /// <summary>
        /// Pure static calculation of aggregated craft properties (mass, health, thrust, energy draw).
        /// Zero dynamic allocations.
        /// </summary>
        public static AggregateCraftStats CalculateAggregateStats(float mass, float health, MountedPartData[] parts)
        {
            var stats = new AggregateCraftStats
            {
                totalMass = mass,
                totalHealth = health,
                totalThrust = 0f,
                totalEnergyDraw = 0f
            };

            if (parts == null) return stats;

            for (int i = 0; i < parts.Length; i++)
            {
                stats.totalMass += parts[i].mass;
                stats.totalHealth += parts[i].health;
                stats.totalThrust += parts[i].thrust;
                stats.totalEnergyDraw += parts[i].energyDraw;
            }

            return stats;
        }

        /// <summary>
        /// Instance overload for ScriptableObject asset inspection.
        /// </summary>
        public AggregateCraftStats CalculateAggregateStats()
        {
            return CalculateAggregateStats(hullMass, hullHealth, mountedParts);
        }
    }
}
