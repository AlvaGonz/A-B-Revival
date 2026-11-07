using System;
using ABRevival.Core.Data;

namespace ABRevival.Core.Data
{
    /// <summary>
    /// Strongly-typed assembly error states for craft construction validation.
    /// </summary>
    public enum AssemblyError
    {
        None = 0,
        ExceedsWingCapacity,
        SocketOccupied,
        SocketOutOfRange,
        InsufficientEnergy,
        MissingHull,
        AssemblyFull
    }

    /// <summary>
    /// Pure C# domain model encapsulating ship construction, socket mounting,
    /// grandfathered wing limit enforcement (ADR 0007), and O(1) incremental stat tracking.
    /// Zero heap allocations in hot paths.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class CraftAssembly : IDisposable
    {
        public const int MaxCapacity = 32;
        public const float MaxEnergyThreshold = 999f;

        private readonly string _hullName;
        private readonly HullCategory _hullCategory;
        private readonly bool _isGrandfathered;
        private readonly int _maxAllowedWings;

        private readonly MountedPartData[] _slots = new MountedPartData[MaxCapacity];
        private readonly bool[] _occupied = new bool[MaxCapacity];

        private AggregateCraftStats _currentStats;
        private int _partCount;
        private int _wingCount;
        private int _bodyCount;
        private bool _isDisposed;

        public string HullName => _hullName;
        public HullCategory HullCategory => _hullCategory;
        public bool IsGrandfathered => _isGrandfathered;
        public int PartCount => _partCount;
        public int WingCount => _wingCount;
        public int BodyCount => _bodyCount;
        public AggregateCraftStats CurrentStats => _currentStats;

        /// <summary>
        /// True if at least one Body part is mounted, wing limit is respected (or grandfathered),
        /// and energy draw does not exceed safety threshold.
        /// </summary>
        public bool IsValid => Validate(out _);

        /// <summary>
        /// Validates overall craft validity with strongly-typed error reporting.
        /// </summary>
        public bool Validate(out AssemblyError error)
        {
            if (_bodyCount <= 0)
            {
                error = AssemblyError.MissingHull;
                return false;
            }

            if (!_isGrandfathered && _wingCount > _maxAllowedWings)
            {
                error = AssemblyError.ExceedsWingCapacity;
                return false;
            }

            if (_currentStats.totalEnergyDraw > MaxEnergyThreshold)
            {
                error = AssemblyError.InsufficientEnergy;
                return false;
            }

            error = AssemblyError.None;
            return true;
        }

        public CraftAssembly(string hullName, float hullMass = 100f, float hullHealth = 500f, bool isGrandfathered = false)
        {
            _hullName = hullName ?? string.Empty;
            _hullCategory = CraftBlueprintSO.GetHullCategoryForName(_hullName);
            _maxAllowedWings = CraftBlueprintSO.GetMaxWingsForCategory(_hullCategory);
            _isGrandfathered = isGrandfathered;

            _currentStats = new AggregateCraftStats
            {
                totalMass = hullMass,
                totalHealth = hullHealth,
                totalThrust = 0f,
                totalEnergyDraw = 0f
            };
        }

        /// <summary>
        /// Attempts to mount a part into the first available socket.
        /// </summary>
        public AssemblyError TryMountPart(in MountedPartData part, out int slotIndex)
        {
            slotIndex = -1;
            if (_partCount >= MaxCapacity)
            {
                return AssemblyError.AssemblyFull;
            }

            // Find first free socket
            for (int i = 0; i < MaxCapacity; i++)
            {
                if (!_occupied[i])
                {
                    if (TryMountPart(i, in part, out var error))
                    {
                        slotIndex = i;
                        return AssemblyError.None;
                    }
                    return error;
                }
            }

            return AssemblyError.AssemblyFull;
        }

        /// <summary>
        /// Attempts to mount a part into an explicit socket index.
        /// Operates with O(1) incremental stat updates.
        /// </summary>
        public bool TryMountPart(int socketIndex, in MountedPartData part, out AssemblyError error)
        {
            if (socketIndex < 0 || socketIndex >= MaxCapacity)
            {
                error = AssemblyError.SocketOutOfRange;
                return false;
            }

            if (_occupied[socketIndex])
            {
                error = AssemblyError.SocketOccupied;
                return false;
            }

            // Validate wing capacity limit per ADR 0007
            if (part.category == PartCategory.Wing && !_isGrandfathered && (_wingCount + 1) > _maxAllowedWings)
            {
                error = AssemblyError.ExceedsWingCapacity;
                return false;
            }

            // Validate energy draw limit
            if ((_currentStats.totalEnergyDraw + part.energyDraw) > MaxEnergyThreshold)
            {
                error = AssemblyError.InsufficientEnergy;
                return false;
            }

            // Mount part
            _slots[socketIndex] = part;
            _occupied[socketIndex] = true;
            _partCount++;

            if (part.category == PartCategory.Wing)
                _wingCount++;
            else if (part.category == PartCategory.Body)
                _bodyCount++;

            // O(1) delta stat accumulation
            _currentStats.totalMass += part.mass;
            _currentStats.totalHealth += part.health;
            _currentStats.totalThrust += part.thrust;
            _currentStats.totalEnergyDraw += part.energyDraw;

            error = AssemblyError.None;
            return true;
        }

        /// <summary>
        /// Unmounts a part from a designated socket index and returns removed part.
        /// Operates with O(1) incremental stat updates.
        /// </summary>
        public bool UnmountPart(int socketIndex, out MountedPartData removedPart)
        {
            if (socketIndex < 0 || socketIndex >= MaxCapacity || !_occupied[socketIndex])
            {
                removedPart = default;
                return false;
            }

            removedPart = _slots[socketIndex];
            _slots[socketIndex] = default;
            _occupied[socketIndex] = false;
            _partCount--;

            if (removedPart.category == PartCategory.Wing)
                _wingCount--;
            else if (removedPart.category == PartCategory.Body)
                _bodyCount--;

            // O(1) delta stat decrement
            _currentStats.totalMass -= removedPart.mass;
            _currentStats.totalHealth -= removedPart.health;
            _currentStats.totalThrust -= removedPart.thrust;
            _currentStats.totalEnergyDraw -= removedPart.energyDraw;

            return true;
        }

        /// <summary>
        /// Convenience unmount overload.
        /// </summary>
        public bool UnmountPart(int socketIndex)
        {
            return UnmountPart(socketIndex, out _);
        }



        public bool IsSlotOccupied(int socketIndex)
        {
            if (socketIndex < 0 || socketIndex >= MaxCapacity) return false;
            return _occupied[socketIndex];
        }

        public MountedPartData GetPart(int socketIndex)
        {
            if (socketIndex < 0 || socketIndex >= MaxCapacity) return default;
            return _slots[socketIndex];
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            Array.Clear(_slots, 0, _slots.Length);
            Array.Clear(_occupied, 0, _occupied.Length);
            _partCount = 0;
            _wingCount = 0;
            _bodyCount = 0;
            _isDisposed = true;
        }
    }
}
