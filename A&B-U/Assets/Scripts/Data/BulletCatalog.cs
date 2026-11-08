using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace ABRevival.Core.Data
{
    /// <summary>One frozen <c>F4DataBullet</c> art definition (index and SWF symbol name).</summary>
    public sealed class BulletArtRecord
    {
        public int bulletId;
        public string name;
        public string mcName;
    }

    /// <summary>
    /// Parses the frozen <c>11_all.AllData_defBullet.bin</c> XML for bullet ids and the symbol
    /// used to draw them. Damage/lifetime parsing stays in <see cref="PartCatalogParser"/>.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class BulletCatalog
    {
        public static List<BulletArtRecord> ParseFromFile(string path)
        {
            return Parse(System.IO.File.ReadAllText(path));
        }

        public static List<BulletArtRecord> Parse(string xml)
        {
            var results = new List<BulletArtRecord>();
            if (string.IsNullOrWhiteSpace(xml)) return results;

            var document = XDocument.Parse(xml);
            foreach (XElement element in document.Descendants("bulletOne"))
            {
                var record = new BulletArtRecord
                {
                    bulletId = ParseInt(element.Element("index")?.Value, 0),
                    name = element.Element("name")?.Value ?? string.Empty,
                    mcName = element.Element("mcName")?.Value ?? "normal"
                };
                if (string.IsNullOrEmpty(record.mcName)) record.mcName = "normal";
                results.Add(record);
            }

            results.Sort((left, right) => left.bulletId.CompareTo(right.bulletId));
            return results;
        }

        private static int ParseInt(string value, int fallback)
        {
            return int.TryParse(value, out int result) ? result : fallback;
        }
    }
}
