using System;
using ABRevival.Core.Data;
using ABRevival.Core.Persistence;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Scene entry point for the Ready / Briefing handoff (the frozen UIMapBuild page):
    /// validates the Center loadout, prints the campaign objective summary, and hands the
    /// selected Player craft plus AI companions to <see cref="GameFlowController"/> before
    /// talkBefore and the opening cinematic start. Back cancels to Center without launching.
    /// It never writes the save; <see cref="GameFlowController"/> owns the payload.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ReadyController : MonoBehaviour
    {
        private const int CompanionSlotCount = CompanionRoster.SlotCount;

        [Header("Ready sprites (frozen 2011 UI library)")]
        [SerializeField] private Sprite readyBackground;
        [SerializeField] private Sprite titlePlate;
        [SerializeField] private Sprite launchButton;
        [SerializeField] private Sprite backButton;
        [SerializeField] private Sprite loadingMark;

        private UIDocument _document;
        private PlayerSaveData _payload = new PlayerSaveData();
        private CenterController _center;
        private readonly Label[] _companionLabels = new Label[CompanionSlotCount];

        private Label _title;
        private Label _playerLabel;
        private Label _statusLabel;
        private Label _briefingWords;
        private Button _launch;

        private bool _initialized;

        /// <summary>Raised when Back cancels the handoff and returns to Center.</summary>
        public event Action Closed;

        /// <summary>Raised when the validated loadout should launch the mission.</summary>
        public event Action LaunchRequested;

        public bool CanLaunch { get; private set; }

        public MissionLoadout Loadout { get; private set; } = new MissionLoadout(-1, null);

        public string CompanionLabel(int slot)
        {
            return slot >= 0 && slot < CompanionSlotCount && _companionLabels[slot] != null
                ? _companionLabels[slot].text
                : string.Empty;
        }

        private void Awake()
        {
            // GameFlowController owns visibility and opens the screen from Center.
            var document = GetComponent<UIDocument>();
            if (document != null && document.rootVisualElement != null)
                document.rootVisualElement.style.display = DisplayStyle.None;
        }

        /// <summary>Builds the screen once and applies the live Center selection.</summary>
        public void Initialize(PlayerSaveData save, CenterController center)
        {
            if (!_initialized)
            {
                _document = GetComponent<UIDocument>();
                if (_document == null || _document.rootVisualElement == null)
                {
                    Debug.LogError("[Ready] UIDocument with a source UXML is required.");
                    return;
                }

                VisualElement root = _document.rootVisualElement;
                ApplySkin(root);
                Bind(root);
                _initialized = true;
            }

            _payload = save ?? new PlayerSaveData();
            _center = center;
            Refresh();
            SetVisible(true);
        }

        /// <summary>Shows or hides the whole Ready overlay.</summary>
        public void SetVisible(bool visible)
        {
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_document != null && _document.rootVisualElement != null)
                _document.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Bind(VisualElement root)
        {
            _title = root.Q<Label>("ready-title");
            _playerLabel = root.Q<Label>("loadout-player");
            _statusLabel = root.Q<Label>("loadout-status");
            _briefingWords = root.Q<Label>("briefing-words");
            _launch = root.Q<Button>("launch-button");
            Button back = root.Q<Button>("back-button");

            for (int slot = 0; slot < CompanionSlotCount; slot++)
                _companionLabels[slot] = root.Q<Label>("companion-" + slot);

            if (_launch != null) _launch.clicked += OnLaunchClicked;
            if (back != null) back.clicked += () => Closed?.Invoke();
        }

        private void ApplySkin(VisualElement root)
        {
            SetSprite(root, "ready-backdrop", readyBackground, ScaleMode.ScaleAndCrop);
            SetSprite(root, "ready-title-plate", titlePlate, ScaleMode.ScaleAndCrop);
            SetSprite(root, "launch-button", launchButton, ScaleMode.ScaleToFit);
            SetSprite(root, "back-button", backButton, ScaleMode.ScaleToFit);
            SetSprite(root, "ready-loading", loadingMark, ScaleMode.ScaleToFit);
        }

        private static void SetSprite(VisualElement root, string name, Sprite sprite, ScaleMode scaleMode)
        {
            VisualElement element = root.Q(name);
            if (element != null && sprite != null)
                UiSpriteSkin.Apply(element, sprite, scaleMode, keepBackground: false);
        }

        private void Refresh()
        {
            int storyIndex = _payload.storyIndex;
            int fleetCount = _center != null ? _center.FleetCount : (_payload.blueprints?.Count ?? 0);
            Loadout = MissionLoadout.Capture(_payload, fleetCount);
            CanLaunch = _center != null && _center.CanLaunch;

            if (_title != null) _title.text = "BATTLE-" + (storyIndex + 1);
            if (_briefingWords != null) _briefingWords.text = MissionBriefing.Words(storyIndex);

            if (_playerLabel != null)
            {
                _playerLabel.text = Loadout.PlayerCraftIndex >= 0
                    ? CraftLabel(Loadout.PlayerCraftIndex)
                    : "NO CRAFT";
            }

            for (int slot = 0; slot < CompanionSlotCount; slot++)
                RefreshCompanion(slot);

            if (_statusLabel != null)
            {
                _statusLabel.text = CanLaunch
                    ? "All slots valid. Launch when ready."
                    : "Assign a valid player craft in Center first.";
            }

            if (_launch != null) _launch.SetEnabled(CanLaunch);
        }

        private void RefreshCompanion(int slot)
        {
            Label label = _companionLabels[slot];
            if (label == null) return;

            int craft = _payload.companions != null && slot < _payload.companions.Count
                ? _payload.companions[slot].craftIndex
                : -1;
            bool assigned = craft >= 0 && craft != Loadout.PlayerCraftIndex;
            label.text = assigned ? CraftLabel(craft) : "-";
            label.EnableInClassList("loadout-empty", !assigned);
        }

        private string CraftLabel(int index)
        {
            if (_center != null && index >= 0 && index < _center.FleetCount)
                return _center.CraftName(index) + "  [" + _center.StatsFor(index).cls + "]";

            if (_payload.blueprints != null && index >= 0 && index < _payload.blueprints.Count)
            {
                BlueprintSaveData blueprint = _payload.blueprints[index];
                return string.IsNullOrEmpty(blueprint.craftName) ? blueprint.hullName : blueprint.craftName;
            }

            return "-";
        }

        private void OnLaunchClicked()
        {
            if (!CanLaunch) return;
            LaunchRequested?.Invoke();
        }
    }
}
