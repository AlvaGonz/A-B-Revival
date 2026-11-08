using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using Unity.Mathematics;
using ABRevival.Core.Data;
using ABRevival.Core.Simulation;

namespace ABRevival.Tests
{
    [TestFixture]
    public class MissionDirectorTests
    {
        private const int ReplayTickLimit = 5000;

        private static LevelCatalog _catalog;

        [OneTimeSetUp]
        public void LoadFrozenLevelPayload()
        {
            _catalog = LevelDataParser.ParseFromFile(GetBinaryDataPath("10_all.AllData_levels.bin"));
        }

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

        [Test]
        public void LevelDataParser_DecodesFrozenPayload_WithDocumentedInventory()
        {
            Assert.AreEqual(38, _catalog.LevelCount);
            Assert.AreEqual(5225, _catalog.EventCount);
            Assert.AreEqual(50505, _catalog.SpawnCount);
            Assert.AreEqual(179, _catalog.AddSpawnCount);
            Assert.AreEqual(50288, _catalog.OuterSpawnCount);
            Assert.AreEqual(38, _catalog.PlayerSpawnCount);
            Assert.AreEqual(5120, _catalog.TimedEventCount);
            Assert.AreEqual(68, _catalog.TalkHookCount);
            // The frozen level payload carries no movie acts; the model keeps an explicit movie hook slot for story data.
            Assert.AreEqual(0, _catalog.MovieHookCount);
            Assert.AreEqual(8, _catalog.PlaceTransitionCount);
            Assert.AreEqual(69, _catalog.DeathEventCount);
            Assert.AreEqual(27, _catalog.WinEventCount);
            Assert.AreEqual(50297, _catalog.FinishActCount);
        }

        [Test]
        public void LevelDataParser_StoryZero_ExposesTimedEventsPlaceDeathAndFinishActs()
        {
            var level = _catalog.GetLevel(0);

            Assert.AreEqual(1, level.storyKey);
            Assert.AreEqual("BG1", level.background);
            Assert.AreEqual(50000f, level.cameraWidth);
            Assert.AreEqual(50000f, level.cameraHeight);
            Assert.AreEqual(4, level.events.Count);
            Assert.AreEqual(27, level.spawns.Count);

            var clockZero = FindEvent(level, "clock_0");
            Assert.IsTrue(clockZero.isTimed);
            Assert.AreEqual(5, clockZero.timeTicks);
            Assert.AreEqual(1, clockZero.finishActs.Count);
            Assert.AreEqual(LevelActKind.Talk, clockZero.finishActs[0].kind);
            Assert.AreEqual(11, clockZero.finishActs[0].talk.firstLine);
            Assert.AreEqual(15, clockZero.finishActs[0].talk.lastLine);

            var place = FindEvent(level, "toPlace_Wang");
            Assert.AreEqual("clock_0", place.after);
            Assert.IsNotNull(place.place);
            Assert.AreEqual("Wang", place.place.pilot);
            Assert.AreEqual(12860f, place.place.x, 0.001f);
            Assert.AreEqual(9860f, place.place.y, 0.001f);
            Assert.AreEqual(280f, place.place.width, 0.001f);
            Assert.AreEqual(280f, place.place.height, 0.001f);
            Assert.AreEqual(22, place.finishActs.Count);
            Assert.AreEqual(21, CountKind(place.finishActs, LevelActKind.Add));
            Assert.AreEqual(1, CountKind(place.finishActs, LevelActKind.Talk));

            var windir = FindEvent(level, "clock_2");
            Assert.AreEqual(108, windir.timeTicks);
            Assert.AreEqual(1, windir.finishActs.Count);
            Assert.AreEqual(LevelActKind.Windir, windir.finishActs[0].kind);

            var player = level.initialActs[0];
            Assert.AreEqual(LevelActKind.Player, player.kind);
            Assert.AreEqual("Wang", player.spawn.pilot);
            Assert.AreEqual(LevelSpawnKind.Player, player.spawn.kind);

            var captain = level.initialActs[1];
            Assert.AreEqual(LevelActKind.Add, captain.kind);
            Assert.AreEqual("Captain", captain.spawn.pilot);
            Assert.IsTrue(captain.spawn.friendly);
            Assert.AreEqual(76, captain.spawn.defIndex);

            var death = FindEvent(_catalog.GetLevel(1), "uDead_Michael_P");
            Assert.IsNotNull(death.death);
            Assert.AreEqual("Michael", death.death.targetPilot);
            Assert.IsTrue(death.death.friendlySide);
            Assert.AreEqual(1, death.death.requiredKills);
            Assert.AreEqual("all", death.death.killerPilots[0]);
        }

