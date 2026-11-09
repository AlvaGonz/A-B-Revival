using System;
using System.Collections.Generic;
using System.IO;
using ABRevival.Core.Camera;
using ABRevival.Core.Data;
using ABRevival.Core.Hud;
using ABRevival.Core.Persistence;
using ABRevival.Core.Shop;
using ABRevival.Core.Simulation;
using ABRevival.Core.Story;
using ABRevival.Presentation.Audio;
using ABRevival.Presentation.Cinematics;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Video;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Story-0 game loop seam: Boot -> Center -> Briefing -> Mission -> Result/Fail -> Return.
    /// Owns the single player save, drives <see cref="CenterController"/>, resolves story cues,
    /// steps the Burst-scheduled simulation with the PhysicsCore2D contact layer, feeds the HUD,
    /// consumes the mission result exactly once, shows the dedicated Results/Repair screens and
    /// advances STORY on the win's Next (UIWin.toNext / UIWin.nextGame).
    /// Original game IP: Vargames (2011).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameFlowController : MonoBehaviour, ICraftDestroyedSink
    {
        public const string DefaultSaveFileName = "player-save.json";
        public const float FixedStepSeconds = MissionRunner.DefaultTickSeconds;
        private const int ForceWinTickLimit = 10000;
        private const int MaxSubstepsPerFrame = 4;
        private const int MaxTalkAdvancesPerSkip = 1024;
        private const int TacticalModeCount = (int)CaptainTacticalMode.Shield + 1;

        private const string DefFlyResourcePath = "Data/3_all.AllData_defFly";
        private const string DefPartResourcePath = "Data/8_all.AllData_defPart";
        private const string DefFireResourcePath = "Data/2_all.AllData_defFire";
        private const string DefBulletResourcePath = "Data/11_all.AllData_defBullet";

        [SerializeField] private MainMenuController _mainMenu;
        [SerializeField] private OptionsController _options;
        [SerializeField] private CenterController _center;
        [SerializeField] private FactoryController _factory;
        [SerializeField] private ShopController _shop;
        [SerializeField] private InfoController _info;
        [SerializeField] private ReadyController _ready;
        [SerializeField] private ResultsController _results;
        [SerializeField] private RepairController _repairScreen;
        [SerializeField] private TalkScreen _talkScreen;
        [SerializeField] private CinematicPlayer _cinematicPlayer;
        [SerializeField] private CombatHudBehaviour _hud;
        [SerializeField] private CombatCameraRig _cameraRig;
        [SerializeField] private MissionSpriteView _spriteView;
        [SerializeField] private PlayerFlightController _flight;
        [SerializeField] private AudioService _audio;
        [SerializeField] private UIDocument _flowDocument;
        [SerializeField] private VideoClip[] _briefingClips = Array.Empty<VideoClip>();
        [SerializeField] private string _saveFileName = DefaultSaveFileName;
        [SerializeField] private string _levelResourcePath = "Levels/10_all.AllData_levels";
        [SerializeField] private int _maxShips = 64;
        [SerializeField] private int _maxProjectiles = 256;
        [SerializeField] private int _contactPartsPerHull = 32;

        private PlayerSaveData _save;
        private StoryDirector _story;
        private TalkScript _talkScript;
        private LevelCatalog _catalog;
        private CraftCatalog _craftCatalog;
        private PartMountTable _mountTable;
        private LevelRecord _level;
        private string _savePath;

        private ScheduledCombatSimulation _simulation;
        private MissionRunner _runner;
        private ProjectileContactSolver _solver;
        private WeaponSystem _weapons;
        private ProjectileDamageHandoff _damageHandoff;
        private ICombatFireInput _combatInput;
        private int[] _hullToShip;
        private int[] _shipToHull;
        private int _registeredShipCount;
        private int _playerHull = -1;
        private float _playerHullMax = 1f;
        private MissionResult? _lastResult;
        private MissionSummary _lastSummary;
        private RepairState _repair;
        private int _moneyBeforeResult;
        private readonly List<DamagedPart> _damagedParts = new List<DamagedPart>();
        private int[] _hullPartBlueprint;
        private int[] _hullPartMounted;
        private MissionLoadout _activeLoadout;

        private IMissionAudio _missionAudio;
        private IAssemblyAudio _assemblyAudio;
        private ICombatAudio _combatAudio;

        private VisualElement _cinematicSurface;
        private RenderTexture _renderTexture;

        private float _accumulator;
        private int _captainIndex = -1;
        private bool _briefingTalkActive;
        private bool _cinematicSkipBound;
        private bool _centerStarted;
        private bool _infoReturnToMainMenu;
        private readonly List<int> _wingmanShipIndices = new List<int>();
        private readonly Dictionary<int, int> _wingmanBlueprints = new Dictionary<int, int>();

        public GameFlowState State { get; private set; } = GameFlowState.Boot;
        public int StoryIndex => _save != null ? _save.storyIndex : 0;
        public MissionRunner Runner => _runner;
        public ScheduledCombatSimulation Simulation => _simulation;
        public PlayerFlightController Flight => _flight;
        public ProjectileContactSolver ContactSolver => _solver;
        public MissionResult? LastResult => _lastResult;
        public MissionSummary LastSummary => _lastSummary;
        public RepairState CurrentRepair => _repair;
        public IReadOnlyList<DamagedPart> DamagedParts => _damagedParts;
        public string SavePath => _savePath;

        /// <summary>Selected Player craft and AI companions handed to the current mission.</summary>
        public MissionLoadout ActiveLoadout => _activeLoadout;

        /// <summary>Projectiles fired by the player craft this mission (real setup path telemetry).</summary>
        public int ShotsFired => _weapons != null ? _weapons.ShotsFired : 0;

        /// <summary>Simulation indices of the AI wingmen spawned from the Ready loadout, in slot order.</summary>
        public IReadOnlyList<int> WingmanShipIndices => _wingmanShipIndices;

        /// <summary>Hull index registered for a simulation ship, or -1 while it has no contact hull.</summary>
        public int HullForShip(int shipIndex)
        {
            return _shipToHull != null && shipIndex >= 0 && shipIndex < _shipToHull.Length
                ? _shipToHull[shipIndex]
                : -1;
        }

        /// <summary>Projectiles fired by one craft, for wingman weapon telemetry.</summary>
        public int ShotsFiredBy(int shipIndex)
        {
            return _weapons != null ? _weapons.ShotsFiredBy(shipIndex) : 0;
        }

        private void Awake()
        {
            _savePath = Path.Combine(Application.persistentDataPath, _saveFileName);
            LoadSave();

            if (_audio != null)
            {
                _missionAudio = _audio;
                _assemblyAudio = _audio;
                _combatAudio = _audio;
            }

            _combatInput = GetComponent<MouseCombatFireInput>();
            if (_combatInput == null) _combatInput = gameObject.AddComponent<MouseCombatFireInput>();

            if (_mainMenu != null)
            {
                _mainMenu.PlayRequested += OnMainMenuPlayRequested;
                _mainMenu.OptionsRequested += OnMainMenuOptionsRequested;
                _mainMenu.DataRequested += OnMainMenuDataRequested;
            }
            if (_options != null)
            {
                _options.Applied += OnOptionsApplied;
                _options.Closed += OnOptionsClosed;
            }
            if (_center != null) _center.Changed += OnCenterChanged;
            if (_center != null) _center.MainMenuRequested += OnCenterMainMenuRequested;
            if (_center != null) _center.FactoryRequested += OnFactoryRequested;
            if (_center != null) _center.ShopRequested += OnShopRequested;
            if (_center != null) _center.InfoRequested += OnInfoRequested;
            if (_center != null) _center.ReadyRequested += OnReadyRequested;
            if (_factory != null) _factory.Changed += OnCenterChanged;
            if (_factory != null) _factory.Closed += OnFactoryClosed;
            if (_shop != null) _shop.Changed += OnCenterChanged;
            if (_shop != null) _shop.Closed += OnShopClosed;
            if (_info != null) _info.Closed += OnInfoClosed;
            if (_ready != null) _ready.Closed += OnReadyClosed;
            if (_ready != null) _ready.LaunchRequested += OnReadyLaunch;
            if (_results != null)
            {
                _results.NextRequested += OnResultNext;
                _results.RetryRequested += OnResultRetry;
                _results.ContinueRequested += OnResultContinue;
                _results.RepairRequested += OnResultRepair;
            }
            if (_repairScreen != null)
            {
                _repairScreen.Closed += OnRepairClosed;
                _repairScreen.Changed += OnRepairChanged;
            }
            if (_talkScreen != null)
            {
                _talkScreen.Completed += OnTalkCompleted;
                if (_audio != null) _talkScreen.Audio = _audio;
            }
            if (_flight != null)
            {
                _flight.TacticalModeRequested += OnTacticalModeRequested;
                _flight.CameraDistanceChanged += OnCameraDistanceChanged;
                _flight.ControlModeChanged += OnControlModeChanged;
            }
        }

        private void Start()
        {
            EnsureOverlayReferences();
            ApplyAudioSettings();
            OptionsController.ApplyDisplayMode(_save.displayMode);
            EnterMainMenu();
        }

        private void OnDestroy()
        {
            if (_mainMenu != null)
            {
                _mainMenu.PlayRequested -= OnMainMenuPlayRequested;
                _mainMenu.OptionsRequested -= OnMainMenuOptionsRequested;
                _mainMenu.DataRequested -= OnMainMenuDataRequested;
            }
            if (_options != null)
            {
                _options.Applied -= OnOptionsApplied;
                _options.Closed -= OnOptionsClosed;
            }
            if (_center != null) _center.Changed -= OnCenterChanged;
            if (_center != null) _center.MainMenuRequested -= OnCenterMainMenuRequested;
            if (_center != null) _center.FactoryRequested -= OnFactoryRequested;
            if (_center != null) _center.ShopRequested -= OnShopRequested;
            if (_center != null) _center.InfoRequested -= OnInfoRequested;
            if (_center != null) _center.ReadyRequested -= OnReadyRequested;
            if (_factory != null) _factory.Changed -= OnCenterChanged;
            if (_factory != null) _factory.Closed -= OnFactoryClosed;
            if (_shop != null) _shop.Changed -= OnCenterChanged;
            if (_shop != null) _shop.Closed -= OnShopClosed;
            if (_info != null) _info.Closed -= OnInfoClosed;
            if (_ready != null) _ready.Closed -= OnReadyClosed;
            if (_ready != null) _ready.LaunchRequested -= OnReadyLaunch;
            if (_results != null)
            {
                _results.NextRequested -= OnResultNext;
                _results.RetryRequested -= OnResultRetry;
                _results.ContinueRequested -= OnResultContinue;
                _results.RepairRequested -= OnResultRepair;
            }
            if (_repairScreen != null)
            {
                _repairScreen.Closed -= OnRepairClosed;
                _repairScreen.Changed -= OnRepairChanged;
            }
            if (_talkScreen != null) _talkScreen.Completed -= OnTalkCompleted;
            if (_flight != null)
            {
                _flight.TacticalModeRequested -= OnTacticalModeRequested;
                _flight.CameraDistanceChanged -= OnCameraDistanceChanged;
                _flight.ControlModeChanged -= OnControlModeChanged;
            }

            DisposeMission();
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Destroy(_renderTexture);
                _renderTexture = null;
            }
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        /// <summary>
        /// Advances the flow by one frame. The mission path is fixed-step and allocation-free;
        /// every other state only flips flags or plays a queued cue.
        /// </summary>
        public void Tick(float deltaTime)
        {
            switch (State)
            {
                case GameFlowState.Mission:
                    TickMission(deltaTime);
                    break;
                case GameFlowState.Return:
                    EnterCenter(firstBoot: false);
                    break;
            }
        }

        // ---------------- Boot / center ----------------

        private void LoadSave()
        {
            _save = new PlayerSaveData();
            try
            {
                if (File.Exists(_savePath))
                {
                    _save = PlayerSaveCodec.LoadFromFile(_savePath);
                }
            }
            catch (Exception exception)
            {
                // Quarantine the unreadable file so the next autosave cannot silently
                // overwrite proof of the corruption.
                string corruptPath = _savePath + ".corrupt";
                try
                {
                    // File.Move(overwrite) is unavailable on netstandard2.1.
                    if (File.Exists(corruptPath)) File.Delete(corruptPath);
                    File.Move(_savePath, corruptPath);
                    Debug.LogError(
                        $"[GameFlow] Save was corrupt and was moved to {corruptPath}; starting a new campaign: {exception.Message}");
                }
                catch (Exception quarantineException)
                {
                    Debug.LogError(
                        $"[GameFlow] Save load failed and {_savePath} could not be quarantined " +
                        $"({quarantineException.Message}); starting a new campaign: {exception.Message}");
                }

                _save = new PlayerSaveData();
            }

            _story = new StoryDirector(_save, Autosave);
            _hud?.State.SetCameraDistance(_save.cameraPreset);
            _cameraRig?.ApplyPreset(_save.cameraPreset);
            _flight?.SetControlMode(_save.controlMode);
        }

        private void ApplyAudioSettings()
        {
            _audio?.SetMusicEnabled(_save.musicEnabled);
            _audio?.SetSfxEnabled(_save.soundEnabled);
        }

        private void Autosave(PlayerSaveData payload)
        {
            try
            {
                // Only the active screen owns the live fleet; capturing the other one would
                // write a stale blueprint list over the edit in progress. Result/Repair already
                // mutate the payload's blueprints directly (repairs and lost parts).
                if (State == GameFlowState.Factory) _factory?.CaptureSave(payload);
                else if (State == GameFlowState.Shop) _shop?.CaptureSave(payload);
                else if (State == GameFlowState.Result || State == GameFlowState.Fail ||
                         State == GameFlowState.Repair)
                {
                    // Payload owns the fleet for this window.
                }
                else if (State == GameFlowState.Mission)
                {
                    // The mission-end save is payload-owned: the reward and every damaged part
                    // are already on the payload, and Center's live shop is the pre-mission
                    // snapshot that would overwrite them.
                }
                else if (State == GameFlowState.Center || State == GameFlowState.Ready ||
                         State == GameFlowState.Briefing || State == GameFlowState.Return)
                    _center?.CaptureSave(payload);
                PlayerSaveCodec.SaveToFile(_savePath, payload);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[GameFlow] Autosave failed: {exception.Message}");
            }
        }

        /// <summary>Center or Factory edits (Player pick, companions, blueprint, parts) persist immediately.</summary>
        private void OnCenterChanged()
        {
            Autosave(_save);
        }

        // ---------------- Main menu / Options ----------------

        /// <summary>Boot enters the frozen UIMain page; PLAY opens the saved campaign in Center.</summary>
        private void EnterMainMenu()
        {
            DisposeMission();
            _briefingTalkActive = false;
            _accumulator = 0f;
            _captainIndex = -1;
            _activeLoadout = null;
            _level = null;

            SetFlowVisible(false);
            _talkScreen?.SetVisible(false);
            _factory?.SetVisible(false);
            _shop?.SetVisible(false);
            _info?.SetVisible(false);
            _ready?.SetVisible(false);
            _center?.SetVisible(false);
            _options?.SetVisible(false);

            _mainMenu?.Initialize(_save);
            _mainMenu?.SetVisible(true);
            _audio?.PlayMainMenuMusic();
            State = GameFlowState.MainMenu;
        }

        private void OnMainMenuPlayRequested()
        {
            if (State != GameFlowState.MainMenu) return;

            _mainMenu?.SetVisible(false);
            EnterCenter(firstBoot: !_centerStarted);
        }

        private void OnMainMenuOptionsRequested()
        {
            if (State != GameFlowState.MainMenu || _options == null) return;

            _mainMenu?.SetVisible(false);
            _options.Initialize(_save, _flight, _audio);
            State = GameFlowState.Options;
        }

        /// <summary>DATA opens the already-delivered Info/Records page on its records tab.</summary>
        private void OnMainMenuDataRequested()
        {
            if (State != GameFlowState.MainMenu || _info == null) return;

            _mainMenu?.SetVisible(false);
            _info.Initialize(_save);
            _info.ShowRecords();
            _infoReturnToMainMenu = true;
            State = GameFlowState.Info;
        }

        private void OnOptionsApplied()
        {
            if (State != GameFlowState.Options) return;
            if (_flight != null) _save.controlMode = _flight.ControlMode;
            Autosave(_save);
        }

        private void OnOptionsClosed()
        {
            if (State != GameFlowState.Options) return;

            _mainMenu?.SetVisible(true);
            _audio?.PlayMainMenuMusic();
            State = GameFlowState.MainMenu;
        }

        /// <summary>Center's BACK returns to the main menu, matching UIMain/UIChooseFlyAI.</summary>
        private void OnCenterMainMenuRequested()
        {
            if (State != GameFlowState.Center) return;

            _center?.SetVisible(false);
            EnterMainMenu();
        }

        // ---------------- Factory ----------------

        /// <summary>Center's FACTORY button opens the dedicated assembly screen (#175).</summary>
        private void OnFactoryRequested()
        {
            if (State != GameFlowState.Center || _center == null || _factory == null) return;

            _center.CaptureSave(_save);
            _factory.Initialize(_save, _center.SelectedCraftIndex);
            _center.SetVisible(false);
            _factory.SetVisible(true);
            _assemblyAudio?.PlayAssemblyMusic();
            State = GameFlowState.Factory;
        }

        /// <summary>Factory's Back to Center persists the blueprints, then Center reloads them.</summary>
        private void OnFactoryClosed()
        {
            if (State != GameFlowState.Factory || _factory == null) return;

            _factory.CaptureSave(_save);
            _factory.SetVisible(false);
            _center?.ApplySave(_save);
            _center?.SetVisible(true);
            State = GameFlowState.Center;
            Autosave(_save);
            _assemblyAudio?.PlayAssemblyMusic();
        }

        // ---------------- Shop ----------------

        /// <summary>Center's SHOP button opens the dedicated purchase screen (#176).</summary>
        private void OnShopRequested()
        {
            if (State != GameFlowState.Center || _center == null || _shop == null) return;

            _center.CaptureSave(_save);
            _shop.Initialize(_save);
            _center.SetVisible(false);
            _shop.SetVisible(true);
            _assemblyAudio?.PlayAssemblyMusic();
            State = GameFlowState.Shop;
        }

        /// <summary>
        /// Shop's CENTER button persists money, stock and owned parts, then Center reloads only
        /// that slice so its fleet, Player pick and companion state survive the visit.
        /// </summary>
        private void OnShopClosed()
        {
            if (State != GameFlowState.Shop || _shop == null) return;

            _shop.CaptureSave(_save);
            _shop.SetVisible(false);
            _center?.ApplyShopSave();
            _center?.SetVisible(true);
            State = GameFlowState.Center;
            Autosave(_save);
            _assemblyAudio?.PlayAssemblyMusic();
        }

        // ---------------- Info / Records ----------------

        /// <summary>
        /// Center's INFO button opens the dedicated help/records screen (#177). The live Center
        /// state is copied into the payload for display only; Info never writes the save.
        /// </summary>
        private void OnInfoRequested()
        {
            if (State != GameFlowState.Center || _center == null || _info == null) return;

            _center.CaptureSave(_save);
            _info.Initialize(_save);
            _center.SetVisible(false);
            _info.SetVisible(true);
            State = GameFlowState.Info;
        }

        /// <summary>Info's BACK returns to its opener without a save write; the screen is read-only.</summary>
        private void OnInfoClosed()
        {
            if (State != GameFlowState.Info || _info == null) return;

            _info.SetVisible(false);
            if (_infoReturnToMainMenu)
            {
                _infoReturnToMainMenu = false;
                _mainMenu?.SetVisible(true);
                _audio?.PlayMainMenuMusic();
                State = GameFlowState.MainMenu;
                return;
            }

            _center?.SetVisible(true);
            State = GameFlowState.Center;
        }

        // ---------------- Ready / Briefing ----------------

        /// <summary>
        /// Center's READY opens the mission handoff (#178). The live loadout is copied into
        /// the payload so the screen validates the same craft set the mission will use.
        /// </summary>
        private void OnReadyRequested()
        {
            if (State != GameFlowState.Center || _center == null || _ready == null) return;
            if (!_center.CanLaunch) return;

            _center.CaptureSave(_save);
            _ready.Initialize(_save, _center);
            _center.SetVisible(false);
            State = GameFlowState.Ready;
        }

        /// <summary>Ready's BACK cancels the handoff and returns to Center without launching.</summary>
        private void OnReadyClosed()
        {
            if (State != GameFlowState.Ready || _ready == null) return;

            _ready.SetVisible(false);
            _center?.SetVisible(true);
            State = GameFlowState.Center;
        }

        /// <summary>
        /// Ready's launch hands the captured save to the existing briefing path:
        /// talkBefore, the opening cinematic and then the mission.
        /// </summary>
        private void OnReadyLaunch()
        {
            if (State != GameFlowState.Ready || _ready == null) return;

            _ready.SetVisible(false);
            State = GameFlowState.Center;
            LaunchMission();
            if (State == GameFlowState.Center) _center?.SetVisible(true);
        }

        private void EnterCenter(bool firstBoot)
        {
            DisposeMission();
            _briefingTalkActive = false;
            _accumulator = 0f;
            _captainIndex = -1;
            _activeLoadout = null;
            _repair = null;
            _damagedParts.Clear();
            // The next launch resolves its level from the saved STORY index.
            _level = null;

            SetFlowVisible(false);
            _talkScreen?.SetVisible(false);
            _mainMenu?.SetVisible(false);
            _options?.SetVisible(false);
            _factory?.SetVisible(false);
            _shop?.SetVisible(false);
            _info?.SetVisible(false);
            _ready?.SetVisible(false);
            _results?.SetVisible(false);
            _repairScreen?.SetVisible(false);

            if (_center != null)
            {
                if (firstBoot) _center.Initialize(_save);
                else _center.ApplySave(_save);
                _center.SetVisible(true);
            }

            _centerStarted = true;
            _infoReturnToMainMenu = false;
            _assemblyAudio?.PlayAssemblyMusic();
            State = GameFlowState.Center;
        }

        // ---------------- Briefing ----------------

        /// <summary>Launches the mission for the current STORY index. The dedicated Ready screen (#178) calls it.</summary>
        public void LaunchMission()
        {
            if (State != GameFlowState.Center) return;

            _activeLoadout = MissionLoadout.Capture(
                _save, _center != null ? _center.FleetCount : (_save.blueprints?.Count ?? 0),
                _center != null ? (Func<int, bool>)_center.IsCraftValid : null);

            try
            {
                if (!TryResolveLevel()) return;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[GameFlow] Level load failed: {exception.Message}");
                return;
            }

            _center?.CaptureSave(_save);
            _center?.SetVisible(false);
            SetFlowVisible(false);
            _lastResult = null;

            _missionAudio?.PlayBriefingMusic();
            State = GameFlowState.Briefing;

            TalkScript before = ResolveCue(_story.ResolveTalkBefore);
            if (before != null && _talkScreen != null)
            {
                _briefingTalkActive = true;
                _talkScreen.SetVisible(true);
                _talkScreen.Begin(before);
                return;
            }

            StartCinematics();
        }

        /// <summary>
        /// Skips the remaining dialogue and the running cinematic. Used by the skip control and
        /// by the deterministic PlayMode smoke run.
        /// </summary>
        public void SkipBriefing()
        {
            if (State != GameFlowState.Briefing) return;

            int guard = 0;
            while (_briefingTalkActive && _talkScreen != null && !_talkScreen.IsComplete && guard < MaxTalkAdvancesPerSkip)
            {
                _talkScreen.Advance();
                guard++;
            }

            if (State != GameFlowState.Briefing) return;

            if (_cinematicPlayer != null && _cinematicPlayer.IsRunning)
            {
                _cinematicPlayer.Skip();
            }

            if (State == GameFlowState.Briefing && !_briefingTalkActive &&
                (_cinematicPlayer == null || !_cinematicPlayer.IsRunning))
            {
                StartMission();
            }
        }

        /// <summary>Forwards one talk advance; the talk screen also advances itself on click.</summary>
        public void AdvanceTalk()
        {
            _talkScreen?.Advance();
        }

        private void OnTalkCompleted()
        {
            _briefingTalkActive = false;

            if (State == GameFlowState.Briefing)
            {
                StartCinematics();
            }
            else if (State == GameFlowState.Result)
            {
                _talkScreen?.SetVisible(false);
            }
        }

        private void StartCinematics()
        {
            _briefingTalkActive = false;
            _talkScreen?.SetVisible(false);

            if (State != GameFlowState.Briefing) return;

            if (_cinematicPlayer == null || _briefingClips == null || _briefingClips.Length == 0)
            {
                OnCinematicsCompleted();
                return;
            }

            ShowCinematicSurface(true);
            _cinematicPlayer.Play(_briefingClips, OnCinematicsCompleted);
        }

        private void OnCinematicsCompleted()
        {
            ShowCinematicSurface(false);
            if (State == GameFlowState.Briefing) StartMission();
        }

        private TalkScript ResolveCue(Func<TalkScript, TalkScript> resolve)
        {
            try
            {
                if (_talkScript == null)
                {
                    TextAsset talkData = Resources.Load<TextAsset>("Talk/5_all.AllData_talk");
                    if (talkData == null) return null;
                    _talkScript = TalkScriptParser.ParseXml(talkData.text);
                }

                return resolve(_talkScript);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[GameFlow] Talk cue unavailable: {exception.Message}");
                return null;
            }
        }

        private bool TryResolveLevel()
        {
            if (_catalog == null)
            {
                TextAsset levelData = Resources.Load<TextAsset>(_levelResourcePath);
                if (levelData == null)
                {
                    Debug.LogError($"[GameFlow] Level payload missing from Resources/{_levelResourcePath}.");
                    return false;
                }

                _catalog = LevelDataParser.Parse(levelData.bytes);
            }

            // STORY advances after a win, so the level must be re-resolved on every launch.
            _level = _catalog.GetLevel(_save.storyIndex);
            return true;
        }

        /// <summary>
        /// Loads the frozen craft definitions once per campaign. The parser is pure C#;
        /// Resources carry the same four binaries the editor importer reads.
        /// </summary>
        private CraftCatalog ResolveCraftCatalog()
        {
            if (_craftCatalog != null) return _craftCatalog;

            try
            {
                TextAsset defFly = Resources.Load<TextAsset>(DefFlyResourcePath);
                TextAsset defPart = Resources.Load<TextAsset>(DefPartResourcePath);
                TextAsset defFire = Resources.Load<TextAsset>(DefFireResourcePath);
                TextAsset defBullet = Resources.Load<TextAsset>(DefBulletResourcePath);
                if (defFly == null || defPart == null || defFire == null || defBullet == null)
                {
                    Debug.LogWarning("[GameFlow] Craft definitions missing from Resources/Data; spawns use placeholder stats.");
                    return null;
                }

                _craftCatalog = CraftCatalogParser.Parse(defFly.bytes, defPart.text, defFire.text, defBullet.text);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[GameFlow] Craft definitions unavailable: {exception.Message}");
                _craftCatalog = null;
            }

            return _craftCatalog;
        }

        /// <summary>
        /// Loads the frozen H/F/L marker table once. It drives socket composition, muzzle
        /// points and engine exhaust points for every hull, player and AI alike.
        /// </summary>
        private PartMountTable ResolveMountTable()
        {
            if (_mountTable != null) return _mountTable;

            try
            {
                TextAsset table = Resources.Load<TextAsset>("Data/PartMounts");
                if (table == null)
                {
                    Debug.LogWarning("[GameFlow] Part mount table missing from Resources/Data; AI parts share the hull centre.");
                    return null;
                }

                _mountTable = PartMountTable.Parse(table.text);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[GameFlow] Part mount table unavailable: {exception.Message}");
                _mountTable = null;
            }

            return _mountTable;
        }

        // ---------------- Mission ----------------

        private void StartMission()
        {
            DisposeMission();

            _mountTable = ResolveMountTable();
            _simulation = new ScheduledCombatSimulation();
            _simulation.Init(_maxShips, _maxProjectiles);
            _runner = new MissionRunner(_level, _simulation, ResolveCraftCatalog());
            _runner.SpawnSkipped += OnSpawnSkipped;
            _runner.Start();
            _simulation.PlayerIndex = _runner.PlayerShipIndex;
            SpawnWingmen();

            TryCreateContactSolver();
            SetupCombat();

            _flight?.BeginMission(_simulation, _runner.PlayerShipIndex, _hud, _cameraRig);
            _spriteView?.SetWeapons(_weapons);
            _spriteView?.BeginMission(
                _simulation,
                _solver,
                _shipToHull,
                _runner.PlayerShipIndex,
                (_maxShips * _contactPartsPerHull) + _maxProjectiles);

            _captainIndex = FindCaptainIndex();
            _hud?.State.SetHullHealth(1f, 1f);
            _hud?.State.SetCaptainPresence(_captainIndex >= 0);
            _hud?.State.SetCaptainMode(CaptainTacticalMode.Anchor);
            if (_cameraRig != null) _hud?.State.SetCameraDistance(_cameraRig.Preset);
            FeedStaticHudValues();

            _accumulator = 0f;
            _damagedParts.Clear();
            _repair = null;

            ShowCinematicSurface(false);
            SetFlowVisible(true);

            _missionAudio?.PlayMissionMusic(_save.storyIndex);
            State = GameFlowState.Mission;
            UpdateHud();
        }

        private static void OnSpawnSkipped(string message)
        {
            Debug.LogWarning($"[GameFlow] {message}");
        }

        private void TryCreateContactSolver()
        {
            try
            {
                _solver = new ProjectileContactSolver(_maxShips, _contactPartsPerHull, _maxProjectiles);
            }
            catch (Exception exception)
            {
                // Missing PhysicsCore2D must not block the mission; projectile damage is optional here.
                Debug.LogWarning($"[GameFlow] Contact solver unavailable: {exception.Message}");
                _solver = null;
            }
        }

        /// <summary>
        /// Deploys the Ready screen's AI companions as friendly escort wingmen around the player.
        /// Each wingman keeps its Center blueprint; the hull defIndex only seeds movement stats.
        /// </summary>
        private void SpawnWingmen()
        {
            _wingmanShipIndices.Clear();
            _wingmanBlueprints.Clear();

            if (_activeLoadout == null || _runner == null || _simulation == null) return;
            if (_save.blueprints == null || _save.blueprints.Count == 0) return;

            int playerIndex = _runner.PlayerShipIndex;
            if (playerIndex < 0 || playerIndex >= _simulation.ShipCount) return;

            ShipEntity player = _simulation.GetShip(playerIndex);
            float2 forward = ShipEntity.Forward(player.rotation);
            float2 right = new float2(forward.y, -forward.x);
            IReadOnlyList<int> companions = _activeLoadout.CompanionCraftIndices;

            for (int slot = 0; slot < companions.Count; slot++)
            {
                int blueprintIndex = companions[slot];
                if (blueprintIndex < 0 || blueprintIndex >= _save.blueprints.Count) continue;

                BlueprintSaveData blueprint = _save.blueprints[blueprintIndex];
                if (blueprint == null) continue;

                string pilot = string.IsNullOrEmpty(blueprint.craftName)
                    ? "Wingman " + (slot + 1)
                    : blueprint.craftName;
                float2 position = player.position
                                  - (forward * 80f)
                                  + (right * (60f * (slot + 1)));
                int defIndex = ResolveBlueprintDefIndex(blueprint);
                int shipIndex = _runner.SpawnFriendlyWingman(
                    pilot, defIndex, position, player.rotation, slot + 1);
                if (shipIndex < 0) continue;

                _wingmanShipIndices.Add(shipIndex);
                _wingmanBlueprints[shipIndex] = blueprintIndex;
            }
        }

        /// <summary>Finds the frozen craft record whose chassis matches the blueprint body.</summary>
        private int ResolveBlueprintDefIndex(BlueprintSaveData blueprint)
        {
            if (_craftCatalog?.Records == null || blueprint?.mountedParts == null) return -1;

            int hullPartId = -1;
            for (int i = 0; i < blueprint.mountedParts.Count; i++)
            {
                MountedPartSaveData part = blueprint.mountedParts[i];
                if (part.category != PartCategory.Body) continue;
                hullPartId = part.partId;
                break;
            }

            if (hullPartId < 0) return -1;

            CraftStatRecord[] records = _craftCatalog.Records;
            for (int i = 0; i < records.Length; i++)
            {
                if (records[i].hullPartId == hullPartId) return records[i].defIndex;
            }

            return -1;
        }

        /// <summary>
        /// Registers every spawned hull with the contact solver and turns the player's assembled
        /// guns into fire ports. Setup only; the per-tick path below allocates nothing.
        /// </summary>
        private void SetupCombat()
        {
            _weapons = new WeaponSystem(_simulation, _simulation);
            _weapons.PlayerShipIndex = _runner.PlayerShipIndex;

            _hullToShip = new int[_simulation.MaxShips];
            _shipToHull = new int[_simulation.MaxShips];
            _hullPartBlueprint = new int[_simulation.MaxShips * _contactPartsPerHull];
            _hullPartMounted = new int[_simulation.MaxShips * _contactPartsPerHull];
            Array.Fill(_hullToShip, -1);
            Array.Fill(_shipToHull, -1);
            Array.Fill(_hullPartBlueprint, -1);
            Array.Fill(_hullPartMounted, -1);
            _registeredShipCount = 0;
            _playerHull = -1;
            _playerHullMax = 1f;

            RegisterSpawnedShips();
            _damageHandoff = new ProjectileDamageHandoff(_hullToShip, _playerHull, _playerHullMax);
        }

        /// <summary>Registers hulls for waves that spawned since the previous tick.</summary>
        private void RegisterSpawnedShips()
        {
            if (_solver == null) return;

            int shipCount = _simulation.ShipCount;
            for (int shipIndex = _registeredShipCount; shipIndex < shipCount; shipIndex++)
                RegisterShip(shipIndex);

            _registeredShipCount = shipCount;
        }

        private void RegisterShip(int shipIndex)
        {
            ShipEntity ship = _simulation.GetShip(shipIndex);
            bool isPlayer = shipIndex == _runner.PlayerShipIndex;
            int wingmanBlueprint = -1;
            bool isWingman = !isPlayer && _wingmanBlueprints.TryGetValue(shipIndex, out wingmanBlueprint);
            _runner.TryGetCraftStats(shipIndex, out CraftStatRecord craft);

            Span<HullPartDefinition> parts = stackalloc HullPartDefinition[_contactPartsPerHull];
            Span<CraftPartPlacement> placements = stackalloc CraftPartPlacement[_contactPartsPerHull];
            int partCount;
            int placementCount = 0;
            int hull = _solver.RegisterHull(ship.position, ship.rotation, ship.factionId);

            if (isPlayer || isWingman)
            {
                int blueprintIndex = isPlayer ? PlayerBlueprintIndex() : wingmanBlueprint;
                partCount = BuildBlueprintParts(parts, ship, blueprintIndex, hull);
            }
            else
            {
                placementCount = _mountTable != null
                    ? CraftComposition.Build(craft, _mountTable, placements)
                    : 0;
                partCount = BuildCraftParts(parts, craft, placements.Slice(0, placementCount));
            }

            if (partCount == 0)
                partCount = AddBodyPart(
                    parts, 0,
                    craft != null ? craft.health : ship.maxHealth,
                    float2.zero,
                    craft != null ? craft.hullPartId : 0);

            _solver.SetHullParts(hull, parts.Slice(0, partCount));
            _shipToHull[shipIndex] = hull;
            _hullToShip[hull] = shipIndex;

            if (isPlayer)
            {
                RegisterBlueprintWeapons(shipIndex, parts.Slice(0, partCount));
                _playerHull = hull;
                _playerHullMax = _solver.GetHullHealth(hull);
            }
            else if (isWingman)
            {
                RegisterBlueprintWeapons(shipIndex, parts.Slice(0, partCount));
            }
            else
            {
                RegisterCraftWeapons(shipIndex, craft, placements.Slice(0, placementCount));
            }
        }

        private int PlayerBlueprintIndex()
        {
            if (_save.blueprints == null || _save.blueprints.Count == 0) return -1;
            return _activeLoadout != null && _activeLoadout.PlayerCraftIndex >= 0
                ? _activeLoadout.PlayerCraftIndex
                : Math.Clamp(_save.playerBlueprintIndex, 0, _save.blueprints.Count - 1);
        }

        /// <summary>
        /// Composes one assembled blueprint through the shared Center/mission resolver:
        /// socket offsets, mirroring, and engine/hull/wing/gun layer order.
        /// </summary>
        private int BuildBlueprintParts(
            Span<HullPartDefinition> parts,
            in ShipEntity ship,
            int blueprintIndex,
            int hull)
        {
            if (_save.blueprints == null || blueprintIndex < 0 || blueprintIndex >= _save.blueprints.Count)
                return 0;

            BlueprintSaveData blueprint = _save.blueprints[blueprintIndex];
            if (blueprint?.mountedParts == null) return 0;

            Span<MountedPartData> mounted = stackalloc MountedPartData[_contactPartsPerHull];
            int mountedCount = 0;
            int bodyIndex = -1;
            for (int i = 0; i < blueprint.mountedParts.Count && mountedCount < mounted.Length; i++)
            {
                MountedPartData part = blueprint.mountedParts[i].ToMountedPart();
                part.mirrored = part.mirrored || IsMirrored(part.partId);
                if (part.category == PartCategory.Body) bodyIndex = mountedCount;
                mounted[mountedCount++] = part;
            }

            if (mountedCount == 0) return 0;

            bool syntheticBody = false;
            if (bodyIndex < 0)
            {
                // Center may save a gun-only blueprint; the chassis is still part of the craft.
                if (mountedCount >= mounted.Length) return 0;
                for (int i = mountedCount; i > 0; i--) mounted[i] = mounted[i - 1];
                float hullHealth = blueprint.hullHealth > 0f ? blueprint.hullHealth : ship.maxHealth;
                mounted[0] = new MountedPartData
                {
                    partId = 0,
                    category = PartCategory.Body,
                    health = hullHealth,
                    maxHealth = hullHealth,
                    mass = blueprint.hullMass
                };
                mountedCount++;
                syntheticBody = true;
            }

            Span<CraftPartPlacement> placements = stackalloc CraftPartPlacement[_contactPartsPerHull];
            int placementCount = CraftComposition.BuildPlayer(
                mounted.Slice(0, mountedCount), _mountTable, placements);

            int mapBase = hull * _contactPartsPerHull;
            int count = 0;
            for (int i = 0; i < placementCount && count < parts.Length; i++)
            {
                CraftPartPlacement placement = placements[i];
                if ((uint)placement.partIndex >= (uint)mountedCount) continue;

                MountedPartData part = mounted[placement.partIndex];
                part.localOffset = placement.localOffset * CameraDistancePresets.MountArtScale;
                part.localRotation = placement.localRotation;
                part.mirrored = placement.mirrored;
                int before = count;
                count = AddMountedPart(parts, count, in part);
                if (count == before) continue;

                if (_hullPartBlueprint != null && _hullPartMounted != null && mapBase + before < _hullPartBlueprint.Length)
                {
                    int mountedIndex = syntheticBody ? placement.partIndex - 1 : placement.partIndex;
                    _hullPartBlueprint[mapBase + before] = blueprintIndex;
                    _hullPartMounted[mapBase + before] = mountedIndex;
                }
            }

            return count;
        }

        private bool IsMirrored(int partId)
        {
            return _mountTable != null && _mountTable.TryGetPart(partId, out PartMountEntry entry) && entry.lr == 1;
        }

        private static int BuildCraftParts(
            Span<HullPartDefinition> parts,
            CraftStatRecord craft,
            ReadOnlySpan<CraftPartPlacement> placements)
        {
            if (craft == null || craft.parts == null) return 0;

            int count = 0;
            for (int i = 0; i < placements.Length && count < parts.Length; i++)
            {
                CraftPartPlacement placement = placements[i];
                if ((uint)placement.partIndex >= (uint)craft.parts.Length) continue;
                CraftPartRecord source = craft.parts[placement.partIndex];
                parts[count++] = new HullPartDefinition
                {
                    part = new MountedPartData
                    {
                        partId = source.partId,
                        category = source.category,
                        health = math.max(1f, source.health),
                        maxHealth = math.max(1f, source.health),
                        mass = source.mass,
                        thrust = source.thrust,
                        localOffset = placement.localOffset * CameraDistancePresets.MountArtScale,
                        localRotation = placement.localRotation,
                        mirrored = placement.mirrored
                    },
                    hitRadius = HitRadius(source.category)
                };
            }

            return count;
        }

        private static int AddMountedPart(Span<HullPartDefinition> parts, int count, in MountedPartData part)
        {
            if (count >= parts.Length) return count;

            MountedPartData mounted = part;
            mounted.health = math.max(1f, mounted.health);
            parts[count] = new HullPartDefinition
            {
                part = mounted,
                hitRadius = HitRadius(part.category)
            };
            return count + 1;
        }

        private static int AddBodyPart(
            Span<HullPartDefinition> parts,
            int count,
            float health,
            float2 offset,
            int partId = 0)
        {
            if (count >= parts.Length) return count;

            parts[count] = new HullPartDefinition
            {
                part = new MountedPartData
                {
                    partId = partId,
                    category = PartCategory.Body,
                    health = math.max(1f, health),
                    maxHealth = math.max(1f, health),
                    localOffset = offset
                },
                hitRadius = HitRadius(PartCategory.Body)
            };
            return count + 1;
        }

        /// <summary>
        /// Frozen collision radii in gameplay pixels. Mount art is drawn at 2x for 1080p
        /// readability, but hitboxes stay on ADR 0017's frozen gameplay scale so damage and
        /// balance are unchanged from the reference release.
        /// </summary>
        private static float HitRadius(PartCategory category)
        {
            return category switch
            {
                PartCategory.Body => 32f,
                PartCategory.Wing => 16f,
                PartCategory.Engine => 10f,
                PartCategory.Gun => 6f,
                _ => 10f
            };
        }

        /// <summary>Maps each assembled gun part to a fire port at its socket offset.</summary>
        private void RegisterBlueprintWeapons(int shipIndex, ReadOnlySpan<HullPartDefinition> parts)
        {
            if (_center == null) return;

            Span<WeaponMount> mounts = stackalloc WeaponMount[_contactPartsPerHull];
            Span<float2> muzzles = stackalloc float2[8];
            int mountCount = 0;

            for (int i = 0; i < parts.Length; i++)
            {
                MountedPartData part = parts[i].part;
                if (part.category != PartCategory.Gun) continue;

                PartDefinitionSO definition = _center.FindPartDefinition(part.partId);
                if (definition == null) continue;

                FireModeSpec spec = definition.CreateWeaponSpec();
                if (spec.lifetime <= 0f) continue;

                spec.projectileType = BulletId(part.partId);
                int muzzleCount = _mountTable != null ? _mountTable.CopyFirePoints(part.partId, muzzles) : 0;
                if (muzzleCount == 0)
                {
                    muzzles[0] = float2.zero;
                    muzzleCount = 1;
                }

                for (int muzzleIndex = 0; muzzleIndex < muzzleCount && mountCount < mounts.Length; muzzleIndex++)
                {
                    float2 local = muzzles[muzzleIndex] * CameraDistancePresets.MountArtScale;
                    if (part.mirrored) local.x = -local.x;

                    mounts[mountCount++] = new WeaponMount
                    {
                        spec = spec,
                        localOffset = part.localOffset,
                        localMuzzle = local,
                        localRotation = part.localRotation,
                        aimRotation = 0f,
                        partIndex = i
                    };
                }
            }

            if (mountCount > 0) _weapons.RegisterCraft(shipIndex, mounts.Slice(0, mountCount));
        }

        /// <summary>
        /// Registers every frozen gun socket on an AI craft at its composed F-marker muzzle.
        /// The Captain uses the same path in all six tactical modes (ADR 0006); hostiles use it
        /// after their pursuit steering.
        /// </summary>
        private void RegisterCraftWeapons(
            int shipIndex,
            CraftStatRecord craft,
            ReadOnlySpan<CraftPartPlacement> placements)
        {
            if (_center == null || craft == null || craft.weaponMounts == null) return;

            Span<WeaponMount> mounts = stackalloc WeaponMount[_contactPartsPerHull];
            Span<float2> muzzles = stackalloc float2[8];
            int mountCount = 0;

            for (int i = 0; i < craft.weaponMounts.Length && mountCount < mounts.Length; i++)
            {
                CraftWeaponMount mount = craft.weaponMounts[i];
                if ((uint)mount.partIndex >= (uint)placements.Length) continue;

                PartDefinitionSO definition = _center.FindPartDefinition(mount.partId);
                if (definition == null) continue;

                FireModeSpec spec = definition.CreateWeaponSpec();
                if (spec.lifetime <= 0f) continue;

                CraftPartPlacement placement = placements[mount.partIndex];
                int muzzleCount = _mountTable != null ? _mountTable.CopyFirePoints(mount.partId, muzzles) : 0;
                if (muzzleCount == 0)
                {
                    muzzles[0] = float2.zero;
                    muzzleCount = 1;
                }

                spec.projectileType = BulletId(mount.partId);

                for (int muzzleIndex = 0; muzzleIndex < muzzleCount && mountCount < mounts.Length; muzzleIndex++)
                {
                    float2 local = muzzles[muzzleIndex] * CameraDistancePresets.MountArtScale;
                    if (placement.mirrored) local.x = -local.x;

                    mounts[mountCount++] = new WeaponMount
                    {
                        spec = spec,
                        localOffset = placement.localOffset * CameraDistancePresets.MountArtScale,
                        localMuzzle = local,
                        localRotation = placement.localRotation,
                        aimRotation = 0f,
                        partIndex = mount.partIndex
                    };
                }
            }

            if (mountCount > 0) _weapons.RegisterCraft(shipIndex, mounts.Slice(0, mountCount), shipIndex == _captainIndex);
        }

        private int BulletId(int partId)
        {
            return _mountTable != null && _mountTable.TryGetPart(partId, out PartMountEntry entry) && entry.bulletId > 0
                ? entry.bulletId
                : 1;
        }

        private void SyncHullPoses()
        {
            if (_solver == null || _shipToHull == null) return;

            int shipCount = _simulation.ShipCount;
            for (int shipIndex = 0; shipIndex < shipCount; shipIndex++)
            {
                int hull = _shipToHull[shipIndex];
                if (hull < 0 || _solver.IsHullDestroyed(hull)) continue;

                ShipEntity ship = _simulation.GetShip(shipIndex);
                _solver.SetHullPose(hull, ship.position, ship.rotation);
            }
        }

        void ICraftDestroyedSink.CraftDestroyed(int shipIndex)
        {
            if (_simulation != null && shipIndex >= 0 && shipIndex < _simulation.ShipCount)
            {
                // Targeting reads health, so a destroyed craft must stop being a candidate.
                ShipEntity ship = _simulation.GetShip(shipIndex);
                ship.health = 0f;
                _simulation.SetShip(shipIndex, ship);
            }

            _runner?.NotifyCraftDestroyed(shipIndex);
        }

        private void TickMission(float deltaTime)
        {
            if (_runner == null || _simulation == null) return;

            _accumulator += deltaTime;
            float maxFrameDelta = FixedStepSeconds * MaxSubstepsPerFrame;
            if (_accumulator > maxFrameDelta) _accumulator = maxFrameDelta;

            while (_accumulator >= FixedStepSeconds)
            {
                _accumulator -= FixedStepSeconds;
                StepMission();
                if (TryConsumeMissionResult()) return;
            }

            UpdateHud();
            RenderMissionFrame(deltaTime);
        }

        /// <summary>
        /// Focuses the camera on the player and submits one BRG frame. Runs once per
        /// rendered frame, never per simulation substep.
        /// </summary>
        private void RenderMissionFrame(float deltaTime)
        {
            int playerIndex = _runner != null ? _runner.PlayerShipIndex : -1;
            if (_simulation != null && playerIndex >= 0 && playerIndex < _simulation.ShipCount)
            {
                ShipEntity player = _simulation.GetShip(playerIndex);
                _cameraRig?.Focus(new Vector2(player.position.x, player.position.y));
            }

            _spriteView?.Render(deltaTime);
        }

        private void StepMission()
        {
            _flight?.Tick(FixedStepSeconds);
            // Fire before the simulation step so new projectiles are integrated and swept this tick.
            TickPlayerWeapons();
            TickAiWeapons();
            _runner.Tick();
            RegisterSpawnedShips();
            SyncHullPoses();

            if (_solver == null) return;

            ContactStepResult contacts = _solver.Step(_simulation.Projectiles, FixedStepSeconds);
            _damageHandoff?.Apply(_solver, _hud?.State, this);

            // A destroyed hull unmounts its surviving parts in the solver; every mapped slot
            // still belongs in the repair ledger (RepairPartsCtrl.removeParts removes them all).
            var destroyed = _solver.DestroyedHulls;
            for (int i = 0; i < destroyed.Length; i++)
            {
                int hull = destroyed[i].hullIndex;
                int partCount = _solver.GetHullPartCount(hull);
                for (int part = 0; part < partCount; part++)
                    RecordDamagedPart(hull, part);
            }

            if (contacts.hitCount == 0) return;

            var hits = _solver.Hits;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].partDetached) RecordDamagedPart(hits[i]);
                _spriteView?.SpawnImpact(hits[i].point, hits[i].partDetached, hits[i].hullDestroyed);
                if (_combatAudio == null) continue;
                if (hits[i].hullDestroyed) _combatAudio.Explosion(1f);
                else if (hits[i].partDetached) _combatAudio.ComponentLost();
            }
        }

        /// <summary>
        /// Maps a detached hull part back to its saved blueprint slot. The player and every
        /// wingman are registered through <see cref="BuildBlueprintParts"/>; hostiles keep -1.
        /// </summary>
        private void RecordDamagedPart(in ProjectileHit hit)
        {
            RecordDamagedPart(hit.hullIndex, hit.partIndex);
        }

        private void RecordDamagedPart(int hullIndex, int partIndex)
        {
            if (_hullPartBlueprint == null || _hullPartMounted == null) return;
            if (hullIndex < 0 || hullIndex >= _maxShips) return;
            if (partIndex < 0 || partIndex >= _contactPartsPerHull) return;

            int slot = (hullIndex * _contactPartsPerHull) + partIndex;
            if (slot < 0 || slot >= _hullPartBlueprint.Length) return;

            int blueprintIndex = _hullPartBlueprint[slot];
            int mountedIndex = _hullPartMounted[slot];
            if (blueprintIndex < 0 || mountedIndex < 0) return;
            AddDamagedPart(blueprintIndex, mountedIndex);
        }

        private bool AddDamagedPart(int blueprintIndex, int mountedIndex)
        {
            if (_save?.blueprints == null) return false;
            if (blueprintIndex < 0 || blueprintIndex >= _save.blueprints.Count) return false;

            BlueprintSaveData blueprint = _save.blueprints[blueprintIndex];
            if (blueprint?.mountedParts == null) return false;
            if (mountedIndex < 0 || mountedIndex >= blueprint.mountedParts.Count) return false;

            for (int i = 0; i < _damagedParts.Count; i++)
            {
                DamagedPart recorded = _damagedParts[i];
                if (recorded.blueprintIndex == blueprintIndex && recorded.mountedIndex == mountedIndex)
                    return false;
            }

            MountedPartSaveData part = blueprint.mountedParts[mountedIndex];
            _damagedParts.Add(new DamagedPart(blueprintIndex, mountedIndex, part.partId, part.category));
            return true;
        }

        /// <summary>
        /// Deterministic verification hook: records a mounted part as destroyed without a
        /// projectile so the result/repair flow can be exercised at 1920x1080.
        /// </summary>
        public bool MarkDamagedPartForVerification(int blueprintIndex, int mountedIndex)
        {
            return AddDamagedPart(blueprintIndex, mountedIndex);
        }

        /// <summary>
        /// Player fire ports aim independently of the hull: keyboard mode tracks the mouse,
        /// mouse mode auto-targets the closest hostile inside the highest-range mounted weapon.
        /// No managed allocation.
        /// </summary>
        private void TickPlayerWeapons()
        {
            if (_weapons == null || _combatInput == null) return;

            int playerIndex = _runner != null ? _runner.PlayerShipIndex : _weapons.PlayerShipIndex;
            if (playerIndex < 0 || _flight == null)
            {
                _weapons.Tick(_combatInput);
                return;
            }

            bool mouseMode = _flight.ControlMode == PlayerControlMode.Mouse;
            bool requireRange = mouseMode;
            float2 aimPoint = _flight.CurrentAimPoint;
            if (mouseMode && _weapons.TryFindAutoTarget(playerIndex, out ShipEntity target))
                aimPoint = target.position;

            _weapons.TickAim(
                playerIndex, _combatInput.FireHeld, _combatInput.Autofire, aimPoint, requireRange);
        }

        /// <summary>
        /// Fires every living AI craft at the nearest enemy selected by the same rule as
        /// <see cref="TacticalAIJob"/>. No managed allocation.
        /// </summary>
        private void TickAiWeapons()
        {
            if (_weapons == null || _simulation == null || _runner == null) return;

            int shipCount = _simulation.ShipCount;
            int playerIndex = _runner.PlayerShipIndex;
            for (int shipIndex = 0; shipIndex < shipCount; shipIndex++)
            {
                if (shipIndex == playerIndex) continue;
                if (!_runner.IsCraftAlive(shipIndex)) continue;
                if (!_simulation.TryFindNearestHostile(shipIndex, out ShipEntity target)) continue;

                _weapons.TickAi(shipIndex, target.position);
            }
        }

        private bool TryConsumeMissionResult()
        {
            if (_runner == null) return false;
            if (!_runner.TryConsumeReturnToHangar(out MissionResult result)) return false;

            HandleMissionEnd(result);
            return true;
        }

        private void HandleMissionEnd(in MissionResult result)
        {
            _lastResult = result;
            _moneyBeforeResult = _save.shop != null ? _save.shop.money : 0;
            _lastSummary = MissionSummary.FromResult(result, _save.storyIndex, _damagedParts.Count);

            TalkScript after = null;
            if (result.state == MissionState.Win)
            {
                after = ResolveCue(_story.ResolveTalkAfter);
                // UIWin constructor: pay the mission reward, then autoSaveMission before STORY advances.
                if (_save.shop != null) _save.shop.money = _moneyBeforeResult + _lastSummary.reward;
                _story.CompleteMission(result);
                _repair = RepairState.Create(_save, _damagedParts, ResolveRepairInfo);
                _missionAudio?.MissionWon();
            }
            else
            {
                _repair = null;
                _missionAudio?.MissionLost();
            }

            DisposeMission();
            // The combat HUD document is the flow overlay; the dedicated result surface replaces it.
            SetFlowVisible(false);
            _repairScreen?.SetVisible(false);
            _results?.Initialize(_lastSummary);

            if (after != null && _talkScreen != null)
            {
                _talkScreen.SetVisible(true);
                _talkScreen.Begin(after);
            }

            State = result.state == MissionState.Win ? GameFlowState.Result : GameFlowState.Fail;
        }

        private RepairPartInfo ResolveRepairInfo(int partId)
        {
            PartDefinitionSO definition = _center != null ? _center.FindPartDefinition(partId) : null;
            if (definition == null) return default;

            float firePower = definition.category == PartCategory.Gun
                ? definition.CalculateDisplayFirePower(1)
                : 0f;
            return new RepairPartInfo(
                definition.partName,
                definition.category,
                definition.lr,
                definition.health,
                definition.mass,
                definition.thrust,
                definition.turnFly,
                firePower,
                definition.cost);
        }

        /// <summary>
        /// Ends the result hold immediately: win Next, or the loss Continue, both return to Center.
        /// Kept as the deterministic PlayMode seam used by the existing flow tests.
        /// </summary>
        public void SkipResult()
        {
            if (State == GameFlowState.Result) OnResultNext();
            else if (State == GameFlowState.Fail) OnResultContinue();
        }

        private void OnResultNext()
        {
            if (State != GameFlowState.Result) return;

            // UIWin.toNext: unrepaired parts leave their craft and become shop stock.
            _repair?.SkipRemaining();
            _repair = null;
            _results?.SetVisible(false);
            // UIWin.nextGame: STORY advances and the payload is saved at the new index.
            _story.AdvanceStory();
            State = GameFlowState.Return;
        }

        private void OnResultRetry()
        {
            if (State != GameFlowState.Result && State != GameFlowState.Fail) return;

            // UIWin.retry restores the purse captured before the reward. A repair already
            // accepted stays paid so Retry cannot refund it and keep the repaired craft.
            if (State == GameFlowState.Result && (_repair == null || _repair.RepairedCount == 0))
            {
                _save.shop.money = _moneyBeforeResult;
                // Persist the refund while the payload still owns the fleet.
                Autosave(_save);
            }

            _repair = null;
            _damagedParts.Clear();
            _results?.SetVisible(false);
            State = GameFlowState.Return;
        }

        private void OnResultContinue()
        {
            if (State != GameFlowState.Fail) return;

            _repair = null;
            _damagedParts.Clear();
            _results?.SetVisible(false);
            State = GameFlowState.Return;
        }

        private void OnResultRepair()
        {
            if (State != GameFlowState.Result || _repair == null || !_repair.HasDamage) return;
            if (_repairScreen == null) return;

            _results?.SetVisible(false);
            _repairScreen.Initialize(_save, _repair);
            State = GameFlowState.Repair;
        }

        private void OnRepairChanged()
        {
            if (State != GameFlowState.Repair) return;
            Autosave(_save);
            _lastSummary = _lastSummary.WithDamagedParts(_repair != null ? _repair.Items.Count : 0);
        }

        private void OnRepairClosed()
        {
            if (State != GameFlowState.Repair) return;

            _repairScreen?.SetVisible(false);
            _lastSummary = _lastSummary.WithDamagedParts(_repair != null ? _repair.Items.Count : 0);
            _results?.Initialize(_lastSummary);
            State = GameFlowState.Result;
        }

        /// <summary>
        /// Cycles the live Captain's ADR 0006 tactical mode. Returns false while no Captain
        /// is deployed or his craft has been destroyed, in which case E does nothing.
        /// </summary>
        public bool CycleCaptainTacticalMode()
        {
            if (State != GameFlowState.Mission || _runner == null || _simulation == null) return false;
            if (_captainIndex < 0 || _captainIndex >= _simulation.ShipCount) return false;
            if (!_runner.IsCraftAlive(_captainIndex)) return false;

            ShipEntity captain = _simulation.GetShip(_captainIndex);
            int nextMode = ((int)(CaptainTacticalMode)captain.tacticalMode + 1) % TacticalModeCount;
            var mode = (CaptainTacticalMode)nextMode;

            captain.tacticalMode = nextMode;
            _simulation.SetShip(_captainIndex, captain);
            _hud?.State.SetCaptainMode(mode);
            _combatAudio?.TacticalModeChanged();
            return true;
        }

        private void OnTacticalModeRequested()
        {
            CycleCaptainTacticalMode();
        }

        private void OnCameraDistanceChanged(CameraDistancePreset preset)
        {
            _save.cameraPreset = preset;
            Autosave(_save);
        }

        private void OnControlModeChanged(PlayerControlMode mode)
        {
            _save.controlMode = mode;
            Autosave(_save);
        }

        /// <summary>
        /// Deterministic verification hook: flies the player east like the Tier-1 replay and
        /// fast-forwards until the mission's own win objective fires. Never fabricates a result.
        /// </summary>
        public bool ForceWin()
        {
            if (State != GameFlowState.Mission || _runner == null || _simulation == null) return false;

            int playerIndex = _runner.PlayerShipIndex;
            if (playerIndex >= 0 && playerIndex < _simulation.ShipCount)
            {
                var player = _simulation.GetShip(playerIndex);
                player.rotation = -math.PI * 0.5f;
                _simulation.SetShip(playerIndex, player);

                var flightInput = new ShipInput { throttle = 1f, steering = 0f };
                if (_flight != null) _flight.BeginDeterministicFlight(in flightInput);
                else _simulation.SetInput(playerIndex, in flightInput);
            }

            _accumulator = 0f;
            try
            {
                for (int i = 0; i < ForceWinTickLimit && _runner.State == MissionState.InProgress; i++)
                {
                    StepMission();
                    if (TryConsumeMissionResult()) return true;
                }

                return _runner.State != MissionState.InProgress;
            }
            finally
            {
                _flight?.EndDeterministicFlight();
            }
        }

        private void UpdateHud()
        {
            if (_hud == null || _runner == null || _simulation == null) return;

            CombatHudState state = _hud.State;
            state.SetMoney(_save != null ? _save.shop.money : 0);
            state.SetMissionSeconds(_runner.CurrentTick * _runner.TickSeconds);
            state.SetAiCount(CountLivingWingmen());
            state.SetEnemyDistance(0f);

            int playerIndex = _runner.PlayerShipIndex;
            if (playerIndex >= 0 && playerIndex < _simulation.ShipCount)
            {
                if (_solver != null && _playerHull >= 0 && _playerHull < _solver.HullCount)
                {
                    state.SetHullHealth(_solver.GetHullHealth(_playerHull), _playerHullMax);
                }
                else
                {
                    var player = _simulation.GetShip(playerIndex);
                    state.SetHullHealth(player.health, player.maxHealth);
                }

                ShipEntity ship = _simulation.GetShip(playerIndex);
                state.SetFuelFraction(ship.BoosterFraction);
                state.SetRadarBearing(math.degrees(-ship.rotation));
                CountLivingHostiles(playerIndex, ship.position, out _, out float nearest);
                state.SetEnemyDistance(nearest);
            }

            if (_captainIndex >= 0 && _captainIndex < _simulation.ShipCount)
            {
                var captain = _simulation.GetShip(_captainIndex);
                state.SetCaptainPresence(true);
                state.SetCaptainMode((CaptainTacticalMode)captain.tacticalMode);
                state.SetCaptainHealth(captain.health, captain.maxHealth);
            }
            else
            {
                state.SetCaptainPresence(false);
            }
        }

        /// <summary>
        /// Values that only change between missions: credits from the save and the pilot
        /// name resolved from the level's player spawn.
        /// </summary>
        private void FeedStaticHudValues()
        {
            if (_hud == null) return;

            CombatHudState state = _hud.State;
            state.SetMoney(_save != null ? _save.shop.money : 0);
            state.SetPilotName(PlayerPilotName());
            state.SetMissionSeconds(0f);
            state.SetAiCount(0);
            state.SetEnemyDistance(0f);
            state.SetFuelFraction(1f);
            state.SetCaptainHealth(1f, 1f);
            state.SetRadarBearing(0f);
        }

        private string PlayerPilotName()
        {
            if (_runner == null) return string.Empty;

            var spawns = _runner.Spawns;
            for (int i = 0; i < spawns.Count; i++)
            {
                if (spawns[i].shipIndex != _runner.PlayerShipIndex) continue;
                if (!string.IsNullOrEmpty(spawns[i].pilot)) return spawns[i].pilot;
            }

            return string.Empty;
        }

        /// <summary>Living AI wingmen from the Ready loadout, matching the frozen AI x N plate.</summary>
        private int CountLivingWingmen()
        {
            if (_runner == null) return 0;

            int count = 0;
            for (int i = 0; i < _wingmanShipIndices.Count; i++)
            {
                if (_runner.IsCraftAlive(_wingmanShipIndices[i])) count++;
            }

            return count;
        }

        /// <summary>Living hostile count and the distance to the nearest one, in world pixels.</summary>
        private void CountLivingHostiles(int playerIndex, float2 playerPosition, out int count, out float nearest)
        {
            count = 0;
            nearest = 0f;

            int shipCount = _simulation.ShipCount;
            for (int shipIndex = 0; shipIndex < shipCount; shipIndex++)
            {
                if (shipIndex == playerIndex) continue;
                if (!_runner.IsCraftAlive(shipIndex)) continue;
                if (_simulation.GetShip(shipIndex).factionId == 0) continue;

                count++;
                float distance = math.distance(playerPosition, _simulation.GetShip(shipIndex).position);
                if (nearest <= 0f || distance < nearest) nearest = distance;
            }
        }

        /// <summary>
        /// Frozen <c>fuel</c> pickup path: refills the player booster tank and reports it.
        /// The item entity layer is not modelled yet, so the level/item system calls this seam.
        /// </summary>
        public bool CollectFuelPickup(float amount = ShipEntity.FuelPickupAmount)
        {
            if (_simulation == null || _runner == null) return false;

            int playerIndex = _runner.PlayerShipIndex;
            if (playerIndex < 0 || playerIndex >= _simulation.ShipCount) return false;

            ShipEntity ship = _simulation.GetShip(playerIndex);
            float before = ship.boosterFuel;
            ship.RefuelBooster(amount);
            if (ship.boosterFuel == before) return false;

            _simulation.SetShip(playerIndex, ship);
            _hud?.State.SetFuelFraction(ship.BoosterFraction);
            _combatAudio?.PickupCollected(PickupKind.Fuel);
            return true;
        }

        private static float Fraction(float current, float max)
        {
            return max > 0f ? math.clamp(current / max, 0f, 1f) : 1f;
        }

        private int FindCaptainIndex()
        {
            int playerIndex = _runner.PlayerShipIndex;
            var spawns = _runner.Spawns;
            for (int i = 0; i < spawns.Count; i++)
            {
                MissionSpawnRecord spawn = spawns[i];
                if (spawn.friendly && spawn.shipIndex >= 0 && spawn.shipIndex != playerIndex)
                    return spawn.shipIndex;
            }

            return -1;
        }

        private void DisposeMission()
        {
            _flight?.EndMission();
            _spriteView?.EndMission();
            _runner = null;
            _weapons = null;
            _damageHandoff = null;
            _hullToShip = null;
            _shipToHull = null;
            _registeredShipCount = 0;
            _playerHull = -1;
            _playerHullMax = 1f;
            _wingmanShipIndices.Clear();
            _wingmanBlueprints.Clear();

            if (_solver != null)
            {
                _solver.Dispose();
                _solver = null;
            }

            if (_simulation != null)
            {
                _simulation.Dispose();
                _simulation = null;
            }
        }

        // ---------------- Overlay ----------------

        private void EnsureOverlayReferences()
        {
            if (_flowDocument != null && _flowDocument.rootVisualElement != null)
            {
                _cinematicSurface ??= _flowDocument.rootVisualElement.Q<VisualElement>("flow-cinematic-surface");

                // The frozen UIMovie overlay skipped on click; the revival clip has no
                // separate control, so the whole movie surface is the skip target.
                if (_cinematicSurface != null && !_cinematicSkipBound)
                {
                    _cinematicSurface.RegisterCallback<ClickEvent>(OnCinematicSurfaceClicked);
                    _cinematicSkipBound = true;
                }
            }
        }

        private void OnCinematicSurfaceClicked(ClickEvent clickEvent)
        {
            SkipBriefing();
        }

        private void SetFlowVisible(bool visible)
        {
            EnsureOverlayReferences();
            if (_flowDocument == null || _flowDocument.rootVisualElement == null) return;
            _flowDocument.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ShowCinematicSurface(bool visible)
        {
            EnsureOverlayReferences();
            if (visible)
            {
                EnsureCinematicRenderTexture();
                SetFlowVisible(true);
            }

            if (_cinematicSurface != null)
            {
                _cinematicSurface.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void EnsureCinematicRenderTexture()
        {
            if (_renderTexture != null) return;

            _renderTexture = new RenderTexture(1920, 1080, 0);
            if (_cinematicSurface != null)
            {
                _cinematicSurface.style.backgroundImage = Background.FromRenderTexture(_renderTexture);
            }

            _cinematicPlayer?.SetTargetTexture(_renderTexture);
        }
    }

    /// <summary>Game-flow states. Mission and Result can span multiple frames; the rest are transitions.</summary>
    public enum GameFlowState
    {
        Boot = 0,
        Center = 1,
        Briefing = 2,
        Mission = 3,
        Result = 4,
        Return = 5,
        Factory = 6,
        Shop = 7,
        Info = 8,
        Ready = 9,
        MainMenu = 10,
        Options = 11,
        Fail = 12,
        Repair = 13
    }
}
