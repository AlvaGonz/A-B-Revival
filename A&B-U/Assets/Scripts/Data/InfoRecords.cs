using System;
using System.Collections.Generic;
using ABRevival.Core.Camera;
using ABRevival.Core.Persistence;

namespace ABRevival.Core.Data
{
    /// <summary>One owned-part aggregate shown by the read-only Info records screen.</summary>
    public readonly struct OwnedPartQuantity
    {
        public readonly int partId;
        public readonly int quantity;

        public OwnedPartQuantity(int partId, int quantity)
        {
            this.partId = partId;
            this.quantity = quantity;
        }
    }

    /// <summary>
    /// Read-only projection of the single player save for the Info/Records screen. These
    /// helpers never mutate <see cref="PlayerSaveData"/>; they only label and tally what is
    /// already stored (story, money, owned parts, fleet and companion slots).
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class InfoRecords
    {
        public const int CompanionSlots = 5;

        public static string StoryLabel(int storyIndex)
        {
            return "BATTLE-" + (storyIndex + 1);
        }

        public static string CameraLabel(CameraDistancePreset preset)
        {
            return preset switch
            {
                CameraDistancePreset.Short => "Short (1.0x)",
                CameraDistancePreset.Medium => "Medium (1.3x)",
                CameraDistancePreset.Far => "Far (1.5x)",
                _ => "Short (1.0x)"
            };
        }

        /// <summary>Total owned units, counting every copy of every part id.</summary>
        public static int CountOwnedParts(IReadOnlyList<int> ownedParts)
        {
            return ownedParts?.Count ?? 0;
        }

        /// <summary>Owned units grouped by part id, sorted ascending so the table is stable.</summary>
        public static List<OwnedPartQuantity> TallyOwnedParts(IReadOnlyList<int> ownedParts)
        {
            var tallies = new List<OwnedPartQuantity>();
            if (ownedParts == null) return tallies;

            for (int i = 0; i < ownedParts.Count; i++)
            {
                int id = ownedParts[i];
                int found = -1;
                for (int j = 0; j < tallies.Count; j++)
                {
                    if (tallies[j].partId != id) continue;
                    found = j;
                    break;
                }

                if (found >= 0)
                    tallies[found] = new OwnedPartQuantity(id, tallies[found].quantity + 1);
                else
                    tallies.Add(new OwnedPartQuantity(id, 1));
            }

            tallies.Sort((left, right) => left.partId.CompareTo(right.partId));
            return tallies;
        }

        /// <summary>Assigned companion slots; empty saves store <c>craftIndex = -1</c>.</summary>
        public static int CountAssignedCompanions(IReadOnlyList<CompanionSaveData> companions)
        {
            if (companions == null) return 0;

            int count = 0;
            for (int i = 0; i < companions.Count; i++)
            {
                if (companions[i].craftIndex >= 0) count++;
            }

            return count;
        }
    }
}
