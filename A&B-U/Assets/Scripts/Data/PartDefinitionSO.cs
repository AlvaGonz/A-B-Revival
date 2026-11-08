using UnityEngine;
using Unity.Mathematics;
using ABRevival.Core.Simulation;

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

        [Header("Hangar Table (F4DataOrg original columns)")]
        public float turnFly;
        public int lr;

        [Header("Hangar Presentation")]
        public Sprite partSprite;

        [Header("Weapon Attributes (Category == Gun)")]
        public float projectileVelocity;
        public float burstAcceleration;
        public float bulletLifetime;
        public float bulletDamage;

        [Header("Fire Control (F4DataFire, frozen frame units)")]
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
    }
}

