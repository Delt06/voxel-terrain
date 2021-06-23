using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Chunks.Lighting.FloodFill.FloodFillUtils;

namespace Chunks.Lighting.FloodFill
{
    internal static partial class FloodFillLighting
    {
        [BurstCompile]
        public static class OnChunkGenerated
        {
            [BurstCompile]
            private struct FindSunlightSourcesJob : IJob
            {
                [ReadOnly]
                public NativeArray<BlockData> Blocks;
                [ReadOnly]
                public int3 ChunkSize;
                public NativeList<int> SunlightSourceIndices;

                public void Execute()
                {
                    for (var xi = 0; xi < ChunkSize.x; xi++)
                    {
                        for (var zi = 0; zi < ChunkSize.z; zi++)
                        {
                            var blockPosition = new int3(xi, ChunkSize.y - 1, zi);
                            var blockIndex = ChunkUtils.PositionToIndex(blockPosition, ChunkSize);
                            var block = Blocks[blockIndex];
                            if (!block.PassesLight()) continue;

                            SunlightSourceIndices.Add(blockIndex);
                        }
                    }
                }
            }

            [BurstCompile]
            private struct ProcessFoundSunlightSourcesJob : IJob
            {
                [ReadOnly]
                public NativeList<int> SunlightSourceIndices;

                [ReadOnly]
                public int2 ChunkXZ;

                public NativeArray<byte> LightmapValues;

                public NativeQueue<FloodFillNode> LightBfsQueue;

                public void Execute()
                {
                    // ReSharper disable once ForCanBeConvertedToForeach
                    for (var i = 0; i < SunlightSourceIndices.Length; i++)
                    {
                        var blockIndex = SunlightSourceIndices[i];
                        SetSunlight(LightmapValues, blockIndex, LightingUtils.MaxLightValue);
                        LightBfsQueue.Enqueue(new FloodFillNode
                            {
                                ChunkXZ = ChunkXZ,
                                BlockIndex = blockIndex,
                            }
                        );
                    }
                }
            }

            [BurstCompile]
            private struct FloodFillLightJob : IJob
            {
                public FloodFillLightingArgs Args;

