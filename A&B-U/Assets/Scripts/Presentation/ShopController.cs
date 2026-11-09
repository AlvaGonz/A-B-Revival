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
    [RequireComponent(typeof(UIDocument))]
    public sealed class ShopController : MonoBehaviour
    {
        [SerializeField] private PartDefinitionSO[] catalogParts = Array.Empty<PartDefinitionSO>();

        [Header("Shop sprites (frozen 2011 UI library)")]
        [SerializeField] private Sprite shopBackground;
        [SerializeField] private Sprite shopHeader;
        [SerializeField] private Sprite panel;
        [SerializeField] private Sprite buttonAccept;
        [SerializeField] private Sprite buttonReset;
        [SerializeField] private Sprite buttonCenter;
        [SerializeField] private Sprite buttonInvert;
        [SerializeField] private Sprite newBadge;
        [SerializeField] private Sprite helpOverlay;
        [SerializeField] private Sprite helpButton;
        [SerializeField] private Sprite categoryAll;
        [SerializeField] private Sprite categoryBody;
        [SerializeField] private Sprite categoryWing;
        [SerializeField] private Sprite categoryEngine;
        [SerializeField] private Sprite categoryGun;

        private UIDocument _document;
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
            var document = GetComponent<UIDocument>();
            if (document != null && document.rootVisualElement != null)
                document.rootVisualElement.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Builds the Shop UI once and applies the shared player save. GameFlowController calls
        /// this when Center's SHOP button raises the screen change.
        /// </summary>
        public void Initialize(PlayerSaveData save)
        {
            if (!_initialized)
            {
                _document = GetComponent<UIDocument>();
                if (_document == null || _document.rootVisualElement == null)
                {
                    Debug.LogError("[Shop] UIDocument with a source UXML is required.");
                    return;
                }

                _definitions = ResolveDefinitions();
                _catalog = BuildCatalog(_definitions);
                _shop = new ShopState(_catalog);
                _mountTable = LoadMountTable();

                VisualElement root = _document.rootVisualElement;
                ApplySkin(root);
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
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_document != null && _document.rootVisualElement != null)
                _document.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
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

        private void ApplySkin(VisualElement root)
        {
            SetPanelBackground(root, "shop-root", shopBackground);
            SetPanelBackground(root, "shop-title-plate", shopHeader);
            SetPanelBackground(root, "buy-panel", panel);
            SetPanelBackground(root, "sell-panel", panel);
            SetPanelBackground(root, "buy-detail", panel);
            SetPanelBackground(root, "sell-detail", panel);

            SetSpriteBackground(root, "accept-button", buttonAccept, ScaleMode.ScaleToFit);
            SetSpriteBackground(root, "reset-button", buttonReset, ScaleMode.ScaleToFit);
            SetSpriteBackground(root, "center-button", buttonCenter, ScaleMode.ScaleToFit);
            SetSpriteBackground(root, "invert-button", buttonInvert, ScaleMode.ScaleToFit);
            SetSpriteBackground(root, "help-button", helpButton, ScaleMode.ScaleToFit);
            SetPanelBackground(root, "shop-help-page", helpOverlay);

            Sprite[] categories = { categoryAll, categoryBody, categoryWing, categoryEngine, categoryGun };
            string[] tables = { "buy", "sell" };
            string[] suffixes = { "all", "body", "wing", "engine", "gun" };
            for (int table = 0; table < tables.Length; table++)
            {
                for (int i = 0; i < suffixes.Length; i++)
                    SetSpriteBackground(root, tables[table] + "-filter-" + suffixes[i], categories[i], ScaleMode.ScaleToFit);
            }
        }

        private static void SetPanelBackground(VisualElement root, string name, Sprite sprite)
        {
            VisualElement element = root.Q(name);
            if (element != null && sprite != null)
                element.style.backgroundImage = new StyleBackground(sprite);
        }

        private static void SetSpriteBackground(VisualElement root, string name, Sprite sprite, ScaleMode scaleMode)
        {
            VisualElement element = root.Q(name);
            if (element != null && sprite != null)
                UiSpriteSkin.Apply(element, sprite, scaleMode);
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
                Debug.LogError("[Shop] No PartDefinitionSO catalog assigned; the shop is empty.");

            return definitions;
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
                Debug.LogWarning("[Shop] Part mount table unavailable: " + exception.Message);
                return null;
            }
        }
    }
}
