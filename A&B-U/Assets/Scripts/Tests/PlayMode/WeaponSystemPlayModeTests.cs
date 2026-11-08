using System;
using NUnit.Framework;
using Unity.Mathematics;
using ABRevival.Core.Data;
using ABRevival.Core.Hud;
using ABRevival.Core.Simulation;
using ABRevival.Presentation;

namespace ABRevival.Tests.PlayMode
{
    /// <summary>
    /// End-to-end weapon path: fire ports spawn projectiles into the scheduled simulation,
    /// the PhysicsCore2D contact layer damages the specific part, HUD telemetry updates,
    /// and hull destruction reaches the mission result exactly once.
    /// </summary>
    [TestFixture]
    public class WeaponSystemPlayModeTests
    {
        private const float DeltaTime = 1f / 60f;

        // Frozen G.25.X1.01: fire 1 (spdS 10, accS 0) and bullet 1 (life 40, dmg 13).
        private static FireModeSpec ReferenceGun()
        {
            return FireModeSpec.FromCatalog(
                fireNum: 1, fireFrame: 8, fireStop: 17, fireEx: 4, fireAcc: 14f,
                spdS: 10f, spdAdd: 0f, accS: 0f, accAdd: 0f, angAdd: 0f,
                bulletLifeFrames: 40f, bulletDamage: 13f);
        }

        [Test]
        public void AutofireVolley_HitsTargetPart_DetachesIt_AndUpdatesHudTelemetry()
        {
            using var simulation = new ScheduledCombatSimulation();
            simulation.Init(maxShips: 2, maxProjectiles: 32);
            simulation.PlayerIndex = 0;
            simulation.ShipCount = 1;
            simulation.SetShip(0, new ShipEntity
            {
                position = float2.zero,
                rotation = 0f,
                mass = 100f,
                health = 1000f,
                maxHealth = 1000f,
                thrustPower = 0f,
                turnSpeed = 0f,
                drag = 0f,
                factionId = 1
            });

            using var solver = new ProjectileContactSolver(maxHulls: 2, maxPartsPerHull: 4, maxProjectiles: 32);
            int playerHull = solver.RegisterHull(new float2(0f, 100f), 0f, factionId: 0);
            solver.SetHullParts(playerHull, SinglePart(PartCategory.Gun, health: 39f, hitRadius: 20f));

            var input = new FixedFireInput { FireHeld = false, Autofire = true };
            var weapons = new WeaponSystem(simulation, simulation) { PlayerShipIndex = 0 };
            Span<WeaponMount> mounts = stackalloc WeaponMount[1];
            mounts[0] = new WeaponMount { spec = ReferenceGun(), localOffset = float2.zero, localRotation = 0f };
            weapons.RegisterCraft(0, mounts);

            var hud = new CombatHudState();
            int[] hullToShip = { 3, -1, -1, -1 };
            var handoff = new ProjectileDamageHandoff(hullToShip, playerHull, playerHullMax: 39f);
            var sink = new RecordingSink();

            bool sawDamage = false;
            for (int tick = 0; tick < 400 && sink.Count == 0; tick++)
            {
                weapons.Tick(input);
                simulation.Tick(DeltaTime);
                var contacts = solver.Step(simulation.Projectiles, DeltaTime);
                if (contacts.hitCount > 0 && solver.GetPart(playerHull, 0).health < 39f) sawDamage = true;
                handoff.Apply(solver, hud, sink);
            }

            Assert.Greater(weapons.ShotsFired, 0, "Autofire must emit projectiles into the scheduled simulation.");
            Assert.IsTrue(sawDamage, "The contact solver must reduce the specific part's health.");
            Assert.IsFalse(solver.IsPartMounted(playerHull, 0), "A lethal hit must detach the part.");
            Assert.AreEqual(1, hud.LostGuns, "Lost-part telemetry must record the destroyed gun.");
            Assert.AreEqual(1, sink.Count, "Hull destruction must be reported exactly once.");
            Assert.AreEqual(3, sink.LastShipIndex, "The hull-to-ship map must select the mission ship.");

            // Replaying the same contact step is idempotent for destruction; HUD telemetry
            // is applied once per step by the caller.
            handoff.Apply(solver, null, sink);
            Assert.AreEqual(1, sink.Count);
            Assert.AreEqual(1, hud.LostGuns);
        }

