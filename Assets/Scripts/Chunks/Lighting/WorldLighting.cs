using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Chunks.Lighting.FloodFill;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Chunks.Lighting
{
    public class WorldLighting : MonoBehaviour
    {
        [SerializeField] private World _world = default;
        [SerializeField, Range(0f, 1f)] private float _minAmbientLighting = 0f;
        [SerializeField] private bool _sunlight = true;
        [SerializeField] private bool _torchlight = true;

        private static readonly int MinAmbientLightingId = Shader.PropertyToID("_MinAmbientLighting");
        private const string VoxelGiKeyword = "VOXEL_GI";

        private readonly Queue<Chunk> _chunkGenerationQueue = new Queue<Chunk>();
        private NativeHashSet<int2> _modifiedChunkPositions;
        private JobHandle? _activeJobHandle;
        private (Neighborhood<BlockData> neighborhood, int2 centerXZ) _lastNeighborhoodData;

        private EventHandler<(Chunk chunk, BlockData oldBlock, int3 localPosition)> _onBlockChanged;
        private EventHandler<Chunk> _onChanging;
        private EventHandler<Chunk> _onWasGenerated;

        private static readonly int ChunkSizeId = Shader.PropertyToID("_ChunkSize");

        private void ScheduleJob(in FloodFillLightingArgs args, JobHandle jobHandle)
        {
            _activeJobHandle = jobHandle;
            RequestLocks(args);
        }

        private void ScheduleJob(in LightingRemovalArgs args, JobHandle jobHandle)
        {
            _activeJobHandle = jobHandle;
            RequestLocks(args);
        }

        private void RequestLocks(in FloodFillLightingArgs args)
        {
            RequestLocks(args.Blocks, args.CenterChunkXZ);
        }

        private void RequestLocks(in LightingRemovalArgs args)
        {
            RequestLocks(args.Blocks, args.CenterChunkXZ);
        }

        private void RequestLocks(in Neighborhood<BlockData> neighborhood, int2 centerXZ)
        {
            _lastNeighborhoodData = (neighborhood, centerXZ);
            _world.RequestLocksInNeighborhood(neighborhood, centerXZ, this);
        }

        private void EnsureJobIsCompleted()
        {
            if (_activeJobHandle.HasValue)
            {
                _activeJobHandle.Value.Complete();
                _world.ReleaseLocksInNeighborhood(_lastNeighborhoodData.neighborhood, _lastNeighborhoodData.centerXZ,
                    this
                );
                _activeJobHandle = default;
            }
        }

        private void CheckIfCompleted()
        {
            if (_activeJobHandle is { IsCompleted: true }) EnsureJobIsCompleted();
        }

        private void OnChunkGenerated(Chunk chunk)
        {
            if (!_sunlight) return;
            EnsureJobIsCompleted();
            var defaultLightmap = new NativeArray<byte>(0, Allocator.Persistent);
            var defaultBlockBuffer = new NativeArray<BlockData>(0, Allocator.Persistent);
            var lightBfsQueue = new NativeQueue<FloodFillNode>(Allocator.Persistent);
            var args = CreateFloodFillLightingArgs(chunk, defaultLightmap, defaultBlockBuffer, lightBfsQueue);
            ScheduleJob(args, FloodFillLighting.OnChunkGenerated.UpdateLightingFromChunkTop(args)
                .ThenDispose(defaultLightmap)
                .ThenDispose(defaultBlockBuffer)
                .ThenDispose(lightBfsQueue)
            );
        }

        private FloodFillLightingArgs CreateFloodFillLightingArgs(Chunk chunk, NativeArray<byte> defaultLightmap,
            NativeArray<BlockData> defaultBlockBuffer,
            NativeQueue<FloodFillNode> lightBfsQueue) =>
            new FloodFillLightingArgs
            {
                Lightmaps = LightmapNeighborhoodUtils.Create(_world, chunk.PositionXZ, defaultLightmap),
                Blocks = BlockNeighborhoodUtils.Create(_world, chunk.PositionXZ, defaultBlockBuffer),
                BfsQueue = lightBfsQueue,
                ModifiedChunkPositions = _modifiedChunkPositions,
                CenterChunkXZ = chunk.PositionXZ,
                ChunkSize = _world.ChunkSize,
            };

        private void OnRemovedLightBlocker(Chunk chunk, int blockIndex)
        {
            if (!_sunlight && !_torchlight) return;
            EnsureJobIsCompleted();
            var defaultLightmap = new NativeArray<byte>(0, Allocator.TempJob);
            var defaultBlockBuffer = new NativeArray<BlockData>(0, Allocator.TempJob);
            var sunlightBfsQueue = new NativeQueue<FloodFillNode>(Allocator.TempJob);
            var torchlightBfsQueue = new NativeQueue<FloodFillNode>(Allocator.TempJob);
            var args = CreateFloodFillLightingArgs(chunk, defaultLightmap, defaultBlockBuffer, sunlightBfsQueue);

            var chunkPositionXZ = chunk.PositionXZ;
            AddNeighborsToModified(chunk);

            var jobHandle = new JobHandle();

            if (_sunlight)
                jobHandle = FloodFillLighting.OnChunkGenerated.UpdateLightingFromNeighborBlocks(args, chunkPositionXZ,
                    blockIndex
                );

            if (_torchlight)
            {
                args.BfsQueue = torchlightBfsQueue;
                jobHandle = FloodFillLighting.OnTorchPlaced.UpdateLightingFromNeighborBlocks(args, chunkPositionXZ,
                    blockIndex, jobHandle
                );
            }

            ScheduleJob(args, jobHandle
                .ThenDispose(defaultLightmap)
                .ThenDispose(defaultBlockBuffer)
                .ThenDispose(sunlightBfsQueue)
                .ThenDispose(torchlightBfsQueue)
            );
        }

        private void OnPlacedBlockInSunlight(Chunk chunk, int blockIndex, int sunlightValue)
        {
            if (!_sunlight) return;
            EnsureJobIsCompleted();

            var lightBfsQueue = new NativeQueue<FloodFillNode>(Allocator.TempJob);
            var lightRemovalBfsQueue = new NativeQueue<LightingRemovalNode>(Allocator.TempJob);
            var defaultLightmap = new NativeArray<byte>(0, Allocator.TempJob);
            var defaultBlockBuffer = new NativeArray<BlockData>(0, Allocator.TempJob);

            AddNeighborsToModified(chunk);
            var args = CreateLightingRemovalArgs(chunk, defaultLightmap, defaultBlockBuffer, lightBfsQueue,
                lightRemovalBfsQueue
            );
            ScheduleJob(args, FloodFillLighting.OnPlacedBlockInSunlight.UpdateLighting(args, blockIndex, sunlightValue)
                .ThenDispose(lightBfsQueue)
                .ThenDispose(lightRemovalBfsQueue)
                .ThenDispose(defaultLightmap)
                .ThenDispose(defaultBlockBuffer)
            );
        }

        private LightingRemovalArgs CreateLightingRemovalArgs(Chunk chunk, NativeArray<byte> defaultLightmap,
            NativeArray<BlockData> defaultBlockBuffer,
            NativeQueue<FloodFillNode> lightBfsQueue, NativeQueue<LightingRemovalNode> lightRemovalBfsQueue) =>
            new LightingRemovalArgs
            {
                Lightmaps = LightmapNeighborhoodUtils.Create(_world, chunk.PositionXZ, defaultLightmap),
                Blocks = BlockNeighborhoodUtils.Create(_world, chunk.PositionXZ, defaultBlockBuffer),
                ModifiedChunkPositions = _modifiedChunkPositions,
                LightBfsQueue = lightBfsQueue,
                RemovalBfsQueue = lightRemovalBfsQueue,
                CenterChunkXZ = chunk.PositionXZ,
                ChunkSize = _world.ChunkSize,
            };

        private void OnTorchPlaced(Chunk chunk, int blockIndex, int emission)
        {
            if (!_torchlight) return;
            EnsureJobIsCompleted();

            var lightBfsQueue = new NativeQueue<FloodFillNode>(Allocator.TempJob);
            var defaultLightmap = new NativeArray<byte>(0, Allocator.TempJob);
            var defaultBlockBuffer = new NativeArray<BlockData>(0, Allocator.TempJob);

            var args = CreateFloodFillLightingArgs(chunk, defaultLightmap, defaultBlockBuffer, lightBfsQueue);
            ScheduleJob(args, FloodFillLighting.OnTorchPlaced.UpdateLighting(args, blockIndex, emission)
                .ThenDispose(lightBfsQueue)
                .ThenDispose(defaultLightmap)
                .ThenDispose(defaultBlockBuffer)
            );
        }

        private void OnTorchRemoved(Chunk chunk, int blockIndex, int emission)
        {
            if (!_torchlight) return;
            EnsureJobIsCompleted();

            var lightBfsQueue = new NativeQueue<FloodFillNode>(Allocator.TempJob);
            var lightRemovalBfsQueue = new NativeQueue<LightingRemovalNode>(Allocator.TempJob);
            var defaultLightmap = new NativeArray<byte>(0, Allocator.TempJob);
            var defaultBlockBuffer = new NativeArray<BlockData>(0, Allocator.TempJob);

            var args = CreateLightingRemovalArgs(chunk, defaultLightmap, defaultBlockBuffer, lightBfsQueue,
                lightRemovalBfsQueue
            );
            ScheduleJob(args, FloodFillLighting.OnTorchRemoved.UpdateLighting(args, blockIndex, emission)
                .ThenDispose(lightBfsQueue)
                .ThenDispose(lightRemovalBfsQueue)
                .ThenDispose(defaultLightmap)
                .ThenDispose(defaultBlockBuffer)
            );
        }

        private void LateUpdate()
        {
            CheckIfCompleted();
            if (_activeJobHandle.HasValue) return;

            if (!_modifiedChunkPositions.IsEmpty)
            {
                RecalculateAttenuationOfModifiedChunks();
                _modifiedChunkPositions.Clear();
            }

            if (_sunlight && _chunkGenerationQueue.Count > 0)
            {
                var chunk = _chunkGenerationQueue.Dequeue();
                AddNeighborsToModified(chunk);
                OnChunkGenerated(chunk);
            }
        }

        private void AddNeighborsToModified(Chunk chunk)
        {
            var positionXZ = chunk.PositionXZ;
            AddNeighborToModified(positionXZ, new int2(1, 0));
            AddNeighborToModified(positionXZ, new int2(-1, 0));
            AddNeighborToModified(positionXZ, new int2(0, 1));
            AddNeighborToModified(positionXZ, new int2(0, -1));
        }

        private void AddNeighborToModified(int2 positionXZ, int2 offsetXZ)
        {
            var neighborPositionXZ = positionXZ + offsetXZ;
            _modifiedChunkPositions.Add(neighborPositionXZ);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RecalculateAttenuationOfModifiedChunks()
        {
            var activeJobs = new NativeArray<JobHandle>(_modifiedChunkPositions.Count(), Allocator.TempJob);

            var index = 0;
            var defaultLightmap = new NativeArray<byte>(0, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            foreach (var modifiedChunkPosition in _modifiedChunkPositions)
            {
                if (!_world.TryGetChunkAt(modifiedChunkPosition, out var modifiedChunk)) continue;
                var chunkLighting = modifiedChunk.GetComponent<ChunkLighting>();
                var job = chunkLighting.CreateCalculateAttenuationJob(_world, defaultLightmap);
                activeJobs[index++] = job.Schedule();
            }

            JobHandle.CompleteAll(activeJobs);
            activeJobs.Dispose();
            defaultLightmap.Dispose();

            foreach (var modifiedChunkPosition in _modifiedChunkPositions)
            {
                if (!_world.TryGetChunkAt(modifiedChunkPosition, out var modifiedChunk)) continue;
                var chunkLighting = modifiedChunk.GetComponent<ChunkLighting>();
                chunkLighting.WriteAttenuationToTexture();
            }
        }

        private void Start()
        {
            Shader.SetGlobalVector(ChunkSizeId, ChunkSizeAsVector4);
        }

        private Vector4 ChunkSizeAsVector4
        {
            get
            {
                var chunkSize = _world.ChunkSize;
                return new Vector4(chunkSize.x, chunkSize.y, chunkSize.z);
            }
        }

        private void OnEnable()
        {
            Shader.SetGlobalFloat(MinAmbientLightingId, _minAmbientLighting);
            Shader.EnableKeyword(VoxelGiKeyword);

            _world.ChunkBlockChanged += _onBlockChanged;
            _world.ChunkWasGenerated += _onWasGenerated;
            _world.ChunkChanging += _onChanging;
        }

        private void OnDisable()
        {
            Shader.SetGlobalFloat(MinAmbientLightingId, 0f);
            Shader.DisableKeyword(VoxelGiKeyword);

            _world.ChunkBlockChanged -= _onBlockChanged;
            _world.ChunkWasGenerated -= _onWasGenerated;
            _world.ChunkChanging -= _onChanging;
        }

        private void Awake()
        {
            _modifiedChunkPositions = new NativeHashSet<int2>(0, Allocator.Persistent);

            _onBlockChanged = (sender, args) =>
            {
                var (chunk, oldBlock, localPosition) = args;
                if (!chunk.TryGetValidBlocks(out var blocksBuffer)) return;
                EnsureJobIsCompleted();

                var chunkSize = _world.ChunkSize;
                var blockIndex = ChunkUtils.PositionToIndex(localPosition, chunkSize);
                var newBlock = blocksBuffer[blockIndex];
                if (HaveSameLightingProperties(oldBlock, newBlock)) return;

                if (newBlock.PassesLight())
                {
                    OnRemovedLightBlocker(chunk, blockIndex);
                }
                else
                {
                    var chunkLighting = chunk.GetComponent<ChunkLighting>();
                    var lightmapValues = chunkLighting.LightmapValues;
                    var lightmapValue = lightmapValues[blockIndex];
                    var sunlightValue = LightingUtils.GetSunlight(lightmapValue);

                    if (!newBlock.EmitsLight())
                    {
                        var torchlightValue = LightingUtils.GetTorchlight(lightmapValue);
                        if (torchlightValue > 0) OnTorchRemoved(chunk, blockIndex, torchlightValue);
                    }

                    if (sunlightValue > 0) OnPlacedBlockInSunlight(chunk, blockIndex, sunlightValue);
                }

                if (oldBlock.EmitsLight()) OnTorchRemoved(chunk, blockIndex, oldBlock.Emission);
                if (newBlock.EmitsLight()) OnTorchPlaced(chunk, blockIndex, newBlock.Emission);
            };

            _onChanging = (sender, args) =>
            {
                if (_activeJobHandle == null) return;
                if (!_lastNeighborhoodData.centerXZ.Equals(args.PositionXZ)) return;

                EnsureJobIsCompleted();
            };

            _onWasGenerated = (sender, args) => _chunkGenerationQueue.Enqueue(args);
        }

        private void OnDestroy()
        {
            EnsureJobIsCompleted();
            _modifiedChunkPositions.DisposeIfCreated();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HaveSameLightingProperties(in BlockData blockData1, in BlockData blockData2) =>
            BothPassOrBlockLight(blockData1, blockData2) && HaveSameEmission(blockData1, blockData2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HaveSameEmission(in BlockData blockData1, in BlockData blockData2) =>
            blockData1.Emission == blockData2.Emission;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool BothPassOrBlockLight(in BlockData blockData1, in BlockData blockData2) =>
            blockData1.PassesLight() == blockData2.PassesLight();
    }
}