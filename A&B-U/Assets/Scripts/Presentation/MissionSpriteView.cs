using System;
using ABRevival.Core.Data;
using ABRevival.Core.Rendering;
using ABRevival.Core.Simulation;
using Unity.Mathematics;
using UnityEngine;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Draws the running mission from the scheduled simulation through one
    /// <see cref="BatchSpriteRenderer"/>: every live hull part and projectile becomes a
    /// struct instance in preallocated native buffers. There is no GameObject per entity,
    /// per-part damage is carried in unity_RendererUserValue, and detached parts are
    /// skipped because the contact solver no longer reports them as mounted.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MissionSpriteView : MonoBehaviour
    {
        private static readonly uint FriendlyProjectileTint = SpriteUserValue.PackTint(120, 210, 255, 255);
        private static readonly uint HostileProjectileTint = SpriteUserValue.PackTint(136, 74, 112, 255);

        [SerializeField] private BatchSpriteRenderer _renderer;
        [SerializeField] private Texture2D _atlas;
        [SerializeField] private TextAsset _uvTableData;
        [SerializeField] private float _projectileLength = 16f;
        [SerializeField] private float _projectileWidth = 4f;

        private SpriteUvTable _uvTable;
        private ScheduledCombatSimulation _simulation;
        private ProjectileContactSolver _solver;
        private int[] _shipToHull;
        private SpriteInstance _instance;
        private bool _active;
        private int _capacity;

        public int InstanceCount => _renderer != null ? _renderer.InstanceCount : 0;
        public int BatchCount => _renderer != null ? _renderer.BatchCount : 0;
        public int Capacity => _capacity;
        public bool IsActive => _active;
        public SpriteUvTable UvTable => _uvTable;
        public BatchSpriteRenderer Renderer => _renderer;

        /// <summary>
        /// Injects the parsed UV table and allocates the BRG buffers. Mission start only;
        /// never called from the render loop.
        /// </summary>
        public void Initialize(SpriteUvTable uvTable, Texture2D atlas, int capacity)
        {
            _uvTable = uvTable ?? throw new ArgumentNullException(nameof(uvTable));
            if (atlas != null) _atlas = atlas;
            _capacity = capacity > 0 ? capacity : 512;

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

                Initialize(_uvTable, _atlas, capacity);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[MissionSpriteView] Batch rendering unavailable: {exception.Message}");
                _active = false;
                return false;
            }

            _simulation = simulation;
            _solver = solver;
            _shipToHull = shipToHull;
            _active = true;
            Render();
            return true;
        }

        public void EndMission()
        {
            _active = false;
            _simulation = null;
            _solver = null;
            _shipToHull = null;

            if (_renderer == null) return;
            _renderer.ClearInstances();
            _renderer.Submit();
        }

        /// <summary>
        /// Rebuilds every instance from the current simulation state and uploads the dirty
        /// range. Allocation-free; the native staging buffers are owned by the renderer.
        /// </summary>
        public void Render()
        {
            if (!_active || _renderer == null || _simulation == null) return;

            _renderer.ClearInstances();

            int shipCount = _simulation.ShipCount;
            for (int shipIndex = 0; shipIndex < shipCount; shipIndex++)
                AddShip(shipIndex);

            AddProjectiles();
            _renderer.Submit();
        }

        private void AddShip(int shipIndex)
        {
            ShipEntity ship = _simulation.GetShip(shipIndex);
            int hull = _shipToHull != null && shipIndex < _shipToHull.Length ? _shipToHull[shipIndex] : -1;

            if (_solver == null || hull < 0 || hull >= _solver.HullCount)
            {
                var fallback = new MountedPartData
                {
                    partId = 0,
                    health = ship.health,
                    maxHealth = ship.maxHealth
                };
                AddPart(in ship, in fallback);
                return;
            }

            if (_solver.IsHullDestroyed(hull)) return;

            int partCount = _solver.GetHullPartCount(hull);
            for (int partIndex = 0; partIndex < partCount; partIndex++)
            {
                if (!_solver.IsPartMounted(hull, partIndex)) continue;
                MountedPartData part = _solver.GetPart(hull, partIndex);
                AddPart(in ship, in part);
            }
        }

        private void AddPart(in ShipEntity ship, in MountedPartData part)
        {
            SpriteUvRect rect = _uvTable.PartOrDefault(part.partId);

            float cos = math.cos(ship.rotation);
            float sin = math.sin(ship.rotation);
            float x = ship.position.x + (cos * part.localOffset.x) - (sin * part.localOffset.y);
            float y = ship.position.y + (sin * part.localOffset.x) + (cos * part.localOffset.y);

            float maxHealth = part.maxHealth > 0f ? part.maxHealth : 1f;
            float damage = 1f - math.saturate(part.health / maxHealth);

            _instance = new SpriteInstance
            {
                X = x,
                Y = y,
                Width = rect.Width,
                Height = rect.Height,
                RotationRadians = ship.rotation + part.localRotation,
                UvX = rect.UvX,
                UvY = rect.UvY,
                UvWidth = rect.UvWidth,
                UvHeight = rect.UvHeight,
                UserValue = SpriteUserValue.DamageTint(damage)
            };
            _renderer.AddInstance(in _instance);
        }

        private void AddProjectiles()
        {
            SpriteUvRect rect = _uvTable.White;
            Span<ProjectileData> projectiles = _simulation.Projectiles;

            for (int i = 0; i < projectiles.Length; i++)
            {
                ref ProjectileData projectile = ref projectiles[i];
                if (!projectile.isAlive) continue;

                float2 velocity = projectile.velocity;
                float heading = math.lengthsq(velocity) > 1e-6f ? math.atan2(-velocity.x, velocity.y) : 0f;

                _instance = new SpriteInstance
                {
                    X = projectile.position.x,
                    Y = projectile.position.y,
                    Width = _projectileWidth,
                    Height = _projectileLength,
                    RotationRadians = heading,
                    UvX = rect.UvX,
                    UvY = rect.UvY,
                    UvWidth = rect.UvWidth,
                    UvHeight = rect.UvHeight,
                    UserValue = projectile.factionId == 0 ? FriendlyProjectileTint : HostileProjectileTint
                };
                _renderer.AddInstance(in _instance);
            }
        }
    }
}
