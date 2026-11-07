using UnityEngine;
using Unity.Mathematics;

namespace ABRevival.Core.Data
{
    [CreateAssetMenu(fileName = "NewPartDefinition", menuName = "ABRevival/Part Definition")]
    public class PartDefinitionSO : ScriptableObject
    {
        public int partId;
        public string partName;
        public PartCategory category;
        public int cost;
        public float health;
        public float mass;
        public float thrust;
        public float energyDraw;

        [Header("Weapon Attributes (Category == Gun)")]
        public float projectileVelocity;
        public float burstAcceleration;
        public float bulletLifetime;
        public float bulletDamage;

        /// <summary>
        /// Calculates theoretical weapon effective firing range using ADR 0012 formula:
        /// d = v0 * t + 0.5 * a * t^2
        /// </summary>
        public float CalculateEffectiveRange()
        {
            if (category != PartCategory.Gun) return 0f;
            return (projectileVelocity * bulletLifetime) + (0.5f * burstAcceleration * bulletLifetime * bulletLifetime);
        }
    }
}
