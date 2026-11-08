using System;
using System.Collections.Generic;
using ABRevival.Core.Data;
using Unity.Mathematics;

namespace ABRevival.Core.Simulation
{
    /// <summary>
    /// Mission outcome state. Only <see cref="InProgress"/> is non-terminal; the runner enters
    /// <see cref="Win"/> or <see cref="Lose"/> at most once per mission.
    /// </summary>
    public enum MissionState
    {
        InProgress = 0,
        Win = 1,
        Lose = 2
    }

    /// <summary>
    /// Which decoded objective ended the mission.
    /// Mapping to the frozen AS3 baseline (f4.evts.EvtCtrl) is documented on <see cref="MissionRunner"/>.
    /// </summary>
    public enum MissionEndCause
    {
        None = 0,
        /// <summary><c>windir</c> finish act: EvtCtrl.win() -> Win.</summary>
        WindirAct,
        /// <summary><c>lose</c> finish act: EvtCtrl.loseNow() -> Lose.</summary>
        LoseAct,
        /// <summary><c>delLose</c> finish act: EvtCtrl.delAllEvtAddLose() -> Lose.</summary>
        DelLoseAct,
        /// <summary>Player craft destroyed without a level-scripted loss objective -> Lose.</summary>
        PlayerDestroyed
    }

    /// <summary>
    /// Terminal mission result and the objective that produced it.
    /// <see cref="objective"/> is the level event name (or pilot name for an implicit player loss).
    /// </summary>
    public readonly struct MissionResult
    {
        public readonly MissionState state;
        public readonly MissionEndCause cause;
        public readonly string objective;
        public readonly int tick;

        public MissionResult(MissionState state, MissionEndCause cause, string objective, int tick)
        {
            this.state = state;
            this.cause = cause;
            this.objective = objective;
            this.tick = tick;
        }
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

    /// <summary>One toPlace_* transition fired when the named pilot entered its activation rect.</summary>
    public sealed class MissionPlaceRecord
    {
        public int tick;
        public string eventName;
        public string pilot;
        public float x;
        public float y;
        public float width;
        public float height;
    }

    /// <summary>
    /// Headless mission director. Replays a decoded level's spawn events against
    /// CombatSimulation through its public API. Time is measured in simulation ticks,
    /// so identical level data and identical external triggers produce identical output.
    /// <para>
    /// Objective mapping, traced to the frozen AS3 baseline (f4.evts.EvtCtrl):
    /// <list type="bullet">
    /// <item><c>win</c> event (<see cref="LevelFinishKind.Win"/>): completes when no living hostile remains
    /// (EvtCtrl.runWinEvt checks efa1.num == 0 and outa1.num == 0).</item>
    /// <item><c>win1</c> event (<see cref="LevelFinishKind.Win1"/>): completes when exactly one living hostile remains
    /// (EvtCtrl.runWin1Evt checks efa1.num == 1 and outa1.num == 0).</item>
    /// <item><c>windir</c> finish act: enters <see cref="MissionState.Win"/> (EvtCtrl.win()).</item>
    /// <item><c>lose</c> finish act: enters <see cref="MissionState.Lose"/> (EvtCtrl.loseNow()).</item>
    /// <item><c>delLose</c> finish act: enters <see cref="MissionState.Lose"/>. The frozen baseline clears every event and
    /// schedules a 5 s clock whose finish act is <c>lose</c> (EvtCtrl.delAllEvtAddLose); the headless runner applies the
    /// loss immediately and skips the delay.</item>
    /// <item><c>uDead_&lt;pilot&gt;_&lt;side&gt;</c> event: EvtCtrl.uDeadAct selects the specific event first and falls back
    /// to <c>uDead_all_&lt;side&gt;</c> only when no specific event is active, increments its kill counter when the killer
    /// matches <c>fireName</c>, and completes at <c>num &gt;= rnum</c>; completion runs its finish acts.</item>
    /// <item>Player craft destroyed: port rule for levels with no scripted <c>uDead_*_P</c> loss objective
    /// (story 0 has none); enters Lose unless a scripted death objective already ended the mission.</item>
    /// </list>
    /// Terminal states are entered at most once; later ticks and kill reports are ignored.
    /// Every terminal transition also raises the return-to-hangar intent, consumed once by the game flow through
    /// <see cref="TryConsumeReturnToHangar"/>.
    /// Active toPlace_* events are evaluated each tick and are surfaced through <see cref="PlaceTransitions"/>.
    /// Original game IP: Vargames (2011).
    /// </para>
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
            public string[] Pilots;
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
        private readonly List<MissionPlaceRecord> _places = new List<MissionPlaceRecord>();
        private readonly List<SpawnedShip> _ships = new List<SpawnedShip>();

        private int _tick;
        private bool _started;
        private MissionState _state;
        private bool _returnToHangarPending;

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

        /// <summary>toPlace_* transitions fired so far, in fire order.</summary>
        public IReadOnlyList<MissionPlaceRecord> PlaceTransitions => _places;

        /// <summary>Terminal result (state, cause, objective, tick), or null while the mission is in progress.</summary>
        public MissionResult? Result { get; private set; }

        /// <summary>
        /// Consumes the single return-to-hangar intent raised when the mission entered Win or Lose.
        /// Returns false while the mission is in progress and on every call after the first.
        /// </summary>
        public bool TryConsumeReturnToHangar(out MissionResult result)
        {
            if (!_returnToHangarPending)
            {
                result = default;
                return false;
            }

            _returnToHangarPending = false;
            result = Result.Value;
            return true;
        }

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
            if (_state != MissionState.InProgress) return;

            _tick++;
            _simulation.Tick(_tickSeconds);

            RunClockEvents();
            if (_state != MissionState.InProgress) return;

            RunPlaceTransitions();
            if (_state != MissionState.InProgress) return;

            RunWinConditions();
        }

