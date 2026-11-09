using System;
using System.Collections.Generic;
using System.Globalization;
using ABRevival.Core.Data;
using ABRevival.Core.Persistence;

namespace ABRevival.Core.Shop
{
    /// <summary>One mounted part destroyed in the mission, with its saved craft coordinates.</summary>
    public readonly struct DamagedPart
    {
        public readonly int blueprintIndex;
        public readonly int mountedIndex;
        public readonly int partId;
        public readonly PartCategory category;

        public DamagedPart(int blueprintIndex, int mountedIndex, int partId, PartCategory category)
        {
            this.blueprintIndex = blueprintIndex;
            this.mountedIndex = mountedIndex;
            this.partId = partId;
            this.category = category;
        }
    }

    /// <summary>Display data for one part id, resolved from the imported part catalog.</summary>
    public readonly struct RepairPartInfo
    {
        public readonly string name;
        public readonly PartCategory category;
        public readonly int lr;
        public readonly float health;
        public readonly float mass;
        public readonly float thrust;
        public readonly float turnFly;
        public readonly float firePower;
        public readonly int cost;

        public RepairPartInfo(
            string name,
            PartCategory category,
            int lr,
            float health,
            float mass,
            float thrust,
            float turnFly,
            float firePower,
            int cost)
        {
            this.name = name;
            this.category = category;
            this.lr = lr;
            this.health = health;
            this.mass = mass;
            this.thrust = thrust;
            this.turnFly = turnFly;
            this.firePower = firePower;
            this.cost = cost;
        }

        public static RepairPartInfo Fallback(int partId, PartCategory category)
        {
            return new RepairPartInfo("Part " + partId.ToString(CultureInfo.InvariantCulture),
                category, 0, 0f, 0f, 0f, 0f, 0f, 0);
        }
    }

    /// <summary>One row of the frozen FormRepair table.</summary>
    public sealed class RepairItem
    {
        public readonly int BlueprintIndex;
        public readonly int MountedIndex;
        public readonly int PartId;
        public readonly string CraftName;
        public readonly string Name;
        public readonly PartCategory Category;
        public readonly int Lr;
        public readonly float Health;
        public readonly float Mass;
        public readonly float Thrust;
        public readonly float TurnFly;
        public readonly float FirePower;
        public readonly int BaseCost;
        public readonly int RepairCost;

        public bool Selected;

        public RepairItem(
            int blueprintIndex,
            int mountedIndex,
            int partId,
            string craftName,
            in RepairPartInfo info,
            int repairCost)
        {
            BlueprintIndex = blueprintIndex;
            MountedIndex = mountedIndex;
            PartId = partId;
            CraftName = craftName ?? string.Empty;
            Name = info.name;
            Category = info.category;
            Lr = info.lr;
            Health = info.health;
            Mass = info.mass;
            Thrust = info.thrust;
            TurnFly = info.turnFly;
            FirePower = info.firePower;
            BaseCost = info.cost;
            RepairCost = repairCost;
        }
    }

