using System;

namespace ABRevival.Core.Rendering
{
    /// <summary>
    /// Pure BatchRendererGroup layout maths (ADR 0014). Instances are split into
    /// bounded 4096-instance GPU batches so no draw command is unbounded. The
    /// instance data buffer is structure-of-arrays: one contiguous array per
    /// DOTS-instanced property, with each property 16-byte aligned.
    /// </summary>
    public static class SpriteBatchPlan
    {
        public const int MaxInstancesPerBatch = 4096;
        public const int PackedTransformBytes = 48;
        public const int UserValueBytes = 4;
        public const int UvRectBytes = 16;
        public const uint PerInstanceMetadataBit = 0x80000000u;

        public static int BatchCount(int instanceCount)
        {
            if (instanceCount < 0)
                throw new ArgumentOutOfRangeException(nameof(instanceCount));
            return (instanceCount + MaxInstancesPerBatch - 1) / MaxInstancesPerBatch;
        }

        public static int BatchStart(int batchIndex)
        {
            if (batchIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(batchIndex));
            return batchIndex * MaxInstancesPerBatch;
        }

        public static int BatchInstanceCount(int instanceCount, int batchIndex)
        {
            if (instanceCount < 0)
                throw new ArgumentOutOfRangeException(nameof(instanceCount));
            int remaining = instanceCount - BatchStart(batchIndex);
            if (remaining <= 0) return 0;
            return remaining > MaxInstancesPerBatch ? MaxInstancesPerBatch : remaining;
        }

        /// <summary>Rounds a byte offset up to the alignment required by DOTS float4 loads.</summary>
        public static int Align16(int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            return (value + 15) & ~15;
        }

        public static int ObjectToWorldOffset(int capacity)
        {
            ValidateCapacity(capacity);
            return 0;
        }

        public static int UserValueOffset(int capacity)
        {
            ValidateCapacity(capacity);
            return Align16(PackedTransformBytes * capacity);
        }

        public static int UvRectOffset(int capacity)
        {
            ValidateCapacity(capacity);
            return Align16(UserValueOffset(capacity) + UserValueBytes * capacity);
        }

        public static int BufferBytes(int capacity)
        {
            ValidateCapacity(capacity);
            return Align16(UvRectOffset(capacity) + UvRectBytes * capacity);
        }

        public static uint MetadataValue(int byteOffset)
        {
            if (byteOffset < 0 || (uint)byteOffset > 0x7FFFFFFFu)
                throw new ArgumentOutOfRangeException(nameof(byteOffset));
            return PerInstanceMetadataBit | (uint)byteOffset;
        }

        private static void ValidateCapacity(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));
        }
    }

    /// <summary>
    /// 48-byte packed float3x4 transform expected by Unity SRP DOTS instancing.
    /// The unit quad is scaled to the sprite extents, rotated, then translated.
    /// </summary>
    public struct PackedTransform
    {
        public float c0x;
        public float c0y;
        public float c0z;
        public float c1x;
        public float c1y;
        public float c1z;
        public float c2x;
        public float c2y;
        public float c2z;
        public float c3x;
        public float c3y;
        public float c3z;

        public static PackedTransform FromTrs(float x, float y, float width, float height, float rotationRadians)
        {
            float cos = MathF.Cos(rotationRadians);
            float sin = MathF.Sin(rotationRadians);

            return new PackedTransform
            {
                c0x = cos * width,
                c0y = sin * width,
                c0z = 0f,
                c1x = -sin * height,
                c1y = cos * height,
                c1z = 0f,
                c2x = 0f,
                c2y = 0f,
                c2z = 1f,
                c3x = x,
                c3y = y,
                c3z = 0f
            };
        }
    }

    /// <summary>One atlas sprite instance submitted to the BRG renderer.</summary>
    public struct SpriteInstance
    {
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public float RotationRadians;
        public float UvX;
        public float UvY;
        public float UvWidth;
        public float UvHeight;

        /// <summary>Per-part tint packed by <see cref="SpriteUserValue"/>, read as unity_RendererUserValue.</summary>
        public uint UserValue;
    }

    /// <summary>
    /// Packs the per-part damage tint into the single uint32 exposed to shaders as
    /// unity_RendererUserValue. R is the low byte, then G, B, A.
    /// </summary>
    public static class SpriteUserValue
    {
        public const uint Pristine = 0xFFFFFFFFu;

        public static uint PackTint(byte r, byte g, byte b, byte a)
        {
            return r | ((uint)g << 8) | ((uint)b << 16) | ((uint)a << 24);
        }

        /// <summary>Pristine white at 0 damage, full red at 1.</summary>
        public static uint DamageTint(float damage01)
        {
            float damage = damage01 < 0f ? 0f : damage01 > 1f ? 1f : damage01;
            byte greenBlue = (byte)(255f * (1f - damage) + 0.5f);
            return PackTint(255, greenBlue, greenBlue, 255);
        }
    }
}
