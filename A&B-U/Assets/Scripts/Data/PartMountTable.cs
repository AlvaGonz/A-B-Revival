using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Unity.Mathematics;

namespace ABRevival.Core.Data
{
    /// <summary>One socket on a frozen part symbol: the art-space point a child mounts to.</summary>
    public sealed class PartMountHole
    {
        public float2 offset;
        public int lr;
        public byte types;

        public bool Accepts(PartCategory category)
        {
            return (types & (1 << (int)category)) != 0;
        }
    }

    /// <summary>
    /// Marker geometry decoded from a part's SWF <c>SHOW</c> frame. Coordinates are stage
    /// pixels in Unity 2D space (+X right, +Y forward); the editor pipeline converts the
    /// frozen twips and Y-down art space in one place. Original game IP: Vargames (2011).
    /// </summary>
    public sealed class PartMountSymbol
    {
        public string mcName;
        public float2 link;
        public float2 size;
        public PartMountHole[] holes = Array.Empty<PartMountHole>();
        public float2[] firePoints = Array.Empty<float2>();
        public bool hasPartPoint;
        public float2 partPoint;
    }

    /// <summary>One catalog part resolved to its mount art and marker symbol.</summary>
    public sealed class PartMountEntry
    {
        public int partId;
        public string mcName;
        public int lr;
        public int bulletId;
        public string mountFile;
        public string thumbnailFile;
    }

    /// <summary>
    /// Parsed <c>PartMounts.txt</c>: the frozen <c>H</c>/<c>F</c>/<c>L</c>/<c>P</c> marker
    /// table plus the mount-art/thumbnail manifest. Built once per mission; lookups allocate
    /// nothing. Original game IP: Vargames (2011).
    /// </summary>
    public sealed class PartMountTable
    {
        public const int MaxPartId = 4096;

        private readonly Dictionary<int, PartMountEntry> _byPart;
        private readonly Dictionary<string, PartMountSymbol> _bySymbol;

        public int ArtScale { get; }

        private PartMountTable(
            Dictionary<int, PartMountEntry> byPart,
            Dictionary<string, PartMountSymbol> bySymbol,
            int artScale)
        {
            _byPart = byPart;
            _bySymbol = bySymbol;
            ArtScale = artScale;
        }

        public int SymbolCount => _bySymbol.Count;
        public int PartCount => _byPart.Count;

        public bool TryGetPart(int partId, out PartMountEntry entry)
        {
            return _byPart.TryGetValue(partId, out entry);
        }

        public bool TryGetSymbol(string mcName, out PartMountSymbol symbol)
        {
            if (string.IsNullOrEmpty(mcName))
            {
                symbol = null;
                return false;
            }
            return _bySymbol.TryGetValue(mcName, out symbol);
        }

        public PartMountSymbol GetSymbol(string mcName)
        {
            return TryGetSymbol(mcName, out PartMountSymbol symbol) ? symbol : null;
        }

        /// <summary>Link point of the part, mirrored for the right-hand variant (lr == 1).</summary>
        public bool TryGetLink(int partId, out float2 link)
        {
            if (!_byPart.TryGetValue(partId, out PartMountEntry entry) ||
                !_bySymbol.TryGetValue(entry.mcName, out PartMountSymbol symbol))
            {
                link = default;
                return false;
            }

            link = entry.lr == 1 ? new float2(-symbol.link.x, symbol.link.y) : symbol.link;
            return true;
        }

        /// <summary>
        /// Resolves the local offset of <paramref name="childPartId"/> mounted in
        /// <paramref name="holeIndex"/> of <paramref name="parentPartId"/>.
        /// </summary>
        public bool TryResolveSocket(
            int parentPartId,
            int holeIndex,
            int childPartId,
            bool parentMirrored,
            out float2 offset)
        {
            offset = default;
            if (!_byPart.TryGetValue(parentPartId, out PartMountEntry parent) ||
                !_bySymbol.TryGetValue(parent.mcName, out PartMountSymbol parentSymbol))
            {
                return false;
            }
            if ((uint)holeIndex >= (uint)parentSymbol.holes.Length) return false;

            PartMountHole hole = parentSymbol.holes[holeIndex];
            if (hole == null) return false;
            if (!TryGetLink(childPartId, out float2 link)) return false;

            float holeX = parentMirrored ? -hole.offset.x : hole.offset.x;
            offset = new float2(holeX - link.x, hole.offset.y - link.y);
            return true;
        }

