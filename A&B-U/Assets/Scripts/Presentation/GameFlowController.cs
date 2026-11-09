using System;
using System.IO;
using ABRevival.Core.Camera;
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
    /// Story-0 game loop seam: Boot -> Center -> Briefing -> Mission -> Result -> Return.
    /// Owns the single player save, drives <see cref="CenterController"/>, resolves story cues,
    /// steps the Burst-scheduled simulation with the PhysicsCore2D contact layer, feeds the HUD,
    /// consumes the mission result exactly once and advances STORY on a win.
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
        public const float ResultDwellSeconds = 1.5f;

        private const string DefFlyResourcePath = "Data/3_all.AllData_defFly";
        private const string DefPartResourcePath = "Data/8_all.AllData_defPart";
        private const string DefFireResourcePath = "Data/2_all.AllData_defFire";
        private const string DefBulletResourcePath = "Data/11_all.AllData_defBullet";

        [SerializeField] private CenterController _center;
        [SerializeField] private FactoryController _factory;
        [SerializeField] private ShopController _shop;
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

        private IMissionAudio _missionAudio;
        private IAssemblyAudio _assemblyAudio;
        private ICombatAudio _combatAudio;

        private Label _resultLabel;
        private VisualElement _cinematicSurface;
        private RenderTexture _renderTexture;

        private float _accumulator;
        private float _resultElapsed;
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

        /// <summary>Projectiles fired by the player craft this mission (real setup path telemetry).</summary>
        public int ShotsFired => _weapons != null ? _weapons.ShotsFired : 0;

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

            if (_center != null) _center.Changed += OnCenterChanged;
            if (_center != null) _center.FactoryRequested += OnFactoryRequested;
            if (_center != null) _center.ShopRequested += OnShopRequested;
            if (_factory != null) _factory.Changed += OnCenterChanged;
            if (_factory != null) _factory.Closed += OnFactoryClosed;
            if (_shop != null) _shop.Changed += OnCenterChanged;
            if (_shop != null) _shop.Closed += OnShopClosed;
            if (_talkScreen != null)
            {
                _talkScreen.Completed += OnTalkCompleted;
                if (_audio != null) _talkScreen.Audio = _audio;
            }
            if (_flight != null)
            {
                _flight.TacticalModeRequested += OnTacticalModeRequested;
                _flight.CameraDistanceChanged += OnCameraDistanceChanged;
            }
        }

        private void Start()
        {
            EnsureOverlayReferences();
            EnterCenter(firstBoot: true);
        }

        private void OnDestroy()
        {
            if (_center != null) _center.Changed -= OnCenterChanged;
            if (_center != null) _center.FactoryRequested -= OnFactoryRequested;
            if (_center != null) _center.ShopRequested -= OnShopRequested;
            if (_factory != null) _factory.Changed -= OnCenterChanged;
            if (_factory != null) _factory.Closed -= OnFactoryClosed;
            if (_shop != null) _shop.Changed -= OnCenterChanged;
            if (_shop != null) _shop.Closed -= OnShopClosed;
            if (_talkScreen != null) _talkScreen.Completed -= OnTalkCompleted;
            if (_flight != null)
            {
                _flight.TacticalModeRequested -= OnTacticalModeRequested;
                _flight.CameraDistanceChanged -= OnCameraDistanceChanged;
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
                case GameFlowState.Result:
                    // Hold the outcome long enough to be read; a loss must be observable.
                    _resultElapsed += deltaTime;
                    if (!_resultTalkPending && _resultElapsed >= ResultDwellSeconds)
                        State = GameFlowState.Return;
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
        }

        private void Autosave(PlayerSaveData payload)
        {
            try
            {
                // Only the active screen owns the live fleet; capturing the other one would
                // write a stale blueprint list over the edit in progress.
                if (State == GameFlowState.Factory) _factory?.CaptureSave(payload);
                else if (State == GameFlowState.Shop) _shop?.CaptureSave(payload);
                else _center?.CaptureSave(payload);
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

        private void EnterCenter(bool firstBoot)
        {
            DisposeMission();
            _briefingTalkActive = false;
            _resultTalkPending = false;
            _resultElapsed = 0f;
            _accumulator = 0f;
            _captainIndex = -1;
            // The next launch resolves its level from the saved STORY index.
            _level = null;

            SetFlowVisible(false);
            _talkScreen?.SetVisible(false);
            _factory?.SetVisible(false);
            _shop?.SetVisible(false);
            if (_resultLabel != null) _resultLabel.style.display = DisplayStyle.None;

            if (_center != null)
            {
                if (firstBoot) _center.Initialize(_save);
                else _center.SetStory(_save.storyIndex);
                _center.SetVisible(true);
            }

            _assemblyAudio?.PlayAssemblyMusic();
            State = GameFlowState.Center;
        }

        // ---------------- Briefing ----------------

        /// <summary>Launches the mission for the current STORY index. The dedicated Ready screen (#178) calls it.</summary>
        public void LaunchMission()
        {
            if (State != GameFlowState.Center) return;

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

            TryCreateContactSolver();
            SetupCombat();

            _flight?.BeginMission(_simulation, _runner.PlayerShipIndex, _hud, _cameraRig);
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
            _resultElapsed = 0f;
            if (_resultLabel != null) _resultLabel.style.display = DisplayStyle.None;

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
        /// Registers every spawned hull with the contact solver and turns the player's assembled
        /// guns into fire ports. Setup only; the per-tick path below allocates nothing.
        /// </summary>
        private void SetupCombat()
        {
            _weapons = new WeaponSystem(_simulation, _simulation);
            _weapons.PlayerShipIndex = _runner.PlayerShipIndex;

            _hullToShip = new int[_simulation.MaxShips];
            _shipToHull = new int[_simulation.MaxShips];
            Array.Fill(_hullToShip, -1);
            Array.Fill(_shipToHull, -1);
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
            _runner.TryGetCraftStats(shipIndex, out CraftStatRecord craft);

            Span<HullPartDefinition> parts = stackalloc HullPartDefinition[_contactPartsPerHull];
            Span<CraftPartPlacement> placements = stackalloc CraftPartPlacement[_contactPartsPerHull];
            int partCount;
            int placementCount = 0;

            if (isPlayer)
            {
                partCount = BuildPlayerParts(parts, ship);
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

            int hull = _solver.RegisterHull(ship.position, ship.rotation, ship.factionId);
            _solver.SetHullParts(hull, parts.Slice(0, partCount));
            _shipToHull[shipIndex] = hull;
            _hullToShip[hull] = shipIndex;

            if (isPlayer)
            {
                RegisterPlayerWeapons(shipIndex, parts.Slice(0, partCount));
                _playerHull = hull;
                _playerHullMax = _solver.GetHullHealth(hull);
            }
            else
            {
                RegisterCraftWeapons(shipIndex, craft, placements.Slice(0, placementCount));
            }
        }

        /// <summary>
        /// Composes the player's assembled blueprint through the shared Center/mission resolver:
        /// socket offsets, mirroring, and engine/hull/wing/gun layer order.
        /// </summary>
        private int BuildPlayerParts(Span<HullPartDefinition> parts, in ShipEntity ship)
        {
            if (_save.blueprints == null || _save.blueprints.Count == 0) return 0;

            int playerBlueprint = Math.Clamp(_save.playerBlueprintIndex, 0, _save.blueprints.Count - 1);
            BlueprintSaveData blueprint = _save.blueprints[playerBlueprint];
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
            }

            Span<CraftPartPlacement> placements = stackalloc CraftPartPlacement[_contactPartsPerHull];
            int placementCount = CraftComposition.BuildPlayer(
                mounted.Slice(0, mountedCount), _mountTable, placements);

            int count = 0;
            for (int i = 0; i < placementCount && count < parts.Length; i++)
            {
                CraftPartPlacement placement = placements[i];
                if ((uint)placement.partIndex >= (uint)mountedCount) continue;

                MountedPartData part = mounted[placement.partIndex];
                part.localOffset = placement.localOffset;
                part.localRotation = placement.localRotation;
                part.mirrored = placement.mirrored;
                count = AddMountedPart(parts, count, in part);
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
                        localOffset = placement.localOffset,
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
        private void RegisterPlayerWeapons(int shipIndex, ReadOnlySpan<HullPartDefinition> parts)
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
                    float2 local = muzzles[muzzleIndex];
                    if (part.mirrored) local.x = -local.x;

                    mounts[mountCount++] = new WeaponMount
                    {
                        spec = spec,
                        localOffset = part.localOffset + local,
                        localRotation = part.localRotation
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
                    float2 local = muzzles[muzzleIndex];
                    if (placement.mirrored) local.x = -local.x;

                    mounts[mountCount++] = new WeaponMount
                    {
                        spec = spec,
                        localOffset = placement.localOffset + local,
                        localRotation = placement.localRotation
                    };
                }
            }

            if (mountCount > 0) _weapons.RegisterCraft(shipIndex, mounts.Slice(0, mountCount));
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
            _weapons?.Tick(_combatInput);
            TickAiWeapons();
            _runner.Tick();
            RegisterSpawnedShips();
            SyncHullPoses();

            if (_solver == null) return;

            ContactStepResult contacts = _solver.Step(_simulation.Projectiles, FixedStepSeconds);
            _damageHandoff?.Apply(_solver, _hud?.State, this);

            if (contacts.hitCount == 0) return;

            var hits = _solver.Hits;
            for (int i = 0; i < hits.Length; i++)
            {
                _spriteView?.SpawnImpact(hits[i].point, hits[i].partDetached, hits[i].hullDestroyed);
                if (_combatAudio == null) continue;
                if (hits[i].hullDestroyed) _combatAudio.Explosion(1f);
                else if (hits[i].partDetached) _combatAudio.ComponentLost();
            }
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
            EnsureOverlayReferences();
            _lastResult = result;
            _resultTalkPending = false;
            _resultElapsed = 0f;

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

        /// <summary>Ends the result hold immediately (result screen click/skip control).</summary>
        public void SkipResult()
        {
            if (State != GameFlowState.Result) return;
            _resultElapsed = ResultDwellSeconds;
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
            state.SetAiCount(0);
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
                state.SetShieldFraction(PlayerShieldFraction());
                state.SetRadarBearing(math.degrees(-ship.rotation));
                CountLivingHostiles(playerIndex, ship.position, out int hostiles, out float nearest);
                state.SetAiCount(hostiles);
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
            state.SetShieldFraction(1f);
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
        /// Shield bar: remaining integrity of the player's non-chassis parts. Full when the
        /// craft carries no modular parts or the contact layer is unavailable.
        /// </summary>
        private float PlayerShieldFraction()
        {
            if (_solver == null || _playerHull < 0 || _playerHull >= _solver.HullCount) return 1f;

            int partCount = _solver.GetHullPartCount(_playerHull);
            float remaining = 0f;
            float total = 0f;
            for (int i = 0; i < partCount; i++)
            {
                MountedPartData part = _solver.GetPart(_playerHull, i);
                if (part.category == PartCategory.Body) continue;
                remaining += math.max(0f, part.health);
                total += part.maxHealth;
            }

            return total > 0f ? math.clamp(remaining / total, 0f, 1f) : 1f;
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
        Center = 1,
        Briefing = 2,
        Mission = 3,
        Result = 4,
        Return = 5,
        Factory = 6,
        Shop = 7
    }
}
