using System;
using ABRevival.Core.Story;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Dedicated Results surface for UIWin (and UIAllWin at the last battle) and UIFail.
    /// The full-page Result backdrop is the frozen space render without its baked text;
    /// the dialog, typography and button plates keep the 2011 composition at 1920x1080.
    /// Next persists the story/reward, Retry returns without advancing, Repair opens UIRepair.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class ResultsController : MonoBehaviour
    {
        private const float PageWidth = 1920f;
        private const float PageHeight = 1080f;
        private const string VargamesUrl = "http://www.vargames.com";
        private const string FrozenTip = "Tip 9 : It's always cheaper to repair a part than re-buy it.";

        [Header("Results sprites (frozen 2011 UI library)")]
        [SerializeField] private Sprite winBackground;
        [SerializeField] private Sprite failBackground;
        [SerializeField] private Sprite nextButton;
        [SerializeField] private Sprite nextButtonHover;
        [SerializeField] private Sprite nextButtonPressed;
        [SerializeField] private Sprite restartButton;
        [SerializeField] private Sprite restartButtonHover;
        [SerializeField] private Sprite restartButtonPressed;
        [SerializeField] private Sprite repairButton;
        [SerializeField] private Sprite repairButtonHover;
        [SerializeField] private Sprite repairButtonPressed;
        [SerializeField] private Sprite retryButton;
        [SerializeField] private Sprite retryButtonHover;
        [SerializeField] private Sprite retryButtonPressed;
        [SerializeField] private Sprite backButton;
        [SerializeField] private Sprite backButtonHover;
        [SerializeField] private Sprite backButtonPressed;
        [SerializeField] private Sprite linkButton;
        [SerializeField] private Sprite linkButtonHover;
        [SerializeField] private Sprite endingButton;
        [SerializeField] private Sprite endingButtonHover;
        [SerializeField] private Sprite endingButtonPressed;
        [SerializeField] private Sprite allWinRestartButton;
        [SerializeField] private Sprite allWinRestartButtonHover;
        [SerializeField] private Sprite allWinRestartButtonPressed;

        private UIDocument _document;
        private VisualElement _page;
        private VisualElement _background;
        private VisualElement _panel;
        private Label _title;
        private Label _battle;
        private Label _cause;
        private Label _body;
        private Label _money;
        private Label _parts;
        private Label _verdict;
        private Label _warning;
        private Label _tip;
        private Button _leftButton;
        private Button _linkButton;
        private Button _rightButton;
        private Button _repairButton;

        private MissionSummary _summary;
        private bool _initialized;
        private Vector2 _lastRootSize;

        public event Action NextRequested;
        public event Action RetryRequested;
        public event Action ContinueRequested;
        public event Action RepairRequested;

        public MissionSummary Summary => _summary;

        private void Awake()
        {
            // GameFlowController owns visibility and opens the screen from the mission end.
            var document = GetComponent<UIDocument>();
            if (document != null && document.rootVisualElement != null)
                document.rootVisualElement.style.display = DisplayStyle.None;
        }

        /// <summary>Builds the screen once and applies one mission summary.</summary>
        public void Initialize(MissionSummary summary)
        {
            if (!_initialized)
            {
                _document = GetComponent<UIDocument>();
                if (_document == null || _document.rootVisualElement == null)
                {
                    Debug.LogError("[Results] UIDocument with a source UXML is required.");
                    return;
                }

                VisualElement root = _document.rootVisualElement;
                Bind(root);
                root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
                _initialized = true;
            }

            Apply(summary);
            SetVisible(true);
        }

        /// <summary>Shows or hides the whole result overlay.</summary>
        public void SetVisible(bool visible)
        {
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_document != null && _document.rootVisualElement != null)
                _document.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void Bind(VisualElement root)
        {
            _page = root.Q("results-page");
            _background = root.Q("results-background");
            _panel = root.Q("results-panel");
            _title = root.Q<Label>("results-title");
            _battle = root.Q<Label>("results-battle");
            _cause = root.Q<Label>("results-cause");
            _body = root.Q<Label>("results-body");
            _money = root.Q<Label>("results-money");
            _parts = root.Q<Label>("results-parts");
            _verdict = root.Q<Label>("results-verdict");
            _warning = root.Q<Label>("results-warning");
            _tip = root.Q<Label>("results-tip");
            _leftButton = root.Q<Button>("results-left");
            _linkButton = root.Q<Button>("results-link");
            _rightButton = root.Q<Button>("results-right");
            _repairButton = root.Q<Button>("results-repair");

            if (_leftButton != null) _leftButton.clicked += OnLeftClicked;
            if (_linkButton != null) _linkButton.clicked += OnLinkClicked;
            if (_rightButton != null) _rightButton.clicked += OnRightClicked;
            if (_repairButton != null) _repairButton.clicked += () => RepairRequested?.Invoke();
        }

        private void Apply(MissionSummary summary)
        {
            _summary = summary;
            bool finalWin = summary.IsFinalBattle;

            if (_title != null) _title.text = summary.Title;
            if (_battle != null) _battle.text = summary.BattleLabel;
            if (_cause != null) _cause.text = "Cause: " + summary.CauseLabel;
            if (_body != null) _body.text = summary.BodyLabel;
            if (_money != null) _money.text = summary.RewardLabel;
            if (_parts != null) _parts.text = summary.PartsLabel;
            if (_verdict != null) _verdict.text = summary.VerdictLabel;
            if (_warning != null)
            {
                _warning.text = summary.WarningLabel;
                _warning.style.display = string.IsNullOrEmpty(summary.WarningLabel)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }

            if (_tip != null) _tip.text = FrozenTip;
            if (_panel != null) _panel.EnableInClassList("results-panel-fail", !summary.Won);
            if (_body != null) _body.EnableInClassList("results-body-fail", !summary.Won);
            if (_title != null) _title.EnableInClassList("results-title-fail", !summary.Won);

            if (_background != null)
            {
                Sprite background = summary.Won ? winBackground : failBackground;
                if (background != null)
                    UiSpriteSkin.Apply(_background, background, ScaleMode.ScaleAndCrop, keepBackground: false);
            }

            if (summary.Won)
            {
                Sprite left = finalWin ? endingButton : restartButton;
                Sprite leftHover = finalWin ? endingButtonHover : restartButtonHover;
                Sprite leftPressed = finalWin ? endingButtonPressed : restartButtonPressed;
                Sprite right = finalWin ? allWinRestartButton : nextButton;
                Sprite rightHover = finalWin ? allWinRestartButtonHover : nextButtonHover;
                Sprite rightPressed = finalWin ? allWinRestartButtonPressed : nextButtonPressed;

                ApplyStates(_leftButton, left, leftHover, leftPressed);
                ApplyStates(_rightButton, right, rightHover, rightPressed);
            }
            else
            {
                ApplyStates(_leftButton, retryButton, retryButtonHover, retryButtonPressed);
                ApplyStates(_rightButton, backButton, backButtonHover, backButtonPressed);
            }

            ApplyStates(_linkButton, linkButton, linkButtonHover, linkButton);
            ApplyStates(_repairButton, repairButton, repairButtonHover, repairButtonPressed);
            if (_repairButton != null)
            {
                bool canRepair = summary.Won && summary.damagedParts > 0;
                _repairButton.style.display = canRepair ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private static void ApplyStates(Button button, Sprite normal, Sprite hover, Sprite pressed)
        {
            if (button != null && normal != null) UiSpriteSkin.ApplyStates(button, normal, hover, pressed);
        }

        private void OnLeftClicked()
        {
            if (_summary.Won && !_summary.IsFinalBattle) RetryRequested?.Invoke();
            else if (_summary.Won) NextRequested?.Invoke();       // UIAllWin ENDING
            else RetryRequested?.Invoke();                        // UIFail RETRY
        }

        private void OnRightClicked()
        {
            if (_summary.Won && !_summary.IsFinalBattle) NextRequested?.Invoke(); // UIWin NEXT
            else if (_summary.Won) RetryRequested?.Invoke();                      // UIAllWin RESTART
            else ContinueRequested?.Invoke();                                     // UIFail BACK
        }

        private static void OnLinkClicked()
        {
            Application.OpenURL(VargamesUrl);
        }

        /// <summary>Scales the 1920x1080 page to cover the root, like the frozen Stage3D canvas.</summary>
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
    }
}
