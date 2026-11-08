using System;
using System.Collections.Generic;
using System.IO;

namespace ABRevival.Core.Data
{
    /// <summary>
    /// Deterministic file names for sprites exported from the frozen SWF symbol library.
    /// The 60 FPS baseline draws part symbols 1:1 into the 1920x1080 canvas
    /// (RenderData.W/H, BMPD2D.copyPixels), so the 1080p export keeps native pixels.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class SpriteNaming
    {
        public const int ExportZoom = 1;

        /// <summary>
        /// In-game mount art is rasterised at 2x (ADR 0013 decision 5), matching the frozen
        /// Stage3D atlas density. Marker coordinates are scaled by the same factor.
        /// </summary>
        public const int MountZoom = 2;

        public static string PartSpriteFile(int canonicalPartId, string mcName)
        {
            return $"part_{canonicalPartId:D3}_{Sanitize(mcName)}.png";
        }

        /// <summary>In-game mount art: nose-up frame 0 at <see cref="MountZoom"/>.</summary>
        public static string PartMountFile(int canonicalPartId, string mcName)
        {
            return $"part_{canonicalPartId:D3}_{Sanitize(mcName)}.png";
        }

        public static string BulletSpriteFile(string mcName)
        {
            return $"bullet_{Sanitize(mcName)}.png";
        }

        public static string UiImageFile(int characterId)
        {
            return $"ui_{characterId:D4}.png";
        }

        public static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "Unnamed";

            char[] invalid = Path.GetInvalidFileNameChars();
            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalid, chars[i]) >= 0) chars[i] = '_';
            }
            return new string(chars).Trim();
        }
    }

    /// <summary>
    /// One catalog part resolved to its shared SWF symbol sprite file.
    /// </summary>
    public sealed class PartSpriteMapping
    {
        public int partId;
        public string mcName;
        public string fileName;
    }

    /// <summary>
    /// Builds the catalog id -> sprite file mapping. Several part ids share one
    /// SWF symbol (left/right wing variants), so the lowest part id names the file.
    /// </summary>
    public static class PartSpriteMap
    {
        public static List<PartSpriteMapping> Build(IEnumerable<PartDefinitionRecord> records)
        {
            var ordered = new List<PartDefinitionRecord>(records);
            ordered.Sort((a, b) => a.partId.CompareTo(b.partId));

            var canonicalIds = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var record in ordered)
            {
                string key = SymbolKey(record.mcName);
                if (!canonicalIds.TryGetValue(key, out int id) || record.partId < id)
                {
                    canonicalIds[key] = record.partId;
                }
            }

            var mappings = new List<PartSpriteMapping>(ordered.Count);
            foreach (var record in ordered)
            {
                string key = SymbolKey(record.mcName);
                mappings.Add(new PartSpriteMapping
                {
                    partId = record.partId,
                    mcName = key,
                    fileName = SpriteNaming.PartSpriteFile(canonicalIds[key], key)
                });
            }
            return mappings;
        }

        private static string SymbolKey(string mcName)
        {
            return string.IsNullOrEmpty(mcName) ? "Unnamed" : mcName;
        }
    }
}