        [Test]
        public void MissionRunner_StoryZero_ReplayIsDeterministic()
        {
            var first = ReplayStoryZero();
            var second = ReplayStoryZero();

            Assert.AreEqual(MissionState.Win, first.State, "Story 0 must finish through its windir act.");
            Assert.AreEqual(27, first.SpawnCount);
            Assert.AreEqual(3, first.HookCount);
            Assert.AreEqual(2618, first.CompletionTick);
            Assert.AreEqual(first.Signature, second.Signature, "Identical input must produce identical spawn order and timestamps.");

            Assert.AreEqual(LevelSpawnKind.Player, first.Spawns[0].kind);
            Assert.AreEqual("Wang", first.Spawns[0].pilot);
            Assert.AreEqual(0, first.Spawns[0].tick);
            Assert.AreEqual("Captain", first.Spawns[1].pilot);
            Assert.IsTrue(first.Spawns[1].friendly);
            Assert.AreEqual(0, first.Spawns[1].tick);

            int placeSpawnTick = first.Spawns[6].tick;
            Assert.AreEqual(1788, placeSpawnTick);
            for (int i = 6; i < 27; i++)
            {
                Assert.AreEqual(placeSpawnTick, first.Spawns[i].tick);
                Assert.AreEqual("toPlace_Wang", first.Spawns[i].eventName);
                Assert.IsFalse(first.Spawns[i].friendly);
            }
        }

        [Test]
        public void MissionRunner_StoryZero_Win_EmitsReturnToHangarOnce()
        {
            using var simulation = new CombatSimulation();
            simulation.Init(maxShips: 64, maxProjectiles: 16);

            var runner = new MissionRunner(_catalog, 0, simulation);
            runner.Start();
            Assert.AreEqual(MissionState.InProgress, runner.State);
            Assert.IsFalse(runner.Result.HasValue);
            Assert.IsFalse(runner.TryConsumeReturnToHangar(out _));

            FlyPlayerEast(simulation, runner);
            for (int i = 0; i < ReplayTickLimit && runner.State == MissionState.InProgress; i++)
                runner.Tick();

            Assert.AreEqual(MissionState.Win, runner.State);
            Assert.IsTrue(runner.Result.HasValue);
            Assert.AreEqual(MissionState.Win, runner.Result.Value.state);
            Assert.AreEqual(MissionEndCause.WindirAct, runner.Result.Value.cause);
            Assert.AreEqual("clock_2", runner.Result.Value.objective);
            Assert.AreEqual(2618, runner.Result.Value.tick);

            Assert.IsTrue(runner.TryConsumeReturnToHangar(out var intent));
            Assert.AreEqual(MissionState.Win, intent.state);
            Assert.IsFalse(runner.TryConsumeReturnToHangar(out _), "The return-to-hangar intent is consumable exactly once.");

            // A terminal mission cannot be entered twice: later kills and ticks change nothing.
            int frozenTick = runner.CurrentTick;
            runner.NotifyCraftDestroyed(runner.PlayerShipIndex);
            runner.NotifyCraftDestroyed(1);
            for (int i = 0; i < 120; i++) runner.Tick();

            Assert.AreEqual(MissionState.Win, runner.State);
            Assert.AreEqual(frozenTick, runner.CurrentTick);
            Assert.AreEqual(2618, runner.Result.Value.tick);
            Assert.IsFalse(runner.TryConsumeReturnToHangar(out _));
        }

