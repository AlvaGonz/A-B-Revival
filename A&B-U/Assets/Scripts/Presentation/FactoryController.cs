using System;
using System.Collections.Generic;
using System.Globalization;
using ABRevival.Core.Data;
using ABRevival.Core.Persistence;
using ABRevival.Core.Shop;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Scene entry point for Factory (the frozen UIMakePlayer assembly screen): the 2x craft
    /// preview, owned-parts table with category tabs, merge/new/add/edit flows, live F4ShowData
    /// stats, the wing counter, the removed-parts undo strip, zoom and Back to Center.
    /// The factory never owns a save file; it mutates the payload owned by
    /// <see cref="GameFlowController"/> and lets Center reload it on return.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class FactoryController : MonoBehaviour
    {
        private const int CraftPageSize = 9;

        [SerializeField] private PartDefinitionSO[] catalogParts = Array.Empty<PartDefinitionSO>();

        [Header("Factory sprites (frozen 2011 UI library)")]
        [SerializeField] private Sprite factoryBackground;
        [SerializeField] private Sprite factoryHeader;
        [SerializeField] private Sprite previewFrame;
        [SerializeField] private Sprite buttonPlate;
        [SerializeField] private Sprite buttonMerge;
        [SerializeField] private Sprite buttonNew;
        [SerializeField] private Sprite buttonAdd;
        [SerializeField] private Sprite buttonBackToCenter;
        [SerializeField] private Sprite buttonPartsCrafts;
        [SerializeField] private Sprite helpButton;
        [SerializeField] private Sprite categoryAll;
        [SerializeField] private Sprite categoryBody;
        [SerializeField] private Sprite categoryWing;
        [SerializeField] private Sprite categoryEngine;
        [SerializeField] private Sprite categoryGun;
        [SerializeField] private MountArtEntry[] mountArt = Array.Empty<MountArtEntry>();

        private UIDocument _document;
        private ShopCatalog _catalog;
        private ShopState _shop;
        private PartMountTable _mountTable;
        private MountArtLookup _mountArtLookup;
        private Dictionary<int, PartDefinitionSO> _byId;
        private List<PartDefinitionSO> _definitions;
        private PlayerSaveData _payload = new PlayerSaveData();

        private readonly List<CraftBlueprintSO> _fleet = new List<CraftBlueprintSO>();
        private CraftAssembly _assembly;
        private AssemblyPresenter _presenter;

        private VisualElement _partsHeader;
        private VisualElement _craftsHeader;
        private VisualElement _help;
        private ScrollView _partsList;
        private ScrollView _craftsList;
        private Label _title;
        private Label _pageLabel;
        private Button _firstButton;
        private Button _previousButton;
        private Button _nextButton;
        private Button _lastButton;
        private Button _partsCraftsButton;

        private int _activeIndex;
        private int _craftPage;
        private bool _showingCrafts;
        private bool _newCraft;
        private bool _initialized;

        /// <summary>Raised after a Factory action mutates the save payload.</summary>
        public event Action Changed;

        /// <summary>Raised when the player asks to return to Center.</summary>
        public event Action Closed;

        public int FleetCount => _fleet.Count;

        public int ActiveCraftIndex => _activeIndex;

        public int ActivePartCount => _assembly != null ? _assembly.PartCount : 0;

        public bool ShowingCrafts => _showingCrafts;

        public bool IsNewCraft => _newCraft;

        public AssemblyPresenter Presenter => _presenter;

        public CraftShowStats ActiveStats => StatsFor(ActiveBlueprint);

        public string ActiveCraftName => CraftName(_activeIndex);

        public int CraftPageIndex => _craftPage;

        public int CraftPageCount => Mathf.Max(1, (_fleet.Count + CraftPageSize - 1) / CraftPageSize);

        public ShopState Shop => _shop;

        private void Awake()
        {
            // GameFlowController owns visibility and calls Initialize with the real payload.
            var document = GetComponent<UIDocument>();
            if (document != null && document.rootVisualElement != null)
                document.rootVisualElement.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Builds the Factory UI once and applies the shared player save. GameFlowController
        /// calls this when Center's FACTORY button raises the screen change.
        /// </summary>
        public void Initialize(PlayerSaveData save, int craftIndex)
        {
            if (!_initialized)
            {
                _document = GetComponent<UIDocument>();
                if (_document == null || _document.rootVisualElement == null)
                {
                    Debug.LogError("[Factory] UIDocument with a source UXML is required.");
                    return;
                }

                _definitions = ResolveDefinitions();
                _byId = new Dictionary<int, PartDefinitionSO>(_definitions.Count);
                for (int i = 0; i < _definitions.Count; i++)
                    _byId[_definitions[i].partId] = _definitions[i];

                _catalog = BuildCatalog(_definitions);
                _shop = new ShopState(_catalog);
                _mountTable = LoadMountTable();
                _mountArtLookup = new MountArtLookup(_mountTable, mountArt);

                VisualElement root = _document.rootVisualElement;
                ApplySkin(root);
                Bind(root);

                _presenter = new AssemblyPresenter(
                    root, null, null, _definitions, _shop, _mountTable, _mountArtLookup,
                    OnDomainChanged);
                _presenter.ViewChanged += RefreshFooter;
                _presenter.SetPartsVisible(!_showingCrafts);
                BindCategorySwitch(root);

                _initialized = true;
            }

            ApplySave(save, craftIndex);
            SetVisible(true);
        }

        /// <summary>Captures shop stock and every fleet blueprint into the single payload.</summary>
        public void CaptureSave(PlayerSaveData target)
        {
            if (target == null || !_initialized) return;

            target.shop = _shop.CreateSave();
            target.blueprints.Clear();
            for (int i = 0; i < _fleet.Count; i++)
                target.blueprints.Add(BlueprintSaveMapper.Capture(_fleet[i]));
        }

        /// <summary>Restores shop and fleet, then opens the requested craft for editing.</summary>
        public void ApplySave(PlayerSaveData payload, int craftIndex)
        {
            if (!_initialized || payload == null) return;

            _payload = payload;
            _shop.LoadFrom(payload.shop);
            RebuildFleet(payload.blueprints);

            _craftPage = 0;
            _showingCrafts = _fleet.Count == 0;
            _newCraft = false;
            SetActive(Mathf.Clamp(craftIndex, 0, Mathf.Max(0, _fleet.Count - 1)), refresh: false);
            ShowCrafts(_showingCrafts);
            RefreshAll();
        }

        /// <summary>Shows or hides the whole Factory overlay.</summary>
        public void SetVisible(bool visible)
        {
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_document != null && _document.rootVisualElement != null)
                _document.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Opens one fleet craft for assembly and returns to the parts view.</summary>
        public void EditCraft(int index)
        {
            if (index < 0 || index >= _fleet.Count) return;
            SetActive(index, refresh: false);
            _newCraft = false;
            ShowCrafts(false);
            RefreshAll();
        }

        /// <summary>Switches the bottom table between owned parts and the fleet craft list.</summary>
        public void ToggleCrafts() => ShowCrafts(!_showingCrafts);

        public void ShowCrafts(bool show)
        {
            _showingCrafts = show;
            if (_partsList != null) _partsList.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
            if (_craftsList != null) _craftsList.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (_partsHeader != null) _partsHeader.style.display = show ? DisplayStyle.None : DisplayStyle.Flex;
            if (_craftsHeader != null) _craftsHeader.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (_partsCraftsButton != null)
            {
                _partsCraftsButton.RemoveFromClassList("tab-active");
                if (show) _partsCraftsButton.AddToClassList("tab-active");
            }

            if (_presenter != null) _presenter.SetPartsVisible(!show);
            RefreshCrafts();
            RefreshFooter();
            RefreshHeader();
        }

        /// <summary>Breaks every non-chassis part back to inventory (frozen NEW flow).</summary>
        public void NewCraft()
        {
            if (_presenter == null || _assembly == null) return;
            _newCraft = true;
            _presenter.NewCraft();
            RefreshHeader();
        }

        /// <summary>
        /// Confirms the working assembly as the active fleet craft (frozen MERGE flow) and
        /// returns to the craft list. Every mount already persists through the save payload.
        /// </summary>
        public void MergeCraft()
        {
            if (_assembly == null) return;
            if (!_assembly.IsValid)
            {
                SetError("This craft needs a hull before it can be merged.");
                return;
            }

            _newCraft = false;
            ShowCrafts(true);
        }

        /// <summary>Adds the working assembly as a new fleet craft and names it CF-n.</summary>
        public void AddCraft()
        {
            if (_assembly == null) return;
            if (!_assembly.IsValid)
            {
                SetError("This craft needs a hull before it can be added.");
                return;
            }

            var blueprint = ScriptableObject.CreateInstance<CraftBlueprintSO>();
            BlueprintSaveMapper.Apply(BlueprintSaveMapper.Capture(_fleet[_activeIndex]), blueprint);
            blueprint.craftName = NextCraftName();
            _fleet.Add(blueprint);

            _activeIndex = _fleet.Count - 1;
            _newCraft = false;
            RebuildAssembly();
            _presenter?.Rebind(_assembly, ActiveBlueprint);
            ShowCrafts(false);
            CaptureSave(_payload);
            RefreshAll();
            Changed?.Invoke();
        }

        /// <summary>Returns to Center; GameFlowController captures and reloads the dashboard.</summary>
        public void BackToCenter() => Closed?.Invoke();

        // ---------------- Binding / skin ----------------

        private void Bind(VisualElement root)
        {
            _partsHeader = root.Q("table-header");
            _craftsHeader = root.Q("crafts-header");
            _help = root.Q("factory-help");
            _partsList = root.Q<ScrollView>("owned-parts-list");
            _craftsList = root.Q<ScrollView>("crafts-list");
            _title = root.Q<Label>("factory-title");
            _pageLabel = root.Q<Label>("page-label");
            _partsCraftsButton = root.Q<Button>("parts-crafts-button");

            _firstButton = root.Q<Button>("first-button");
            _previousButton = root.Q<Button>("prev-button");
            _nextButton = root.Q<Button>("next-button");
            _lastButton = root.Q<Button>("last-button");

            if (_craftsList != null) _craftsList.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            Bind(root, "merge-button", MergeCraft);
            Bind(root, "new-button", NewCraft);
            Bind(root, "add-button", AddCraft);
            Bind(root, "back-to-center-button", BackToCenter);
            Bind(root, "parts-crafts-button", ToggleCrafts);
            Bind(root, "help-button", ToggleHelp);
            Bind(root, "help-close", () => { if (_help != null) _help.style.display = DisplayStyle.None; });
            Bind(root, "first-button", FirstPage);
            Bind(root, "prev-button", PreviousPage);
            Bind(root, "next-button", NextPage);
            Bind(root, "last-button", LastPage);

            if (_craftsList != null) _craftsList.style.display = DisplayStyle.None;
            if (_help != null) _help.style.display = DisplayStyle.None;
        }

        private static void Bind(VisualElement root, string name, Action action)
        {
            Button button = root.Q<Button>(name);
            if (button != null) button.clicked += action;
        }

        /// <summary>The page keeps the category tabs visible; choosing one returns to the parts form.</summary>
        private void BindCategorySwitch(VisualElement root)
        {
            string[] tabs = { "tab-all", "tab-body", "tab-wing", "tab-engine", "tab-gun" };
            for (int i = 0; i < tabs.Length; i++)
            {
                Button button = root.Q<Button>(tabs[i]);
                if (button != null) button.clicked += () => ShowCrafts(false);
            }
        }

        private void ApplySkin(VisualElement root)
        {
            SetPanelBackground(root, "factory-root", factoryBackground);
            SetPanelBackground(root, "factory-header", factoryHeader);
            SetPanelBackground(root, "preview-frame", previewFrame);

            SetSpriteBackground(root, "help-button", helpButton, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "merge-button", buttonMerge != null ? buttonMerge : buttonPlate, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "new-button", buttonNew != null ? buttonNew : buttonPlate, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "add-button", buttonAdd != null ? buttonAdd : buttonPlate, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "back-to-center-button", buttonBackToCenter != null ? buttonBackToCenter : buttonPlate, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "parts-crafts-button", buttonPartsCrafts != null ? buttonPartsCrafts : buttonPlate, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "help-close", buttonPlate, ScaleMode.ScaleAndCrop);

            SetSpriteBackground(root, "tab-all", categoryAll, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "tab-body", categoryBody, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "tab-wing", categoryWing, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "tab-engine", categoryEngine, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "tab-gun", categoryGun, ScaleMode.ScaleAndCrop);
        }

        private static void SetPanelBackground(VisualElement root, string name, Sprite sprite)
        {
            if (sprite == null) return;
            VisualElement element = root.Q(name);
            if (element != null) element.style.backgroundImage = new StyleBackground(sprite);
        }

        private static void SetSpriteBackground(VisualElement root, string name, Sprite sprite, ScaleMode scaleMode)
        {
            if (sprite == null) return;
            VisualElement element = root.Q(name);
            if (element != null) UiSpriteSkin.Apply(element, sprite, scaleMode);
        }

        private void ToggleHelp()
        {
            if (_help == null) return;
            _help.style.display = _help.resolvedStyle.display == DisplayStyle.None
                ? DisplayStyle.Flex
                : DisplayStyle.None;
        }

        // ---------------- Refresh ----------------

        private void OnDomainChanged()
        {
            if (_assembly != null && _activeIndex >= 0 && _activeIndex < _fleet.Count)
                _fleet[_activeIndex].mountedParts = CaptureMountedParts();

            CaptureSave(_payload);
            RefreshCrafts();
            RefreshHeader();
            Changed?.Invoke();
        }

        private void RefreshAll()
        {
            if (!_initialized) return;
            RefreshHeader();
            RefreshCrafts();
            _presenter?.Refresh();
            RefreshFooter();
        }

        private void RefreshHeader()
        {
            if (_title != null) _title.text = _newCraft ? "New" : "Edit";
        }

        private void RefreshCrafts()
        {
            if (_craftsList == null) return;
            _craftsList.Clear();
            _craftPage = Mathf.Clamp(_craftPage, 0, CraftPageCount - 1);

            int start = _craftPage * CraftPageSize;
            int end = Mathf.Min(start + CraftPageSize, _fleet.Count);
            for (int index = start; index < end; index++)
                _craftsList.Add(CreateCraftRow(index));

            if (_fleet.Count == 0)
                _craftsList.Add(new Label("NO CRAFTS") { name = "crafts-empty" });
        }

        private VisualElement CreateCraftRow(int index)
        {
            var row = new VisualElement { name = "craft-row-" + index };
            row.AddToClassList("row");
            row.AddToClassList("font-data");
            row.AddToClassList(index % 2 == 0 ? "row-even" : "row-odd");
            if (index == _activeIndex) row.AddToClassList("row-selected");
            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.target is Button) return;
                row.schedule.Execute(() => EditCraft(index));
            });

            AddCell(row, index.ToString(CultureInfo.InvariantCulture), "col-index");
            AddCell(row, CraftName(index), "col-name");

            var edit = new Button(() => EditCraft(index)) { text = "EDIT", name = "craft-edit" };
            edit.AddToClassList("row-action");
            row.Add(edit);

            var breakCraft = new Button(() => BreakCraft(index)) { text = "BREAK", name = "craft-break" };
            breakCraft.AddToClassList("row-action");
            breakCraft.SetEnabled(CanBreakCraft(index));
            breakCraft.tooltip = CanBreakCraft(index) ? "Break this craft" : "In use by Player or AI";
            row.Add(breakCraft);
            return row;
        }

        private bool CanBreakCraft(int index)
        {
            if (index < 0 || index >= _fleet.Count) return false;
            if (index == _payload.playerBlueprintIndex) return false;
            if (_payload.companions == null) return true;
            for (int i = 0; i < _payload.companions.Count; i++)
            {
                if (_payload.companions[i].craftIndex == index) return false;
            }

            return true;
        }

        /// <summary>
        /// Frozen FormFly BREAK: removes a spare fleet craft. The Player and AI-assigned
        /// crafts are protected, and surviving indices shift down with the list.
        /// </summary>
        public void BreakCraft(int index)
        {
            if (!CanBreakCraft(index))
            {
                SetError("The Player and AI crafts cannot be broken.");
                return;
            }

            CraftBlueprintSO removed = _fleet[index];
            _fleet.RemoveAt(index);
            if (removed != null) Destroy(removed);

            if (_payload.playerBlueprintIndex > index) _payload.playerBlueprintIndex--;
            if (_payload.companions != null)
            {
                for (int i = 0; i < _payload.companions.Count; i++)
                {
                    CompanionSaveData slot = _payload.companions[i];
                    if (slot.craftIndex > index) slot.craftIndex--;
                    _payload.companions[i] = slot;
                }
            }

            _activeIndex = Mathf.Clamp(_activeIndex, 0, Mathf.Max(0, _fleet.Count - 1));
            RebuildAssembly();
            _presenter?.Rebind(_assembly, ActiveBlueprint);
            ShowCrafts(true);
            CaptureSave(_payload);
            RefreshAll();
            Changed?.Invoke();
        }

        private void RefreshFooter()
        {
            if (_pageLabel == null) return;

            bool crafts = _showingCrafts;
            int page = crafts ? _craftPage : _presenter != null ? _presenter.PageIndex : 0;
            int pages = crafts ? CraftPageCount : _presenter != null ? _presenter.PageCount : 1;

            _pageLabel.text = (page + 1) + " / " + pages;
            if (_firstButton != null) _firstButton.SetEnabled(page > 0);
            if (_previousButton != null) _previousButton.SetEnabled(page > 0);
            if (_nextButton != null) _nextButton.SetEnabled(page < pages - 1);
            if (_lastButton != null) _lastButton.SetEnabled(page < pages - 1);
        }

        public void FirstPage()
        {
            if (ChangeCraftPage(0)) return;
            _presenter?.FirstPage();
        }

        public void PreviousPage()
        {
            if (ChangeCraftPage(_craftPage - 1)) return;
            _presenter?.PreviousPage();
        }

        public void NextPage()
        {
            if (ChangeCraftPage(_craftPage + 1)) return;
            _presenter?.NextPage();
        }

        public void LastPage()
        {
            if (ChangeCraftPage(CraftPageCount - 1)) return;
            _presenter?.LastPage();
        }

        private bool ChangeCraftPage(int page)
        {
            if (!_showingCrafts) return false;
            int clamped = Mathf.Clamp(page, 0, CraftPageCount - 1);
            if (clamped == _craftPage) return true;
            _craftPage = clamped;
            RefreshCrafts();
            RefreshFooter();
            return true;
        }

        // ---------------- Craft catalog / fleet ----------------

        public string CraftName(int index)
        {
            if (index < 0 || index >= _fleet.Count) return string.Empty;
            CraftBlueprintSO blueprint = _fleet[index];
            return string.IsNullOrEmpty(blueprint.craftName) ? blueprint.hullName : blueprint.craftName;
        }

        public CraftShowStats StatsFor(CraftBlueprintSO blueprint)
        {
            if (blueprint == null) return new CraftShowStats();

            Span<CraftPartStat> parts = stackalloc CraftPartStat[CraftAssembly.MaxCapacity];
            int count = 0;
            MountedPartData[] mounted = blueprint.mountedParts;
            if (mounted != null)
            {
                for (int i = 0; i < mounted.Length && count < parts.Length; i++)
                {
                    MountedPartData part = mounted[i];
                    if (!_byId.TryGetValue(part.partId, out PartDefinitionSO definition)) continue;
                    float fire = definition.category == PartCategory.Gun
                        ? definition.CalculateDisplayFirePower(FirePointCount(definition.partId))
                        : 0f;
                    parts[count++] = new CraftPartStat(
                        definition.category, definition.mass, definition.health,
                        definition.thrust, definition.turnFly, definition.cost, fire);
                }
            }

            return CraftShowData.Evaluate(
                blueprint.craftName, blueprint.hullName,
                blueprint.hullMass, blueprint.hullHealth, parts.Slice(0, count));
        }

        private void SetActive(int index, bool refresh)
        {
            _activeIndex = index;
            RebuildAssembly();
            _presenter?.Rebind(_assembly, ActiveBlueprint);
            if (refresh) RefreshAll();
        }

        private CraftBlueprintSO ActiveBlueprint =>
            _activeIndex >= 0 && _activeIndex < _fleet.Count ? _fleet[_activeIndex] : null;

        private void RebuildAssembly()
        {
            _assembly?.Dispose();
            _assembly = ActiveBlueprint != null ? BuildAssembly(ActiveBlueprint) : null;
        }

        private CraftAssembly BuildAssembly(CraftBlueprintSO source)
        {
            var assembly = new CraftAssembly(
                source.hullName, source.hullMass, source.hullHealth, source.isGrandfathered);
            if (source.mountedParts != null)
            {
                for (int i = 0; i < source.mountedParts.Length; i++)
                {
                    MountedPartData part = source.mountedParts[i];
                    AssemblyError error = assembly.TryMountPart(part, out _);
                    if (error != AssemblyError.None)
                        Debug.LogWarning($"[Factory] Blueprint part {part.partId} was not mounted: {error}.");
                }
            }

            return assembly;
        }

        private MountedPartData[] CaptureMountedParts()
        {
            if (_assembly == null) return Array.Empty<MountedPartData>();

            var mounted = new List<MountedPartData>(_assembly.PartCount);
            for (int i = 0; i < CraftAssembly.MaxCapacity; i++)
            {
                if (_assembly.IsSlotOccupied(i)) mounted.Add(_assembly.GetPart(i));
            }

            return mounted.ToArray();
        }

        private void RebuildFleet(List<BlueprintSaveData> saved)
        {
            _assembly?.Dispose();
            _assembly = null;
            _activeIndex = 0;

            for (int i = 0; i < _fleet.Count; i++)
            {
                if (_fleet[i] != null) Destroy(_fleet[i]);
            }
            _fleet.Clear();

            if (saved == null) return;
            for (int i = 0; i < saved.Count; i++)
            {
                var blueprint = ScriptableObject.CreateInstance<CraftBlueprintSO>();
                BlueprintSaveMapper.Apply(saved[i], blueprint);
                if (string.IsNullOrEmpty(blueprint.hullName))
                {
                    PartDefinitionSO body = FirstMountedBody(blueprint);
                    if (body != null)
                    {
                        blueprint.hullName = body.partName;
                        blueprint.hullCategory = CraftBlueprintSO.GetHullCategoryForName(body.partName);
                        blueprint.hullMass = body.mass;
                        blueprint.hullHealth = body.health;
                    }
                }

                _fleet.Add(blueprint);
            }
        }

        private PartDefinitionSO FirstMountedBody(CraftBlueprintSO blueprint)
        {
            if (blueprint.mountedParts == null) return null;
            for (int i = 0; i < blueprint.mountedParts.Length; i++)
            {
                MountedPartData part = blueprint.mountedParts[i];
                if (part.category != PartCategory.Body) continue;
                if (_byId.TryGetValue(part.partId, out PartDefinitionSO definition)) return definition;
            }

            return null;
        }

        private string NextCraftName()
        {
            for (int i = 1; i < 1000; i++)
            {
                string candidate = "CF-" + i.ToString(CultureInfo.InvariantCulture);
                bool used = false;
                for (int j = 0; j < _fleet.Count; j++)
                {
                    if (!string.Equals(_fleet[j].craftName, candidate, StringComparison.Ordinal)) continue;
                    used = true;
                    break;
                }

                if (!used) return candidate;
            }

            return "CF-" + _fleet.Count.ToString(CultureInfo.InvariantCulture);
        }

        // ---------------- Definitions / mounts ----------------

        private List<PartDefinitionSO> ResolveDefinitions()
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
                Debug.LogError("[Factory] No PartDefinitionSO catalog assigned; the parts list is empty.");

            return definitions;
        }

        private static ShopCatalog BuildCatalog(IReadOnlyList<PartDefinitionSO> definitions)
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

        private static PartMountTable LoadMountTable()
        {
            try
            {
                TextAsset table = Resources.Load<TextAsset>("Data/PartMounts");
                return table != null ? PartMountTable.Parse(table.text) : null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Factory] Part mount table unavailable: {exception.Message}");
                return null;
            }
        }

        private int FirePointCount(int partId)
        {
            return _mountTable != null && _mountTable.TryGetPart(partId, out PartMountEntry entry) &&
                   _mountTable.TryGetSymbol(entry.mcName, out PartMountSymbol symbol)
                ? symbol.firePoints.Length
                : 0;
        }

        private void SetError(string message)
        {
            VisualElement root = _document != null ? _document.rootVisualElement : null;
            Label label = root != null ? root.Q<Label>("assembly-error") : null;
            if (label != null) label.text = message;
        }

        private static void AddCell(VisualElement row, string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList("col");
            label.AddToClassList(className);
            row.Add(label);
        }

        private void OnDestroy()
        {
            _assembly?.Dispose();
            for (int i = 0; i < _fleet.Count; i++)
            {
                if (_fleet[i] != null) Destroy(_fleet[i]);
            }
            _fleet.Clear();
        }
    }
}
