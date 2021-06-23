using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Chunks.Lighting.FloodFill.FloodFillUtils;

namespace Chunks.Lighting.FloodFill
{
    [BurstCompile]
    internal static partial class FloodFillLighting
    {
        [BurstCompile]
        public static class OnPlacedBlockInSunlight
        {
            [BurstCompile]
            private struct UpdateLightingJob : IJob
            {
                public LightingRemovalArgs Args;
                public NativeArray<byte> CenterLightmapValues;
                public int BlockIndex;
                public int SunlightValue;

                public void Execute()
                {
                    var lightRemovalBfsQueue = Args.RemovalBfsQueue;

                    SetSunlight(CenterLightmapValues, BlockIndex, 0);
                    lightRemovalBfsQueue.Enqueue(new LightingRemovalNode
                    {
                        BlockIndex = BlockIndex,
                        ChunkXZ = Args.CenterChunkXZ,
                        LightLevel = SunlightValue,
                    }
                    );
                    Args.ModifiedChunkPositions.Add(Args.CenterChunkXZ);

                    RemoveLight(Args);

                    var fillLightingArgs =
                        new FloodFillLightingArgs
                        {
                            Blocks = Args.Blocks,
                            Lightmaps = Args.Lightmaps,
                            BfsQueue = Args.LightBfsQueue,
                            ChunkSize = Args.ChunkSize,
                            ModifiedChunkPositions = Args.ModifiedChunkPositions,
                            CenterChunkXZ = Args.CenterChunkXZ,
                        };
                    OnChunkGenerated.FloodFillLight(fillLightingArgs);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static JobHandle UpdateLighting(LightingRemovalArgs args, int blockIndex,
                int sunlightValue)
            {
                if (!args.Lightmaps.TryGetCenterBuffer(out var lightmapValues)) return default;

                return new UpdateLightingJob
                {
                    Args = args,
                    BlockIndex = blockIndex,
                    SunlightValue = sunlightValue,
                    CenterLightmapValues = lightmapValues,
                }.Schedule();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void RemoveLight(in LightingRemovalArgs args)
            {
                var removalBfsQueue = args.RemovalBfsQueue;
                while (removalBfsQueue.Count > 0)
                {
                    var node = removalBfsQueue.Dequeue();
                    RemoveInNeighbors(args, node);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static void RemoveInNeighbors(in LightingRemovalArgs args, in LightingRemovalNode node)
            {
                for (var i = 0; i < LightDirections.Count; i++)
                {
                    var lightDirection = LightDirections.GetAt(i);
                    if (LightDirections.SunlightDecays(lightDirection))
                        RemoveInNeighbor(args, node, lightDirection);
                    else
                        RemoveInNeighbor(args, node, lightDirection, true);
                }
            }

            private static void RemoveInNeighbor(LightingRemovalArgs args, in LightingRemovalNode node,
                in int3 offset, bool removeIfMaximum = false)
            {
                if (!TryGetNeighborBlockData(args, node.ChunkXZ, node.BlockIndex, offset, out var neighborXZ,
                    out _, out var neighborLightmapValues, out var neighborBlockIndex
                ))
                    return;

                var lightLevel = node.LightLevel;

                var neighborLightmapValue = neighborLightmapValues[neighborBlockIndex];
                var neighborLightLevel = LightingUtils.GetSunlight(neighborLightmapValue);

                if (neighborLightLevel != 0 && neighborLightLevel < lightLevel ||
                    removeIfMaximum && lightLevel == LightingUtils.MaxLightValue)
                {
                    SetSunlight(neighborLightmapValues, neighborBlockIndex, 0);
                    args.RemovalBfsQueue.Enqueue(new LightingRemovalNode
                    {
                        ChunkXZ = neighborXZ,
                        BlockIndex = neighborBlockIndex,
                        LightLevel = neighborLightLevel,
                    }
                    );
                    args.ModifiedChunkPositions.Add(neighborXZ);
                }
                else if (neighborLightLevel >= lightLevel)
                {
                    args.LightBfsQueue.Enqueue(new FloodFillNode
                    {
                        ChunkXZ = neighborXZ,
                        BlockIndex = neighborBlockIndex,
                    }
                    );
                }
            }
        }
    }
}