        [Test]
        public void MissionRunner_StoryZero_Lose_OnPlayerDestruction_EmitsReturnToHangarOnce()
        {
            using var simulation = new CombatSimulation();
            simulation.Init(maxShips: 64, maxProjectiles: 16);

            var runner = new MissionRunner(_catalog, 0, simulation);
            runner.Start();
            for (int i = 0; i < 300; i++) runner.Tick();

            Assert.AreEqual(MissionState.InProgress, runner.State);
            Assert.IsFalse(runner.Result.HasValue);

            runner.NotifyCraftDestroyed(runner.PlayerShipIndex, "Pilot XA");

            Assert.AreEqual(MissionState.Lose, runner.State);
            Assert.IsTrue(runner.Result.HasValue);
            Assert.AreEqual(MissionState.Lose, runner.Result.Value.state);
            Assert.AreEqual(MissionEndCause.PlayerDestroyed, runner.Result.Value.cause);
            Assert.AreEqual("Wang", runner.Result.Value.objective);
            Assert.AreEqual(300, runner.Result.Value.tick);

            Assert.IsTrue(runner.TryConsumeReturnToHangar(out var intent));
            Assert.AreEqual(MissionState.Lose, intent.state);
            Assert.IsFalse(runner.TryConsumeReturnToHangar(out _));

            // Lose is terminal: replaying the death or ticking does not re-enter a state.
            int frozenTick = runner.CurrentTick;
            runner.NotifyCraftDestroyed(runner.PlayerShipIndex);
            for (int i = 0; i < 120; i++) runner.Tick();

            Assert.AreEqual(MissionState.Lose, runner.State);
            Assert.AreEqual(frozenTick, runner.CurrentTick);
            Assert.AreEqual(300, runner.Result.Value.tick);
            Assert.AreEqual(MissionEndCause.PlayerDestroyed, runner.Result.Value.cause);
            Assert.IsFalse(runner.TryConsumeReturnToHangar(out _));
        }

        [Test]
        public void MissionRunner_StoryOne_ScriptedDeathObjective_ReachesLose()
        {
            using var simulation = new CombatSimulation();
            simulation.Init(maxShips: 64, maxProjectiles: 16);

            var runner = new MissionRunner(_catalog, 1, simulation);
            runner.Start();
            Assert.AreEqual(MissionState.InProgress, runner.State);

            // uDead_Michael_P completes on Michael's death and runs delLose.
            runner.NotifyCraftDestroyed(runner.PlayerShipIndex, "Pilot XA");

            Assert.AreEqual(MissionState.Lose, runner.State);
            Assert.AreEqual(MissionEndCause.DelLoseAct, runner.Result.Value.cause);
            Assert.AreEqual("uDead_Michael_P", runner.Result.Value.objective);
            Assert.AreEqual(0, runner.Result.Value.tick);
            Assert.IsTrue(runner.TryConsumeReturnToHangar(out _));

            runner.Tick();
            Assert.AreEqual(MissionState.Lose, runner.State, "The scripted loss must not be overwritten by later ticks.");
        }

        [TestCase(LevelFinishKind.Win)]
        [TestCase(LevelFinishKind.Win1)]
        public void MissionRunner_WinEvent_EvaluatesLivingHostiles(LevelFinishKind finishKind)
        {
            using var simulation = new CombatSimulation();
            simulation.Init(maxShips: 8, maxProjectiles: 8);

            var runner = new MissionRunner(BuildWinLevel(finishKind), simulation);
            runner.Start();
            Assert.AreEqual(MissionState.InProgress, runner.State);

            // E1 dies. Win needs zero hostiles; Win1 completes as soon as exactly one remains.
            runner.NotifyCraftDestroyed(1);
            Assert.AreEqual(finishKind == LevelFinishKind.Win1 ? MissionState.Win : MissionState.InProgress, runner.State);

            runner.NotifyCraftDestroyed(2);
            Assert.AreEqual(MissionState.Win, runner.State);
            Assert.AreEqual(MissionEndCause.WindirAct, runner.Result.Value.cause);
            Assert.AreEqual(finishKind == LevelFinishKind.Win1 ? "win1" : "win", runner.Result.Value.objective);
        }

