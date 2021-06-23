using System;
using System.Collections.Generic;
using Chunks;
using Chunks.Lighting;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Simulation.Liquids
{
    public class WorldLiquidSimulation : MonoBehaviour, ITickSystem, IWorldChangingSystem
    {
        [SerializeField] private World _world = default;

        void ITickSystem.OnTick()
        {
            if (_modifiedPositions.Count == 0)
            {
                if (CheckIfJobIsCompleted())
                {
                    ProcessResultsIfAny();
                    ReleaseLocksAndClear();
                }

                return;
            }

            EnsureJobIsCompleted();
            ProcessResultsIfAny();
            ReleaseLocksAndClear();
            TryScheduleJob();
        }

        void IWorldChangingSystem.OnChanging() => EnsureJobIsCompleted();

        private void EnsureJobIsCompleted()
        {
            if (_activeJobHandle != null)
            {
                _activeJobHandle.Value.Complete();
                _activeJobHandle = null;
            }
        }

        private bool CheckIfJobIsCompleted()
        {
            if (_activeJobHandle is { IsCompleted: true })
            {
                EnsureJobIsCompleted();
                return true;
            }

            return false;
        }

        private void ProcessResultsIfAny()
        {
            if (_resultingBlockChanges.IsEmpty) return;

            foreach (var (chunkAndBlockPosition, newBlock) in _resultingBlockChanges)
            {
                var chunkXZ = chunkAndBlockPosition.ChunkXZ;
                if (!_world.TryGetChunkAt(chunkXZ, out var chunk)) continue;

                var blockIndex = chunkAndBlockPosition.BlockIndex;
                var localPosition = ChunkUtils.IndexToPosition(blockIndex, _world.ChunkSize);
                var currentBlock = chunk.GetBlockAt(localPosition);
                if (currentBlock.Equals(newBlock) && currentBlock.Metadata == newBlock.Metadata) continue;

                chunk.SetBlockAt(localPosition, newBlock);
            }

            _resultingBlockChanges.Clear();
        }

        private void ReleaseLocksAndClear()
        {
            foreach (var position in _currentlyProcessedPositions)
            {
                if (_world.TryGetChunkAt(position.ChunkXZ, out var chunk))
                    chunk.ReleaseLock(LockSource);
            }

            _currentlyProcessedPositions.Clear();
        }

        private void TryScheduleJob()
        {
            if (_modifiedPositions.Count == 0) return;

            var defaultBlockBuffer = new NativeArray<BlockData>(0, Allocator.Persistent);
            var jobsByChunk = new Dictionary<int2, WorldLiquidSimulationJob>();

            const int maxAffectedBlocks = 6;
            _resultingBlockChanges.Capacity = math.max(_resultingBlockChanges.Capacity,
                _modifiedPositions.Count * maxAffectedBlocks
            );

            foreach (var modifiedPosition in _modifiedPositions)
            {
                var chunkXZ = modifiedPosition.ChunkXZ;

                if (!jobsByChunk.TryGetValue(chunkXZ, out var job))
                {
                    var blocksNeighborhood =
                        BlockNeighborhoodUtils.Create(_world, chunkXZ, defaultBlockBuffer);

                    var modifiedBlockIndices = new NativeList<ChunkAndBlockPosition>(Allocator.Persistent);

                    job = new WorldLiquidSimulationJob
                    {
                        ResultingBlockChanges = _resultingBlockChanges.AsParallelWriter(),
                        BlocksNeighborhood = blocksNeighborhood,
                        ChunkSize = _world.ChunkSize,
                        CenterChunkXZ = chunkXZ,
                        ModifiedBlocks = modifiedBlockIndices,
                    };
                    jobsByChunk[chunkXZ] = job;
                }

                job.ModifiedBlocks.Add(modifiedPosition);
                _currentlyProcessedPositions.Add(modifiedPosition);
            }

            _modifiedPositions.Clear();

            var allJobsHandle = new JobHandle();

            foreach (var job in jobsByChunk.Values)
            {
                var thisJobHandle = job.Schedule().ThenDispose(job.ModifiedBlocks);
                allJobsHandle = JobHandle.CombineDependencies(thisJobHandle, allJobsHandle);
            }

            allJobsHandle = defaultBlockBuffer.Dispose(allJobsHandle);
            _activeJobHandle = allJobsHandle;
        }

        private object LockSource => this;

        private void OnEnable()
        {
            _world.ChunkBlockChanged += _onChunkBlockChanged;
            _world.ChunkWasGenerated += _onChunkWasGenerated;
        }

        private void OnDisable()
        {
            _world.ChunkBlockChanged -= _onChunkBlockChanged;
            _world.ChunkWasGenerated -= _onChunkWasGenerated;
        }

        private void Awake()
        {
            _resultingBlockChanges.EnsureCreated(100, Allocator.Persistent);

            _onChunkBlockChanged = OnChunkBlockChanged;
            _onChunkWasGenerated = (sender, chunk) =>
            {
                var chunkSize = _world.ChunkSize;
                var chunkVolume = chunkSize.x * chunkSize.y * chunkSize.z;

                for (var index = 0; index < chunkVolume; index++)
                {
                    if (!chunk.TryGetValidBlocks(out var blocks)) continue;

                    var block = blocks[index];
                    if (!block.IsLiquid()) continue;

                    var localPosition = ChunkUtils.IndexToPosition(index, chunkSize);
                    var args = (chunk, BlockData.Empty, localPosition);
                    OnChunkBlockChanged(sender, args);
                }
            };
        }

        private void OnDestroy()
        {
            EnsureJobIsCompleted();
            _resultingBlockChanges.DisposeIfCreated();
        }

        private void OnChunkBlockChanged(object sender, (Chunk chunk, BlockData oldBlock, int3 localPosition) args)
        {
            var (chunk, _, localPosition) = args;
            var blockIndex = ChunkUtils.PositionToIndex(localPosition, _world.ChunkSize);
            var chunkAndBlockPosition = new ChunkAndBlockPosition(chunk.PositionXZ, blockIndex);
            _modifiedPositions.Add(chunkAndBlockPosition);
        }

        private JobHandle? _activeJobHandle;
        private EventHandler<(Chunk chunk, BlockData oldBlock, int3 localPosition)> _onChunkBlockChanged;
        private EventHandler<Chunk> _onChunkWasGenerated;
        private NativeHashMap<ChunkAndBlockPosition, BlockData> _resultingBlockChanges;

        private readonly HashSet<ChunkAndBlockPosition> _modifiedPositions = new HashSet<ChunkAndBlockPosition>();
        private readonly HashSet<ChunkAndBlockPosition> _currentlyProcessedPositions =
            new HashSet<ChunkAndBlockPosition>();
    }
}