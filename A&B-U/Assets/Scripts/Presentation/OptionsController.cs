using System;
using ABRevival.Core.Persistence;
using ABRevival.Core.Simulation;
using ABRevival.Presentation.Audio;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Scene entry point for the frozen 2011 UIOptions page: the six original settings
    /// (Bloom, Candy!, Background, Resolution, Sound, Music) plus the #179 control-mode
    /// switch. Values persist through the single player save; OK applies them, CANCEL
    /// restores the values the screen opened with. Bloom, Candy! and Background have no
    /// revival renderer to drive, so they are persisted fidelity settings only.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [RequireComponent(typeof(ScreenPanel))]
    public sealed class OptionsController : MonoBehaviour
    {
        private const float PageWidth = 850f;
        private const float PageHeight = 490f;

        private static readonly string[] DisplayModeLabels =
        {
            "640×480",
            "1280×720",
            "1920×1080",
            "Fullscreen"
        };

        [Header("Options hover sprites (frozen 2011 UI library)")]
        [SerializeField] private Sprite okHover;
        [SerializeField] private Sprite cancelHover;

        private ScreenPanel _screen;
        private VisualElement _page;
        private Button _bloomRow;
        private Button _candyRow;
        private Button _backgroundRow;
        private Button _resolutionRow;
        private Button _soundRow;
        private Button _musicRow;
        private Button _controlRow;
        private Label _bloomState;
        private Label _candyState;
        private Label _backgroundState;
        private Label _resolutionState;
        private Label _soundState;
        private Label _musicState;
        private Label _controlState;
        private Button _okButton;
        private Button _cancelButton;

        private PlayerSaveData _payload = new PlayerSaveData();
        private PlayerFlightController _flight;
        private AudioService _audio;

        private bool _bloom;
        private bool _candy;
        private bool _background;
        private int _displayMode;
        private bool _sound;
        private bool _music;
        private PlayerControlMode _controlMode;

        private bool _openedBloom;
        private bool _openedCandy;
        private bool _openedBackground;
        private int _openedDisplayMode;
        private bool _openedSound;
        private bool _openedMusic;
        private PlayerControlMode _openedControlMode;

        private bool _initialized;
        private Vector2 _lastRootSize;

        /// <summary>Raised after OK writes the working values into the shared payload.</summary>
        public event Action Applied;

        /// <summary>Raised on OK or CANCEL; GameFlowController returns to the main menu.</summary>
        public event Action Closed;

        public int DisplayMode => _displayMode;
        public bool SoundEnabled => _sound;
        public bool MusicEnabled => _music;
        public bool BloomEnabled => _bloom;
        public bool CandyEnabled => _candy;
        public bool BackgroundEnabled => _background;
        public PlayerControlMode ControlMode => _controlMode;

        private void Awake()
        {
            // GameFlowController owns visibility and opens Options from the main menu.
            _screen = GetComponent<ScreenPanel>();
            if (_screen != null) _screen.SetVisible(false);
        }

        /// <summary>Builds the screen once and opens it with the persisted settings.</summary>
        public void Initialize(PlayerSaveData save, PlayerFlightController flight, AudioService audio)
        {
            _screen = GetComponent<ScreenPanel>();
            if (_screen == null)
            {
                Debug.LogError("[Options] ScreenPanel with a PanelRenderer is required.");
                return;
            }

            _screen.WhenReady(root => Open(save, flight, audio, root));
        }

        /// <summary>Shows or hides the whole Options overlay.</summary>
        public void SetVisible(bool visible)
        {
            if (_screen == null) _screen = GetComponent<ScreenPanel>();
            _screen?.SetVisible(visible);
        }

        private void Open(PlayerSaveData save, PlayerFlightController flight, AudioService audio, VisualElement root)
        {
            if (!_initialized)
            {
                Bind(root);
                root.RegisterCallback<GeometryChangedEvent>(OnRootGeometryChanged);
                if (root.resolvedStyle.width > 1f && root.resolvedStyle.height > 1f)
                    FitPage(root.resolvedStyle.width, root.resolvedStyle.height);
                _initialized = true;
            }

            _payload = save ?? new PlayerSaveData();
            _flight = flight;
            _audio = audio;
            LoadWorking();
            RefreshRows();
            SetVisible(true);
        }

        /// <summary>
        /// Applies the persisted display cycle. The Editor owns its Game view size, so the
        /// resolution is only sent to the platform player.
        /// </summary>
        public static void ApplyDisplayMode(int mode)
        {
#if !UNITY_EDITOR
            switch (mode)
            {
                case 0:
                    Screen.SetResolution(640, 480, FullScreenMode.Windowed);
                    break;
                case 1:
                    Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
                    break;
                case 3:
                    Screen.SetResolution(
                        Screen.currentResolution.width,
                        Screen.currentResolution.height,
                        FullScreenMode.FullScreenWindow);
                    break;
                default:
                    Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
                    break;
            }
#endif
        }

        private void LoadWorking()
        {
            _bloom = _payload.bloomEnabled;
            _candy = _payload.candyEnabled;
            _background = _payload.backgroundEnabled;
            _displayMode = Mathf.Clamp(_payload.displayMode, 0, DisplayModeLabels.Length - 1);
            _sound = _payload.soundEnabled;
            _music = _payload.musicEnabled;
            _controlMode = _payload.controlMode;

            _openedBloom = _bloom;
            _openedCandy = _candy;
            _openedBackground = _background;
            _openedDisplayMode = _displayMode;
            _openedSound = _sound;
            _openedMusic = _music;
            _openedControlMode = _controlMode;
        }

        private void Bind(VisualElement root)
        {
            _page = root.Q("options-page");
            _bloomRow = root.Q<Button>("bloom-row");
            _candyRow = root.Q<Button>("candy-row");
            _backgroundRow = root.Q<Button>("background-row");
            _resolutionRow = root.Q<Button>("resolution-row");
            _soundRow = root.Q<Button>("sound-row");
            _musicRow = root.Q<Button>("music-row");
            _controlRow = root.Q<Button>("control-row");
            _bloomState = root.Q<Label>("bloom-state");
            _candyState = root.Q<Label>("candy-state");
            _backgroundState = root.Q<Label>("background-state");
            _resolutionState = root.Q<Label>("resolution-state");
            _soundState = root.Q<Label>("sound-state");
            _musicState = root.Q<Label>("music-state");
            _controlState = root.Q<Label>("control-state");
            _okButton = root.Q<Button>("ok-button");
            _cancelButton = root.Q<Button>("cancel-button");

            if (_bloomRow != null) _bloomRow.clicked += () => Toggle(ref _bloom);
            if (_candyRow != null) _candyRow.clicked += () => Toggle(ref _candy);
            if (_backgroundRow != null) _backgroundRow.clicked += () => Toggle(ref _background);
            if (_resolutionRow != null) _resolutionRow.clicked += CycleDisplayMode;
            if (_soundRow != null) _soundRow.clicked += ToggleSound;
            if (_musicRow != null) _musicRow.clicked += ToggleMusic;
            if (_controlRow != null) _controlRow.clicked += ToggleControlMode;
            if (_okButton != null) _okButton.clicked += Apply;
            if (_cancelButton != null) _cancelButton.clicked += Cancel;

            BindHover(_okButton, okHover);
            BindHover(_cancelButton, cancelHover);
        }

        private static void BindHover(Button button, Sprite hover)
        {
            if (button == null) return;
            SpriteElement art = button.Q<SpriteElement>("button-art");
            if (art == null || art.sprite == null || hover == null) return;

            Sprite normal = art.sprite;
            button.RegisterCallback<PointerEnterEvent>(_ => art.sprite = hover);
            button.RegisterCallback<PointerLeaveEvent>(_ => art.sprite = normal);
        }

        private void Toggle(ref bool value)
        {
            value = !value;
            _audio?.MenuClicked();
            RefreshRows();
        }

        private void ToggleSound()
        {
            _sound = !_sound;
            _audio?.MenuClicked();
            _audio?.SetSfxEnabled(_sound);
            RefreshRows();
        }

        private void ToggleMusic()
        {
            _music = !_music;
            _audio?.MenuClicked();
            _audio?.SetMusicEnabled(_music);
            RefreshRows();
        }

        private void ToggleControlMode()
        {
            _controlMode = _controlMode == PlayerControlMode.Keyboard
                ? PlayerControlMode.Mouse
                : PlayerControlMode.Keyboard;
            _audio?.MenuClicked();
            RefreshRows();
        }

        private void CycleDisplayMode()
        {
            _displayMode = (_displayMode + 1) % DisplayModeLabels.Length;
            _audio?.MenuClicked();
            RefreshRows();
        }

        private void RefreshRows()
        {
            SetState(_bloomState, _bloom);
            SetState(_candyState, _candy);
            SetState(_backgroundState, _background);
            SetState(_soundState, _sound);
            SetState(_musicState, _music);
            if (_resolutionState != null) _resolutionState.text = DisplayModeLabels[_displayMode];
            if (_controlState != null)
                _controlState.text = _controlMode == PlayerControlMode.Keyboard ? "Keyboard" : "Mouse";
        }

        private static void SetState(Label label, bool enabled)
        {
            if (label != null) label.text = enabled ? "On" : "Off";
        }

        private void Apply()
        {
            _payload.bloomEnabled = _bloom;
            _payload.candyEnabled = _candy;
            _payload.backgroundEnabled = _background;
            _payload.displayMode = _displayMode;
            _payload.soundEnabled = _sound;
            _payload.musicEnabled = _music;
            _payload.controlMode = _controlMode;

            _flight?.SetControlMode(_controlMode, notify: false);
            _audio?.SetSfxEnabled(_sound);
            _audio?.SetMusicEnabled(_music);
            ApplyDisplayMode(_displayMode);
            _audio?.MenuConfirmed();

            Applied?.Invoke();
            Close();
        }

        private void Cancel()
        {
            _bloom = _openedBloom;
            _candy = _openedCandy;
            _background = _openedBackground;
            _displayMode = _openedDisplayMode;
            _sound = _openedSound;
            _music = _openedMusic;
            _controlMode = _openedControlMode;

            _flight?.SetControlMode(_openedControlMode, notify: false);
            _audio?.SetSfxEnabled(_openedSound);
            _audio?.SetMusicEnabled(_openedMusic);
            _audio?.MenuCancelled();
            Close();
        }

        private void Close()
        {
            SetVisible(false);
            Closed?.Invoke();
        }

        /// <summary>Centres the 850x490 page and scales it uniformly so the full page is visible.</summary>
        private void OnRootGeometryChanged(GeometryChangedEvent evt)
        {
            FitPage(evt.newRect.width, evt.newRect.height);
        }

        private void FitPage(float width, float height)
        {
            MenuPageFitter.Fit(
                _page, width, height, PageWidth, PageHeight, cover: true, ref _lastRootSize);
        }
    }
}
