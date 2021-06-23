using Blocks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Chunks.TerrainGeneration
{
    [RequireComponent(typeof(Chunk))]
    public sealed class ChunkTerrainGenerator : MonoBehaviour
    {
        [SerializeField, Min(0f)]
        private float _scale = 0.01f;

        [SerializeField, Min(1f)]
        private float _minTerrainHeight = 5f;

        [SerializeField, Min(1f)]
        private float _maxTerrainHeight = 10f;

        [SerializeField]
        private BlockConfig _grass = default;

        [SerializeField]
        private BlockConfig _dirt = default;

        [SerializeField, Min(0f)]
        private float _stoneScale = 0.005f;

        [SerializeField, Min(0f)]
        private float _minStoneHeight = 5f;

        [SerializeField, Min(0f)]
        private float _maxStoneHeight = 5f;

        [SerializeField, Range(0f, 1f)]
        private float _relativeWaterLevel = 0.5f;

        [SerializeField]
        private BlockConfig _stone = default;

        [SerializeField]
        private BlockConfig _water = default;

        [SerializeField, Min(1)] private int _batchCount = 4;

        private void Update()
        {
            if (_jobHandle == null)
            {
                if (!_chunk.TryGetValidBlocks(out _))
                    ScheduleJob();

                return;
            }

            if (!_jobHandle.Value.IsCompleted) return;

            _jobHandle.Value.Complete();
            _jobHandle = null;

            if (_jobIsDirty)
            {
                ScheduleJob();
                return;
            }

            _chunk.BlocksBuffer.CopyFrom(_blocks);
            _chunk.OnWasGenerated();
            _chunk.ReleaseLock(this);
        }

        private void ScheduleJob()
        {
            var chunkSize = new int3(_chunk.SizeX, _chunk.SizeY, _chunk.SizeZ);
            var job = new TerrainGenerationJob
            {
                BatchCount = _batchCount,
                ChunkSize = chunkSize,

                Scale = _scale,
                MinTerrainHeight = _minTerrainHeight,
                MaxTerrainHeight = _maxTerrainHeight,

                StoneScale = _stoneScale,
                MinStoneHeight = _minStoneHeight,
                MaxStoneHeight = _maxStoneHeight,
                RelativeWaterLevel = _relativeWaterLevel,

                Grass = _grass,
                Dirt = _dirt,
                Stone = _stone,
                WaterSource = _water,

                Origin = Origin,

                Blocks = _blocks,
            };
            _jobHandle = job.Schedule(_batchCount, 1);
            _jobIsDirty = false;
            _chunk.RequestLock(this);
        }

        private Vector3 Origin => transform.position;

        private void OnEnable()
        {
            if (_jobHandle != null) _jobIsDirty = true;
        }

        private void Awake()
        {
            _chunk = GetComponent<Chunk>();
            _blocks = new NativeArray<BlockData>(ChunkVolume, Allocator.Persistent);
        }

        private int ChunkVolume => _chunk.SizeX * _chunk.SizeY * _chunk.SizeZ;

        private void OnDestroy()
        {
            if (_blocks.IsCreated)
            {
                _jobHandle?.Complete();
                _blocks.Dispose();
            }
        }

        private JobHandle? _jobHandle;
        private bool _jobIsDirty;
        private NativeArray<BlockData> _blocks;
        private Chunk _chunk;
    }
}