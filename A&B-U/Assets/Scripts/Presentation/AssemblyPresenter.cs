using System;
using System.Collections.Generic;
using ABRevival.Core.Data;
using ABRevival.Core.Shop;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// UI Toolkit presenter for the hangar assembly panel. All construction
    /// validation goes through <see cref="CraftAssembly"/>; this class only
    /// binds controls and renders the live stat readout (ADR 0014).
    /// </summary>
    public sealed class AssemblyPresenter
    {
        private readonly ShopState _shop;
        private readonly List<PartDefinitionSO> _definitions;
        private readonly Dictionary<int, string> _partNames = new Dictionary<int, string>();
        private readonly Action _onChanged;

        private readonly Label _hullLabel;
        private readonly Label _wingLabel;
        private readonly Label _statsLabel;
        private readonly Label _errorLabel;
        private readonly ScrollView _ownedList;
        private readonly ScrollView _mountedList;

        private CraftAssembly _assembly;
        private CraftBlueprintSO _blueprint;

        public AssemblyPresenter(
            VisualElement root,
            CraftAssembly assembly,
            CraftBlueprintSO blueprint,
            IReadOnlyList<PartDefinitionSO> definitions,
            ShopState shop,
            Action onChanged)
        {
            _assembly = assembly;
            _blueprint = blueprint;
            _shop = shop;
            _onChanged = onChanged;
            _definitions = new List<PartDefinitionSO>();
            if (definitions != null) _definitions.AddRange(definitions);

            for (int i = 0; i < _definitions.Count; i++)
            {
                PartDefinitionSO definition = _definitions[i];
                if (definition != null) _partNames[definition.partId] = definition.partName;
            }

            _hullLabel = root.Q<Label>("hull-label");
            _wingLabel = root.Q<Label>("wing-label");
            _statsLabel = root.Q<Label>("stats-label");
            _errorLabel = root.Q<Label>("assembly-error");
            _ownedList = root.Q<ScrollView>("owned-parts-list");
            _mountedList = root.Q<ScrollView>("mounted-parts-list");
        }

        public void Rebind(CraftAssembly assembly, CraftBlueprintSO blueprint)
        {
            _assembly = assembly;
            _blueprint = blueprint;
            Refresh();
        }

        public void Refresh()
        {
            if (_assembly == null) return;

            int limit = _assembly.MaxWingCapacity;
            _hullLabel.text = $"Hull: {_assembly.HullName}  [{_assembly.HullCategory}]";
            _wingLabel.text = $"Wings: {_assembly.WingCount}/{limit}";
            _wingLabel.RemoveFromClassList("stat-warning");
            _wingLabel.RemoveFromClassList("stat-danger");

            if (_assembly.WingCount > limit)
            {
                _wingLabel.AddToClassList("stat-danger");
            }
            else if (_assembly.WingCount == limit)
            {
                _wingLabel.AddToClassList("stat-warning");
            }

            AggregateCraftStats stats = _assembly.CurrentStats;
            _statsLabel.text =
                $"Hp {stats.totalHealth:0}   Mass {stats.totalMass:0}   Thrust {stats.totalThrust:0}   Energy {stats.totalEnergyDraw:0}";

            BuildOwnedList();
            BuildMountedList();
        }

        private void BuildOwnedList()
        {
            _ownedList.Clear();

            int shown = 0;
            for (int i = 0; i < _definitions.Count; i++)
            {
                PartDefinitionSO definition = _definitions[i];
                if (definition == null) continue;

                int owned = _shop.OwnedCount(definition.partId);
                if (owned <= 0) continue;

                shown++;
                var row = new VisualElement();
                row.AddToClassList("row");
                row.Add(new Label($"{definition.partName}  x{owned}") { name = "owned-name" });

                PartDefinitionSO captured = definition;
                var mount = new Button(() => Mount(captured)) { text = "Mount" };
                mount.AddToClassList("row-action");
                row.Add(mount);
                _ownedList.Add(row);
            }

            if (shown == 0) _ownedList.Add(new Label("No owned parts.") { name = "owned-empty" });
        }

        private void BuildMountedList()
        {
            _mountedList.Clear();

            for (int i = 0; i < CraftAssembly.MaxCapacity; i++)
            {
                if (!_assembly.IsSlotOccupied(i)) continue;

                MountedPartData part = _assembly.GetPart(i);
                string name = _partNames.TryGetValue(part.partId, out string registered)
                    ? registered
                    : $"Part {part.partId}";

                var row = new VisualElement();
                row.AddToClassList("row");
                row.Add(new Label($"{name}  [slot {i}]"));

                int capturedIndex = i;
                var unmount = new Button(() => Unmount(capturedIndex)) { text = "Unmount" };
                unmount.AddToClassList("row-action");
                row.Add(unmount);
                _mountedList.Add(row);
            }

            if (_assembly.PartCount == 0)
            {
                _mountedList.Add(new Label("No mounted parts.") { name = "mounted-empty" });
            }
        }

        private void Mount(PartDefinitionSO definition)
        {
            ClearError();

            if (!_shop.TryTakeOwnedPart(definition.partId))
            {
                SetError("Part is not owned.");
                return;
            }

            MountedPartData part = new MountedPartData
            {
                partId = definition.partId,
                category = definition.category,
                health = definition.health,
                maxHealth = definition.health,
                mass = definition.mass,
                thrust = definition.thrust,
                energyDraw = definition.energyDraw
            };

            AssemblyError error = _assembly.TryMountPart(part, out _);
            if (error != AssemblyError.None)
            {
                _shop.AddOwnedPart(definition.partId);
                SetError(error.ToString());
                return;
            }

            CaptureBlueprint();
            _onChanged?.Invoke();
        }

        private void Unmount(int socketIndex)
        {
            ClearError();

            if (!_assembly.UnmountPart(socketIndex, out MountedPartData removed)) return;

            _shop.AddOwnedPart(removed.partId);
            CaptureBlueprint();
            _onChanged?.Invoke();
        }

        private void CaptureBlueprint()
        {
            if (_blueprint == null) return;

            var mounted = new List<MountedPartData>();
            for (int i = 0; i < CraftAssembly.MaxCapacity; i++)
            {
                if (_assembly.IsSlotOccupied(i)) mounted.Add(_assembly.GetPart(i));
            }

            _blueprint.mountedParts = mounted.ToArray();
        }

        private void SetError(string message)
        {
            if (_errorLabel != null) _errorLabel.text = message;
            Refresh();
        }

        private void ClearError()
        {
            if (_errorLabel != null) _errorLabel.text = string.Empty;
        }
    }
}
