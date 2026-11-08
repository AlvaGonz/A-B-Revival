using System;
using System.Runtime.InteropServices;
using ABRevival.Core.Rendering;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace ABRevival.Presentation
{
    /// <summary>
    /// Renders modular ship sprites through one BatchRendererGroup and a single
    /// unified-atlas material (ADR 0014). Instances are plain structs written into
    /// preallocated native buffers; there are no per-instance GameObjects. Batches
    /// are bounded to <see cref="SpriteBatchPlan.MaxInstancesPerBatch"/> instances,
    /// and per-part damage tints travel as unity_RendererUserValue.
    ///
    /// The unified atlas texture and per-part UV rectangles are supplied by ticket
    /// #124. Until then a white texture and full-rect UVs render placeholder quads.
    /// Original game IP: Vargames (2011).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BatchSpriteRenderer : MonoBehaviour
    {
        private const string DefaultShaderName = "ABRevival/InstancedSprite";

        private static readonly int ObjectToWorldId = Shader.PropertyToID("unity_ObjectToWorld");
        private static readonly int RendererUserValueId = Shader.PropertyToID("unity_RendererUserValuesPropertyEntry");
        private static readonly int UvRectId = Shader.PropertyToID("_UVRect");
        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

        [SerializeField] private int _capacity;
        [SerializeField] private Texture2D _atlas;
        [SerializeField] private Shader _spriteShader;

        private BatchRendererGroup _brg;
        private GraphicsBuffer _instanceBuffer;
        private NativeArray<PackedTransform> _transforms;
        private NativeArray<uint> _userValues;
        private NativeArray<Vector4> _uvRects;
        private BatchID[] _batchIds;
        private SpriteBatchCullingState _cullingState;
        private GCHandle _cullingHandle;
        private Mesh _mesh;
        private Material _material;
        private int _objectToWorldOffset;
        private int _userValueOffset;
        private int _uvRectOffset;
        private int _maxInstances;
        private int _instanceCount;
        private int _dirtyStart = int.MaxValue;
        private int _dirtyEnd;
        private bool _disposed = true;

        public int Capacity => _maxInstances;
        public int InstanceCount => _instanceCount;
        public bool IsInitialized => !_disposed && _brg != null;
        public int BatchCount => SpriteBatchPlan.BatchCount(_instanceCount);
        public int DrawCommandCount => BatchCount;

        private void OnEnable()
        {
            if (_capacity > 0 && !IsInitialized)
                Configure(_capacity, _atlas);
        }

        private void OnDisable()
        {
            DisposeRenderer();
        }

        private void OnDestroy()
        {
            DisposeRenderer();
        }

        /// <summary>
        /// Allocates the BRG, GPU buffer, and native staging arrays. Call once before
        /// adding instances. Allocations happen here, not in the per-frame path.
        /// </summary>
        public void Configure(int capacity, Texture2D atlas = null)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity));

            DisposeRenderer();

            if (atlas != null)
                _atlas = atlas;

            _capacity = capacity;
            _maxInstances = capacity;
            _objectToWorldOffset = SpriteBatchPlan.ObjectToWorldOffset(capacity);
            _userValueOffset = SpriteBatchPlan.UserValueOffset(capacity);
            _uvRectOffset = SpriteBatchPlan.UvRectOffset(capacity);
            int bufferBytes = SpriteBatchPlan.BufferBytes(capacity);
            _instanceCount = 0;
            _dirtyStart = int.MaxValue;
            _dirtyEnd = 0;
            _disposed = false;

            try
            {
                _transforms = new NativeArray<PackedTransform>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                _userValues = new NativeArray<uint>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                _uvRects = new NativeArray<Vector4>(capacity, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
                // Raw buffers are DWORD-stride; the byte layout is defined by the
                // metadata offsets and the per-property native arrays.
                _instanceBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, bufferBytes / sizeof(int), sizeof(int));

                Shader shader = _spriteShader != null ? _spriteShader : Shader.Find(DefaultShaderName);
                if (shader == null)
                    throw new InvalidOperationException($"Shader '{DefaultShaderName}' was not found. The sprite shader asset must be imported.");

                _mesh = CreateQuadMesh();
                _material = new Material(shader) { name = "ABRevival/BrgSpriteMaterial" };
                _material.SetTexture(MainTexId, _atlas != null ? _atlas : Texture2D.whiteTexture);

                _batchIds = new BatchID[SpriteBatchPlan.BatchCount(capacity)];
                _cullingState = new SpriteBatchCullingState { BatchIds = _batchIds, InstanceCount = 0 };
                _cullingHandle = GCHandle.Alloc(_cullingState);
                _brg = new BatchRendererGroup(SpriteBatchCulling.Perform, GCHandle.ToIntPtr(_cullingHandle));
                _cullingState.MeshId = _brg.RegisterMesh(_mesh);
                _cullingState.MaterialId = _brg.RegisterMaterial(_material);

                for (int i = 0; i < _batchIds.Length; i++)
                    _batchIds[i] = AddBatch(i);

                _brg.SetGlobalBounds(new Bounds(Vector3.zero, new Vector3(1e6f, 1e6f, 1e6f)));
            }
            catch
            {
                DisposeRenderer();
                throw;
            }
        }

        /// <summary>Adds a sprite instance and returns its buffer index.</summary>
        public int AddInstance(in SpriteInstance instance)
        {
            ThrowIfNotConfigured();
            if (_instanceCount >= _maxInstances)
                throw new InvalidOperationException($"Sprite capacity {_maxInstances} exceeded.");

            int index = _instanceCount++;
            WriteInstance(index, in instance);
            return index;
        }

        /// <summary>Updates an existing sprite instance.</summary>
        public void SetInstance(int index, in SpriteInstance instance)
        {
            ThrowIfNotConfigured();
            if ((uint)index >= (uint)_instanceCount)
                throw new ArgumentOutOfRangeException(nameof(index));

            WriteInstance(index, in instance);
        }

        public void ClearInstances()
        {
            _instanceCount = 0;
            _dirtyStart = int.MaxValue;
            _dirtyEnd = 0;
            if (_cullingState != null)
                _cullingState.InstanceCount = 0;
        }

        /// <summary>Per-instance tint last written at <paramref name="index"/> (test/telemetry read-back).</summary>
        public uint GetUserValue(int index)
        {
            ThrowIfNotConfigured();
            if ((uint)index >= (uint)_instanceCount)
                throw new ArgumentOutOfRangeException(nameof(index));
            return _userValues[index];
        }

        /// <summary>
        /// Uploads only the dirty instance range to the GPU. Call after adding or
        /// updating instances; this path performs no managed heap allocations.
        /// </summary>
        public void Submit()
        {
            if (_disposed)
                return;

            _cullingState.InstanceCount = _instanceCount;
            if (_dirtyStart >= _dirtyEnd)
                return;

            int count = _dirtyEnd - _dirtyStart;
            _instanceBuffer.SetData(
                _transforms,
                _dirtyStart,
                _objectToWorldOffset / SpriteBatchPlan.PackedTransformBytes + _dirtyStart,
                count);
            _instanceBuffer.SetData(
                _userValues,
                _dirtyStart,
                _userValueOffset / SpriteBatchPlan.UserValueBytes + _dirtyStart,
                count);
            _instanceBuffer.SetData(
                _uvRects,
                _dirtyStart,
                _uvRectOffset / SpriteBatchPlan.UvRectBytes + _dirtyStart,
                count);

            _dirtyStart = int.MaxValue;
            _dirtyEnd = 0;
        }

        private BatchID AddBatch(int batchIndex)
        {
            var metadata = new NativeArray<MetadataValue>(3, Allocator.Temp);
            try
            {
                int instanceStart = SpriteBatchPlan.BatchStart(batchIndex);
                metadata[0] = new MetadataValue
                {
                    NameID = ObjectToWorldId,
                    Value = SpriteBatchPlan.MetadataValue(
                        _objectToWorldOffset + instanceStart * SpriteBatchPlan.PackedTransformBytes)
                };
                metadata[1] = new MetadataValue
                {
                    NameID = RendererUserValueId,
                    Value = SpriteBatchPlan.MetadataValue(
                        _userValueOffset + instanceStart * SpriteBatchPlan.UserValueBytes)
                };
                metadata[2] = new MetadataValue
                {
                    NameID = UvRectId,
                    Value = SpriteBatchPlan.MetadataValue(
                        _uvRectOffset + instanceStart * SpriteBatchPlan.UvRectBytes)
                };

                return _brg.AddBatch(metadata, _instanceBuffer.bufferHandle);
            }
            finally
            {
                metadata.Dispose();
            }
        }

        private void WriteInstance(int index, in SpriteInstance instance)
        {
            _transforms[index] = PackedTransform.FromTrs(
                instance.X,
                instance.Y,
                instance.Width,
                instance.Height,
                instance.RotationRadians);
            _userValues[index] = instance.UserValue;
            _uvRects[index] = new Vector4(instance.UvX, instance.UvY, instance.UvWidth, instance.UvHeight);

            if (index < _dirtyStart)
                _dirtyStart = index;
            if (index + 1 > _dirtyEnd)
                _dirtyEnd = index + 1;
        }

        private void ThrowIfNotConfigured()
        {
            if (_disposed)
                throw new InvalidOperationException("Configure must be called before using the batch sprite renderer.");
        }

        private void DisposeRenderer()
        {
            if (_disposed)
                return;
            _disposed = true;

            if (_brg != null)
            {
                _brg.Dispose();
                _brg = null;
            }

            if (_cullingHandle.IsAllocated)
                _cullingHandle.Free();

            if (_instanceBuffer != null)
            {
                _instanceBuffer.Dispose();
                _instanceBuffer = null;
            }

            if (_transforms.IsCreated)
                _transforms.Dispose();
            if (_userValues.IsCreated)
                _userValues.Dispose();
            if (_uvRects.IsCreated)
                _uvRects.Dispose();

            _batchIds = null;
            _cullingState = null;
            _maxInstances = 0;
            _instanceCount = 0;
            _dirtyStart = int.MaxValue;
            _dirtyEnd = 0;

            if (_material != null)
            {
                DestroyAsset(_material);
                _material = null;
            }

            if (_mesh != null)
            {
                DestroyAsset(_mesh);
                _mesh = null;
            }
        }

        private static void DestroyAsset(UnityEngine.Object asset)
        {
            if (asset == null)
                return;

            if (Application.isPlaying)
                Destroy(asset);
            else
                DestroyImmediate(asset);
        }

        private static Mesh CreateQuadMesh()
        {
            var mesh = new Mesh { name = "ABRevival/InstancedSpriteQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f)
            };
            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
