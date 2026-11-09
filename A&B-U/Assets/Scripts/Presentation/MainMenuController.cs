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
        private const float ShipWidth = 427f;
        private const float ShipHeight = 344f;
        private const float ShipWidthFraction = 0.8f;
        private const float ShipBandMargin = 20f;
        private const float SafeAreaPadding = 8f;

        [Header("Manual layout (tune in the Boot scene Inspector)")]
        [Tooltip("Multiplier on the live button size.")]
        [SerializeField] private float buttonScale = 1f;
        [Tooltip("Bottom margin in page pixels; used when Anchor Bottom Row is on.")]
        [SerializeField] private float bottomMargin = 20f;
        [Tooltip("Keep the four bottom buttons bottom-anchored. Off uses each authored Y.")]
        [SerializeField] private bool anchorBottomRow = true;

        [Header("Object layout (page pixels; zero backdrop size = full screen)")]
        [SerializeField] private Vector2 titlePlatePosition = new Vector2(0f, 0f);
        [SerializeField] private Vector2 titlePlateSize = new Vector2(460f, 92f);
        [SerializeField] private Vector2 titleMainPosition = new Vector2(8f, -2f);
        [SerializeField] private Vector2 titleMainSize = new Vector2(418f, 63f);
        [SerializeField] private Vector2 titleSubtitlePosition = new Vector2(16f, 45f);
        [SerializeField] private Vector2 titleSubtitleSize = new Vector2(402f, 21f);
        [Tooltip("Multiplier on the auto-fitted ship scale; 1 = fit the available band.")]
        [SerializeField] private float shipScale = 1f;
        [SerializeField] private Vector2 buttonSize = new Vector2(153.7f, 42.2f);
        [SerializeField] private Vector2 backdropOffset = Vector2.zero;
        [SerializeField] private Vector2 backdropSize = Vector2.zero;

        [Header("Button positions (page pixels)")]
        [SerializeField] private Vector2 playButtonPosition = new Vector2(20.15f, 469.1f);
        [SerializeField] private Vector2 dataButtonPosition = new Vector2(176.55f, 469.1f);
        [SerializeField] private Vector2 optionsButtonPosition = new Vector2(335.75f, 470.1f);
        [SerializeField] private Vector2 creditsButtonPosition = new Vector2(492.15f, 470.1f);
        [SerializeField] private Vector2 linkButtonPosition = new Vector2(492.15f, 51.2f);
        [SerializeField] private Vector2 playerButtonPosition = new Vector2(492.15f, 103.85f);

        [Header("Main menu sprites (frozen 2011 UI library)")]
        [SerializeField] private Sprite backdrop;
        [SerializeField] private Sprite titlePlateArt;
        [SerializeField] private Sprite titleArt;
        [SerializeField] private Sprite titleSubtitleArt;
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
        private VisualElement _backdrop;
        private VisualElement _titlePlate;
        private VisualElement _title;
        private VisualElement _titleSubtitle;
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
            _backdrop = root.Q("main-backdrop");
            _titlePlate = root.Q("main-title-plate");
            _title = root.Q("main-title");
            _titleSubtitle = root.Q("main-title-subtitle");
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
            // One background: the frozen starfield. Title plate, main lettering and subtitle are separate.
            SetSprite(root, "main-backdrop", backdrop, ScaleMode.ScaleAndCrop);
            SetSprite(root, "main-title-plate", titlePlateArt, ScaleMode.StretchToFill);
            SetSprite(root, "main-title", titleArt, ScaleMode.StretchToFill);
            SetSprite(root, "main-title-subtitle", titleSubtitleArt, ScaleMode.StretchToFill);
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
            if (_backdrop == null || _titlePlate == null || _title == null || _titleSubtitle == null ||
                _ship == null || width <= 0f || height <= 0f) return;
            if (Mathf.Approximately(width, _lastRootSize.x) && Mathf.Approximately(height, _lastRootSize.y))
                return;

            _lastRootSize = new Vector2(width, height);
            float pageScale = width / PageWidth;

            float safeTop = 0f;
            float safeBottom = 0f;
            if (Screen.height > 0f)
            {
                Rect safeArea = Screen.safeArea;
                safeTop = (1f - safeArea.yMax / Screen.height) * height;
                safeBottom = safeArea.yMin / Screen.height * height;
            }

            // Backdrop: full screen by default; a non-zero authored size opts into manual control.
            if (backdropSize.x <= 0f || backdropSize.y <= 0f)
            {
                _backdrop.style.left = 0f;
                _backdrop.style.top = 0f;
                _backdrop.style.width = width;
                _backdrop.style.height = height;
            }
            else
            {
                _backdrop.style.left = backdropOffset.x * pageScale;
                _backdrop.style.top = backdropOffset.y * pageScale;
                _backdrop.style.width = backdropSize.x * pageScale;
                _backdrop.style.height = backdropSize.y * pageScale;
            }

            // Title: plate, main lettering and subtitle are independent page-space layers.
            float plateLeft = titlePlatePosition.x * pageScale;
            float plateTop = Mathf.Max(titlePlatePosition.y * pageScale, safeTop);
            float titleDelta = plateTop - titlePlatePosition.y * pageScale;
            _titlePlate.style.left = plateLeft;
            _titlePlate.style.top = plateTop;
            _titlePlate.style.width = titlePlateSize.x * pageScale;
            _titlePlate.style.height = titlePlateSize.y * pageScale;

            _title.style.left = titleMainPosition.x * pageScale;
            _title.style.top = titleMainPosition.y * pageScale + titleDelta;
            _title.style.width = titleMainSize.x * pageScale;
            _title.style.height = titleMainSize.y * pageScale;

            _titleSubtitle.style.left = titleSubtitlePosition.x * pageScale;
            _titleSubtitle.style.top = titleSubtitlePosition.y * pageScale + titleDelta;
            _titleSubtitle.style.width = titleSubtitleSize.x * pageScale;
            _titleSubtitle.style.height = titleSubtitleSize.y * pageScale;

            float subtitleBottom = titleSubtitlePosition.y * pageScale + titleDelta +
                                   titleSubtitleSize.y * pageScale;

            float scaledBottomMargin = Mathf.Max(bottomMargin * pageScale, safeBottom + SafeAreaPadding);
            LayoutBottomButton(_playButton, playButtonPosition, height, pageScale, buttonScale,
                scaledBottomMargin, anchorBottomRow, buttonSize);
            LayoutBottomButton(_dataButton, dataButtonPosition, height, pageScale, buttonScale,
                scaledBottomMargin, anchorBottomRow, buttonSize);
            LayoutBottomButton(_optionsButton, optionsButtonPosition, height, pageScale, buttonScale,
                scaledBottomMargin, anchorBottomRow, buttonSize);
            LayoutBottomButton(_creditsButton, creditsButtonPosition, height, pageScale, buttonScale,
                scaledBottomMargin, anchorBottomRow, buttonSize);
            LayoutScreenButton(_linkButton, linkButtonPosition, pageScale, buttonScale, buttonSize);
            LayoutScreenButton(_playerButton, playerButtonPosition, pageScale, buttonScale, buttonSize);

            // Automatic ship: centred band between the subtitle bottom and the bottom control row.
            float sizeScale = pageScale * Mathf.Max(0.1f, buttonScale);
            float controlTop = height - buttonSize.y * sizeScale - scaledBottomMargin;
            float bandTop = subtitleBottom + ShipBandMargin * pageScale;
            float bandBottom = controlTop - ShipBandMargin * pageScale;
            float bandHeight = Mathf.Max(0f, bandBottom - bandTop);
            float topControlLeft = linkButtonPosition.x * pageScale;
            float maxShipWidth = width * ShipWidthFraction;
            if (width > height)
            {
                // A centred landscape ship stops at the top-right control column instead of
                // passing behind VARGAMES/MUSIC PLAYER (visible on 4:3 tablets).
                maxShipWidth = Mathf.Min(maxShipWidth,
                    Mathf.Max(0f, 2f * (topControlLeft - width * 0.5f)));
            }

            float fitScale = Mathf.Min(bandHeight / ShipHeight, maxShipWidth / ShipWidth);
            float appliedShipScale = Mathf.Max(0f, fitScale * Mathf.Max(0f, shipScale));
            float shipWidth = ShipWidth * appliedShipScale;
            float shipHeight = ShipHeight * appliedShipScale;
            _ship.style.left = (width - shipWidth) * 0.5f;
            _ship.style.top = bandTop + (bandHeight - shipHeight) * 0.5f;
            _ship.style.width = shipWidth;
            _ship.style.height = shipHeight;
        }

        private static void LayoutBottomButton(Button button, Vector2 position, float screenHeight,
            float pageScale, float buttonScale, float scaledBottomMargin, bool anchorBottom,
            Vector2 authoredSize)
        {
            if (button == null) return;
            float sizeScale = pageScale * Mathf.Max(0.1f, buttonScale);
            float buttonWidth = authoredSize.x * sizeScale;
            float buttonHeight = authoredSize.y * sizeScale;
            button.style.left = position.x * pageScale;
            button.style.top = anchorBottom
                ? screenHeight - buttonHeight - scaledBottomMargin
                : position.y * pageScale;
            button.style.width = buttonWidth;
            button.style.height = buttonHeight;
        }

        private static void LayoutScreenButton(Button button, Vector2 position,
            float pageScale, float buttonScale, Vector2 authoredSize)
        {
            if (button == null) return;
            float sizeScale = pageScale * Mathf.Max(0.1f, buttonScale);
            button.style.left = position.x * pageScale;
            button.style.top = position.y * pageScale;
            button.style.width = authoredSize.x * sizeScale;
            button.style.height = authoredSize.y * sizeScale;
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
