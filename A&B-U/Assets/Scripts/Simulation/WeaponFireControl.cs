using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace ABRevival.Core.Simulation
{
    /// <summary>
    /// One weapon's fire-control parameters, already converted from the frozen catalog's
    /// frame units into Unity pixels and seconds.
    /// <para>
    /// The frozen <c>F4DataBullet.readFromObj</c> rescales bullet life by 60/36, so a catalog
    /// lifetime of 40 frames is 40/36 s. Projectile speed is expressed per 36 Hz frame by
    /// <c>MoveAs</c>; one frame-second is 36 px/s after the baseline 0.6 position scale.
    /// Converting both preserves the ADR 0012 range: (spdS * 36) * (life / 36) == spdS * life.
    /// </para>
    /// </summary>
    public struct FireModeSpec
    {
        /// <summary>Effective frame rate of the frozen 36 Hz simulation (ADR 0010/0013).</summary>
        public const float CatalogTicksPerSecond = 36f;

        /// <summary><c>F4DataFire.fireNum</c>: projectiles emitted by one firing frame.</summary>
        public int burstCount;

        /// <summary><c>F4DataFire.fireFrame</c>: ticks in the firing window of a cycle.</summary>
        public int fireFrame;

        /// <summary><c>F4DataFire.fireStop</c>: cooldown ticks after the firing window.</summary>
        public int cooldownFrames;

        /// <summary><c>F4DataFire.fireEx</c>: fire on every Nth tick of the firing window.</summary>
        public int fireEveryFrames;

        /// <summary><c>F4DataFire.fireAcc</c>: full dispersion cone in degrees (recoil).</summary>
        public float spreadDegrees;

        /// <summary>Muzzle speed in pixels per second.</summary>
        public float muzzleSpeed;

        /// <summary>Per-shot burst speed increment in pixels per second.</summary>
        public float speedAdd;

        /// <summary>Projectile acceleration in pixels per second squared.</summary>
        public float acceleration;

        /// <summary>Per-shot burst acceleration increment in pixels per second squared.</summary>
        public float accelerationAdd;

        /// <summary><c>F4DataFire.angAdd</c>: per-shot angle increment in degrees.</summary>
        public float angleAddDegrees;

        /// <summary>Projectile lifetime in seconds.</summary>
        public float lifetime;

        public float damage;
        public int projectileType;

        public int CycleTicks => fireFrame + cooldownFrames;
        public int ShotsPerVolley => burstCount < 1 ? 1 : burstCount;

        /// <summary>ADR 0012 range using the average muzzle speed and acceleration of a burst.</summary>
        public float EffectiveRange
        {
            get
            {
                float v0 = muzzleSpeed + (speedAdd * ShotsPerVolley * 0.5f);
                float a = acceleration + (accelerationAdd * ShotsPerVolley * 0.5f);
                return Ballistics.EffectiveRange(v0, a, lifetime);
            }
        }

        /// <summary>Shots per second while the firing window is active.</summary>
        public float ShotsPerSecond(float tickRate)
        {
            float every = fireEveryFrames < 1 ? 1f : fireEveryFrames;
            return tickRate / every;
        }

        /// <summary>Duration of the stop window in seconds.</summary>
        public float CooldownSeconds(float tickRate)
        {
            return cooldownFrames / tickRate;
        }

        /// <summary>
        /// Converts the frozen <c>F4DataFire</c>/<c>F4DataBullet</c> values into Unity units.
        /// Spd/acc are per 36 Hz frame; lifetime is in 36 Hz frames.
        /// </summary>
        public static FireModeSpec FromCatalog(
            int fireNum,
            int fireFrame,
            int fireStop,
            int fireEx,
            float fireAcc,
            float spdS,
            float spdAdd,
            float accS,
            float accAdd,
            float angAdd,
            float bulletLifeFrames,
            float bulletDamage,
            int projectileType = 1)
        {
            const float hz = CatalogTicksPerSecond;
            return new FireModeSpec
            {
                burstCount = fireNum < 1 ? 1 : fireNum,
                fireFrame = fireFrame < 1 ? 1 : fireFrame,
                cooldownFrames = fireStop < 0 ? 0 : fireStop,
                fireEveryFrames = fireEx < 1 ? 1 : fireEx,
                spreadDegrees = math.max(0f, fireAcc),
                muzzleSpeed = spdS * hz,
                speedAdd = spdAdd * hz,
                acceleration = accS * hz * hz,
                accelerationAdd = accAdd * hz * hz,
                angleAddDegrees = angAdd,
                lifetime = math.max(0f, bulletLifeFrames) / hz,
                damage = math.max(0f, bulletDamage),
                projectileType = projectileType
            };
        }
    }

    /// <summary>
    /// Per-port timer equivalent to the frozen <c>FireTimer</c>. The timer advances one
    /// simulation tick per call; a cycle is <c>fireFrame + fireStop</c> ticks long.
    /// </summary>
    public struct FireControlState
    {
        public int timer;
        public int idleTicks;
        public int shotCount;
    }

    /// <summary>
    /// Pure fire-control math traced to the frozen AS3 baseline:
    /// <list type="bullet">
    /// <item><c>FireTimer.isFireFrame</c>: fire while <c>timer % (fireFrame + fireStop) &lt; fireFrame</c>
    /// and <c>timer % fireEx == 0</c>.</item>
    /// <item><c>FireTimer.unFire</c>: when the trigger is released the cycle is held in its stop
    /// window; after 108 ticks the weapon is ready again.</item>
    /// <item><c>FireMode.runMode</c>: one firing frame emits <c>fireNum</c> projectiles, each with
    /// its own speed/acceleration/angle increment.</item>
    /// <item><c>FireMode.fireOne</c>: each shot is offset by a random angle inside the
    /// <c>fireAcc</c> cone. The deterministic port alternates the sign instead of using
    /// <c>Math.random</c>, preserving the cone bounds without allocating.</item>
    /// </list>
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class WeaponFireControl
    {
        /// <summary><c>FireTimer.unFire</c>: 3 s at the frozen 36 Hz rate.</summary>
        public const int UnFireDelayTicks = 108;

        /// <summary>Advances one port by one simulation tick and reports a firing frame.</summary>
        public static bool Tick(ref FireControlState state, in FireModeSpec spec, bool triggerHeld, bool autofire)
        {
            int cycle = spec.CycleTicks;
            if (cycle <= 0) return false;

            if (triggerHeld || autofire)
            {
                state.idleTicks = 0;
                state.timer++;

                int phase = state.timer % cycle;
                return phase < spec.fireFrame && state.timer % spec.fireEveryFrames == 0;
            }

            // FireTimer.unFire: clamp the timer into the stop window, then release after 108 ticks.
            if (state.idleTicks < UnFireDelayTicks && state.timer % cycle >= spec.fireFrame)
            {
                state.idleTicks = UnFireDelayTicks - 1;
            }

            state.idleTicks++;
            if (state.idleTicks == UnFireDelayTicks)
            {
                state.timer = spec.fireFrame;
            }
            else if (state.idleTicks > UnFireDelayTicks)
            {
                state.timer++;
                if (state.timer % cycle == 0) state.timer--;
            }

            return false;
        }

        /// <summary>
        /// Recoil dispersion for one shot: the frozen cone half-angle, alternating sign per shot.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RecoilRadians(in FireModeSpec spec, int shotIndex)
        {
            float half = math.radians(math.max(0f, spec.spreadDegrees)) * 0.5f;
            return (shotIndex & 1) == 0 ? half : -half;
        }

        /// <summary>
        /// Builds the projectile for one shot of a burst. Speed, acceleration, and heading ramp
        /// with the burst index exactly as <c>FireMode.runMode</c> does.
        /// </summary>
        public static ProjectileData CreateProjectile(
            in FireModeSpec spec,
            float2 origin,
            float headingRadians,
            int burstIndex,
            float recoilRadians,
            int factionId)
        {
            float speed = spec.muzzleSpeed + (burstIndex * spec.speedAdd);
            float acceleration = spec.acceleration + (burstIndex * spec.accelerationAdd);
            float heading = headingRadians + math.radians(burstIndex * spec.angleAddDegrees) + recoilRadians;
            float2 direction = ShipEntity.Forward(heading);

            return new ProjectileData
            {
                position = origin,
                velocity = direction * speed,
                acceleration = direction * acceleration,
                lifetime = spec.lifetime,
                distanceTraveled = 0f,
                damage = spec.damage,
                factionId = factionId,
                projectileType = spec.projectileType,
                isAlive = spec.lifetime > 0f
            };
        }
    }
}
