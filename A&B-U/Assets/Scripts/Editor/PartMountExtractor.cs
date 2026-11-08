using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;
using ABRevival.Core.Data;
using ABRevival.Core.Rendering;

namespace ABRevival.Editor
{
    /// <summary>
    /// Decodes the frozen <c>H</c>/<c>F</c>/<c>L</c>/<c>P</c> marker layers from the part SWF
    /// XML and writes the committed part-sprite manifest. Marker grammar is traced to
    /// <c>MC2F4Data.readMcData</c>:
    /// <c>H_&lt;lr&gt;_&lt;types&gt;</c> is a socket (lr 0 left / 1 right / 2 both, digits are the
    /// accepted part categories), <c>F</c> is a muzzle, <c>L</c> is the child link point and
    /// <c>P</c> is the engine exhaust point. Coordinates are twips in Y-down art space and are
    /// converted once to stage pixels in Unity Y-up space. Any symbol missing a <c>SHOW</c>
    /// frame fails the pipeline loudly; a missing socket falls back to the documented
    /// <see cref="PartMountTable.FallbackOffset"/> layout at runtime instead of centring.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class PartMountExtractor
    {
        private const double TwipsPerPixel = 20.0;

        public sealed class Result
        {
            public string MountTableText;
            public string BulletTableText;
            public string ManifestMarkdown;
            public int SymbolCount;
            public int PartCount;
        }

        /// <summary>
        /// Runs FFDec for the two files the extractor needs (SWF XML and symbols.csv) and
        /// returns the generated table texts. Temporary output is removed before returning.
        /// </summary>
        public static Result ExtractFromSwf(
            string ffdec,
            string sourceSwf,
            IReadOnlyList<PartDefinitionRecord> parts,
            IReadOnlyList<BulletArtRecord> bullets,
            IReadOnlyDictionary<string, SpritePixelSize> mountSizes,
            IReadOnlyDictionary<string, SpritePixelSize> bulletSizes)
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), "ab-revival-part-mount-extract");
            if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            Directory.CreateDirectory(tempRoot);

            try
            {
                string xmlPath = Path.Combine(tempRoot, "data.xml");
                string symbolDir = Path.Combine(tempRoot, "symbols");
                Directory.CreateDirectory(symbolDir);

                SpriteExportPipeline.RunFfdec(ffdec, "-swf2xml \"" + sourceSwf + "\" \"" + xmlPath + "\"");
                SpriteExportPipeline.RunFfdec(ffdec, "-export symbolClass \"" + symbolDir + "\" \"" + sourceSwf + "\"");

                string csv = Path.Combine(symbolDir, "symbols.csv");
                if (!File.Exists(csv))
                    throw new InvalidOperationException("FFDec did not write symbols.csv for " + sourceSwf);

                return Extract(xmlPath, parts, bullets, SpriteExportPipeline.ReadSymbolIds(csv), mountSizes, bulletSizes);
            }
            finally
            {
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, true);
            }
        }

        public static Result Extract(
            string swfXmlPath,
            IReadOnlyList<PartDefinitionRecord> parts,
            IReadOnlyList<BulletArtRecord> bullets,
            IReadOnlyDictionary<string, int> symbolIds,
            IReadOnlyDictionary<string, SpritePixelSize> mountSizes,
            IReadOnlyDictionary<string, SpritePixelSize> bulletSizes)
        {
            if (parts == null) throw new ArgumentNullException(nameof(parts));
            if (bullets == null) throw new ArgumentNullException(nameof(bullets));
            if (symbolIds == null) throw new ArgumentNullException(nameof(symbolIds));
            if (mountSizes == null) throw new ArgumentNullException(nameof(mountSizes));
            if (bulletSizes == null) throw new ArgumentNullException(nameof(bulletSizes));

            XDocument document = XDocument.Load(swfXmlPath);
            var sprites = new Dictionary<int, XElement>();
            foreach (XElement tag in document.Root.Element("tags").Elements("item"))
            {
                string spriteId = tag.Attribute("spriteId")?.Value;
                if (spriteId != null) sprites[int.Parse(spriteId, CultureInfo.InvariantCulture)] = tag;
            }

            var mappings = PartSpriteMap.Build(parts);
            var canonicalFile = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (PartSpriteMapping mapping in mappings)
            {
                if (!canonicalFile.ContainsKey(mapping.mcName)) canonicalFile[mapping.mcName] = mapping.fileName;
            }

            var table = new StringBuilder(parts.Count * 96);
            table.Append("# ABRevival part mount table v1\n");
            table.Append("# S/L/A/H/F/T rows are the frozen SHOW-frame geometry; P rows separate mount art from thumbnails.\n");
            table.Append("# Coordinates are stage pixels in Unity 2D space (+X right, +Y forward).\n");
            table.Append("scale=").Append(SpriteNaming.MountZoom.ToString(CultureInfo.InvariantCulture)).Append('\n');

            var processedSymbols = new HashSet<string>(StringComparer.Ordinal);
            var manifest = new StringBuilder(parts.Count * 128);
            manifest.Append("# Part sprites: in-game mount art vs shop thumbnails\n\n");
            manifest.Append("Generated by `Tools/A&B Revival/Regenerate Sprites and Atlases` from the frozen SWF. ");
            manifest.Append("Vargames (2011) owns the original art.\n\n");
            manifest.Append("`PartMounts/` is the in-game frame-0 art at 2x (nose-up). ");
            manifest.Append("`Parts/` is the shop icon canvas at 1x. The machine-readable tables are ");
            manifest.Append("`Assets/Resources/Data/PartMounts.txt` and `BulletSprites.txt`.\n\n");
            manifest.Append("Marker grammar (`MC2F4Data.readMcData`): `H_<lr>_<types>` sockets, `F` muzzles, ");
            manifest.Append("`L` child link points and `P` engine exhaust points. Coordinates are converted ");
            manifest.Append("from twips/Y-down art space to stage pixels/Y-up in one place. The frozen part ");
            manifest.Append("catalog has no `rot` value, so every placement rotation is 0; a marker with a ");
            manifest.Append("rotation or scale is rejected by the extractor instead of being guessed. If a ");
            manifest.Append("socket is missing, the runtime uses the documented category fallback (wings ");
            manifest.Append("outboard, engines aft, guns forward) and never silently centres the child.\n\n");
            manifest.Append("One row per shared SWF symbol; `PartMounts.txt` lists every catalog part id, ");
            manifest.Append("including the left/right `lr` variants that share the same mount art.\n\n");
            manifest.Append("| part | mcName | category | lr | mount art | thumbnail | holes | fire points |\n");
            manifest.Append("|---|---|---|---|---|---|---|---|\n");

            int symbolCount = 0;
            foreach (PartDefinitionRecord part in parts)
            {
                if (!processedSymbols.Add(part.mcName)) continue;

                if (!symbolIds.TryGetValue(part.mcName, out int wrapperId))
                    throw new InvalidOperationException($"SWF symbol '{part.mcName}' is not in symbols.csv.");

                XElement wrapper = RequireSprite(sprites, wrapperId, part.mcName);
                XElement show = FindChild(wrapper, "SHOW");
                if (show == null)
                {
                    throw new InvalidOperationException(
                        $"Part symbol '{part.mcName}' ({wrapperId}) has no SHOW frame; its mount art and markers cannot be decoded.");
                }

                int showId = int.Parse(show.Attribute("characterId").Value, CultureInfo.InvariantCulture);
                XElement showSprite = RequireSprite(sprites, showId, part.mcName + "/SHOW");
                var markers = ReadMarkers(showSprite, part.mcName);

                if (!canonicalFile.TryGetValue(part.mcName, out string mountFile))
                    throw new InvalidOperationException($"No mount file mapping for '{part.mcName}'.");
                if (!mountSizes.TryGetValue(mountFile, out SpritePixelSize size))
                    throw new InvalidOperationException($"Exported mount art missing for '{mountFile}'.");

                table.Append("S;").Append(part.mcName).Append('\n');
                table.Append("L;").Append(part.mcName).Append(';')
                    .Append(Format(markers.linkX)).Append(';').Append(Format(markers.linkY)).Append('\n');
                table.Append("A;").Append(part.mcName).Append(';')
                    .Append(size.Width.ToString(CultureInfo.InvariantCulture)).Append(';')
                    .Append(size.Height.ToString(CultureInfo.InvariantCulture)).Append('\n');

                for (int i = 0; i < markers.holes.Count; i++)
                {
                    MarkerHole hole = markers.holes[i];
                    table.Append("H;").Append(part.mcName).Append(';')
                        .Append(i.ToString(CultureInfo.InvariantCulture)).Append(';')
                        .Append(Format(hole.x)).Append(';').Append(Format(hole.y)).Append(';')
                        .Append(hole.lr.ToString(CultureInfo.InvariantCulture)).Append(';')
                        .Append(hole.types.ToString(CultureInfo.InvariantCulture)).Append('\n');
                }

                for (int i = 0; i < markers.firePoints.Count; i++)
                {
                    FirePoint point = markers.firePoints[i];
                    table.Append("F;").Append(part.mcName).Append(';')
                        .Append(i.ToString(CultureInfo.InvariantCulture)).Append(';')
                        .Append(Format(point.x)).Append(';').Append(Format(point.y)).Append('\n');
                }

                if (markers.hasPartPoint)
                {
                    table.Append("T;").Append(part.mcName).Append(';')
                        .Append(Format(markers.partPointX)).Append(';')
                        .Append(Format(markers.partPointY)).Append('\n');
                }

                manifest.Append("| ").Append(part.partId).Append(" | ").Append(part.mcName)
                    .Append(" | ").Append(part.category).Append(" | ").Append(part.lr)
                    .Append(" | `PartMounts/").Append(mountFile).Append("` | `Parts/")
                    .Append(SpriteNaming.PartSpriteFile(CanonicalId(mappings, part.mcName), part.mcName))
                    .Append("` | ").Append(markers.holes.Count)
                    .Append(" | ").Append(markers.firePoints.Count).Append(" |\n");
                symbolCount++;
            }

            foreach (PartDefinitionRecord part in parts)
            {
                int canonicalId = CanonicalId(mappings, part.mcName);
                table.Append("P;").Append(part.partId.ToString(CultureInfo.InvariantCulture)).Append(';')
                    .Append(part.mcName).Append(';')
                    .Append(part.lr.ToString(CultureInfo.InvariantCulture)).Append(';')
                    .Append(part.bulletId.ToString(CultureInfo.InvariantCulture)).Append(';')
                    .Append("PartMounts/").Append(SpriteNaming.PartMountFile(canonicalId, part.mcName)).Append(';')
                    .Append("Parts/").Append(SpriteNaming.PartSpriteFile(canonicalId, part.mcName)).Append('\n');
            }

            var bulletTable = new StringBuilder(bullets.Count * 64);
            bulletTable.Append("# ABRevival bullet art table v1\n");
            bulletTable.Append("# B;bulletId;mcName;atlasSymbol;worldWidth;worldHeight\n");
            foreach (BulletArtRecord bullet in bullets)
            {
                string symbol = bullet.mcName == "normal"
                    ? SpriteNaming.BulletSpriteFile("normal")
                    : SpriteNaming.BulletSpriteFile(bullet.mcName);
                if (!bulletSizes.TryGetValue(symbol, out SpritePixelSize size))
                    throw new InvalidOperationException($"Exported bullet art missing for '{symbol}'.");
                bulletTable.Append("B;").Append(bullet.bulletId.ToString(CultureInfo.InvariantCulture)).Append(';')
                    .Append(bullet.mcName).Append(';').Append(symbol).Append(';')
                    .Append(size.Width.ToString(CultureInfo.InvariantCulture)).Append(';')
                    .Append(size.Height.ToString(CultureInfo.InvariantCulture)).Append('\n');
            }

            return new Result
            {
                MountTableText = table.ToString(),
                BulletTableText = bulletTable.ToString(),
                ManifestMarkdown = manifest.ToString(),
                SymbolCount = symbolCount,
                PartCount = parts.Count
            };
        }

        private static int CanonicalId(IReadOnlyList<PartSpriteMapping> mappings, string mcName)
        {
            for (int i = 0; i < mappings.Count; i++)
                if (mappings[i].mcName == mcName) return mappings[i].partId;
            return 0;
        }

        private static XElement RequireSprite(Dictionary<int, XElement> sprites, int id, string label)
        {
            if (!sprites.TryGetValue(id, out XElement sprite))
                throw new InvalidOperationException($"DefineSprite {id} ({label}) is missing from the SWF XML.");
            return sprite;
        }

        private static XElement FindChild(XElement sprite, string name)
        {
            foreach (XElement item in sprite.Elements("subTags").Elements("item"))
                if (item.Attribute("name")?.Value == name) return item;
            return null;
        }

        private sealed class MarkerData
        {
            public float linkX;
            public float linkY;
            public List<MarkerHole> holes = new List<MarkerHole>();
            public List<FirePoint> firePoints = new List<FirePoint>();
            public bool hasPartPoint;
            public float partPointX;
            public float partPointY;
        }

        private readonly struct MarkerHole
        {
            public readonly float x;
            public readonly float y;
            public readonly int lr;
            public readonly int types;

            public MarkerHole(float x, float y, int lr, int types)
            {
                this.x = x;
                this.y = y;
                this.lr = lr;
                this.types = types;
            }
        }

        private readonly struct FirePoint
        {
            public readonly float x;
            public readonly float y;

            public FirePoint(float x, float y)
            {
                this.x = x;
                this.y = y;
            }
        }

        private static MarkerData ReadMarkers(XElement showSprite, string label)
        {
            var markers = new MarkerData();
            var items = new List<(int depth, XElement item)>();
            foreach (XElement item in showSprite.Elements("subTags").Elements("item"))
            {
                string name = item.Attribute("name")?.Value;
                if (string.IsNullOrEmpty(name)) continue;
                if (name[0] != 'L' && name[0] != 'H' && name[0] != 'F' && name[0] != 'P') continue;
                if (name == "HIT") continue;

                XElement matrix = item.Element("matrix");
                if (matrix == null)
                    throw new InvalidOperationException($"Marker '{name}' on '{label}' has no matrix.");
                if (matrix.Attribute("hasRotate")?.Value == "true" || matrix.Attribute("hasScale")?.Value == "true")
                {
                    throw new InvalidOperationException(
                        $"Marker '{name}' on '{label}' is rotated or scaled; the frozen reader uses translate only.");
                }

                int depth = int.Parse(item.Attribute("depth")?.Value ?? "0", CultureInfo.InvariantCulture);
                items.Add((depth, item));
            }

            items.Sort((left, right) => left.depth.CompareTo(right.depth));

            foreach ((int _, XElement item) in items)
            {
                string name = item.Attribute("name").Value;
                double x = ParseDouble(item.Element("matrix").Attribute("translateX"));
                double y = ParseDouble(item.Element("matrix").Attribute("translateY"));
                float worldX = ToWorldX(x);
                float worldY = ToWorldY(y);

                switch (name[0])
                {
                    case 'L':
                        markers.linkX = worldX;
                        markers.linkY = worldY;
                        break;

                    case 'H':
                    {
                        (int lr, int types) = ParseHoleName(name, label);
                        markers.holes.Add(new MarkerHole(worldX, worldY, lr, types));
                        break;
                    }

                    case 'F':
                        markers.firePoints.Add(new FirePoint(worldX, worldY));
                        break;

                    case 'P':
                        markers.hasPartPoint = true;
                        markers.partPointX = worldX;
                        markers.partPointY = worldY;
                        break;
                }
            }

            return markers;
        }

        private static (int lr, int types) ParseHoleName(string name, string label)
        {
            string[] parts = name.Split('_');
            if (parts.Length != 3 ||
                !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int lr))
            {
                throw new InvalidOperationException($"Hole marker '{name}' on '{label}' is not H_<lr>_<types>.");
            }

            int types = 0;
            for (int i = 0; i < parts[2].Length; i++)
            {
                char digit = parts[2][i];
                if (digit < '0' || digit > '7')
                    throw new InvalidOperationException($"Hole marker '{name}' on '{label}' has a non-digit type list.");
                types |= 1 << (digit - '0');
            }
            return (lr, types);
        }

        private static float ToWorldX(double twips)
        {
            return (float)(twips / TwipsPerPixel * SpriteNaming.MountZoom);
        }

        private static float ToWorldY(double twips)
        {
            return -(float)(twips / TwipsPerPixel * SpriteNaming.MountZoom);
        }

        private static double ParseDouble(XAttribute attribute)
        {
            return attribute == null ? 0.0 : double.Parse(attribute.Value, CultureInfo.InvariantCulture);
        }

        private static string Format(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
