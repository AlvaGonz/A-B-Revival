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

        /// <summary>F4DataOrg.lr: 0 left, 1 right (mirrored art), 2 both/neutral.</summary>
        public int lr;

        /// <summary>F4DataBullet index this part's gun fires; 0 when not a gun.</summary>
        public int bulletId;
        public float mass;
        public float health;
        public float thrust;
        public float turnFly;
        public float energyDraw;
        public int cost;

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
        public float accAngS;

        // Bullet power terms from F4DataBullet.
        public float bulletHp;
        public float bulletDeadAct;
        public float bulletBtwh;

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

        /// <summary>F4DataFire.firePowRate in frozen frame units.</summary>
        public float CalculateFirePowerRate()
        {
            float window = fireEx / (float)fireFrame;
            float burst = window > 0f
                ? (0.5f * accAngS + projectileVelocity * 15f) * fireNum / window
                : 0f;
            float rate = burst + 1000f / (fireFrame + fireStop);
            return float.IsNaN(rate) || float.IsInfinity(rate) ? 0f : rate;
        }

        /// <summary>F4DataBullet.bulletPow with the frozen 60 FPS life rescale.</summary>
        public float CalculateBulletPower()
        {
            float lifeAt60 = (float)Math.Round(bulletLifetime * (60f / 36f), MidpointRounding.AwayFromZero);
            float hit = bulletBtwh > 0f ? bulletBtwh : 2f;
            return bulletDamage * (lifeAt60 + 1f) + bulletHp + bulletDeadAct + (hit + hit) * 8f;
        }

        /// <summary>Frozen display fire power for one part (FormShop/ShowPart).</summary>
        public float CalculateDisplayFirePower(int firePointCount)
        {
            if (category != PartCategory.Gun || firePointCount <= 0) return 0f;
            return firePointCount * CalculateFirePowerRate() * CalculateBulletPower() / 300000f;
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
            so.turnFly = turnFly;
            so.lr = lr;
            so.energyDraw = energyDraw;
            so.cost = cost;
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
            so.accAngS = accAngS;
            so.bulletHp = bulletHp;
            so.bulletDeadAct = bulletDeadAct;
            so.bulletBtwh = bulletBtwh;
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
            public float accAngS;
            public float angAdd;
            public float fireAcc;
            public int fireNum;
            public int fireFrame;
            public int fireStop;
            public int fireEx;

            /// <summary>F4DataFire.firePowRate, evaluated with the legacy formula.</summary>
            public float FirePowerRate
            {
                get
                {
                    float window = fireEx / (float)fireFrame;
                    float burst = window > 0f
                        ? (0.5f * accAngS + spdS * 15f) * fireNum / window
                        : 0f;
                    float rate = burst + 1000f / (fireFrame + fireStop);
                    return float.IsNaN(rate) || float.IsInfinity(rate) ? 0f : rate;
                }
            }
        }

        private struct BulletDef
        {
            public float dmg;
            public float life;
            public float hp;
            public float deadAct;
            public float btwh;

            /// <summary>F4DataBullet.bulletPow, using the 60 FPS life rescale from readFromObj.</summary>
            public float BulletPower
            {
                get
                {
                    float lifeAt60 = life * (60f / 36f);
                    // shortcut: non-normal bullet symbols carry their hit box in the SWF symbol.
                    // btwh is the exported fallback; assets without it use the frozen 2 px default.
                    float hit = btwh > 0f ? btwh : 2f;
                    return dmg * (lifeAt60 + 1f) + hp + deadAct + (hit + hit) * 8f;
                }
            }
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
                        accAngS = ParseFloat(el.Element("accAngS")?.Value, 0f, invCulture),
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
                    bulletLookup[index] = new BulletDef
                    {
                        dmg = dmg,
                        life = life,
                        hp = ParseFloat(el.Element("hp")?.Value, 0f, invCulture),
                        deadAct = ParseFloat(el.Element("deadAct")?.Value, 0f, invCulture),
                        btwh = ParseFloat(el.Element("btwh")?.Value, 0f, invCulture)
                    };
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
                    record.lr = ParseInt(el.Element("lr")?.Value, 2);

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
                    record.turnFly = ParseFloat(el.Element("turnFly")?.Value, 0f, invCulture);
                    record.energyDraw = 0f; // Original Flash baseline managed energy at craft/hull level

                    var fireDef = default(FireDef);
                    var bulletDef = default(BulletDef);
                    bool hasFire = false;
                    bool hasBullet = false;

                    if (record.category == PartCategory.Gun)
                    {
                        int fireIdx = ParseInt(el.Element("fire")?.Value, 0);
                        int bulletIdx = ParseInt(el.Element("bullet")?.Value, 0);
                        record.bulletId = bulletIdx;

                        if (fireLookup.TryGetValue(fireIdx, out fireDef))
                        {
                            hasFire = true;
                            record.projectileVelocity = fireDef.spdS;
                            record.burstAcceleration = fireDef.accS;
                            record.fireNum = fireDef.fireNum;
                            record.fireFrame = fireDef.fireFrame;
                            record.fireStop = fireDef.fireStop;
                            record.fireEx = fireDef.fireEx;
                            record.spreadDegrees = fireDef.fireAcc;
                            record.muzzleSpeedAdd = fireDef.spdAdd;
                            record.accelerationAdd = fireDef.accAdd;
                            record.angleAddDegrees = fireDef.angAdd;
                            record.accAngS = fireDef.accAngS;
                        }

                        if (bulletLookup.TryGetValue(bulletIdx, out bulletDef))
                        {
                            hasBullet = true;
                            record.bulletLifetime = bulletDef.life;
                            record.bulletDamage = bulletDef.dmg;
                            record.bulletHp = bulletDef.hp;
                            record.bulletDeadAct = bulletDef.deadAct;
                            record.bulletBtwh = bulletDef.btwh;
                        }
                    }

                    record.cost = CalculateLegacyCost(record, hasFire, fireDef, hasBullet, bulletDef);
                    results.Add(record);
                }
            }

            return results;
        }

        /// <summary>
        /// F4DataOrg.makeCost in its frozen form. The XML does not carry holes, fire-point
        /// positions, or the bullet hit box, so the metadata-dependent terms use the frozen
        /// defaults: one fire point for a gun, no holes, and btwh (or 2 px) for the bullet.
        /// The resulting price is non-zero and uses every parsed legacy term.
        /// </summary>
        private static int CalculateLegacyCost(
            PartDefinitionRecord record,
            bool hasFire,
            FireDef fireDef,
            bool hasBullet,
            BulletDef bulletDef)
        {
            float healthTerm = record.health * 24f;
            float turnRatio = record.mass > 0f ? record.turnFly / record.mass : 0f;
            float thrustRatio = record.mass > 0f ? record.thrust / record.mass : 0f;

            float firePoints = record.category == PartCategory.Gun && hasFire && hasBullet ? 1f : 0f;
            float firePower = hasFire ? fireDef.FirePowerRate : 0f;
            if (record.category == PartCategory.Body) firePower *= 0.11f;
            float bulletPower = hasBullet ? bulletDef.BulletPower : 0f;

            float multiplier = 1f + (turnRatio * 74f) + (thrustRatio * 54f);
            float total = (healthTerm + firePoints * firePower * bulletPower) * multiplier * 0.0065f;
            if (float.IsNaN(total) || float.IsInfinity(total) || total <= 0f) return 0;
            return (int)Math.Ceiling(total);
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
