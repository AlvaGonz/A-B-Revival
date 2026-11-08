using System;
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
        [Tooltip("Opaque sprite rect in texture pixels (x, y, width, height); zero falls back to the full sprite rect.")]
        public Vector4 spriteContentRect;

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
        public float accAngS;

        [Header("Bullet Power Terms (F4DataBullet)")]
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

        /// <summary>
        /// Frozen display fire power for one part: fire points * firePowRate * bulletPow / 300000
        /// (FormShop.f4linkToArray / ShowPart.addPart).
        /// </summary>
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
    }
}