        [Test]
        public void MissionRunner_WinEvent_WaitsForRequiredObjective()
        {
            using var simulation = new CombatSimulation();
            simulation.Init(maxShips: 8, maxProjectiles: 8);

            var level = new LevelRecord { storyKey = 1, cameraWidth = 1000f, cameraHeight = 1000f, background = "test" };
            level.initialActs.Add(SpawnAct("player", LevelSpawnKind.Player, "Wang", friendly: true));
            level.initialActs.Add(SpawnAct("add", LevelSpawnKind.Add, "E1", friendly: false));

            var gate = new LevelEvent { name = "gate", isTimed = true, timeTicks = 2 };
            gate.finish.Add("gate");
            level.events.Add(gate);

            var win = new LevelEvent { name = "win", finishKind = LevelFinishKind.Win };
            win.requires.Add("gate");
            win.finishActs.Add(new LevelAct { name = "windir", kind = LevelActKind.Windir });
            level.events.Add(win);

            var runner = new MissionRunner(level, simulation);
            runner.Start();
            runner.NotifyCraftDestroyed(1);

            Assert.AreEqual(MissionState.InProgress, runner.State,
                "An unmet require must keep the win objective active, not discard it.");

            for (int i = 0; i < 5; i++) runner.Tick();
            Assert.AreEqual(MissionState.Win, runner.State);
            Assert.AreEqual("win", runner.Result.Value.objective);
        }

        [Test]
        public void MissionRunner_DeathObjective_SpecificEventWinsOverUDeadAll()
        {
            using var simulation = new CombatSimulation();
            simulation.Init(maxShips: 8, maxProjectiles: 8);

            var level = new LevelRecord { storyKey = 1, cameraWidth = 1000f, cameraHeight = 1000f, background = "test" };
            level.initialActs.Add(SpawnAct("player", LevelSpawnKind.Player, "Wang", friendly: true));
            level.events.Add(DeathEvent("uDead_Wang_P", "Wang", LevelActKind.Windir));
            level.events.Add(DeathEvent("uDead_all_P", null, LevelActKind.DelLose));

            var runner = new MissionRunner(level, simulation);
            runner.Start();
            runner.NotifyCraftDestroyed(runner.PlayerShipIndex);

            Assert.AreEqual(MissionState.Win, runner.State, "The specific uDead event must pre-empt uDead_all.");
            Assert.AreEqual("uDead_Wang_P", runner.Result.Value.objective);
        }

        private static LevelEvent DeathEvent(string name, string targetPilot, LevelActKind actKind)
        {
            var ev = new LevelEvent
            {
                name = name,
                death = new LevelDeathTrigger
                {
                    targetPilot = targetPilot,
                    friendlySide = true,
                    killerPilots = new[] { "all" },
                    requiredKills = 1
                }
            };
            ev.finishActs.Add(new LevelAct { name = actKind == LevelActKind.Windir ? "windir" : "delLose", kind = actKind });
            return ev;
        }

        private static LevelRecord BuildWinLevel(LevelFinishKind finishKind)
        {
            var level = new LevelRecord { storyKey = 1, cameraWidth = 1000f, cameraHeight = 1000f, background = "test" };
            level.initialActs.Add(SpawnAct("player", LevelSpawnKind.Player, "Wang", friendly: true));
            level.initialActs.Add(SpawnAct("add", LevelSpawnKind.Add, "E1", friendly: false));
            level.initialActs.Add(SpawnAct("add", LevelSpawnKind.Add, "E2", friendly: false));

            var win = new LevelEvent
            {
                name = finishKind == LevelFinishKind.Win ? "win" : "win1",
                finishKind = finishKind
            };
            win.finishActs.Add(new LevelAct { name = "windir", kind = LevelActKind.Windir });
            level.events.Add(win);
            return level;
        }

