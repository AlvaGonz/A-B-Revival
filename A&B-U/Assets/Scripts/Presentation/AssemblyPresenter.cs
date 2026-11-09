using System;
using System.Collections.Generic;
using System.Globalization;
using ABRevival.Core.Data;
using ABRevival.Core.Shop;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// UI Toolkit presenter for the frozen Factory assembly form (FormPlayerPart):
    /// 2x craft preview, the right-hand F4ShowData stat card, the small craft stats card
    /// with the wing counter, the owned/mounted parts table, category filters, the
    /// removed-parts undo strip and preview pan/zoom.
    /// All construction validation goes through <see cref="CraftAssembly"/> (ADR 0014).
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class AssemblyPresenter
    {
        public const int PageSize = 9;
        public const int UndoSlots = 12;

        private const float DefaultCanvasWidth = 564f;
        private const float DefaultCanvasHeight = 458f;
        private const float PreviewAreaFill = 0.65f;
        private const float PreviewMaxDimensionFill = 0.94f;
        private const float MinZoom = 0.5f;
        private const float MaxZoom = 4f;

        private readonly ShopState _shop;
        private readonly PartMountTable _mounts;
        private readonly MountArtLookup _mountArt;
        private readonly IReadOnlyDictionary<int, PartDefinitionSO> _byId;
        private readonly Action _onChanged;

        private readonly Label _statName;
        private readonly Label _statHp;
        private readonly Label _statFire;
        private readonly Label _statSpeed;
        private readonly Label _statSteering;
        private readonly Label _statClass;
        private readonly Label _statParts;
        private readonly Label _statValue;
        private readonly Label _detailHp;
        private readonly Label _detailSteering;
        private readonly Label _detailSpeed;
        private readonly Label _detailFire;
        private readonly Label _detailParts;
        private readonly Label _hullLabel;
        private readonly Label _wingLabel;
        private readonly Label _errorLabel;
        private readonly Label _previewName;
        private readonly VisualElement _previewCanvas;
        private readonly VisualElement _previewClip;
        private readonly ScrollView _partsList;
        private readonly List<Button> _tabs = new List<Button>();
        private readonly Button[] _undoButtons = new Button[UndoSlots];
        private readonly int[] _undoPartIds = new int[UndoSlots];

        private CraftAssembly _assembly;
        private CraftBlueprintSO _blueprint;
        private PartCategory? _filter;
        private int _selectedPartId = -1;
        private int _page;
        private int _visibleRows;
        private int _builtRows;
        private bool _panning;
        private Vector2 _pan;
        private Vector2 _panStart;
        private Vector2 _panOrigin;
        private float _zoom = 1f;
        private Vector2 _lastPreviewSize;

        public AssemblyPresenter(
            VisualElement root,
            CraftAssembly assembly,
            CraftBlueprintSO blueprint,
            IReadOnlyList<PartDefinitionSO> definitions,
            ShopState shop,
            PartMountTable mounts,
            MountArtLookup mountArt,
            Action onChanged)
        {
            _assembly = assembly;
            _blueprint = blueprint;
            _shop = shop;
            _onChanged = onChanged;
            _mounts = mounts;
            _mountArt = mountArt;

            var byId = new Dictionary<int, PartDefinitionSO>();
            if (definitions != null)
            {
                for (int i = 0; i < definitions.Count; i++)
                {
                    PartDefinitionSO definition = definitions[i];
                    if (definition != null) byId[definition.partId] = definition;
                }
            }

            _byId = byId;

            _statName = root.Q<Label>("stat-name");
            _statHp = root.Q<Label>("stat-hp");
            _statFire = root.Q<Label>("stat-fire");
            _statSpeed = root.Q<Label>("stat-speed");
            _statSteering = root.Q<Label>("stat-steering");
            _statClass = root.Q<Label>("stat-class");
            _statParts = root.Q<Label>("stat-parts");
            _statValue = root.Q<Label>("stat-value");
            _detailHp = root.Q<Label>("detail-hp");
            _detailSteering = root.Q<Label>("detail-steering");
            _detailSpeed = root.Q<Label>("detail-speed");
            _detailFire = root.Q<Label>("detail-fire");
            _detailParts = root.Q<Label>("detail-parts");
            _hullLabel = root.Q<Label>("hull-label");
            _wingLabel = root.Q<Label>("wing-label");
            _errorLabel = root.Q<Label>("assembly-error");
            _previewName = root.Q<Label>("preview-name");
            _previewCanvas = root.Q<VisualElement>("preview-canvas");
            _previewClip = root.Q<VisualElement>("preview-clip");
            _partsList = root.Q<ScrollView>("owned-parts-list");

            BindTabs(root);
            BindZoom(root);
            BindUndo(root);
            BindPreviewInput();
            if (_partsList != null) _partsList.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            for (int i = 0; i < UndoSlots; i++) _undoPartIds[i] = -1;
        }

        /// <summary>Raised after any action that changes the visible table or footer.</summary>
        public event Action ViewChanged;

        public int VisibleRowCount => _visibleRows;

        public int PageIndex => _page;

        public int PageCount => Mathf.Max(1, (_visibleRows + PageSize - 1) / PageSize);

        public float PreviewZoom => _zoom;

        public bool PreviewUsesMountArt { get; private set; }

        public Vector2 PreviewBoundsSize { get; private set; }

        public float PreviewFillFraction { get; private set; }

        public void Rebind(CraftAssembly assembly, CraftBlueprintSO blueprint)
        {
            _assembly = assembly;
            _blueprint = blueprint;
            _selectedPartId = -1;
            _page = 0;
            Refresh();
        }

        public void SetPartsVisible(bool visible)
        {
            if (_partsList != null) _partsList.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void Refresh()
        {
            if (_assembly == null) return;

            RefreshStats();
            RefreshTabs();
            RefreshPreview();
            BuildPartsTable();
            RefreshUndo();
            ViewChanged?.Invoke();
        }

        /// <summary>Selects an owned part for the standalone MERGE button (also used by tests).</summary>
        public void SelectPart(int partId)
        {
            _selectedPartId = partId;
            BuildPartsTable();
            RefreshPreview();
        }

        /// <summary>Selects a category tab; null selects All. Public so tests can drive the tabs.</summary>
        public void SelectCategory(PartCategory? category)
        {
            _filter = category;
            _page = 0;
            RefreshTabs();
            BuildPartsTable();
            ViewChanged?.Invoke();
        }

        public void FirstPage() => GoToPage(0);
        public void PreviousPage() => GoToPage(_page - 1);
        public void NextPage() => GoToPage(_page + 1);
        public void LastPage() => GoToPage(PageCount - 1);

        public void GoToPage(int page)
        {
            int clamped = Mathf.Clamp(page, 0, PageCount - 1);
            if (clamped == _page) return;
            _page = clamped;
            BuildPartsTable();
            ViewChanged?.Invoke();
        }

        public void ResetView()
        {
            _pan = Vector2.zero;
            _zoom = 1f;
            ApplyPreviewTransform();
        }

        public void ZoomIn() => SetZoom(_zoom * 1.25f);
        public void ZoomOut() => SetZoom(_zoom * 0.75f);

        /// <summary>
        /// Breaks every non-chassis part back to inventory (the frozen NEW flow). The chassis
        /// stays mounted so the blueprint remains a valid craft.
        /// </summary>
        public void NewCraft()
        {
            ClearError();
            bool changed = false;
            for (int i = 0; i < CraftAssembly.MaxCapacity; i++)
            {
                if (!_assembly.IsSlotOccupied(i)) continue;
                MountedPartData part = _assembly.GetPart(i);
                if (part.category == PartCategory.Body) continue;
                if (!_assembly.UnmountPart(i, out MountedPartData removed)) continue;
                _shop.AddOwnedPart(removed.partId);
                PushUndo(removed.partId);
                changed = true;
            }

            if (!changed) return;
            _selectedPartId = -1;
            _page = 0;
            CaptureBlueprint();
            Refresh();
            _onChanged?.Invoke();
        }

        // ---------------- Binding ----------------

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
            button.clicked += () => SelectCategory(category);
        }

        private void BindZoom(VisualElement root)
        {
            Button zoomIn = root.Q<Button>("zoom-in-button");
            Button zoomOut = root.Q<Button>("zoom-out-button");
            Button center = root.Q<Button>("center-view-button");
            if (zoomIn != null) zoomIn.clicked += ZoomIn;
            if (zoomOut != null) zoomOut.clicked += ZoomOut;
            if (center != null) center.clicked += ResetView;
        }

        private void BindUndo(VisualElement root)
        {
            for (int i = 0; i < UndoSlots; i++)
            {
                Button button = root.Q<Button>("undo-" + i);
                if (button == null) continue;
                int slot = i;
                button.clicked += () => ReAdd(slot);
                _undoButtons[i] = button;
            }
        }

        private void BindPreviewInput()
        {
            if (_previewClip == null) return;
            _previewClip.RegisterCallback<PointerDownEvent>(OnPreviewPointerDown);
            _previewClip.RegisterCallback<PointerMoveEvent>(OnPreviewPointerMove);
            _previewClip.RegisterCallback<PointerUpEvent>(OnPreviewPointerUp);
            _previewClip.RegisterCallback<WheelEvent>(OnPreviewWheel);
            _previewClip.RegisterCallback<GeometryChangedEvent>(OnPreviewGeometryChanged);
        }

        private void RefreshTabs()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                Button tab = _tabs[i];
                tab.RemoveFromClassList("tab-active");
                if (TabCategory(tab.name) == _filter) tab.AddToClassList("tab-active");
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

        // ---------------- Stats ----------------

        private void RefreshStats()
        {
            CraftShowStats stats = EvaluateStats(_assembly, _blueprint);

            if (_statName != null) _statName.text = stats.name;
            if (_statHp != null) _statHp.text = stats.hp.ToString(CultureInfo.InvariantCulture);
            if (_statFire != null) _statFire.text = stats.fire.ToString("0.00", CultureInfo.InvariantCulture);
            if (_statSpeed != null) _statSpeed.text = stats.speed.ToString("0.0", CultureInfo.InvariantCulture);
            if (_statSteering != null) _statSteering.text = stats.steering.ToString("0.0", CultureInfo.InvariantCulture);
            if (_statClass != null) _statClass.text = stats.cls;
            if (_statParts != null) _statParts.text = stats.parts.ToString(CultureInfo.InvariantCulture);
            if (_statValue != null) _statValue.text = stats.value.ToString(CultureInfo.InvariantCulture);

            if (_detailHp != null) _detailHp.text = stats.hp.ToString(CultureInfo.InvariantCulture);
            if (_detailSteering != null) _detailSteering.text = stats.steering.ToString("0.0", CultureInfo.InvariantCulture);
            if (_detailSpeed != null) _detailSpeed.text = stats.speed.ToString("0.0", CultureInfo.InvariantCulture);
            if (_detailFire != null) _detailFire.text = stats.fire.ToString("0.00", CultureInfo.InvariantCulture);
            if (_detailParts != null) _detailParts.text = stats.parts.ToString(CultureInfo.InvariantCulture);

            string hullName = !string.IsNullOrEmpty(_blueprint?.hullName)
                ? _blueprint.hullName
                : _assembly.HullName;
            if (_hullLabel != null) _hullLabel.text = hullName;
            if (_wingLabel != null)
            {
                _wingLabel.text = "Wings " + _assembly.WingCount + "/" + _assembly.MaxWingCapacity;
                _wingLabel.RemoveFromClassList("stat-warning");
                _wingLabel.RemoveFromClassList("stat-danger");
                if (_assembly.IsOverWingCapacity) _wingLabel.AddToClassList("stat-danger");
                else if (_assembly.WingCount == _assembly.MaxWingCapacity) _wingLabel.AddToClassList("stat-warning");
            }

            if (_previewName != null) _previewName.text = stats.name;
        }

        /// <summary>Frozen F4ShowData card values, identical to Center's stat card.</summary>
        public CraftShowStats EvaluateStats(CraftAssembly assembly, CraftBlueprintSO blueprint)
        {
            if (assembly == null) return new CraftShowStats();

            Span<CraftPartStat> parts = stackalloc CraftPartStat[CraftAssembly.MaxCapacity];
            int count = 0;
            for (int i = 0; i < CraftAssembly.MaxCapacity; i++)
            {
                if (!assembly.IsSlotOccupied(i)) continue;
                MountedPartData part = assembly.GetPart(i);
                if (!_byId.TryGetValue(part.partId, out PartDefinitionSO definition)) continue;

                float fire = definition.category == PartCategory.Gun
                    ? definition.CalculateDisplayFirePower(FirePointCount(definition.partId))
                    : 0f;
                parts[count++] = new CraftPartStat(
                    definition.category, definition.mass, definition.health,
                    definition.thrust, definition.turnFly, definition.cost, fire);
            }

            float hullMass = blueprint != null ? blueprint.hullMass : assembly.CurrentStats.totalMass;
            float hullHealth = blueprint != null ? blueprint.hullHealth : assembly.CurrentStats.totalHealth;
            return CraftShowData.Evaluate(
                blueprint != null ? blueprint.craftName : null,
                blueprint != null ? blueprint.hullName : assembly.HullName,
                hullMass, hullHealth, parts.Slice(0, count));
        }

        // ---------------- Preview ----------------

        private void RefreshPreview()
        {
            if (_previewCanvas == null || _previewClip == null || _assembly == null) return;

            Span<MountedPartData> parts = stackalloc MountedPartData[CraftAssembly.MaxCapacity];
            int partCount = CraftPreviewPainter.CollectParts(_assembly, _byId, parts);
            if (partCount == 0)
            {
                _previewCanvas.Clear();
                PreviewUsesMountArt = false;
                PreviewBoundsSize = Vector2.zero;
                PreviewFillFraction = 0f;
                return;
            }

            Span<CraftPartPlacement> placements = stackalloc CraftPartPlacement[CraftAssembly.MaxCapacity];
            int placementCount = CraftPreviewPainter.PlaceParts(
                parts.Slice(0, partCount), _mounts, placements);
            if (placementCount == 0)
            {
                _previewCanvas.Clear();
                return;
            }

            bool useMountArt = CraftPreviewPainter.AllUseMountArt(
                placements.Slice(0, placementCount), _mountArt, out List<int> missing);
            PreviewUsesMountArt = useMountArt;
            if (!useMountArt && missing != null)
            {
                Debug.LogWarning(
                    "[Factory] No mount art for part id(s) " + string.Join(", ", missing) +
                    "; using the shop thumbnail set for the whole preview.");
            }

            float canvasWidth = _previewClip.resolvedStyle.width;
            float canvasHeight = _previewClip.resolvedStyle.height;
            if (float.IsNaN(canvasWidth) || float.IsNaN(canvasHeight) ||
                canvasWidth <= 1f || canvasHeight <= 1f)
            {
                canvasWidth = DefaultCanvasWidth;
                canvasHeight = DefaultCanvasHeight;
            }

            CraftPreviewFit fit = CraftPreviewPainter.Fit(
                placements.Slice(0, placementCount), _byId, _mountArt, useMountArt,
                canvasWidth, canvasHeight, PreviewAreaFill, PreviewMaxDimensionFill);

            PreviewBoundsSize = fit.ScaledSize;
            PreviewFillFraction = canvasWidth * canvasHeight > 0f
                ? PreviewBoundsSize.x * PreviewBoundsSize.y / (canvasWidth * canvasHeight)
                : 0f;

            CraftPreviewPainter.Paint(
                _previewCanvas, placements.Slice(0, placementCount), _byId, _mountArt,
                useMountArt, fit, canvasWidth, canvasHeight, SelectPart);

            PaintSockets(placements.Slice(0, placementCount), fit, canvasWidth, canvasHeight);
        }

        /// <summary>
        /// Shows every free frozen H marker that accepts the selected owned part. Clicking a
        /// marker mounts the part at that exact socket; the default MERGE uses the first free one.
        /// </summary>
        private void PaintSockets(
            ReadOnlySpan<CraftPartPlacement> placements,
            in CraftPreviewFit fit,
            float canvasWidth,
            float canvasHeight)
        {
            if (_selectedPartId < 0 || _mounts == null || _shop == null) return;
            if (fit.scale <= 0f) return;
            if (_shop.OwnedCount(_selectedPartId) <= 0) return;
            if (!_byId.TryGetValue(_selectedPartId, out PartDefinitionSO definition)) return;
            if (!_mounts.TryGetPart(_selectedPartId, out _)) return;

            int bodyIndex = -1;
            for (int i = 0; i < placements.Length; i++)
            {
                if (placements[i].category != PartCategory.Body) continue;
                bodyIndex = i;
                break;
            }

            if (bodyIndex < 0) return;
            CraftPartPlacement body = placements[bodyIndex];
            if (!_mounts.TryGetPart(body.partId, out PartMountEntry bodyEntry)) return;
            if (!_mounts.TryGetSymbol(bodyEntry.mcName, out PartMountSymbol symbol)) return;

            float centerX = canvasWidth * 0.5f;
            float centerY = canvasHeight * 0.5f;
            int wantedLr = definition.lr == 1 ? 1 : 0;

            for (int holeIndex = 0; holeIndex < symbol.holes.Length; holeIndex++)
            {
                PartMountHole hole = symbol.holes[holeIndex];
                if (hole == null || !hole.Accepts(definition.category)) continue;
                if (hole.lr != 2 && hole.lr != wantedLr) continue;
                if (!_mounts.TryResolveSocket(
                        body.partId, holeIndex, definition.partId, body.mirrored, out float2 offset))
                    continue;
                if (IsSocketOccupied(placements, offset)) continue;

                var marker = new VisualElement { name = "preview-socket-" + holeIndex };
                marker.AddToClassList("preview-socket");
                marker.style.left = centerX + ((offset.x - fit.boundsCenterX) * fit.scale) - 9f;
                marker.style.top = centerY - ((offset.y - fit.boundsCenterY) * fit.scale) - 9f;
                bool mirrored = hole.lr == 1;
                marker.RegisterCallback<PointerDownEvent>(evt =>
                {
                    evt.StopPropagation();
                    marker.schedule.Execute(() => MountAt(definition.partId, offset, mirrored));
                });
                _previewCanvas.Add(marker);
            }
        }

        private static bool IsSocketOccupied(
            ReadOnlySpan<CraftPartPlacement> placements, float2 offset)
        {
            for (int i = 0; i < placements.Length; i++)
            {
                if (placements[i].category == PartCategory.Body) continue;
                if (math.lengthsq(placements[i].localOffset - offset) < 1e-4f) return true;
            }

            return false;
        }

        private void SetZoom(float zoom)
        {
            _zoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
            ApplyPreviewTransform();
        }

        private void OnPreviewGeometryChanged(GeometryChangedEvent evt)
        {
            var size = new Vector2(evt.newRect.width, evt.newRect.height);
            if (Mathf.Approximately(size.x, _lastPreviewSize.x) &&
                Mathf.Approximately(size.y, _lastPreviewSize.y))
            {
                return;
            }

            _lastPreviewSize = size;
            RefreshPreview();
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
            SetZoom(_zoom * (evt.delta.y > 0f ? 1.1f : 0.9f));
            evt.StopPropagation();
        }

        private void ApplyPreviewTransform()
        {
            if (_previewCanvas == null) return;
            _previewCanvas.style.translate = new StyleTranslate(new Translate(_pan.x, _pan.y));
            _previewCanvas.style.scale = new StyleScale(new Scale(new Vector3(_zoom, _zoom, 1f)));
        }

        // ---------------- Parts table ----------------

        private readonly struct PartRow
        {
            public readonly PartDefinitionSO definition;
            public readonly MountedPartData part;
            public readonly int socket;
            public readonly bool mounted;

            public PartRow(PartDefinitionSO definition, MountedPartData part, int socket, bool mounted)
            {
                this.definition = definition;
                this.part = part;
                this.socket = socket;
                this.mounted = mounted;
            }
        }

        private void BuildPartsTable()
        {
            if (_partsList == null) return;
            _partsList.Clear();

            var rows = new List<PartRow>(CraftAssembly.MaxCapacity + 16);
            for (int i = 0; i < CraftAssembly.MaxCapacity; i++)
            {
                if (!_assembly.IsSlotOccupied(i)) continue;
                MountedPartData part = _assembly.GetPart(i);
                _byId.TryGetValue(part.partId, out PartDefinitionSO definition);
                if (definition != null && !MatchesFilter(definition.category)) continue;
                rows.Add(new PartRow(definition, part, i, mounted: true));
            }

            IReadOnlyList<int> owned = _shop.OwnedParts;
            for (int i = 0; i < owned.Count; i++)
            {
                if (!_byId.TryGetValue(owned[i], out PartDefinitionSO definition)) continue;
                if (!MatchesFilter(definition.category)) continue;
                rows.Add(new PartRow(definition, FromDefinition(definition), -1, mounted: false));
            }

            _visibleRows = rows.Count;
            _page = Mathf.Clamp(_page, 0, PageCount - 1);
            int start = _page * PageSize;
            int end = Mathf.Min(start + PageSize, rows.Count);
            _builtRows = 0;
            for (int i = start; i < end; i++)
            {
                _partsList.Add(CreatePartRow(rows[i]));
                _builtRows++;
            }

            if (rows.Count == 0) _partsList.Add(new Label("NO PARTS") { name = "parts-empty" });
        }

        private bool MatchesFilter(PartCategory? category)
        {
            return _filter == null || category == _filter;
        }

        private VisualElement CreatePartRow(PartRow row)
        {
            PartDefinitionSO definition = row.definition;
            int partId = definition != null ? definition.partId : row.part.partId;

            var element = new VisualElement { name = "part-row-" + partId + "-" + _builtRows };
            element.AddToClassList("row");
            element.AddToClassList(_builtRows % 2 == 0 ? "row-even" : "row-odd");
            if (partId == _selectedPartId) element.AddToClassList("row-selected");
            element.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.target is Button) return;
                element.schedule.Execute(() => SelectPart(partId));
            });

            AddCell(element, partId.ToString(CultureInfo.InvariantCulture), "col-index");
            AddCell(element, definition != null ? definition.partName : $"Part {partId}", "col-name");
            AddCell(element, CategoryLabel(row.part.category), "col-type");
            AddCell(element, definition != null ? LeftRightLabel(definition.lr) : "-", "col-lr");
            AddCell(element, definition != null
                ? definition.health.ToString("0", CultureInfo.InvariantCulture)
                : row.part.health.ToString("0", CultureInfo.InvariantCulture), "col-hp");
            AddCell(element, definition != null
                ? (definition.turnFly * Mathf.Rad2Deg).ToString("0.0", CultureInfo.InvariantCulture)
                : "0.0", "col-steering");
            AddCell(element, definition != null ? EngineColumn(definition) : row.part.thrust.ToString("0", CultureInfo.InvariantCulture), "col-engine");
            AddCell(element, definition != null ? definition.mass.ToString("0", CultureInfo.InvariantCulture) : row.part.mass.ToString("0", CultureInfo.InvariantCulture), "col-mass");
            AddCell(element, definition != null ? FireColumn(definition) : "0", "col-fire");

            if (row.mounted)
            {
                int socket = row.socket;
                var breakButton = new Button(() => Unmount(socket)) { text = "BREAK", name = "row-break" };
                breakButton.AddToClassList("row-action");
                element.Add(breakButton);
            }
            else
            {
                var merge = new Button(() => Mount(partId)) { text = "MERGE", name = "row-merge" };
                merge.AddToClassList("row-action");
                element.Add(merge);
            }

            return element;
        }

        private void Mount(int partId)
        {
            if (TryResolveFreeSocket(partId, out float2 offset, out bool mirrored))
                MountInternal(partId, offset, mirrored, chooseSocket: true);
            else
                MountInternal(partId, float2.zero, false, chooseSocket: false);
        }

        /// <summary>First free frozen H marker that accepts the part, or false when none exists.</summary>
        private bool TryResolveFreeSocket(int partId, out float2 offset, out bool mirrored)
        {
            offset = float2.zero;
            mirrored = false;
            if (_mounts == null || _assembly == null) return false;
            if (!_byId.TryGetValue(partId, out PartDefinitionSO definition)) return false;

            Span<MountedPartData> parts = stackalloc MountedPartData[CraftAssembly.MaxCapacity];
            int partCount = CraftPreviewPainter.CollectParts(_assembly, _byId, parts);
            if (partCount == 0) return false;

            Span<CraftPartPlacement> placements = stackalloc CraftPartPlacement[CraftAssembly.MaxCapacity];
            int placementCount = CraftPreviewPainter.PlaceParts(
                parts.Slice(0, partCount), _mounts, placements);
            if (placementCount == 0) return false;

            int bodyIndex = -1;
            for (int i = 0; i < placementCount; i++)
            {
                if (placements[i].category != PartCategory.Body) continue;
                bodyIndex = i;
                break;
            }

            if (bodyIndex < 0) return false;
            CraftPartPlacement body = placements[bodyIndex];
            if (!_mounts.TryGetPart(body.partId, out PartMountEntry bodyEntry)) return false;
            if (!_mounts.TryGetSymbol(bodyEntry.mcName, out PartMountSymbol symbol)) return false;

            int wantedLr = definition.lr == 1 ? 1 : 0;
            for (int holeIndex = 0; holeIndex < symbol.holes.Length; holeIndex++)
            {
                PartMountHole hole = symbol.holes[holeIndex];
                if (hole == null || !hole.Accepts(definition.category)) continue;
                if (hole.lr != 2 && hole.lr != wantedLr) continue;
                if (!_mounts.TryResolveSocket(
                        body.partId, holeIndex, definition.partId, body.mirrored, out float2 candidate))
                    continue;
                if (IsSocketOccupied(placements.Slice(0, placementCount), candidate)) continue;

                offset = candidate;
                mirrored = hole.lr == 1;
                return true;
            }

            return false;
        }

        private void MountAt(int partId, float2 offset, bool mirrored)
        {
            MountInternal(partId, offset, mirrored, chooseSocket: true);
        }

        private void MountInternal(int partId, float2 offset, bool mirrored, bool chooseSocket)
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

            // No explicit socket leaves the offset at zero so CraftComposition resolves the
            // real frozen H marker; a clicked socket pins that exact marker instead.
            MountedPartData part = FromDefinition(definition);
            if (chooseSocket)
            {
                part.localOffset = offset;
                part.mirrored = mirrored || definition.lr == 1;
            }

            AssemblyError error = _assembly.TryMountPart(part, out _);
            if (error != AssemblyError.None)
            {
                _shop.AddOwnedPart(partId);
                SetError(error.ToString());
                return;
            }

            RemoveUndo(partId);
            _selectedPartId = -1;
            _page = 0;
            CaptureBlueprint();
            Refresh();
            _onChanged?.Invoke();
        }

        private void Unmount(int socketIndex)
        {
            ClearError();
            if (!_assembly.UnmountPart(socketIndex, out MountedPartData removed)) return;

            _shop.AddOwnedPart(removed.partId);
            PushUndo(removed.partId);
            _selectedPartId = -1;
            CaptureBlueprint();
            Refresh();
            _onChanged?.Invoke();
        }

        private void CaptureBlueprint()
        {
            if (_blueprint == null) return;

            var mounted = new List<MountedPartData>();
            PartDefinitionSO body = null;
            for (int i = 0; i < CraftAssembly.MaxCapacity; i++)
            {
                if (!_assembly.IsSlotOccupied(i)) continue;
                MountedPartData part = _assembly.GetPart(i);
                mounted.Add(part);
                if (part.category == PartCategory.Body && body == null)
                    _byId.TryGetValue(part.partId, out body);
            }

            _blueprint.mountedParts = mounted.ToArray();
            if (body == null) return;

            _blueprint.hullName = body.partName;
            _blueprint.hullCategory = CraftBlueprintSO.GetHullCategoryForName(body.partName);
            _blueprint.hullMass = body.mass;
            _blueprint.hullHealth = body.health;
        }

        // ---------------- Removed-part undo strip ----------------

        private void PushUndo(int partId)
        {
            for (int i = 0; i < _undoPartIds.Length; i++)
            {
                if (_undoPartIds[i] == partId) return;
            }

            for (int i = _undoPartIds.Length - 1; i > 0; i--)
                _undoPartIds[i] = _undoPartIds[i - 1];
            _undoPartIds[0] = partId;
        }

        private void RemoveUndo(int partId)
        {
            for (int i = 0; i < _undoPartIds.Length; i++)
            {
                if (_undoPartIds[i] != partId) continue;
                for (int j = i; j < _undoPartIds.Length - 1; j++)
                    _undoPartIds[j] = _undoPartIds[j + 1];
                _undoPartIds[_undoPartIds.Length - 1] = -1;
            }
        }

        private void RefreshUndo()
        {
            for (int i = 0; i < UndoSlots; i++)
            {
                Button button = _undoButtons[i];
                if (button == null) continue;

                int partId = _undoPartIds[i];
                if (partId >= 0 && _byId.TryGetValue(partId, out PartDefinitionSO definition))
                {
                    button.text = CategoryLetter(definition.category);
                    button.tooltip = "Re-add " + definition.partName;
                    button.SetEnabled(_shop.OwnedCount(partId) > 0);
                }
                else
                {
                    button.text = "-";
                    button.tooltip = "Empty";
                    button.SetEnabled(false);
                }
            }
        }

        private void ReAdd(int slot)
        {
            int partId = _undoPartIds[slot];
            if (partId < 0 || !_byId.ContainsKey(partId)) return;
            if (_shop.OwnedCount(partId) <= 0) return;
            Mount(partId);
        }

        // ---------------- Helpers ----------------

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
                energyDraw = definition.energyDraw,
                mirrored = definition.lr == 1
            };
        }

        private int FirePointCount(int partId)
        {
            return _mounts != null && _mounts.TryGetPart(partId, out PartMountEntry entry) &&
                   _mounts.TryGetSymbol(entry.mcName, out PartMountSymbol symbol)
                ? symbol.firePoints.Length
                : 0;
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

        private static string CategoryLetter(PartCategory category)
        {
            switch (category)
            {
                case PartCategory.Body: return "B";
                case PartCategory.Wing: return "W";
                case PartCategory.Engine: return "E";
                case PartCategory.Gun: return "G";
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
                ? definition.CalculateEffectiveRange().ToString("0", CultureInfo.InvariantCulture)
                : definition.thrust.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private string FireColumn(PartDefinitionSO definition)
        {
            return definition.category == PartCategory.Gun
                ? definition.CalculateDisplayFirePower(FirePointCount(definition.partId))
                    .ToString("0.00", CultureInfo.InvariantCulture)
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
