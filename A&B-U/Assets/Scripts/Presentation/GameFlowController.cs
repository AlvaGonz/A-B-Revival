using System;
using System.IO;
using ABRevival.Core.Data;
using ABRevival.Core.Hud;
using ABRevival.Core.Persistence;
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
    /// Story-0 game loop seam: Boot -> Hangar -> Briefing -> Mission -> Result -> Return.
    /// Owns the single player save, drives the assembly/shop presenters through
    /// <see cref="HangarController"/>, resolves story cues, steps the Burst-scheduled
    /// simulation with the PhysicsCore2D contact layer, feeds the HUD, consumes the
    /// mission result exactly once and advances STORY on a win.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameFlowController : MonoBehaviour
    {
        public const string DefaultSaveFileName = "player-save.json";
        public const float FixedStepSeconds = MissionRunner.DefaultTickSeconds;
        private const int ForceWinTickLimit = 10000;
        private const int MaxSubstepsPerFrame = 4;
        private const int MaxTalkAdvancesPerSkip = 1024;

        [SerializeField] private HangarController _hangar;
        [SerializeField] private TalkScreen _talkScreen;
        [SerializeField] private CinematicPlayer _cinematicPlayer;
        [SerializeField] private CombatHudBehaviour _hud;
        [SerializeField] private CombatCameraRig _cameraRig;
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
        private LevelRecord _level;
        private string _savePath;
        private bool _hasSave;

        private ScheduledCombatSimulation _simulation;
        private MissionRunner _runner;
        private ProjectileContactSolver _solver;
        private MissionResult? _lastResult;

        private IMissionAudio _missionAudio;
        private IAssemblyAudio _assemblyAudio;
        private ICombatAudio _combatAudio;

        private Label _resultLabel;
        private VisualElement _cinematicSurface;
        private RenderTexture _renderTexture;

        private float _accumulator;
        private int _captainIndex = -1;
        private bool _briefingTalkActive;
        private bool _resultTalkPending;

        public GameFlowState State { get; private set; } = GameFlowState.Boot;
        public int StoryIndex => _save != null ? _save.storyIndex : 0;
        public MissionRunner Runner => _runner;
        public ScheduledCombatSimulation Simulation => _simulation;
        public PlayerFlightController Flight => _flight;
        public ProjectileContactSolver ContactSolver => _solver;
        public MissionResult? LastResult => _lastResult;
        public string SavePath => _savePath;

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

            if (_hangar != null) _hangar.LaunchRequested += LaunchMission;
            if (_talkScreen != null)
            {
                _talkScreen.Completed += OnTalkCompleted;
                if (_audio != null) _talkScreen.Audio = _audio;
            }
        }

        private void Start()
        {
            EnsureOverlayReferences();
            EnterHangar(firstBoot: true);
        }

        private void OnDestroy()
        {
            if (_hangar != null) _hangar.LaunchRequested -= LaunchMission;
            if (_talkScreen != null) _talkScreen.Completed -= OnTalkCompleted;

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
                case GameFlowState.Result:
                    if (!_resultTalkPending) State = GameFlowState.Return;
                    break;
                case GameFlowState.Return:
                    EnterHangar(firstBoot: false);
                    break;
            }
        }

        // ---------------- Boot / hangar ----------------

        private void LoadSave()
        {
            _save = new PlayerSaveData();
            try
            {
                if (File.Exists(_savePath))
                {
                    _save = PlayerSaveCodec.LoadFromFile(_savePath);
                    _hasSave = true;
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[GameFlow] Save load failed, starting a new campaign: {exception.Message}");
                _save = new PlayerSaveData();
                _hasSave = false;
            }

            _story = new StoryDirector(_save, Autosave);
        }

        private void Autosave(PlayerSaveData payload)
        {
            try
            {
                _hangar?.CaptureSave(payload);
                PlayerSaveCodec.SaveToFile(_savePath, payload);
                _hasSave = true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[GameFlow] Autosave failed: {exception.Message}");
            }
        }

        private void EnterHangar(bool firstBoot)
        {
            DisposeMission();
            _briefingTalkActive = false;
            _resultTalkPending = false;
            _accumulator = 0f;
            _captainIndex = -1;

            SetFlowVisible(false);
            _talkScreen?.SetVisible(false);
            if (_resultLabel != null) _resultLabel.style.display = DisplayStyle.None;

            if (_hangar != null)
            {
                if (firstBoot) _hangar.Initialize(_hasSave ? _save : null);
                else _hangar.SetStory(_save.storyIndex);
                _hangar.SetVisible(true);
            }

            _assemblyAudio?.PlayAssemblyMusic();
            State = GameFlowState.Hangar;
        }

        // ---------------- Briefing ----------------

        /// <summary>Launches the mission for the current STORY index. Also the hangar button handler.</summary>
        public void LaunchMission()
        {
            if (State != GameFlowState.Hangar) return;

            try
            {
                if (!TryResolveLevel()) return;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[GameFlow] Level load failed: {exception.Message}");
                return;
            }

            _hangar?.CaptureSave(_save);
            _hangar?.SetVisible(false);
            SetFlowVisible(false);
            _resultTalkPending = false;
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
                _resultTalkPending = false;
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
            if (_level != null) return true;

            TextAsset levelData = Resources.Load<TextAsset>(_levelResourcePath);
            if (levelData == null)
            {
                Debug.LogError($"[GameFlow] Level payload missing from Resources/{_levelResourcePath}.");
                return false;
            }

            _catalog = LevelDataParser.Parse(levelData.bytes);
            _level = _catalog.GetLevel(_save.storyIndex);
            return true;
        }

        // ---------------- Mission ----------------

        private void StartMission()
        {
            DisposeMission();

            _simulation = new ScheduledCombatSimulation();
            _simulation.Init(_maxShips, _maxProjectiles);
            _runner = new MissionRunner(_level, _simulation);
            _runner.Start();
            _simulation.PlayerIndex = _runner.PlayerShipIndex;

            TryCreateContactSolver();

            _flight?.BeginMission(_simulation, _runner.PlayerShipIndex, _hud, _cameraRig);

            _captainIndex = FindCaptainIndex();
            _hud?.State.SetHullHealth(1f, 1f);
            _hud?.State.SetCaptainPresence(_captainIndex >= 0);
            _hud?.State.SetCaptainMode(CaptainTacticalMode.Anchor);
            if (_cameraRig != null) _hud?.State.SetCameraDistance(_cameraRig.Preset);

            _accumulator = 0f;
            if (_resultLabel != null) _resultLabel.style.display = DisplayStyle.None;

            ShowCinematicSurface(false);
            SetFlowVisible(true);

            _missionAudio?.PlayMissionMusic(_save.storyIndex);
            State = GameFlowState.Mission;
            UpdateHud();
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
        }

        private void StepMission()
        {
            _flight?.Tick(FixedStepSeconds);
            _runner.Tick();

            if (_solver == null) return;

            ContactStepResult contacts = _solver.Step(_simulation.Projectiles, FixedStepSeconds);
            if (contacts.hitCount == 0 || _combatAudio == null) return;

            var hits = _solver.Hits;
            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i].hullDestroyed) _combatAudio.Explosion(1f);
                else if (hits[i].partDetached) _combatAudio.ComponentLost();
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
            EnsureOverlayReferences();
            _lastResult = result;
            _resultTalkPending = false;

            TalkScript after = null;
            if (result.state == MissionState.Win)
            {
                after = ResolveCue(_story.ResolveTalkAfter);
                // Applies the win once: STORY advances and the two Flash autosave points fire.
                _story.ApplyMissionResult(result);
                _missionAudio?.MissionWon();
            }
            else
            {
                _missionAudio?.MissionLost();
            }

            DisposeMission();

            if (after != null && _talkScreen != null)
            {
                _resultTalkPending = true;
                _talkScreen.SetVisible(true);
                _talkScreen.Begin(after);
            }

            if (_resultLabel != null)
            {
                _resultLabel.text = result.state == MissionState.Win ? "MISSION COMPLETE" : "MISSION FAILED";
                _resultLabel.style.display = DisplayStyle.Flex;
            }

            State = GameFlowState.Result;
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

            int playerIndex = _runner.PlayerShipIndex;
            if (playerIndex >= 0 && playerIndex < _simulation.ShipCount)
            {
                var player = _simulation.GetShip(playerIndex);
                _hud.State.SetHullHealth(player.health, player.maxHealth);
            }

            if (_captainIndex >= 0 && _captainIndex < _simulation.ShipCount)
            {
                var captain = _simulation.GetShip(_captainIndex);
                _hud.State.SetCaptainPresence(true);
                _hud.State.SetCaptainMode((CaptainTacticalMode)captain.tacticalMode);
            }
            else
            {
                _hud.State.SetCaptainPresence(false);
            }
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
            _runner = null;
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
                _resultLabel ??= _flowDocument.rootVisualElement.Q<Label>("flow-result-label");
                _cinematicSurface ??= _flowDocument.rootVisualElement.Q<VisualElement>("flow-cinematic-surface");
            }
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
        Hangar = 1,
        Briefing = 2,
        Mission = 3,
        Result = 4,
        Return = 5
    }
}
