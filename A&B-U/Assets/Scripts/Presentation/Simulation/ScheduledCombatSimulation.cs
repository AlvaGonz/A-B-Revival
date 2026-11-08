using System;
using Unity.Collections;
using Unity.Jobs;
using ABRevival.Core.Simulation;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Unity-side simulation adapter that owns persistent <see cref="NativeArray{T}"/> pools and
    /// schedules the Burst graph: TacticalAIJob -> ShipMovementJob -> ProjectileMoveJob.
    /// Mirrors <see cref="CombatSimulation"/>'s Init/Tick/Dispose lifecycle while keeping job
    /// handles and native buffer lifetimes out of controllers.
    /// </summary>
    public sealed class ScheduledCombatSimulation : IDisposable, IMissionSimulation
    {
        private const int InnerLoopBatchCount = 32;

        private NativeArray<ShipEntity> _ships;
        private NativeArray<ShipInput> _inputs;
        private NativeArray<ProjectileData> _projectiles;

        private int _maxShips;
        private int _maxProjectiles;
        private int _shipCount;
        private int _projectileCount;
        private int _playerIndex;

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

        /// <summary>Live projectiles for the contact solver. Empty until weapons spawn projectiles.</summary>
        public Span<ProjectileData> Projectiles =>
            _projectiles.IsCreated ? _projectiles.AsSpan().Slice(0, _projectileCount) : Span<ProjectileData>.Empty;

        /// <summary>
        /// Index of the player-controlled ship. Tactical AI never overwrites this ship's input.
        /// Set to -1 when every ship is AI-controlled. Defaults to 0.
        /// </summary>
        public int PlayerIndex
        {
            get => _playerIndex;
            set
            {
                ThrowIfDisposed();
                if (value < -1 || value >= _maxShips)
                    throw new ArgumentOutOfRangeException(nameof(value), $"PlayerIndex must be between -1 and {_maxShips - 1}.");
                _playerIndex = value;
            }
        }

        /// <summary>
        /// Allocates persistent native buffers so Tick() performs no heap allocations.
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
            _playerIndex = 0;

            _ships = new NativeArray<ShipEntity>(maxShips, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _inputs = new NativeArray<ShipInput>(maxShips, Allocator.Persistent, NativeArrayOptions.ClearMemory);
            _projectiles = new NativeArray<ProjectileData>(maxProjectiles, Allocator.Persistent, NativeArrayOptions.ClearMemory);

            _isInitialized = true;
        }

        public void SetShip(int index, in ShipEntity ship)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            ThrowIfIndexOutOfRange(index, _maxShips, nameof(index));
            _ships[index] = ship;
        }

        public ShipEntity GetShip(int index)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            ThrowIfIndexOutOfRange(index, _maxShips, nameof(index));
            return _ships[index];
        }

        public void SetInput(int index, in ShipInput input)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            ThrowIfIndexOutOfRange(index, _maxShips, nameof(index));
            _inputs[index] = input;
        }

        public ShipInput GetInput(int index)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            ThrowIfIndexOutOfRange(index, _maxShips, nameof(index));
            return _inputs[index];
        }

        public void SetProjectile(int index, in ProjectileData projectile)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            ThrowIfIndexOutOfRange(index, _maxProjectiles, nameof(index));
            _projectiles[index] = projectile;
        }

        public ProjectileData GetProjectile(int index)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();
            ThrowIfIndexOutOfRange(index, _maxProjectiles, nameof(index));
            return _projectiles[index];
        }

        /// <summary>
        /// Schedules TacticalAIJob -> ShipMovementJob -> ProjectileMoveJob and completes the
        /// graph before returning, so the caller always observes a consistent simulation step.
        /// </summary>
        public void Tick(float deltaTime)
        {
            ThrowIfDisposed();
            ThrowIfNotInitialized();

            JobHandle handle = default;

            if (_shipCount > 0)
            {
                var tactical = new TacticalAIJob
                {
                    ships = _ships,
                    outputs = _inputs,
                    shipCount = _shipCount,
                    playerIndex = _playerIndex
                };

                var movement = new ShipMovementJob
                {
                    ships = _ships,
                    inputs = _inputs,
                    deltaTime = deltaTime
                };

                handle = tactical.Schedule(_shipCount, InnerLoopBatchCount);
                handle = movement.Schedule(_shipCount, InnerLoopBatchCount, handle);
            }

            if (_projectileCount > 0)
            {
                var projectiles = new ProjectileMoveJob
                {
                    projectiles = _projectiles,
                    deltaTime = deltaTime
                };

                handle = projectiles.Schedule(_projectileCount, InnerLoopBatchCount, handle);
            }

            handle.Complete();
        }

        private void ReleaseBuffers()
        {
            if (_ships.IsCreated) _ships.Dispose();
            if (_inputs.IsCreated) _inputs.Dispose();
            if (_projectiles.IsCreated) _projectiles.Dispose();

            _isInitialized = false;
        }

        private static void ThrowIfIndexOutOfRange(int index, int capacity, string paramName)
        {
            if (index < 0 || index >= capacity)
                throw new ArgumentOutOfRangeException(paramName);
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(ScheduledCombatSimulation));
        }

        private void ThrowIfNotInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("ScheduledCombatSimulation must be initialized with Init() before use.");
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            ReleaseBuffers();
            _isDisposed = true;
        }
    }
}
