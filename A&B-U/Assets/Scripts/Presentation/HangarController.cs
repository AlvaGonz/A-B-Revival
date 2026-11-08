using System;
using System.Collections.Generic;
using System.IO;
using ABRevival.Core.Data;
using ABRevival.Core.Persistence;
using ABRevival.Core.Shop;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Scene entry point for the hangar prototype. Hosts the single player save
    /// payload and wires the assembly and shop presenters to one domain state.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class HangarController : MonoBehaviour
    {
        [SerializeField] private PartDefinitionSO[] catalogParts = Array.Empty<PartDefinitionSO>();
        [SerializeField] private CraftBlueprintSO blueprint;
        [SerializeField] private int startingMoney = 2000;
        [SerializeField] private string saveFileName = "hangar-save.json";

        private UIDocument _document;
        private ShopCatalog _catalog;
        private ShopState _shop;
        private CraftAssembly _assembly;
        private CraftBlueprintSO _runtimeBlueprint;
        private List<PartDefinitionSO> _definitions;
        private AssemblyPresenter _assemblyPresenter;
        private ShopPresenter _shopPresenter;
        private Label _messageLabel;
        private readonly PlayerSaveData _payload = new PlayerSaveData();

        private void Start()
        {
            _document = GetComponent<UIDocument>();
            if (_document == null || _document.rootVisualElement == null)
            {
                Debug.LogError("[Hangar] UIDocument with a source UXML is required.");
                return;
            }

            _definitions = ResolveDefinitions();
            _catalog = BuildCatalog(_definitions);
            _shop = new ShopState(_catalog, startingMoney);
            for (int i = 0; i < _definitions.Count; i++)
            {
                PartDefinitionSO definition = _definitions[i];
                _shop.Unlock(definition.partId, definition.category == PartCategory.Body ? 1 : 0);
            }

            _runtimeBlueprint = blueprint != null ? Instantiate(blueprint) : CreateDefaultBlueprint();
            _assembly = BuildAssembly(_runtimeBlueprint);

            VisualElement root = _document.rootVisualElement;
            _messageLabel = root.Q<Label>("message-label");
            _assemblyPresenter = new AssemblyPresenter(
                root, _assembly, _runtimeBlueprint, _definitions, _shop, OnDomainChanged);
            _shopPresenter = new ShopPresenter(root, _shop, OnDomainChanged);

            Button saveButton = root.Q<Button>("save-button");
            if (saveButton != null) saveButton.clicked += Save;
            Button loadButton = root.Q<Button>("load-button");
            if (loadButton != null) loadButton.clicked += Load;

            OnDomainChanged();
        }

        private void OnDestroy()
        {
            _assembly?.Dispose();
        }

        private void OnDomainChanged()
        {
            _assemblyPresenter?.Refresh();
            _shopPresenter?.Refresh();
        }

        private void Save()
        {
            try
            {
                _payload.shop = _shop.CreateSave();
                _payload.blueprints.Clear();
                _payload.blueprints.Add(BlueprintSaveMapper.Capture(_runtimeBlueprint));
                PlayerSaveCodec.SaveToFile(SavePath, _payload);
                SetMessage($"Saved to {SavePath}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Hangar] Save failed: {exception}");
                SetMessage("Save failed.");
            }
        }

        private void Load()
        {
            if (!File.Exists(SavePath))
            {
                SetMessage("No save file yet.");
                return;
            }

            try
            {
                PlayerSaveData payload = PlayerSaveCodec.LoadFromFile(SavePath);
                _payload.storyIndex = payload.storyIndex;
                _shop.LoadFrom(payload.shop);

                if (payload.blueprints.Count > 0)
                {
                    BlueprintSaveMapper.Apply(payload.blueprints[0], _runtimeBlueprint);
                }

                _assembly.Dispose();
                _assembly = BuildAssembly(_runtimeBlueprint);
                _assemblyPresenter.Rebind(_assembly, _runtimeBlueprint);
                OnDomainChanged();
                SetMessage($"Loaded {SavePath}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Hangar] Load failed: {exception}");
                SetMessage("Load failed.");
            }
        }

        private string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);

        private CraftAssembly BuildAssembly(CraftBlueprintSO source)
        {
            var assembly = new CraftAssembly(
                source.hullName,
                source.hullMass,
                source.hullHealth,
                source.isGrandfathered);

            if (source.mountedParts != null)
            {
                for (int i = 0; i < source.mountedParts.Length; i++)
                {
                    assembly.TryMountPart(source.mountedParts[i], out _);
                }
            }

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
            source.mountedParts = Array.Empty<MountedPartData>();
            return source;
        }

        private static PartDefinitionSO[] CreateDemoCatalog()
        {
            return new[]
            {
                CreateDemoPart(0, "B.Small.01", PartCategory.Body, 500, 1300f, 100f, 5f),
                CreateDemoPart(71, "G.25.X1.01", PartCategory.Gun, 120, 425f, 17f, 0f),
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

        private void SetMessage(string message)
        {
            if (_messageLabel != null) _messageLabel.text = message;
        }
    }
}