                public void Execute()
                {
                    Args.ModifiedChunkPositions.Add(Args.CenterChunkXZ);
                    FloodFillLight(Args);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static JobHandle UpdateLightingFromChunkTop(FloodFillLightingArgs args)
            {
                if (!args.Blocks.TryGetCenterBuffer(out var blocks)) return default;
                if (!args.Lightmaps.TryGetCenterBuffer(out var lightmaps)) return default;

                var blockIndices = new NativeList<int>(0, Allocator.TempJob);
                return new FindSunlightSourcesJob
                    {
                        Blocks = blocks,
                        ChunkSize = args.ChunkSize,
                        SunlightSourceIndices = blockIndices,
                    }
                    .Schedule()
                    .Then(new ProcessFoundSunlightSourcesJob
                        {
                            LightmapValues = lightmaps,
                            ChunkXZ = args.CenterChunkXZ,
                            LightBfsQueue = args.BfsQueue,
                            SunlightSourceIndices = blockIndices,
                        }
                    )
                    .ThenDispose(blockIndices)
                    .Then(new FloodFillLightJob { Args = args });
            }

            [BurstCompile]
            private struct UpdateLightingFromNeighborBlocksJob : IJob
            {
                public FloodFillLightingArgs Args;
                public int2 ChunkPositionXZ;
                public int BlockIndex;

                public void Execute()
                {
                    for (var i = 0; i < LightDirections.Count; i++)
                    {
                        var lightDirection = LightDirections.GetAt(i);
                        TryAddNeighborToQueue(Args, ChunkPositionXZ, BlockIndex, lightDirection);
                    }

                    FloodFillLight(Args);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static JobHandle UpdateLightingFromNeighborBlocks(in FloodFillLightingArgs args,
                int2 chunkPositionXZ,
                int blockIndex, JobHandle dependsOn = default) =>
                new UpdateLightingFromNeighborBlocksJob
                {
                    Args = args,
                    BlockIndex = blockIndex,
                    ChunkPositionXZ = chunkPositionXZ,
                }.Schedule(dependsOn);

            private static void TryAddNeighborToQueue(FloodFillLightingArgs args, int2 chunkPositionXZ, int blockIndex,
                in int3 offset)
            {
                if (!TryGetNeighborBlockData(args, chunkPositionXZ, blockIndex, offset,
                    out var neighborXZ,
                    out var neighborBlocks, out var neighborLightmapValues,
                    out var neighborBlockIndex
                ))
                    return;

                var block = neighborBlocks[neighborBlockIndex];
                if (!block.PassesLight()) return;

                var sunlight = LightingUtils.GetSunlight(neighborLightmapValues[neighborBlockIndex]);
                if (sunlight == 0) return;

                args.BfsQueue.Enqueue(new FloodFillNode
                    {
                        ChunkXZ = neighborXZ,
                        BlockIndex = neighborBlockIndex,
                    }
                );
                args.ModifiedChunkPositions.Add(neighborXZ);
            }

            public static void FloodFillLight(in FloodFillLightingArgs args)
            {
                var lightBfsQueue = args.BfsQueue;
                while (lightBfsQueue.Count > 0)
                {
                    var node = lightBfsQueue.Dequeue();
                    var blockIndex = node.BlockIndex;
                    if (!args.Lightmaps.TryGetBuffer(args.CenterChunkXZ, node.ChunkXZ, out var lightmapValues))
                        continue;

                    var lightmapValue = lightmapValues[blockIndex];
                    if (LightingUtils.GetSunlight(lightmapValue) <= 1) continue;

                    PropagateToNeighbors(args, node);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void PropagateToNeighbors(in FloodFillLightingArgs args, in FloodFillNode node)
            {
                for (var i = 0; i < LightDirections.Count; i++)
                {
                    var lightDirection = LightDirections.GetAt(i);
                    if (LightDirections.SunlightDecays(lightDirection))
                        PropagateToNeighbor(args, node, lightDirection);
                    else
                        PropagateToNeighbor(args, node, lightDirection, false);
                }
            }

            private static void PropagateToNeighbor(FloodFillLightingArgs args, in FloodFillNode node,
                in int3 offset, bool decayIfMaximum = true)
            {
                var chunkPositionXZ = node.ChunkXZ;
                var blockIndex = node.BlockIndex;

                if (!TryGetNeighborBlockData(args, chunkPositionXZ, blockIndex, offset,
                    out var neighborXZ,
                    out var neighborBlocks, out var neighborLightmapValues,
                    out var neighborBlockIndex
                ))
                    return;

                if (!TryGetLightmapValues(args, chunkPositionXZ, out var lightmapValues))
                    return;

                var neighborBlock = neighborBlocks[neighborBlockIndex];
                if (!neighborBlock.PassesLight()) return;

                var lightmapValue = lightmapValues[blockIndex];
                var lightLevel = LightingUtils.GetSunlight(lightmapValue);

                var neighborLightmapValue = neighborLightmapValues[neighborBlockIndex];
                var neighborLightLevel = LightingUtils.GetSunlight(neighborLightmapValue);

                var decay = 1;
                if (!decayIfMaximum && lightLevel == LightingUtils.MaxLightValue)
                    decay = 0;

                var propagatedLightLevel = lightLevel - decay;
                if (neighborLightLevel >= propagatedLightLevel) return;

                SetSunlight(neighborLightmapValues, neighborBlockIndex, propagatedLightLevel);
                args.BfsQueue.Enqueue(new FloodFillNode
                    {
                        ChunkXZ = neighborXZ,
                        BlockIndex = neighborBlockIndex,
                    }
                );
                args.ModifiedChunkPositions.Add(neighborXZ);
            }
        }
    }
}