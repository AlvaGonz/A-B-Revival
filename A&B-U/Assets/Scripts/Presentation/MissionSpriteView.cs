using System;
using ABRevival.Core.Camera;
using ABRevival.Core.Data;
using ABRevival.Core.Rendering;
using ABRevival.Core.Simulation;
using Unity.Mathematics;
using UnityEngine;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Draws the running mission from the scheduled simulation through one
    /// <see cref="BatchSpriteRenderer"/>: every live hull part, projectile, starfield star and
    /// combat particle becomes a struct instance in preallocated native buffers. There is no
    /// GameObject per entity, per-part damage is carried in unity_RendererUserValue, and
    /// detached parts are skipped because the contact solver no longer reports them as mounted.
    ///
    /// Layering matches the frozen <c>AIURender</c> contract: engine mounts first, then the
    /// hull, wings, and guns/modules in front. The starfield is the frozen
    /// <c>StarBGUnitSimple</c> four-layer parallax field; engine trails and explosion sparks
    /// are the frozen <c>Particle</c> line/flash behaviour expressed as atlas quads.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MissionSpriteView : MonoBehaviour
    {
        private const int MaxParticles = 512;

        private static readonly StarLayer[] StarLayers =
        {
            new StarLayer(0.2f, 140, 1f, 15, 70),
            new StarLayer(0.4f, 100, 2f, 20, 60),
            new StarLayer(0.65f, 50, 2f, 25, 50),
            new StarLayer(1f, 50, 2f, 15, 40)
        };

        [SerializeField] private BatchSpriteRenderer _renderer;
        [SerializeField] private Texture2D _atlas;
        [SerializeField] private TextAsset _uvTableData;
        [SerializeField] private TextAsset _mountTableData;
        [SerializeField] private TextAsset _bulletTableData;
        [SerializeField] private TextAsset _bulletCatalogData;
        [SerializeField] private Camera _camera;

        private SpriteUvTable _uvTable;
        private PartMountTable _mountTable;
        private BulletSpriteTable _bulletTable;
        private ProjectileVisualTable _projectileVisuals;
        private ProjectileRenderStyle[] _projectileStyles = Array.Empty<ProjectileRenderStyle>();
        private ScheduledCombatSimulation _simulation;
        private ProjectileContactSolver _solver;
        private WeaponSystem _weapons;
        private int[] _shipToHull;
        private SpriteInstance _instance;
        private bool _active;
        private int _capacity;

        private Star[] _stars;
        private int _starCount;
        private EffectParticle[] _particles;
        private int _particleCount;
        private float[] _trailRemainders = Array.Empty<float>();
        private uint _randomState = 0x9E3779B9u;
        private float _viewWidth = CameraDistancePresets.DesignWidth;
        private float _viewHeight = CameraDistancePresets.DesignHeight;

        public int InstanceCount => _renderer != null ? _renderer.InstanceCount : 0;
        public int BatchCount => _renderer != null ? _renderer.BatchCount : 0;
        public int Capacity => _capacity;
        public bool IsActive => _active;
        public SpriteUvTable UvTable => _uvTable;
        public PartMountTable MountTable => _mountTable;
        public ProjectileVisualTable ProjectileVisuals => _projectileVisuals;
        public BatchSpriteRenderer Renderer => _renderer;

        /// <summary>Test hook: disables the procedural background so instance counts stay focused.</summary>
        public bool StarfieldEnabled { get; set; } = true;

        /// <summary>Test hook: disables engine trails and impact particles.</summary>
        public bool EffectsEnabled { get; set; } = true;

        public int ActiveParticleCount => _particleCount;
        public int StarCount => _starCount;

        /// <summary>
        /// Injects the parsed UV table and allocates the BRG buffers. Mission start only;
        /// never called from the render loop.
        /// </summary>
        public void Initialize(SpriteUvTable uvTable, Texture2D atlas, int capacity)
        {
            _uvTable = uvTable ?? throw new ArgumentNullException(nameof(uvTable));
            if (atlas != null) _atlas = atlas;

            if (_mountTable == null) _mountTable = LoadMountTable();
            if (_bulletTable == null) _bulletTable = LoadBulletTable();
            if (_projectileVisuals == null) _projectileVisuals = LoadProjectileVisuals();
            BuildProjectileStyles();
            if (_stars == null || _particles == null) AllocateEffects();

            _capacity = (capacity > 0 ? capacity : 512) + StarCapacity() + MaxParticles;

            if (_renderer == null) _renderer = GetComponent<BatchSpriteRenderer>();
            if (_renderer == null) _renderer = gameObject.AddComponent<BatchSpriteRenderer>();

            _renderer.Configure(_capacity, _atlas);
        }

        /// <summary>
        /// Starts drawing the mission. Missing scene references or a graphics device that
        /// rejects BatchRendererGroup is a warning, not a mission failure.
        /// </summary>
        public bool BeginMission(
            ScheduledCombatSimulation simulation,
            ProjectileContactSolver solver,
            int[] shipToHull,
            int playerIndex,
            int capacity)
        {
            if (simulation == null) return false;

            try
            {
                if (_uvTable == null)
                {
                    if (_uvTableData == null)
                    {
                        Debug.LogError("[MissionSpriteView] No UV table assigned; mission sprites are disabled.");
                        return false;
                    }
                    _uvTable = SpriteUvTable.Parse(_uvTableData.text);
                }

                // One instance per part, one per projectile and one trail per projectile.
                Initialize(_uvTable, _atlas, capacity + simulation.MaxProjectiles);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[MissionSpriteView] Batch rendering unavailable: {exception.Message}");
                _active = false;
                return false;
            }

            if (_camera == null) _camera = Camera.main;
            if (_camera != null) _camera.orthographic = true;
            UpdateViewSize();
            _simulation = simulation;
            _solver = solver;
            _shipToHull = shipToHull;
            int trailSlots = simulation.MaxShips * CraftAssembly.MaxCapacity;
            if (_trailRemainders.Length < trailSlots) _trailRemainders = new float[trailSlots];
            else Array.Clear(_trailRemainders, 0, _trailRemainders.Length);
            _particleCount = 0;
            _active = true;
            Render(0f);
            return true;
        }

        public void EndMission()
        {
            _active = false;
            _simulation = null;
            _solver = null;
            _shipToHull = null;
            _particleCount = 0;
            Array.Clear(_trailRemainders, 0, _trailRemainders.Length);

            if (_renderer == null) return;
            _renderer.ClearInstances();
            _renderer.Submit();
        }

        /// <summary>Injects the camera used for parallax and viewport sizing (tests and scene wiring).</summary>
        public void SetCamera(Camera camera)
        {
            _camera = camera;
            UpdateViewSize();
        }

        /// <summary>Injects the weapon system so gun sprites rotate with their independent turret aim.</summary>
        public void SetWeapons(WeaponSystem weapons)
        {
            _weapons = weapons;
        }

        /// <summary>
        /// Rebuilds every instance from the current simulation state and uploads the dirty
        /// range. Allocation-free; the native staging buffers are owned by the renderer.
        /// </summary>
        public void Render()
        {
            Render(0f);
        }

        /// <summary>
        /// Rebuilds the frame and advances visual-only effects. The simulation is untouched.
        /// </summary>
        public void Render(float deltaTime)
        {
            if (!_active || _renderer == null || _simulation == null) return;

            UpdateViewSize();
            AdvanceEffects(deltaTime);
            _renderer.ClearInstances();

            if (StarfieldEnabled) AddStarfield();

            int shipCount = _simulation.ShipCount;
            for (int shipIndex = 0; shipIndex < shipCount; shipIndex++)
                AddShip(shipIndex, deltaTime);

            AddProjectiles();
            AddParticles();
            _renderer.Submit();
        }

        /// <summary>Spawns the frozen impact behaviour for one projectile contact.</summary>
        public void SpawnImpact(float2 position, bool partDetached, bool hullDestroyed)
        {
            if (!EffectsEnabled) return;

            if (hullDestroyed)
            {
                SpawnExplosion(position, 1.5f, 30);
                return;
            }

            if (partDetached)
            {
                SpawnExplosion(position, 0.95f, 14);
                SpawnFlash(position, 22f, new Color32(255, 245, 210, 255));
                return;
            }

            SpawnExplosion(position, 0.35f, 4);
        }

        // ---------------- Ships ----------------

        private void AddShip(int shipIndex, float deltaTime)
        {
            ShipEntity ship = _simulation.GetShip(shipIndex);
            int hull = _shipToHull != null && shipIndex < _shipToHull.Length ? _shipToHull[shipIndex] : -1;

            if (_solver == null || hull < 0 || hull >= _solver.HullCount)
            {
                var fallback = new MountedPartData
                {
                    partId = 0,
                    category = PartCategory.Body,
                    health = ship.health,
                    maxHealth = ship.maxHealth
                };
                AddPart(shipIndex, 0, in ship, in fallback);
                return;
            }

            if (_solver.IsHullDestroyed(hull)) return;

            int partCount = _solver.GetHullPartCount(hull);
            for (int layer = 0; layer <= 3; layer++)
            {
                for (int partIndex = 0; partIndex < partCount; partIndex++)
                {
                    if (!_solver.IsPartMounted(hull, partIndex)) continue;
                    MountedPartData part = _solver.GetPart(hull, partIndex);
                    if (CraftComposition.Layer(part.category) != layer) continue;
                    AddPart(shipIndex, partIndex, in ship, in part);
                    if (EffectsEnabled && part.category == PartCategory.Engine)
                        SpawnEngineTrail(hull, partIndex, in ship, in part, deltaTime);
                }
            }
        }

        private void AddPart(int shipIndex, int partIndex, in ShipEntity ship, in MountedPartData part)
        {
            SpriteUvRect rect = _uvTable.PartOrDefault(part.partId);

            float cos = math.cos(ship.rotation);
            float sin = math.sin(ship.rotation);
            float x = ship.position.x + (cos * part.localOffset.x) - (sin * part.localOffset.y);
            float y = ship.position.y + (sin * part.localOffset.x) + (cos * part.localOffset.y);

            float rotation = ship.rotation + part.localRotation;
            if (part.category == PartCategory.Gun && _weapons != null &&
                _weapons.TryGetPartAim(shipIndex, partIndex, out float aim))
            {
                rotation = aim;
            }

            float maxHealth = part.maxHealth > 0f ? part.maxHealth : 1f;
            float damage = 1f - math.saturate(part.health / maxHealth);
            float artScale = CameraDistancePresets.MountArtScale;

            _instance = new SpriteInstance
            {
                X = x,
                Y = y,
                Width = part.mirrored ? -rect.Width * artScale : rect.Width * artScale,
                Height = rect.Height * artScale,
                RotationRadians = rotation,
                UvX = rect.UvX,
                UvY = rect.UvY,
                UvWidth = rect.UvWidth,
                UvHeight = rect.UvHeight,
                UserValue = SpriteUserValue.DamageTint(damage)
            };
            _renderer.AddInstance(in _instance);
        }

        // ---------------- Projectiles ----------------

        private void AddProjectiles()
        {
            if (_projectileStyles.Length == 0) return;

            Span<ProjectileData> projectiles = _simulation.Projectiles;

            for (int i = 0; i < projectiles.Length; i++)
            {
                ref ProjectileData projectile = ref projectiles[i];
                if (!projectile.isAlive) continue;

                float2 velocity = projectile.velocity;
                float speed = math.length(velocity);
                float heading = speed > 1e-4f ? math.atan2(-velocity.x, velocity.y) : 0f;
                float2 forward = new float2(-math.sin(heading), math.cos(heading));

                int type = projectile.projectileType;
                if ((uint)type >= (uint)_projectileStyles.Length) type = 0;
                ProjectileRenderStyle style = _projectileStyles[type];

                if (style.isMissile)
                {
                    float tail = style.height * 0.6f;
                    if (EffectsEnabled)
                    {
                        AddProjectileQuad(
                            projectile.position - (forward * (style.height * 0.5f + tail * 0.5f)),
                            math.max(2f, style.width * 0.1f), tail, heading,
                            _uvTable.White, ProjectilePalette.Trail(projectile.paletteRole));
                    }

                    AddProjectileQuad(
                        projectile.position, style.width, style.height, heading,
                        style.uv, ProjectilePalette.MissileBody(projectile.paletteRole));
                    continue;
                }

                float thickness = style.thickness;
                float length = ProjectileGeometry.Length(speed, thickness);
                if (EffectsEnabled)
                {
                    float tail = length * 0.9f;
                    AddProjectileQuad(
                        projectile.position - (forward * (length + tail * 0.5f)),
                        math.max(2f, thickness * 0.5f), tail, heading,
                        _uvTable.White, ProjectilePalette.Trail(projectile.paletteRole));
                }

                AddProjectileQuad(
                    projectile.position - (forward * (length * 0.5f)),
                    thickness, length, heading,
                    style.uv, ProjectilePalette.Body(projectile.paletteRole));
            }
        }

        private void AddProjectileQuad(
            float2 position, float width, float height, float rotation, in SpriteUvRect rect, uint tint)
        {
            _instance = new SpriteInstance
            {
                X = position.x,
                Y = position.y,
                Width = width,
                Height = height,
                RotationRadians = rotation,
                UvX = rect.UvX,
                UvY = rect.UvY,
                UvWidth = rect.UvWidth,
                UvHeight = rect.UvHeight,
                UserValue = tint
            };
            _renderer.AddInstance(in _instance);
        }

        // ---------------- Starfield ----------------

        private void AddStarfield()
        {
            if (_stars == null || _starCount == 0) return;

            float cameraX = 0f;
            float cameraY = 0f;
            if (_camera != null)
            {
                Vector3 position = _camera.transform.position;
                cameraX = position.x;
                cameraY = position.y;
            }

            float originX = cameraX - (_viewWidth * 0.5f);
            float originY = cameraY - (_viewHeight * 0.5f);
            SpriteUvRect white = _uvTable.White;

            for (int i = 0; i < _starCount; i++)
            {
                ref Star star = ref _stars[i];
                float layerWidth = (_viewWidth + 100f) / star.parallax;
                float layerHeight = (_viewHeight + 100f) / star.parallax;

                float fieldX = Repeat(star.fieldX - cameraX, layerWidth);
                float fieldY = Repeat(star.fieldY - cameraY, layerHeight);
                float screenX = (fieldX * star.parallax) - 100f;
                float screenY = (fieldY * star.parallax) - 100f;

                if (screenX < -star.size || screenX > _viewWidth + star.size) continue;
                if (screenY < -star.size || screenY > _viewHeight + star.size) continue;

                _instance = new SpriteInstance
                {
                    X = originX + screenX,
                    Y = originY + screenY,
                    Width = star.size,
                    Height = star.size,
                    RotationRadians = 0f,
                    UvX = white.UvX,
                    UvY = white.UvY,
                    UvWidth = white.UvWidth,
                    UvHeight = white.UvHeight,
                    UserValue = SpriteUserValue.PackTint(255, 255, 255, star.alpha)
                };
                _renderer.AddInstance(in _instance);
            }
        }

        // ---------------- Effects ----------------

        private void AdvanceEffects(float deltaTime)
        {
            if (!EffectsEnabled || deltaTime <= 0f || _particles == null) return;

            for (int i = 0; i < _particleCount; i++)
            {
                ref EffectParticle particle = ref _particles[i];
                particle.life -= deltaTime;
                if (particle.life <= 0f)
                {
                    _particles[i] = _particles[_particleCount - 1];
                    _particleCount--;
                    i--;
                    continue;
                }

                particle.position += particle.velocity * deltaTime;
                float damping = 1f - (particle.drag * deltaTime);
                if (damping < 0f) damping = 0f;
                particle.velocity *= damping;
            }
        }

        private void AddParticles()
        {
            if (_particleCount == 0) return;

            SpriteUvRect white = _uvTable.White;
            for (int i = 0; i < _particleCount; i++)
            {
                ref EffectParticle particle = ref _particles[i];
                float fraction = particle.maxLife > 0f ? particle.life / particle.maxLife : 0f;
                if (fraction < 0f) fraction = 0f;
                byte alpha = (byte)(particle.color.a * fraction);
                if (alpha == 0) continue;

                _instance = new SpriteInstance
                {
                    X = particle.position.x,
                    Y = particle.position.y,
                    Width = particle.size * (0.4f + (0.6f * fraction)),
                    Height = particle.size * (0.4f + (0.6f * fraction)),
                    RotationRadians = 0f,
                    UvX = white.UvX,
                    UvY = white.UvY,
                    UvWidth = white.UvWidth,
                    UvHeight = white.UvHeight,
                    UserValue = SpriteUserValue.PackTint(particle.color.r, particle.color.g, particle.color.b, alpha)
                };
                _renderer.AddInstance(in _instance);
            }
        }

        private void SpawnEngineTrail(int hull, int partIndex, in ShipEntity ship, in MountedPartData part, float deltaTime)
        {
            if (deltaTime <= 0f || _mountTable == null) return;

            int emitter = (hull * CraftAssembly.MaxCapacity) + partIndex;
            if ((uint)emitter >= (uint)_trailRemainders.Length) return;

            float speed = math.length(ship.velocity);
            float rate = 12f + (speed * 0.05f);
            // Emit fractionally: (int)(rate * dt) would truncate to zero below ~960 px/s.
            float pending = _trailRemainders[emitter] + (rate * deltaTime);
            int count = (int)pending;
            _trailRemainders[emitter] = pending - count;
            if (count <= 0) return;

            float cos = math.cos(ship.rotation);
            float sin = math.sin(ship.rotation);
            float2 origin = new float2(
                ship.position.x + (cos * part.localOffset.x) - (sin * part.localOffset.y),
                ship.position.y + (sin * part.localOffset.x) + (cos * part.localOffset.y));

            float2 exhaustLocal = _mountTable.ExhaustLocal(part.partId) * CameraDistancePresets.MountArtScale;
            float2 exhaust = origin + new float2(
                (cos * exhaustLocal.x) - (sin * exhaustLocal.y),
                (sin * exhaustLocal.x) + (cos * exhaustLocal.y));

            float2 forward = ShipEntity.Forward(ship.rotation);
            float2 lateral = new float2(cos, sin);
            bool friendly = ship.factionId == 0;
            var color = friendly
                ? new Color32(90, 220, 255, 200)
                : new Color32(255, 130, 70, 200);
            int available = _particles.Length - _particleCount;
            if (count > available) count = available;

            for (int i = 0; i < count; i++)
            {
                float spread = (NextFloat() * 2f - 1f) * 24f;
                _particles[_particleCount++] = new EffectParticle
                {
                    position = exhaust + (lateral * ((NextFloat() * 2f - 1f) * 2f)),
                    velocity = (-forward * (30f + (speed * 0.25f))) + (lateral * spread) + (ship.velocity * 0.35f),
                    life = 0.18f + (NextFloat() * 0.18f),
                    maxLife = 0.36f,
                    size = 2f + (NextFloat() * 2f),
                    drag = 1.5f,
                    color = color
                };
            }
        }

        private void SpawnExplosion(float2 position, float scale, int count)
        {
            if (_particles == null) return;

            for (int i = 0; i < count && _particleCount < _particles.Length; i++)
            {
                float angle = NextFloat() * math.PI * 2f;
                float speed = (30f + (NextFloat() * 140f)) * scale;
                float life = 0.35f + (NextFloat() * 0.55f);
                bool whiteHot = NextFloat() < 0.35f;

                Color32 color = whiteHot
                    ? new Color32(255, 255, 240, 255)
                    : new Color32(255, (byte)(120 + (NextFloat() * 120f)), 40, 255);

                _particles[_particleCount++] = new EffectParticle
                {
                    position = position,
                    velocity = new float2(math.cos(angle) * speed, math.sin(angle) * speed),
                    life = life,
                    maxLife = life,
                    size = (2f + (NextFloat() * 4f)) * scale,
                    drag = 1.2f,
                    color = color
                };
            }
        }

        private void SpawnFlash(float2 position, float size, Color32 color)
        {
            if (_particles == null || _particleCount >= _particles.Length) return;

            _particles[_particleCount++] = new EffectParticle
            {
                position = position,
                velocity = float2.zero,
                life = 0.08f,
                maxLife = 0.08f,
                size = size,
                drag = 0f,
                color = color
            };
        }

        // ---------------- Allocation / data ----------------

        private void AllocateEffects()
        {
            _stars = new Star[StarCapacity()];
            _particles = new EffectParticle[MaxParticles];
            _particleCount = 0;

            var random = new System.Random(0x5EED);
            int index = 0;
            float farWidth = CameraDistancePresets.DesignWidth * 1.5f;
            float farHeight = CameraDistancePresets.DesignHeight * 1.5f;
            for (int layerIndex = 0; layerIndex < StarLayers.Length; layerIndex++)
            {
                StarLayer layer = StarLayers[layerIndex];
                float fieldWidth = (farWidth + 100f) / layer.parallax;
                float fieldHeight = (farHeight + 100f) / layer.parallax;
                for (int i = 0; i < layer.count; i++)
                {
                    _stars[index++] = new Star
                    {
                        fieldX = (float)random.NextDouble() * fieldWidth,
                        fieldY = (float)random.NextDouble() * fieldHeight,
                        parallax = layer.parallax,
                        size = layer.size,
                        alpha = (byte)(layer.alphaMin + (random.Next() % (layer.alphaMax - layer.alphaMin + 1)))
                    };
                }
            }
            _starCount = index;
        }

        private static int StarCapacity()
        {
            int total = 0;
            for (int i = 0; i < StarLayers.Length; i++) total += StarLayers[i].count;
            return total;
        }

        private void UpdateViewSize()
        {
            if (_camera != null && _camera.orthographic)
            {
                _viewHeight = _camera.orthographicSize * 2f;
                _viewWidth = _viewHeight * _camera.aspect;
                return;
            }

            _viewWidth = CameraDistancePresets.DesignWidth;
            _viewHeight = CameraDistancePresets.DesignHeight;
        }

        private PartMountTable LoadMountTable()
        {
            try
            {
                if (_mountTableData != null) return PartMountTable.Parse(_mountTableData.text);
                TextAsset table = Resources.Load<TextAsset>("Data/PartMounts");
                return table != null ? PartMountTable.Parse(table.text) : null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[MissionSpriteView] Part mount table unavailable: {exception.Message}");
                return null;
            }
        }

        private BulletSpriteTable LoadBulletTable()
        {
            try
            {
                if (_bulletTableData != null) return BulletSpriteTable.Parse(_bulletTableData.text);
                TextAsset table = Resources.Load<TextAsset>("Data/BulletSprites");
                return table != null ? BulletSpriteTable.Parse(table.text) : null;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[MissionSpriteView] Bullet art table unavailable: {exception.Message}");
                return null;
            }
        }

        /// <summary>
        /// Joins the frozen bullet XML (btwh and mcName) with the extracted art table. Mission
        /// start only; the per-frame path just indexes the resolved style array.
        /// </summary>
        private ProjectileVisualTable LoadProjectileVisuals()
        {
            try
            {
                if (_bulletTable == null) _bulletTable = LoadBulletTable();
                if (_bulletCatalogData != null)
                    return ProjectileVisualTable.Build(BulletCatalog.Parse(_bulletCatalogData.text), _bulletTable);

                TextAsset catalog = Resources.Load<TextAsset>("Data/11_all.AllData_defBullet");
                return catalog != null
                    ? ProjectileVisualTable.Build(BulletCatalog.Parse(catalog.text), _bulletTable)
                    : ProjectileVisualTable.Build(null, _bulletTable);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[MissionSpriteView] Bullet visual catalog unavailable: {exception.Message}");
                return ProjectileVisualTable.Build(null, _bulletTable);
            }
        }

        private void BuildProjectileStyles()
        {
            int count = ProjectileVisualTable.MaxBulletId + 1;
            if (_projectileStyles == null || _projectileStyles.Length != count)
                _projectileStyles = new ProjectileRenderStyle[count];

            SpriteUvRect white = _uvTable != null ? _uvTable.White : default;
            for (int id = 0; id < count; id++)
            {
                ProjectileVisual visual = _projectileVisuals != null ? _projectileVisuals.Get(id) : default;
                if (visual.isMissile && _uvTable != null && _uvTable.TryGetSymbol(visual.symbol, out SpriteUvRect rect))
                {
                    _projectileStyles[id] = new ProjectileRenderStyle
                    {
                        isMissile = true,
                        uv = rect,
                        width = visual.spriteWidth * CameraDistancePresets.MountArtScale,
                        height = visual.spriteHeight * CameraDistancePresets.MountArtScale
                    };
                    continue;
                }

                _projectileStyles[id] = new ProjectileRenderStyle
                {
                    isMissile = false,
                    uv = white,
                    thickness = ProjectileGeometry.Thickness(visual.thickness)
                };
            }
        }

        private float NextFloat()
        {
            _randomState ^= _randomState << 13;
            _randomState ^= _randomState >> 17;
            _randomState ^= _randomState << 5;
            return (_randomState & 0xFFFFFF) / 16777216f;
        }

        private static float Repeat(float value, float period)
        {
            if (period <= 0f) return 0f;
            float result = value - (math.floor(value / period) * period);
            return result < 0f ? result + period : result;
        }

        private readonly struct StarLayer
        {
            public readonly float parallax;
            public readonly int count;
            public readonly float size;
            public readonly int alphaMin;
            public readonly int alphaMax;

            public StarLayer(float parallax, int count, float size, int alphaMin, int alphaMax)
            {
                this.parallax = parallax;
                this.count = count;
                this.size = size;
                this.alphaMin = alphaMin;
                this.alphaMax = alphaMax;
            }
        }

        private struct Star
        {
            public float fieldX;
            public float fieldY;
            public float parallax;
            public float size;
            public byte alpha;
        }

        private struct EffectParticle
        {
            public float2 position;
            public float2 velocity;
            public float life;
            public float maxLife;
            public float size;
            public float drag;
            public Color32 color;
        }

        /// <summary>Resolved per-bullet draw rule: a procedural stroke size or extracted missile art.</summary>
        private struct ProjectileRenderStyle
        {
            public bool isMissile;
            public float thickness;
            public SpriteUvRect uv;
            public float width;
            public float height;
        }
    }
}