    /// <summary>
    /// Pure repair ledger for UIRepair. Damaged parts are the mounted parts destroyed during
    /// the mission. Repairing a selected part costs FormRepair's 70% of its catalog value and
    /// restores it to full health. Skipping the remaining repairs repeats UIWin.toNext ->
    /// RepairPartsCtrl.removeParts: the part leaves the craft and becomes shop stock again.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class RepairState
    {
        /// <summary>FormRepair costMut / InitData.REPAIR: int(part.cost * 0.7).</summary>
        public const int RepairPercent = 70;

        private readonly PlayerSaveData _save;
        private readonly List<RepairItem> _items = new List<RepairItem>();

        private RepairState(PlayerSaveData save)
        {
            _save = save;
        }

        public IReadOnlyList<RepairItem> Items => _items;

        public int Money => _save?.shop != null ? _save.shop.money : 0;

        public bool HasDamage => _items.Count > 0;

        public int SelectedCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < _items.Count; i++) if (_items[i].Selected) count++;
                return count;
            }
        }

        public int SelectedCost
        {
            get
            {
                long cost = 0;
                for (int i = 0; i < _items.Count; i++) if (_items[i].Selected) cost += _items[i].RepairCost;
                return cost > int.MaxValue ? int.MaxValue : (int)cost;
            }
        }

        /// <summary>Money left if the current selection is accepted; negative means unaffordable.</summary>
        public int RemainingMoney => Money - SelectedCost;

        public bool CanAfford => RemainingMoney >= 0;

        public bool CanApply => SelectedCount > 0 && CanAfford;

        /// <summary>Number of parts already repaired through this ledger.</summary>
        public int RepairedCount { get; private set; }

        public static RepairState Create(
            PlayerSaveData save,
            IReadOnlyList<DamagedPart> damage,
            Func<int, RepairPartInfo> resolve)
        {
            var state = new RepairState(save);
            if (save?.blueprints == null || damage == null) return state;

            for (int i = 0; i < damage.Count; i++)
            {
                DamagedPart damaged = damage[i];
                if (damaged.blueprintIndex < 0 || damaged.blueprintIndex >= save.blueprints.Count) continue;

                BlueprintSaveData blueprint = save.blueprints[damaged.blueprintIndex];
                if (blueprint?.mountedParts == null) continue;
                if (damaged.mountedIndex < 0 || damaged.mountedIndex >= blueprint.mountedParts.Count) continue;

                MountedPartSaveData part = blueprint.mountedParts[damaged.mountedIndex];
                if (part.partId != damaged.partId) continue;
                if (state.Contains(damaged.blueprintIndex, damaged.mountedIndex)) continue;

                RepairPartInfo info = resolve != null
                    ? resolve(damaged.partId)
                    : RepairPartInfo.Fallback(damaged.partId, damaged.category);
                if (string.IsNullOrEmpty(info.name)) info = RepairPartInfo.Fallback(damaged.partId, damaged.category);

                state._items.Add(new RepairItem(
                    damaged.blueprintIndex,
                    damaged.mountedIndex,
                    damaged.partId,
                    CraftName(blueprint, damaged.blueprintIndex),
                    in info,
                    RepairCost(info.cost)));
            }

            state._items.Sort(CompareItems);
            return state;
        }

        public void Toggle(int index)
        {
            if (index < 0 || index >= _items.Count) return;
            _items[index].Selected = !_items[index].Selected;
        }

        /// <summary>Selects every matching row; returns the total cost of the newly selected rows.</summary>
        public int SelectAll(PartCategory? category = null)
        {
            int cost = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                RepairItem item = _items[i];
                if (item.Selected) continue;
                if (category.HasValue && item.Category != category.Value) continue;
                item.Selected = true;
                cost += item.RepairCost;
            }

            return cost;
        }

        public void Reset()
        {
            for (int i = 0; i < _items.Count; i++) _items[i].Selected = false;
        }

        /// <summary>
        /// Accepts the selected repairs: restores each part to full health and charges 70% of
        /// its catalog value. All-or-nothing when the purse cannot cover the selection.
        /// </summary>
        public int ApplySelected()
        {
            if (!CanApply) return 0;

            int repaired = 0;
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                RepairItem item = _items[i];
                if (!item.Selected) continue;

                if (!TryGetPart(item, out BlueprintSaveData blueprint, out int partIndex))
                {
                    _items.RemoveAt(i);
                    continue;
                }

                MountedPartSaveData part = blueprint.mountedParts[partIndex];
                float maxHealth = part.maxHealth > 0f ? part.maxHealth : part.health;
                if (maxHealth <= 0f) maxHealth = 1f;
                part.health = maxHealth;
                part.maxHealth = maxHealth;
                blueprint.mountedParts[partIndex] = part;

                _save.shop.money -= item.RepairCost;
                _items.RemoveAt(i);
                repaired++;
            }

            RepairedCount += repaired;
            return repaired;
        }

        /// <summary>
        /// UIWin.toNext / RepairPartsCtrl.removeParts: every part still damaged at departure
        /// leaves its craft and returns to the shop stock (bodies add a hull unit, modules
        /// unlock their row). Returns the number of parts removed.
        /// </summary>
        public int SkipRemaining()
        {
            if (_items.Count == 0) return 0;
            if (_save.shop == null) _save.shop = new ShopSaveData();

            _items.Sort((left, right) =>
            {
                int byBlueprint = left.BlueprintIndex.CompareTo(right.BlueprintIndex);
                return byBlueprint != 0 ? byBlueprint : right.MountedIndex.CompareTo(left.MountedIndex);
            });

            int removed = 0;
            for (int i = 0; i < _items.Count; i++)
            {
                RepairItem item = _items[i];
                if (!TryGetPart(item, out BlueprintSaveData blueprint, out int partIndex)) continue;

                blueprint.mountedParts.RemoveAt(partIndex);
                AddBuyBackStock(item);
                removed++;
            }

            _items.Clear();
            return removed;
        }

        private bool TryGetPart(RepairItem item, out BlueprintSaveData blueprint, out int partIndex)
        {
            blueprint = null;
            partIndex = -1;
            if (_save?.blueprints == null) return false;
            if (item.BlueprintIndex < 0 || item.BlueprintIndex >= _save.blueprints.Count) return false;

            blueprint = _save.blueprints[item.BlueprintIndex];
            if (blueprint?.mountedParts == null) return false;
            if (item.MountedIndex < 0 || item.MountedIndex >= blueprint.mountedParts.Count) return false;
            if (blueprint.mountedParts[item.MountedIndex].partId != item.PartId) return false;

            partIndex = item.MountedIndex;
            return true;
        }

        private void AddBuyBackStock(RepairItem item)
        {
            List<ShopStockSave> stock = _save.shop.stock;
            if (stock == null)
            {
                stock = new List<ShopStockSave>();
                _save.shop.stock = stock;
            }

            for (int i = 0; i < stock.Count; i++)
            {
                if (stock[i].partId != item.PartId) continue;
                if (item.Category != PartCategory.Body) return;
                stock[i] = new ShopStockSave
                {
                    partId = item.PartId,
                    quantity = Math.Max(1, stock[i].quantity) + 1,
                    seen = true
                };
                return;
            }

            stock.Add(new ShopStockSave
            {
                partId = item.PartId,
                quantity = item.Category == PartCategory.Body ? 1 : 0,
                seen = true
            });
        }

        private bool Contains(int blueprintIndex, int mountedIndex)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].BlueprintIndex == blueprintIndex && _items[i].MountedIndex == mountedIndex)
                    return true;
            }

            return false;
        }

        private static int RepairCost(int baseCost)
        {
            if (baseCost <= 0) return 0;
            return (int)((long)baseCost * RepairPercent / 100L);
        }

        private static string CraftName(BlueprintSaveData blueprint, int index)
        {
            if (!string.IsNullOrEmpty(blueprint.craftName)) return blueprint.craftName;
            if (!string.IsNullOrEmpty(blueprint.hullName)) return blueprint.hullName;
            return "Craft " + (index + 1).ToString(CultureInfo.InvariantCulture);
        }

        private static int CompareItems(RepairItem left, RepairItem right)
        {
            int byCraft = string.CompareOrdinal(left.CraftName, right.CraftName);
            if (byCraft != 0) return byCraft;
            int byBlueprint = left.BlueprintIndex.CompareTo(right.BlueprintIndex);
            return byBlueprint != 0 ? byBlueprint : left.MountedIndex.CompareTo(right.MountedIndex);
        }
    }
}
