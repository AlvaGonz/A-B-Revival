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
    /// Scene entry point for Center (the frozen UIChooseFlyAI dashboard): craft preview,
    /// F4ShowData stat card, Player/AI assignment, companion grades, Reset/Best, pagination
    /// and the INFO / READY / SHOP / FACTORY / BACK navigation targets.
    /// The center never owns a save file; it mutates the single payload owned by
    /// <see cref="GameFlowController"/>.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [RequireComponent(typeof(ScreenPanel))]
    public sealed class CenterController : MonoBehaviour
    {
        private const int PageSize = 8;
        private const int CompanionSlotCount = CompanionRoster.SlotCount;
        private const float PreviewAreaFill = 0.65f;
        private const float PreviewMaxDimensionFill = 0.98f;

        [SerializeField] private PartDefinitionSO[] catalogParts = Array.Empty<PartDefinitionSO>();
        [SerializeField] private int startingMoney = 2000;

        [Header("Center sprites (frozen 2011 UI library)")]
        [SerializeField] private Sprite centerBackground;
        [SerializeField] private Sprite centerHeader;
        [SerializeField] private Sprite previewFrame;
        [SerializeField] private Sprite panel;
        [SerializeField] private Sprite buttonPlate;
        [SerializeField] private Sprite buttonReady;
        [SerializeField] private Sprite gradePlate;
        [SerializeField] private Sprite helpButton;
        [SerializeField] private MountArtEntry[] mountArt = Array.Empty<MountArtEntry>();

        private ScreenPanel _panel;
        private ShopCatalog _catalog;
        private ShopState _shop;
        private PartMountTable _mountTable;
        private CraftCatalog _craftCatalog;
        private Dictionary<int, PartDefinitionSO> _byId;
        private MountArtLookup _mountArtLookup;
        private List<PartDefinitionSO> _definitions;
        private PlayerSaveData _payload = new PlayerSaveData();

        private readonly List<CraftBlueprintSO> _fleet = new List<CraftBlueprintSO>();
        private readonly List<CraftAssembly> _assemblies = new List<CraftAssembly>();
        private readonly CompanionRoster _roster = new CompanionRoster();
        private readonly Button[] _gradeButtons = new Button[CompanionSlotCount];

        private VisualElement _screen;
        private VisualElement _placeholder;
        private VisualElement _previewClip;
        private VisualElement _previewCanvas;
        private VisualElement _companionSlots;
        private ScrollView _craftList;
        private Label _battleLabel;
        private Label _moneyLabel;
        private Label _previewName;
        private Label _statName;
        private Label _statHp;
        private Label _statFire;
        private Label _statSpeed;
        private Label _statSteering;
        private Label _statClass;
        private Label _statParts;
        private Label _statValue;
        private Label _inuseLabel;
        private Label _instructionLabel;
        private Label _pageLabel;
        private Label _placeholderTitle;
        private Button _resetButton;
        private Button _bestButton;
        private Button _helpButton;
        private Button _infoButton;
        private Button _readyButton;
        private Button _shopButton;
        private Button _factoryButton;
        private Button _backButton;
        private Button _placeholderBack;
        private Button _firstButton;
        private Button _previousButton;
        private Button _nextButton;
        private Button _lastButton;

        private int _playerIndex;
        private int _selectedIndex;
        private int _page;
        private bool _initialized;
        private Vector2 _lastPreviewSize;

        /// <summary>Raised after a Center action mutates the save payload.</summary>
        public event Action Changed;

        /// <summary>Raised when BACK asks to return to the frozen UIMain page (#181).</summary>
        public event Action MainMenuRequested;

        /// <summary>Raised when the player asks for the dedicated Factory screen (#175).</summary>
        public event Action FactoryRequested;

        /// <summary>Raised when the player asks for the dedicated Shop screen (#176).</summary>
        public event Action ShopRequested;

        /// <summary>Raised when the player asks for the Info/Records screen (#177).</summary>
        public event Action InfoRequested;

        /// <summary>Raised when the player asks for the Ready/Briefing handoff (#178).</summary>
        public event Action ReadyRequested;

        /// <summary>Current shop money, exposed for boot/save verification.</summary>
        public int Money => _shop != null ? _shop.Money : 0;

        /// <summary>The selected Player craft index into the saved fleet.</summary>
        public int PlayerCraftIndex => _playerIndex;

        /// <summary>The craft currently shown in the preview and stat card.</summary>
        public int SelectedCraftIndex => _selectedIndex;

        /// <summary>
        /// True when the selected Player craft is a valid assembly. The frozen Center
        /// disables READY when no Player fly link exists (UIChooseFlyAI.afterFadeIn).
        /// </summary>
        public bool CanLaunch =>
            _initialized && _fleet.Count > 0 && _playerIndex >= 0 &&
            _playerIndex < _assemblies.Count && _assemblies[_playerIndex].IsValid;

        /// <summary>True when the fleet craft at the index has a valid assembly (hull present).</summary>
        public bool IsCraftValid(int index)
        {
            return index >= 0 && index < _assemblies.Count && _assemblies[index].IsValid;
        }

        public int FleetCount => _fleet.Count;

        public int PreviewPartCount => _previewCanvas != null ? _previewCanvas.childCount : 0;

        /// <summary>True when every preview part resolved to its 2x PartMounts sprite.</summary>
        public bool PreviewUsesMountArt { get; private set; }

        /// <summary>Measured opaque craft bounds in the preview clip, in panel pixels.</summary>
        public Vector2 PreviewBoundsSize { get; private set; }

        /// <summary>Measured opaque bounds area divided by the usable clip area.</summary>
        public float PreviewFillFraction { get; private set; }

        public int PageIndex => _page;

        public CompanionRoster Companions => _roster;

        public string PlaceholderTitle => _placeholderTitle != null ? _placeholderTitle.text : string.Empty;

        /// <summary>True when the shop has a stock row for the part (save reconciliation telemetry).</summary>
        public bool IsUnlocked(int partId) => _shop != null && _shop.IsUnlocked(partId);

        /// <summary>
        /// Resolves a mounted part's authored definition. Mission loadout conversion uses it
        /// to turn an assembled gun part into fire-control parameters.
        /// </summary>
        public PartDefinitionSO FindPartDefinition(int partId)
        {
            if (_definitions == null) return null;
            for (int i = 0; i < _definitions.Count; i++)
            {
                PartDefinitionSO definition = _definitions[i];
                if (definition != null && definition.partId == partId) return definition;
            }

            return null;
        }

        private void Awake()
        {
            // GameFlowController owns visibility and opens Center from the main menu.
            _panel = GetComponent<ScreenPanel>();
            if (_panel != null) _panel.SetVisible(false);
        }

        private void Start()
        {
            Initialize(null);
        }

        /// <summary>
        /// Builds the Center UI once and applies a player save. GameFlowController calls this at
        /// boot; calling it again with a save refreshes the fleet, companions and money.
        /// </summary>
        public void Initialize(PlayerSaveData save)
        {
            _panel = GetComponent<ScreenPanel>();
            if (_panel == null)
            {
                Debug.LogError("[Center] ScreenPanel with a PanelRenderer is required.");
                return;
            }

            _panel.WhenReady(root => Open(save, root));
        }

        private void Open(PlayerSaveData save, VisualElement root)
        {
            if (_initialized)
            {
                if (save != null)
                {
                    ApplySave(save);
                    SeedStartingMoney(save);
                }
                return;
            }

            _definitions = PartCatalogBootstrap.Collect(catalogParts, "Center");
            _byId = new Dictionary<int, PartDefinitionSO>(_definitions.Count);
            for (int i = 0; i < _definitions.Count; i++)
                _byId[_definitions[i].partId] = _definitions[i];

            _catalog = PartCatalogBootstrap.BuildCatalog(_definitions);
            _shop = new ShopState(_catalog, startingMoney);
            _mountTable = PartCatalogBootstrap.LoadMountTable("Center");
            _mountArtLookup = new MountArtLookup(_mountTable, mountArt);

            ApplySkin(root);
            Bind(root);

            _initialized = true;
            if (save != null)
            {
                ApplySave(save);
                SeedStartingMoney(save);
            }
            else
            {
                RebuildFleet(null);
                ReconcileShop(0);
                _selectedIndex = 0;
                RefreshAll();
            }
        }

        /// <summary>
        /// A brand-new campaign has an empty payload; the constructed ShopState's starting
        /// money would otherwise be overwritten by LoadFrom. Existing saves (even a spent
        /// 0 G balance) are left untouched.
        /// </summary>
        private void SeedStartingMoney(PlayerSaveData save)
        {
            if (save.shop == null || save.shop.money != 0) return;
            if (save.shop.stock.Count > 0 || save.shop.ownedParts.Count > 0) return;
            if (save.blueprints != null && save.blueprints.Count > 0) return;

            _shop = new ShopState(_catalog, startingMoney);
            ReconcileShop(save.storyIndex);
            RefreshHeader();
        }

        /// <summary>Captures the shop, every fleet craft, the Player pick and companion slots.</summary>
        public void CaptureSave(PlayerSaveData target)
        {
            if (target == null || !_initialized) return;

            target.shop = _shop.CreateSave();
            target.blueprints.Clear();
            for (int i = 0; i < _fleet.Count; i++)
                target.blueprints.Add(BlueprintSaveMapper.Capture(_fleet[i]));

            target.playerBlueprintIndex = _playerIndex;
            target.companions = _roster.ToSave();
        }

        /// <summary>Restores shop, fleet, Player pick and companions, then reconciles catalog stock.</summary>
        public void ApplySave(PlayerSaveData payload)
        {
            if (!_initialized || payload == null) return;

            _payload = payload;
            _shop.LoadFrom(payload.shop);
            RebuildFleet(payload.blueprints);

            _playerIndex = ClampCraftIndex(payload.playerBlueprintIndex);
            _selectedIndex = _playerIndex;
            _roster.Load(payload.companions, _playerIndex, _fleet.Count);
            _page = 0;

            ReconcileShop(payload.storyIndex);
            RefreshAll();
        }

        /// <summary>
        /// Reloads only the shop slice after the dedicated Shop screen (#176) changed money,
        /// stock or owned parts. The fleet, Player pick, companions and selection stay untouched.
        /// </summary>
        public void ApplyShopSave()
        {
            if (!_initialized) return;

            _shop.LoadFrom(_payload.shop);
            ReconcileShop(_payload.storyIndex);
            RefreshHeader();
        }

        /// <summary>Shows or hides the whole Center overlay.</summary>
        public void SetVisible(bool visible)
        {
            if (_panel == null) _panel = GetComponent<ScreenPanel>();
            _panel?.SetVisible(visible);
        }

        /// <summary>Updates the BATTLE-n label from the saved STORY index.</summary>
        public void SetStory(int storyIndex)
        {
            _payload.storyIndex = storyIndex;
            RefreshHeader();
        }

        // ---------------- Center actions ----------------

        public void SelectCraft(int index)
        {
            if (index < 0 || index >= _fleet.Count) return;
            _selectedIndex = index;
            RefreshSelected();
            RefreshTable();
        }

        /// <summary>Assigns a fleet craft as Player, clearing it from any companion slot.</summary>
        public bool AssignPlayer(int index)
        {
            if (index < 0 || index >= _fleet.Count || !_assemblies[index].IsValid) return false;

            _roster.RemoveAll(index);
            _playerIndex = index;
            _selectedIndex = index;
            CommitChange();
            return true;
        }

        /// <summary>Adds the craft to the next free companion slot; the AI column is a count.</summary>
        public bool AssignCompanion(int index)
        {
            if (index < 0 || index >= _fleet.Count || index == _playerIndex) return false;
            if (!IsCraftValid(index)) return false;

            bool changed = _roster.AssignFirstFree(index);
            if (changed) CommitChange();
            return changed;
        }

        public bool RemoveCompanion(int slot)
        {
            if (!_roster.RemoveAt(slot)) return false;
            CommitChange();
            return true;
        }

        public void ResetCompanions()
        {
            if (_roster.AssignedCount == 0) return;
            _roster.Clear();
            CommitChange();
        }

        /// <summary>Fills every empty companion slot with the highest-value unassigned craft.</summary>
        public void FillBestCompanions()
        {
            bool changed = false;
            while (_roster.AssignedCount < CompanionSlotCount)
            {
                int best = -1;
                int bestValue = -1;
                for (int i = 0; i < _fleet.Count; i++)
                {
                    if (i == _playerIndex || _roster.Contains(i) || !_assemblies[i].IsValid) continue;
                    int value = StatsFor(i).value;
                    if (value <= bestValue) continue;
                    best = i;
                    bestValue = value;
                }

                if (best < 0) break;
                if (!_roster.AssignFirstFree(best)) break;
                changed = true;
            }

            if (changed) CommitChange();
        }

        public void NextPage() => GoToPage(_page + 1);
        public void PreviousPage() => GoToPage(_page - 1);
        public void FirstPage() => GoToPage(0);
        public void LastPage() => GoToPage(PageCount - 1);

        public void GoToPage(int page)
        {
            int clamped = Mathf.Clamp(page, 0, PageCount - 1);
            if (clamped == _page) return;
            _page = clamped;
            RefreshTable();
        }

        /// <summary>Opens the placeholder shell that the remaining section tickets replace.</summary>
        public void OpenPlaceholder(string title)
        {
            if (_screen != null) _screen.style.display = DisplayStyle.None;
            if (_placeholder != null) _placeholder.style.display = DisplayStyle.Flex;
            if (_placeholderTitle != null) _placeholderTitle.text = title;
        }

        public void ShowCenter()
        {
            if (_screen != null) _screen.style.display = DisplayStyle.Flex;
            if (_placeholder != null) _placeholder.style.display = DisplayStyle.None;
        }

        // ---------------- Binding / skin ----------------

        private void Bind(VisualElement root)
        {
            _screen = root.Q("center-screen");
            _placeholder = root.Q("center-placeholder");
            _previewClip = root.Q("preview-clip");
            _previewCanvas = root.Q("preview-canvas");
            _companionSlots = root.Q("companion-slots");
            _craftList = root.Q<ScrollView>("craft-list");

            _battleLabel = root.Q<Label>("battle-label");
            _moneyLabel = root.Q<Label>("money-label");
            _previewName = root.Q<Label>("preview-name");
            _statName = root.Q<Label>("stat-name");
            _statHp = root.Q<Label>("stat-hp");
            _statFire = root.Q<Label>("stat-fire");
            _statSpeed = root.Q<Label>("stat-speed");
            _statSteering = root.Q<Label>("stat-steering");
            _statClass = root.Q<Label>("stat-class");
            _statParts = root.Q<Label>("stat-parts");
            _statValue = root.Q<Label>("stat-value");
            _inuseLabel = root.Q<Label>("inuse-label");
            _instructionLabel = root.Q<Label>("instruction-label");
            _pageLabel = root.Q<Label>("page-label");
            _placeholderTitle = root.Q<Label>("placeholder-title");

            _resetButton = root.Q<Button>("reset-button");
            _bestButton = root.Q<Button>("best-button");
            _helpButton = root.Q<Button>("help-button");
            _infoButton = root.Q<Button>("info-button");
            _readyButton = root.Q<Button>("ready-button");
            _shopButton = root.Q<Button>("shop-button");
            _factoryButton = root.Q<Button>("factory-button");
            _backButton = root.Q<Button>("back-button");
            _placeholderBack = root.Q<Button>("placeholder-back");
            _firstButton = root.Q<Button>("first-button");
            _previousButton = root.Q<Button>("prev-button");
            _nextButton = root.Q<Button>("next-button");
            _lastButton = root.Q<Button>("last-button");

            for (int i = 0; i < CompanionSlotCount; i++)
                _gradeButtons[i] = root.Q<Button>("grade-" + i);

            if (_resetButton != null) _resetButton.clicked += ResetCompanions;
            if (_bestButton != null) _bestButton.clicked += FillBestCompanions;
            if (_helpButton != null) _helpButton.clicked += () => OpenPlaceholder("HELP");
            if (_infoButton != null) _infoButton.clicked += () => InfoRequested?.Invoke();
            if (_readyButton != null) _readyButton.clicked += () =>
            {
                if (CanLaunch) ReadyRequested?.Invoke();
            };
            if (_shopButton != null) _shopButton.clicked += () => ShopRequested?.Invoke();
            if (_factoryButton != null) _factoryButton.clicked += () => FactoryRequested?.Invoke();
            if (_backButton != null) _backButton.clicked += () => MainMenuRequested?.Invoke();
            if (_placeholderBack != null) _placeholderBack.clicked += ShowCenter;
            if (_firstButton != null) _firstButton.clicked += FirstPage;
            if (_previousButton != null) _previousButton.clicked += PreviousPage;
            if (_nextButton != null) _nextButton.clicked += NextPage;
            if (_lastButton != null) _lastButton.clicked += LastPage;

            for (int i = 0; i < CompanionSlotCount; i++)
            {
                int slot = i;
                if (_gradeButtons[i] != null) _gradeButtons[i].clicked += () => RemoveCompanion(slot);
            }

            if (_craftList != null) _craftList.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            if (_previewClip != null) _previewClip.RegisterCallback<GeometryChangedEvent>(OnPreviewGeometryChanged);
        }

        private void ApplySkin(VisualElement root)
        {
            SetPanelBackground(root, "center-root", centerBackground);
            SetPanelBackground(root, "center-header", centerHeader);
            SetPanelBackground(root, "preview-frame", previewFrame);
            SetPanelBackground(root, "craft-table", panel);
            SetPanelBackground(root, "center-placeholder", panel);
            SetPanelBackground(root, "placeholder-panel", panel);

            SetSpriteBackground(root, "help-button", helpButton, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "ready-button", buttonReady, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "info-button", buttonPlate, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "shop-button", buttonPlate, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "factory-button", buttonPlate, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "back-button", buttonPlate, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "reset-button", buttonPlate, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "best-button", buttonPlate, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "first-button", buttonPlate, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "prev-button", buttonPlate, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "next-button", buttonPlate, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "last-button", buttonPlate, ScaleMode.ScaleAndCrop);
            SetSpriteBackground(root, "placeholder-back", buttonPlate, ScaleMode.ScaleAndCrop);

            for (int i = 0; i < CompanionSlotCount; i++)
                SetSpriteBackground(root, "grade-" + i, gradePlate, ScaleMode.ScaleAndCrop);
        }

        private static void SetPanelBackground(VisualElement root, string name, Sprite sprite)
        {
            VisualElement element = root.Q(name);
            if (element != null && sprite != null)
                UiSpriteSkin.Apply(element, sprite, ScaleMode.StretchToFill, keepBackground: false);
        }

        private static void SetSpriteBackground(VisualElement root, string name, Sprite sprite, ScaleMode scaleMode)
        {
            VisualElement element = root.Q(name);
            if (element != null && sprite != null)
                UiSpriteSkin.Apply(element, sprite, scaleMode, keepBackground: false);
        }

        // ---------------- Refresh ----------------

        private void CommitChange()
        {
            CaptureSave(_payload);
            RefreshAll();
            Changed?.Invoke();
        }

        private void RefreshAll()
        {
            if (!_initialized) return;
            RefreshHeader();
            RefreshTable();
            RefreshSelected();
            RefreshCompanions();
            RefreshReady();
        }

        private void RefreshReady()
        {
            if (_readyButton != null) _readyButton.SetEnabled(CanLaunch);
        }

        private void RefreshHeader()
        {
            if (_battleLabel != null) _battleLabel.text = "BATTLE-" + (_payload.storyIndex + 1);
            if (_moneyLabel != null) _moneyLabel.text = Money.ToString("N0", CultureInfo.InvariantCulture) + " G";
        }

        private void RefreshSelected()
        {
            if (_fleet.Count == 0) return;
            _selectedIndex = ClampCraftIndex(_selectedIndex);

            CraftShowStats stats = StatsFor(_selectedIndex);
            if (_statName != null) _statName.text = stats.name;
            if (_statHp != null) _statHp.text = stats.hp.ToString(CultureInfo.InvariantCulture);
            if (_statFire != null) _statFire.text = stats.fire.ToString("0.00", CultureInfo.InvariantCulture);
            if (_statSpeed != null) _statSpeed.text = stats.speed.ToString("0.0", CultureInfo.InvariantCulture);
            if (_statSteering != null) _statSteering.text = stats.steering.ToString("0.0", CultureInfo.InvariantCulture);
            if (_statClass != null) _statClass.text = stats.cls;
            if (_statParts != null) _statParts.text = stats.parts.ToString(CultureInfo.InvariantCulture);
            if (_statValue != null) _statValue.text = stats.value.ToString(CultureInfo.InvariantCulture);
            if (_previewName != null) _previewName.text = stats.name;
            if (_inuseLabel != null)
                _inuseLabel.style.display = IsInUse(_selectedIndex) ? DisplayStyle.Flex : DisplayStyle.None;

            if (_instructionLabel != null)
            {
                _instructionLabel.text = _assemblies[_selectedIndex].IsValid
                    ? "Pick crafts here and ready to fight!"
                    : "This craft needs a hull before it can launch.";
            }

            RefreshPreview();
        }

        private void RefreshCompanions()
        {
            bool hasCompanions = _roster.AssignedCount > 0;
            if (_companionSlots != null)
                _companionSlots.style.display = hasCompanions ? DisplayStyle.Flex : DisplayStyle.None;

            for (int slot = 0; slot < CompanionSlotCount; slot++)
            {
                Button button = _gradeButtons[slot];
                if (button == null) continue;

                int craftIndex = _roster.CraftIndex(slot);
                bool assigned = craftIndex >= 0;
                button.text = assigned ? StatsFor(craftIndex).cls : "-";
                UiSpriteSkin.SyncText(button);
                button.SetEnabled(assigned);
                button.tooltip = assigned ? "Remove companion " + (slot + 1) : "Empty companion slot";
            }
        }

        private void RefreshTable()
        {
            if (_craftList == null) return;

            _page = Mathf.Clamp(_page, 0, PageCount - 1);
            _craftList.Clear();

            int start = _page * PageSize;
            int end = Mathf.Min(start + PageSize, _fleet.Count);
            for (int index = start; index < end; index++)
                _craftList.Add(CreateCraftRow(index));

            if (_fleet.Count == 0)
                _craftList.Add(new Label("NO CRAFTS") { name = "craft-empty" });

            if (_pageLabel != null) _pageLabel.text = (_page + 1) + " / " + PageCount;
            if (_firstButton != null) _firstButton.SetEnabled(_page > 0);
            if (_previousButton != null) _previousButton.SetEnabled(_page > 0);
            if (_nextButton != null) _nextButton.SetEnabled(_page < PageCount - 1);
            if (_lastButton != null) _lastButton.SetEnabled(_page < PageCount - 1);
        }

        private VisualElement CreateCraftRow(int index)
        {
            var row = new VisualElement { name = "craft-row-" + index };
            row.AddToClassList("craft-row");
            row.AddToClassList("font-data");
            row.AddToClassList(index % 2 == 0 ? "craft-row-even" : "craft-row-odd");
            if (index == _selectedIndex) row.AddToClassList("craft-row-selected");
            if (IsInUse(index)) row.AddToClassList("craft-row-inuse");
            row.RegisterCallback<PointerDownEvent>(_ => SelectCraft(index));

            AddCell(row, index.ToString(CultureInfo.InvariantCulture), "col-index");
            AddCell(row, CraftName(index), "col-name");

            var playerCell = new Button(() => AssignPlayer(index))
            {
                name = "player-cell",
                text = index == _playerIndex ? "1" : "0",
                tooltip = "Use this craft"
            };
            playerCell.AddToClassList("cell-button");
            playerCell.AddToClassList("col-player");
            if (index == _playerIndex) playerCell.AddToClassList("cell-active");
            row.Add(playerCell);

            int slot = _roster.SlotOf(index);
            var aiCell = new Button(() => AssignCompanion(index))
            {
                name = "ai-cell",
                text = _roster.CountOf(index).ToString(CultureInfo.InvariantCulture),
                tooltip = slot >= 0 ? "Assign this craft to another AI slot" : "Assign as AI companion"
            };
            aiCell.AddToClassList("cell-button");
            aiCell.AddToClassList("col-ai");
            if (slot >= 0) aiCell.AddToClassList("cell-active");
            aiCell.SetEnabled(index != _playerIndex && IsCraftValid(index) && _roster.FirstFreeSlot() >= 0);
            row.Add(aiCell);

            return row;
        }

        private static void AddCell(VisualElement row, string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList("cell");
            label.AddToClassList(className);
            row.Add(label);
        }

        private void RefreshPreview()
        {
            if (_previewCanvas == null || _previewClip == null || _fleet.Count == 0) return;

            CraftAssembly assembly = _assemblies[ClampCraftIndex(_selectedIndex)];

            Span<MountedPartData> parts = stackalloc MountedPartData[CraftAssembly.MaxCapacity];
            int partCount = CraftPreviewPainter.CollectParts(assembly, _byId, parts);
            if (partCount == 0)
            {
                _previewCanvas.Clear();
                return;
            }

            Span<CraftPartPlacement> placements = stackalloc CraftPartPlacement[CraftAssembly.MaxCapacity];
            int placementCount = CraftPreviewPainter.PlaceParts(
                parts.Slice(0, partCount), _mountTable, placements);
            if (placementCount == 0)
            {
                _previewCanvas.Clear();
                return;
            }

            bool useMountArt = CraftPreviewPainter.AllUseMountArt(
                placements.Slice(0, placementCount), _mountArtLookup, out List<int> missingMountArt);
            PreviewUsesMountArt = useMountArt;
            if (!useMountArt)
            {
                Debug.LogWarning(
                    "[Center] No mount art for part id(s) " + string.Join(", ", missingMountArt) +
                    "; using the shop thumbnail set for the whole preview.");
            }

            float canvasWidth = _previewClip.resolvedStyle.width;
            float canvasHeight = _previewClip.resolvedStyle.height;
            if (float.IsNaN(canvasWidth) || float.IsNaN(canvasHeight) || canvasWidth <= 1f || canvasHeight <= 1f)
            {
                canvasWidth = 494f;
                canvasHeight = 510f;
            }

            CraftPreviewFit fit = CraftPreviewPainter.Fit(
                placements.Slice(0, placementCount), _byId, _mountArtLookup, useMountArt,
                canvasWidth, canvasHeight, PreviewAreaFill, PreviewMaxDimensionFill);
            if (fit.scale <= 0f) return;

            PreviewBoundsSize = fit.ScaledSize;
            PreviewFillFraction = PreviewBoundsSize.x * PreviewBoundsSize.y / (canvasWidth * canvasHeight);

            CraftPreviewPainter.Paint(
                _previewCanvas, placements.Slice(0, placementCount), _byId, _mountArtLookup,
                useMountArt, fit, canvasWidth, canvasHeight);
        }

        private void OnPreviewGeometryChanged(GeometryChangedEvent evt)
        {
            var size = new Vector2(evt.newRect.width, evt.newRect.height);
            if (Mathf.Approximately(size.x, _lastPreviewSize.x) && Mathf.Approximately(size.y, _lastPreviewSize.y)) return;
            _lastPreviewSize = size;
            RefreshPreview();
        }

        // ---------------- Craft catalog / seed fleet ----------------

        public CraftShowStats StatsFor(int index)
        {
            if (_fleet.Count == 0) return new CraftShowStats();
            index = ClampCraftIndex(index);

            CraftAssembly assembly = _assemblies[index];
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

            CraftBlueprintSO blueprint = _fleet[index];
            return CraftShowData.Evaluate(
                blueprint.craftName, blueprint.hullName,
                blueprint.hullMass, blueprint.hullHealth, parts.Slice(0, count));
        }

        public string CraftName(int index)
        {
            if (index < 0 || index >= _fleet.Count) return string.Empty;
            CraftBlueprintSO blueprint = _fleet[index];
            return string.IsNullOrEmpty(blueprint.craftName) ? blueprint.hullName : blueprint.craftName;
        }

        public bool IsInUse(int index)
        {
            return index == _playerIndex || _roster.Contains(index);
        }

        private int PageCount => Mathf.Max(1, (_fleet.Count + PageSize - 1) / PageSize);

        private int ClampCraftIndex(int index)
        {
            return _fleet.Count == 0 ? 0 : Mathf.Clamp(index, 0, _fleet.Count - 1);
        }

        private void RebuildFleet(List<BlueprintSaveData> saved)
        {
            bool newCampaign = saved == null || saved.Count == 0;
            for (int i = 0; i < _assemblies.Count; i++) _assemblies[i]?.Dispose();
            _assemblies.Clear();

            for (int i = 0; i < _fleet.Count; i++)
            {
                if (_fleet[i] != null) Destroy(_fleet[i]);
            }
            _fleet.Clear();

            if (saved != null)
            {
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

            CraftCatalog catalog = ResolveCraftCatalog();
            if (_fleet.Count == 0) _fleet.Add(CreatePlayerCraft(catalog));
            // Only a brand-new campaign seeds the AI fleet; reloading a save that already
            // owns its crafts must not append nine duplicates on every Center visit.
            if (newCampaign && _fleet.Count == 1) SeedAiCandidates(_fleet, catalog);

            for (int i = 0; i < _fleet.Count; i++) _assemblies.Add(BuildAssembly(_fleet[i]));
        }

        private PartDefinitionSO FirstMountedBody(CraftBlueprintSO blueprint)
        {
            if (blueprint.mountedParts == null) return null;
            for (int i = 0; i < blueprint.mountedParts.Length; i++)
            {
                MountedPartData part = blueprint.mountedParts[i];
                if (part.category != PartCategory.Body) continue;
                PartDefinitionSO definition = FindPartDefinition(part.partId);
                if (definition != null) return definition;
            }

            return null;
        }

        private void SeedAiCandidates(List<CraftBlueprintSO> fleet, CraftCatalog catalog)
        {
            string[] names = { "Wingman 01", "Wingman 02", "Wingman 03", "Wingman 04", "Wingman 05", "Wingman 06", "Wingman 07", "Wingman 08", "Wingman 09" };
            if (catalog != null)
            {
                var frozen = new List<CraftBlueprintSO>(names.Length);
                bool complete = true;
                for (int i = 0; i < names.Length; i++)
                {
                    CraftBlueprintSO blueprint = CreateFrozenCraft(catalog, i + 1, names[i]);
                    if (blueprint == null)
                    {
                        complete = false;
                        break;
                    }
                    frozen.Add(blueprint);
                }

                if (complete)
                {
                    fleet.AddRange(frozen);
                    return;
                }

                for (int i = 0; i < frozen.Count; i++) Destroy(frozen[i]);
            }

            fleet.Add(CreateBlueprint("Wingman 01", 1, 173, 222, 134, 72));
            fleet.Add(CreateBlueprint("Wingman 02", 2, 174, 223, 135, 73));
            fleet.Add(CreateBlueprint("Wingman 03", 3, 175, 224, 136, 74));
            fleet.Add(CreateBlueprint("Wingman 04", 7, 176, 225, 137, 140, 72, 78));
            fleet.Add(CreateBlueprint("Wingman 05", 10, 177, 226, 138, 141, 80, 81));
            fleet.Add(CreateBlueprint("Wingman 06", 11, 178, 227, 139, 142, 82, 83));
            fleet.Add(CreateBlueprint("Wingman 07", 18, 181, 230, 143, 144, 84, 85));
            fleet.Add(CreateBlueprint("Wingman 08", 19, 182, 231, 145, 146, 86, 87));
            fleet.Add(CreateBlueprint("Wingman 09", 36, 186, 235, 143, 144, 84, 85));
        }

        private CraftBlueprintSO CreatePlayerCraft(CraftCatalog catalog)
        {
            return CreateFrozenCraft(catalog, 0, "Michael's")
                   ?? CreateBlueprint("Michael's", 0, 173, 222, 133, 71);
        }

        /// <summary>
        /// Builds a blueprint from a frozen defFly tree and bakes the decoded socket placements
        /// into localOffsets, so the Center preview and the mission use identical geometry.
        /// </summary>
        private CraftBlueprintSO CreateFrozenCraft(CraftCatalog catalog, int defIndex, string craftName)
        {
            CraftStatRecord craft = catalog != null ? catalog.Find(defIndex) : null;
            if (craft == null || craft.parts == null || craft.parts.Length == 0) return null;

            PartDefinitionSO hull = FindPartDefinition(craft.hullPartId);
            if (hull == null) return null;

            var blueprint = ScriptableObject.CreateInstance<CraftBlueprintSO>();
            blueprint.craftName = craftName;
            blueprint.hullName = hull.partName;
            blueprint.hullCategory = CraftBlueprintSO.GetHullCategoryForName(hull.partName);
            blueprint.hullMass = hull.mass;
            blueprint.hullHealth = hull.health;

            Span<CraftPartPlacement> placements = stackalloc CraftPartPlacement[CraftAssembly.MaxCapacity];
            int placementCount = _mountTable != null
                ? CraftComposition.Build(craft, _mountTable, placements)
                : 0;

            var mounted = new List<MountedPartData>(craft.parts.Length);
            for (int i = 0; i < craft.parts.Length; i++)
            {
                CraftPartRecord record = craft.parts[i];
                PartDefinitionSO definition = FindPartDefinition(record.partId);
                if (definition == null) continue;

                MountedPartData part = FromDefinition(definition);
                part.mirrored = record.lr == 1;
                if (i < placementCount && placements[i].partIndex == i)
                {
                    part.localOffset = placements[i].localOffset;
                    part.localRotation = placements[i].localRotation;
                }
                mounted.Add(part);
            }

            blueprint.mountedParts = mounted.ToArray();
            return blueprint;
        }

        private CraftBlueprintSO CreateBlueprint(string name, int bodyPartId, params int[] partIds)
        {
            PartDefinitionSO body = FindPartDefinition(bodyPartId) ?? FindPartDefinition(0);
            var blueprint = ScriptableObject.CreateInstance<CraftBlueprintSO>();
            blueprint.craftName = name;
            if (body == null)
            {
                blueprint.hullName = "B.Small.01";
                blueprint.hullCategory = HullCategory.Small;
                blueprint.hullMass = 100f;
                blueprint.hullHealth = 1300f;
                blueprint.mountedParts = Array.Empty<MountedPartData>();
                return blueprint;
            }

            blueprint.hullName = body.partName;
            blueprint.hullCategory = CraftBlueprintSO.GetHullCategoryForName(body.partName);
            blueprint.hullMass = body.mass;
            blueprint.hullHealth = body.health;

            var mounted = new List<MountedPartData>(partIds.Length + 1) { FromDefinition(body) };
            for (int i = 0; i < partIds.Length; i++)
            {
                PartDefinitionSO definition = FindPartDefinition(partIds[i]);
                if (definition == null || definition.category == PartCategory.Body) continue;
                mounted.Add(FromDefinition(definition));
            }

            blueprint.mountedParts = mounted.ToArray();
            return blueprint;
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
                energyDraw = definition.energyDraw,
                mirrored = definition.lr == 1
            };
        }

        private CraftAssembly BuildAssembly(CraftBlueprintSO source)
        {
            var assembly = new CraftAssembly(source.hullName, source.hullMass, source.hullHealth, source.isGrandfathered);
            if (source.mountedParts != null)
            {
                for (int i = 0; i < source.mountedParts.Length; i++)
                {
                    MountedPartData part = source.mountedParts[i];
                    AssemblyError error = assembly.TryMountPart(part, out _);
                    if (error != AssemblyError.None)
                        Debug.LogWarning($"[Center] Blueprint part {part.partId} was not mounted: {error}.");
                }
            }

            return assembly;
        }

        private void ReconcileShop(int storyIndex)
        {
            var hullNames = new List<string>(_fleet.Count);
            for (int i = 0; i < _fleet.Count; i++)
            {
                if (!string.IsNullOrEmpty(_fleet[i].hullName)) hullNames.Add(_fleet[i].hullName);
            }

            _shop.ReconcileStock(BuildDefaultGifts(_definitions), storyIndex, hullNames);
        }

        private static int[][] BuildDefaultGifts(IReadOnlyList<PartDefinitionSO> definitions)
        {
            var partIds = new int[definitions.Count];
            for (int i = 0; i < definitions.Count; i++) partIds[i] = definitions[i].partId;
            return new[] { partIds };
        }

        private int FirePointCount(int partId)
        {
            return _mountTable != null && _mountTable.TryGetPart(partId, out PartMountEntry entry) &&
                   _mountTable.TryGetSymbol(entry.mcName, out PartMountSymbol symbol)
                ? symbol.firePoints.Length
                : 0;
        }

        private CraftCatalog ResolveCraftCatalog()
        {
            if (_craftCatalog != null) return _craftCatalog;

            try
            {
                TextAsset defFly = Resources.Load<TextAsset>("Data/3_all.AllData_defFly");
                TextAsset defPart = Resources.Load<TextAsset>("Data/8_all.AllData_defPart");
                TextAsset defFire = Resources.Load<TextAsset>("Data/2_all.AllData_defFire");
                TextAsset defBullet = Resources.Load<TextAsset>("Data/11_all.AllData_defBullet");
                if (defFly == null || defPart == null || defFire == null || defBullet == null)
                {
                    Debug.LogWarning("[Center] Frozen craft definitions missing from Resources/Data; using the fallback fleet.");
                    return null;
                }

                _craftCatalog = CraftCatalogParser.Parse(defFly.bytes, defPart.text, defFire.text, defBullet.text);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Center] Frozen craft definitions unavailable: {exception.Message}");
                _craftCatalog = null;
            }

            return _craftCatalog;
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _assemblies.Count; i++) _assemblies[i]?.Dispose();
            _assemblies.Clear();

            for (int i = 0; i < _fleet.Count; i++)
            {
                if (_fleet[i] != null) Destroy(_fleet[i]);
            }
            _fleet.Clear();
        }
    }
}
