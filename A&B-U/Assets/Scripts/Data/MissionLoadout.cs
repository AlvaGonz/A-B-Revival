using System.Collections.Generic;
using ABRevival.Core.Persistence;

namespace ABRevival.Core.Data
{
    /// <summary>
    /// The handoff from Center to the mission: the selected Player craft and every valid
    /// AI companion slot. The frozen readyYes() copies the same indices into
    /// InitData.CORE.levelData.afa / repair.flys before the mission starts.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class MissionLoadout
    {
        private readonly List<int> _companions;

        public int PlayerCraftIndex { get; }
        public IReadOnlyList<int> CompanionCraftIndices => _companions;
        public int CompanionCount => _companions.Count;

        public MissionLoadout(int playerCraftIndex, IEnumerable<int> companions)
        {
            PlayerCraftIndex = playerCraftIndex;
            _companions = companions != null ? new List<int>(companions) : new List<int>();
        }

        /// <summary>
        /// Snapshots the save's Player pick and occupied companion slots, dropping entries
        /// that point outside the live fleet or duplicate the Player craft.
        /// </summary>
        public static MissionLoadout Capture(PlayerSaveData save, int fleetCount)
        {
            if (fleetCount <= 0) fleetCount = save?.blueprints != null ? save.blueprints.Count : 0;
            if (fleetCount <= 0) return new MissionLoadout(-1, null);

            int player = save != null ? save.playerBlueprintIndex : 0;
            if (player < 0 || player >= fleetCount) player = 0;

            var companions = new List<int>();
            if (save?.companions != null)
            {
                for (int i = 0; i < save.companions.Count; i++)
                {
                    int craft = save.companions[i].craftIndex;
                    if (craft < 0 || craft >= fleetCount || craft == player) continue;
                    companions.Add(craft);
                }
            }

            return new MissionLoadout(player, companions);
        }
    }
}
