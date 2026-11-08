using System;
using System.Collections.Generic;
using System.Globalization;

namespace ABRevival.Core.Data
{
    /// <summary>One bullet id resolved to its atlas sprite and world size.</summary>
    public readonly struct BulletSprite
    {
        public readonly int bulletId;
        public readonly string mcName;
        public readonly string symbol;
        public readonly float width;
        public readonly float height;

        public BulletSprite(int bulletId, string mcName, string symbol, float width, float height)
        {
            this.bulletId = bulletId;
            this.mcName = mcName;
            this.symbol = symbol;
            this.width = width;
            this.height = height;
        }
    }

    /// <summary>
    /// Parsed <c>BulletSprites.txt</c>: frozen <c>F4DataBullet.mcName</c> resolved to an
    /// extracted sprite. Ids whose source symbol is the procedural <c>normal</c> disc map to
    /// the baked original disc, not a synthetic white cell. Original game IP: Vargames (2011).
    /// </summary>
    public sealed class BulletSpriteTable
    {
        public const string NormalSymbol = "bullet_normal.png";

        private readonly Dictionary<int, BulletSprite> _byId;
        private readonly BulletSprite _fallback;

        private BulletSpriteTable(Dictionary<int, BulletSprite> byId, BulletSprite fallback)
        {
            _byId = byId;
            _fallback = fallback;
        }

        public int Count => _byId.Count;

        public bool TryGet(int bulletId, out BulletSprite sprite)
        {
            return _byId.TryGetValue(bulletId, out sprite);
        }

        /// <summary>Known bullet art, or the frozen normal disc when the id has no row.</summary>
        public BulletSprite Get(int bulletId)
        {
            return _byId.TryGetValue(bulletId, out BulletSprite sprite) ? sprite : _fallback;
        }

        public static BulletSpriteTable Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Bullet sprite table payload is empty.", nameof(text));

            var byId = new Dictionary<int, BulletSprite>();
            BulletSprite fallback = default;

            string[] lines = text.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#') continue;

                string[] cells = line.Split(';');
                if (cells.Length != 6 || cells[0] != "B")
                    throw new FormatException($"Malformed bullet sprite row on line {i + 1}.");

                if (!int.TryParse(cells[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int bulletId) ||
                    !float.TryParse(cells[4], NumberStyles.Float, CultureInfo.InvariantCulture, out float width) ||
                    !float.TryParse(cells[5], NumberStyles.Float, CultureInfo.InvariantCulture, out float height))
                {
                    throw new FormatException($"Malformed bullet sprite row on line {i + 1}.");
                }
                if (width <= 0f || height <= 0f || cells[3].Length == 0)
                    throw new FormatException($"Bullet sprite row out of range on line {i + 1}.");

                var sprite = new BulletSprite(bulletId, cells[2], cells[3], width, height);
                if (byId.ContainsKey(bulletId))
                    throw new FormatException($"Duplicate bullet id {bulletId} on line {i + 1}.");
                byId.Add(bulletId, sprite);

                if (bulletId == 1) fallback = sprite;
            }

            if (byId.Count == 0)
                throw new FormatException("The bullet sprite table has no rows.");

            if (fallback.symbol == null)
            {
                // Bullet 1 is frozen "normal"; fall back to any normal row, else the first row.
                foreach (BulletSprite sprite in byId.Values)
                {
                    fallback = sprite;
                    if (sprite.mcName == "normal") break;
                }
            }

            return new BulletSpriteTable(byId, fallback);
        }
    }
}
