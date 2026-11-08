using System;
using ABRevival.Core.Simulation;
using Unity.Mathematics;

namespace ABRevival.Presentation
{
    /// <summary>
    /// One hardpoint on a craft: the fire mode shared by its ports and the port pose in hull space.
    /// </summary>
    public struct WeaponMount
    {
        public FireModeSpec spec;

        /// <summary>Port offset in hull space: +X is starboard, +Y is forward.</summary>
        public float2 localOffset;

        /// <summary>Port heading offset in radians relative to the hull heading.</summary>
        public float localRotation;
    }

    /// <summary>
    /// Owns the fire ports of every registered craft, advances the frozen <c>FireTimer</c> cadence
    /// once per simulation tick, and spawns <see cref="ProjectileData"/> into the scheduled
    /// simulation. Recoil is the frozen <c>fireAcc</c> dispersion cone applied to each shot.
    /// All per-craft state is allocated at registration; <see cref="Tick"/> allocates nothing.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class WeaponSystem
    {
        private sealed class CraftWeapons
        {
            public WeaponMount[] Mounts;
            public FireControlState[] States;
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

            var states = new FireControlState[mounts.Length];
            _crafts[shipIndex] = new CraftWeapons
            {
                Mounts = mounts.ToArray(),
                States = states
            };
        }

        /// <summary>Advances the player craft's ports by one simulation tick.</summary>
        public int Tick(ICombatFireInput input)
        {
            if (input == null || PlayerShipIndex < 0) return 0;
            return TickCraft(PlayerShipIndex, input.FireHeld, input.Autofire);
        }

        /// <summary>Advances one craft's ports by one simulation tick.</summary>
        public int TickCraft(int shipIndex, bool fireHeld, bool autofire)
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

                if (!WeaponFireControl.Tick(ref state, in mount.spec, fireHeld, autofire))
                    continue;

                float2 origin = PortPosition(in ship, mount.localOffset);
                float heading = ship.rotation + mount.localRotation;

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
            float cos = math.cos(ship.rotation);
            float sin = math.sin(ship.rotation);
            return ship.position + new float2(
                (cos * localOffset.x) - (sin * localOffset.y),
                (sin * localOffset.x) + (cos * localOffset.y));
        }
    }
}
