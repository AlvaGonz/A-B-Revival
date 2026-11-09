using System;
using ABRevival.Core.Persistence;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Scene entry point for the frozen 2011 UIMain page: play, options, data, credits and
    /// the player entry. The menu recomposes the frozen background, title lettering and ship
    /// as separate responsive layers (ADR 0024); the starfield fills the screen.
    /// Credits, player and the VARGAMES link have no revival screen yet and report that
    /// explicitly instead of silently doing nothing.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuController : MonoBehaviour
    {
        private const float PageWidth = 667f;
        private const float ButtonWidth = 153.7f;
        private const float ButtonHeight = 42.2f;
        private const float TitleWidth = 394f;
        private const float TitleHeight = 52f;
        private const float TitleLeft = 20.15f;
        private const float TitleTop = 10f;
        private const float ShipWidth = 427f;
        private const float ShipHeight = 344f;
        private const float ShipWidthFraction = 0.8f;
        private const float ShipBandMargin = 20f;
        private const float TopControlLeft = 492.15f;
        private const float SafeAreaPadding = 8f;

        [Header("Manual layout (tune in the Boot scene Inspector)")]
        [Tooltip("Multiplier on the live button size.")]
        [SerializeField] private float buttonScale = 1f;
        [Tooltip("Bottom margin in page pixels before scaling.")]
        [SerializeField] private float bottomMargin = 20f;

        [Header("Main menu sprites (frozen 2011 UI library)")]
        [SerializeField] private Sprite backdrop;
        [SerializeField] private Sprite titleArt;
        [SerializeField] private Sprite shipArt;
        [SerializeField] private Sprite playNormal;
        [SerializeField] private Sprite playHover;
        [SerializeField] private Sprite dataNormal;
        [SerializeField] private Sprite dataHover;
        [SerializeField] private Sprite optionsNormal;
        [SerializeField] private Sprite optionsHover;
        [SerializeField] private Sprite creditsNormal;
        [SerializeField] private Sprite creditsHover;
        [Tooltip("Only the normal 20_* frame exists; it is also the hover frame.")]
        [SerializeField] private Sprite linkNormal;
        [SerializeField] private Sprite linkHover;
        [SerializeField] private Sprite playerNormal;
        [SerializeField] private Sprite playerHover;

        private UIDocument _document;
        private VisualElement _title;
        private VisualElement _ship;
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
            _title = root.Q("main-title");
            _ship = root.Q("main-ship");
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
            BindHover(_linkButton, linkNormal, linkHover);
            BindHover(_playerButton, playerNormal, playerHover);
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

        private void ApplySkin(VisualElement root)
        {
            // One background: the frozen starfield. Title and ship are separate layers.
            SetSprite(root, "main-backdrop", backdrop, ScaleMode.ScaleAndCrop);
            SetSprite(root, "main-title", titleArt, ScaleMode.StretchToFill);
            SetSprite(root, "main-ship", shipArt, ScaleMode.StretchToFill);
        }

        private static void SetSprite(VisualElement root, string name, Sprite sprite, ScaleMode scaleMode)
        {
            VisualElement element = root.Q(name);
            if (element != null && sprite != null)
                UiSpriteSkin.Apply(element, sprite, scaleMode, keepBackground: false);
        }

        /// <summary>Lays out the frozen layers at the current 667-wide page scale.</summary>
        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            FitPage(evt.newRect.width, evt.newRect.height);
        }

        private void FitPage(float width, float height)
        {
            if (_title == null || _ship == null || width <= 0f || height <= 0f) return;
            if (Mathf.Approximately(width, _lastRootSize.x) && Mathf.Approximately(height, _lastRootSize.y))
                return;

            _lastRootSize = new Vector2(width, height);
            float pageScale = width / PageWidth;
            float titleLeft = TitleLeft * pageScale;
            float titleWidth = TitleWidth * pageScale;
            float titleHeight = TitleHeight * pageScale;

            float safeTop = 0f;
            float safeBottom = 0f;
            if (Screen.height > 0f)
            {
                Rect safeArea = Screen.safeArea;
                safeTop = (1f - safeArea.yMax / Screen.height) * height;
                safeBottom = safeArea.yMin / Screen.height * height;
            }

            float titleTop = Mathf.Max(TitleTop * pageScale, safeTop + SafeAreaPadding);
            _title.style.left = titleLeft;
            _title.style.top = titleTop;
            _title.style.width = titleWidth;
            _title.style.height = titleHeight;

            float scaledBottomMargin = Mathf.Max(bottomMargin * pageScale, safeBottom + SafeAreaPadding);
            LayoutBottomButton(_playButton, 20.15f, height, pageScale, buttonScale, scaledBottomMargin);
            LayoutBottomButton(_dataButton, 176.55f, height, pageScale, buttonScale, scaledBottomMargin);
            LayoutBottomButton(_optionsButton, 335.75f, height, pageScale, buttonScale, scaledBottomMargin);
            LayoutBottomButton(_creditsButton, 492.15f, height, pageScale, buttonScale, scaledBottomMargin);
            LayoutScreenButton(_linkButton, TopControlLeft, 51.2f, pageScale, buttonScale);
            LayoutScreenButton(_playerButton, TopControlLeft, 103.85f, pageScale, buttonScale);

            // Ship fills a centred band between the title bottom and the bottom control row,
            // capped at 80% of the width so portrait phones keep side margins.
            float sizeScale = pageScale * Mathf.Max(0.1f, buttonScale);
            float controlTop = height - ButtonHeight * sizeScale - scaledBottomMargin;
            float bandTop = titleTop + titleHeight + ShipBandMargin * pageScale;
            float bandBottom = controlTop - ShipBandMargin * pageScale;
            float bandHeight = Mathf.Max(0f, bandBottom - bandTop);
            float topControlLeft = TopControlLeft * pageScale;
            float maxShipWidth = width * ShipWidthFraction;
            if (width > height)
            {
                // A centred landscape ship stops at the top-right control column instead of
                // passing behind VARGAMES/MUSIC PLAYER (visible on 4:3 tablets).
                maxShipWidth = Mathf.Min(maxShipWidth,
                    Mathf.Max(0f, 2f * (topControlLeft - width * 0.5f)));
            }

            float shipScale = Mathf.Min(bandHeight / ShipHeight, maxShipWidth / ShipWidth);
            shipScale = Mathf.Max(0f, shipScale);

            float shipWidth = ShipWidth * shipScale;
            float shipHeight = ShipHeight * shipScale;
            float shipLeft = (width - shipWidth) * 0.5f;
            float shipTop = bandTop + (bandHeight - shipHeight) * 0.5f;
            _ship.style.left = shipLeft;
            _ship.style.top = shipTop;
            _ship.style.width = shipWidth;
            _ship.style.height = shipHeight;

            // Development guards for the responsive contract.
            Debug.Assert(shipWidth > 0f && shipHeight > 0f,
                "[MainMenu] Ship collapsed to a non-positive size.");
            Debug.Assert(titleLeft >= 0f && titleLeft + titleWidth <= width,
                "[MainMenu] Title leaves the horizontal root bounds.");
            float gapTop = shipTop - bandTop;
            float gapBottom = bandBottom - (shipTop + shipHeight);
            Debug.Assert(Mathf.Abs(gapTop - gapBottom) <= bandHeight * 0.01f,
                "[MainMenu] Ship is not vertically centred in its band.");
            if (width > height)
            {
                // The landscape cap above guarantees horizontal separation from the top-right
                // controls; the epsilon keeps the touching 4:3 edge from tripping the guard.
                float buttonWidth = ButtonWidth * sizeScale;
                float buttonHeight = ButtonHeight * sizeScale;
                var shipRect = new Rect(shipLeft, shipTop,
                    Mathf.Max(0f, shipWidth - 0.01f), shipHeight);
                Debug.Assert(!shipRect.Overlaps(new Rect(
                        topControlLeft, 51.2f * pageScale, buttonWidth, buttonHeight)),
                    "[MainMenu] Ship overlaps the VARGAMES control.");
                Debug.Assert(!shipRect.Overlaps(new Rect(
                        topControlLeft, 103.85f * pageScale, buttonWidth, buttonHeight)),
                    "[MainMenu] Ship overlaps the MUSIC PLAYER control.");
            }
        }

        private static void LayoutBottomButton(Button button, float pageX, float screenHeight,
            float pageScale, float buttonScale, float scaledBottomMargin)
        {
            if (button == null) return;
            float sizeScale = pageScale * Mathf.Max(0.1f, buttonScale);
            float buttonWidth = ButtonWidth * sizeScale;
            float buttonHeight = ButtonHeight * sizeScale;
            button.style.left = pageX * pageScale;
            button.style.top = screenHeight - buttonHeight - scaledBottomMargin;
            button.style.width = buttonWidth;
            button.style.height = buttonHeight;
        }

        private static void LayoutScreenButton(Button button, float pageX, float pageY,
            float pageScale, float buttonScale)
        {
            if (button == null) return;
            float sizeScale = pageScale * Mathf.Max(0.1f, buttonScale);
            button.style.left = pageX * pageScale;
            button.style.top = pageY * pageScale;
            button.style.width = ButtonWidth * sizeScale;
            button.style.height = ButtonHeight * sizeScale;
        }

        /// <summary>
        /// Applies Inspector tuning immediately while playing, so the Game view updates
        /// without restarting the scene. Scene view never renders runtime UI Toolkit.
        /// </summary>
        private void OnValidate()
        {
            if (!Application.isPlaying || _document == null) return;
            VisualElement root = _document.rootVisualElement;
            if (root == null) return;

            _lastRootSize = Vector2.zero;
            FitPage(root.resolvedStyle.width, root.resolvedStyle.height);
        }

        [ContextMenu("Reapply Main Menu Layout")]
        private void ReapplyLayout()
        {
            if (_document == null) _document = GetComponent<UIDocument>();
            if (_document == null || _document.rootVisualElement == null) return;

            _lastRootSize = Vector2.zero;
            FitPage(_document.rootVisualElement.resolvedStyle.width,
                _document.rootVisualElement.resolvedStyle.height);
        }
    }
}