        [Test]
        public void HullDestruction_ReachesMissionResult_ExactlyOnce()
        {
            using var simulation = new ScheduledCombatSimulation();
            simulation.Init(maxShips: 4, maxProjectiles: 32);

            var level = new LevelRecord { storyKey = 1, cameraWidth = 1000f, cameraHeight = 1000f, background = "test" };
            level.initialActs.Add(SpawnAct("player", LevelSpawnKind.Player, "Wang", friendly: true, new float2(0f, -40f)));
            level.initialActs.Add(SpawnAct("add", LevelSpawnKind.Add, "E1", friendly: false, new float2(0f, 60f)));

            var win = new LevelEvent { name = "win", finishKind = LevelFinishKind.Win };
            win.finishActs.Add(new LevelAct { name = "windir", kind = LevelActKind.Windir });
            level.events.Add(win);

            var runner = new MissionRunner(level, simulation);
            runner.Start();
            simulation.PlayerIndex = runner.PlayerShipIndex;

            // Pin the hostile in place so the firing line is deterministic.
            PinShip(simulation, 1);

            using var solver = new ProjectileContactSolver(maxHulls: 4, maxPartsPerHull: 4, maxProjectiles: 32);
            int playerHull = solver.RegisterHull(simulation.GetShip(0).position, 0f, factionId: 0);
            solver.SetHullParts(playerHull, SinglePart(PartCategory.Body, health: 1000f, hitRadius: 32f));

            int hostileHull = solver.RegisterHull(simulation.GetShip(1).position, 0f, factionId: 1);
            solver.SetHullParts(hostileHull, SinglePart(PartCategory.Gun, health: 39f, hitRadius: 20f));

            var weapons = new WeaponSystem(simulation, simulation) { PlayerShipIndex = runner.PlayerShipIndex };
            Span<WeaponMount> mounts = stackalloc WeaponMount[1];
            mounts[0] = new WeaponMount { spec = ReferenceGun(), localOffset = float2.zero, localRotation = 0f };
            weapons.RegisterCraft(runner.PlayerShipIndex, mounts);

            int[] hullToShip = { -1, -1, -1, -1 };
            hullToShip[playerHull] = 0;
            hullToShip[hostileHull] = 1;

            var hud = new CombatHudState();
            var handoff = new ProjectileDamageHandoff(hullToShip, playerHull, playerHullMax: 1000f);
            var sink = new MissionSink(runner);
            var input = new FixedFireInput { FireHeld = false, Autofire = true };

            for (int tick = 0; tick < 400 && runner.State == MissionState.InProgress; tick++)
            {
                weapons.Tick(input);
                runner.Tick();
                SyncHullPoses(simulation, solver, playerHull, hostileHull);
                solver.Step(simulation.Projectiles, DeltaTime);
                handoff.Apply(solver, hud, sink);
            }

            Assert.AreEqual(MissionState.Win, runner.State, "Destroying the last hostile must complete the win objective.");
            Assert.IsTrue(runner.Result.HasValue);
            Assert.AreEqual(1, sink.Count, "Hull destruction must reach MissionRunner exactly once.");

            int resultTick = runner.Result.Value.tick;
            handoff.Apply(solver, hud, sink);
            Assert.AreEqual(1, sink.Count, "A replayed contact step must not notify twice.");
            Assert.AreEqual(resultTick, runner.Result.Value.tick);

            Assert.IsTrue(runner.TryConsumeReturnToHangar(out MissionResult result));
            Assert.AreEqual(MissionState.Win, result.state);
            Assert.IsFalse(runner.TryConsumeReturnToHangar(out _), "The return intent is consumed once.");
        }

