using System;
using System.Collections.Generic;
using ABRevival.Core.Data;
using ABRevival.Core.Shop;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// UI Toolkit presenter for the frozen shop form: stock rows with the original part icon,
    /// name, price, stock and <c>xN</c> purchase stack, checkout and unit selling (ADR 0003).
    /// </summary>
    public sealed class ShopPresenter
    {
        private readonly ShopState _shop;
        private readonly Action _onChanged;
        private readonly Dictionary<int, PartDefinitionSO> _parts = new Dictionary<int, PartDefinitionSO>();
        private readonly Sprite _addButton;
        private readonly Sprite[] _categoryThumbnails;

        private readonly Label _moneyLabel;
        private readonly Label _stackLabel;
        private readonly Label _errorLabel;
        private readonly ScrollView _stockList;
        private readonly ScrollView _ownedList;
        private readonly Button _checkoutButton;
        private readonly Button _clearStackButton;

        public ShopPresenter(
            VisualElement root,
            ShopState shop,
            Action onChanged,
            IReadOnlyList<PartDefinitionSO> definitions,
            Sprite addButton,
            Sprite thumbnailBody,
            Sprite thumbnailWing,
            Sprite thumbnailEngine,
            Sprite thumbnailGun,
            Sprite thumbnailModule)
        {
            _shop = shop;
            _onChanged = onChanged;
            _addButton = addButton;
            _categoryThumbnails = new[]
            {
                thumbnailBody, thumbnailWing, thumbnailEngine, thumbnailGun, thumbnailModule
            };

            if (definitions != null)
            {
                for (int i = 0; i < definitions.Count; i++)
                {
                    PartDefinitionSO definition = definitions[i];
                    if (definition != null) _parts[definition.partId] = definition;
                }
            }

            _moneyLabel = root.Q<Label>("money-label");
            _stackLabel = root.Q<Label>("stack-total-label");
            _errorLabel = root.Q<Label>("shop-error");
            _stockList = root.Q<ScrollView>("shop-stock-list");
            _ownedList = root.Q<ScrollView>("owned-sell-list");
            _checkoutButton = root.Q<Button>("checkout-button");
            _clearStackButton = root.Q<Button>("clear-stack-button");

            if (_checkoutButton != null) _checkoutButton.clicked += Checkout;
            if (_clearStackButton != null) _clearStackButton.clicked += ClearStack;
            if (_stockList != null) _stockList.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            if (_ownedList != null) _ownedList.verticalScrollerVisibility = ScrollerVisibility.Hidden;
        }

        public void Refresh()
        {
            _moneyLabel.text = $"{_shop.Money} G";
            _stackLabel.text = $"STACK {_shop.PurchaseStack.TotalCost(_shop.Catalog)} G";

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

                VisualElement row = CreateRow(i);
                row.Add(CreateIcon(part.PartId, part.Category));
                var name = new Label(part.Name) { name = "shop-name", tooltip = part.Name };
                name.AddToClassList("shop-name");
                row.Add(name);
                var price = new Label($"{part.Cost} G") { name = "shop-price" };
                price.AddToClassList("shop-price");
                row.Add(price);
                var stockLabel = new Label(part.IsBody ? $"x{entry.Quantity}" : "INF") { name = "shop-stock" };
                stockLabel.AddToClassList("shop-stock");
                row.Add(stockLabel);

                int quantity = _shop.PurchaseStack.GetQuantity(part.PartId);
                var stack = new Label(quantity <= 0 ? "-" : $"x{quantity}") { name = "shop-stack" };
                stack.AddToClassList("shop-stack");
                row.Add(stack);

                int capturedId = part.PartId;
                var add = new Button(() => AddToStack(capturedId));
                add.AddToClassList("sprite-button");
                add.AddToClassList("shop-add");
                add.tooltip = "Add to stack";
                if (_addButton != null) add.style.backgroundImage = new StyleBackground(_addButton);
                row.Add(add);

                _stockList.Add(row);
            }

            if (stock.Count == 0) _stockList.Add(EmptyRow("NO STOCK"));
        }

        private void BuildOwnedList()
        {
            _ownedList.Clear();

            IReadOnlyList<int> owned = _shop.OwnedParts;
            if (owned.Count == 0)
            {
                _ownedList.Add(EmptyRow("NO PARTS"));
                return;
            }

            var counts = new Dictionary<int, int>();
            var order = new List<int>();
            for (int i = 0; i < owned.Count; i++)
            {
                int partId = owned[i];
                if (counts.TryGetValue(partId, out int count))
                {
                    counts[partId] = count + 1;
                }
                else
                {
                    counts[partId] = 1;
                    order.Add(partId);
                }
            }

            for (int i = 0; i < order.Count; i++)
            {
                int partId = order[i];
                if (!_shop.Catalog.TryGet(partId, out ShopPart part)) continue;

                VisualElement row = CreateRow(i);
                row.Add(CreateIcon(partId, part.Category));
                var name = new Label($"{part.Name} x{counts[partId]}") { name = "sell-name" };
                name.AddToClassList("shop-name");
                row.Add(name);

                int capturedId = partId;
                var sell = new Button(() => Sell(capturedId)) { text = $"SELL {_shop.GetSellValue(partId)} G" };
                sell.AddToClassList("row-action");
                row.Add(sell);
                _ownedList.Add(row);
            }
        }

        private VisualElement CreateRow(int index)
        {
            var row = new VisualElement();
            row.AddToClassList("shop-row");
            row.AddToClassList(index % 2 == 0 ? "shop-row-even" : "shop-row-odd");
            return row;
        }

        private VisualElement CreateIcon(int partId, PartCategory category)
        {
            var icon = new VisualElement { name = "shop-icon" };
            icon.AddToClassList("shop-icon");
            Sprite sprite = null;
            if (_parts.TryGetValue(partId, out PartDefinitionSO definition)) sprite = definition.partSprite;
            if (sprite == null) sprite = ThumbnailFor(category);
            if (sprite != null) icon.style.backgroundImage = new StyleBackground(sprite);
            return icon;
        }

        private Sprite ThumbnailFor(PartCategory category)
        {
            switch (category)
            {
                case PartCategory.Body: return _categoryThumbnails[0];
                case PartCategory.Wing: return _categoryThumbnails[1];
                case PartCategory.Engine: return _categoryThumbnails[2];
                case PartCategory.Gun: return _categoryThumbnails[3];
                default: return _categoryThumbnails[4];
            }
        }

        private static Label EmptyRow(string text)
        {
            var label = new Label(text);
            label.AddToClassList("empty-row");
            return label;
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
