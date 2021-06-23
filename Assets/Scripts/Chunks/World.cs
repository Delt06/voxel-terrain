using System;
using System.Collections.Generic;
using Pooling;
using Unity.Mathematics;
using UnityEngine;

namespace Chunks
{
    public class World : MonoBehaviour
    {
        [SerializeField] private GameObjectPool _pool = default;
        [SerializeField] private Transform _reference = default;
        [SerializeField, Min(1)] private int _renderDistanceInChunks = 4;
        [SerializeField, Min(1)] private int _maxSpawnedChunksPerFrame = 1;
        [SerializeField, Min(1)] private int _updatePeriodInFrames = 4;

        public int3 ChunkSize { get; private set; }

        public event EventHandler<(Chunk chunk, BlockData oldBlock, int3 localPosition)> ChunkBlockChanged;
        public event EventHandler<Chunk> ChunkWasGenerated;
        public event EventHandler<Chunk> ChunkChanging;

        public bool TryGetChunkAt(Vector3 worldPosition, out Chunk chunk)
        {
            var chunkPositionXZ = WorldToChunkCoordinates(worldPosition);
            return TryGetChunkAt(chunkPositionXZ, out chunk);
        }

        public bool TryGetChunkAt(int2 chunkPositionXZ, out Chunk chunk)
        {
            if (_activeChunksAtPosition.TryGetValue(chunkPositionXZ, out var chunkGameObject))
            {
                chunk = GetChunk(chunkGameObject);
                return true;
            }

            chunk = default;
            return false;
        }

        private Chunk GetChunk(GameObject chunkGameObject)
        {
            if (!_chunkComponents.TryGetValue(chunkGameObject, out var chunk))
                _chunkComponents[chunkGameObject] = chunk = chunkGameObject.GetComponent<Chunk>();

            return chunk;
        }

        public int RenderDistanceInChunks
        {
            get => _renderDistanceInChunks;
            set => _renderDistanceInChunks = Mathf.Max(1, value);
        }

        public int MaxSpawnedChunksPerFrame
        {
            get => _maxSpawnedChunksPerFrame;
            set => _maxSpawnedChunksPerFrame = Mathf.Max(1, value);
        }

        private void Update()
        {
            if (Time.frameCount % _updatePeriodInFrames != 0) return;

            UpdateChunks();
        }

        private void UpdateChunks(bool applyLimits = true)
        {
            var referencePositionXZ = WorldToChunkCoordinates(_reference.position);
            SpawnNearChunks(referencePositionXZ, applyLimits);
            DespawnFarChunks(referencePositionXZ);
        }

        private void SpawnNearChunks(int2 referencePositionXZ, bool applyLimits = true)
        {
            var spawnedCount = 0;

            for (var dx = -_renderDistanceInChunks; dx <= _renderDistanceInChunks; dx++)
            {
                for (var dz = -_renderDistanceInChunks; dz <= _renderDistanceInChunks; dz++)
                {
                    var magnitude = math.length(new float2(dx, dz));
                    if (magnitude > _renderDistanceInChunks) continue;

                    var chunkPositionXZ = referencePositionXZ + new int2(dx, dz);
                    if (_activeChunksAtPosition.ContainsKey(chunkPositionXZ)) continue;

                    var worldOrigin = ChunkToWorldCoordinates(chunkPositionXZ);
                    var chunkGameObject = _pool.GetObject(worldOrigin);
                    _activeChunksAtPosition[chunkPositionXZ] = chunkGameObject;
                    _activeChunks.Add((chunkPositionXZ, chunkGameObject));

                    var chunk = GetChunk(chunkGameObject);
                    chunk.PositionXZ = chunkPositionXZ;
                    StartListeningTo(chunk);

                    if (!applyLimits) continue;
                    spawnedCount++;
                    if (spawnedCount >= _maxSpawnedChunksPerFrame)
                        return;
                }
            }
        }

        private void StartListeningTo(Chunk chunk)
        {
            chunk.BlockChanged += _onBlockChanged;
            chunk.WasGenerated += _onWasGenerated;
            chunk.Changing += _onChanging;
        }

        private void DespawnFarChunks(int2 referencePositionXZ)
        {
            for (var activeChunkIndex = _activeChunks.Count - 1; activeChunkIndex >= 0; activeChunkIndex--)
            {
                var (chunkPositionXZ, chunkGameObject) = _activeChunks[activeChunkIndex];
                var distance = math.length(chunkPositionXZ - referencePositionXZ);
                if (distance <= _renderDistanceInChunks) continue;

                var chunk = GetChunk(chunkGameObject);
                if (chunk.IsLocked) continue;

                chunkGameObject.SetActive(false);
                _activeChunksAtPosition.Remove(chunkPositionXZ);
                _activeChunks.RemoveAt(activeChunkIndex);
                StopListeningTo(chunk);
            }
        }

        private void StopListeningTo(Chunk chunk)
        {
            chunk.BlockChanged -= _onBlockChanged;
            chunk.WasGenerated -= _onWasGenerated;
            chunk.Changing -= _onChanging;
        }

        public int2 WorldToChunkCoordinates(float3 worldPosition)
        {
            var x = (int) math.floor(worldPosition.x / _stepX);
            var z = (int) math.floor(worldPosition.z / _stepZ);
            return new int2(x, z);
        }

        public Vector3 ChunkToWorldCoordinates(int2 positionXZ) =>
            new Vector3(positionXZ.x * _stepX, 0, positionXZ.y * _stepZ);

        private void Start()
        {
            var renderSquareSide = Mathf.CeilToInt(_renderDistanceInChunks);
            var renderSquareArea = renderSquareSide * renderSquareSide * 4;
            _pool.IncreaseCapacityTo(renderSquareArea);
            UpdateChunks(false);
        }

        private void OnDestroy()
        {
            foreach (var activeChunk in _activeChunks)
            {
                GetChunk(activeChunk.chunk).BlockChanged -= _onBlockChanged;
            }
        }

        private void Awake()
        {
            _onBlockChanged = (sender, args) =>
            {
                var changeArgs = ((Chunk) sender, args.oldBlock, args.localPosition);
                ChunkBlockChanged?.Invoke(this, changeArgs);
            };
            _onWasGenerated = (sender, args) => ChunkWasGenerated?.Invoke(this, (Chunk) sender);
            _onChanging = (sender, args) => ChunkChanging?.Invoke(this, (Chunk) sender);
            var chunk = _pool.Prefab.GetComponent<Chunk>();
            ChunkSize = new int3(chunk.SizeX, chunk.SizeY, chunk.SizeZ);
            _stepX = ChunkSize.x;
            _stepZ = ChunkSize.z;
            Shader.SetGlobalVector(ChunkSizeId, (Vector3) (float3) ChunkSize);
        }

        private int _stepX;
        private int _stepZ;
        private EventHandler<(BlockData oldBlock, int3 localPosition)> _onBlockChanged;
        private EventHandler _onWasGenerated;
        private EventHandler _onChanging;

        private readonly Dictionary<GameObject, Chunk> _chunkComponents = new Dictionary<GameObject, Chunk>();

        private readonly List<(int2 chunkIndexXZ, GameObject chunk)> _activeChunks =
            new List<(int2 chunkIndexXZ, GameObject chunk)>();

        private readonly Dictionary<int2, GameObject> _activeChunksAtPosition =
            new Dictionary<int2, GameObject>();
        private static readonly int ChunkSizeId = Shader.PropertyToID("_ChunkSize");
    }
}