        /// <summary>First muzzle point of a gun in hull space, or the part origin when it has no F marker.</summary>
        public float2 MuzzleOffset(int partId, float2 origin)
        {
            if (_byPart.TryGetValue(partId, out PartMountEntry entry) &&
                _bySymbol.TryGetValue(entry.mcName, out PartMountSymbol symbol) &&
                symbol.firePoints.Length > 0)
            {
                return origin + symbol.firePoints[0];
            }
            return origin;
        }

        /// <summary>
        /// Copies every F marker of a gun into <paramref name="destination"/> and returns the
        /// count. Zero means the part has no muzzle marker. Allocation-free.
        /// </summary>
        public int CopyFirePoints(int partId, Span<float2> destination)
        {
            if (!_byPart.TryGetValue(partId, out PartMountEntry entry) ||
                !_bySymbol.TryGetValue(entry.mcName, out PartMountSymbol symbol))
            {
                return 0;
            }

            int count = math.min(symbol.firePoints.Length, destination.Length);
            for (int i = 0; i < count; i++) destination[i] = symbol.firePoints[i];
            return count;
        }

        /// <summary>Engine exhaust point (P marker) in part-local space, or the origin.</summary>
        public float2 ExhaustLocal(int partId)
        {
            if (_byPart.TryGetValue(partId, out PartMountEntry entry) &&
                _bySymbol.TryGetValue(entry.mcName, out PartMountSymbol symbol) &&
                symbol.hasPartPoint)
            {
                return symbol.partPoint;
            }
            return float2.zero;
        }

        /// <summary>Engine exhaust point (P marker) in hull space, or the part origin.</summary>
        public float2 ExhaustPoint(int partId, float2 origin)
        {
            return origin + ExhaustLocal(partId);
        }

        /// <summary>
        /// Closest derivable layout when a frozen parent has no matching H marker: wings sit
        /// outboard, engines aft, guns forward. Never silently centres a child on the parent.
        /// </summary>
        public static float2 FallbackOffset(PartMountSymbol parent, PartCategory category, int siblingIndex)
        {
            float width = parent != null && parent.size.x > 0f ? parent.size.x : 32f;
            float height = parent != null && parent.size.y > 0f ? parent.size.y : 32f;
            bool right = (siblingIndex & 1) == 1;
            float outboard = width * 0.35f;
            float spread = width * 0.15f * (1 + (siblingIndex / 2));

            switch (category)
            {
                case PartCategory.Wing:
                    return new float2(right ? outboard : -outboard, -height * 0.08f);
                case PartCategory.Engine:
                    return new float2(right ? spread : -spread, -height * 0.34f);
                case PartCategory.Gun:
                    return new float2(right ? spread : -spread, height * 0.24f);
                default:
                    return new float2(0f, 0f);
            }
        }

        public static PartMountTable Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Part mount table payload is empty.", nameof(text));

            var byPart = new Dictionary<int, PartMountEntry>();
            var bySymbol = new Dictionary<string, PartMountSymbol>(StringComparer.Ordinal);
            var pendingParts = new List<(int partId, string mcName, int lr, int bulletId, string mount, string thumbnail)>();
            int artScale = 1;

            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                if (line.StartsWith("scale=", StringComparison.Ordinal))
                {
                    if (!int.TryParse(line.Substring(6), NumberStyles.Integer, CultureInfo.InvariantCulture, out artScale) || artScale <= 0)
                        throw new FormatException($"Malformed mount scale on line {i + 1}.");
                    continue;
                }

