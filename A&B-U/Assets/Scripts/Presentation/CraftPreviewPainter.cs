using System;
using System.Collections.Generic;
using System.IO;
using ABRevival.Core.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace ABRevival.Presentation
{
    /// <summary>2x mount sprite plus its opaque pixel rect in bottom-left texture space.</summary>
    [Serializable]
    public sealed class MountArtEntry
    {
        public Sprite sprite;
        public Vector4 contentRect;
    }

    /// <summary>
    /// Resolves a part id to the frozen in-game SHOW sprite. All screens that draw a craft
    /// (Center, Factory, mission) share this lookup so the 2x art and opaque rects agree.
    /// </summary>
    public sealed class MountArtLookup
    {
        private readonly PartMountTable _mounts;
        private readonly Dictionary<string, MountArtEntry> _byName;

        public MountArtLookup(PartMountTable mounts, MountArtEntry[] entries)
        {
            _mounts = mounts;
            _byName = new Dictionary<string, MountArtEntry>(entries?.Length ?? 0, StringComparer.Ordinal);
            if (entries == null) return;

            for (int i = 0; i < entries.Length; i++)
            {
                MountArtEntry entry = entries[i];
                if (entry == null || entry.sprite == null) continue;
                // Single-sprite textures import the sub-sprite as "<file>_0".
                string name = entry.sprite.name;
                string key = name.EndsWith("_0", StringComparison.Ordinal)
                    ? name.Substring(0, name.Length - 2)
                    : name;
                _byName[key] = entry;
            }
        }

        public bool HasMount(int partId)
        {
            if (_mounts == null || !_mounts.TryGetPart(partId, out PartMountEntry mount)) return false;
            if (string.IsNullOrEmpty(mount.mountFile)) return false;
            string name = Path.GetFileNameWithoutExtension(mount.mountFile);
            return name != null && _byName.ContainsKey(name);
        }

        public bool TryResolve(
            int partId,
            out Sprite sprite,
            out Vector2 contentCenter,
            out Vector2 contentSize)
        {
            sprite = null;
            contentCenter = Vector2.zero;
            contentSize = Vector2.zero;
            if (!HasMount(partId)) return false;

            _mounts.TryGetPart(partId, out PartMountEntry mount);
            string name = Path.GetFileNameWithoutExtension(mount.mountFile);
            MountArtEntry entry = _byName[name];
            sprite = entry.sprite;

            Vector4 content = entry.contentRect;
            if (content.z > 0f && content.w > 0f)
            {
                contentCenter = new Vector2(
                    content.x + (content.z * 0.5f) - (sprite.rect.width * 0.5f),
                    content.y + (content.w * 0.5f) - (sprite.rect.height * 0.5f));
                contentSize = new Vector2(content.z, content.w);
            }
            else
            {
                contentSize = sprite.rect.size;
            }

            return true;
        }
    }

    /// <summary>Measured craft bounds and the panel scale chosen for one preview frame.</summary>
    public readonly struct CraftPreviewFit
    {
        public readonly float scale;
        public readonly float boundsWidth;
        public readonly float boundsHeight;
        public readonly float boundsCenterX;
        public readonly float boundsCenterY;

        public CraftPreviewFit(
            float scale,
            float boundsWidth,
            float boundsHeight,
            float boundsCenterX,
            float boundsCenterY)
        {
            this.scale = scale;
            this.boundsWidth = boundsWidth;
            this.boundsHeight = boundsHeight;
            this.boundsCenterX = boundsCenterX;
            this.boundsCenterY = boundsCenterY;
        }

        public Vector2 ScaledSize => new Vector2(boundsWidth * scale, boundsHeight * scale);
    }

    /// <summary>
    /// Shared craft preview composition: collects a <see cref="CraftAssembly"/>, resolves it
    /// through <see cref="CraftComposition"/> (socket offsets, mirroring, layer order) and
    /// paints the 2x mount art into a UI Toolkit canvas. Center, Factory and the mission use
    /// the same placement set, so a craft looks identical in every screen.
    /// Original game IP: Vargames (2011).
    /// </summary>
    public static class CraftPreviewPainter
    {
        public const float DefaultAreaFill = 0.65f;
        public const float DefaultMaxDimensionFill = 0.96f;
        public const float MinScale = 0.15f;
        public const float MaxScale = 24f;

        public static int CollectParts(
            CraftAssembly assembly,
            IReadOnlyDictionary<int, PartDefinitionSO> definitions,
            Span<MountedPartData> parts)
        {
            if (assembly == null) return 0;

            int count = 0;
            for (int i = 0; i < CraftAssembly.MaxCapacity && count < parts.Length; i++)
            {
                if (!assembly.IsSlotOccupied(i)) continue;
                MountedPartData part = assembly.GetPart(i);
                // The save payload does not persist the mirror variant; the catalog is authoritative.
                if (definitions != null && definitions.TryGetValue(part.partId, out PartDefinitionSO definition))
                    part.mirrored = definition.lr == 1;
                parts[count++] = part;
            }

            return count;
        }

        public static int PlaceParts(
            ReadOnlySpan<MountedPartData> parts,
            PartMountTable mounts,
            Span<CraftPartPlacement> placements)
        {
            return CraftComposition.BuildPlayer(parts, mounts, placements);
        }

        /// <summary>True when every placement resolves to its 2x mount sprite; never mixes sets.</summary>
        public static bool AllUseMountArt(
            ReadOnlySpan<CraftPartPlacement> placements,
            MountArtLookup art,
            out List<int> missing)
        {
            bool all = true;
            missing = null;
            if (art == null) return false;

            for (int i = 0; i < placements.Length; i++)
            {
                if (art.HasMount(placements[i].partId)) continue;
                all = false;
                missing ??= new List<int>();
                if (!missing.Contains(placements[i].partId)) missing.Add(placements[i].partId);
            }

            return all;
        }

        public static CraftPreviewFit Fit(
            ReadOnlySpan<CraftPartPlacement> placements,
            IReadOnlyDictionary<int, PartDefinitionSO> definitions,
            MountArtLookup art,
            bool useMountArt,
            float canvasWidth,
            float canvasHeight,
            float areaFill = DefaultAreaFill,
            float maxDimensionFill = DefaultMaxDimensionFill)
        {
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            for (int i = 0; i < placements.Length; i++)
            {
                CraftPartPlacement placement = placements[i];
                if (!TryResolveSprite(
                        placement.partId, definitions, art, useMountArt,
                        out _, out Vector2 contentCenter, out Vector2 contentSize))
                {
                    continue;
                }

                if (placement.mirrored) contentCenter.x = -contentCenter.x;
                float x = placement.localOffset.x + contentCenter.x;
                float y = placement.localOffset.y + contentCenter.y;
                minX = Mathf.Min(minX, x - (contentSize.x * 0.5f));
                maxX = Mathf.Max(maxX, x + (contentSize.x * 0.5f));
                minY = Mathf.Min(minY, y - (contentSize.y * 0.5f));
                maxY = Mathf.Max(maxY, y + (contentSize.y * 0.5f));
            }

            if (minX > maxX) return default;

            float boundsWidth = Mathf.Max(1f, maxX - minX);
            float boundsHeight = Mathf.Max(1f, maxY - minY);
            float areaScale = Mathf.Sqrt(areaFill * canvasWidth * canvasHeight / (boundsWidth * boundsHeight));
            float fitScale = Mathf.Min(
                canvasWidth * maxDimensionFill / boundsWidth,
                canvasHeight * maxDimensionFill / boundsHeight);
            float scale = Mathf.Clamp(Mathf.Min(areaScale, fitScale), MinScale, MaxScale);

            return new CraftPreviewFit(
                scale, boundsWidth, boundsHeight, (minX + maxX) * 0.5f, (minY + maxY) * 0.5f);
        }

        /// <summary>
        /// Paints one fit into the canvas. <paramref name="onPartSelected"/> is null for a
        /// read-only preview (Center) and set for the Factory, where parts are clickable.
        /// Returns the number of part elements added.
        /// </summary>
        public static int Paint(
            VisualElement canvas,
            ReadOnlySpan<CraftPartPlacement> placements,
            IReadOnlyDictionary<int, PartDefinitionSO> definitions,
            MountArtLookup art,
            bool useMountArt,
            in CraftPreviewFit fit,
            float canvasWidth,
            float canvasHeight,
            Action<int> onPartSelected = null)
        {
            if (canvas == null) return 0;
            canvas.Clear();
            if (fit.scale <= 0f) return 0;

            float centerX = canvasWidth * 0.5f;
            float centerY = canvasHeight * 0.5f;
            int painted = 0;

            for (int i = 0; i < placements.Length; i++)
            {
                CraftPartPlacement placement = placements[i];
                if (!TryResolveSprite(
                        placement.partId, definitions, art, useMountArt,
                        out Sprite sprite, out Vector2 contentCenter, out _))
                {
                    continue;
                }

                float width = sprite.rect.width * fit.scale;
                float height = sprite.rect.height * fit.scale;
                if (placement.mirrored) contentCenter.x = -contentCenter.x;

                var element = new VisualElement
                {
                    name = "preview-part-" + placement.partId,
                    pickingMode = onPartSelected != null ? PickingMode.Position : PickingMode.Ignore
                };
                element.style.position = Position.Absolute;
                element.style.width = width;
                element.style.height = height;
                element.style.left = centerX +
                    ((placement.localOffset.x + contentCenter.x - fit.boundsCenterX) * fit.scale) -
                    (width * 0.5f);
                element.style.top = centerY -
                    ((placement.localOffset.y + contentCenter.y - fit.boundsCenterY) * fit.scale) -
                    (height * 0.5f);
                UiSpriteSkin.Apply(element, sprite, ScaleMode.StretchToFill, keepBackground: false);
                element.style.rotate = new StyleRotate(
                    new Rotate(new Angle(placement.localRotation * Mathf.Rad2Deg, AngleUnit.Degree)));
                if (placement.mirrored)
                    element.style.scale = new StyleScale(new Scale(new Vector3(-1f, 1f, 1f)));

                if (onPartSelected != null)
                {
                    int partId = placement.partId;
                    element.RegisterCallback<PointerDownEvent>(
                        _ => element.schedule.Execute(() => onPartSelected(partId)));
                }

                canvas.Add(element);
                painted++;
            }

            return painted;
        }

        /// <summary>
        /// Mount art when requested and available; otherwise the 1x shop thumbnail with its
        /// authored opaque rect. The caller decides once per frame; the sets never mix.
        /// </summary>
        public static bool TryResolveSprite(
            int partId,
            IReadOnlyDictionary<int, PartDefinitionSO> definitions,
            MountArtLookup art,
            bool useMountArt,
            out Sprite sprite,
            out Vector2 contentCenter,
            out Vector2 contentSize)
        {
            contentCenter = Vector2.zero;
            contentSize = Vector2.zero;
            if (useMountArt && art != null &&
                art.TryResolve(partId, out sprite, out contentCenter, out contentSize))
            {
                return true;
            }

            sprite = null;
            if (definitions == null || !definitions.TryGetValue(partId, out PartDefinitionSO definition))
                return false;

            sprite = definition.partSprite;
            if (sprite == null) return false;

            Vector4 content = definition.spriteContentRect;
            if (content.z > 0f && content.w > 0f)
            {
                contentCenter = new Vector2(
                    content.x + (content.z * 0.5f) - (sprite.rect.width * 0.5f),
                    content.y + (content.w * 0.5f) - (sprite.rect.height * 0.5f));
                contentSize = new Vector2(content.z, content.w);
            }
            else
            {
                contentSize = sprite.rect.size;
            }

            return true;
        }
    }
}
