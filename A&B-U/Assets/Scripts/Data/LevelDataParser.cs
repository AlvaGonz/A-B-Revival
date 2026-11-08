using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ABRevival.Core.Data
{
    /// <summary>Act kinds emitted by the frozen level scripting payload.</summary>
    public enum LevelActKind
    {
        Other = 0,
        Add,
        Outer,
        Player,
        Talk,
        Movie,
        AimPlayer,
        AimUnit,
        Camera,
        God,
        Kill,
        Windir,
        Lose,
        GiveItem,
        NoHit,
        TempMove,
        Mode,
        CleanEnemies,
        Help,
        DelLose,
        SPointToWho,
        SPointToItem,
        Fire,
        FireAtB,
        Money
    }

    public enum LevelSpawnKind
    {
        Add = 0,
        Outer = 1,
        Player = 2
    }

    public enum LevelFinishKind
    {
        None = 0,
        Win,
        Win1
    }

    /// <summary>
    /// One craft spawn or outer-wave directive decoded from a level act.
    /// Level data carries identity and placement only; combat stats come from the craft catalog.
    /// </summary>
    public sealed class LevelSpawn
    {
        public LevelSpawnKind kind;
        public string pilot;
        public string[] pilots = Array.Empty<string>();
        public bool friendly;
        public int defIndex;
        public int hue;
        public float x;
        public float y;
        public string aimTarget;
        public bool ai;
        public int aiData;

        // Player-only fields.
        public bool playerDef;
        public int playerIndex;
        public int playerMode;

        // Outer-wave-only fields.
        public int outerMax;
        public int outerLimit;
        public int outerFormation;
        public int outerSpread;
        public int outerFe;
    }

    /// <summary>Talk range [first, last, place] from the frozen dialogue index.</summary>
    public sealed class LevelTalkHook
    {
        public int firstLine;
        public int lastLine;
        public int place = -1;
    }

    /// <summary>Movie hook. The frozen level payload contains none; movie timing lives in the story index.</summary>
    public sealed class LevelMovieHook
    {
        public int index;
    }

    /// <summary>Activation rectangle for a toPlace_* event, stored in top-left form.</summary>
    public sealed class LevelPlaceTransition
    {
        public string pilot;
        public float x;
        public float y;
        public float width;
        public float height;

        public float CenterX => x + width * 0.5f;
        public float CenterY => y + height * 0.5f;
    }

    /// <summary>Kill counter for a uDead_* event.</summary>
    public sealed class LevelDeathTrigger
    {
        public string targetPilot;
        public bool friendlySide;
        public string[] killerPilots = Array.Empty<string>();
        public int requiredKills;
        public int initialKills;
    }

    /// <summary>One [name, data] act from the level script.</summary>
    public sealed class LevelAct
    {
        public string name;
        public LevelActKind kind;
        public LevelSpawn spawn;
        public LevelTalkHook talk;
        public LevelMovieHook movie;
    }

    /// <summary>One [actName, interval, data] frame act of a timed event.</summary>
    public sealed class LevelFrameAct
    {
        public string name;
        public int interval;
        public LevelAct act;
    }

    /// <summary>
    /// Explicit level event model: timed events, acts, hooks, place transitions, deaths, and finish conditions.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class LevelEvent
    {
        public string name;
        public string after;
        public bool isTimed;
        public int timeTicks;
        public int loopTime = 1;
        public bool always;

        public List<string> requires = new List<string>();
        public List<string> finish = new List<string>();
        public List<string> finishRemoveFinished = new List<string>();
        public List<string> finishRemoveEvents = new List<string>();
        public List<string> finishRemoveClock = new List<string>();

        public List<LevelAct> finishActs = new List<LevelAct>();
        public List<LevelFrameAct> frameActs = new List<LevelFrameAct>();

        public LevelPlaceTransition place;
        public LevelDeathTrigger death;
        public LevelFinishKind finishKind;
    }

    /// <summary>One story-level record decoded from the frozen level payload.</summary>
    public sealed class LevelRecord
    {
        public int storyKey;
        public float cameraWidth;
        public float cameraHeight;
        public string background = string.Empty;
        public LevelTalkHook talk;

        public List<LevelAct> initialActs = new List<LevelAct>();
        public List<LevelEvent> events = new List<LevelEvent>();

        /// <summary>All spawn and outer-wave acts in data order (initial acts, then event acts).</summary>
        public List<LevelSpawn> spawns = new List<LevelSpawn>();

        public int StoryIndex => storyKey - 1;
    }

    /// <summary>
    /// Entire decoded mission catalog. Story index N maps to root key N + 1, matching
    /// Targets.initEvts (Fly4UI.LVDATA["" + (STORY + 1)]).
    /// Frozen payload inventory: 38 levels, 5,225 events, 50,505 spawn acts
    /// (179 add, 50,288 outer, 38 player), 5,120 timed events, 68 talk hooks,
    /// 8 place transitions, 69 death events, 27 win events, 50,297 finish acts.
    /// </summary>
    public sealed class LevelCatalog
    {
        public List<LevelRecord> levels = new List<LevelRecord>();

        public int LevelCount => levels.Count;

        public int EventCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < levels.Count; i++) count += levels[i].events.Count;
                return count;
            }
        }

        public int SpawnCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < levels.Count; i++) count += levels[i].spawns.Count;
                return count;
            }
        }

        public int AddSpawnCount => CountSpawns(LevelSpawnKind.Add);
        public int OuterSpawnCount => CountSpawns(LevelSpawnKind.Outer);
        public int PlayerSpawnCount => CountSpawns(LevelSpawnKind.Player);

        public int TimedEventCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < levels.Count; i++)
                {
                    var events = levels[i].events;
                    for (int j = 0; j < events.Count; j++) if (events[j].isTimed) count++;
                }
                return count;
            }
        }

        public int TalkHookCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < levels.Count; i++) count += CountHooks(levels[i], true);
                return count;
            }
        }

        public int MovieHookCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < levels.Count; i++) count += CountHooks(levels[i], false);
                return count;
            }
        }

        public int PlaceTransitionCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < levels.Count; i++)
                {
                    var events = levels[i].events;
                    for (int j = 0; j < events.Count; j++) if (events[j].place != null) count++;
                }
                return count;
            }
        }

        public int DeathEventCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < levels.Count; i++)
                {
                    var events = levels[i].events;
                    for (int j = 0; j < events.Count; j++) if (events[j].death != null) count++;
                }
                return count;
            }
        }

        public int WinEventCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < levels.Count; i++)
                {
                    var events = levels[i].events;
                    for (int j = 0; j < events.Count; j++) if (events[j].finishKind != LevelFinishKind.None) count++;
                }
                return count;
            }
        }

        public int FinishActCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < levels.Count; i++)
                {
                    var events = levels[i].events;
                    for (int j = 0; j < events.Count; j++) count += events[j].finishActs.Count;
                }
                return count;
            }
        }

        public LevelRecord GetLevel(int storyIndex)
        {
            if (TryGetLevel(storyIndex, out var level)) return level;
            throw new KeyNotFoundException($"No level record for story index {storyIndex}.");
        }

        public bool TryGetLevel(int storyIndex, out LevelRecord level)
        {
            int key = storyIndex + 1;
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i].storyKey == key)
                {
                    level = levels[i];
                    return true;
                }
            }
            level = null;
            return false;
        }

        private int CountSpawns(LevelSpawnKind kind)
        {
            int count = 0;
            for (int i = 0; i < levels.Count; i++)
            {
                var spawns = levels[i].spawns;
                for (int j = 0; j < spawns.Count; j++) if (spawns[j].kind == kind) count++;
            }
            return count;
        }

        private static int CountHooks(LevelRecord level, bool talk)
        {
            int count = talk && level.talk != null ? 1 : 0;
            count += CountActHooks(level.initialActs, talk);
            for (int i = 0; i < level.events.Count; i++)
            {
                var ev = level.events[i];
                count += CountActHooks(ev.finishActs, talk);
                for (int j = 0; j < ev.frameActs.Count; j++)
                {
                    if (ev.frameActs[j].act == null) continue;
                    if (talk ? ev.frameActs[j].act.talk != null : ev.frameActs[j].act.movie != null) count++;
                }
            }
            return count;
        }

        private static int CountActHooks(List<LevelAct> acts, bool talk)
        {
            int count = 0;
            for (int i = 0; i < acts.Count; i++)
            {
                if (talk ? acts[i].talk != null : acts[i].movie != null) count++;
            }
            return count;
        }
    }

    /// <summary>
    /// Pure C# decoder for the frozen level scripting payload
    /// (10_all.AllData_levels.bin: zlib-compressed AMF3).
    /// No Unity API is used so the parser runs inside the Tier 1 dotnet test gate.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class LevelDataParser
    {
        private const string ClockMarker = "clock";
        private const string PlacePrefix = "toPlace_";
        private const string DeathPrefix = "uDead_";

        public static LevelCatalog ParseFromFile(string path)
        {
            return Parse(File.ReadAllBytes(path));
        }

        public static LevelCatalog Parse(byte[] compressed)
        {
            if (compressed == null) throw new ArgumentNullException(nameof(compressed));

            byte[] payload = InflateZlib(compressed);
            var root = new Amf3Reader(payload).ReadValue() as Dictionary<string, object>;
            if (root == null) throw new InvalidDataException("Level payload root is not an AMF3 object.");

            var keys = new List<int>();
            foreach (var key in root.Keys)
            {
                if (int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int storyKey))
                    keys.Add(storyKey);
            }
            keys.Sort();

            var catalog = new LevelCatalog();
            for (int i = 0; i < keys.Count; i++)
            {
                object levelValue = root[keys[i].ToString(CultureInfo.InvariantCulture)];
                catalog.levels.Add(ParseLevel(keys[i], AsObject(levelValue)));
            }
            return catalog;
        }

        private static LevelRecord ParseLevel(int storyKey, Dictionary<string, object> source)
        {
            var level = new LevelRecord { storyKey = storyKey };

            if (source.TryGetValue("cam", out object camValue) && camValue != null)
            {
                var cam = AsArray(camValue);
                if (cam.Count > 0) level.cameraWidth = ToFloat(cam[0]);
                if (cam.Count > 1) level.cameraHeight = ToFloat(cam[1]);
                if (cam.Count > 2) level.background = ToStringValue(cam[2]);
            }

            if (source.TryGetValue("talk", out object talkValue) && talkValue != null)
                level.talk = ParseTalkHook(talkValue);

            if (source.TryGetValue("acts", out object actsValue) && actsValue != null)
            {
                var acts = AsArray(actsValue);
                for (int i = 0; i < acts.Count; i++)
                {
                    var act = ParseAct(acts[i]);
                    level.initialActs.Add(act);
                    if (act.spawn != null) level.spawns.Add(act.spawn);
                }
            }

            if (source.TryGetValue("evts", out object eventsValue) && eventsValue != null)
            {
                var events = AsArray(eventsValue);
                for (int i = 0; i < events.Count; i++)
                {
                    var ev = ParseEvent(AsObject(events[i]));
                    level.events.Add(ev);
                    for (int j = 0; j < ev.finishActs.Count; j++)
                    {
                        if (ev.finishActs[j].spawn != null) level.spawns.Add(ev.finishActs[j].spawn);
                    }
                    for (int j = 0; j < ev.frameActs.Count; j++)
                    {
                        var act = ev.frameActs[j].act;
                        if (act != null && act.spawn != null) level.spawns.Add(act.spawn);
                    }
                }
            }

            return level;
        }

        private static LevelEvent ParseEvent(Dictionary<string, object> source)
        {
            var ev = new LevelEvent();
            ev.name = GetString(source, "name") ?? string.Empty;
            ev.after = GetStringOrNull(source, "after");
            ev.isTimed = ev.name.IndexOf(ClockMarker, StringComparison.Ordinal) >= 0;
            ev.loopTime = GetInt(source, "looptime", 1);
            ev.always = GetBool(source, "always", false);

            ReadStringList(source, "require", ev.requires);
            ReadStringList(source, "finish", ev.finish);
            ReadStringList(source, "finishRemoveCF", ev.finishRemoveFinished);
            ReadStringList(source, "finishRemove", ev.finishRemoveEvents);
            ReadStringList(source, "finishRemoveClock", ev.finishRemoveClock);

            if (source.TryGetValue("data", out object dataValue) && dataValue != null)
            {
                var data = AsObject(dataValue);

                if (data.TryGetValue("time", out object timeValue))
                {
                    ev.isTimed = true;
                    ev.timeTicks = ToInt(timeValue);
                }

                if (data.TryGetValue("rectPlace", out object rectValue) && rectValue != null)
                {
                    var rect = AsArray(rectValue);
                    if (rect.Count >= 4)
                    {
                        float width = ToFloat(rect[2]);
                        float height = ToFloat(rect[3]);
                        ev.place = new LevelPlaceTransition
                        {
                            pilot = ev.name.StartsWith(PlacePrefix, StringComparison.Ordinal)
                                ? ev.name.Substring(PlacePrefix.Length)
                                : string.Empty,
                            x = ToFloat(rect[0]) - width * 0.5f,
                            y = ToFloat(rect[1]) - height * 0.5f,
                            width = width,
                            height = height
                        };
                    }
                }

                if (ev.name.StartsWith(DeathPrefix, StringComparison.Ordinal))
                    ev.death = ParseDeathTrigger(ev.name, data);
            }

            if (ev.name == "win") ev.finishKind = LevelFinishKind.Win;
            else if (ev.name == "win1") ev.finishKind = LevelFinishKind.Win1;

            if (source.TryGetValue("finishActs", out object finishActsValue) && finishActsValue != null)
            {
                var acts = AsArray(finishActsValue);
                for (int i = 0; i < acts.Count; i++) ev.finishActs.Add(ParseAct(acts[i]));
            }

            if (source.TryGetValue("frameActs", out object frameActsValue) && frameActsValue != null)
            {
                var frameActs = AsArray(frameActsValue);
                for (int i = 0; i < frameActs.Count; i++)
                {
                    var frame = AsArray(frameActs[i]);
                    if (frame.Count < 3) continue;
                    ev.frameActs.Add(new LevelFrameAct
                    {
                        name = ToStringValue(frame[0]),
                        interval = ToInt(frame[1]),
                        act = ParseAct(ToStringValue(frame[0]), frame[2])
                    });
                }
            }

            return ev;
        }

        private static LevelDeathTrigger ParseDeathTrigger(string eventName, Dictionary<string, object> data)
        {
            string remainder = eventName.Substring(DeathPrefix.Length);
            bool friendly = remainder.EndsWith("_P", StringComparison.Ordinal);
            if (friendly || remainder.EndsWith("_E", StringComparison.Ordinal))
                remainder = remainder.Substring(0, remainder.Length - 2);

            var trigger = new LevelDeathTrigger
            {
                friendlySide = friendly,
                targetPilot = remainder == "all" ? null : remainder,
                requiredKills = GetInt(data, "rnum", 1),
                initialKills = GetInt(data, "num", 0)
            };

            if (data.TryGetValue("fireName", out object fireValue) && fireValue != null)
            {
                if (fireValue is List<object> list)
                {
                    trigger.killerPilots = new string[list.Count];
                    for (int i = 0; i < list.Count; i++) trigger.killerPilots[i] = ToStringValue(list[i]);
                }
                else
                {
                    trigger.killerPilots = new[] { ToStringValue(fireValue) };
                }
            }

            return trigger;
        }

        private static LevelAct ParseAct(object raw)
        {
            if (raw is Dictionary<string, object> actObject)
            {
                return ParseAct(GetString(actObject, "name"), actObject.TryGetValue("data", out object actData) ? actData : null);
            }

            var pair = AsArray(raw);
            if (pair.Count < 1) throw new InvalidDataException("Level act is empty.");
            string name = ToStringValue(pair[0]);
            object data = pair.Count > 1 ? pair[1] : null;
            return ParseAct(name, data);
        }

        private static LevelAct ParseAct(string name, object data)
        {
            var act = new LevelAct { name = name, kind = ClassifyAct(name) };
            if (data == null) return act;

            switch (act.kind)
            {
                case LevelActKind.Add:
                    act.spawn = ParseAddSpawn(AsObject(data));
                    break;
                case LevelActKind.Outer:
                    act.spawn = ParseOuterSpawn(AsObject(data));
                    break;
                case LevelActKind.Player:
                    act.spawn = ParsePlayerSpawn(AsObject(data));
                    break;
                case LevelActKind.Talk:
                    act.talk = ParseTalkHook(data);
                    break;
                case LevelActKind.Movie:
                    act.movie = new LevelMovieHook { index = ToInt(data) };
                    break;
            }

            return act;
        }

        private static LevelSpawn ParseAddSpawn(Dictionary<string, object> data)
        {
            string[] pilots = GetStringList(data, "pilot");
            return new LevelSpawn
            {
                kind = LevelSpawnKind.Add,
                pilot = pilots.Length > 0 ? pilots[0] : string.Empty,
                pilots = pilots,
                friendly = GetBool(data, "friend", false),
                defIndex = GetInt(data, "defIndex", 0),
                hue = GetInt(data, "hue", 0),
                x = GetFloat(data, "xp", 0f),
                y = GetFloat(data, "yp", 0f),
                aimTarget = GetString(data, "iaim"),
                ai = GetBool(data, "ai", false),
                aiData = GetInt(data, "aidata", 0)
            };
        }

        private static LevelSpawn ParseOuterSpawn(Dictionary<string, object> data)
        {
            string[] pilots = GetStringList(data, "pilot");
            return new LevelSpawn
            {
                kind = LevelSpawnKind.Outer,
                pilot = pilots.Length > 0 ? pilots[0] : string.Empty,
                pilots = pilots,
                friendly = false,
                defIndex = GetInt(data, "defIndex", 0),
                hue = GetInt(data, "hue", 0),
                x = GetFloat(data, "xp", 0f),
                y = GetFloat(data, "yp", 0f),
                aimTarget = GetString(data, "iaim"),
                ai = GetBool(data, "ai", false),
                aiData = GetInt(data, "aidata", 0),
                outerMax = GetInt(data, "imax", 0),
                outerLimit = GetInt(data, "ilim", 0),
                outerFormation = GetInt(data, "ff", 0),
                outerSpread = GetInt(data, "sf", 0),
                outerFe = GetInt(data, "fe", 0)
            };
        }

        private static LevelSpawn ParsePlayerSpawn(Dictionary<string, object> data)
        {
            string[] pilots = GetStringList(data, "pilot");
            return new LevelSpawn
            {
                kind = LevelSpawnKind.Player,
                pilot = pilots.Length > 0 ? pilots[0] : string.Empty,
                pilots = pilots,
                friendly = true,
                defIndex = GetInt(data, "index", 0),
                hue = GetInt(data, "hue", 0),
                x = GetFloat(data, "xp", 0f),
                y = GetFloat(data, "yp", 0f),
                aimTarget = GetString(data, "iaim"),
                ai = GetBool(data, "ai", false),
                aiData = GetInt(data, "aidata", 0),
                playerDef = GetBool(data, "def", false),
                playerIndex = GetInt(data, "index", 0),
                playerMode = GetInt(data, "mode", 0)
            };
        }

        private static LevelTalkHook ParseTalkHook(object raw)
        {
            var values = AsArray(raw);
            var hook = new LevelTalkHook();
            if (values.Count > 0) hook.firstLine = ToInt(values[0]);
            if (values.Count > 1) hook.lastLine = ToInt(values[1]);
            if (values.Count > 2 && values[2] != null) hook.place = ToInt(values[2]);
            return hook;
        }

        private static LevelActKind ClassifyAct(string name)
        {
            switch (name)
            {
                case "add": return LevelActKind.Add;
                case "out": return LevelActKind.Outer;
                case "player": return LevelActKind.Player;
                case "talk": return LevelActKind.Talk;
                case "movie": return LevelActKind.Movie;
                case "aimp": return LevelActKind.AimPlayer;
                case "aimu": return LevelActKind.AimUnit;
                case "cam": return LevelActKind.Camera;
                case "god": return LevelActKind.God;
                case "kill": return LevelActKind.Kill;
                case "windir": return LevelActKind.Windir;
                case "lose": return LevelActKind.Lose;
                case "giveItem": return LevelActKind.GiveItem;
                case "nohit": return LevelActKind.NoHit;
                case "tmove": return LevelActKind.TempMove;
                case "mode": return LevelActKind.Mode;
                case "cleanE": return LevelActKind.CleanEnemies;
                case "help": return LevelActKind.Help;
                case "delLose": return LevelActKind.DelLose;
                case "spwho": return LevelActKind.SPointToWho;
                case "spitem": return LevelActKind.SPointToItem;
                case "fire": return LevelActKind.Fire;
                case "fireAB": return LevelActKind.FireAtB;
                case "money": return LevelActKind.Money;
                default: return LevelActKind.Other;
            }
        }

        private static byte[] InflateZlib(byte[] data)
        {
            if (data.Length < 6) throw new InvalidDataException("Compressed level payload is too short.");
            int cmf = data[0];
            int flg = data[1];
            if ((cmf & 0x0F) != 8) throw new InvalidDataException("Level payload is not zlib-deflated.");
            if ((cmf * 256 + flg) % 31 != 0) throw new InvalidDataException("Level payload has an invalid zlib header.");

            // Skip the two-byte zlib header and four-byte Adler-32 trailer; DeflateStream reads the raw stream.
            using (var payload = new MemoryStream(data, 2, data.Length - 6, writable: false))
            using (var deflate = new DeflateStream(payload, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                deflate.CopyTo(output);
                return output.ToArray();
            }
        }

        private static Dictionary<string, object> AsObject(object value)
        {
            return value as Dictionary<string, object>
                ?? throw new InvalidDataException("Expected an AMF3 object.");
        }

        private static List<object> AsArray(object value)
        {
            return value as List<object>
                ?? throw new InvalidDataException("Expected an AMF3 array.");
        }

        private static string GetString(Dictionary<string, object> source, string key)
        {
            return source.TryGetValue(key, out object value) && value != null ? ToStringValue(value) : null;
        }

        private static string GetStringOrNull(Dictionary<string, object> source, string key)
        {
            string value = GetString(source, key);
            return string.IsNullOrEmpty(value) ? null : value;
        }

        private static int GetInt(Dictionary<string, object> source, string key, int fallback)
        {
            return source.TryGetValue(key, out object value) && value != null ? ToInt(value) : fallback;
        }

        private static float GetFloat(Dictionary<string, object> source, string key, float fallback)
        {
            return source.TryGetValue(key, out object value) && value != null ? ToFloat(value) : fallback;
        }

        private static bool GetBool(Dictionary<string, object> source, string key, bool fallback)
        {
            return source.TryGetValue(key, out object value) && value is bool flag ? flag : fallback;
        }

        private static string[] GetStringList(Dictionary<string, object> source, string key)
        {
            if (!source.TryGetValue(key, out object value) || value == null) return Array.Empty<string>();
            if (value is List<object> list)
            {
                var values = new string[list.Count];
                for (int i = 0; i < list.Count; i++) values[i] = ToStringValue(list[i]);
                return values;
            }
            return new[] { ToStringValue(value) };
        }

        private static void ReadStringList(Dictionary<string, object> source, string key, List<string> destination)
        {
            if (!source.TryGetValue(key, out object value) || value == null) return;
            if (value is List<object> list)
            {
                for (int i = 0; i < list.Count; i++) destination.Add(ToStringValue(list[i]));
            }
            else
            {
                destination.Add(ToStringValue(value));
            }
        }

        private static string ToStringValue(object value)
        {
            if (value == null) return null;
            return value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static int ToInt(object value)
        {
            if (value is int intValue) return intValue;
            if (value is double doubleValue) return (int)doubleValue;
            if (value is float floatValue) return (int)floatValue;
            if (value is long longValue) return (int)longValue;
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private static float ToFloat(object value)
        {
            if (value is float floatValue) return floatValue;
            if (value is double doubleValue) return (float)doubleValue;
            if (value is int intValue) return intValue;
            if (value is long longValue) return longValue;
            return Convert.ToSingle(value, CultureInfo.InvariantCulture);
        }

        private readonly struct Traits
        {
            public readonly int Encoding;
            public readonly string[] Names;

            public Traits(int encoding, string[] names)
            {
                Encoding = encoding;
                Names = names;
            }
        }

        /// <summary>Minimal AMF3 reader covering the markers used by the frozen level payload.</summary>
        private sealed class Amf3Reader
        {
            private readonly byte[] _data;
            private readonly List<string> _strings = new List<string>();
            private readonly List<object> _objects = new List<object>();
            private readonly List<Traits> _traits = new List<Traits>();
            private int _position;

            public Amf3Reader(byte[] data)
            {
                _data = data;
            }

            public object ReadValue()
            {
                int marker = ReadByte();
                switch (marker)
                {
                    case 0x00: // undefined
                    case 0x01: // null
                        return null;
                    case 0x02: return false;
                    case 0x03: return true;
                    case 0x04: return ReadInt();
                    case 0x05: return ReadDouble();
                    case 0x06: return ReadString();
                    case 0x09: return ReadArray();
                    case 0x0A: return ReadObject();
                    default:
                        throw new NotSupportedException(
                            $"AMF3 marker 0x{marker:X2} at offset {_position - 1} is not present in level data.");
                }
            }

            private object ReadArray()
            {
                uint value = ReadU29();
                if ((value & 1) == 0) return _objects[(int)(value >> 1)];

                int count = (int)(value >> 1);
                var list = new List<object>(count);
                _objects.Add(list);

                // AMF3 arrays put associative entries before the dense entries; level data has none.
                string key = ReadString();
                if (key.Length != 0)
                    throw new NotSupportedException("Associative AMF3 arrays are not used by level data.");

                for (int i = 0; i < count; i++) list.Add(ReadValue());
                return list;
            }

            private object ReadObject()
            {
                uint value = ReadU29();
                if ((value & 1) == 0) return _objects[(int)(value >> 1)];

                value >>= 1;
                Traits traits;
                if ((value & 1) != 0)
                {
                    value >>= 1;
                    ReadString(); // class name; anonymous in this payload
                    int encoding = (int)(value & 3);
                    int sealedCount = (int)(value >> 2);
                    var names = new string[sealedCount];
                    for (int i = 0; i < sealedCount; i++) names[i] = ReadString();
                    traits = new Traits(encoding, names);
                    _traits.Add(traits);
                }
                else
                {
                    traits = _traits[(int)(value >> 1)];
                }

                if (traits.Encoding != 0 && traits.Encoding != 2)
                    throw new NotSupportedException("Externalizable AMF3 objects are not used by level data.");

                var result = new Dictionary<string, object>(traits.Names.Length + 4);
                _objects.Add(result);

                for (int i = 0; i < traits.Names.Length; i++)
                    result[traits.Names[i]] = ReadValue();

                if (traits.Encoding == 2)
                {
                    while (true)
                    {
                        string key = ReadString();
                        if (key.Length == 0) break;
                        result[key] = ReadValue();
                    }
                }

                return result;
            }

            private string ReadString()
            {
                uint value = ReadU29();
                if ((value & 1) == 0) return _strings[(int)(value >> 1)];

                int length = (int)(value >> 1);
                if (length == 0) return string.Empty;

                string result = Encoding.UTF8.GetString(_data, _position, length);
                _position += length;
                _strings.Add(result);
                return result;
            }

            private int ReadInt()
            {
                uint value = ReadU29();
                if ((value & 0x10000000u) != 0) return (int)(value | 0xE0000000u);
                return (int)value;
            }

            private double ReadDouble()
            {
                var bytes = new byte[8];
                Array.Copy(_data, _position, bytes, 0, bytes.Length);
                _position += bytes.Length;
                if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
                return BitConverter.ToDouble(bytes, 0);
            }

            private uint ReadU29()
            {
                uint value = 0;
                for (int i = 0; i < 3; i++)
                {
                    byte next = ReadByte();
                    if ((next & 0x80) != 0)
                    {
                        value = (value << 7) | (uint)(next & 0x7F);
                    }
                    else
                    {
                        return (value << 7) | next;
                    }
                }
                return (value << 8) | ReadByte();
            }

            private byte ReadByte()
            {
                if (_position >= _data.Length)
                    throw new EndOfStreamException("Unexpected end of AMF3 level payload.");
                return _data[_position++];
            }
        }
    }
}
