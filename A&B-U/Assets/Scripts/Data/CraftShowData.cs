using System;
using Unity.Mathematics;

namespace ABRevival.Core.Data
{
    /// <summary>One mounted part's contribution to the frozen F4ShowData craft card.</summary>
    public readonly struct CraftPartStat
    {
        public readonly PartCategory category;
        public readonly float mass;
        public readonly float health;
        public readonly float thrust;
        public readonly float turnFly;
        public readonly float cost;
        public readonly float firePower;

        public CraftPartStat(
            PartCategory category,
            float mass,
            float health,
            float thrust,
            float turnFly,
            float cost,
            float firePower = 0f)
        {
            this.category = category;
            this.mass = mass;
            this.health = health;
            this.thrust = thrust;
            this.turnFly = turnFly;
            this.cost = cost;
            this.firePower = firePower;
        }
    }

    /// <summary>
    /// Center stat card values using the frozen F4ShowData formulas (Hp, Speed, Steering,
    /// Fire pow., Class, Parts, Value). Pure C# so the Tier 1 gate pins the numbers.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public sealed class CraftShowStats
    {
        public string name = string.Empty;
        public int hp;
        public float fire;
        public float speed;
        public float steering;
        public string cls = "F";
        public int parts;
        public int value;
        public float mass;
    }

    public static class CraftShowData
    {
        /// <summary>F4ShowData.initData class thresholds against (hp/1000 + steering + speed + fire) / 100.</summary>
        public static string ClassFor(float score)
        {
            if (score < 0.5f) return "F";
            if (score < 1f) return "E";
            if (score < 1.5f) return "D";
            if (score < 2.3f) return "C";
            if (score < 4f) return "B";
            if (score < 5.8f) return "A";
            return "S";
        }

        /// <summary>
        /// Sums every mounted part. When no Body part is present, the hull header is the
        /// chassis contribution, matching F4ShowData's root node.
        /// </summary>
        public static CraftShowStats Evaluate(
            string craftName,
            string hullName,
            float hullMass,
            float hullHealth,
            ReadOnlySpan<CraftPartStat> parts)
        {
            var stats = new CraftShowStats
            {
                name = string.IsNullOrEmpty(craftName) ? hullName : craftName,
                parts = parts.Length
            };

            bool hasBody = false;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].category == PartCategory.Body)
                {
                    hasBody = true;
                    break;
                }
            }

            if (!hasBody)
            {
                stats.hp += Round(hullHealth);
                stats.mass += hullMass;
                stats.parts++;
            }

            float turnFly = 0f;
            for (int i = 0; i < parts.Length; i++)
            {
                CraftPartStat part = parts[i];
                stats.hp += Round(part.health);
                stats.mass += part.mass;
                stats.value += Round(part.cost);
                stats.fire += part.firePower;
                turnFly += part.turnFly;
                stats.speed += part.thrust;
            }

            if (stats.mass > 0f)
            {
                stats.speed = stats.speed / stats.mass * 100f;
                stats.steering = turnFly / stats.mass * (180f / math.PI) * 10f;
            }

            float score = (stats.hp / 1000f + stats.steering + stats.speed + stats.fire) / 100f;
            stats.cls = ClassFor(score);
            return stats;
        }

        private static int Round(float value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }
    }
}