                string[] cells = line.Split(';');
                switch (cells[0])
                {
                    case "S":
                        RequireCells(cells, 2, i);
                        bySymbol[cells[1]] = new PartMountSymbol { mcName = cells[1] };
                        break;

                    case "A":
                    {
                        RequireCells(cells, 4, i);
                        PartMountSymbol symbol = RequireSymbol(bySymbol, cells[1], i);
                        symbol.size = new float2(ParseFloat(cells[2], i), ParseFloat(cells[3], i));
                        break;
                    }

                    case "L":
                    {
                        RequireCells(cells, 4, i);
                        PartMountSymbol symbol = RequireSymbol(bySymbol, cells[1], i);
                        symbol.link = new float2(ParseFloat(cells[2], i), ParseFloat(cells[3], i));
                        break;
                    }

                    case "H":
                    {
                        RequireCells(cells, 7, i);
                        PartMountSymbol symbol = RequireSymbol(bySymbol, cells[1], i);
                        var hole = new PartMountHole
                        {
                            offset = new float2(ParseFloat(cells[3], i), ParseFloat(cells[4], i)),
                            lr = ParseInt(cells[5], i),
                            types = (byte)ParseInt(cells[6], i)
                        };
                        int index = ParseInt(cells[2], i);
                        if (index < 0) throw new FormatException($"Negative hole index on line {i + 1}.");
                        EnsureLength(ref symbol.holes, index + 1, i);
                        symbol.holes[index] = hole;
                        break;
                    }

                    case "F":
                    {
                        RequireCells(cells, 5, i);
                        PartMountSymbol symbol = RequireSymbol(bySymbol, cells[1], i);
                        int index = ParseInt(cells[2], i);
                        if (index < 0) throw new FormatException($"Negative fire index on line {i + 1}.");
                        EnsureLength(ref symbol.firePoints, index + 1, i);
                        symbol.firePoints[index] = new float2(ParseFloat(cells[3], i), ParseFloat(cells[4], i));
                        break;
                    }

                    case "T":
                    {
                        RequireCells(cells, 4, i);
                        PartMountSymbol symbol = RequireSymbol(bySymbol, cells[1], i);
                        symbol.hasPartPoint = true;
                        symbol.partPoint = new float2(ParseFloat(cells[2], i), ParseFloat(cells[3], i));
                        break;
                    }

                    case "P":
                        RequireCells(cells, 7, i);
                        pendingParts.Add((
                            ParseInt(cells[1], i),
                            cells[2],
                            ParseInt(cells[3], i),
                            ParseInt(cells[4], i),
                            cells[5],
                            cells[6]));
                        break;

                    default:
                        throw new FormatException($"Unknown mount table row '{cells[0]}' on line {i + 1}.");
                }
            }

            for (int i = 0; i < pendingParts.Count; i++)
            {
                var row = pendingParts[i];
                if (row.partId < 0 || row.partId > MaxPartId)
                    throw new FormatException($"Part id {row.partId} is outside 0..{MaxPartId}.");
                if (!bySymbol.ContainsKey(row.mcName))
                    throw new FormatException($"Part {row.partId} references unknown symbol '{row.mcName}'.");
                if (byPart.ContainsKey(row.partId))
                    throw new FormatException($"Duplicate part id {row.partId} in the mount table.");

                byPart.Add(row.partId, new PartMountEntry
                {
                    partId = row.partId,
                    mcName = row.mcName,
                    lr = row.lr,
                    bulletId = row.bulletId,
                    mountFile = row.mount,
                    thumbnailFile = row.thumbnail
                });
            }

            return new PartMountTable(byPart, bySymbol, artScale);
        }

        private static void RequireCells(string[] cells, int count, int line)
        {
            if (cells.Length != count)
                throw new FormatException($"Expected {count} cells on line {line + 1}, found {cells.Length}.");
        }

        private static PartMountSymbol RequireSymbol(Dictionary<string, PartMountSymbol> symbols, string mcName, int line)
        {
            if (!symbols.TryGetValue(mcName, out PartMountSymbol symbol))
                throw new FormatException($"Symbol '{mcName}' is used before its S row on line {line + 1}.");
            return symbol;
        }

        private static void EnsureLength<T>(ref T[] array, int length, int line)
        {
            if (array.Length >= length) return;
            if (length > 4096) throw new FormatException($"Marker index {length - 1} is too large on line {line + 1}.");
            Array.Resize(ref array, length);
        }

        private static float ParseFloat(string value, int line)
        {
            if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float result))
                throw new FormatException($"Malformed number '{value}' on line {line + 1}.");
            return result;
        }

        private static int ParseInt(string value, int line)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result))
                throw new FormatException($"Malformed integer '{value}' on line {line + 1}.");
            return result;
        }
    }
}
