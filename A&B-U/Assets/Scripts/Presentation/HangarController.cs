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
            _assemblyPresenter = new AssemblyPresenter(
                root, _assembly, _runtimeBlueprint, _definitions, _shop, OnDomainChanged);
            _shopPresenter = new ShopPresenter(root, _shop, OnDomainChanged);

            Button launchButton = root.Q<Button>("launch-button");
            if (launchButton != null) launchButton.clicked += OnLaunchClicked;

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

        private void OnLaunchClicked()
        {
            LaunchRequested?.Invoke();
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
                // shortcut: built-in demo catalog until PartCatalogImporter populates
                // priced PartDefinitionSO assets; replace the scene catalog then.
                Debug.LogWarning("[Hangar] No PartDefinitionSO catalog assigned; using demo catalog.");
                definitions.AddRange(CreateDemoCatalog());
            }

            return definitions;
        }

        private CraftBlueprintSO CreateDefaultBlueprint()
        {
            var source = ScriptableObject.CreateInstance<CraftBlueprintSO>();
            source.craftName = "Player Craft";
            source.hullName = "B.Small.01";
            source.hullCategory = CraftBlueprintSO.GetHullCategoryForName(source.hullName);
            source.hullMass = 100f;
            source.hullHealth = 1300f;

            // The boot scene ships without authored catalog/blueprint assets, so the
            // fallback craft must be armed or WeaponSystem never mounts a fire port.
            source.mountedParts = new[]
            {
                new MountedPartData
                {
                    partId = 0,
                    category = PartCategory.Body,
                    health = 1300f,
                    maxHealth = 1300f,
                    mass = 100f
                },
                new MountedPartData
                {
                    partId = 71,
                    category = PartCategory.Gun,
                    health = 425f,
                    maxHealth = 425f,
                    mass = 17f,
                    localOffset = new Unity.Mathematics.float2(0f, -60f)
                }
            };
            return source;
        }

        private static PartDefinitionSO[] CreateDemoCatalog()
        {
            PartDefinitionSO gun = CreateDemoPart(71, "G.25.X1.01", PartCategory.Gun, 120, 425f, 17f, 0f);
            // Frozen F4DataFire / F4DataBullet row for G.25.X1.01 (fire 1, bullet 1).
            gun.projectileVelocity = 10f;
            gun.bulletLifetime = 40f;
            gun.bulletDamage = 13f;
            gun.fireNum = 1;
            gun.fireFrame = 8;
            gun.fireStop = 17;
            gun.fireEx = 4;
            gun.spreadDegrees = 14f;

            return new[]
            {
                CreateDemoPart(0, "B.Small.01", PartCategory.Body, 500, 1300f, 100f, 5f),
                gun,
                CreateDemoPart(133, "E.Low.01", PartCategory.Engine, 150, 600f, 20f, 16f),
                CreateDemoPart(173, "W.Cls.F.01.L", PartCategory.Wing, 80, 1300f, 20f, 0f),
                CreateDemoPart(222, "W.Cls.F.01.R", PartCategory.Wing, 80, 1300f, 20f, 0f)
            };
        }

        private static PartDefinitionSO CreateDemoPart(
            int partId,
            string name,
            PartCategory category,
            int cost,
            float health,
            float mass,
            float thrust)
        {
            var part = ScriptableObject.CreateInstance<PartDefinitionSO>();
            part.partId = partId;
            part.partName = name;
            part.category = category;
            part.cost = cost;
            part.health = health;
            part.mass = mass;
            part.thrust = thrust;
            return part;
        }

    }
}
