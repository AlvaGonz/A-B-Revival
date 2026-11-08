using System;
using System.IO;
using NUnit.Framework;
using Unity.Mathematics;
using ABRevival.Core.Data;
using ABRevival.Core.Simulation;

namespace ABRevival.Tests
{
    /// <summary>
    /// Tier 1 parity checks for the frozen <c>FireTimer</c>/<c>FireMode</c> math and the
    /// ADR 0012 range sourced from the imported part catalog.
    /// </summary>
    [TestFixture]
    public class WeaponFireControlTests
    {
        private const float DeltaTime = 1f / 60f;

        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                dir = dir.Parent;
            }
            return dir?.FullName ?? @"C:\Dev\A&B revival";
        }

        private static string GetBinaryDataPath(string filename)
        {
            return Path.Combine(FindRepoRoot(), "src", "assets", "binaryData", filename);
        }

        private static PartDefinitionRecord LoadReferenceGun()
        {
            var records = PartCatalogParser.ParseCatalogFromFiles(
                GetBinaryDataPath("8_all.AllData_defPart.bin"),
                GetBinaryDataPath("2_all.AllData_defFire.bin"),
                GetBinaryDataPath("11_all.AllData_defBullet.bin"));

            PartDefinitionRecord gun = records.Find(p => p.partName == "G.25.X1.01");
            Assert.IsNotNull(gun, "The frozen catalog must contain G.25.X1.01.");
            return gun;
        }

        [Test]
        public void ReferenceGun_FireControlAndAdr0012Range_MatchTheFrozenCatalog()
        {
            PartDefinitionRecord gun = LoadReferenceGun();

            Assert.AreEqual(1, gun.fireNum);
            Assert.AreEqual(8, gun.fireFrame, "fireFrame");
            Assert.AreEqual(17, gun.fireStop, "fireStop");
            Assert.AreEqual(4, gun.fireEx, "fireEx");
            Assert.AreEqual(14f, gun.spreadDegrees, 0.001f, "fireAcc");

            // fire 1: spdS = 10, accS = 0; bullet 1: life = 40, dmg = 13.
            Assert.AreEqual(400f, gun.CalculateEffectiveRange(), 0.001f, "ADR 0012 display range");

            FireModeSpec spec = gun.CreateWeaponSpec();
            Assert.AreEqual(360f, spec.muzzleSpeed, 0.001f, "spdS * 36 px/s");
            Assert.AreEqual(40f / 36f, spec.lifetime, 0.0001f, "bullet life / 36 s");
            Assert.AreEqual(400f, spec.EffectiveRange, 0.001f);
            Assert.AreEqual(13f, spec.damage, 0.001f);
        }

        [Test]
        public void ReferenceGun_IntegratedFlight_TravelsTheAdr0012Range()
        {
            FireModeSpec spec = LoadReferenceGun().CreateWeaponSpec();

            var projectile = WeaponFireControl.CreateProjectile(
                in spec, float2.zero, headingRadians: 0f, burstIndex: 0, recoilRadians: 0f, factionId: 0);

            for (int step = 0; step < 10000 && projectile.isAlive; step++)
            {
                ProjectileMoveJob.Integrate(ref projectile, math.min(DeltaTime, projectile.lifetime));
            }

            Assert.IsFalse(projectile.isAlive);
            Assert.AreEqual(spec.EffectiveRange, projectile.distanceTraveled, spec.EffectiveRange * 0.005f,
                "Lifetime and muzzle speed must preserve the ADR 0012 range after unit conversion.");
        }

        [Test]
        public void FireControl_FiresInsideTheWindowAndWaitsOutTheCooldown()
        {
            FireModeSpec spec = LoadReferenceGun().CreateWeaponSpec();
            var state = default(FireControlState);

            int fireTicks = 0;
            int firstFire = -1;
            int secondFire = -1;
            int nextWindowShot = -1;

            for (int tick = 1; tick <= 60; tick++)
            {
                if (!WeaponFireControl.Tick(ref state, in spec, triggerHeld: true, autofire: false)) continue;

                fireTicks++;
                if (firstFire < 0) firstFire = tick;
                else if (secondFire < 0) secondFire = tick;

                if (tick > spec.CycleTicks && nextWindowShot < 0) nextWindowShot = tick;
            }

            // Cycle = 8 + 17 = 25 ticks; fireEx = 4. Firing phases are t % 4 == 0 with
            // t % 25 in 0..7, so the cadence is 4, 28, 32, 52, 56 inside 60 ticks.
            Assert.AreEqual(4, firstFire, "First shot lands on the first fireEx tick.");
            Assert.AreEqual(28, secondFire, "The next shot waits out the stop window.");
            Assert.AreEqual(28, nextWindowShot);
            Assert.AreEqual(5, fireTicks, "The frozen FireTimer cadence fires five times per 60 ticks.");
            Assert.AreEqual(25, spec.CycleTicks);
            Assert.AreEqual(15f, spec.ShotsPerSecond(60f), 0.001f, "One shot every fireEx ticks while firing.");
            Assert.AreEqual(17f / 60f, spec.CooldownSeconds(60f), 0.0001f);
        }

        [Test]
        public void FireControl_ReleasingTheTrigger_HoldsTheStopWindowUntilUnFireCompletes()
        {
            FireModeSpec spec = LoadReferenceGun().CreateWeaponSpec();
            var state = default(FireControlState);

            for (int tick = 1; tick <= 4; tick++)
                Assert.IsTrue(WeaponFireControl.Tick(ref state, in spec, true, false) == (tick == 4));

            // FireTimer.unFire resets the cycle after 108 idle ticks and never enters the
            // firing window while the trigger is released.
            for (int tick = 0; tick < 107; tick++)
                Assert.IsFalse(WeaponFireControl.Tick(ref state, in spec, false, false));

            Assert.IsFalse(WeaponFireControl.Tick(ref state, in spec, false, false),
                "The 108th idle tick re-arms the timer but never fires.");
            Assert.AreEqual(spec.fireFrame, state.timer, "unFire re-arms at the first stop-phase tick.");

            int heldTicks = 0;
            bool fired = false;
            while (!fired && heldTicks < spec.CycleTicks)
            {
                heldTicks++;
                fired = WeaponFireControl.Tick(ref state, in spec, true, false);
            }

            Assert.IsTrue(fired, "Holding the trigger after re-arm must fire within one cycle.");
        }

        [Test]
        public void Burst_EmitsFireNumProjectilesWithSourceRampsAndRecoil()
        {
            var spec = new FireModeSpec
            {
                burstCount = 3,
                fireFrame = 1,
                cooldownFrames = 0,
                fireEveryFrames = 1,
                spreadDegrees = 10f,
                muzzleSpeed = 360f,
                speedAdd = 36f,
                acceleration = 12f,
                accelerationAdd = 6f,
                angleAddDegrees = 2f,
                lifetime = 1f,
                damage = 5f,
                projectileType = 1
            };

            var state = default(FireControlState);
            Assert.IsTrue(WeaponFireControl.Tick(ref state, in spec, true, false));
            Assert.AreEqual(3, spec.ShotsPerVolley);

            float halfCone = math.radians(spec.spreadDegrees) * 0.5f;

            ProjectileData shot0 = BurstShot(in spec, ref state, burstIndex: 0);
            ProjectileData shot1 = BurstShot(in spec, ref state, burstIndex: 1);
            ProjectileData shot2 = BurstShot(in spec, ref state, burstIndex: 2);

            Assert.AreEqual(360f, math.length(shot0.velocity), 0.001f);
            Assert.AreEqual(396f, math.length(shot1.velocity), 0.001f);
            Assert.AreEqual(432f, math.length(shot2.velocity), 0.001f);
            Assert.AreEqual(12f, math.length(shot0.acceleration), 0.001f);
            Assert.AreEqual(18f, math.length(shot1.acceleration), 0.001f);
            Assert.AreEqual(24f, math.length(shot2.acceleration), 0.001f);

            // Heading = hull heading + burst ramp + alternating recoil.
            Assert.AreEqual(+halfCone, HeadingOf(shot0), 0.0001f);
            Assert.AreEqual((-halfCone) + math.radians(2f), HeadingOf(shot1), 0.0001f);
            Assert.AreEqual((+halfCone) + math.radians(4f), HeadingOf(shot2), 0.0001f);

            Assert.AreEqual(1f, shot0.lifetime, 0.0001f);
            Assert.AreEqual(5f, shot0.damage, 0.0001f);
        }

        [Test]
        public void Autofire_UsesTheSameTimerAsHeldFire()
        {
            FireModeSpec spec = LoadReferenceGun().CreateWeaponSpec();
            var held = default(FireControlState);
            var autofire = default(FireControlState);

            for (int tick = 1; tick <= 120; tick++)
            {
                bool heldShot = WeaponFireControl.Tick(ref held, in spec, triggerHeld: true, autofire: false);
                bool autoShot = WeaponFireControl.Tick(ref autofire, in spec, triggerHeld: false, autofire: true);
                Assert.AreEqual(heldShot, autoShot, $"Tick {tick}");
            }
        }

        private static ProjectileData BurstShot(in FireModeSpec spec, ref FireControlState state, int burstIndex)
        {
            float recoil = WeaponFireControl.RecoilRadians(in spec, state.shotCount);
            state.shotCount++;
            return WeaponFireControl.CreateProjectile(
                in spec, float2.zero, headingRadians: 0f, burstIndex, recoil, factionId: 0);
        }

        private static float HeadingOf(in ProjectileData projectile)
        {
            // ShipEntity.Forward(r) = (-sin r, cos r), so recover r with a negated atan2.
            return -math.atan2(projectile.velocity.x, projectile.velocity.y);
        }
    }
}
