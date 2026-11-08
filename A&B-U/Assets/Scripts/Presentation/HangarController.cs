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
    /// Scene entry point for the hangar prototype. Mutates the single player save
    /// payload owned by <see cref="GameFlowController"/> and wires the assembly and
    /// shop presenters to one domain state. The hangar never owns a save file.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HangarController : MonoBehaviour
    {
        [SerializeField] private PartDefinitionSO[] catalogParts = Array.Empty<PartDefinitionSO>();
        [SerializeField] private CraftBlueprintSO blueprint;
        [SerializeField] private int startingMoney = 2000;

        [Header("Original UI sprites (frozen 2011 UI library)")]
        [SerializeField] private Sprite hangarBackground;
        [SerializeField] private Sprite hangarHeader;
        [SerializeField] private Sprite hangarFrame;
        [SerializeField] private Sprite hangarPanel;
        [SerializeField] private Sprite hangarTableHeader;
        [SerializeField] private Sprite hangarStatsCard;
        [SerializeField] private Sprite buttonMerge;
        [SerializeField] private Sprite buttonNew;
        [SerializeField] private Sprite buttonBackToCenter;
        [SerializeField] private Sprite buttonPartsCrafts;
        [SerializeField] private Sprite buttonLaunch;
        [SerializeField] private Sprite buttonAccept;
        [SerializeField] private Sprite buttonReset;
        [SerializeField] private Sprite buttonAdd;
        [SerializeField] private Sprite categoryAll;
        [SerializeField] private Sprite categoryBody;
        [SerializeField] private Sprite categoryWing;
        [SerializeField] private Sprite categoryEngine;
        [SerializeField] private Sprite categoryGun;
        [SerializeField] private Sprite thumbnailBody;
        [SerializeField] private Sprite thumbnailWing;
        [SerializeField] private Sprite thumbnailEngine;
        [SerializeField] private Sprite thumbnailGun;
        [SerializeField] private Sprite thumbnailModule;

        private UIDocument _document;
        private ShopCatalog _catalog;
        private ShopState _shop;
        private CraftAssembly _assembly;
        private CraftBlueprintSO _runtimeBlueprint;
        private List<PartDefinitionSO> _definitions;
        private int[][] _defaultGifts;
        private AssemblyPresenter _assemblyPresenter;
        private ShopPresenter _shopPresenter;
        private PlayerSaveData _payload = new PlayerSaveData();
        private bool _initialized;

        /// <summary>Current shop money, exposed for boot/save verification.</summary>
        public int Money => _shop != null ? _shop.Money : 0;

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

        /// <summary>Raised when the player asks to launch the next story mission.</summary>
        public event Action LaunchRequested;

        private void Start()
        {
            Initialize(null);
        }

        /// <summary>
        /// Builds the hangar UI once and applies a player save. GameFlowController calls this at
        /// boot; calling it again with a save refreshes the shop, blueprint and story label.
        /// </summary>
        public void Initialize(PlayerSaveData save)
        {
            if (_initialized)
            {
                if (save != null) ApplySave(save);
                return;
            }

            _document = GetComponent<UIDocument>();
            if (_document == null || _document.rootVisualElement == null)
            {
                Debug.LogError("[Hangar] UIDocument with a source UXML is required.");
                return;
            }

            _definitions = ResolveDefinitions();
            _catalog = BuildCatalog(_definitions);
            _defaultGifts = BuildDefaultGifts(_definitions);
            _shop = new ShopState(_catalog, startingMoney);

            _runtimeBlueprint = blueprint != null ? Instantiate(blueprint) : CreateDefaultBlueprint();
            _assembly = BuildAssembly(_runtimeBlueprint);

            VisualElement root = _document.rootVisualElement;
            ApplySkin(root);
            _assemblyPresenter = new AssemblyPresenter(
                root, _assembly, _runtimeBlueprint, _definitions, _shop, OnDomainChanged);
            _shopPresenter = new ShopPresenter(
                root, _shop, OnDomainChanged, _definitions,
                buttonAdd,
                thumbnailBody, thumbnailWing, thumbnailEngine, thumbnailGun, thumbnailModule);

            Button launchButton = root.Q<Button>("launch-button");
            if (launchButton != null) launchButton.clicked += RequestLaunch;

            _initialized = true;
            if (save != null)
            {
                ApplySave(save);
            }
            else
            {
                ReconcileShop(_payload.storyIndex);
                OnDomainChanged();
                SetStoryLabel(_payload.storyIndex);
            }
        }

        /// <summary>Captures the current shop and blueprint into the single player payload.</summary>
        public void CaptureSave(PlayerSaveData target)
        {
            if (target == null || !_initialized) return;

            target.shop = _shop.CreateSave();
            target.blueprints.Clear();
            target.blueprints.Add(BlueprintSaveMapper.Capture(_runtimeBlueprint));
        }

        /// <summary>
        /// Restores shop money, stock, owned parts and the mounted blueprint, then
        /// reconciles missing catalog stock rows before the UI is refreshed.
        /// </summary>
        public void ApplySave(PlayerSaveData payload)
        {
            if (!_initialized || payload == null) return;

            _payload = payload;
            _shop.LoadFrom(payload.shop);
            if (payload.blueprints.Count > 0)
            {
                BlueprintSaveMapper.Apply(payload.blueprints[0], _runtimeBlueprint);
            }

            ReconcileShop(payload.storyIndex);

            _assembly.Dispose();
            _assembly = BuildAssembly(_runtimeBlueprint);
            _assemblyPresenter.Rebind(_assembly, _runtimeBlueprint);
            OnDomainChanged();
            SetStoryLabel(payload.storyIndex);
        }

        private void ReconcileShop(int storyIndex)
        {
            _shop.ReconcileStock(_defaultGifts, storyIndex, ActiveHullNames());
        }

        private string[] ActiveHullNames()
        {
            return _runtimeBlueprint != null && !string.IsNullOrEmpty(_runtimeBlueprint.hullName)
                ? new[] { _runtimeBlueprint.hullName }
                : null;
        }

        private static int[][] BuildDefaultGifts(IReadOnlyList<PartDefinitionSO> definitions)
        {
            var partIds = new int[definitions.Count];
            for (int i = 0; i < definitions.Count; i++)
                partIds[i] = definitions[i].partId;
            return new[] { partIds };
        }

        /// <summary>Shows or hides the whole hangar overlay.</summary>
        public void SetVisible(bool visible)
        {
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_document != null && _document.rootVisualElement != null)
            {
                _document.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>Updates the story index shown in the hangar header.</summary>
        public void SetStory(int storyIndex)
        {
            _payload.storyIndex = storyIndex;
            SetStoryLabel(storyIndex);
        }

        /// <summary>Raises the launch intent; the button and PlayMode tests use the same seam.</summary>
        public void RequestLaunch()
        {
            LaunchRequested?.Invoke();
        }

        /// <summary>
        /// Applies the extracted 2011 sprite set to the named controls. Every background is
        /// an explicit project asset wired by BootSceneBuilder; no theme art is relied on.
        /// </summary>
        private void ApplySkin(VisualElement root)
        {
            SetBackground(root, "hangar-screen", hangarBackground);
            SetBackground(root, "hangar-header", hangarHeader);
            SetBackground(root, "hangar-frame", hangarFrame);
            SetBackground(root, "table-area", hangarPanel);
            SetBackground(root, "stats-card", hangarStatsCard);

            VisualElement tableHeader = root.Q("table-header");
            if (tableHeader != null) tableHeader.style.backgroundImage = new StyleBackground(hangarTableHeader);

            SetBackground(root, "launch-button", buttonLaunch);
            SetBackground(root, "merge-button", buttonMerge);
            SetBackground(root, "new-button", buttonNew);
            SetBackground(root, "back-to-center-button", buttonBackToCenter);
            SetBackground(root, "parts-crafts-button", buttonPartsCrafts);
            SetBackground(root, "checkout-button", buttonAccept);
            SetBackground(root, "clear-stack-button", buttonReset);

            SetBackground(root, "tab-all", categoryAll);
            SetBackground(root, "tab-body", categoryBody);
            SetBackground(root, "tab-wing", categoryWing);
            SetBackground(root, "tab-engine", categoryEngine);
            SetBackground(root, "tab-gun", categoryGun);
        }

        private static void SetBackground(VisualElement root, string name, Sprite sprite)
        {
            VisualElement element = root.Q(name);
            if (element != null && sprite != null)
            {
                element.style.backgroundImage = new StyleBackground(sprite);
            }
        }

        private void SetStoryLabel(int storyIndex)
        {
            if (_document == null || _document.rootVisualElement == null) return;
            Label storyLabel = _document.rootVisualElement.Q<Label>("story-label");
            if (storyLabel != null) storyLabel.text = $"STORY {storyIndex}";
        }

        private void OnDestroy()
        {
            _assembly?.Dispose();
        }

        private void OnDomainChanged()
        {
            // The flow owns the file; the hangar only mutates its payload.
            CaptureSave(_payload);
            _assemblyPresenter?.Refresh();
            _shopPresenter?.Refresh();
        }

        private CraftAssembly BuildAssembly(CraftBlueprintSO source)
        {
            var assembly = new CraftAssembly(
                source.hullName,
                source.hullMass,
                source.hullHealth,
                source.isGrandfathered);

            var mounted = new List<MountedPartData>();
            if (source.mountedParts != null)
            {
                for (int i = 0; i < source.mountedParts.Length; i++)
                {
                    MountedPartData part = source.mountedParts[i];
                    AssemblyError error = assembly.TryMountPart(part, out _);
                    if (error == AssemblyError.None)
                    {
                        mounted.Add(part);
                        continue;
                    }

                    Debug.LogWarning($"[Hangar] Blueprint part {part.partId} was not mounted: {error}.");
                }
            }

            // Normalize the blueprint to what the chassis actually accepted.
            source.mountedParts = mounted.ToArray();
            return assembly;
        }

        private static ShopCatalog BuildCatalog(IReadOnlyList<PartDefinitionSO> definitions)
        {
            var parts = new List<ShopPart>(definitions.Count);
            for (int i = 0; i < definitions.Count; i++)
            {
                PartDefinitionSO definition = definitions[i];
                if (definition != null)
                {
                    parts.Add(new ShopPart(definition.partId, definition.partName, definition.category, definition.cost));
                }
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
            {
                Debug.LogError("[Hangar] No PartDefinitionSO catalog assigned; the shop and assembly are empty.");
            }

            return definitions;
        }

        private CraftBlueprintSO CreateDefaultBlueprint()
        {
            var source = ScriptableObject.CreateInstance<CraftBlueprintSO>();
            source.craftName = "Player Craft";

            PartDefinitionSO body = FindPartDefinition(0);
            if (body != null)
            {
                source.hullName = body.partName;
                source.hullCategory = CraftBlueprintSO.GetHullCategoryForName(body.partName);
                source.hullMass = body.mass;
                source.hullHealth = body.health;
            }
            else
            {
                source.hullName = "B.Small.01";
                source.hullCategory = CraftBlueprintSO.GetHullCategoryForName(source.hullName);
                source.hullMass = 100f;
                source.hullHealth = 1300f;
            }

            var mounted = new List<MountedPartData>(5);
            mounted.Add(new MountedPartData
            {
                partId = body != null ? body.partId : 0,
                category = PartCategory.Body,
                health = source.hullHealth,
                maxHealth = source.hullHealth,
                mass = source.hullMass
            });

            AddDefaultPart(mounted, FindPartDefinition(133), 0f, 8f);
            AddDefaultPart(mounted, FindPartDefinition(173), -14f, 0f);
            AddDefaultPart(mounted, FindPartDefinition(174), 14f, 0f);
            AddDefaultPart(mounted, FindPartDefinition(71), 0f, -16f);

            source.mountedParts = mounted.ToArray();
            return source;
        }

        private static void AddDefaultPart(List<MountedPartData> mounted, PartDefinitionSO definition, float x, float y)
        {
            if (definition == null) return;
            mounted.Add(new MountedPartData
            {
                partId = definition.partId,
                category = definition.category,
                health = definition.health,
                maxHealth = definition.health,
                mass = definition.mass,
                thrust = definition.thrust,
                energyDraw = definition.energyDraw,
                localOffset = new Unity.Mathematics.float2(x, y)
            });
        }

    }
}
