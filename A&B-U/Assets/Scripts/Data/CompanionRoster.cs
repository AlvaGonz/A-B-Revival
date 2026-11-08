using System;
using System.Collections.Generic;
using ABRevival.Core.Persistence;

namespace ABRevival.Core.Data
{
    /// <summary>
    /// Five AI wingman slots from Center (the frozen UIChooseFlyAI companion array).
    /// Slot rules are pure C# so they run in the Tier 1 gate and the save payload stays
    /// the single persistence format. Original game IP: Vargames (2011).
    /// </summary>
    public sealed class CompanionRoster
    {
        public const int SlotCount = 5;
        public const int RoleEscort = 1;
        public const int LeaderMichael = 0;

        private readonly CompanionSaveData[] _slots = new CompanionSaveData[SlotCount];

        public CompanionRoster()
        {
            Clear();
        }

        public int AssignedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _slots.Length; i++)
                {
                    if (_slots[i].craftIndex >= 0) count++;
                }

                return count;
            }
        }

        public int CraftIndex(int slot)
        {
            return slot >= 0 && slot < SlotCount ? _slots[slot].craftIndex : -1;
        }

        public int Role(int slot)
        {
            return slot >= 0 && slot < SlotCount ? _slots[slot].role : RoleEscort;
        }

        public bool IsAssigned(int slot)
        {
            return CraftIndex(slot) >= 0;
        }

        public int SlotOf(int craftIndex)
        {
            if (craftIndex < 0) return -1;
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].craftIndex == craftIndex) return i;
            }

            return -1;
        }

        public bool Contains(int craftIndex)
        {
            return SlotOf(craftIndex) >= 0;
        }

        public int FirstFreeSlot()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].craftIndex < 0) return i;
            }

            return -1;
        }

        /// <summary>Assigns a craft to a slot, rejecting duplicates like the frozen ckAIUse check.</summary>
        public bool Assign(int slot, int craftIndex)
        {
            if (slot < 0 || slot >= SlotCount || craftIndex < 0) return false;

            int existing = SlotOf(craftIndex);
            if (existing >= 0 && existing != slot) return false;

            CompanionSaveData entry = _slots[slot];
            entry.craftIndex = craftIndex;
            if (entry.role < 0 || entry.role > 5) entry.role = RoleEscort;
            if (entry.leader != LeaderMichael && entry.leader != 1) entry.leader = LeaderMichael;
            _slots[slot] = entry;
            return true;
        }

        public bool AssignFirstFree(int craftIndex)
        {
            int slot = FirstFreeSlot();
            return slot >= 0 && Assign(slot, craftIndex);
        }

        public bool RemoveAt(int slot)
        {
            if (slot < 0 || slot >= SlotCount) return false;
            if (_slots[slot].craftIndex < 0) return false;

            CompanionSaveData entry = _slots[slot];
            entry.craftIndex = -1;
            _slots[slot] = entry;
            return true;
        }

        public bool Remove(int craftIndex)
        {
            int slot = SlotOf(craftIndex);
            return slot >= 0 && RemoveAt(slot);
        }

        public void Clear()
        {
            for (int i = 0; i < _slots.Length; i++) _slots[i] = CompanionSaveData.Empty;
        }

        /// <summary>Loads saved slots, ignoring out-of-range, duplicate and player-owned entries.</summary>
        public void Load(IReadOnlyList<CompanionSaveData> saved, int playerCraftIndex = -1, int fleetCount = int.MaxValue)
        {
            Clear();
            if (saved == null) return;

            for (int slot = 0; slot < SlotCount && slot < saved.Count; slot++)
            {
                CompanionSaveData entry = saved[slot];
                if (entry.craftIndex < 0 || entry.craftIndex >= fleetCount) continue;
                if (entry.craftIndex == playerCraftIndex) continue;
                if (Contains(entry.craftIndex)) continue;
                if (entry.role < 0 || entry.role > 5) entry.role = RoleEscort;
                if (entry.leader != LeaderMichael && entry.leader != 1) entry.leader = LeaderMichael;
                _slots[slot] = entry;
            }
        }

        public List<CompanionSaveData> ToSave()
        {
            var saved = new List<CompanionSaveData>(SlotCount);
            for (int i = 0; i < _slots.Length; i++) saved.Add(_slots[i]);
            return saved;
        }
    }
}
