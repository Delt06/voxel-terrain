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
        public static class OnTorchPlaced
        {
            [BurstCompile]
            private struct FloodFillLightJob : IJob
            {
                public FloodFillLightingArgs Args;
                public NativeArray<byte> CenterLightmapValues;
                public int BlockIndex;
                public int Emission;

                public void Execute()
                {
                    var chunkXZ = Args.CenterChunkXZ;
                    var lightBfsQueue = Args.BfsQueue;
                    SetTorchlight(CenterLightmapValues, BlockIndex, Emission);
                    lightBfsQueue.Enqueue(new FloodFillNode
                    {
                        BlockIndex = BlockIndex,
                        ChunkXZ = chunkXZ,
                    }
                    );
                    Args.ModifiedChunkPositions.Add(chunkXZ);
                    FloodFillLight(Args);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static JobHandle UpdateLighting(FloodFillLightingArgs args, int blockIndex, int emission)
            {
                if (!args.Lightmaps.TryGetCenterBuffer(out var lightmapValues)) return default;

                return new FloodFillLightJob
                {
                    Args = args,
                    Emission = emission,
                    BlockIndex = blockIndex,
                    CenterLightmapValues = lightmapValues,
                }.Schedule();
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
                if (!block.PassesLight() && !block.EmitsLight()) return;

                var torchlight = LightingUtils.GetTorchlight(neighborLightmapValues[neighborBlockIndex]);
                if (!block.EmitsLight() && torchlight == 0) return;

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
                    if (!args.Lightmaps.TryGetBuffer(args.CenterChunkXZ, node.ChunkXZ, out var lightmapValues))
                        continue;

                    var blockIndex = node.BlockIndex;
                    var lightmapValue = lightmapValues[blockIndex];
                    if (LightingUtils.GetTorchlight(lightmapValue) <= 1) continue;

                    PropagateToNeighbors(args, node);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void PropagateToNeighbors(in FloodFillLightingArgs args, in FloodFillNode node)
            {
                for (var i = 0; i < LightDirections.Count; i++)
                {
                    var lightDirection = LightDirections.GetAt(i);
                    PropagateToNeighbor(args, node, lightDirection);
                }
            }

            private static void PropagateToNeighbor(FloodFillLightingArgs args, in FloodFillNode node,
                in int3 offset, int decay = 1)
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
                var lightLevel = LightingUtils.GetTorchlight(lightmapValue);

                var neighborLightmapValue = neighborLightmapValues[neighborBlockIndex];
                var neighborLightLevel = LightingUtils.GetTorchlight(neighborLightmapValue);
                var propagatedLightLevel = lightLevel - decay;
                if (neighborLightLevel >= propagatedLightLevel) return;

                SetTorchlight(neighborLightmapValues, neighborBlockIndex, propagatedLightLevel);
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