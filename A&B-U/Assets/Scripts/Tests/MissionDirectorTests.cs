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

            Assert.AreEqual(MissionState.Complete, first.State, "Story 0 must finish through its windir act.");
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

        private static ReplayResult ReplayStoryZero()
        {
            using var simulation = new CombatSimulation();
            simulation.Init(maxShips: 64, maxProjectiles: 16);

            var runner = new MissionRunner(_catalog, 0, simulation);
            runner.Start();

            int playerIndex = runner.PlayerShipIndex;
            Assert.GreaterOrEqual(playerIndex, 0);

            // Fly east into the toPlace_Wang rectangle: forward = (-sin(r), cos(r)), so -PI/2 faces +X.
            var player = simulation.GetShip(playerIndex);
            player.rotation = -math.PI * 0.5f;
            simulation.SetShip(playerIndex, player);
            simulation.SetInput(playerIndex, new ShipInput { throttle = 1f, steering = 0f });

            for (int i = 0; i < ReplayTickLimit && runner.State == MissionState.Running; i++)
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
