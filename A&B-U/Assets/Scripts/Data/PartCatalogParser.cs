using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using ABRevival.Core.Simulation;

namespace ABRevival.Core.Data
{
    /// <summary>
    /// Pure C# data record representing a parsed ship component from legacy XML definitions.
    /// Free of Unity native engine ECall dependencies, enabling CLI execution and testing.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class PartDefinitionRecord
    {
        public int partId;
        public string partName;
        public string mcName;
        public PartCategory category;
        public float mass;
        public float health;
        public float thrust;
        public float energyDraw;

        // Weapon attributes (Category == Gun), raw frozen frame units.
        public float projectileVelocity;
        public float burstAcceleration;
        public float bulletLifetime;
        public float bulletDamage;

        // Fire-control attributes from F4DataFire (raw frozen frame units).
        public int fireNum;
        public int fireFrame;
        public int fireStop;
        public int fireEx;
        public float spreadDegrees;
        public float muzzleSpeedAdd;
        public float accelerationAdd;
        public float angleAddDegrees;

        /// <summary>
        /// Calculates theoretical weapon effective firing range using ADR 0012 formula:
        /// d = v0 * t + 0.5 * a * t^2, with the burst-average muzzle speed and acceleration.
        /// </summary>
        public float CalculateEffectiveRange()
        {
            if (category != PartCategory.Gun) return 0f;
            float v0 = projectileVelocity + (muzzleSpeedAdd * fireNum * 0.5f);
            float acc = burstAcceleration + (accelerationAdd * fireNum * 0.5f);
            return Ballistics.EffectiveRange(v0, acc, bulletLifetime);
        }

        /// <summary>Converts the raw catalog values into Unity seconds and pixels per second.</summary>
        public FireModeSpec CreateWeaponSpec()
        {
            return FireModeSpec.FromCatalog(
                fireNum, fireFrame, fireStop, fireEx, spreadDegrees,
                projectileVelocity, muzzleSpeedAdd, burstAcceleration, accelerationAdd,
                angleAddDegrees, bulletLifetime, bulletDamage);
        }

        public void PopulateScriptableObject(PartDefinitionSO so)
        {
            if (so == null) return;
            so.partId = partId;
            so.partName = partName;
            so.category = category;
            so.mass = mass;
            so.health = health;
            so.thrust = thrust;
            so.energyDraw = energyDraw;
            so.projectileVelocity = projectileVelocity;
            so.burstAcceleration = burstAcceleration;
            so.bulletLifetime = bulletLifetime;
            so.bulletDamage = bulletDamage;
            so.fireNum = fireNum;
            so.fireFrame = fireFrame;
            so.fireStop = fireStop;
            so.fireEx = fireEx;
            so.spreadDegrees = spreadDegrees;
            so.muzzleSpeedAdd = muzzleSpeedAdd;
            so.accelerationAdd = accelerationAdd;
            so.angleAddDegrees = angleAddDegrees;
        }
    }