        private static LevelAct SpawnAct(string name, LevelSpawnKind kind, string pilot, bool friendly)
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
                    friendly = friendly
                }
            };
        }

        private static ReplayResult ReplayStoryZero()
        {
            using var simulation = new CombatSimulation();
            simulation.Init(maxShips: 64, maxProjectiles: 16);

            var runner = new MissionRunner(_catalog, 0, simulation);
            runner.Start();

            FlyPlayerEast(simulation, runner);

            for (int i = 0; i < ReplayTickLimit && runner.State == MissionState.InProgress; i++)
                runner.Tick();

            return new ReplayResult
            {
                State = runner.State,
                CompletionTick = runner.CurrentTick,
                SpawnCount = runner.Spawns.Count,
                HookCount = runner.Hooks.Count,
                Spawns = CopySpawns(runner),
                Signature = BuildSignature(runner)
            };
        }

        private static void FlyPlayerEast(CombatSimulation simulation, MissionRunner runner)
        {
            int playerIndex = runner.PlayerShipIndex;
            Assert.GreaterOrEqual(playerIndex, 0);

            // Fly east into the toPlace_Wang rectangle: forward = (-sin(r), cos(r)), so -PI/2 faces +X.
            var player = simulation.GetShip(playerIndex);
            player.rotation = -math.PI * 0.5f;
            simulation.SetShip(playerIndex, player);
            simulation.SetInput(playerIndex, new ShipInput { throttle = 1f, steering = 0f });
        }

        private static MissionSpawnRecord[] CopySpawns(MissionRunner runner)
        {
            var copy = new MissionSpawnRecord[runner.Spawns.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = runner.Spawns[i];
            return copy;
        }

        private static string BuildSignature(MissionRunner runner)
        {
            var builder = new StringBuilder();
            builder.Append("tick=").Append(runner.CurrentTick).Append(';').Append(runner.State).AppendLine();
            for (int i = 0; i < runner.Spawns.Count; i++)
            {
                var spawn = runner.Spawns[i];
                builder.Append(spawn.tick).Append('|')
                    .Append(spawn.order).Append('|')
                    .Append(spawn.kind).Append('|')
                    .Append(spawn.eventName).Append('|')
                    .Append(spawn.pilot).Append('|')
                    .Append(spawn.friendly).Append('|')
                    .Append(spawn.defIndex).Append('|')
                    .Append(spawn.x.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                    .Append(spawn.y.ToString("R", CultureInfo.InvariantCulture)).Append('|')
                    .Append(spawn.shipIndex)
                    .AppendLine();
            }
            for (int i = 0; i < runner.Hooks.Count; i++)
            {
                var hook = runner.Hooks[i];
                builder.Append(hook.tick).Append('|')
                    .Append(hook.kind).Append('|')
                    .Append(hook.firstLine).Append('|')
                    .Append(hook.lastLine).Append('|')
                    .Append(hook.movieIndex)
                    .AppendLine();
            }
            return builder.ToString();
        }

        private static LevelEvent FindEvent(LevelRecord level, string name)
        {
            for (int i = 0; i < level.events.Count; i++)
                if (level.events[i].name == name) return level.events[i];
            Assert.Fail($"Event '{name}' not found in story level {level.storyKey}.");
            return null;
        }

        private static int CountKind(System.Collections.Generic.List<LevelAct> acts, LevelActKind kind)
        {
            int count = 0;
            for (int i = 0; i < acts.Count; i++) if (acts[i].kind == kind) count++;
            return count;
        }

        private sealed class ReplayResult
        {
            public MissionState State;
            public int CompletionTick;
            public int SpawnCount;
            public int HookCount;
            public MissionSpawnRecord[] Spawns;
            public string Signature;
        }
    }
}
