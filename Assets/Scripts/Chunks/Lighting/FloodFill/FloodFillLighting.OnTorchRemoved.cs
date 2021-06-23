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
        public static class OnTorchRemoved
        {
            [BurstCompile]
            private struct UpdateLightingRemoveJob : IJob
            {
                public LightingRemovalArgs Args;
                public NativeArray<byte> CenterLightmapValues;
                public int BlocksIndex;
                public int Emission;

                public void Execute()
                {
                    var lightRemovalBfsQueue = Args.RemovalBfsQueue;
                    var lightmapValues = CenterLightmapValues;
                    SetTorchlight(lightmapValues, BlocksIndex, 0);
                    var chunkXZ = Args.CenterChunkXZ;
                    lightRemovalBfsQueue.Enqueue(new LightingRemovalNode
                    {
                        BlockIndex = BlocksIndex,
                        ChunkXZ = chunkXZ,
                        LightLevel = Emission,
                    }
                    );
                    Args.ModifiedChunkPositions.Add(chunkXZ);

                    RemoveLight(Args);

                    var fillLightingArgs = new FloodFillLightingArgs
                    {
                        Blocks = Args.Blocks,
                        Lightmaps = Args.Lightmaps,
                        BfsQueue = Args.LightBfsQueue,
                        ChunkSize = Args.ChunkSize,
                        ModifiedChunkPositions = Args.ModifiedChunkPositions,
                        CenterChunkXZ = Args.CenterChunkXZ,
                    };
                    OnTorchPlaced.FloodFillLight(fillLightingArgs);
                }
            }


            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static JobHandle UpdateLighting(LightingRemovalArgs args, int blockIndex, int emission)
            {
                if (!args.Lightmaps.TryGetCenterBuffer(out var lightmapValues)) return default;

                return new UpdateLightingRemoveJob
                {
                    Args = args,
                    Emission = emission,
                    BlocksIndex = blockIndex,
                    CenterLightmapValues = lightmapValues,
                }.Schedule();
            }

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
                    RemoveInNeighbor(args, node, lightDirection);
                }
            }

            private static void RemoveInNeighbor(LightingRemovalArgs args, in LightingRemovalNode node,
                in int3 offset)
            {
                if (!TryGetNeighborBlockData(args, node.ChunkXZ, node.BlockIndex, offset, out var neighborXZ,
                    out _, out var neighborLightmapValues, out var neighborBlockIndex
                ))
                    return;
                var lightLevel = node.LightLevel;

                var neighborLightmapValue = neighborLightmapValues[neighborBlockIndex];
                var neighborLightLevel = LightingUtils.GetTorchlight(neighborLightmapValue);

                if (neighborLightLevel != 0 && neighborLightLevel < lightLevel)
                {
                    SetTorchlight(neighborLightmapValues, neighborBlockIndex, 0);
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