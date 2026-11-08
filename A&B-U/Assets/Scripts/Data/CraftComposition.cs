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
        /// <summary>Render layer order used by the hangar preview and the mission (ADR 0019).</summary>
        public static int Layer(PartCategory category)
        {
            switch (category)
            {
                case PartCategory.Engine: return 0;
                case PartCategory.Body: return 1;
                case PartCategory.Wing: return 2;
                default: return 3;
            }
        }

        /// <summary>
        /// Resolves a player blueprint (body plus mounted parts) with the same socket,
        /// mirroring and layer rules as the mission. <see cref="CraftPartPlacement.partIndex"/>
        /// is the index into <paramref name="parts"/>; output is sorted engine, hull, wing, gun.
        /// </summary>
        public static int BuildPlayer(
            ReadOnlySpan<MountedPartData> parts,
            PartMountTable mounts,
            Span<CraftPartPlacement> output)
        {
            if (parts.Length == 0 || output.Length == 0) return 0;

            int bodyIndex = -1;
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].category != PartCategory.Body) continue;
                bodyIndex = i;
                break;
            }

            if (bodyIndex < 0) bodyIndex = 0;

            MountedPartData body = parts[bodyIndex];
            PartMountSymbol bodySymbol = null;
            if (mounts != null && mounts.TryGetPart(body.partId, out PartMountEntry bodyEntry))
                bodySymbol = mounts.GetSymbol(bodyEntry.mcName);

            int count = 0;
            output[count++] = new CraftPartPlacement(
                bodyIndex, body.partId, PartCategory.Body, float2.zero, 0f, body.mirrored);

            ulong usedHoles = 0;
            int siblingIndex = 0;
            for (int i = 0; i < parts.Length && count < output.Length; i++)
            {
                if (i == bodyIndex) continue;

                MountedPartData part = parts[i];
                float2 offset;
                if (math.lengthsq(part.localOffset) > 1e-4f)
                {
                    // Explicit blueprint placement (seeded from the frozen defFly tree or edited
                    // in Factory) wins over re-deriving a socket from category alone.
                    offset = part.localOffset;
                }
                else if (mounts == null ||
                         !TryTakeHole(bodySymbol, part.category, part.mirrored, ref usedHoles, out int holeIndex) ||
                         !mounts.TryResolveSocket(body.partId, holeIndex, part.partId, body.mirrored, out offset))
                {
                    // No explicit placement and no decodable matching marker: use the closest
                    // derivable layout. Never silently centre a child on the parent.
                    offset = PartMountTable.FallbackOffset(bodySymbol, part.category, siblingIndex);
                }

                output[count++] = new CraftPartPlacement(
                    i, part.partId, part.category, offset, part.localRotation, part.mirrored);
                siblingIndex++;
            }

            SortByLayer(output.Slice(0, count));
            return count;
        }

        private static bool TryTakeHole(
            PartMountSymbol bodySymbol,
            PartCategory category,
            bool mirrored,
            ref ulong usedHoles,
            out int holeIndex)
        {
            holeIndex = -1;
            if (bodySymbol == null || bodySymbol.holes == null) return false;

            int wantedLr = mirrored ? 1 : 0;
            for (int i = 0; i < bodySymbol.holes.Length && i < 64; i++)
            {
                PartMountHole hole = bodySymbol.holes[i];
                if (hole == null || (usedHoles & (1UL << i)) != 0) continue;
                if (!hole.Accepts(category)) continue;
                if (hole.lr != 2 && hole.lr != wantedLr) continue;

                usedHoles |= 1UL << i;
                holeIndex = i;
                return true;
            }

            return false;
        }

        private static void SortByLayer(Span<CraftPartPlacement> placements)
        {
            for (int i = 1; i < placements.Length; i++)
            {
                CraftPartPlacement item = placements[i];
                int layer = Layer(item.category);
                int j = i - 1;
                while (j >= 0 && Layer(placements[j].category) > layer)
                {
                    placements[j + 1] = placements[j];
                    j--;
                }

                placements[j + 1] = item;
            }
        }

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
