using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using ABRevival.Core.Data;

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
        public const int CurrentVersion = 1;

        [DataMember(Order = 0)] public int version = CurrentVersion;
        [DataMember(Order = 1)] public ShopSaveData shop = new ShopSaveData();
        [DataMember(Order = 2)] public List<BlueprintSaveData> blueprints = new List<BlueprintSaveData>();
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

                if (save.version != PlayerSaveData.CurrentVersion)
                {
                    throw new InvalidDataException($"Unsupported save version {save.version}.");
                }

                save.shop ??= new ShopSaveData();
                save.shop.stock ??= new List<ShopStockSave>();
                save.shop.ownedParts ??= new List<int>();
                save.blueprints ??= new List<BlueprintSaveData>();
                return save;
            }
        }

        public static void SaveToFile(string path, PlayerSaveData save)
        {
            File.WriteAllText(path, ToJson(save));
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
