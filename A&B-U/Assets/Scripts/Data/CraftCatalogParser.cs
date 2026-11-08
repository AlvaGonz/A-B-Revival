using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Mathematics;

namespace ABRevival.Core.Data
{
    /// <summary>One mounted part in a frozen <c>defFly</c> craft definition.</summary>
    public sealed class CraftPartRecord
    {
        public int partId;
        public PartCategory category;
        public float health;
        public float mass;
        public float thrust;

        /// <summary>Index into <see cref="CraftStatRecord.parts"/> of the hosting part; -1 for the hull.</summary>
        public int parentIndex;

        /// <summary>Hole index on the parent part this part occupies; -1 for the hull.</summary>
        public int socketIndex;

        /// <summary>Part mirror variant from F4DataOrg.lr (0 left, 1 right, 2 both).</summary>
        public int lr;
    }

    /// <summary>
    /// One gun socket in a frozen craft definition. The socket index is the hole entry in
    /// <c>F4DataLinks.thole</c>; the muzzle offset lives in the part's SWF symbol.
    /// </summary>
    public sealed class CraftWeaponMount
    {
        public int partId;
        public int holeIndex;

        /// <summary>Index into <see cref="CraftStatRecord.parts"/> of the gun's placement.</summary>
        public int partIndex;
    }

    /// <summary>
    /// Aggregated stats for one frozen <c>F4DataLinks</c> craft definition.
    /// Health, mass and thrust follow <c>F4ShowData.initDataRec</c>; turn is the display
    /// metric from the same class. All values are pure data, no Unity references.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class CraftStatRecord
    {
        public int defIndex;
        public string name;
        public int hullPartId;
        public string hullPartName;

        public float health;
        public float mass;
        public float thrust;
        public float turn;

        /// <summary>Legacy turn rate in radians per second (turnFly/mass at the 36 Hz baseline).</summary>
        public float turnRate;

        public CraftPartRecord[] parts = Array.Empty<CraftPartRecord>();
        public CraftWeaponMount[] weaponMounts = Array.Empty<CraftWeaponMount>();
    }

    /// <summary>
    /// Indexed craft definitions resolved from <c>3_all.AllData_defFly.bin</c>.
    /// <see cref="LevelSpawn.defIndex"/> indexes this catalog, matching EvtCtrl.add's
    /// <c>ConfigAndStory.ALLPART.defFly[defIndex]</c> lookup.
    /// </summary>
    public sealed class CraftCatalog
    {
        private readonly Dictionary<int, CraftStatRecord> _byDefIndex;
        public CraftStatRecord[] Records { get; }

        public CraftCatalog(IEnumerable<CraftStatRecord> records)
        {
            if (records == null) throw new ArgumentNullException(nameof(records));

            var list = new List<CraftStatRecord>();
            _byDefIndex = new Dictionary<int, CraftStatRecord>();
            foreach (CraftStatRecord record in records)
            {
                if (record == null || _byDefIndex.ContainsKey(record.defIndex)) continue;
                list.Add(record);
                _byDefIndex.Add(record.defIndex, record);
            }

            Records = list.ToArray();
        }

        public int Count => Records.Length;

        public CraftStatRecord Find(int defIndex)
        {
            return _byDefIndex.TryGetValue(defIndex, out CraftStatRecord record) ? record : null;
        }

        public bool TryGet(int defIndex, out CraftStatRecord record)
        {
            return _byDefIndex.TryGetValue(defIndex, out record);
        }
    }

