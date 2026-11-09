using System;
using System.Collections.Generic;
using System.Globalization;
using ABRevival.Core.Data;
using ABRevival.Core.Shop;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// UI Toolkit presenter for the frozen UIShop page: the stock table (form1) and the player
    /// parts table (form2) with category filters, pagination, the <c>xN</c> purchase stack and
    /// hold-to-stack auto-repeat, unit selling, net cost and the ACCEPT transaction (ADR 0003).
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class ShopPresenter
    {
        public const int PageSize = 5;
        public const int AllFilter = 4;

        private const long HoldTickMilliseconds = 50;
        private const int HoldDelayTicks = 6;
        private const int HoldAcceleratedTicks = 16;

        private static readonly int[] FilterValues = { AllFilter, 0, 1, 2, 3 };
        private static readonly string[] DetailKeys =
        {
            "type", "name", "hp", "steering", "engine", "fire", "mass", "value"
        };

        private readonly ShopState _shop;
        private readonly Action _onChanged;
        private readonly Dictionary<int, PartDefinitionSO> _parts = new Dictionary<int, PartDefinitionSO>();
        private readonly PartMountTable _mountTable;
        private readonly VisualElement _root;
        private readonly Label _moneyLabel;
        private readonly Label _costLabel;
        private readonly Label _nowLabel;
        private readonly Label _errorLabel;
        private readonly Label _buyPageLabel;
        private readonly Label _sellPageLabel;
        private readonly VisualElement _buyRows;
        private readonly VisualElement _sellRows;
        private readonly Button _acceptButton;
        private readonly Button[] _buyFilters = new Button[FilterValues.Length];
        private readonly Button[] _sellFilters = new Button[FilterValues.Length];
        private readonly Label[] _buyDetail = new Label[DetailKeys.Length];
        private readonly Label[] _sellDetail = new Label[DetailKeys.Length];
        private readonly List<ShopStockEntry> _buyView = new List<ShopStockEntry>();
        private readonly List<int> _sellView = new List<int>();
        private readonly HashSet<int> _sellSelection = new HashSet<int>();
        private readonly IVisualElementScheduledItem _holdSchedule;

        private int _buyFilter = AllFilter;
        private int _sellFilter = AllFilter;
        private int _buyPage;
        private int _sellPage;
        private int _buyDetailPartId = -1;
        private int _sellDetailPartId = -1;
        private int _holdPartId = -1;
        private int _holdTicks;
        private string _error = string.Empty;

        public ShopPresenter(
            VisualElement root,
            ShopState shop,
            Action onChanged,
            IReadOnlyList<PartDefinitionSO> definitions,
            PartMountTable mountTable)
        {
            _shop = shop ?? throw new ArgumentNullException(nameof(shop));
            _onChanged = onChanged;
            _mountTable = mountTable;
            _root = root;

            if (definitions != null)
            {
                for (int i = 0; i < definitions.Count; i++)
                {
                    PartDefinitionSO definition = definitions[i];
                    if (definition != null) _parts[definition.partId] = definition;
                }
            }

            _moneyLabel = root.Q<Label>("money-label");
            _costLabel = root.Q<Label>("cost-label");
            _nowLabel = root.Q<Label>("now-label");
            _errorLabel = root.Q<Label>("shop-error");
            _buyPageLabel = root.Q<Label>("buy-page-label");
            _sellPageLabel = root.Q<Label>("sell-page-label");
            _buyRows = root.Q("buy-rows");
            _sellRows = root.Q("sell-rows");
            _acceptButton = root.Q<Button>("accept-button");

            BindFilters(root, "buy-filter-", _buyFilters, buy: true);
            BindFilters(root, "sell-filter-", _sellFilters, buy: false);

            for (int i = 0; i < DetailKeys.Length; i++)
            {
                _buyDetail[i] = root.Q<Label>("buy-detail-" + DetailKeys[i]);
                _sellDetail[i] = root.Q<Label>("sell-detail-" + DetailKeys[i]);
            }

            BindPage(root, "buy-", GoToBuyPage);
            BindPage(root, "sell-", GoToSellPage);

            if (_acceptButton != null) _acceptButton.clicked += Accept;
            Button reset = root.Q<Button>("reset-button");
            if (reset != null) reset.clicked += Reset;

            // The release must be caught on the root: a stack increment rebuilds the row.
            root.RegisterCallback<PointerUpEvent>(_ => EndHold());
            root.RegisterCallback<PointerLeaveEvent>(_ => EndHold());
            root.RegisterCallback<PointerCaptureOutEvent>(_ => EndHold());
            _holdSchedule = root.schedule.Execute(AdvanceHoldTick).Every(HoldTickMilliseconds);
            _holdSchedule.Pause();
        }

        public int BuyPageIndex => _buyPage;
        public int SellPageIndex => _sellPage;
        public int BuyPageCount => PageCount(_buyView.Count);
        public int SellPageCount => PageCount(_sellView.Count);
        public int BuyFilter => _buyFilter;
        public int SellFilter => _sellFilter;
        public int BuyRowCount => RowsOnPage(_buyView.Count, _buyPage);
        public int SellRowCount => RowsOnPage(_sellView.Count, _sellPage);
        public int SellSelectionCount => _sellSelection.Count;

        /// <summary>Stages one unit of a stock row; body rows toggle 0/1, modules cycle 0..99.</summary>
        public void AddToStack(int partId)
        {
            if (!_shop.Catalog.TryGet(partId, out ShopPart part)) return;

            if (part.IsBody)
            {
                // Frozen FormShop: a hull row toggles 0/1; stacking never exceeds its stock.
                _shop.PurchaseStack.SetQuantity(partId, _shop.PurchaseStack.GetQuantity(partId) > 0 ? 0 : 1);
            }
            else
            {
                _shop.PurchaseStack.Increment(partId);
            }

            ClearError();
            Refresh();
        }

        /// <summary>Starts the frozen 50 ms auto-repeat for a held, infinite-stock row.</summary>
        public void BeginHold(int partId)
        {
            if (!_shop.Catalog.TryGet(partId, out ShopPart part) || part.IsBody)
            {
                EndHold();
                return;
            }

            _holdPartId = partId;
            _holdTicks = 0;
            _holdSchedule?.Resume();
        }

        /// <summary>
        /// One 50 ms hold tick: 300 ms delay, half-rate ticks until 800 ms, then full rate
        /// (UIShop.onHoldTick).
        /// </summary>
        public void AdvanceHoldTick()
        {
            if (_holdPartId < 0) return;

            _holdTicks++;
            if (_holdTicks < HoldDelayTicks ||
                (_holdTicks < HoldAcceleratedTicks && _holdTicks % 2 != 0))
            {
                return;
            }

            AddToStack(_holdPartId);
        }

        public void EndHold()
        {
            _holdPartId = -1;
            _holdTicks = 0;
            _holdSchedule?.Pause();
        }

        /// <summary>Toggles one owned unit of the row between N and Y for the next ACCEPT.</summary>
        public bool ToggleSell(int partId)
        {
            if (_shop.OwnedCount(partId) <= 0) return false;

            if (!_sellSelection.Add(partId)) _sellSelection.Remove(partId);
            ClearError();
            Refresh();
            return true;
        }

        /// <summary>Clears the purchase stack and the staged sell units.</summary>
        public void Reset()
        {
            EndHold();
            _shop.PurchaseStack.Clear();
            _sellSelection.Clear();
            ClearError();
            Refresh();
        }

        /// <summary>
        /// Applies the deal atomically from the player's point of view: sells are credited first
        /// so the net price matches the frozen <c>Cost/Now</c> display, then the buy stack checks out.
        /// </summary>
        public void Accept()
        {
            EndHold();

            int buyTotal = _shop.PurchaseStack.TotalCost(_shop.Catalog);
            int sellCredit = SellCredit();
            if (buyTotal == 0 && sellCredit == 0) return;

            if (_shop.Money + sellCredit < buyTotal)
            {
                SetError(ShopError.InsufficientFunds);
                return;
            }

            bool changed = false;
            foreach (int partId in _sellSelection)
            {
                ShopResult sell = _shop.Sell(partId);
                if (!sell.Success)
                {
                    SetError(sell.Error);
                    if (changed) _onChanged?.Invoke();
                    return;
                }

                changed = true;
            }

            _sellSelection.Clear();

            if (!_shop.PurchaseStack.IsEmpty)
            {
                ShopResult checkout = _shop.Checkout();
                if (!checkout.Success)
                {
                    SetError(checkout.Error);
                    if (changed) _onChanged?.Invoke();
                    return;
                }

                changed = true;
            }

            ClearError();
            Refresh();
            if (changed) _onChanged?.Invoke();
        }

        public void SetBuyFilter(int filter) => SetFilter(filter, buy: true);

        public void SetSellFilter(int filter) => SetFilter(filter, buy: false);

        public void GoToBuyPage(int page) => GoToPage(page, buy: true);

        public void GoToSellPage(int page) => GoToPage(page, buy: false);

        public void Refresh()
        {
            _sellSelection.RemoveWhere(id => _shop.OwnedCount(id) <= 0);
            BuildBuyView();
            BuildSellView();

            _buyPage = Mathf.Clamp(_buyPage, 0, BuyPageCount - 1);
            _sellPage = Mathf.Clamp(_sellPage, 0, SellPageCount - 1);

            if (_moneyLabel != null) _moneyLabel.text = _shop.Money.ToString("N0", CultureInfo.InvariantCulture) + " G";
            RebuildBuyRows();
            RebuildSellRows();
            RefreshFooter();
            RefreshFilters();
            RefreshDetail();
            RefreshTrade();
            if (_errorLabel != null) _errorLabel.text = _error;
        }

        private void BuildBuyView()
        {
            _buyView.Clear();
            IReadOnlyList<ShopStockEntry> stock = _shop.Stock;
            for (int i = 0; i < stock.Count; i++)
            {
                ShopStockEntry entry = stock[i];
                if (!_shop.Catalog.TryGet(entry.PartId, out ShopPart part)) continue;
                if (_buyFilter != AllFilter && (int)part.Category != _buyFilter) continue;
                _buyView.Add(entry);
            }
        }

        private void BuildSellView()
        {
            _sellView.Clear();
            var seen = new HashSet<int>();
            IReadOnlyList<int> owned = _shop.OwnedParts;
            for (int i = 0; i < owned.Count; i++)
            {
                int partId = owned[i];
                if (!seen.Add(partId)) continue;
                if (!_shop.Catalog.TryGet(partId, out ShopPart part)) continue;
                if (_sellFilter != AllFilter && (int)part.Category != _sellFilter) continue;
                _sellView.Add(partId);
            }
        }

        private void RebuildBuyRows()
        {
            if (_buyRows == null) return;
            _buyRows.Clear();

            int start = _buyPage * PageSize;
            for (int slot = 0; slot < PageSize; slot++)
            {
                int index = start + slot;
                _buyRows.Add(index < _buyView.Count
                    ? CreateBuyRow(slot, _buyView[index])
                    : CreateEmptyRow("buy", slot));
            }
        }

        private void RebuildSellRows()
        {
            if (_sellRows == null) return;
            _sellRows.Clear();

            int start = _sellPage * PageSize;
            for (int slot = 0; slot < PageSize; slot++)
            {
                int index = start + slot;
                _sellRows.Add(index < _sellView.Count
                    ? CreateSellRow(slot, _sellView[index])
                    : CreateEmptyRow("sell", slot));
            }
        }

        private VisualElement CreateBuyRow(int slot, ShopStockEntry entry)
        {
            _shop.Catalog.TryGet(entry.PartId, out ShopPart part);
            int quantity = _shop.PurchaseStack.GetQuantity(entry.PartId);

            var row = CreateRow("buy", slot, quantity > 0);
            row.RegisterCallback<PointerDownEvent>(_ => OnBuyDown(entry.PartId));
            row.RegisterCallback<PointerEnterEvent>(_ => ShowBuyDetail(entry.PartId));

            AddIcon(row, entry.PartId);
            AddCell(row, part.PartId.ToString(CultureInfo.InvariantCulture), "col-index");
            AddCell(row, part.Name, "col-name");
            AddCell(row, TypeLabel(part.Category), "col-type");
            AddCell(row, LrLabel(entry.PartId), "col-lr");
            AddCell(row, Number(entry.PartId, d => d.health, "0"), "col-hp");
            AddCell(row, Number(entry.PartId, d => d.turnFly * Mathf.Rad2Deg, "0.0"), "col-steering");
            AddCell(row, EngineLabel(entry.PartId), "col-engine");
            AddCell(row, Number(entry.PartId, d => d.mass, "0"), "col-mass");
            AddCell(row, FireLabel(entry.PartId), "col-fire");
            AddCell(row, part.Cost.ToString(CultureInfo.InvariantCulture), "col-value");
            AddCell(row, part.IsBody ? "x" + entry.Quantity.ToString(CultureInfo.InvariantCulture) : "INF", "col-stock");
            AddCell(row, quantity <= 0 ? "N" : "x" + quantity.ToString(CultureInfo.InvariantCulture), "col-sel");
            return row;
        }

        private VisualElement CreateSellRow(int slot, int partId)
        {
            _shop.Catalog.TryGet(partId, out ShopPart part);
            bool selected = _sellSelection.Contains(partId);

            var row = CreateRow("sell", slot, selected);
            row.RegisterCallback<PointerDownEvent>(_ => ToggleSell(partId));
            row.RegisterCallback<PointerEnterEvent>(_ => ShowSellDetail(partId));

            AddIcon(row, partId);
            AddCell(row, part.PartId.ToString(CultureInfo.InvariantCulture), "col-index");
            AddCell(row, part.Name, "col-name");
            AddCell(row, TypeLabel(part.Category), "col-type");
            AddCell(row, LrLabel(partId), "col-lr");
            AddCell(row, Number(partId, d => d.health, "0"), "col-hp");
            AddCell(row, Number(partId, d => d.turnFly * Mathf.Rad2Deg, "0.0"), "col-steering");
            AddCell(row, EngineLabel(partId), "col-engine");
            AddCell(row, Number(partId, d => d.mass, "0"), "col-mass");
            AddCell(row, FireLabel(partId), "col-fire");
            AddCell(row, _shop.GetSellValue(partId).ToString(CultureInfo.InvariantCulture), "col-value");
            AddCell(row, "x" + _shop.OwnedCount(partId).ToString(CultureInfo.InvariantCulture), "col-stock");
            AddCell(row, selected ? "Y" : "N", "col-sel");
            return row;
        }

        private static VisualElement CreateRow(string prefix, int slot, bool staged)
        {
            var row = new VisualElement { name = prefix + "-row-" + slot };
            row.AddToClassList("shop-row");
            row.AddToClassList(slot % 2 == 0 ? "shop-row-even" : "shop-row-odd");
            if (staged) row.AddToClassList("shop-row-staged");
            return row;
        }

        private static VisualElement CreateEmptyRow(string prefix, int slot)
        {
            var row = new VisualElement { name = prefix + "-empty-" + slot };
            row.AddToClassList("shop-row");
            row.AddToClassList("shop-row-empty");
            if (slot % 2 == 0) row.AddToClassList("shop-row-even");
            return row;
        }

        private void AddIcon(VisualElement row, int partId)
        {
            var icon = new VisualElement { name = "shop-icon" };
            icon.AddToClassList("shop-icon");
            if (_parts.TryGetValue(partId, out PartDefinitionSO definition) && definition.partSprite != null)
                icon.style.backgroundImage = new StyleBackground(definition.partSprite);
            row.Add(icon);
        }

        private static Label AddCell(VisualElement row, string text, string className)
        {
            var label = new Label(text);
            label.AddToClassList("cell");
            label.AddToClassList(className);
            row.Add(label);
            return label;
        }

        private void OnBuyDown(int partId)
        {
            AddToStack(partId);
            BeginHold(partId);
        }

        private void RefreshFooter()
        {
            if (_buyPageLabel != null) _buyPageLabel.text = (_buyPage + 1) + " / " + BuyPageCount;
            if (_sellPageLabel != null) _sellPageLabel.text = (_sellPage + 1) + " / " + SellPageCount;

            SetPageButtons("buy-", _buyPage, BuyPageCount);
            SetPageButtons("sell-", _sellPage, SellPageCount);
        }

        private void SetPageButtons(string prefix, int page, int pageCount)
        {
            if (_root == null) return;
            SetEnabled(_root, prefix + "first", page > 0);
            SetEnabled(_root, prefix + "prev", page > 0);
            SetEnabled(_root, prefix + "next", page < pageCount - 1);
            SetEnabled(_root, prefix + "last", page < pageCount - 1);
        }

        private static void SetEnabled(VisualElement root, string name, bool enabled)
        {
            Button button = root.Q<Button>(name);
            if (button != null) button.SetEnabled(enabled);
        }

        private void RefreshFilters()
        {
            SetFilterState(_buyFilters, _buyFilter);
            SetFilterState(_sellFilters, _sellFilter);
        }

        private static void SetFilterState(Button[] buttons, int filter)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null)
                    buttons[i].EnableInClassList("filter-active", FilterValues[i] == filter);
            }
        }

        private void RefreshDetail()
        {
            if (_buyDetailPartId < 0 || !ContainsBuyPart(_buyDetailPartId))
                _buyDetailPartId = BuyRowCount > 0 ? _buyView[_buyPage * PageSize].PartId : -1;
            if (_sellDetailPartId < 0 || !ContainsSellPart(_sellDetailPartId))
                _sellDetailPartId = SellRowCount > 0 ? _sellView[_sellPage * PageSize] : -1;

            FillDetail(_buyDetail, _buyDetailPartId, sell: false);
            FillDetail(_sellDetail, _sellDetailPartId, sell: true);
        }

        private bool ContainsBuyPart(int partId)
        {
            for (int i = 0; i < _buyView.Count; i++)
            {
                if (_buyView[i].PartId == partId) return true;
            }

            return false;
        }

        private bool ContainsSellPart(int partId)
        {
            for (int i = 0; i < _sellView.Count; i++)
            {
                if (_sellView[i] == partId) return true;
            }

            return false;
        }

        private void ShowBuyDetail(int partId)
        {
            _buyDetailPartId = partId;
            FillDetail(_buyDetail, partId, sell: false);
        }

        private void ShowSellDetail(int partId)
        {
            _sellDetailPartId = partId;
            FillDetail(_sellDetail, partId, sell: true);
        }

        private void FillDetail(Label[] labels, int partId, bool sell)
        {
            if (partId < 0 || !_shop.Catalog.TryGet(partId, out ShopPart part))
            {
                for (int i = 0; i < labels.Length; i++)
                {
                    if (labels[i] != null) labels[i].text = "-";
                }

                return;
            }

            SetDetail(labels, 0, partId, d => TypeLabel(part.Category));
            SetDetail(labels, 1, partId, d => part.Name);
            SetDetail(labels, 2, partId, d => d.health.ToString("0", CultureInfo.InvariantCulture));
            SetDetail(labels, 3, partId, d => (d.turnFly * Mathf.Rad2Deg).ToString("0.0", CultureInfo.InvariantCulture));
            SetDetail(labels, 4, partId, d => EngineLabel(partId));
            SetDetail(labels, 5, partId, d => FireLabel(partId));
            SetDetail(labels, 6, partId, d => d.mass.ToString("0", CultureInfo.InvariantCulture));
            SetDetail(labels, 7, partId,
                d => (sell ? _shop.GetSellValue(partId) : part.Cost).ToString(CultureInfo.InvariantCulture));
        }

        private void SetDetail(Label[] labels, int index, int partId, Func<PartDefinitionSO, string> format)
        {
            if (labels[index] == null) return;
            labels[index].text = _parts.TryGetValue(partId, out PartDefinitionSO definition)
                ? format(definition)
                : "-";
        }

        private void RefreshTrade()
        {
            int buyTotal = _shop.PurchaseStack.TotalCost(_shop.Catalog);
            int sellCredit = SellCredit();
            int delta = sellCredit - buyTotal;

            if (_costLabel != null)
                _costLabel.text = "Cost= " + buyTotal.ToString(CultureInfo.InvariantCulture) + " G";
            if (_nowLabel != null)
            {
                string sign = delta > 0 ? "+" : string.Empty;
                _nowLabel.text = "Now= " + sign + delta.ToString(CultureInfo.InvariantCulture) + " G";
                _nowLabel.EnableInClassList("trade-negative", _shop.Money + delta < 0);
            }

            if (_acceptButton != null) _acceptButton.SetEnabled(delta != 0);
        }

        private int SellCredit()
        {
            int total = 0;
            foreach (int partId in _sellSelection) total += _shop.GetSellValue(partId);
            return total;
        }

        private void SetFilter(int filter, bool buy)
        {
            if (buy)
            {
                if (_buyFilter == filter) return;
                _buyFilter = filter;
                _buyPage = 0;
            }
            else
            {
                if (_sellFilter == filter) return;
                _sellFilter = filter;
                _sellPage = 0;
            }

            EndHold();
            ClearError();
            Refresh();
        }

        private void GoToPage(int page, bool buy)
        {
            if (buy)
            {
                int clamped = Mathf.Clamp(page, 0, BuyPageCount - 1);
                if (clamped == _buyPage) return;
                _buyPage = clamped;
            }
            else
            {
                int clamped = Mathf.Clamp(page, 0, SellPageCount - 1);
                if (clamped == _sellPage) return;
                _sellPage = clamped;
            }

            EndHold();
            Refresh();
        }

        private void BindFilters(VisualElement root, string prefix, Button[] buttons, bool buy)
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                string name = prefix + FilterSuffix(i);
                buttons[i] = root.Q<Button>(name);
                if (buttons[i] == null) continue;

                int filter = FilterValues[i];
                if (buy) buttons[i].clicked += () => SetBuyFilter(filter);
                else buttons[i].clicked += () => SetSellFilter(filter);
            }
        }

        private static string FilterSuffix(int index)
        {
            switch (index)
            {
                case 0: return "all";
                case 1: return "body";
                case 2: return "wing";
                case 3: return "engine";
                default: return "gun";
            }
        }

        private void BindPage(VisualElement root, string prefix, Action<int> goTo)
        {
            Button first = root.Q<Button>(prefix + "first");
            Button previous = root.Q<Button>(prefix + "prev");
            Button next = root.Q<Button>(prefix + "next");
            Button last = root.Q<Button>(prefix + "last");

            if (first != null) first.clicked += () => goTo(0);
            if (previous != null) previous.clicked += () => goTo((prefix == "buy-" ? _buyPage : _sellPage) - 1);
            if (next != null) next.clicked += () => goTo((prefix == "buy-" ? _buyPage : _sellPage) + 1);
            if (last != null) last.clicked += () => goTo(prefix == "buy-" ? BuyPageCount - 1 : SellPageCount - 1);
        }

        private static int PageCount(int itemCount)
        {
            return Mathf.Max(1, (itemCount + PageSize - 1) / PageSize);
        }

        private static int RowsOnPage(int itemCount, int page)
        {
            int remaining = itemCount - page * PageSize;
            return Mathf.Clamp(remaining, 0, PageSize);
        }

        private void SetError(ShopError error)
        {
            _error = error.ToString();
            Refresh();
        }

        private void ClearError()
        {
            _error = string.Empty;
        }

        private string LrLabel(int partId)
        {
            if (!_parts.TryGetValue(partId, out PartDefinitionSO definition)) return "-";
            if (definition.lr == 0) return "L";
            if (definition.lr == 1) return "R";
            return "LR";
        }

        private string EngineLabel(int partId)
        {
            if (!_parts.TryGetValue(partId, out PartDefinitionSO definition)) return "-";
            return definition.category == PartCategory.Gun
                ? ((int)definition.CalculateEffectiveRange()).ToString(CultureInfo.InvariantCulture)
                : definition.thrust.ToString("0.0", CultureInfo.InvariantCulture);
        }

        private string FireLabel(int partId)
        {
            if (!_parts.TryGetValue(partId, out PartDefinitionSO definition)) return "-";
            return definition.CalculateDisplayFirePower(FirePointCount(partId))
                .ToString("0.00", CultureInfo.InvariantCulture);
        }

        private string Number(int partId, Func<PartDefinitionSO, float> value, string format)
        {
            return _parts.TryGetValue(partId, out PartDefinitionSO definition)
                ? value(definition).ToString(format, CultureInfo.InvariantCulture)
                : "-";
        }

        private int FirePointCount(int partId)
        {
            return _mountTable != null && _mountTable.TryGetPart(partId, out PartMountEntry entry) &&
                   _mountTable.TryGetSymbol(entry.mcName, out PartMountSymbol symbol)
                ? symbol.firePoints.Length
                : 0;
        }

        private static string TypeLabel(PartCategory category)
        {
            switch (category)
            {
                case PartCategory.Body: return "Body";
                case PartCategory.Wing: return "Wing";
                case PartCategory.Engine: return "Eng.";
                case PartCategory.Gun: return "Gun";
                default: return "Other";
            }
        }
    }
}
