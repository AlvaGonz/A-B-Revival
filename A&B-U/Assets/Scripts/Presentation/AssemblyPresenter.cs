using System;
using System.Collections.Generic;
using ABRevival.Core.Data;
using ABRevival.Core.Shop;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// UI Toolkit presenter for the frozen hangar assembly screen: craft preview, right-hand
    /// stat card, owned/mounted parts table, category filters and the original craft buttons.
    /// All construction validation goes through <see cref="CraftAssembly"/> (ADR 0014).
    /// </summary>
    public sealed class AssemblyPresenter
    {
        private const float CanvasWidth = 564f;
        private const float CanvasHeight = 458f;
        private const float PreviewFill = 0.7f;
        private const float PreviewMinScale = 0.15f;
        private const float PreviewMaxScale = 24f;

        private readonly ShopState _shop;
        private readonly Dictionary<int, PartDefinitionSO> _byId = new Dictionary<int, PartDefinitionSO>();
        private readonly Action _onChanged;

        private readonly Label _statType;
        private readonly Label _statName;
        private readonly Label _statHp;
        private readonly Label _statStrength;
        private readonly Label _statEngine;
        private readonly Label _statFire;
        private readonly Label _statMass;
        private readonly Label _statValue;
        private readonly Label _hullLabel;
        private readonly Label _wingLabel;
        private readonly Label _errorLabel;
        private readonly Label _previewName;
        private readonly VisualElement _previewCanvas;
        private readonly VisualElement _previewClip;
        private readonly ScrollView _partsList;
        private readonly ScrollView _craftsList;
        private readonly Button _mergeButton;
        private readonly Button _newButton;
        private readonly Button _backToCenterButton;
        private readonly Button _partsCraftsButton;
        private readonly List<Button> _tabs = new List<Button>();

        private CraftAssembly _assembly;
        private CraftBlueprintSO _blueprint;
        private PartCategory? _filter;
        private bool _showCrafts;
        private int _selectedPartId = -1;
        private bool _panning;
        private Vector2 _pan;
        private Vector2 _panStart;
        private Vector2 _panOrigin;
        private float _zoom = 1f;

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

            if (definitions != null)
            {
                for (int i = 0; i < definitions.Count; i++)
                {
                    PartDefinitionSO definition = definitions[i];
                    if (definition != null) _byId[definition.partId] = definition;
                }
            }

            _statType = root.Q<Label>("stat-type");
            _statName = root.Q<Label>("stat-name");
            _statHp = root.Q<Label>("stat-hp");
            _statStrength = root.Q<Label>("stat-strength");
            _statEngine = root.Q<Label>("stat-engine");
            _statFire = root.Q<Label>("stat-fire");
            _statMass = root.Q<Label>("stat-mass");
            _statValue = root.Q<Label>("stat-value");
            _hullLabel = root.Q<Label>("hull-label");
            _wingLabel = root.Q<Label>("wing-label");
            _errorLabel = root.Q<Label>("assembly-error");
            _previewName = root.Q<Label>("preview-name");
            _previewCanvas = root.Q<VisualElement>("preview-canvas");
            _previewClip = root.Q<VisualElement>("preview-clip");
            _partsList = root.Q<ScrollView>("owned-parts-list");
            _craftsList = root.Q<ScrollView>("crafts-list");

            _mergeButton = root.Q<Button>("merge-button");
            _newButton = root.Q<Button>("new-button");
            _backToCenterButton = root.Q<Button>("back-to-center-button");
            _partsCraftsButton = root.Q<Button>("parts-crafts-button");

            if (_mergeButton != null) _mergeButton.clicked += MergeSelected;
            if (_newButton != null) _newButton.clicked += NewCraft;
            if (_backToCenterButton != null) _backToCenterButton.clicked += CenterPreview;
            if (_partsCraftsButton != null) _partsCraftsButton.clicked += ToggleCrafts;

            BindTabs(root);
            BindPreviewInput();
            if (_partsList != null) _partsList.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            if (_craftsList != null)
            {
                _craftsList.verticalScrollerVisibility = ScrollerVisibility.Hidden;
                _craftsList.style.display = DisplayStyle.None;
            }
        }

        public bool ShowingCrafts => _showCrafts;

        public float PreviewZoom => _zoom;

        public int VisibleRowCount { get; private set; }

        public void Rebind(CraftAssembly assembly, CraftBlueprintSO blueprint)
        {
            _assembly = assembly;
            _blueprint = blueprint;
            Refresh();
        }

        public void Refresh()
        {
            if (_assembly == null) return;

            RefreshStats();
            RefreshTabs();
            RefreshPreview();
            BuildPartsTable();
            BuildCraftsTable();
        }

        /// <summary>Selects an owned part for the standalone MERGE button (also used by tests).</summary>
        public void SelectPart(int partId)
        {
            _selectedPartId = partId;
        }

        private void BindTabs(VisualElement root)
        {
            BindTab(root, "tab-all", null);
            BindTab(root, "tab-body", PartCategory.Body);
            BindTab(root, "tab-wing", PartCategory.Wing);
            BindTab(root, "tab-engine", PartCategory.Engine);
            BindTab(root, "tab-gun", PartCategory.Gun);
        }

        private void BindTab(VisualElement root, string name, PartCategory? category)
        {
            Button button = root.Q<Button>(name);
            if (button == null) return;
            _tabs.Add(button);
            button.clicked += () => SetFilter(category);
        }

        private void SetFilter(PartCategory? category)
        {
            _filter = category;
            RefreshTabs();
            BuildPartsTable();
        }

        private void RefreshTabs()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                Button tab = _tabs[i];
                tab.RemoveFromClassList("tab-active");
                if (!_showCrafts && MatchesFilter(TabCategory(tab.name)))
                {
                    tab.AddToClassList("tab-active");
                }
            }

            if (_partsCraftsButton != null)
            {
                _partsCraftsButton.AddToClassList("tab-active");
            }
        }

        private static PartCategory? TabCategory(string name)
        {
            switch (name)
            {
                case "tab-body": return PartCategory.Body;
                case "tab-wing": return PartCategory.Wing;
                case "tab-engine": return PartCategory.Engine;
                case "tab-gun": return PartCategory.Gun;
                default: return null;
            }
        }

        private bool MatchesFilter(PartCategory? category)
        {
            return _filter == null || category == _filter;
        }

        private void RefreshStats()
        {
            AggregateCraftStats stats = _assembly.CurrentStats;
            float firePower = 0f;
            int value = 0;

            for (int i = 0; i < CraftAssembly.MaxCapacity; i++)
            {
                if (!_assembly.IsSlotOccupied(i)) continue;
                MountedPartData part = _assembly.GetPart(i);
                if (!_byId.TryGetValue(part.partId, out PartDefinitionSO definition)) continue;
                value += definition.cost;
                if (definition.category == PartCategory.Gun)
                {
                    firePower += definition.bulletDamage * Mathf.Max(1, definition.fireNum);
                }
            }

            _statType.text = _assembly.HullCategory.ToString().ToUpperInvariant();
            _statName.text = string.IsNullOrEmpty(_blueprint?.craftName)
                ? _assembly.HullName
                : _blueprint.craftName;
            _statHp.text = $"{stats.totalHealth:0}";
            _statStrength.text = $"{_assembly.WingCount}/{_assembly.MaxWingCapacity}";
            _statEngine.text = $"{stats.totalThrust:0}";
            _statFire.text = $"{firePower:0.0}";
            _statMass.text = $"{stats.totalMass:0}";
            _statValue.text = value.ToString();

            _hullLabel.text = _assembly.HullName;
            _wingLabel.text = $"Wings {_assembly.WingCount}/{_assembly.MaxWingCapacity}";
            _wingLabel.RemoveFromClassList("stat-warning");
            _wingLabel.RemoveFromClassList("stat-danger");
            if (_assembly.IsOverWingCapacity)
            {
                _wingLabel.AddToClassList("stat-danger");
            }
            else if (_assembly.WingCount == _assembly.MaxWingCapacity)
            {
                _wingLabel.AddToClassList("stat-warning");
            }

            if (_previewName != null)
            {
                _previewName.text = string.IsNullOrEmpty(_blueprint?.craftName)
                    ? _assembly.HullName
                    : _blueprint.craftName;
            }
        }

        private void RefreshPreview()
        {
            if (_previewCanvas == null) return;
            _previewCanvas.Clear();

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            for (int i = 0; i < CraftAssembly.MaxCapacity; i++)
            {
                if (!_assembly.IsSlotOccupied(i)) continue;
                MountedPartData part = _assembly.GetPart(i);
                if (!_byId.TryGetValue(part.partId, out PartDefinitionSO definition)) continue;
                Sprite sprite = definition.partSprite;
                if (sprite == null) continue;

                Vector2 contentCenter = ContentCenter(definition, sprite);
                Vector2 contentSize = ContentSize(definition, sprite);
                float x = part.localOffset.x + contentCenter.x;
                float y = part.localOffset.y + contentCenter.y;
                minX = Mathf.Min(minX, x - (contentSize.x * 0.5f));
                maxX = Mathf.Max(maxX, x + (contentSize.x * 0.5f));
                minY = Mathf.Min(minY, y - (contentSize.y * 0.5f));
                maxY = Mathf.Max(maxY, y + (contentSize.y * 0.5f));
            }

            if (minX > maxX) return;

            float boundsWidth = Mathf.Max(1f, maxX - minX);
            float boundsHeight = Mathf.Max(1f, maxY - minY);
            float scale = Mathf.Min(
                CanvasWidth * PreviewFill / boundsWidth,
                CanvasHeight * PreviewFill / boundsHeight);
            scale = Mathf.Clamp(scale, PreviewMinScale, PreviewMaxScale);

            float centerX = CanvasWidth * 0.5f;
            float centerY = CanvasHeight * 0.5f;
            float boundsCenterX = (minX + maxX) * 0.5f;
            float boundsCenterY = (minY + maxY) * 0.5f;

            for (int i = 0; i < CraftAssembly.MaxCapacity; i++)
            {
                if (!_assembly.IsSlotOccupied(i)) continue;
                MountedPartData part = _assembly.GetPart(i);
                if (!_byId.TryGetValue(part.partId, out PartDefinitionSO definition)) continue;
                Sprite sprite = definition.partSprite;
                if (sprite == null) continue;

                float width = sprite.rect.width * scale;
                float height = sprite.rect.height * scale;
                var element = new VisualElement { name = "preview-part-" + part.partId };
                element.style.position = Position.Absolute;
                element.style.width = width;
                element.style.height = height;
                element.style.left = centerX + ((part.localOffset.x - boundsCenterX) * scale) - (width * 0.5f);
                element.style.top = centerY - ((part.localOffset.y - boundsCenterY) * scale) - (height * 0.5f);
                element.style.backgroundImage = new StyleBackground(sprite);
                element.style.rotate = new StyleRotate(
                    new Rotate(new Angle(part.localRotation * Mathf.Rad2Deg, AngleUnit.Degree)));
                _previewCanvas.Add(element);
            }
        }

        private void BuildPartsTable()
        {
            if (_partsList == null) return;
            _partsList.Clear();
            VisibleRowCount = 0;

            for (int i = 0; i < CraftAssembly.MaxCapacity; i++)
            {
                if (!_assembly.IsSlotOccupied(i)) continue;
                MountedPartData part = _assembly.GetPart(i);
                _byId.TryGetValue(part.partId, out PartDefinitionSO definition);
                if (definition != null && !MatchesFilter(definition.category)) continue;
                _partsList.Add(CreatePartRow(definition, part, i, mounted: true));
                VisibleRowCount++;
            }

            IReadOnlyList<int> owned = _shop.OwnedParts;
            for (int i = 0; i < owned.Count; i++)
            {
                if (!_byId.TryGetValue(owned[i], out PartDefinitionSO definition)) continue;
                if (!MatchesFilter(definition.category)) continue;
                MountedPartData part = FromDefinition(definition);
                _partsList.Add(CreatePartRow(definition, part, -1, mounted: false));
                VisibleRowCount++;
            }

            if (VisibleRowCount == 0)
            {
                _partsList.Add(new Label("NO PARTS") { name = "parts-empty" });
            }

            _partsList.style.display = _showCrafts ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private VisualElement CreatePartRow(
            PartDefinitionSO definition, MountedPartData part, int socketIndex, bool mounted)
        {
            var row = new VisualElement();
            row.AddToClassList("row");
            row.AddToClassList(VisibleRowCount % 2 == 0 ? "row-even" : "row-odd");
            AddCell(row, definition != null ? definition.partId.ToString() : part.partId.ToString(), "col-index");
            AddCell(row, definition != null ? definition.partName : $"Part {part.partId}", "col-name");
            AddCell(row, CategoryLabel(part.category), "col-type");
            AddCell(row, definition != null ? LeftRightLabel(definition.lr) : "-", "col-lr");
            AddCell(row, definition != null ? definition.health.ToString("0") : part.health.ToString("0"), "col-hp");
            AddCell(row, definition != null
                ? (definition.turnFly * Mathf.Rad2Deg).ToString("0.0")
                : "0.0", "col-steering");
            AddCell(row, definition != null ? EngineColumn(definition) : part.thrust.ToString("0"), "col-engine");
            AddCell(row, definition != null ? definition.mass.ToString("0") : part.mass.ToString("0"), "col-mass");
            AddCell(row, definition != null ? FireColumn(definition) : "0", "col-fire");

            int capturedId = definition != null ? definition.partId : part.partId;
            if (mounted)
            {
                int capturedSlot = socketIndex;
                var breakButton = new Button(() => Unmount(capturedSlot)) { text = "BREAK" };
                breakButton.AddToClassList("row-action");
                row.Add(breakButton);
            }
            else
            {
                var merge = new Button(() => Mount(capturedId)) { text = "MERGE" };
                merge.AddToClassList("row-action");
                row.Add(merge);
            }

            row.RegisterCallback<PointerDownEvent>(_ => SelectPart(capturedId));
            return row;
        }

        private void BuildCraftsTable()
        {
            if (_craftsList == null) return;
            _craftsList.Clear();

            var row = new VisualElement();
            row.AddToClassList("row");
            row.AddToClassList("row-even");
            AddCell(row, "0", "col-index");
            AddCell(row, string.IsNullOrEmpty(_blueprint?.craftName) ? _assembly.HullName : _blueprint.craftName, "col-name");
            AddCell(row, _assembly.HullCategory.ToString().ToUpperInvariant(), "col-type");
            AddCell(row, "-", "col-lr");
            AddCell(row, _assembly.CurrentStats.totalHealth.ToString("0"), "col-hp");
            AddCell(row, "-", "col-steering");
            AddCell(row, _assembly.CurrentStats.totalThrust.ToString("0"), "col-engine");
            AddCell(row, _assembly.CurrentStats.totalMass.ToString("0"), "col-mass");
            AddCell(row, "0", "col-fire");
            var edit = new Button(ToggleCrafts) { text = "EDIT" };
            edit.AddToClassList("row-action");
            row.Add(edit);
            _craftsList.Add(row);

            _craftsList.style.display = _showCrafts ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void MergeSelected()
        {
            if (_selectedPartId < 0) return;
            Mount(_selectedPartId);
        }

        private void Mount(int partId)
        {
            ClearError();
            if (!_byId.TryGetValue(partId, out PartDefinitionSO definition))
            {
                SetError("Unknown part.");
                return;
            }

            if (!_shop.TryTakeOwnedPart(partId))
            {
                SetError("Part is not owned.");
                return;
            }

            MountedPartData part = FromDefinition(definition);
            part.localOffset = DefaultOffset(definition.category, _assembly.PartCount);

            AssemblyError error = _assembly.TryMountPart(part, out _);
            if (error != AssemblyError.None)
            {
                _shop.AddOwnedPart(partId);
                SetError(error.ToString());
                return;
            }

            _selectedPartId = -1;
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

        private void NewCraft()
        {
            ClearError();
            for (int i = 0; i < CraftAssembly.MaxCapacity; i++)
            {
                if (!_assembly.IsSlotOccupied(i)) continue;
                MountedPartData part = _assembly.GetPart(i);
                if (part.category == PartCategory.Body) continue;
                if (_assembly.UnmountPart(i, out MountedPartData removed)) _shop.AddOwnedPart(removed.partId);
            }

            _selectedPartId = -1;
            CaptureBlueprint();
            _onChanged?.Invoke();
        }

        private void ToggleCrafts()
        {
            _showCrafts = !_showCrafts;
            RefreshTabs();
            BuildPartsTable();
            BuildCraftsTable();
        }

        private void BindPreviewInput()
        {
            if (_previewClip == null) return;
            _previewClip.RegisterCallback<PointerDownEvent>(OnPreviewPointerDown);
            _previewClip.RegisterCallback<PointerMoveEvent>(OnPreviewPointerMove);
            _previewClip.RegisterCallback<PointerUpEvent>(OnPreviewPointerUp);
            _previewClip.RegisterCallback<WheelEvent>(OnPreviewWheel);
        }

        private void OnPreviewPointerDown(PointerDownEvent evt)
        {
            _panning = true;
            _panStart = evt.position;
            _panOrigin = _pan;
            _previewClip.CapturePointer(evt.pointerId);
        }

        private void OnPreviewPointerMove(PointerMoveEvent evt)
        {
            if (!_panning) return;
            _pan = _panOrigin + (Vector2)evt.position - _panStart;
            ApplyPreviewTransform();
        }

        private void OnPreviewPointerUp(PointerUpEvent evt)
        {
            _panning = false;
            if (_previewClip.HasPointerCapture(evt.pointerId)) _previewClip.ReleasePointer(evt.pointerId);
        }

        private void OnPreviewWheel(WheelEvent evt)
        {
            _zoom = Mathf.Clamp(_zoom * (evt.delta.y > 0f ? 1.1f : 0.9f), 0.5f, 3f);
            ApplyPreviewTransform();
            evt.StopPropagation();
        }

        private void CenterPreview()
        {
            _pan = Vector2.zero;
            _zoom = 1f;
            ApplyPreviewTransform();
        }

        private void ApplyPreviewTransform()
        {
            if (_previewCanvas == null) return;
            _previewCanvas.style.translate = new StyleTranslate(new Translate(_pan.x, _pan.y));
            _previewCanvas.style.scale = new StyleScale(new Scale(new Vector3(_zoom, _zoom, 1f)));
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

        private static MountedPartData FromDefinition(PartDefinitionSO definition)
        {
            return new MountedPartData
            {
                partId = definition.partId,
                category = definition.category,
                health = definition.health,
                maxHealth = definition.health,
                mass = definition.mass,
                thrust = definition.thrust,
                energyDraw = definition.energyDraw
            };
        }

        private static Vector2 ContentCenter(PartDefinitionSO definition, Sprite sprite)
        {
            Vector4 content = definition.spriteContentRect;
            if (content.z <= 0f || content.w <= 0f) return Vector2.zero;
            return new Vector2(
                content.x + (content.z * 0.5f) - (sprite.rect.width * 0.5f),
                content.y + (content.w * 0.5f) - (sprite.rect.height * 0.5f));
        }

        private static Vector2 ContentSize(PartDefinitionSO definition, Sprite sprite)
        {
            Vector4 content = definition.spriteContentRect;
            if (content.z <= 0f || content.w <= 0f) return sprite.rect.size;
            return new Vector2(content.z, content.w);
        }

        private static float2 DefaultOffset(PartCategory category, int slotIndex)
        {
            switch (category)
            {
                case PartCategory.Body: return float2.zero;
                case PartCategory.Wing: return new float2(slotIndex % 2 == 0 ? -22f : 22f, 0f);
                case PartCategory.Engine: return new float2(0f, 10f);
                case PartCategory.Gun: return new float2(0f, -18f);
                default: return float2.zero;
            }
        }

        private static void AddCell(VisualElement row, string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList("col");
            label.AddToClassList(className);
            row.Add(label);
        }

        private static string CategoryLabel(PartCategory category)
        {
            switch (category)
            {
                case PartCategory.Body: return "Body";
                case PartCategory.Wing: return "Wing";
                case PartCategory.Engine: return "Eng.";
                case PartCategory.Gun: return "Gun";
                default: return "?";
            }
        }

        private static string LeftRightLabel(int lr)
        {
            return lr == 0 ? "L" : lr == 1 ? "R" : "LR";
        }

        private static string EngineColumn(PartDefinitionSO definition)
        {
            return definition.category == PartCategory.Gun
                ? definition.CalculateEffectiveRange().ToString("0")
                : definition.thrust.ToString("0.0");
        }

        private static string FireColumn(PartDefinitionSO definition)
        {
            return definition.category == PartCategory.Gun
                ? (definition.bulletDamage * Mathf.Max(1, definition.fireNum)).ToString("0.0")
                : "0";
        }

        private void SetError(string message)
        {
            if (_errorLabel != null) _errorLabel.text = message;
        }

        private void ClearError()
        {
            if (_errorLabel != null) _errorLabel.text = string.Empty;
        }
    }
}
