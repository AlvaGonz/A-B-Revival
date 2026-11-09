using System;
using System.Collections.Generic;
using System.Globalization;
using ABRevival.Core.Data;
using ABRevival.Core.Persistence;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Scene entry point for Info/Records (the frozen UI_HelpGame pages plus the UI_Data
    /// records shell): the two How-to-play sheets, the control reference for both control
    /// modes and the revival bindings, and a read-only projection of the single save
    /// (story, money, fleet, companions, owned parts and camera distance).
    /// It never captures or mutates the payload; <see cref="GameFlowController"/> owns the save.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class InfoController : MonoBehaviour
    {
        private const int HelpPageCount = 2;

        [SerializeField] private PartDefinitionSO[] catalogParts = Array.Empty<PartDefinitionSO>();

        [Header("Info sprites (frozen 2011 UI library)")]
        [SerializeField] private Sprite infoBackground;
        [SerializeField] private Sprite infoHeader;
        [SerializeField] private Sprite buttonPlate;
        [SerializeField] private Sprite backButton;
        [SerializeField] private Sprite helpPageOne;
        [SerializeField] private Sprite helpPageTwo;

        private UIDocument _document;
        private Dictionary<int, PartDefinitionSO> _byId;
        private PlayerSaveData _payload = new PlayerSaveData();

        private VisualElement _helpPage;
        private VisualElement _recordsPage;
        private VisualElement _helpArt;
        private ScrollView _partsList;
        private Label _moneyLabel;
        private Label _helpPageLabel;
        private Label _recordsStory;
        private Label _recordsMoney;
        private Label _recordsPlayer;
        private Label _recordsFleet;
        private Label _recordsCompanions;
        private Label _recordsPartsTotal;
        private Label _recordsCamera;
        private Label _partsCount;
        private Button _helpTab;
        private Button _recordsTab;
        private Button _previousPage;
        private Button _nextPage;

        private int _helpIndex;
        private bool _showingRecords;
        private bool _initialized;

        /// <summary>Raised when the player asks to return to Center.</summary>
        public event Action Closed;

        public bool ShowingRecords => _showingRecords;

        public int HelpPageIndex => _helpIndex;

        public int OwnedPartTypes => InfoRecords.TallyOwnedParts(OwnedParts()).Count;

        private void Awake()
        {
            // GameFlowController owns visibility and opens the screen from Center.
            var document = GetComponent<UIDocument>();
            if (document != null && document.rootVisualElement != null)
                document.rootVisualElement.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Builds the Info UI once and applies the shared player save. GameFlowController
        /// calls this when Center's INFO button raises the screen change.
        /// </summary>
        public void Initialize(PlayerSaveData save)
        {
            if (!_initialized)
            {
                _document = GetComponent<UIDocument>();
                if (_document == null || _document.rootVisualElement == null)
                {
                    Debug.LogError("[Info] UIDocument with a source UXML is required.");
                    return;
                }

                _byId = new Dictionary<int, PartDefinitionSO>();
                if (catalogParts != null)
                {
                    for (int i = 0; i < catalogParts.Length; i++)
                    {
                        PartDefinitionSO definition = catalogParts[i];
                        if (definition != null) _byId[definition.partId] = definition;
                    }
                }

                VisualElement root = _document.rootVisualElement;
                ApplySkin(root);
                Bind(root);
                _initialized = true;
            }

            ApplySave(save);
            SetVisible(true);
        }

        /// <summary>Rebuilds the read-only records from the shared payload.</summary>
        public void ApplySave(PlayerSaveData payload)
        {
            if (!_initialized || payload == null) return;

            _payload = payload;
            _helpIndex = 0;
            _showingRecords = false;
            RefreshHeader();
            RefreshRecords();
            ShowHelp();
        }

        /// <summary>Shows or hides the whole Info overlay.</summary>
        public void SetVisible(bool visible)
        {
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_document != null && _document.rootVisualElement != null)
                _document.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void ShowHelp()
        {
            _showingRecords = false;
            SetTabs();
            if (_helpPage != null) _helpPage.style.display = DisplayStyle.Flex;
            if (_recordsPage != null) _recordsPage.style.display = DisplayStyle.None;
            ShowHelpPage(_helpIndex);
        }

        public void ShowRecords()
        {
            _showingRecords = true;
            SetTabs();
            if (_helpPage != null) _helpPage.style.display = DisplayStyle.None;
            if (_recordsPage != null) _recordsPage.style.display = DisplayStyle.Flex;
            RefreshRecords();
        }

        public void NextHelpPage()
        {
            if (_helpIndex < HelpPageCount - 1) ShowHelpPage(_helpIndex + 1);
        }

        public void PreviousHelpPage()
        {
            if (_helpIndex > 0) ShowHelpPage(_helpIndex - 1);
        }

        public void ShowHelpPage(int index)
        {
            if (!_initialized) return;

            _helpIndex = Mathf.Clamp(index, 0, HelpPageCount - 1);
            _showingRecords = false;
            SetTabs();
            if (_helpPage != null) _helpPage.style.display = DisplayStyle.Flex;
            if (_recordsPage != null) _recordsPage.style.display = DisplayStyle.None;

            Sprite page = _helpIndex == 0 ? helpPageOne : helpPageTwo;
            if (_helpArt != null && page != null)
                UiSpriteSkin.Apply(_helpArt, page, ScaleMode.ScaleToFit, keepBackground: false);

            if (_helpPageLabel != null)
                _helpPageLabel.text = (_helpIndex + 1) + " / " + HelpPageCount;
            if (_previousPage != null) _previousPage.SetEnabled(_helpIndex > 0);
            if (_nextPage != null) _nextPage.SetEnabled(_helpIndex < HelpPageCount - 1);
        }

        // ---------------- Binding / skin ----------------

        private void Bind(VisualElement root)
        {
            _helpPage = root.Q("help-page");
            _recordsPage = root.Q("records-page");
            _helpArt = root.Q("help-art");
            _partsList = root.Q<ScrollView>("parts-list");

            _moneyLabel = root.Q<Label>("money-label");
            _helpPageLabel = root.Q<Label>("help-page-label");
            _recordsStory = root.Q<Label>("records-story");
            _recordsMoney = root.Q<Label>("records-money");
            _recordsPlayer = root.Q<Label>("records-player");
            _recordsFleet = root.Q<Label>("records-fleet");
            _recordsCompanions = root.Q<Label>("records-companions");
            _recordsPartsTotal = root.Q<Label>("records-parts-total");
            _recordsCamera = root.Q<Label>("records-camera");
            _partsCount = root.Q<Label>("parts-count");

            _helpTab = root.Q<Button>("help-tab");
            _recordsTab = root.Q<Button>("records-tab");
            _previousPage = root.Q<Button>("help-prev");
            _nextPage = root.Q<Button>("help-next");

            if (_helpTab != null) _helpTab.clicked += ShowHelp;
            if (_recordsTab != null) _recordsTab.clicked += ShowRecords;
            if (_previousPage != null) _previousPage.clicked += PreviousHelpPage;
            if (_nextPage != null) _nextPage.clicked += NextHelpPage;

            Button back = root.Q<Button>("back-button");
            if (back != null) back.clicked += () => Closed?.Invoke();

            if (_partsList != null) _partsList.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        }

        private void SetTabs()
        {
            if (_helpTab != null) _helpTab.EnableInClassList("tab-active", !_showingRecords);
            if (_recordsTab != null) _recordsTab.EnableInClassList("tab-active", _showingRecords);
        }

        private void ApplySkin(VisualElement root)
        {
            SetSprite(root, "info-root", infoBackground, ScaleMode.ScaleAndCrop);
            SetSprite(root, "info-title-plate", infoHeader, ScaleMode.ScaleAndCrop);
            SetSprite(root, "records-title-plate", infoHeader, ScaleMode.ScaleAndCrop);
            SetSprite(root, "help-tab", buttonPlate, ScaleMode.ScaleAndCrop);
            SetSprite(root, "records-tab", buttonPlate, ScaleMode.ScaleAndCrop);
            SetSprite(root, "back-button", backButton, ScaleMode.ScaleToFit);
        }

        private static void SetSprite(VisualElement root, string name, Sprite sprite, ScaleMode scaleMode)
        {
            VisualElement element = root.Q(name);
            if (element != null && sprite != null)
                UiSpriteSkin.Apply(element, sprite, scaleMode, keepBackground: false);
        }

        // ---------------- Read-only refresh ----------------

        private void RefreshHeader()
        {
            if (_moneyLabel != null)
                _moneyLabel.text = Money().ToString("N0", CultureInfo.InvariantCulture) + " G";
        }

        private void RefreshRecords()
        {
            int money = Money();
            int fleet = _payload.blueprints != null ? _payload.blueprints.Count : 0;
            int owned = InfoRecords.CountOwnedParts(OwnedParts());

            if (_recordsStory != null) _recordsStory.text = InfoRecords.StoryLabel(_payload.storyIndex);
            if (_recordsMoney != null)
                _recordsMoney.text = money.ToString("N0", CultureInfo.InvariantCulture) + " G";
            if (_recordsPlayer != null) _recordsPlayer.text = PlayerCraftName();
            if (_recordsFleet != null) _recordsFleet.text = fleet + (fleet == 1 ? " craft" : " crafts");
            if (_recordsCompanions != null)
                _recordsCompanions.text = InfoRecords.CountAssignedCompanions(_payload.companions) +
                                          " / " + InfoRecords.CompanionSlots;
            if (_recordsPartsTotal != null) _recordsPartsTotal.text = owned + (owned == 1 ? " unit" : " units");
            if (_recordsCamera != null) _recordsCamera.text = InfoRecords.CameraLabel(_payload.cameraPreset);

            RefreshPartsTable();
        }

        private void RefreshPartsTable()
        {
            if (_partsList == null) return;

            _partsList.Clear();
            List<OwnedPartQuantity> tallies = InfoRecords.TallyOwnedParts(OwnedParts());
            if (_partsCount != null)
                _partsCount.text = tallies.Count + (tallies.Count == 1 ? " type" : " types");

            if (tallies.Count == 0)
            {
                _partsList.Add(new Label("No parts owned") { name = "parts-empty", pickingMode = PickingMode.Ignore });
                return;
            }

            for (int i = 0; i < tallies.Count; i++)
                _partsList.Add(CreatePartRow(tallies[i], i));
        }

        private VisualElement CreatePartRow(in OwnedPartQuantity tally, int row)
        {
            var element = new VisualElement { name = "part-row-" + tally.partId, userData = tally.partId };
            element.AddToClassList("part-row");
            element.AddToClassList("font-data");
            element.AddToClassList(row % 2 == 0 ? "part-row-even" : "part-row-odd");

            _byId.TryGetValue(tally.partId, out PartDefinitionSO definition);
            string name = definition != null && !string.IsNullOrEmpty(definition.partName)
                ? definition.partName
                : "Part " + tally.partId;
            string type = definition != null ? CategoryLabel(definition.category) : "Unknown";

            AddPartCell(element, tally.partId.ToString(CultureInfo.InvariantCulture), "col-part-id");
            AddPartCell(element, name, "col-part-name");
            AddPartCell(element, type, "col-part-type");
            AddPartCell(element, tally.quantity.ToString(CultureInfo.InvariantCulture), "col-part-qty");
            return element;
        }

        private static void AddPartCell(VisualElement row, string text, string className)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("part-cell");
            label.AddToClassList(className);
            row.Add(label);
        }

        private static string CategoryLabel(PartCategory category)
        {
            return category switch
            {
                PartCategory.Body => "Body",
                PartCategory.Wing => "Wing",
                PartCategory.Engine => "Engine",
                PartCategory.Gun => "Gun",
                _ => "Unknown"
            };
        }

        private IReadOnlyList<int> OwnedParts()
        {
            return _payload.shop != null ? _payload.shop.ownedParts : null;
        }

        private int Money()
        {
            return _payload.shop != null ? _payload.shop.money : 0;
        }

        private string PlayerCraftName()
        {
            if (_payload.blueprints == null || _payload.blueprints.Count == 0) return "-";

            int index = Mathf.Clamp(_payload.playerBlueprintIndex, 0, _payload.blueprints.Count - 1);
            BlueprintSaveData blueprint = _payload.blueprints[index];
            if (blueprint == null) return "-";
            if (!string.IsNullOrEmpty(blueprint.craftName)) return blueprint.craftName;
            return string.IsNullOrEmpty(blueprint.hullName) ? "-" : blueprint.hullName;
        }
    }
}
