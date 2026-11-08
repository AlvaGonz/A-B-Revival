using System;
using Unity.Mathematics;

namespace ABRevival.Core.Data
{
    [Serializable]
    public struct MountedPartData
    {
        public int partId;
        public PartCategory category;
        public float health;
        public float maxHealth;
        public float mass;
        public float thrust;
        public float energyDraw;
        public float2 localOffset;
        public float localRotation;

        /// <summary>Right-hand part variant: the frozen baseline draws its art with scaleX = -1.</summary>
        public bool mirrored;
    }

    public struct AggregateCraftStats
    {
        public float totalMass;
        public float totalHealth;
        public float totalThrust;
        public float totalEnergyDraw;
    }
}

