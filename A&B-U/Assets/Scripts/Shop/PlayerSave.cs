using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using ABRevival.Core.Camera;
using ABRevival.Core.Data;
using ABRevival.Core.Simulation;

namespace ABRevival.Core.Persistence
{
    /// <summary>
    /// Single serialized payload for player progression. New systems (for example
    /// story progression in ticket #156) extend this payload instead of adding a
    /// second save format.
    /// </summary>
    [DataContract]
    public sealed class PlayerSaveData
    {
        /// <summary>v2 adds the six Options fields plus the battle control mode.</summary>
        public const int CurrentVersion = 2;

        [DataMember(Order = 0)] public int version = CurrentVersion;
        [DataMember(Order = 1)] public ShopSaveData shop = new ShopSaveData();
        [DataMember(Order = 2)] public List<BlueprintSaveData> blueprints = new List<BlueprintSaveData>();

        /// <summary>Campaign index (the frozen release's STORY). Missing members from version-1 saves default to 0.</summary>
        [DataMember(Order = 3)] public int storyIndex;

        /// <summary>ADR 0005 camera distance preset restored at boot and saved whenever C cycles it.</summary>
        [DataMember(Order = 4)] public CameraDistancePreset cameraPreset;

        /// <summary>Center (UIChooseFlyAI) selected Player craft, an index into <see cref="blueprints"/>.</summary>
        [DataMember(Order = 5)] public int playerBlueprintIndex;

        /// <summary>Center companion slots 0..4. Empty slots keep <c>craftIndex = -1</c>.</summary>
        [DataMember(Order = 6)] public List<CompanionSaveData> companions = new List<CompanionSaveData>();

        /// <summary>Battle control scheme (ticket #179); also the Options setting wired in #181.</summary>
        [DataMember(Order = 7)] public PlayerControlMode controlMode = PlayerControlMode.Keyboard;

        /// <summary>Options display cycle: 0 = 640x480, 1 = 1280x720, 2 = 1920x1080, 3 = fullscreen.</summary>
        [DataMember(Order = 8)] public int displayMode = 2;

        /// <summary>Frozen CONFIG[0] (Bloom); persisted for the 2011 options set.</summary>
        [DataMember(Order = 9)] public bool bloomEnabled = true;

        /// <summary>Frozen CONFIG[1] (Candy!); persisted for the 2011 options set.</summary>
        [DataMember(Order = 10)] public bool candyEnabled;

        /// <summary>Frozen CONFIG[5] (Background); persisted for the 2011 options set.</summary>
        [DataMember(Order = 11)] public bool backgroundEnabled = true;

        /// <summary>Frozen CONFIG[3] (Sound) drives the SFX bus.</summary>
        [DataMember(Order = 12)] public bool soundEnabled = true;

        /// <summary>Frozen CONFIG[4] (Music) drives the music bus.</summary>
        [DataMember(Order = 13)] public bool musicEnabled = true;

        /// <summary>
        /// DataContractJsonSerializer bypasses the constructor and field initialisers, so a
        /// version-1 payload that predates these members would otherwise load 640x480, silent
        /// and without bloom/background. Members present in the JSON still overwrite these.
        /// </summary>
        [OnDeserializing]
        private void InitializeDefaults(StreamingContext context)
        {
            shop = new ShopSaveData();
            blueprints = new List<BlueprintSaveData>();
            companions = new List<CompanionSaveData>();
            controlMode = PlayerControlMode.Keyboard;
            displayMode = 2;
            bloomEnabled = true;
            candyEnabled = false;
            backgroundEnabled = true;
            soundEnabled = true;
            musicEnabled = true;
        }
    }

    /// <summary>One AI companion slot from Center, mirroring the frozen COMPANIONS record.</summary>
    [DataContract]
    public struct CompanionSaveData
    {
        [DataMember(Order = 0)] public int craftIndex;
        [DataMember(Order = 1)] public int role;
        [DataMember(Order = 2)] public int leader;

        public static CompanionSaveData Empty => new CompanionSaveData
        {
            craftIndex = -1,
            role = 1,
            leader = 0
        };
    }

    [DataContract]
    public sealed class ShopSaveData
    {
        [DataMember(Order = 0)] public int money;
        [DataMember(Order = 1)] public List<ShopStockSave> stock = new List<ShopStockSave>();
        [DataMember(Order = 2)] public List<int> ownedParts = new List<int>();
    }

    [DataContract]
    public struct ShopStockSave
    {
        [DataMember(Order = 0)] public int partId;
        [DataMember(Order = 1)] public int quantity;

        /// <summary>False while the row still shows the frozen NEW badge (old saves load false).</summary>
        [DataMember(Order = 2)] public bool seen;
    }

    [DataContract]
    public sealed class BlueprintSaveData
    {
        [DataMember(Order = 0)] public string craftName = string.Empty;
        [DataMember(Order = 1)] public string hullName = string.Empty;
        [DataMember(Order = 2)] public HullCategory hullCategory;
        [DataMember(Order = 3)] public float hullMass;
        [DataMember(Order = 4)] public float hullHealth;
        [DataMember(Order = 5)] public bool isGrandfathered;
        [DataMember(Order = 6)] public List<MountedPartSaveData> mountedParts = new List<MountedPartSaveData>();
    }

    [DataContract]
    public struct MountedPartSaveData
    {
        [DataMember(Order = 0)] public int partId;
        [DataMember(Order = 1)] public PartCategory category;
        [DataMember(Order = 2)] public float health;
        [DataMember(Order = 3)] public float maxHealth;
        [DataMember(Order = 4)] public float mass;
        [DataMember(Order = 5)] public float thrust;
        [DataMember(Order = 6)] public float energyDraw;
        [DataMember(Order = 7)] public float localOffsetX;
        [DataMember(Order = 8)] public float localOffsetY;
        [DataMember(Order = 9)] public float localRotation;

