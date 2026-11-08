using System;
using System.Collections.Generic;
using ABRevival.Core.Data;
using Unity.Mathematics;

namespace ABRevival.Core.Simulation
{
    public enum MissionState
    {
        Running = 0,
        Complete = 1,
        Failed = 2
    }

    /// <summary>One spawn or outer-wave directive emitted by a mission replay.</summary>
    public sealed class MissionSpawnRecord
    {
        public int tick;
        public int order;
        public LevelSpawnKind kind;
        public string eventName;
        public string pilot;
        public bool friendly;
        public int defIndex;
        public float x;
        public float y;
        public int shipIndex = -1;
    }

    /// <summary>One talk or movie hook emitted by a mission replay.</summary>
    public sealed class MissionHookRecord
    {
        public int tick;
        public LevelActKind kind;
        public int firstLine;
        public int lastLine;
        public int movieIndex = -1;
    }

    /// <summary>
    /// Headless mission director. Replays a decoded level's spawn events against
    /// CombatSimulation through its public API. Time is measured in simulation ticks,
    /// so identical level data and identical external triggers produce identical output.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class MissionRunner
    {
        public const float DefaultTickSeconds = 1f / 60f;

        private const float DefaultMass = 100f;
        private const float DefaultHealth = 1000f;
        private const float DefaultEnergy = 500f;
        private const float DefaultThrust = 1000f;
        private const float DefaultTurnSpeed = 2f;
        private const float DefaultDrag = 0.05f;

        private sealed class TimedEventState
        {
            public LevelEvent Event;
            public int RemainingTicks;
        }

        private sealed class SpawnedShip
        {
            public int ShipIndex;
            public string Pilot;
            public bool Friendly;
            public bool Alive = true;
        }

        private readonly LevelRecord _level;
        private readonly CombatSimulation _simulation;
        private readonly float _tickSeconds;
        private readonly List<LevelEvent> _activeEvents = new List<LevelEvent>();
        private readonly List<TimedEventState> _clockEvents = new List<TimedEventState>();
        private readonly Dictionary<string, List<LevelEvent>> _childrenByParent = new Dictionary<string, List<LevelEvent>>();
        private readonly Dictionary<LevelEvent, int> _remainingLoops = new Dictionary<LevelEvent, int>();
        private readonly Dictionary<LevelEvent, int> _deathCounts = new Dictionary<LevelEvent, int>();
        private readonly HashSet<string> _finishedEvents = new HashSet<string>();
        private readonly List<MissionSpawnRecord> _spawns = new List<MissionSpawnRecord>();
        private readonly List<MissionHookRecord> _hooks = new List<MissionHookRecord>();
        private readonly List<SpawnedShip> _ships = new List<SpawnedShip>();

        private int _tick;
        private bool _started;
        private MissionState _state;

        public MissionRunner(LevelRecord level, CombatSimulation simulation, float tickSeconds = DefaultTickSeconds)
        {
            _level = level ?? throw new ArgumentNullException(nameof(level));
            _simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            if (!simulation.IsInitialized)
                throw new ArgumentException("CombatSimulation must be initialized before creating a MissionRunner.", nameof(simulation));
            if (tickSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(tickSeconds), "tickSeconds must be greater than zero.");

            _tickSeconds = tickSeconds;
            BuildEventLinks();
        }

        public MissionRunner(LevelCatalog catalog, int storyIndex, CombatSimulation simulation, float tickSeconds = DefaultTickSeconds)
            : this(catalog != null ? catalog.GetLevel(storyIndex) : throw new ArgumentNullException(nameof(catalog)), simulation, tickSeconds)
        {
        }

        public LevelRecord Level => _level;
        public int CurrentTick => _tick;
        public float TickSeconds => _tickSeconds;
        public MissionState State => _state;
        public IReadOnlyList<MissionSpawnRecord> Spawns => _spawns;
        public IReadOnlyList<MissionHookRecord> Hooks => _hooks;

        /// <summary>Simulation index of the player craft spawned by the level's player act, or -1.</summary>
        public int PlayerShipIndex { get; private set; } = -1;

        /// <summary>Spawns the level's initial acts and arms its root events at tick zero.</summary>
        public void Start()
        {
            if (_started) throw new InvalidOperationException("MissionRunner has already started.");
            _started = true;

            for (int i = 0; i < _level.initialActs.Count; i++)
                ExecuteAct(_level.initialActs[i], null);

            for (int i = 0; i < _level.events.Count; i++)
            {
                var ev = _level.events[i];
                if (string.IsNullOrEmpty(ev.after)) Activate(ev);
            }
        }

        /// <summary>Advances one simulation tick, then evaluates clocks, place transitions, and win conditions.</summary>
        public void Tick()
        {
            if (!_started) throw new InvalidOperationException("MissionRunner.Start() must be called first.");
            if (_state != MissionState.Running) return;

            _tick++;
            _simulation.Tick(_tickSeconds);

            RunClockEvents();
            if (_state != MissionState.Running) return;

            RunPlaceTransitions();
            if (_state != MissionState.Running) return;

            RunWinConditions();
        }

        /// <summary>
        /// Reports a destroyed craft. Drives uDead_* counters and the win/win1 conditions.
        /// Callers reach this through gameplay code once the simulation exposes damage events.
        /// </summary>
        public void NotifyCraftDestroyed(int shipIndex, string killerPilot = null)
        {
            SpawnedShip ship = null;
            for (int i = 0; i < _ships.Count; i++)
            {
                if (_ships[i].ShipIndex == shipIndex)
                {
                    ship = _ships[i];
                    break;
                }
            }

            if (ship == null || !ship.Alive) return;
            ship.Alive = false;
            RunDeathEvents(ship, killerPilot);
            if (_state == MissionState.Running) RunWinConditions();
        }

        private void BuildEventLinks()
        {
            for (int i = 0; i < _level.events.Count; i++)
            {
                var ev = _level.events[i];
                _remainingLoops[ev] = Math.Max(1, ev.loopTime);
                if (ev.death != null) _deathCounts[ev] = ev.death.initialKills;
                if (string.IsNullOrEmpty(ev.after)) continue;

                if (!_childrenByParent.TryGetValue(ev.after, out var children))
                {
                    children = new List<LevelEvent>();
                    _childrenByParent.Add(ev.after, children);
                }
                children.Add(ev);
            }
        }

        private void Activate(LevelEvent ev)
        {
            if (ev.isTimed)
            {
                for (int i = 0; i < _clockEvents.Count; i++)
                    if (_clockEvents[i].Event == ev) return;
                _clockEvents.Add(new TimedEventState { Event = ev, RemainingTicks = ev.timeTicks });
                return;
            }

            if (!_activeEvents.Contains(ev)) _activeEvents.Add(ev);
        }

        private void RunClockEvents()
        {
            // Match EvtCtrl.runClock: events appended by a completion start next tick, not this one.
            int count = _clockEvents.Count;
            int index = 0;
            while (index < count)
            {
                var state = _clockEvents[index];
                int remaining = state.RemainingTicks;

                for (int i = 0; i < state.Event.frameActs.Count; i++)
                {
                    var frame = state.Event.frameActs[i];
                    if (frame.interval > 0 && remaining % frame.interval == 0)
                        ExecuteAct(frame.act, state.Event);
                }

                state.RemainingTicks = remaining - 1;
                if (remaining <= 0)
                {
                    _clockEvents.RemoveAt(index);
                    count--;
                    TryComplete(state.Event);
                }
                else
                {
                    index++;
                }

                if (_state != MissionState.Running) return;
            }
        }

        private void RunPlaceTransitions()
        {
            for (int i = 0; i < _activeEvents.Count; i++)
            {
                var ev = _activeEvents[i];
                if (ev.place == null) continue;

                for (int s = 0; s < _ships.Count; s++)
                {
                    var ship = _ships[s];
                    if (!ship.Alive || ship.ShipIndex < 0 || ship.ShipIndex >= _simulation.ShipCount) continue;
                    if (!string.Equals(ship.Pilot, ev.place.pilot, StringComparison.Ordinal)) continue;

                    var entity = _simulation.GetShip(ship.ShipIndex);
                    if (entity.position.x < ev.place.x || entity.position.x > ev.place.x + ev.place.width) continue;
                    if (entity.position.y < ev.place.y || entity.position.y > ev.place.y + ev.place.height) continue;

                    _activeEvents.RemoveAt(i);
                    i--;
                    TryComplete(ev);
                    break;
                }

                if (_state != MissionState.Running) return;
            }
        }

        private void RunWinConditions()
        {
            for (int i = 0; i < _activeEvents.Count; i++)
            {
                var ev = _activeEvents[i];
                if (ev.finishKind == LevelFinishKind.None) continue;

                int hostiles = CountLivingHostiles();
                bool complete = ev.finishKind == LevelFinishKind.Win ? hostiles == 0 : hostiles == 1;
                if (!complete) continue;

                _activeEvents.RemoveAt(i);
                i--;
                TryComplete(ev);
                if (_state != MissionState.Running) return;
            }
        }

        private void RunDeathEvents(SpawnedShip destroyed, string killerPilot)
        {
            for (int i = 0; i < _activeEvents.Count; i++)
            {
                var ev = _activeEvents[i];
                var trigger = ev.death;
                if (trigger == null) continue;
                if (trigger.friendlySide != destroyed.Friendly) continue;
                if (trigger.targetPilot != null && !string.Equals(trigger.targetPilot, destroyed.Pilot, StringComparison.Ordinal))
                    continue;
                if (!KillerMatches(trigger.killerPilots, killerPilot)) continue;

                _deathCounts[ev] = _deathCounts.TryGetValue(ev, out int count) ? count + 1 : 1;
                if (_deathCounts[ev] < trigger.requiredKills) continue;

                _activeEvents.RemoveAt(i);
                i--;
                TryComplete(ev);
                if (_state != MissionState.Running) return;
            }
        }

        private bool TryComplete(LevelEvent ev)
        {
            for (int i = 0; i < ev.requires.Count; i++)
                if (!_finishedEvents.Contains(ev.requires[i])) return false;

            for (int i = 0; i < ev.finish.Count; i++) _finishedEvents.Add(ev.finish[i]);
            for (int i = 0; i < ev.finishRemoveFinished.Count; i++) _finishedEvents.Remove(ev.finishRemoveFinished[i]);
            for (int i = 0; i < ev.finishRemoveEvents.Count; i++) RemoveActiveEvent(ev.finishRemoveEvents[i]);
            for (int i = 0; i < ev.finishRemoveClock.Count; i++) RemoveClockEvent(ev.finishRemoveClock[i]);

            if (_childrenByParent.TryGetValue(ev.name, out var children))
            {
                for (int i = 0; i < children.Count; i++) Activate(children[i]);
            }

            for (int i = 0; i < ev.finishActs.Count; i++)
            {
                ExecuteAct(ev.finishActs[i], ev);
                if (_state != MissionState.Running) break;
            }

            int remaining = _remainingLoops.TryGetValue(ev, out int loops) ? loops - 1 : 0;
            _remainingLoops[ev] = remaining;
            if (!ev.always && remaining <= 0) _activeEvents.Remove(ev);
            return true;
        }

        private void ExecuteAct(LevelAct act, LevelEvent owner)
        {
            if (act == null) return;
            switch (act.kind)
            {
                case LevelActKind.Add:
                case LevelActKind.Player:
                    SpawnCraft(act.spawn, owner);
                    break;
                case LevelActKind.Outer:
                    RecordOuterWave(act.spawn, owner);
                    break;
                case LevelActKind.Talk:
                    // Talk pauses gameplay in the reference build; the runner records the hook only.
                    if (act.talk != null)
                    {
                        _hooks.Add(new MissionHookRecord
                        {
                            tick = _tick,
                            kind = LevelActKind.Talk,
                            firstLine = act.talk.firstLine,
                            lastLine = act.talk.lastLine
                        });
                    }
                    break;
                case LevelActKind.Movie:
                    _hooks.Add(new MissionHookRecord
                    {
                        tick = _tick,
                        kind = LevelActKind.Movie,
                        movieIndex = act.movie != null ? act.movie.index : -1
                    });
                    break;
                case LevelActKind.Windir:
                    _state = MissionState.Complete;
                    break;
                case LevelActKind.Lose:
                    _state = MissionState.Failed;
                    break;
            }
        }

        private void SpawnCraft(LevelSpawn spawn, LevelEvent owner)
        {
            if (spawn == null) return;
            if (_simulation.ShipCount >= _simulation.MaxShips)
                throw new InvalidOperationException(
                    $"Level spawn exceeds CombatSimulation capacity of {_simulation.MaxShips} ships.");

            var entity = new ShipEntity
            {
                position = new float2(spawn.x, spawn.y),
                velocity = float2.zero,
                rotation = 0f,
                angularVelocity = 0f,
                mass = DefaultMass,
                health = DefaultHealth,
                maxHealth = DefaultHealth,
                energy = DefaultEnergy,
                maxEnergy = DefaultEnergy,
                thrustPower = DefaultThrust,
                turnSpeed = DefaultTurnSpeed,
                drag = DefaultDrag,
                factionId = spawn.friendly ? 0 : 1,
                tacticalMode = (int)CaptainTacticalMode.Anchor
            };

            int shipIndex = _simulation.ShipCount;
            _simulation.ShipCount = shipIndex + 1;
            _simulation.SetShip(shipIndex, entity);

            if (spawn.kind == LevelSpawnKind.Player) PlayerShipIndex = shipIndex;

            _ships.Add(new SpawnedShip
            {
                ShipIndex = shipIndex,
                Pilot = spawn.pilot,
                Friendly = spawn.friendly
            });

            RecordSpawn(spawn, owner, shipIndex);
        }

        private void RecordOuterWave(LevelSpawn spawn, LevelEvent owner)
        {
            RecordSpawn(spawn, owner, -1);
        }

        private void RecordSpawn(LevelSpawn spawn, LevelEvent owner, int shipIndex)
        {
            _spawns.Add(new MissionSpawnRecord
            {
                tick = _tick,
                order = _spawns.Count,
                kind = spawn.kind,
                eventName = owner != null ? owner.name : null,
                pilot = spawn.pilot,
                friendly = spawn.friendly,
                defIndex = spawn.defIndex,
                x = spawn.x,
                y = spawn.y,
                shipIndex = shipIndex
            });
        }

        private int CountLivingHostiles()
        {
            int count = 0;
            for (int i = 0; i < _ships.Count; i++)
                if (_ships[i].Alive && !_ships[i].Friendly) count++;
            return count;
        }

        private void RemoveActiveEvent(string name)
        {
            for (int i = _activeEvents.Count - 1; i >= 0; i--)
                if (_activeEvents[i].name == name) _activeEvents.RemoveAt(i);
        }

        private void RemoveClockEvent(string name)
        {
            for (int i = _clockEvents.Count - 1; i >= 0; i--)
                if (_clockEvents[i].Event.name == name) _clockEvents.RemoveAt(i);
        }

        private static bool KillerMatches(string[] killerPilots, string killerPilot)
        {
            if (killerPilots.Length == 0) return true;
            for (int i = 0; i < killerPilots.Length; i++)
            {
                if (killerPilots[i] == "all") return true;
                if (string.Equals(killerPilots[i], killerPilot, StringComparison.Ordinal)) return true;
            }
            return false;
        }
    }
}
