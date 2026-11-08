using System;
using System.Collections.Generic;
using ABRevival.Core.Shop;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// UI Toolkit presenter for the shop panel: stock rows, purchase stack
    /// controls, checkout and unit selling (ADR 0003).
    /// </summary>
    public sealed class ShopPresenter
    {
        private readonly ShopState _shop;
        private readonly Action _onChanged;

        private readonly Label _moneyLabel;
        private readonly Label _stackLabel;
        private readonly Label _errorLabel;
        private readonly ScrollView _stockList;
        private readonly ScrollView _ownedList;
        private readonly Button _checkoutButton;
        private readonly Button _clearStackButton;

        public ShopPresenter(VisualElement root, ShopState shop, Action onChanged)
        {
            _shop = shop;
            _onChanged = onChanged;

            _moneyLabel = root.Q<Label>("money-label");
            _stackLabel = root.Q<Label>("stack-total-label");
            _errorLabel = root.Q<Label>("shop-error");
            _stockList = root.Q<ScrollView>("shop-stock-list");
            _ownedList = root.Q<ScrollView>("owned-sell-list");
            _checkoutButton = root.Q<Button>("checkout-button");
            _clearStackButton = root.Q<Button>("clear-stack-button");

            if (_checkoutButton != null) _checkoutButton.clicked += Checkout;
            if (_clearStackButton != null) _clearStackButton.clicked += ClearStack;
        }

        public void Refresh()
        {
            _moneyLabel.text = $"{_shop.Money} G";
            _stackLabel.text = $"Stack total: {_shop.PurchaseStack.TotalCost(_shop.Catalog)} G";

            BuildStockList();
            BuildOwnedList();
        }

        private void BuildStockList()
        {
            _stockList.Clear();

            IReadOnlyList<ShopStockEntry> stock = _shop.Stock;
            for (int i = 0; i < stock.Count; i++)
            {
                ShopStockEntry entry = stock[i];
                if (!_shop.Catalog.TryGet(entry.PartId, out ShopPart part)) continue;

                var row = new VisualElement();
                row.AddToClassList("row");

                string availability = part.IsBody ? $"x{entry.Quantity}" : "infinite";
                var label = new Label($"{part.Name}  {part.Cost} G  [{availability}]");
                label.AddToClassList("row-name");
                row.Add(label);

                int quantity = _shop.PurchaseStack.GetQuantity(part.PartId);
                row.Add(new Label(quantity <= 0 ? "N" : $"x{quantity}") { name = "stack-quantity" });

                int capturedId = part.PartId;
                var add = new Button(() => AddToStack(capturedId)) { text = "+" };
                add.AddToClassList("row-action");
                row.Add(add);

                _stockList.Add(row);
            }

            if (stock.Count == 0) _stockList.Add(new Label("Shop stock is empty."));
        }

        private void BuildOwnedList()
        {
            _ownedList.Clear();

            IReadOnlyList<int> owned = _shop.OwnedParts;
            if (owned.Count == 0)
            {
                _ownedList.Add(new Label("No parts owned."));
                return;
            }

            var seen = new Dictionary<int, int>();
            for (int i = 0; i < owned.Count; i++)
            {
                int partId = owned[i];
                if (seen.ContainsKey(partId))
                {
                    seen[partId]++;
                }
                else
                {
                    seen[partId] = 1;
                }
            }

            foreach (KeyValuePair<int, int> pair in seen)
            {
                if (!_shop.Catalog.TryGet(pair.Key, out ShopPart part)) continue;

                var row = new VisualElement();
                row.AddToClassList("row");
                row.Add(new Label($"{part.Name}  x{pair.Value}") { name = "owned-sell-name" });

                int capturedId = pair.Key;
                var sell = new Button(() => Sell(capturedId)) { text = $"Sell {_shop.GetSellValue(capturedId)} G" };
                sell.AddToClassList("row-action");
                row.Add(sell);

                _ownedList.Add(row);
            }
        }

        private void AddToStack(int partId)
        {
            _shop.PurchaseStack.Increment(partId);
            ClearError();
            Refresh();
        }

        private void Checkout()
        {
            ShopResult result = _shop.Checkout();
            if (!result.Success)
            {
                SetError(result.Error.ToString());
                return;
            }

            ClearError();
            _onChanged?.Invoke();
        }

        private void ClearStack()
        {
            _shop.PurchaseStack.Clear();
            ClearError();
            Refresh();
        }

        private void Sell(int partId)
        {
            ShopResult result = _shop.Sell(partId);
            if (!result.Success)
            {
                SetError(result.Error.ToString());
                return;
            }

            ClearError();
            _onChanged?.Invoke();
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
