using System;
using Unity.Mathematics;

namespace ABRevival.Core.Simulation
{
    /// <summary>
    /// Deep simulation runner encapsulating contiguous entity buffers,
    /// Burst job execution graphs, and zero-allocation combat stepping.
    /// Provides CLI test harness compatibility and zero GC allocations during Tick().
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class CombatSimulation : IDisposable
    {
        private ShipEntity[] _ships;
        private ShipInput[] _inputs;
        private ProjectileData[] _projectiles;

        private int _maxShips;
        private int _maxProjectiles;
        private int _shipCount;
        private int _projectileCount;

        private bool _isInitialized;
        private bool _isDisposed;

        public bool IsInitialized => _isInitialized;
        public bool IsDisposed => _isDisposed;
        public int MaxShips => _maxShips;
        public int MaxProjectiles => _maxProjectiles;

        public int ShipCount
        {
            get => _shipCount;
            set
            {
                ThrowIfDisposed();
                ThrowIfNotInitialized();
                if (value < 0 || value > _maxShips)
                    throw new ArgumentOutOfRangeException(nameof(value), $"ShipCount must be between 0 and {_maxShips}.");
                _shipCount = value;
            }
        }

        public int ProjectileCount
        {
            get => _projectileCount;
            set
            {
                ThrowIfDisposed();
                ThrowIfNotInitialized();
                if (value < 0 || value > _maxProjectiles)
                    throw new ArgumentOutOfRangeException(nameof(value), $"ProjectileCount must be between 0 and {_maxProjectiles}.");
                _projectileCount = value;
            }
        }

        public Span<ShipEntity> Ships => _ships != null ? _ships.AsSpan(0, _shipCount) : Span<ShipEntity>.Empty;
        public Span<ShipInput> Inputs => _inputs != null ? _inputs.AsSpan(0, _shipCount) : Span<ShipInput>.Empty;
        public Span<ProjectileData> Projectiles => _projectiles != null ? _projectiles.AsSpan(0, _projectileCount) : Span<ProjectileData>.Empty;

        public CombatSimulation()
        {
        }

        /// <summary>
        /// Allocates pre-sized contiguous memory buffers for ships, inputs, and projectiles.
        /// Pre-allocates all memory so that Tick() runs with 0 heap allocations.
        /// </summary>
        public void Init(int maxShips, int maxProjectiles)
        {
            ThrowIfDisposed();

            if (maxShips <= 0) throw new ArgumentOutOfRangeException(nameof(maxShips), "maxShips must be greater than 0.");
            if (maxProjectiles <= 0) throw new ArgumentOutOfRangeException(nameof(maxProjectiles), "maxProjectiles must be greater than 0.");

            ReleaseBuffers();

            _maxShips = maxShips;
            _maxProjectiles = maxProjectiles;
            _shipCount = 0;
            _projectileCount = 0;

            _ships = new ShipEntity[_maxShips];
            _inputs = new ShipInput[_maxShips];
            _projectiles = new ProjectileData[_maxProjectiles];

            _isInitialized = true;
        }

        public void SetShip(int index, in ShipEntity ship)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            if (index < 0 || index >= _maxShips)
                throw new ArgumentOutOfRangeException(nameof(index));
            _ships[index] = ship;
        }

        public ShipEntity GetShip(int index)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            if (index < 0 || index >= _maxShips)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _ships[index];
        }

        /// <summary>
        /// Nearest living hostile for one craft, using the same faction/health rule as
        /// <see cref="TacticalAIJob"/>. Used by the Tier 1 weapon seam.
        /// </summary>
        public bool TryFindNearestHostile(int shipIndex, out ShipEntity target)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();

            target = default;
            if (shipIndex < 0 || shipIndex >= _shipCount) return false;

            ShipEntity ship = _ships[shipIndex];
            float nearestSq = float.MaxValue;
            bool found = false;
            for (int i = 0; i < _shipCount; i++)
            {
                ShipEntity candidate = _ships[i];
                if (candidate.health <= 0f) continue;
                if (candidate.factionId == ship.factionId) continue;

                float distanceSq = math.lengthsq(candidate.position - ship.position);
                if (distanceSq >= nearestSq) continue;

                nearestSq = distanceSq;
                target = candidate;
                found = true;
            }

            return found;
        }

        public void SetInput(int index, in ShipInput input)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            if (index < 0 || index >= _maxShips)
                throw new ArgumentOutOfRangeException(nameof(index));
            _inputs[index] = input;
        }

        public ShipInput GetInput(int index)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            if (index < 0 || index >= _maxShips)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _inputs[index];
        }

        public void SetProjectile(int index, in ProjectileData projectile)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            if (index < 0 || index >= _maxProjectiles)
                throw new ArgumentOutOfRangeException(nameof(index));
            _projectiles[index] = projectile;
        }

        public ProjectileData GetProjectile(int index)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            if (index < 0 || index >= _maxProjectiles)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _projectiles[index];
        }

        /// <summary>
        /// Advances the combat simulation by deltaTime seconds.
        /// Chained execution: Tactical Steering -> Ship Movement Integration -> Projectile Ballistics.
        /// Strictly 0 GC heap allocations.
        /// </summary>
        public void Tick(float deltaTime)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();

            // 1 & 2: Ship Movement & Physics Integration
            for (int i = 0; i < _shipCount; i++)
            {
                ref var ship = ref _ships[i];
                ShipMovementJob.Integrate(ref ship, in _inputs[i], deltaTime);
            }

            // 3: Projectile Ballistics Integration
            for (int j = 0; j < _projectileCount; j++)
            {
                ref var p = ref _projectiles[j];
                ProjectileMoveJob.Integrate(ref p, deltaTime);
            }
        }

        private void ReleaseBuffers()
        {
            _ships = null;
            _inputs = null;
            _projectiles = null;
            _isInitialized = false;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(CombatSimulation));
        }

        private void ThrowIfNotInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("CombatSimulation must be initialized with Init() before use.");
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            ReleaseBuffers();
            _isDisposed = true;
        }
    }
}
