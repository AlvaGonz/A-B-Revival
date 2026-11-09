using System;
using ABRevival.Core.Data;
using ABRevival.Core.Persistence;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Scene entry point for the Ready / Briefing handoff: the frozen UI_MapBuilding page at
    /// full-bleed 1920x1080. The original page render supplies the metal panel, window frame,
    /// OK plate and MAP LOADING badge; the live window replaces its placeholder title and
    /// word lines with the campaign briefing and a compact loadout pre-check. Launch hands the
    /// selected Player craft plus AI companions to <see cref="GameFlowController"/> before
    /// talkBefore and the opening cinematic start. Back cancels to Center without launching.
    /// It never writes the save; <see cref="GameFlowController"/> owns the payload.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ReadyController : MonoBehaviour
    {
        private const float PageWidth = 851f;
        private const float PageHeight = 521f;

        [Header("Ready sprites (frozen 2011 UI library)")]
        [SerializeField] private Sprite pageArt;
        [SerializeField] private Sprite titlePlate;
        [SerializeField] private Sprite launchButton;
        [SerializeField] private Sprite backButton;

        private UIDocument _document;
        private PlayerSaveData _payload = new PlayerSaveData();
        private CenterController _center;

        private VisualElement _page;
        private Label _title;
        private Label _briefingWords;
        private Label _status;
        private Button _launch;

        private bool _initialized;
        private Vector2 _lastRootSize;

        /// <summary>Raised when Back cancels the handoff and returns to Center.</summary>
        public event Action Closed;

        /// <summary>Raised when the validated loadout should launch the mission.</summary>
        public event Action LaunchRequested;

        public bool CanLaunch { get; private set; }

        public MissionLoadout Loadout { get; private set; } = new MissionLoadout(-1, null);

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
                root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
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
            _page = root.Q("ready-page");
            _title = root.Q<Label>("ready-title");
            _briefingWords = root.Q<Label>("briefing-words");
            _status = root.Q<Label>("loadout-status");
            _launch = root.Q<Button>("launch-button");
            Button back = root.Q<Button>("back-button");

            if (_launch != null) _launch.clicked += OnLaunchClicked;
            if (back != null) back.clicked += () => Closed?.Invoke();
        }

        private void ApplySkin(VisualElement root)
        {
            SetSprite(root, "ready-page-art", pageArt, ScaleMode.ScaleToFit);
            SetSprite(root, "ready-title-plate", titlePlate, ScaleMode.ScaleAndCrop);
            SetSprite(root, "launch-button", launchButton, ScaleMode.ScaleToFit);
            SetSprite(root, "back-button", backButton, ScaleMode.ScaleToFit);
        }

        private static void SetSprite(VisualElement root, string name, Sprite sprite, ScaleMode scaleMode)
        {
            VisualElement element = root.Q(name);
            if (element != null && sprite != null)
                UiSpriteSkin.Apply(element, sprite, scaleMode, keepBackground: false);
        }

        /// <summary>
        /// Scales the 851x521 page to cover the root. Children keep page-local pixel
        /// coordinates, so the window, OK plate and MAP LOADING badge stay on their
        /// frozen positions at 16:9 and 16:10.
        /// </summary>
        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            FitPage(evt.newRect.width, evt.newRect.height);
        }

        private void FitPage(float width, float height)
        {
            if (_page == null || width <= 1f || height <= 1f) return;
            if (Mathf.Approximately(width, _lastRootSize.x) && Mathf.Approximately(height, _lastRootSize.y)) return;
            _lastRootSize = new Vector2(width, height);

            float scale = Mathf.Max(width / PageWidth, height / PageHeight);
            _page.style.left = (width - PageWidth) * 0.5f;
            _page.style.top = (height - PageHeight) * 0.5f;
            _page.style.transformOrigin = new StyleTransformOrigin(
                new TransformOrigin(Length.Percent(50f), Length.Percent(50f), 0f));
            _page.style.scale = new StyleScale(new Scale(new Vector3(scale, scale, 1f)));
        }

        private void Refresh()
        {
            int storyIndex = _payload.storyIndex;
            int fleetCount = _center != null ? _center.FleetCount : (_payload.blueprints?.Count ?? 0);
            Loadout = MissionLoadout.Capture(_payload, fleetCount);
            CanLaunch = _center != null && _center.CanLaunch;

            if (_title != null) _title.text = "BATTLE-" + (storyIndex + 1);
            if (_briefingWords != null) _briefingWords.text = MissionBriefing.Words(storyIndex);
            if (_status != null) _status.text = StatusLine();
            if (_launch != null) _launch.SetEnabled(CanLaunch);
        }

        private string StatusLine()
        {
            if (!CanLaunch || Loadout.PlayerCraftIndex < 0) return "NO VALID PLAYER CRAFT";

            int assigned = 0;
            if (_payload.companions != null)
            {
                for (int i = 0; i < _payload.companions.Count; i++)
                {
                    int craft = _payload.companions[i].craftIndex;
                    if (craft >= 0 && craft != Loadout.PlayerCraftIndex) assigned++;
                }
            }

            return "PLAYER " + CraftLabel(Loadout.PlayerCraftIndex) +
                   "   AI " + Mathf.Min(assigned, CompanionRoster.SlotCount) + "/" + CompanionRoster.SlotCount;
        }

        private string CraftLabel(int index)
        {
            if (_center != null && index >= 0 && index < _center.FleetCount)
                return _center.CraftName(index) + " [" + _center.StatsFor(index).cls + "]";

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
