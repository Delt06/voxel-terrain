using System;
using Blocks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Chunks.MeshGeneration
{
    [RequireComponent(typeof(MeshFilter)), RequireComponent(typeof(MeshRenderer)),
     RequireComponent(typeof(MeshCollider))]
    public sealed class ChunkMeshGenerator : MonoBehaviour
    {
        [SerializeField] private Chunk _chunk = default;
        [SerializeField] private string _meshName = "Terrain";
        [SerializeField, Min(0)] private byte _meshIndex = 0;

        public void Construct(IBlockDataProvider service)
        {
            _blockDataProvider = service;
        }

        private void LateUpdate()
        {
            if (!_isDirty) return;

            RebuildMesh();
            _isDirty = false;
        }

        private void Update()
        {
            if (!CheckIfCompleted()) return;

            if (!_jobIsDirty)
            {
                var vertexCount = GetSum(_vertexCounts);
                _mesh.Clear();
                _mesh.subMeshCount = _subMeshesCount;
                _mesh.SetVertices(_vertexBuffer, 0, vertexCount);
                _mesh.SetUVs(0, _uvBuffer, 0, vertexCount);
                _mesh.SetNormals(_normalBuffer, 0, vertexCount);

                for (var subMeshIndex = 0; subMeshIndex < _subMeshesCount; subMeshIndex++)
                {
                    var count = _triangleCounts[subMeshIndex];
                    if (count == 0) continue;

                    var start = subMeshIndex * TrianglesPerSubMesh;
                    _mesh.SetIndices(_triangleBuffer, start, count, MeshTopology.Triangles, subMeshIndex);
                }

                _mesh.RecalculateBounds();

                if (!_meshRenderer.enabled)
                    _meshRenderer.enabled = true;

                MeshChanged?.Invoke(this, EventArgs.Empty);
            }

            _jobHandle = null;
            DisposeBuffers();
        }

        private bool CheckIfCompleted()
        {
            if (_jobHandle is { IsCompleted: true })
            {
                EnsureJobIsCompleted();
                return true;
            }

            return false;
        }

        private void EnsureJobIsCompleted()
        {
            if (_jobHandle.HasValue)
            {
                _jobHandle.Value.Complete();
                _chunk.ReleaseLock(this);
                _jobHandle = default;
            }
        }

        public event EventHandler MeshChanged;

        private int GetSum(NativeArray<int> array)
        {
            var sum = 0;

            for (var index = 0; index < array.Length; index++)
            {
                sum += array[index];
            }

            return sum;
        }

        private void RebuildMesh()
        {
            if (_jobHandle != null) return;
            if (!_chunk.TryGetValidBlocks(out var blocks)) return;

            _jobIsDirty = false;

            var chunkSize = new int3(_chunk.SizeX, _chunk.SizeY, _chunk.SizeZ);
            CreateBuffers(Allocator.Persistent);
            _blocks.CopyFrom(blocks);
            var generationJob = new MeshGenerationJob
            {
                MeshIndex = _meshIndex,
                Blocks = _blocks,
                ChunkSize = chunkSize,
                UVs = _blockDataProvider.UVs,
                Checked = _checked,

                VertexBuffer = _vertexBuffer,
                TriangleBuffer = _triangleBuffer,
                UvBuffer = _uvBuffer,
                NormalBuffer = _normalBuffer,

                VertexCounts = _vertexCounts,
                TriangleCounts = _triangleCounts,

                SubMeshTriangleCount = TrianglesPerSubMesh,
            };

            _jobHandle = generationJob.Schedule();
            _chunk.RequestLock(this);
        }

        private void OnEnable()
        {
            if (_jobHandle != null)
                _jobIsDirty = true;

            _meshRenderer.enabled = false;
            _isDirty = true;
            _chunk.Changed += _onChanged;
            _chunk.Changing += _onChanging;
        }

        private void OnDisable()
        {
            _mesh.Clear();
            _meshRenderer.enabled = false;
            _chunk.Changed -= _onChanged;
            _chunk.Changing -= _onChanging;
        }

        private void Awake()
        {
            var meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _subMeshesCount = _meshRenderer.materials.Length;
            _mesh = new Mesh { name = _meshName, subMeshCount = _subMeshesCount };
            meshFilter.sharedMesh = _mesh;
            GetComponent<MeshCollider>().sharedMesh = _mesh;

            _onChanging = (sender, args) => EnsureJobIsCompleted();
            _onChanged = (sender, args) => _isDirty = true;
        }

        private void CreateBuffers(Allocator allocator)
        {
            _blocks.EnsureCreated(ChunkVolume, allocator, NativeArrayOptions.UninitializedMemory);
            _checked.EnsureCreated(ChunkVolume, allocator, NativeArrayOptions.UninitializedMemory);
            _vertexCounts.EnsureCreated(1, allocator, NativeArrayOptions.UninitializedMemory);
            _triangleCounts.EnsureCreated(_subMeshesCount, allocator, NativeArrayOptions.UninitializedMemory);
            var blocksCount = ChunkVolume;
            var vertices = VerticesPerBlock * blocksCount;
            var triangles = _subMeshesCount * TrianglesPerBlock * blocksCount;
            _vertexBuffer.EnsureCreated(vertices, allocator, NativeArrayOptions.UninitializedMemory);
            _triangleBuffer.EnsureCreated(triangles, allocator, NativeArrayOptions.UninitializedMemory);
            _uvBuffer.EnsureCreated(vertices, allocator, NativeArrayOptions.UninitializedMemory);
            _normalBuffer.EnsureCreated(vertices, allocator, NativeArrayOptions.UninitializedMemory);
        }

        private int TrianglesPerSubMesh => ChunkVolume * TrianglesPerBlock;

        private int ChunkVolume => _chunk.SizeX * _chunk.SizeY * _chunk.SizeZ;

        private const int VerticesPerBlock = SidesPerBlock * VerticesPerSide;
        private const int TrianglesPerBlock = SidesPerBlock * TrianglesPerSide;
        private const int SidesPerBlock = 6;
        private const int VerticesPerSide = 4;
        private const int TrianglesPerSide = 2;

        private void DisposeBuffers()
        {
            _blocks.DisposeIfCreated();
            _checked.DisposeIfCreated();
            _vertexCounts.DisposeIfCreated();
            _triangleCounts.DisposeIfCreated();
            _vertexBuffer.DisposeIfCreated();
            _triangleBuffer.DisposeIfCreated();
            _uvBuffer.DisposeIfCreated();
            _normalBuffer.DisposeIfCreated();
        }

        private int _subMeshesCount;
        private bool _jobIsDirty;
        private MeshRenderer _meshRenderer;
        private Mesh _mesh;
        private bool _isDirty;
        private EventHandler _onChanged;
        private EventHandler _onChanging;
        private IBlockDataProvider _blockDataProvider;

        private JobHandle? _jobHandle;
        private NativeArray<BlockData> _blocks;
        private NativeArray<bool> _checked;

        private NativeArray<int> _vertexCounts;
        private NativeArray<int> _triangleCounts;

        private NativeArray<float3> _vertexBuffer;
        private NativeArray<float2> _uvBuffer;
        private NativeArray<int> _triangleBuffer;
        private NativeArray<float3> _normalBuffer;
    }
}