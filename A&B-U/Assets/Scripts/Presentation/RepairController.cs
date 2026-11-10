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
    /// Dedicated Repair surface for UIRepair: the damaged mounted parts left by the mission,
    /// their frozen FormRepair value (70% of the catalog cost) and the ACCEPT / RESET /
    /// SELECT ALL / EXIT actions. Accepting restores the part in place; EXIT (Skip) leaves the
    /// remaining damage for <see cref="GameFlowController"/>'s Next, which returns the parts
    /// to the shop exactly like UIWin.toNext -> RepairPartsCtrl.removeParts.
    /// The screen mutates only the shared save slice; it never captures Center's fleet.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [RequireComponent(typeof(ScreenPanel))]
    public sealed class RepairController : MonoBehaviour
    {
        private const float PageWidth = 1920f;
        private const float PageHeight = 1080f;
        private const int PageSize = 10;

        [SerializeField] private PartDefinitionSO[] catalogParts = Array.Empty<PartDefinitionSO>();

        [Header("Repair hover/pressed sprites (frozen 2011 UI library)")]
        [SerializeField] private Sprite exitButtonHover;
        [SerializeField] private Sprite exitButtonPressed;
        [SerializeField] private Sprite resetButtonHover;
        [SerializeField] private Sprite resetButtonPressed;
        [SerializeField] private Sprite acceptButtonHover;
        [SerializeField] private Sprite acceptButtonPressed;
        [SerializeField] private Sprite selectAllButtonHover;
        [SerializeField] private Sprite selectAllButtonPressed;

        private ScreenPanel _screen;
        private VisualElement _page;
        private VisualElement _rows;
        private VisualElement _helpOverlay;
        private Label _money;
        private Label _cost;
        private Label _now;
        private Label _error;
        private Label _pageLabel;
        private Label _craftList;
        private Button _accept;
        private Button _first;
        private Button _previous;
        private Button _next;
        private Button _last;
        private Button _filterAll;
        private Button _filterBody;
        private Button _filterWing;
        private Button _filterEngine;
        private Button _filterGun;

        private PlayerSaveData _payload;
        private RepairState _repair;
        private readonly List<int> _visible = new List<int>(PageSize);
        private PartCategory? _filter;
        private int _pageIndex;
        private bool _initialized;
        private Vector2 _lastRootSize;

        /// <summary>Raised when repair/skip returns to the result screen.</summary>
        public event Action Closed;

        /// <summary>Raised after an accepted repair transaction so GameFlow can autosave.</summary>
        public event Action Changed;

        public RepairState Repair => _repair;

        public int VisibleRowCount => _visible.Count;

        public int PageIndex => _pageIndex;

        /// <summary>Builds the ledger from the screen's imported part catalog (shared with the flow).</summary>
        public RepairState CreateRepairState(PlayerSaveData save, IReadOnlyList<DamagedPart> damage)
        {
            return RepairState.Create(save, damage, ResolvePartInfo);
        }

        private RepairPartInfo ResolvePartInfo(int partId)
        {
            if (catalogParts != null)
            {
                for (int i = 0; i < catalogParts.Length; i++)
                {
                    PartDefinitionSO definition = catalogParts[i];
                    if (definition == null || definition.partId != partId) continue;
                    float firePower = definition.category == PartCategory.Gun
                        ? definition.CalculateDisplayFirePower(1)
                        : 0f;
                    return new RepairPartInfo(
                        definition.partName,
                        definition.category,
                        definition.lr,
                        definition.health,
                        definition.mass,
                        definition.thrust,
                        definition.turnFly,
                        firePower,
                        definition.cost);
                }
            }

            return default;
        }


        private void Awake()
        {
            // GameFlowController owns visibility and opens the screen from the result.
            _screen = GetComponent<ScreenPanel>();
            if (_screen != null) _screen.SetVisible(false);
        }

        /// <summary>Binds the screen once and reloads the live damaged-part ledger.</summary>
        public void Initialize(PlayerSaveData save, RepairState repair)
        {
            _screen = GetComponent<ScreenPanel>();
            if (_screen == null)
            {
                Debug.LogError("[Repair] ScreenPanel with a PanelRenderer is required.");
                return;
            }

            _screen.WhenReady(root => Open(save, repair, root));
        }

        private void Open(PlayerSaveData save, RepairState repair, VisualElement root)
        {
            if (!_initialized)
            {
                Bind(root);
                root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
                if (root.resolvedStyle.width > 1f && root.resolvedStyle.height > 1f)
                    FitPage(root.resolvedStyle.width, root.resolvedStyle.height);
                _initialized = true;
            }

            _payload = save;
            _repair = repair;
            _pageIndex = 0;
            _error.text = string.Empty;
            Refresh();
            SetVisible(true);
        }

        /// <summary>Shows or hides the whole Repair overlay.</summary>
        public void SetVisible(bool visible)
        {
            if (_screen == null) _screen = GetComponent<ScreenPanel>();
            _screen?.SetVisible(visible);
        }

        private void Bind(VisualElement root)
        {
            _page = root.Q("repair-page");
            _rows = root.Q("repair-rows");
            _helpOverlay = root.Q("repair-help");
            _money = root.Q<Label>("repair-money");
            _cost = root.Q<Label>("repair-cost");
            _now = root.Q<Label>("repair-now");
            _error = root.Q<Label>("repair-error");
            _pageLabel = root.Q<Label>("repair-page-label");
            _craftList = root.Q<Label>("repair-craft-list");
            _accept = root.Q<Button>("repair-accept");
            _first = root.Q<Button>("repair-first");
            _previous = root.Q<Button>("repair-prev");
            _next = root.Q<Button>("repair-next");
            _last = root.Q<Button>("repair-last");

            _filterAll = root.Q<Button>("repair-filter-all");
            _filterBody = root.Q<Button>("repair-filter-body");
            _filterWing = root.Q<Button>("repair-filter-wing");
            _filterEngine = root.Q<Button>("repair-filter-engine");
            _filterGun = root.Q<Button>("repair-filter-gun");

            Button exit = root.Q<Button>("repair-exit");
            Button reset = root.Q<Button>("repair-reset");
            Button selectAll = root.Q<Button>("repair-all");
            Button help = root.Q<Button>("repair-help-button");

            if (exit != null) exit.clicked += OnExit;
            if (reset != null) reset.clicked += OnReset;
            if (selectAll != null) selectAll.clicked += OnSelectAll;
            if (help != null) help.clicked += ShowHelp;
            if (_accept != null) _accept.clicked += OnAccept;
            if (_first != null) _first.clicked += () => GoToPage(0);
            if (_previous != null) _previous.clicked += () => GoToPage(_pageIndex - 1);
            if (_next != null) _next.clicked += () => GoToPage(_pageIndex + 1);
            if (_last != null) _last.clicked += () => GoToPage(FilteredPageCount - 1);

            if (_filterAll != null) _filterAll.clicked += () => SetFilter(null);
            if (_filterBody != null) _filterBody.clicked += () => SetFilter(PartCategory.Body);
            if (_filterWing != null) _filterWing.clicked += () => SetFilter(PartCategory.Wing);
            if (_filterEngine != null) _filterEngine.clicked += () => SetFilter(PartCategory.Engine);
            if (_filterGun != null) _filterGun.clicked += () => SetFilter(PartCategory.Gun);

            // Static normal plates live in Repair.uxml; only hover/pressed frames stay here.
            BindStates(exit, exitButtonHover, exitButtonPressed);
            BindStates(reset, resetButtonHover, resetButtonPressed);
            BindStates(_accept, acceptButtonHover, acceptButtonPressed);
            BindStates(selectAll, selectAllButtonHover, selectAllButtonPressed);

            if (_helpOverlay != null)
                _helpOverlay.RegisterCallback<ClickEvent>(_ => HideHelp());
        }

        /// <summary>Swaps the UXML-authored plate for the frozen hover/pressed frames.</summary>
        private static void BindStates(Button button, Sprite hover, Sprite pressed)
        {
            if (button == null) return;
            SpriteElement art = button.Q<SpriteElement>("button-art");
            if (art == null || art.sprite == null) return;

            Sprite normal = art.sprite;
            Sprite hoverFrame = hover != null ? hover : normal;
            Sprite pressedFrame = pressed != null ? pressed : normal;
            button.RegisterCallback<PointerEnterEvent>(_ => art.sprite = hoverFrame);
            button.RegisterCallback<PointerLeaveEvent>(_ => art.sprite = normal);
            button.RegisterCallback<PointerDownEvent>(_ => art.sprite = pressedFrame);
            button.RegisterCallback<PointerUpEvent>(_ => art.sprite = hoverFrame);
        }

        // ---------------- Table ----------------

        private int FilteredCount
        {
            get
            {
                if (_repair == null) return 0;
                if (!_filter.HasValue) return _repair.Items.Count;

                int count = 0;
                for (int i = 0; i < _repair.Items.Count; i++)
                    if (_repair.Items[i].Category == _filter.Value) count++;
                return count;
            }
        }

        private int FilteredPageCount => Mathf.Max(1, (FilteredCount + PageSize - 1) / PageSize);

        private void Refresh()
        {
            if (!_initialized) return;

            // A partial accept can shrink the ledger below the current page.
            _pageIndex = Mathf.Clamp(_pageIndex, 0, FilteredPageCount - 1);
            RefreshHeader();
            RefreshCraftList();
            RefreshRows();
            RefreshSelection();
            RefreshPager();
        }

        private void RefreshHeader()
        {
            if (_money != null)
                _money.text = (_payload?.shop != null ? _payload.shop.money : 0)
                    .ToString("N0", CultureInfo.InvariantCulture) + " G";
        }

        private void RefreshCraftList()
        {
            if (_craftList == null) return;
            if (_repair == null || _repair.Items.Count == 0)
            {
                _craftList.text = "No damaged craft.\nEvery part is at full health.";
                return;
            }

            var lines = new List<string>();
            var names = new List<string>();
            var counts = new List<int>();
            for (int i = 0; i < _repair.Items.Count; i++)
            {
                string craft = _repair.Items[i].CraftName;
                int index = names.IndexOf(craft);
                if (index < 0)
                {
                    names.Add(craft);
                    counts.Add(1);
                }
                else
                {
                    counts[index]++;
                }
            }

            for (int i = 0; i < names.Count; i++)
                lines.Add(names[i] + "   " + counts[i] + (counts[i] == 1 ? " damaged part" : " damaged parts"));
            _craftList.text = string.Join("\n", lines);
        }

        private void RefreshRows()
        {
            if (_rows == null || _repair == null) return;
            _rows.Clear();
            _visible.Clear();

            BuildVisibleIndices();
            int first = _pageIndex * PageSize;
            int shown = Mathf.Min(PageSize, Mathf.Max(0, _visible.Count - first));

            for (int row = 0; row < shown; row++)
            {
                int itemIndex = _visible[first + row];
                RepairItem item = _repair.Items[itemIndex];
                _rows.Add(CreateRow(item, itemIndex, row));
            }

            if (shown == 0)
            {
                var empty = new Label(_repair.Items.Count == 0 ? "No damaged parts" : "No parts in this category")
                {
                    name = "repair-empty",
                    pickingMode = PickingMode.Ignore
                };
                empty.AddToClassList("repair-empty");
                _rows.Add(empty);
            }
        }

        private void BuildVisibleIndices()
        {
            _visible.Clear();
            for (int i = 0; i < _repair.Items.Count; i++)
            {
                if (_filter.HasValue && _repair.Items[i].Category != _filter.Value) continue;
                _visible.Add(i);
            }
        }

        private VisualElement CreateRow(RepairItem item, int itemIndex, int row)
        {
            var element = new VisualElement { name = "repair-row-" + itemIndex, userData = itemIndex };
            element.AddToClassList("repair-row");
            element.AddToClassList("font-data");
            element.AddToClassList(row % 2 == 0 ? "repair-row-even" : "repair-row-odd");
            element.EnableInClassList("repair-row-selected", item.Selected);

            AddCell(element, (itemIndex + 1).ToString(CultureInfo.InvariantCulture), "col-index");
            AddCell(element, item.CraftName, "col-craft");
            AddCell(element, item.Name, "col-name");
            AddCell(element, CategoryLabel(item.Category), "col-type");
            AddCell(element, item.Lr == 0 ? "L" : "R", "col-lr");
            AddCell(element, Mathf.RoundToInt(item.Health).ToString(CultureInfo.InvariantCulture), "col-hp");
            AddCell(element, (item.TurnFly * Mathf.Rad2Deg).ToString("0.0", CultureInfo.InvariantCulture), "col-steering");
            AddCell(element, item.Thrust.ToString("0.0", CultureInfo.InvariantCulture), "col-engine");
            AddCell(element, item.Mass.ToString("0.0", CultureInfo.InvariantCulture), "col-mass");
            AddCell(element, item.FirePower.ToString("0.00", CultureInfo.InvariantCulture), "col-fire");
            AddCell(element, item.RepairCost.ToString(CultureInfo.InvariantCulture), "col-value");
            AddCell(element, item.Selected ? "Y" : "N", "col-sel");

            element.RegisterCallback<ClickEvent>(_ => OnRowClicked(itemIndex));
            return element;
        }

        private static void AddCell(VisualElement row, string text, string className)
        {
            var label = new Label(text) { pickingMode = PickingMode.Ignore };
            label.AddToClassList("repair-cell");
            label.AddToClassList(className);
            row.Add(label);
        }

        private void OnRowClicked(int itemIndex)
        {
            if (_repair == null) return;
            _repair.Toggle(itemIndex);
            RefreshRows();
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            if (_repair == null) return;
            int cost = _repair.SelectedCost;
            int money = _repair.Money;
            if (_cost != null) _cost.text = cost.ToString("N0", CultureInfo.InvariantCulture) + " G";
            if (_now != null) _now.text = (money - cost).ToString("N0", CultureInfo.InvariantCulture) + " G";
            if (_now != null) _now.EnableInClassList("repair-negative", money - cost < 0);
            if (_accept != null) _accept.SetEnabled(_repair.CanApply);
        }

        private void RefreshPager()
        {
            if (_pageLabel != null)
                _pageLabel.text = (_pageIndex + 1) + " / " + FilteredPageCount;
            if (_first != null) _first.SetEnabled(_pageIndex > 0);
            if (_previous != null) _previous.SetEnabled(_pageIndex > 0);
            if (_next != null) _next.SetEnabled(_pageIndex < FilteredPageCount - 1);
            if (_last != null) _last.SetEnabled(_pageIndex < FilteredPageCount - 1);

            if (_filterAll != null) _filterAll.EnableInClassList("filter-active", !_filter.HasValue);
            if (_filterBody != null) _filterBody.EnableInClassList("filter-active", _filter == PartCategory.Body);
            if (_filterWing != null) _filterWing.EnableInClassList("filter-active", _filter == PartCategory.Wing);
            if (_filterEngine != null) _filterEngine.EnableInClassList("filter-active", _filter == PartCategory.Engine);
            if (_filterGun != null) _filterGun.EnableInClassList("filter-active", _filter == PartCategory.Gun);
        }

        private void SetFilter(PartCategory? category)
        {
            _filter = category;
            _pageIndex = 0;
            _error.text = string.Empty;
            Refresh();
        }

        private void GoToPage(int page)
        {
            _pageIndex = Mathf.Clamp(page, 0, FilteredPageCount - 1);
            Refresh();
        }

        // ---------------- Actions ----------------

        private void OnSelectAll()
        {
            if (_repair == null) return;
            _error.text = string.Empty;
            _repair.SelectAll(_filter);
            RefreshRows();
            RefreshSelection();
        }

        private void OnReset()
        {
            if (_repair == null) return;
            _error.text = string.Empty;
            _repair.Reset();
            RefreshRows();
            RefreshSelection();
        }

        private void OnAccept()
        {
            if (_repair == null) return;
            if (_repair.SelectedCount == 0)
            {
                _error.text = "Select at least one damaged part.";
                return;
            }

            if (!_repair.CanAfford)
            {
                _error.text = "Not enough G to repair the selection.";
                return;
            }

            int repaired = _repair.ApplySelected();
            if (repaired == 0)
            {
                _error.text = "Nothing could be repaired.";
                return;
            }

            Changed?.Invoke();
            Refresh();
            Closed?.Invoke();
        }

        private void OnExit()
        {
            // UIRepair EXIT = skip: the remaining parts are handled by Next (shop buy-back).
            Closed?.Invoke();
        }

        private void ShowHelp()
        {
            if (_helpOverlay == null) return;
            _helpOverlay.style.display = DisplayStyle.Flex;
        }

        private void HideHelp()
        {
            if (_helpOverlay != null) _helpOverlay.style.display = DisplayStyle.None;
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

        /// <summary>Scales the 1920x1080 page to cover the root, like the frozen Stage3D canvas.</summary>
        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            FitPage(evt.newRect.width, evt.newRect.height);
        }

        private void FitPage(float width, float height)
        {
            MenuPageFitter.Fit(
                _page, width, height, PageWidth, PageHeight, cover: true, ref _lastRootSize);
        }
    }
}
