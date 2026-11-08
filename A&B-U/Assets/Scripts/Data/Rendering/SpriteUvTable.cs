using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using ABRevival.Core.Data;

namespace ABRevival.Core.Rendering
{
    /// <summary>Exported sprite pixel dimensions read from the PNG header.</summary>
    public readonly struct SpritePixelSize
    {
        public readonly int Width;
        public readonly int Height;

        public SpritePixelSize(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            Width = width;
            Height = height;
        }
    }

    /// <summary>One symbol and the pixel size it occupies in the atlas.</summary>
    public readonly struct SpriteSymbolSize
    {
        public readonly string Symbol;
        public readonly int Width;
        public readonly int Height;

        public SpriteSymbolSize(string symbol, int width, int height)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            Width = width;
            Height = height;
        }
    }

    /// <summary>One symbol's cell in the atlas, in top-left pixel coordinates.</summary>
    public readonly struct SpriteSymbolRect
    {
        public readonly string Symbol;
        public readonly int X;
        public readonly int Y;
        public readonly int Width;
        public readonly int Height;

        public SpriteSymbolRect(string symbol, int x, int y, int width, int height)
        {
            Symbol = symbol;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    /// <summary>
    /// One atlas cell resolved for the renderer. Pixel coordinates keep the atlas image
    /// convention (origin at the top-left); the UVs flip Y into Unity texture space.
    /// </summary>
    public readonly struct SpriteUvRect
    {
        public readonly int X;
        public readonly int Y;
        public readonly int Width;
        public readonly int Height;
        public readonly int AtlasWidth;
        public readonly int AtlasHeight;

        public SpriteUvRect(int x, int y, int width, int height, int atlasWidth, int atlasHeight)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            AtlasWidth = atlasWidth;
            AtlasHeight = atlasHeight;
        }

        public bool IsValid => Width > 0 && Height > 0;

        public float UvX => X / (float)AtlasWidth;
        public float UvY => 1f - (Y + Height) / (float)AtlasHeight;
        public float UvWidth => Width / (float)AtlasWidth;
        public float UvHeight => Height / (float)AtlasHeight;
    }

    /// <summary>
    /// Deterministic shelf packer for the unified part atlas. Symbols are packed in
    /// ordinal order with a fixed padding, so identical sprite sizes always produce
    /// identical rectangles regardless of input order (ADR 0014).
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class SpriteAtlasLayout
    {
        public const int DefaultWidth = 2048;
        public const int DefaultHeight = 2048;
        public const int Padding = 4;

        public static List<SpriteSymbolRect> Pack(
            IReadOnlyList<SpriteSymbolSize> sprites,
            int atlasWidth = DefaultWidth,
            int atlasHeight = DefaultHeight)
        {
            if (sprites == null) throw new ArgumentNullException(nameof(sprites));
            if (atlasWidth <= 0) throw new ArgumentOutOfRangeException(nameof(atlasWidth));
            if (atlasHeight <= 0) throw new ArgumentOutOfRangeException(nameof(atlasHeight));

            var ordered = new List<SpriteSymbolSize>(sprites);
            ordered.Sort((left, right) => string.CompareOrdinal(left.Symbol, right.Symbol));

            var rects = new List<SpriteSymbolRect>(ordered.Count);
            var seen = new HashSet<string>(StringComparer.Ordinal);
            int x = 0;
            int y = 0;
            int rowHeight = 0;

            for (int i = 0; i < ordered.Count; i++)
            {
                SpriteSymbolSize sprite = ordered[i];
                if (!seen.Add(sprite.Symbol))
                    throw new InvalidOperationException($"Duplicate atlas symbol '{sprite.Symbol}'.");
                if (sprite.Width > atlasWidth)
                    throw new InvalidOperationException($"Sprite '{sprite.Symbol}' is wider than the atlas.");

                if (x + sprite.Width > atlasWidth)
                {
                    y += rowHeight + Padding;
                    x = 0;
                    rowHeight = 0;
                }

                if (y + sprite.Height > atlasHeight)
                    throw new InvalidOperationException($"The atlas is too small for '{sprite.Symbol}'.");

                rects.Add(new SpriteSymbolRect(sprite.Symbol, x, y, sprite.Width, sprite.Height));
                x += sprite.Width + Padding;
                if (sprite.Height > rowHeight) rowHeight = sprite.Height;
            }

            return rects;
        }
    }

    /// <summary>
    /// Builds the committed UV table from <see cref="PartSpriteMap"/> and the exported
    /// PNG sizes. One row per part id, plus the synthetic white cell used for projectiles.
    /// </summary>
    public static class SpriteUvTableBuilder
    {
        public const string WhiteSymbol = "__white";
        public const int WhiteCellSize = 8;

        public static List<SpriteSymbolRect> BuildLayout(
            IReadOnlyList<PartSpriteMapping> mappings,
            IReadOnlyDictionary<string, SpritePixelSize> spriteSizes,
            int atlasWidth = SpriteAtlasLayout.DefaultWidth,
            int atlasHeight = SpriteAtlasLayout.DefaultHeight,
            IReadOnlyList<string> extraSymbols = null)
        {
            if (mappings == null) throw new ArgumentNullException(nameof(mappings));
            if (spriteSizes == null) throw new ArgumentNullException(nameof(spriteSizes));

            var symbols = new List<SpriteSymbolSize>(spriteSizes.Count + 1)
            {
                new SpriteSymbolSize(WhiteSymbol, WhiteCellSize, WhiteCellSize)
            };
            var seen = new HashSet<string>(StringComparer.Ordinal) { WhiteSymbol };

            for (int i = 0; i < mappings.Count; i++)
            {
                PartSpriteMapping mapping = mappings[i];
                if (mapping == null) throw new ArgumentNullException(nameof(mappings));
                if (!seen.Add(mapping.fileName)) continue;
                if (!spriteSizes.TryGetValue(mapping.fileName, out SpritePixelSize size))
                    throw new InvalidOperationException($"Exported sprite missing for '{mapping.fileName}'.");
                symbols.Add(new SpriteSymbolSize(mapping.fileName, size.Width, size.Height));
            }

            if (extraSymbols != null)
            {
                for (int i = 0; i < extraSymbols.Count; i++)
                {
                    string symbol = extraSymbols[i];
                    if (string.IsNullOrEmpty(symbol) || !seen.Add(symbol)) continue;
                    if (!spriteSizes.TryGetValue(symbol, out SpritePixelSize size))
                        throw new InvalidOperationException($"Exported sprite missing for '{symbol}'.");
                    symbols.Add(new SpriteSymbolSize(symbol, size.Width, size.Height));
                }
            }

            return SpriteAtlasLayout.Pack(symbols, atlasWidth, atlasHeight);
        }

        public static string Build(
            IReadOnlyList<PartSpriteMapping> mappings,
            IReadOnlyDictionary<string, SpritePixelSize> spriteSizes,
            int atlasWidth = SpriteAtlasLayout.DefaultWidth,
            int atlasHeight = SpriteAtlasLayout.DefaultHeight,
            IReadOnlyList<string> extraSymbols = null)
        {
            List<SpriteSymbolRect> layout = BuildLayout(mappings, spriteSizes, atlasWidth, atlasHeight, extraSymbols);
            var rectBySymbol = new Dictionary<string, SpriteSymbolRect>(layout.Count, StringComparer.Ordinal);
            for (int i = 0; i < layout.Count; i++)
                rectBySymbol[layout[i].Symbol] = layout[i];

            var text = new StringBuilder(layout.Count * 48);
            text.Append("# ABRevival parts atlas UV table v1\n");
            text.Append("# Pixel rects use a top-left origin; the runtime converts them to UVs.\n");
            text.Append("atlas=")
                .Append(atlasWidth.ToString(CultureInfo.InvariantCulture))
                .Append('x')
                .Append(atlasHeight.ToString(CultureInfo.InvariantCulture))
                .Append('\n');

            AppendRow(text, -1, rectBySymbol[WhiteSymbol]);

            if (extraSymbols != null)
            {
                for (int i = 0; i < extraSymbols.Count; i++)
                {
                    string symbol = extraSymbols[i];
                    if (string.IsNullOrEmpty(symbol) || !rectBySymbol.TryGetValue(symbol, out SpriteSymbolRect rect))
                        continue;
                    AppendSymbolRow(text, rect);
                }
            }

            var partIds = new HashSet<int>();
            for (int i = 0; i < mappings.Count; i++)
            {
                PartSpriteMapping mapping = mappings[i];
                if (!partIds.Add(mapping.partId))
                    throw new InvalidOperationException($"Duplicate catalog part id {mapping.partId} in the sprite map.");
                AppendRow(text, mapping.partId, rectBySymbol[mapping.fileName]);
            }

            return text.ToString();
        }

        private static void AppendSymbolRow(StringBuilder text, in SpriteSymbolRect rect)
        {
            text.Append("symbol=").Append(rect.Symbol).Append(';')
                .Append(rect.X.ToString(CultureInfo.InvariantCulture)).Append(';')
                .Append(rect.Y.ToString(CultureInfo.InvariantCulture)).Append(';')
                .Append(rect.Width.ToString(CultureInfo.InvariantCulture)).Append(';')
                .Append(rect.Height.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }

        private static void AppendRow(StringBuilder text, int partId, in SpriteSymbolRect rect)
        {
            text.Append(partId.ToString(CultureInfo.InvariantCulture)).Append(';')
                .Append(rect.Symbol).Append(';')
                .Append(rect.X.ToString(CultureInfo.InvariantCulture)).Append(';')
                .Append(rect.Y.ToString(CultureInfo.InvariantCulture)).Append(';')
                .Append(rect.Width.ToString(CultureInfo.InvariantCulture)).Append(';')
                .Append(rect.Height.ToString(CultureInfo.InvariantCulture)).Append('\n');
        }
    }

    /// <summary>
    /// Parsed part-id -> atlas rect table. Built once when a mission starts, then read
    /// per instance without allocating.
    /// </summary>
    public sealed class SpriteUvTable
    {
        public const int MaxPartId = 4096;

        private readonly SpriteUvRect[] _byPart;
        private readonly SpriteUvRect _white;
        private readonly Dictionary<string, SpriteUvRect> _bySymbol;

        public int AtlasWidth { get; }
        public int AtlasHeight { get; }
        public int PartCount => _byPart.Length;
        public int SymbolCount { get; }
        public SpriteUvRect White => _white;
        public bool HasWhite => _white.IsValid;

        private SpriteUvTable(
            SpriteUvRect[] byPart,
            SpriteUvRect white,
            Dictionary<string, SpriteUvRect> bySymbol,
            int atlasWidth,
            int atlasHeight)
        {
            _byPart = byPart;
            _white = white;
            _bySymbol = bySymbol;
            SymbolCount = bySymbol.Count;
            AtlasWidth = atlasWidth;
            AtlasHeight = atlasHeight;
        }

        public bool TryGetPart(int partId, out SpriteUvRect rect)
        {
            if ((uint)partId < (uint)_byPart.Length && _byPart[partId].IsValid)
            {
                rect = _byPart[partId];
                return true;
            }

            rect = default;
            return false;
        }

        /// <summary>Resolves an explicit symbol row (bullet art, effects) by atlas file name.</summary>
        public bool TryGetSymbol(string symbol, out SpriteUvRect rect)
        {
            if (!string.IsNullOrEmpty(symbol) && _bySymbol.TryGetValue(symbol, out rect))
                return true;

            rect = default;
            return false;
        }

        /// <summary>Part rect, or the white cell when the id has no exported symbol.</summary>
        public SpriteUvRect PartOrDefault(int partId)
        {
            return TryGetPart(partId, out SpriteUvRect rect) ? rect : _white;
        }

        public static SpriteUvTable Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("UV table payload is empty.", nameof(text));

            int atlasWidth = 0;
            int atlasHeight = 0;
            bool whiteSeen = false;
            SpriteUvRect white = default;
            var rows = new List<KeyValuePair<int, (string symbol, int x, int y, int width, int height)>>();
            var symbolRects = new Dictionary<string, SpriteUvRect>(StringComparer.Ordinal);
            int maxPartId = -1;

            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                if (line.StartsWith("atlas=", StringComparison.Ordinal))
                {
                    string[] size = line.Substring(6).Split('x');
                    if (size.Length != 2 ||
                        !int.TryParse(size[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out atlasWidth) ||
                        !int.TryParse(size[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out atlasHeight) ||
                        atlasWidth <= 0 || atlasHeight <= 0)
                    {
                        throw new FormatException($"Malformed atlas size on line {i + 1}.");
                    }
                    continue;
                }

                if (atlasWidth <= 0)
                    throw new FormatException("The UV table must declare atlas=WxH before its rows.");

                if (line.StartsWith("symbol=", StringComparison.Ordinal))
                {
                    string[] symbolCells = line.Split(';');
                    if (symbolCells.Length != 5 ||
                        !int.TryParse(symbolCells[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int sx) ||
                        !int.TryParse(symbolCells[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int sy) ||
                        !int.TryParse(symbolCells[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int sw) ||
                        !int.TryParse(symbolCells[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int sh))
                    {
                        throw new FormatException($"Malformed symbol row on line {i + 1}.");
                    }

                    string symbolName = symbolCells[0].Substring(7);
                    if (symbolName.Length == 0 || sw <= 0 || sh <= 0 || sx < 0 || sy < 0 ||
                        sx + sw > atlasWidth || sy + sh > atlasHeight)
                    {
                        throw new FormatException($"Symbol row out of atlas bounds on line {i + 1}.");
                    }
                    if (symbolRects.ContainsKey(symbolName))
                        throw new FormatException($"Duplicate atlas symbol '{symbolName}' on line {i + 1}.");

                    symbolRects.Add(symbolName, new SpriteUvRect(sx, sy, sw, sh, atlasWidth, atlasHeight));
                    continue;
                }

                string[] cells = line.Split(';');
                if (cells.Length != 6)
                    throw new FormatException($"Malformed UV row on line {i + 1}.");
                if (!int.TryParse(cells[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int partId) ||
                    !int.TryParse(cells[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) ||
                    !int.TryParse(cells[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y) ||
                    !int.TryParse(cells[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int width) ||
                    !int.TryParse(cells[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int height))
                {
                    throw new FormatException($"Malformed UV row on line {i + 1}.");
                }

                if (partId > MaxPartId)
                    throw new FormatException($"Part id {partId} exceeds the supported maximum of {MaxPartId} on line {i + 1}.");

                string symbol = cells[1];
                if (symbol.Length == 0 || width <= 0 || height <= 0 || x < 0 || y < 0 ||
                    x + width > atlasWidth || y + height > atlasHeight)
                {
                    throw new FormatException($"UV row out of atlas bounds on line {i + 1}.");
                }

                var rect = new SpriteUvRect(x, y, width, height, atlasWidth, atlasHeight);
                if (symbolRects.TryGetValue(symbol, out SpriteUvRect existing) &&
                    (existing.X != x || existing.Y != y || existing.Width != width || existing.Height != height))
                {
                    throw new FormatException($"Symbol '{symbol}' has conflicting atlas rects on line {i + 1}.");
                }
                symbolRects[symbol] = rect;
                if (partId < 0)
                {
                    if (symbol != SpriteUvTableBuilder.WhiteSymbol)
                        throw new FormatException($"Only '{SpriteUvTableBuilder.WhiteSymbol}' may use a negative part id.");
                    whiteSeen = true;
                    white = rect;
                    symbolRects[symbol] = rect;
                    continue;
                }

                rows.Add(new KeyValuePair<int, (string, int, int, int, int)>(partId, (symbol, x, y, width, height)));
                if (partId > maxPartId) maxPartId = partId;
            }

            if (!whiteSeen)
                throw new FormatException($"The UV table is missing the '{SpriteUvTableBuilder.WhiteSymbol}' cell.");

            var byPart = new SpriteUvRect[maxPartId + 1];
            for (int i = 0; i < rows.Count; i++)
            {
                int partId = rows[i].Key;
                var row = rows[i].Value;
                if (byPart[partId].IsValid)
                    throw new FormatException($"Duplicate part id {partId} in the UV table.");
                byPart[partId] = new SpriteUvRect(row.x, row.y, row.width, row.height, atlasWidth, atlasHeight);
            }

            return new SpriteUvTable(byPart, white, symbolRects, atlasWidth, atlasHeight);
        }
    }

    /// <summary>Minimal PNG header reader so the UV table can be derived without Unity.</summary>
    public static class PngSize
    {
        private static readonly byte[] Signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        public static SpritePixelSize Read(string path)
        {
            if (!TryRead(path, out SpritePixelSize size))
                throw new InvalidDataException($"'{path}' is not a readable PNG.");
            return size;
        }

        public static bool TryRead(string path, out SpritePixelSize size)
        {
            size = default;
            try
            {
                using (var stream = System.IO.File.OpenRead(path))
                {
                    var header = new byte[24];
                    int read = stream.Read(header, 0, header.Length);
                    if (read < header.Length) return false;
                    for (int i = 0; i < Signature.Length; i++)
                        if (header[i] != Signature[i]) return false;

                    int width = ReadBigEndian(header, 16);
                    int height = ReadBigEndian(header, 20);
                    if (width <= 0 || height <= 0) return false;
                    size = new SpritePixelSize(width, height);
                    return true;
                }
            }
            catch (System.IO.IOException)
            {
                return false;
            }
        }

        public static Dictionary<string, SpritePixelSize> ReadFolder(string folder)
        {
            if (folder == null) throw new ArgumentNullException(nameof(folder));

            string[] files = System.IO.Directory.GetFiles(folder, "*.png");
            Array.Sort(files, StringComparer.Ordinal);
            var sizes = new Dictionary<string, SpritePixelSize>(files.Length, StringComparer.Ordinal);
            for (int i = 0; i < files.Length; i++)
                sizes[System.IO.Path.GetFileName(files[i])] = Read(files[i]);
            return sizes;
        }

        private static int ReadBigEndian(byte[] buffer, int offset)
        {
            return (buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3];
        }
    }
}
