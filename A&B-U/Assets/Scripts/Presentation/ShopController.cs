using System;
using System.Collections.Generic;
using ABRevival.Core.Data;
using ABRevival.Core.Persistence;
using ABRevival.Core.Shop;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Scene entry point for Shop (the frozen UIShop page): stock and player-parts tables with
    /// category filters, the <c>xN</c> purchase stack and hold-to-stack, buy/sell checkout,
    /// money and Back to Center. It mutates the single payload owned by
    /// <see cref="GameFlowController"/>; Center reloads the shop slice on return.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [RequireComponent(typeof(ScreenPanel))]
    public sealed class ShopController : MonoBehaviour
    {
        [SerializeField] private PartDefinitionSO[] catalogParts = Array.Empty<PartDefinitionSO>();

        [Header("Shop sprites (frozen 2011 UI library)")]
        // Only the NEW badge is drawn at runtime; the page, panels and button art live in Shop.uxml.
        [SerializeField] private Sprite newBadge;

        private ScreenPanel _screen;
        private ShopCatalog _catalog;
        private ShopState _shop;
        private PartMountTable _mountTable;
        private ShopPresenter _presenter;
        private List<PartDefinitionSO> _definitions;
        private PlayerSaveData _payload = new PlayerSaveData();
        private VisualElement _help;
        private bool _initialized;

        /// <summary>Raised after a shop action mutates the save payload.</summary>
        public event Action Changed;

        /// <summary>Raised when the player asks to return to Center.</summary>
        public event Action Closed;

        public ShopState Shop => _shop;
        public ShopPresenter Presenter => _presenter;
        public int Money => _shop != null ? _shop.Money : 0;

        private void Awake()
        {
            // GameFlowController owns visibility and opens the screen from Center.
            _screen = GetComponent<ScreenPanel>();
            if (_screen != null) _screen.SetVisible(false);
        }

        /// <summary>
        /// Builds the Shop UI once and applies the shared player save. GameFlowController calls
        /// this when Center's SHOP button raises the screen change.
        /// </summary>
        public void Initialize(PlayerSaveData save)
        {
            _screen = GetComponent<ScreenPanel>();
            if (_screen == null)
            {
                Debug.LogError("[Shop] ScreenPanel with a PanelRenderer is required.");
                return;
            }

            _screen.WhenReady(root => Open(save, root));
        }

        private void Open(PlayerSaveData save, VisualElement root)
        {
            if (!_initialized)
            {
                _definitions = PartCatalogBootstrap.Collect(catalogParts, "Shop");
                _catalog = PartCatalogBootstrap.BuildCatalog(_definitions);
                _shop = new ShopState(_catalog);
                _mountTable = PartCatalogBootstrap.LoadMountTable("Shop");

                Bind(root);

                _presenter = new ShopPresenter(
                    root, _shop, OnShopChanged, _definitions, _mountTable, newBadge);

                _initialized = true;
            }

            ApplySave(save);
            CloseHelp();
            SetVisible(true);
        }

        /// <summary>Captures money, stock and owned parts; blueprints stay Center's.</summary>
        public void CaptureSave(PlayerSaveData target)
        {
            if (target == null || !_initialized) return;
            target.shop = _shop.CreateSave();
        }

        /// <summary>Restores the shop slice, then reconciles it against the gift table (ADR 0003).</summary>
        public void ApplySave(PlayerSaveData payload)
        {
            if (!_initialized || payload == null) return;

            _payload = payload;
            _shop.LoadFrom(payload.shop);
            ReconcileShop(payload);
            _presenter.Refresh();
        }

        /// <summary>Shows or hides the whole Shop overlay.</summary>
        public void SetVisible(bool visible)
        {
            if (_screen == null) _screen = GetComponent<ScreenPanel>();
            _screen?.SetVisible(visible);
            if (visible) CloseHelp();
        }

        /// <summary>Opens or closes the frozen UI_HelpShop page.</summary>
        public void ToggleHelp()
        {
            if (_help == null) return;
            _help.style.display = _help.resolvedStyle.display == DisplayStyle.Flex
                ? DisplayStyle.None
                : DisplayStyle.Flex;
        }

        public void CloseHelp()
        {
            if (_help != null) _help.style.display = DisplayStyle.None;
        }

        private void Bind(VisualElement root)
        {
            Button center = root.Q<Button>("center-button");
            if (center != null) center.clicked += () => Closed?.Invoke();

            Button help = root.Q<Button>("help-button");
            if (help != null) help.clicked += ToggleHelp;

            Button helpClose = root.Q<Button>("shop-help-close");
            if (helpClose != null) helpClose.clicked += CloseHelp;

            _help = root.Q("shop-help");

            Label player = root.Q<Label>("player-label");
            if (player != null) player.text = "Michael";
        }

        private void OnShopChanged()
        {
            Changed?.Invoke();
        }

        private void ReconcileShop(PlayerSaveData payload)
        {
            var hullNames = new List<string>();
            if (payload.blueprints != null)
            {
                for (int i = 0; i < payload.blueprints.Count; i++)
                {
                    string hullName = payload.blueprints[i].hullName;
                    if (!string.IsNullOrEmpty(hullName)) hullNames.Add(hullName);
                }
            }

            _shop.ReconcileStock(BuildDefaultGifts(_definitions), payload.storyIndex, hullNames);
        }

        private static int[][] BuildDefaultGifts(IReadOnlyList<PartDefinitionSO> definitions)
        {
            var partIds = new int[definitions.Count];
            for (int i = 0; i < definitions.Count; i++) partIds[i] = definitions[i].partId;
            return new[] { partIds };
        }
    }
}