        public static MountedPartSaveData FromMountedPart(MountedPartData part)
        {
            return new MountedPartSaveData
            {
                partId = part.partId,
                category = part.category,
                health = part.health,
                maxHealth = part.maxHealth,
                mass = part.mass,
                thrust = part.thrust,
                energyDraw = part.energyDraw,
                localOffsetX = part.localOffset.x,
                localOffsetY = part.localOffset.y,
                localRotation = part.localRotation
            };
        }

        public MountedPartData ToMountedPart()
        {
            return new MountedPartData
            {
                partId = partId,
                category = category,
                health = health,
                maxHealth = maxHealth,
                mass = mass,
                thrust = thrust,
                energyDraw = energyDraw,
                localOffset = new Unity.Mathematics.float2(localOffsetX, localOffsetY),
                localRotation = localRotation
            };
        }
    }

    /// <summary>
    /// Deterministic JSON codec for the single player payload. Uses
    /// DataContractJsonSerializer so the Tier 1 gate needs no extra package.
    /// </summary>
    public static class PlayerSaveCodec
    {
        public static string ToJson(PlayerSaveData save)
        {
            if (save == null) throw new ArgumentNullException(nameof(save));

            var serializer = new DataContractJsonSerializer(typeof(PlayerSaveData));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, save);
                return System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static PlayerSaveData FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Save payload is empty.", nameof(json));
            }

            var serializer = new DataContractJsonSerializer(typeof(PlayerSaveData));
            using (var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            {
                var save = serializer.ReadObject(stream) as PlayerSaveData;
                if (save == null)
                {
                    throw new InvalidDataException("Save payload could not be deserialized.");
                }

                if (save.version < 1 || save.version > PlayerSaveData.CurrentVersion)
                {
                    throw new InvalidDataException($"Unsupported save version {save.version}.");
                }

                save.shop ??= new ShopSaveData();
                save.shop.stock ??= new List<ShopStockSave>();
                save.shop.ownedParts ??= new List<int>();
                save.blueprints ??= new List<BlueprintSaveData>();
                save.companions ??= new List<CompanionSaveData>();
                if ((int)save.cameraPreset < 0 || (int)save.cameraPreset >= CameraDistancePresets.Count)
                    save.cameraPreset = CameraDistancePreset.Short;
                if ((int)save.controlMode < 0 || (int)save.controlMode > (int)PlayerControlMode.Mouse)
                    save.controlMode = PlayerControlMode.Keyboard;
                if (save.displayMode < 0 || save.displayMode > 3)
                    save.displayMode = 2;

                // v1 predates the Options fields; [OnDeserializing] already restored their
                // intended defaults, so the migration only stamps the current version.
                if (save.version < PlayerSaveData.CurrentVersion)
                    save.version = PlayerSaveData.CurrentVersion;
                return save;
            }
        }

        /// <summary>
        /// Writes the payload to a sibling temporary file and swaps it in, so a failed
        /// write never truncates the previous save.
        /// </summary>
        public static void SaveToFile(string path, PlayerSaveData save)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, ToJson(save));

            if (File.Exists(path))
            {
                try
                {
                    File.Replace(tempPath, path, null);
                    return;
                }
                catch (PlatformNotSupportedException)
                {
                    // File.Move(overwrite) is unavailable on netstandard2.1; delete first.
                    // shortcut: the fallback has a small non-atomic window, upgrade with
                    // File.Move(source, destination, overwrite: true) when the API is available.
                    File.Delete(path);
                }
            }

            File.Move(tempPath, path);
        }

        public static PlayerSaveData LoadFromFile(string path)
        {
            return FromJson(File.ReadAllText(path));
        }
    }

    /// <summary>
    /// Maps <see cref="CraftBlueprintSO"/> selection state to and from the payload.
    /// </summary>
    public static class BlueprintSaveMapper
    {
        public static BlueprintSaveData Capture(CraftBlueprintSO blueprint)
        {
            if (blueprint == null) return new BlueprintSaveData();

            var data = new BlueprintSaveData
            {
                craftName = blueprint.craftName ?? string.Empty,
                hullName = blueprint.hullName ?? string.Empty,
                hullCategory = blueprint.hullCategory,
                hullMass = blueprint.hullMass,
                hullHealth = blueprint.hullHealth,
                isGrandfathered = blueprint.isGrandfathered
            };

            if (blueprint.mountedParts != null)
            {
                for (int i = 0; i < blueprint.mountedParts.Length; i++)
                {
                    data.mountedParts.Add(MountedPartSaveData.FromMountedPart(blueprint.mountedParts[i]));
                }
            }

            return data;
        }

        public static void Apply(BlueprintSaveData data, CraftBlueprintSO blueprint)
        {
            if (data == null || blueprint == null) return;

            blueprint.craftName = data.craftName ?? string.Empty;
            blueprint.hullName = data.hullName ?? string.Empty;
            blueprint.hullCategory = data.hullCategory;
            blueprint.hullMass = data.hullMass;
            blueprint.hullHealth = data.hullHealth;
            blueprint.isGrandfathered = data.isGrandfathered;
            blueprint.mountedParts = ToMountedParts(data).ToArray();
        }

        public static List<MountedPartData> ToMountedParts(BlueprintSaveData data)
        {
            var parts = new List<MountedPartData>();
            if (data?.mountedParts == null) return parts;

            for (int i = 0; i < data.mountedParts.Count; i++)
            {
                parts.Add(data.mountedParts[i].ToMountedPart());
            }

            return parts;
        }
    }
}