        /// <summary>
        /// Reports a destroyed craft. Drives uDead_* counters and the win/win1 conditions.
        /// Callers reach this through gameplay code once the simulation exposes damage events.
        /// Ignored once the mission is terminal.
        /// </summary>
        public void NotifyCraftDestroyed(int shipIndex, string killerPilot = null)
        {
            if (_state != MissionState.InProgress) return;

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
            if (_state == MissionState.InProgress && ship.ShipIndex == PlayerShipIndex)
                EndMission(MissionState.Lose, MissionEndCause.PlayerDestroyed, ship.Pilot);
            if (_state == MissionState.InProgress) RunWinConditions();
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

                if (_state != MissionState.InProgress) return;
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
                    if (!RequiresMet(ev)) continue;

                    _activeEvents.RemoveAt(i);
                    i--;
                    RecordPlaceTransition(ev);
                    TryComplete(ev);
                    break;
                }

                if (_state != MissionState.InProgress) return;
            }
        }

        private void RunWinConditions()
        {
            // shortcut: outer-wave directives are recorded, not simulated, so win/win1 count only spawned craft.
            // The reference mission (story 0) has no outer acts; expand when a mission with outer waves needs them.
            for (int i = 0; i < _activeEvents.Count; i++)
            {
                var ev = _activeEvents[i];
                if (ev.finishKind == LevelFinishKind.None) continue;

                int hostiles = CountLivingHostiles();
                bool complete = ev.finishKind == LevelFinishKind.Win ? hostiles == 0 : hostiles == 1;
                if (!complete || !RequiresMet(ev)) continue;

                _activeEvents.RemoveAt(i);
                i--;
                TryComplete(ev);
                if (_state != MissionState.InProgress) return;
            }
        }

        private void RunDeathEvents(SpawnedShip destroyed, string killerPilot)
        {
            LevelEvent specific = null;
            LevelEvent all = null;
            for (int i = 0; i < _activeEvents.Count; i++)
            {
                var ev = _activeEvents[i];
                var trigger = ev.death;
                if (trigger == null) continue;
                if (trigger.friendlySide != destroyed.Friendly) continue;

                if (trigger.targetPilot == null)
                {
                    if (all == null) all = ev;
                }
                else if (PilotMatches(trigger.targetPilot, destroyed))
                {
                    specific = ev;
                    break;
                }
            }

            // EvtCtrl.uDeadAct: the specific event wins; uDead_all_* is used only when no specific event exists.
            var matched = specific ?? all;
            if (matched == null) return;

            var death = matched.death;
            if (!KillerMatches(death.killerPilots, killerPilot)) return;

            _deathCounts[matched] = _deathCounts.TryGetValue(matched, out int count) ? count + 1 : 1;
            if (_deathCounts[matched] < death.requiredKills) return;
            if (!RequiresMet(matched)) return;

            _activeEvents.Remove(matched);
            TryComplete(matched);
        }

        private static bool PilotMatches(string targetPilot, SpawnedShip destroyed)
        {
            for (int i = 0; i < destroyed.Pilots.Length; i++)
                if (string.Equals(destroyed.Pilots[i], targetPilot, StringComparison.Ordinal)) return true;
            return string.Equals(destroyed.Pilot, targetPilot, StringComparison.Ordinal);
        }

        private bool RequiresMet(LevelEvent ev)
        {
            for (int i = 0; i < ev.requires.Count; i++)
                if (!_finishedEvents.Contains(ev.requires[i])) return false;
            return true;
        }

        private bool TryComplete(LevelEvent ev)
        {
            if (!RequiresMet(ev)) return false;

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
                if (_state != MissionState.InProgress) break;
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
                    EndMission(MissionState.Win, MissionEndCause.WindirAct, owner);
                    break;
                case LevelActKind.Lose:
                    EndMission(MissionState.Lose, MissionEndCause.LoseAct, owner);
                    break;
                case LevelActKind.DelLose:
                    EndMission(MissionState.Lose, MissionEndCause.DelLoseAct, owner);
                    break;
            }
        }

        private void EndMission(MissionState state, MissionEndCause cause, LevelEvent objective)
        {
            EndMission(state, cause, objective != null ? objective.name : null);
        }

        /// <summary>Enters a terminal state at most once and raises the return-to-hangar intent.</summary>
        private void EndMission(MissionState state, MissionEndCause cause, string objective)
        {
            if (_state != MissionState.InProgress) return;

            _state = state;
            Result = new MissionResult(state, cause, objective, _tick);
            _returnToHangarPending = true;
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
                Pilots = spawn.pilots,
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

        private void RecordPlaceTransition(LevelEvent ev)
        {
            var place = ev.place;
            _places.Add(new MissionPlaceRecord
            {
                tick = _tick,
                eventName = ev.name,
                pilot = place.pilot,
                x = place.x,
                y = place.y,
                width = place.width,
                height = place.height
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
