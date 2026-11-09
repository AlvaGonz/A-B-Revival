using System;
using ABRevival.Core.Persistence;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Scene entry point for the frozen 2011 UIMain page: play, options, data, credits and
    /// the player entry at 1920x1080 full-bleed. The committed UI_Main page render supplies
    /// the title, ship and button art; the original starfield extends it to the wider canvas.
    /// Credits, player and the VARGAMES link have no revival screen yet and report that
    /// explicitly instead of silently doing nothing.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuController : MonoBehaviour
    {
        private const float PageWidth = 667f;
        private const float PageHeight = 535f;
        private const float HeroHeight = 455f;

        [Header("Manual layout (tune in the Boot scene Inspector)")]
        [Tooltip("Multiplier on the full-width hero. 1 = exact screen width.")]
        [SerializeField] private float heroScale = 1f;
        [Tooltip("Hero X offset in page pixels at the current scale.")]
        [SerializeField] private float heroOffsetX = 0f;
        [Tooltip("Hero Y offset in page pixels. Negative hides the page's top plate line.")]
        [SerializeField] private float heroOffsetY = -18f;
        [Tooltip("Multiplier on the live button size.")]
        [SerializeField] private float buttonScale = 1f;
        [Tooltip("Bottom margin in page pixels before scaling.")]
        [SerializeField] private float bottomMargin = 23.7f;
        [Tooltip("Show the live VARGAMES/MUSIC PLAYER art. Leave off: the hero already contains the baked originals.")]
        [SerializeField] private bool showLiveTopButtons = false;

        [Header("Main menu sprites (frozen 2011 UI library)")]
        [SerializeField] private Sprite backdrop;
        [SerializeField] private Sprite pageArt;
        [SerializeField] private Sprite playNormal;
        [SerializeField] private Sprite playHover;
        [SerializeField] private Sprite dataNormal;
        [SerializeField] private Sprite dataHover;
        [SerializeField] private Sprite optionsNormal;
        [SerializeField] private Sprite optionsHover;
        [SerializeField] private Sprite creditsNormal;
        [SerializeField] private Sprite creditsHover;
        [SerializeField] private Sprite linkHover;
        [SerializeField] private Sprite playerNormal;
        [SerializeField] private Sprite playerHover;

        private UIDocument _document;
        private VisualElement _page;
        private VisualElement _deferred;
        private Label _deferredTitle;
        private Label _deferredBody;
        private Button _playButton;
        private Button _dataButton;
        private Button _optionsButton;
        private Button _creditsButton;
        private Button _linkButton;
        private Button _playerButton;

        private bool _initialized;
        private Vector2 _lastRootSize;

        /// <summary>Raised by the START GAME button; GameFlowController opens Center.</summary>
        public event Action PlayRequested;

        /// <summary>Raised by OPTIONS; GameFlowController opens the Options screen.</summary>
        public event Action OptionsRequested;

        /// <summary>Raised by DATA; GameFlowController opens the read-only records page.</summary>
        public event Action DataRequested;

        private void Awake()
        {
            // GameFlowController owns visibility and opens the main menu at boot.
            var document = GetComponent<UIDocument>();
            if (document != null && document.rootVisualElement != null)
                document.rootVisualElement.style.display = DisplayStyle.None;
        }

        /// <summary>Builds the screen once; the main menu is static and reads no save data.</summary>
        public void Initialize(PlayerSaveData save)
        {
            if (_initialized)
            {
                SetVisible(true);
                return;
            }

            _document = GetComponent<UIDocument>();
            if (_document == null || _document.rootVisualElement == null)
            {
                Debug.LogError("[MainMenu] UIDocument with a source UXML is required.");
                return;
            }

            VisualElement root = _document.rootVisualElement;
            ApplySkin(root);
            Bind(root);
            ApplyTopControlVisibility();
            root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            _initialized = true;
            SetVisible(true);
        }

        /// <summary>Shows or hides the whole main menu overlay.</summary>
        public void SetVisible(bool visible)
        {
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_document != null && _document.rootVisualElement != null)
                _document.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible) HideDeferred();
        }

        /// <summary>Opens the dated-entry panel (credits, player, external link).</summary>
        public void ShowDeferred(string title, string reason)
        {
            if (_deferred == null) return;
            if (_deferredTitle != null) _deferredTitle.text = title;
            if (_deferredBody != null) _deferredBody.text = reason;
            _deferred.style.display = DisplayStyle.Flex;
        }

        public void HideDeferred()
        {
            if (_deferred != null) _deferred.style.display = DisplayStyle.None;
        }

        private void Bind(VisualElement root)
        {
            _page = root.Q("main-page");
            _deferred = root.Q("main-deferred");
            _deferredTitle = root.Q<Label>("deferred-title");
            _deferredBody = root.Q<Label>("deferred-body");

            _playButton = root.Q<Button>("play-button");
            _dataButton = root.Q<Button>("data-button");
            _optionsButton = root.Q<Button>("options-button");
            _creditsButton = root.Q<Button>("credits-button");
            _linkButton = root.Q<Button>("link-button");
            _playerButton = root.Q<Button>("player-button");

            if (_playButton != null) _playButton.clicked += () => PlayRequested?.Invoke();
            if (_dataButton != null) _dataButton.clicked += () => DataRequested?.Invoke();
            if (_optionsButton != null) _optionsButton.clicked += () => OptionsRequested?.Invoke();
            if (_creditsButton != null)
                _creditsButton.clicked += () => ShowDeferred(
                    "CREDITS",
                    "The 2011 credits sequence is delivered by the credits ticket. No credits surface exists in the revival yet.");
            if (_linkButton != null)
                _linkButton.clicked += () => ShowDeferred(
                    "VARGAMES",
                    "The frozen menu opened vargames.com. The preservation build does not open external sites.");
            if (_playerButton != null)
                _playerButton.clicked += () => ShowDeferred(
                    "PLAYER",
                    "The frozen $player entry opens UIPlayer on the UI_Mission page. That screen is delivered by its own ticket.");

            Button back = root.Q<Button>("deferred-back");
            if (back != null) back.clicked += HideDeferred;

            BindHover(_playButton, playNormal, playHover);
            BindHover(_dataButton, dataNormal, dataHover);
            BindHover(_optionsButton, optionsNormal, optionsHover);
            BindHover(_creditsButton, creditsNormal, creditsHover);
            BindHover(_playerButton, playerNormal, playerHover);
            // The page render already carries the grey VARGAMES plate; only its hover
            // highlight is a separate widget.
            BindOverlayHover(_linkButton, linkHover);
        }

        private static void BindHover(Button button, Sprite normal, Sprite hover)
        {
            if (button == null || normal == null) return;
            UiSpriteSkin.Apply(button, normal, ScaleMode.StretchToFill, keepBackground: false);
            if (hover != null)
            {
                button.RegisterCallback<PointerEnterEvent>(
                    _ => UiSpriteSkin.Apply(button, hover, ScaleMode.StretchToFill, keepBackground: false));
                button.RegisterCallback<PointerLeaveEvent>(
                    _ => UiSpriteSkin.Apply(button, normal, ScaleMode.StretchToFill, keepBackground: false));
            }
        }

        private static void BindOverlayHover(Button button, Sprite hover)
        {
            if (button == null || hover == null) return;
            button.RegisterCallback<PointerEnterEvent>(
                _ => UiSpriteSkin.Apply(button, hover, ScaleMode.StretchToFill, keepBackground: false));
            button.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                Image art = button.Q<Image>("sprite-art");
                if (art != null) art.sprite = null;
            });
        }

        private void ApplySkin(VisualElement root)
        {
            // One background: the frozen starfield. The hero crop above it is feathered at
            // its bottom so the transition is continuous on tall/portrait screens.
            SetSprite(root, "main-backdrop", backdrop, ScaleMode.ScaleAndCrop);
            SetSprite(root, "main-art", pageArt, ScaleMode.StretchToFill);
        }

        private static void SetSprite(VisualElement root, string name, Sprite sprite, ScaleMode scaleMode)
        {
            VisualElement element = root.Q(name);
            if (element != null && sprite != null)
                UiSpriteSkin.Apply(element, sprite, scaleMode, keepBackground: false);
        }

        /// <summary>Centres the 667x535 page and scales it uniformly so the full menu is visible.</summary>
        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            FitPage(evt.newRect.width, evt.newRect.height);
        }

        private void FitPage(float width, float height)
        {
            if (_page == null || width <= 0f || height <= 0f) return;
            if (Mathf.Approximately(width, _lastRootSize.x) && Mathf.Approximately(height, _lastRootSize.y))
                return;

            _lastRootSize = new Vector2(width, height);
            float pageWidth = width * Mathf.Max(0.1f, heroScale);
            float scale = pageWidth / PageWidth;

            // The hero is the manual tuning surface: offset/scale are Inspector fields.
            _page.style.left = heroOffsetX * scale;
            _page.style.top = heroOffsetY * scale;
            _page.style.width = pageWidth;
            _page.style.height = HeroHeight * scale;

            LayoutBottomButton(_playButton, 20.15f, height, scale, buttonScale, bottomMargin, heroOffsetX);
            LayoutBottomButton(_dataButton, 176.55f, height, scale, buttonScale, bottomMargin, heroOffsetX);
            LayoutBottomButton(_optionsButton, 335.75f, height, scale, buttonScale, bottomMargin, heroOffsetX);
            LayoutBottomButton(_creditsButton, 492.15f, height, scale, buttonScale, bottomMargin, heroOffsetX);
            LayoutScreenButton(_linkButton, 492.15f, 51.2f, scale, buttonScale, heroOffsetX, heroOffsetY);
            LayoutScreenButton(_playerButton, 492.15f, 103.85f, scale, buttonScale, heroOffsetX, heroOffsetY);
        }

        private static void LayoutBottomButton(Button button, float pageX, float screenHeight,
            float scale, float buttonScale, float bottomMargin, float heroOffsetX)
        {
            if (button == null) return;
            float sizeScale = scale * Mathf.Max(0.1f, buttonScale);
            float buttonWidth = 153.7f * sizeScale;
            float buttonHeight = 42.2f * sizeScale;
            button.style.left = (pageX + heroOffsetX) * scale;
            button.style.top = screenHeight - buttonHeight - (bottomMargin * scale);
            button.style.width = buttonWidth;
            button.style.height = buttonHeight;
        }

        private static void LayoutScreenButton(Button button, float pageX, float pageY,
            float scale, float buttonScale, float heroOffsetX, float heroOffsetY)
        {
            if (button == null) return;
            float sizeScale = scale * Mathf.Max(0.1f, buttonScale);
            button.style.left = (pageX + heroOffsetX) * scale;
            button.style.top = (pageY + heroOffsetY) * scale;
            button.style.width = 153.7f * sizeScale;
            button.style.height = 42.2f * sizeScale;
        }

        private void ApplyTopControlVisibility()
        {
            float opacity = showLiveTopButtons ? 1f : 0f;
            if (_linkButton != null) _linkButton.style.opacity = opacity;
            if (_playerButton != null) _playerButton.style.opacity = opacity;
        }
    }
}
