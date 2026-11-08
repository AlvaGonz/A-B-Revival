using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace ABRevival.Core.Rendering
{
    /// <summary>
    /// Managed state shared with the BRG culling callback. Allocated once when the
    /// renderer is configured; the callback only reads it.
    /// </summary>
    public sealed class SpriteBatchCullingState
    {
        public BatchID[] BatchIds = Array.Empty<BatchID>();
        public BatchMaterialID MaterialId;
        public BatchMeshID MeshId;
        public int InstanceCount;
    }

    /// <summary>
    /// Emits bounded BRG draw commands. This lives in the Core assembly only
    /// because ABRevival.Presentation.asmdef disallows unsafe code; the renderer
    /// itself (GameObject, buffers, shader, batches) lives in Presentation.
    /// All scratch memory uses Allocator.TempJob, so culling performs no managed
    /// heap allocations.
    /// </summary>
    public static class SpriteBatchCulling
    {
        public static unsafe JobHandle Perform(
            BatchRendererGroup rendererGroup,
            BatchCullingContext cullingContext,
            BatchCullingOutput cullingOutput,
            IntPtr userContext)
        {
            if (cullingOutput.drawCommands.Length == 0)
                return default;

            var drawCommands = cullingOutput.drawCommands[0];
            drawCommands.drawCommands = null;
            drawCommands.drawRanges = null;
            drawCommands.visibleInstances = null;
            drawCommands.instanceSortingPositions = null;
            drawCommands.drawCommandPickingEntityIds = null;
            drawCommands.drawCommandCount = 0;
            drawCommands.drawRangeCount = 0;
            drawCommands.visibleInstanceCount = 0;
            drawCommands.instanceSortingPositionFloatCount = 0;

            if (userContext == IntPtr.Zero)
            {
                cullingOutput.drawCommands[0] = drawCommands;
                return default;
            }

            var state = GCHandle.FromIntPtr(userContext).Target as SpriteBatchCullingState;
            if (state == null || state.InstanceCount <= 0 || state.BatchIds.Length == 0)
            {
                cullingOutput.drawCommands[0] = drawCommands;
                return default;
            }

            int batchCount = SpriteBatchPlan.BatchCount(state.InstanceCount);
            if (batchCount > state.BatchIds.Length)
                batchCount = state.BatchIds.Length;
            if (batchCount == 0)
            {
                cullingOutput.drawCommands[0] = drawCommands;
                return default;
            }

            int alignment = UnsafeUtility.AlignOf<long>();
            var commands = (BatchDrawCommand*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<BatchDrawCommand>() * batchCount, alignment, Allocator.TempJob);
            var ranges = (BatchDrawRange*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<BatchDrawRange>() * batchCount, alignment, Allocator.TempJob);
            var visibleInstances = (int*)UnsafeUtility.Malloc(
                sizeof(int) * state.InstanceCount, alignment, Allocator.TempJob);

            var filterSettings = new BatchFilterSettings
            {
                renderingLayerMask = 0xFFFFFFFFu,
                batchLayer = 0,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
                motionMode = MotionVectorGenerationMode.ForceNoMotion,
                staticShadowCaster = false,
                allDepthSorted = false,
                sceneCullingMask = ulong.MaxValue
            };

            int visibleWrite = 0;
            for (int batch = 0; batch < batchCount; batch++)
            {
                int batchInstances = SpriteBatchPlan.BatchInstanceCount(state.InstanceCount, batch);

                commands[batch] = new BatchDrawCommand
                {
                    flags = BatchDrawCommandFlags.None,
                    batchID = state.BatchIds[batch],
                    materialID = state.MaterialId,
                    meshID = state.MeshId,
                    submeshIndex = 0,
                    splitVisibilityMask = 0xFF,
                    lightmapIndex = 0,
                    sortingPosition = 0,
                    visibleOffset = (uint)visibleWrite,
                    visibleCount = (uint)batchInstances,
                    activeMeshLod = 0
                };

                // The batch metadata is offset to this batch's slice, so instance
                // indices are local to the batch.
                for (int i = 0; i < batchInstances; i++)
                    visibleInstances[visibleWrite + i] = i;
                visibleWrite += batchInstances;

                ranges[batch] = new BatchDrawRange
                {
                    drawCommandsType = BatchDrawCommandType.Direct,
                    drawCommandsBegin = (uint)batch,
                    drawCommandsCount = 1,
                    filterSettings = filterSettings
                };
            }

            drawCommands.drawCommands = commands;
            drawCommands.drawRanges = ranges;
            drawCommands.visibleInstances = visibleInstances;
            drawCommands.drawCommandCount = batchCount;
            drawCommands.drawRangeCount = batchCount;
            drawCommands.visibleInstanceCount = state.InstanceCount;
            cullingOutput.drawCommands[0] = drawCommands;

            return default;
        }
    }
}