    /// <summary>
    /// Pure core utility to parse XML part catalog definitions from frozen AS3 binaryData assets
    /// (8_all.AllData_defPart.bin, 2_all.AllData_defFire.bin, 11_all.AllData_defBullet.bin)
    /// into PartDefinitionRecord entries with ADR 0012 weapon range calculation.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class PartCatalogParser
    {
        private struct FireDef
        {
            public float spdS;
            public float spdAdd;
            public float accS;
            public float accAdd;
            public float angAdd;
            public float fireAcc;
            public int fireNum;
            public int fireFrame;
            public int fireStop;
            public int fireEx;
        }

        private struct BulletDef
        {
            public float dmg;
            public float life;
        }

        public static List<PartDefinitionRecord> ParseCatalogFromFiles(string partPath, string firePath, string bulletPath)
        {
            string partXml = File.ReadAllText(partPath);
            string fireXml = File.ReadAllText(firePath);
            string bulletXml = File.ReadAllText(bulletPath);

            return ParseCatalogFromStrings(partXml, fireXml, bulletXml);
        }

        public static List<PartDefinitionRecord> ParseCatalogFromStrings(string partXml, string fireXml, string bulletXml)
        {
            var invCulture = CultureInfo.InvariantCulture;

            // 1. Parse Fire definitions
            var fireLookup = new Dictionary<int, FireDef>();
            if (!string.IsNullOrWhiteSpace(fireXml))
            {
                var fireDoc = XDocument.Parse(fireXml);
                foreach (var el in fireDoc.Descendants("fireOne"))
                {
                    int index = ParseInt(el.Element("index")?.Value, 0);
                    fireLookup[index] = new FireDef
                    {
                        spdS = ParseFloat(el.Element("spdS")?.Value, 0f, invCulture),
                        spdAdd = ParseFloat(el.Element("spdAdd")?.Value, 0f, invCulture),
                        accS = ParseFloat(el.Element("accS")?.Value, 0f, invCulture),
                        accAdd = ParseFloat(el.Element("accAdd")?.Value, 0f, invCulture),
                        angAdd = ParseFloat(el.Element("angAdd")?.Value, 0f, invCulture),
                        fireAcc = ParseFloat(el.Element("fireAcc")?.Value, 0f, invCulture),
                        fireNum = ParseInt(el.Element("fireNum")?.Value, 0),
                        fireFrame = ParseInt(el.Element("fireFrame")?.Value, 0),
                        fireStop = ParseInt(el.Element("fireStop")?.Value, 0),
                        fireEx = ParseInt(el.Element("fireEx")?.Value, 0)
                    };
                }
            }

            // 2. Parse Bullet definitions
            var bulletLookup = new Dictionary<int, BulletDef>();
            if (!string.IsNullOrWhiteSpace(bulletXml))
            {
                var bulletDoc = XDocument.Parse(bulletXml);
                foreach (var el in bulletDoc.Descendants("bulletOne"))
                {
                    int index = ParseInt(el.Element("index")?.Value, 0);
                    float dmg = ParseFloat(el.Element("dmg")?.Value, 0f, invCulture);
                    float life = ParseFloat(el.Element("life")?.Value, 0f, invCulture);
                    bulletLookup[index] = new BulletDef { dmg = dmg, life = life };
                }
            }

            // 3. Parse Part definitions
            var results = new List<PartDefinitionRecord>();
            if (!string.IsNullOrWhiteSpace(partXml))
            {
                var partDoc = XDocument.Parse(partXml);
                foreach (var el in partDoc.Descendants("partOne"))
                {
                    var record = new PartDefinitionRecord();

                    record.partId = ParseInt(el.Element("index")?.Value, 0);
                    record.partName = el.Element("name")?.Value ?? string.Empty;
                    record.mcName = el.Element("mcName")?.Value ?? string.Empty;

                    int typeInt = ParseInt(el.Element("type")?.Value, 0);
                    record.category = typeInt switch
                    {
                        0 => PartCategory.Body,
                        1 => PartCategory.Wing,
                        2 => PartCategory.Engine,
                        3 => PartCategory.Gun,
                        _ => PartCategory.Body
                    };

                    record.mass = ParseFloat(el.Element("mass")?.Value, 0f, invCulture);
                    record.health = ParseFloat(el.Element("maxhp")?.Value, 0f, invCulture);
                    record.thrust = ParseFloat(el.Element("powFly")?.Value, 0f, invCulture);
                    record.energyDraw = 0f; // Original Flash baseline managed energy at craft/hull level

                    if (record.category == PartCategory.Gun)
                    {
                        int fireIdx = ParseInt(el.Element("fire")?.Value, 0);
                        int bulletIdx = ParseInt(el.Element("bullet")?.Value, 0);

                        if (fireLookup.TryGetValue(fireIdx, out var fDef))
                        {
                            record.projectileVelocity = fDef.spdS;
                            record.burstAcceleration = fDef.accS;
                            record.fireNum = fDef.fireNum;
                            record.fireFrame = fDef.fireFrame;
                            record.fireStop = fDef.fireStop;
                            record.fireEx = fDef.fireEx;
                            record.spreadDegrees = fDef.fireAcc;
                            record.muzzleSpeedAdd = fDef.spdAdd;
                            record.accelerationAdd = fDef.accAdd;
                            record.angleAddDegrees = fDef.angAdd;
                        }

                        if (bulletLookup.TryGetValue(bulletIdx, out var bDef))
                        {
                            record.bulletLifetime = bDef.life;
                            record.bulletDamage = bDef.dmg;
                        }
                    }

                    results.Add(record);
                }
            }

            return results;
        }

        private static int ParseInt(string value, int fallback)
        {
            if (string.IsNullOrEmpty(value)) return fallback;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int val) ? val : fallback;
        }

        private static float ParseFloat(string value, float fallback, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(value)) return fallback;
            return float.TryParse(value, NumberStyles.Float, culture, out float val) ? val : fallback;
        }
    }
}
