using System;
using Unity.Mathematics;

namespace ABRevival.Core.Data
{
    /// <summary>
    /// One mounted part resolved to hull-local space from the frozen <c>defFly</c> tree and
    /// the part's <c>H</c>/<c>L</c> markers. <see cref="localRotation"/> is always zero today
    /// because the frozen part catalog carries no <c>rot</c> value.
    /// </summary>
    public readonly struct CraftPartPlacement
    {
        public readonly int partIndex;
        public readonly int partId;
        public readonly PartCategory category;
        public readonly float2 localOffset;
        public readonly float localRotation;
        public readonly bool mirrored;

        public CraftPartPlacement(
            int partIndex,
            int partId,
            PartCategory category,
            float2 localOffset,
            float localRotation,
            bool mirrored)
        {
            this.partIndex = partIndex;
            this.partId = partId;
            this.category = category;
            this.localOffset = localOffset;
            this.localRotation = localRotation;
            this.mirrored = mirrored;
        }
    }

    /// <summary>
    /// Resolves a frozen <c>defFly</c> craft composition into hull-local part placements.
    /// The frozen baseline positions each child at <c>hole.xy - child.linkOff</c> (Maker.makePartByLink);
    /// this class applies the same rule in Unity 2D space and mirrors right-hand variants.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class CraftComposition
    {
        public static int Build(
            CraftStatRecord craft,
            PartMountTable mounts,
            Span<CraftPartPlacement> output)
        {
            if (craft == null || craft.parts == null || mounts == null || output.Length == 0) return 0;

            int count = 0;
            for (int i = 0; i < craft.parts.Length && count < output.Length; i++)
            {
                CraftPartRecord part = craft.parts[i];
                if (part == null) continue;

                float2 offset = float2.zero;
                bool mirrored = part.lr == 1;

                if (part.parentIndex >= 0)
                {
                    if (part.parentIndex >= count)
                        throw new InvalidOperationException(
                            $"Craft {craft.defIndex} part {i} references parent {part.parentIndex} before it was placed.");

                    CraftPartPlacement parent = output[part.parentIndex];
                    if (!mounts.TryResolveSocket(
                            parent.partId,
                            part.socketIndex,
                            part.partId,
                            parent.mirrored,
                            out float2 socketOffset))
                    {
                        // Frozen marker missing or undecodable: use the closest derivable
                        // layout instead of silently drawing every child at the parent centre.
                        PartMountSymbol parentSymbol = mounts.GetSymbol(MountSymbolName(mounts, parent.partId));
                        socketOffset = PartMountTable.FallbackOffset(parentSymbol, part.category, count);
                    }

                    offset = parent.localOffset + socketOffset;
                }

                output[count] = new CraftPartPlacement(count, part.partId, part.category, offset, 0f, mirrored);
                count++;
            }

            return count;
        }

        private static string MountSymbolName(PartMountTable mounts, int partId)
        {
            return mounts.TryGetPart(partId, out PartMountEntry entry) ? entry.mcName : null;
        }
    }
}
