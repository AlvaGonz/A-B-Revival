using System;
using ABRevival.Core.Persistence;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Scene entry point for the frozen 2011 UIMain page: play, options, data, credits and
    /// the player entry. Backdrop, title and buttons are authored in Main.uxml/Main.uss
    /// (ADR 0025); the ship band, bottom-row anchoring and safe area stay responsive in C#
    /// (ADR 0024). Credits, player and the VARGAMES link have no revival screen yet and
    /// report that explicitly instead of silently doing nothing.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [RequireComponent(typeof(ScreenPanel))]
    public sealed class MainMenuController : MonoBehaviour
    {
        private const float PageWidth = 667f;
        private const float ShipWidth = 427f;
        private const float ShipHeight = 344f;
        private const float ShipWidthFraction = 0.8f;
        private const float ShipBandMargin = 20f;
        private const float SafeAreaPadding = 8f;

        [Header("Responsive layout (tune in the Boot scene Inspector)")]
        [Tooltip("Bottom margin in page pixels; used when Anchor Bottom Row is on.")]
        [SerializeField] private float bottomMargin = 20f;
        [Tooltip("Keep the four bottom buttons bottom-anchored. Off uses each authored USS top.")]
        [SerializeField] private bool anchorBottomRow = true;
        [Tooltip("Multiplier on the auto-fitted ship scale; 1 = fit the available band.")]
        [SerializeField] private float shipScale = 1f;

        [Header("Main menu button sprites (frozen 2011 UI library)")]
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

        private ScreenPanel _screen;
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
            _screen = GetComponent<ScreenPanel>();
            if (_screen != null) _screen.SetVisible(false);
        }

        /// <summary>Builds the screen once; the main menu is static and reads no save data.</summary>
        public void Initialize(PlayerSaveData save)
        {
            if (_initialized)
            {
                SetVisible(true);
                return;
            }

            _screen = GetComponent<ScreenPanel>();
            if (_screen == null)
            {
                Debug.LogError("[MainMenu] ScreenPanel with a PanelRenderer is required.");
                return;
            }

            _initialized = true;
            _screen.WhenReady(Build);
            SetVisible(true);
        }

        /// <summary>Shows or hides the whole main menu overlay.</summary>
        public void SetVisible(bool visible)
        {
            if (_screen == null) _screen = GetComponent<ScreenPanel>();
            _screen?.SetVisible(visible);
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

        private void Build(VisualElement root)
        {
            Bind(root);
            root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
            // The panel may already be laid out; the geometry callback may not fire again.
            if (_title != null && _title.resolvedStyle.height > 0f)
                FitPage(root.resolvedStyle.width, root.resolvedStyle.height);
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

            SpriteElement art = button.Q<SpriteElement>("button-art");
            if (art != null)
            {
                art.sprite = normal;
                if (hover != null)
                {
                    button.RegisterCallback<PointerEnterEvent>(_ => art.sprite = hover);
                    button.RegisterCallback<PointerLeaveEvent>(_ => art.sprite = normal);
                }

                return;
            }

            // Other screens still render through UiSpriteSkin until the #217 migration.
            UiSpriteSkin.Apply(button, normal, ScaleMode.StretchToFill, keepBackground: false);
            if (hover != null)
            {
                button.RegisterCallback<PointerEnterEvent>(
                    _ => UiSpriteSkin.Apply(button, hover, ScaleMode.StretchToFill, keepBackground: false));
                button.RegisterCallback<PointerLeaveEvent>(
                    _ => UiSpriteSkin.Apply(button, normal, ScaleMode.StretchToFill, keepBackground: false));
            }
        }

        /// <summary>Lays out the responsive ship band and bottom-row anchor at the current page scale.</summary>
        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            FitPage(evt.newRect.width, evt.newRect.height);
        }

        private void FitPage(float width, float height)
        {
            if (_title == null || _ship == null || !(width > 0f) || !(height > 0f)) return;
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

            // The title position comes from USS; only a safe-area intrusion overrides it.
            if (safeTop > 0.01f) _title.style.top = safeTop;

            float scaledBottomMargin = Mathf.Max(bottomMargin * pageScale, safeBottom + SafeAreaPadding);
            float buttonHeight = _playButton != null ? _playButton.resolvedStyle.height : 0f;
            ApplyButtonHeight(buttonHeight);
            AnchorBottomRow(buttonHeight, height, scaledBottomMargin);

            float controlTop;
            if (anchorBottomRow && _playButton != null)
            {
                controlTop = height - buttonHeight - scaledBottomMargin;
            }
            else
            {
                controlTop = MinTop(_playButton, _dataButton, _optionsButton, _creditsButton);
                if (controlTop == float.MaxValue) controlTop = height;
            }

            // Automatic ship: centred band between the title bottom and the bottom control row.
            float bandTop = _title.worldBound.yMax + ShipBandMargin * pageScale;
            float bandBottom = controlTop - ShipBandMargin * pageScale;
            float bandHeight = Mathf.Max(0f, bandBottom - bandTop);
            float topControlLeft = _linkButton != null ? _linkButton.worldBound.xMin : width;
            float maxShipWidth = width * ShipWidthFraction;
            // A centred ship stops at the top-right control column instead of passing behind
            // VARGAMES/MUSIC PLAYER. Apply whenever the control column is right of centre.
            float controlCap = 2f * (topControlLeft - width * 0.5f) - 2f;
            if (controlCap > 0f) maxShipWidth = Mathf.Min(maxShipWidth, controlCap);

            float fitScale = Mathf.Min(bandHeight / ShipHeight, maxShipWidth / ShipWidth);
            // Owner-authored multiplier: no clamp; the Inspector value is authoritative.
            float appliedShipScale = Mathf.Max(0f, fitScale * Mathf.Max(0f, shipScale));
            float shipWidth = ShipWidth * appliedShipScale;
            float shipHeight = ShipHeight * appliedShipScale;
            _ship.style.left = (width - shipWidth) * 0.5f;
            _ship.style.top = bandTop + (bandHeight - shipHeight) * 0.5f;
            _ship.style.width = shipWidth;
            _ship.style.height = shipHeight;
        }

        /// <summary>Overrides only the shipped USS top; other authored layout stays untouched.</summary>
        private void AnchorBottomRow(float buttonHeight, float height, float scaledBottomMargin)
        {
            // Off: leave the authored USS top so UI Builder edits survive at runtime.
            if (!anchorBottomRow) return;

            float top = height - buttonHeight - scaledBottomMargin;
            SetTop(_playButton, top);
            SetTop(_dataButton, top);
            SetTop(_optionsButton, top);
            SetTop(_creditsButton, top);
        }

        /// <summary>
        /// Locks all six frames to the same height. USS aspect-ratio rounds the top row
        /// differently from the bottom row at portrait scales; the shared height is the
        /// invariant the owner asked for.
        /// </summary>
        private void ApplyButtonHeight(float height)
        {
            if (height <= 0f) return;
            SetHeight(_playButton, height);
            SetHeight(_dataButton, height);
            SetHeight(_optionsButton, height);
            SetHeight(_creditsButton, height);
            SetHeight(_linkButton, height);
            SetHeight(_playerButton, height);
        }

        private static void SetHeight(Button button, float height)
        {
            if (button != null) button.style.height = height;
        }

        private static void SetTop(Button button, float top)
        {
            if (button != null) button.style.top = top;
        }

        private static float MinTop(params Button[] buttons)
        {
            float top = float.MaxValue;
            foreach (Button button in buttons)
                if (button != null) top = Mathf.Min(top, button.worldBound.yMin);
            return top;
        }

        /// <summary>
        /// Applies Inspector tuning immediately while playing, so the Game view updates
        /// without restarting the scene. Scene view never renders runtime UI Toolkit.
        /// </summary>
        private void OnValidate()
        {
            if (!Application.isPlaying || _screen == null) return;
            VisualElement root = _screen.Root;
            if (root == null) return;

            _lastRootSize = Vector2.zero;
            FitPage(root.resolvedStyle.width, root.resolvedStyle.height);
        }

        [ContextMenu("Reapply Main Menu Layout")]
        private void ReapplyLayout()
        {
            if (_screen == null) _screen = GetComponent<ScreenPanel>();
            VisualElement root = _screen != null ? _screen.Root : null;
            if (root == null) return;

            _lastRootSize = Vector2.zero;
            FitPage(root.resolvedStyle.width, root.resolvedStyle.height);
        }
    }
}