        [Test]
        public void WeaponFirePath_60Ticks_ZeroManagedAllocations()
        {
            using var simulation = new ScheduledCombatSimulation();
            simulation.Init(maxShips: 2, maxProjectiles: 64);
            simulation.PlayerIndex = 0;
            simulation.ShipCount = 1;
            simulation.SetShip(0, new ShipEntity
            {
                position = float2.zero,
                rotation = 0f,
                mass = 100f,
                health = 1000f,
                maxHealth = 1000f,
                thrustPower = 0f,
                turnSpeed = 0f,
                drag = 0f,
                factionId = 0
            });

            using var solver = new ProjectileContactSolver(maxHulls: 2, maxPartsPerHull: 2, maxProjectiles: 64);
            int targetHull = solver.RegisterHull(new float2(0f, 5000f), 0f, factionId: 1);
            solver.SetHullParts(targetHull, SinglePart(PartCategory.Body, health: 1_000_000f, hitRadius: 20f));

            var input = new FixedFireInput { FireHeld = false, Autofire = true };
            var weapons = new WeaponSystem(simulation, simulation) { PlayerShipIndex = 0 };
            Span<WeaponMount> mounts = stackalloc WeaponMount[1];
            mounts[0] = new WeaponMount { spec = ReferenceGun(), localOffset = float2.zero, localRotation = 0f };
            weapons.RegisterCraft(0, mounts);
            var hud = new CombatHudState();
            var handoff = new ProjectileDamageHandoff(new[] { -1, -1 }, playerHull: -1, playerHullMax: 1f);

            for (int tick = 0; tick < 30; tick++) StepWeapons(weapons, simulation, solver, input, hud, handoff);

            int shotsBefore = weapons.ShotsFired;
            long allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int tick = 0; tick < 60; tick++) StepWeapons(weapons, simulation, solver, input, hud, handoff);
            long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

            Assert.Greater(weapons.ShotsFired, shotsBefore, "The measured window must exercise the firing path.");
            Assert.AreEqual(0, allocatedBytes,
                $"Weapon fire path allocated {allocatedBytes} bytes over 60 ticks. Zero-alloc contract violated.");
        }

        private static void StepWeapons(
            WeaponSystem weapons,
            ScheduledCombatSimulation simulation,
            ProjectileContactSolver solver,
            ICombatFireInput input,
            CombatHudState hud,
            ProjectileDamageHandoff handoff)
        {
            weapons.Tick(input);
            simulation.Tick(DeltaTime);
            solver.Step(simulation.Projectiles, DeltaTime);
            handoff.Apply(solver, hud, null);
        }

        private static void PinShip(ScheduledCombatSimulation simulation, int shipIndex)
        {
            var ship = simulation.GetShip(shipIndex);
            ship.thrustPower = 0f;
            ship.turnSpeed = 0f;
            ship.velocity = float2.zero;
            simulation.SetShip(shipIndex, ship);
            simulation.SetInput(shipIndex, default);
        }

        private static void SyncHullPoses(
            ScheduledCombatSimulation simulation,
            ProjectileContactSolver solver,
            int playerHull,
            int hostileHull)
        {
            solver.SetHullPose(playerHull, simulation.GetShip(0).position, 0f);
            solver.SetHullPose(hostileHull, simulation.GetShip(1).position, 0f);
        }

        private static HullPartDefinition[] SinglePart(PartCategory category, float health, float hitRadius)
        {
            return new[]
            {
                new HullPartDefinition
                {
                    part = new MountedPartData
                    {
                        partId = 1,
                        category = category,
                        health = health,
                        maxHealth = health,
                        localOffset = float2.zero
                    },
                    hitRadius = hitRadius
                }
            };
        }

        private static LevelAct SpawnAct(string name, LevelSpawnKind kind, string pilot, bool friendly, float2 position)
        {
            return new LevelAct
            {
                name = name,
                kind = kind == LevelSpawnKind.Player ? LevelActKind.Player : LevelActKind.Add,
                spawn = new LevelSpawn
                {
                    kind = kind,
                    pilot = pilot,
                    pilots = new[] { pilot },
                    friendly = friendly,
                    x = position.x,
                    y = position.y
                }
            };
        }

        private sealed class FixedFireInput : ICombatFireInput
        {
            public bool FireHeld { get; set; }
            public bool Autofire { get; set; }
        }

        private sealed class RecordingSink : ICraftDestroyedSink
        {
            public int Count { get; private set; }
            public int LastShipIndex { get; private set; } = -1;

            public void CraftDestroyed(int shipIndex)
            {
                Count++;
                LastShipIndex = shipIndex;
            }
        }

        private sealed class MissionSink : ICraftDestroyedSink
        {
            private readonly MissionRunner _runner;
            public int Count { get; private set; }

            public MissionSink(MissionRunner runner)
            {
                _runner = runner;
            }

            public void CraftDestroyed(int shipIndex)
            {
                Count++;
                _runner.NotifyCraftDestroyed(shipIndex);
            }
        }
    }
}