    /// <summary>
    /// Parses the frozen craft definitions (zlib-compressed AMF3 <c>flyDef</c> graph in
    /// <c>3_all.AllData_defFly.bin</c>) and aggregates each root chassis plus its attached
    /// parts from <c>8_all.AllData_defPart.bin</c>.
    /// <para>
    /// The AMF3 graph carries part ids per socket, but not socket positions or muzzle points.
    /// Those live in the part's SWF symbol (<c>MC2F4Data.readMcData</c> reads the H/F/P marker
    /// layers); <see cref="PartMountTable"/> carries the decoded table and
    /// <see cref="CraftComposition"/> turns each record's parent/socket link into a hull-local
    /// placement.
    /// </para>
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class CraftCatalogParser
    {
        public static CraftCatalog ParseFromFiles(
            string defFlyPath,
            string partPath,
            string firePath,
            string bulletPath)
        {
            List<PartDefinitionRecord> parts = PartCatalogParser.ParseCatalogFromFiles(partPath, firePath, bulletPath);
            return Parse(File.ReadAllBytes(defFlyPath), parts);
        }

        public static CraftCatalog Parse(
            byte[] compressedDefFly,
            string partXml,
            string fireXml,
            string bulletXml)
        {
            return Parse(compressedDefFly, PartCatalogParser.ParseCatalogFromStrings(partXml, fireXml, bulletXml));
        }

        public static CraftCatalog Parse(byte[] compressedDefFly, IReadOnlyList<PartDefinitionRecord> partRecords)
        {
            if (partRecords == null) throw new ArgumentNullException(nameof(partRecords));

            var partLookup = new Dictionary<int, PartDefinitionRecord>(partRecords.Count);
            for (int i = 0; i < partRecords.Count; i++)
            {
                PartDefinitionRecord part = partRecords[i];
                if (part != null) partLookup[part.partId] = part;
            }

            byte[] payload = ZlibPayload.Inflate(compressedDefFly);
            object rootValue = new Amf3Reader(payload).ReadValue();
            var root = rootValue as Dictionary<string, object>
                ?? throw new InvalidDataException("Craft payload root is not an AMF3 object.");
            var flyDef = AsObject(root["flyDef"]);

            var keys = new List<int>(flyDef.Count);
            foreach (string key in flyDef.Keys)
            {
                if (int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                    keys.Add(index);
            }
            keys.Sort();

            var records = new List<CraftStatRecord>(keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                int defIndex = keys[i];
                records.Add(ParseCraft(defIndex, AsObject(flyDef[defIndex.ToString(CultureInfo.InvariantCulture)]), partLookup));
            }

            return new CraftCatalog(records);
        }

        private static CraftStatRecord ParseCraft(
            int defIndex,
            Dictionary<string, object> source,
            Dictionary<int, PartDefinitionRecord> partLookup)
        {
            var record = new CraftStatRecord
            {
                defIndex = defIndex,
                name = GetString(source, "name"),
                hullPartId = GetInt(source, "self", 0)
            };

            var parts = new List<CraftPartRecord>();
            var mounts = new List<CraftWeaponMount>();
            float turnFly = 0f;

            AddPart(source, record.hullPartId, -1, -1, partLookup, parts, mounts, ref turnFly);

            record.parts = parts.ToArray();
            record.weaponMounts = mounts.ToArray();

            float mass = 0f;
            float health = 0f;
            float thrust = 0f;
            for (int i = 0; i < record.parts.Length; i++)
            {
                CraftPartRecord part = record.parts[i];
                mass += part.mass;
                health += part.health;
                thrust += part.thrust;
            }

            record.mass = mass;
            record.health = health;
            // F4ShowData.initData: speed and turn are normalised by mass and scaled for display.
            record.thrust = mass > 0f ? thrust / mass * 100f : 0f;
            record.turn = mass > 0f ? turnFly / mass * (180f / math.PI) * 10f : 0f;
            // The frozen rotLimit is turnFly/mass radians per 36 Hz frame.
            record.turnRate = mass > 0f ? turnFly / mass * 36f : 0f;

            if (partLookup.TryGetValue(record.hullPartId, out PartDefinitionRecord hull))
                record.hullPartName = hull.partName;

            return record;
        }

        private static void AddPart(
            Dictionary<string, object> node,
            int partId,
            int parentIndex,
            int holeIndex,
            Dictionary<int, PartDefinitionRecord> partLookup,
            List<CraftPartRecord> parts,
            List<CraftWeaponMount> mounts,
            ref float turnFly)
        {
            if (!partLookup.TryGetValue(partId, out PartDefinitionRecord definition)) return;

            int partIndex = parts.Count;
            parts.Add(new CraftPartRecord
            {
                partId = partId,
                category = definition.category,
                health = definition.health,
                mass = definition.mass,
                thrust = definition.thrust,
                parentIndex = parentIndex,
                socketIndex = holeIndex,
                lr = definition.lr
            });
            turnFly += definition.turnFly;

            if (definition.category == PartCategory.Gun)
                mounts.Add(new CraftWeaponMount { partId = partId, holeIndex = holeIndex, partIndex = partIndex });

            if (!node.TryGetValue("thole", out object tholeValue) || tholeValue == null) return;
            var thole = AsObject(tholeValue);

            var indices = new List<int>(thole.Count);
            foreach (string key in thole.Keys)
            {
                if (int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                    indices.Add(index);
            }
            indices.Sort();

            for (int i = 0; i < indices.Count; i++)
            {
                int childHole = indices[i];
                int childPartId = ToInt(thole[childHole.ToString(CultureInfo.InvariantCulture)]);
                if (!node.TryGetValue("hole" + childHole.ToString(CultureInfo.InvariantCulture), out object childValue) || childValue == null)
                    continue;

                AddPart(AsObject(childValue), childPartId, partIndex, childHole, partLookup, parts, mounts, ref turnFly);
            }
        }

        private static Dictionary<string, object> AsObject(object value)
        {
            return value as Dictionary<string, object>
                ?? throw new InvalidDataException("Expected an AMF3 object in craft data.");
        }

        private static string GetString(Dictionary<string, object> source, string key)
        {
            return source.TryGetValue(key, out object value) && value != null
                ? Convert.ToString(value, CultureInfo.InvariantCulture)
                : null;
        }

        private static int GetInt(Dictionary<string, object> source, string key, int fallback)
        {
            return source.TryGetValue(key, out object value) && value != null ? ToInt(value) : fallback;
        }

        private static int ToInt(object value)
        {
            if (value is int intValue) return intValue;
            if (value is double doubleValue) return (int)doubleValue;
            if (value is float floatValue) return (int)floatValue;
            if (value is long longValue) return (int)longValue;
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
    }
}
