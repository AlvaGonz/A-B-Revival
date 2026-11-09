using System;
using ABRevival.Core.Simulation;
using Unity.Mathematics;

namespace ABRevival.Presentation
{
    /// <summary>
    /// One hardpoint on a craft: the fire mode shared by its ports and the port pose in hull space.
    /// Gun mounts rotate independently of the hull; <see cref="localOffset"/> is the part pivot
    /// and <see cref="localMuzzle"/> the F marker in part space.
    /// </summary>
    public struct WeaponMount
    {
        public FireModeSpec spec;

        /// <summary>Part pivot in hull space: +X is starboard, +Y is forward.</summary>
        public float2 localOffset;

        /// <summary>Muzzle marker in part-local space, rotated with the turret aim.</summary>
        public float2 localMuzzle;

        /// <summary>Part heading offset in radians relative to the hull heading.</summary>
        public float localRotation;

        /// <summary>Current world aim heading in radians, full 360 degrees.</summary>
        public float aimRotation;

        /// <summary>Hull part index used by the renderer to rotate the gun sprite.</summary>
        public int partIndex;
    }

    /// <summary>
    /// Owns the fire ports of every registered craft, advances the frozen <c>FireTimer</c> cadence
    /// once per simulation tick, and spawns <see cref="ProjectileData"/> into the scheduled
    /// simulation. Recoil is the frozen <c>fireAcc</c> dispersion cone applied to each shot.
    /// Every gun mount tracks its own full-360 aim; wings, engines and armour stay hull-fixed.
    /// All per-craft state is allocated at registration; <see cref="Tick"/> allocates nothing.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class WeaponSystem
    {
        private sealed class CraftWeapons
        {
            public WeaponMount[] Mounts;
            public FireControlState[] States;
            public float MaxRange;
        }

        private readonly IMissionSimulation _simulation;
        private readonly IProjectileSpawner _projectiles;
        private readonly CraftWeapons[] _crafts;

        public WeaponSystem(IMissionSimulation simulation, IProjectileSpawner projectiles)
        {
            _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            _projectiles = projectiles ?? throw new ArgumentNullException(nameof(projectiles));
            _crafts = new CraftWeapons[simulation.MaxShips];
        }

        /// <summary>Craft driven by <see cref="ICombatFireInput"/>; -1 disables input firing.</summary>
        public int PlayerShipIndex { get; set; } = -1;

        /// <summary>Projectiles spawned since construction. Telemetry for tests.</summary>
        public int ShotsFired { get; private set; }

        /// <summary>Copies a craft's ports into weapon state. Safe to call once per craft; not a hot path.</summary>
        public void RegisterCraft(int shipIndex, ReadOnlySpan<WeaponMount> mounts)
        {
            if (shipIndex < 0 || shipIndex >= _crafts.Length)
                throw new ArgumentOutOfRangeException(nameof(shipIndex));
            if (mounts.Length == 0) return;

            ShipEntity ship = _simulation.GetShip(shipIndex);
            var copied = mounts.ToArray();
            float maxRange = 0f;
            for (int i = 0; i < copied.Length; i++)
            {
                if (copied[i].aimRotation == 0f)
                    copied[i].aimRotation = ship.rotation + copied[i].localRotation;
                maxRange = math.max(maxRange, copied[i].spec.EffectiveRange);
            }

            _crafts[shipIndex] = new CraftWeapons
            {
                Mounts = copied,
                States = new FireControlState[copied.Length],
                MaxRange = maxRange
            };
        }

        /// <summary>Advances the player craft's ports by one simulation tick.</summary>
        public int Tick(ICombatFireInput input)
        {
            if (input == null || PlayerShipIndex < 0) return 0;
            return TickCraft(PlayerShipIndex, input.FireHeld, input.Autofire, false, default, false);
        }

        /// <summary>
        /// Advances one craft with an explicit aim point. Player keyboard fire does not gate on
        /// range; mouse-mode auto-target and every AI craft do.
        /// </summary>
        public int TickAim(int shipIndex, bool fireHeld, bool autofire, float2 aimPoint, bool requireRange)
        {
            return TickCraft(shipIndex, fireHeld, autofire, true, aimPoint, requireRange);
        }

        /// <summary>Advances one craft's ports by one simulation tick.</summary>
        public int TickCraft(int shipIndex, bool fireHeld, bool autofire)
        {
            return TickCraft(shipIndex, fireHeld, autofire, false, default, false);
        }

        /// <summary>
        /// Advances one AI craft's ports and aims every mount at the target position. A mount
        /// only fires while the target is inside its ADR 0012 effective range, so the Captain
        /// fires in all six tactical modes (ADR 0006) without mode-specific branches.
        /// </summary>
        public int TickAi(int shipIndex, float2 targetPosition)
        {
            return TickCraft(shipIndex, true, false, true, targetPosition, true);
        }

        /// <summary>Highest effective range among a craft's mounted weapons, in world pixels.</summary>
        public float MaxEffectiveRange(int shipIndex)
        {
            if (shipIndex < 0 || shipIndex >= _crafts.Length || _crafts[shipIndex] == null) return 0f;
            return _crafts[shipIndex].MaxRange;
        }

        /// <summary>
        /// Mouse-mode auto-target: the closest hostile inside the highest-range mounted weapon.
        /// Returns false when no mount exists or every hostile is out of range.
        /// </summary>
        public bool TryFindAutoTarget(int shipIndex, out ShipEntity target)
        {
            target = default;
            if (shipIndex < 0 || shipIndex >= _crafts.Length) return false;

            float range = MaxEffectiveRange(shipIndex);
            if (range <= 0f) return false;

            ShipEntity origin = _simulation.GetShip(shipIndex);
            float rangeSq = range * range;
            float nearestSq = float.MaxValue;
            bool found = false;

            int shipCount = _simulation.ShipCount;
            for (int i = 0; i < shipCount; i++)
            {
                if (i == shipIndex) continue;
                ShipEntity candidate = _simulation.GetShip(i);
                if (candidate.factionId == origin.factionId) continue;

                float distanceSq = math.lengthsq(candidate.position - origin.position);
                if (distanceSq > rangeSq || distanceSq >= nearestSq) continue;

                nearestSq = distanceSq;
                target = candidate;
                found = true;
            }

            return found;
        }

        /// <summary>World aim heading of the first mount belonging to a gun part.</summary>
        public bool TryGetPartAim(int shipIndex, int partIndex, out float aimRotation)
        {
            aimRotation = 0f;
            if (shipIndex < 0 || shipIndex >= _crafts.Length) return false;

            CraftWeapons craft = _crafts[shipIndex];
            if (craft == null) return false;

            for (int i = 0; i < craft.Mounts.Length; i++)
            {
                if (craft.Mounts[i].partIndex != partIndex) continue;
                aimRotation = craft.Mounts[i].aimRotation;
                return true;
            }

            return false;
        }

        private int TickCraft(
            int shipIndex,
            bool fireHeld,
            bool autofire,
            bool hasAim,
            float2 aimPoint,
            bool requireRange)
        {
            if (shipIndex < 0 || shipIndex >= _crafts.Length) return 0;
            CraftWeapons craft = _crafts[shipIndex];
            if (craft == null) return 0;

            ShipEntity ship = _simulation.GetShip(shipIndex);
            int spawned = 0;

            for (int mountIndex = 0; mountIndex < craft.Mounts.Length; mountIndex++)
            {
                ref WeaponMount mount = ref craft.Mounts[mountIndex];
                ref FireControlState state = ref craft.States[mountIndex];

                float2 pivot = PortPosition(in ship, mount.localOffset);
                float heading = ship.rotation + mount.localRotation;
                bool canFire = true;

                if (hasAim)
                {
                    float2 toTarget = aimPoint - pivot;
                    heading = math.atan2(-toTarget.x, toTarget.y);
                    if (requireRange)
                    {
                        float range = mount.spec.EffectiveRange;
                        canFire = math.lengthsq(toTarget) <= range * range;
                    }
                }

                mount.aimRotation = heading;
                float2 origin = pivot + Rotate(
                    mount.localMuzzle, heading - (ship.rotation + mount.localRotation));

                // Out-of-range AI ports tick as released so they do not burn a firing frame.
                bool triggerHeld = (fireHeld || autofire) && canFire;
                if (!WeaponFireControl.Tick(ref state, in mount.spec, triggerHeld, false))
                    continue;

                for (int burstIndex = 0; burstIndex < mount.spec.ShotsPerVolley; burstIndex++)
                {
                    float recoil = WeaponFireControl.RecoilRadians(in mount.spec, state.shotCount);
                    state.shotCount++;

                    ProjectileData projectile = WeaponFireControl.CreateProjectile(
                        in mount.spec, origin, heading, burstIndex, recoil, ship.factionId);
                    if (!projectile.isAlive || !TrySpawn(in projectile)) break;
                    spawned++;
                }
            }

            ShotsFired += spawned;
            return spawned;
        }

        private bool TrySpawn(in ProjectileData projectile)
        {
            int count = _projectiles.ProjectileCount;
            for (int i = 0; i < count; i++)
            {
                if (_projectiles.GetProjectile(i).isAlive) continue;
                _projectiles.SetProjectile(i, in projectile);
                return true;
            }

            if (count >= _projectiles.MaxProjectiles) return false;

            _projectiles.SetProjectile(count, in projectile);
            _projectiles.ProjectileCount = count + 1;
            return true;
        }

        /// <summary>Hull-space port position using +X starboard and +Y forward.</summary>
        private static float2 PortPosition(in ShipEntity ship, float2 localOffset)
        {
            return ship.position + Rotate(localOffset, ship.rotation);
        }

        private static float2 Rotate(float2 value, float radians)
        {
            float cos = math.cos(radians);
            float sin = math.sin(radians);
            return new float2(
                (cos * value.x) - (sin * value.y),
                (sin * value.x) + (cos * value.y));
        }
    }
}
