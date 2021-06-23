using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace Chunks.Lighting.FloodFill
{
    [BurstCompile]
    internal static class FloodFillUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetNeighborBlockData(in FloodFillLightingArgs args, int2 chunkPositionXZ,
            int blockIndex,
            in int3 offset, out int2 neighborXZ, out NativeArray<BlockData> neighborBlocks,
            out NativeArray<byte> neighborLightmapValues,
            out int neighborBlockIndex)
        {
            neighborXZ = default;
            neighborBlocks = default;
            neighborLightmapValues = default;
            neighborBlockIndex = default;

            var chunkSize = args.ChunkSize;
            var localPosition = ChunkUtils.IndexToPosition(blockIndex, chunkSize);

            if (!ChunkUtils.TryApplyOffset(chunkPositionXZ, localPosition, offset, chunkSize, out var offsetResult))
                return false;

            neighborXZ = offsetResult.ChunkXZ;
            if (!TryGetBlockBuffer(args, neighborXZ, out neighborBlocks))
                return false;
            if (!TryGetLightmapValues(args, neighborXZ, out neighborLightmapValues))
                return false;

            neighborBlockIndex = ChunkUtils.PositionToIndex(offsetResult.LocalPosition, chunkSize);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetNeighborBlockData(in LightingRemovalArgs args, int2 chunkPositionXZ,
            int blockIndex,
            in int3 offset, out int2 neighborXZ, out NativeArray<BlockData> neighborBlocks,
            out NativeArray<byte> neighborLightmapValues,
            out int neighborBlockIndex)
        {
            neighborXZ = default;
            neighborBlocks = default;
            neighborLightmapValues = default;
            neighborBlockIndex = default;

            var chunkSize = args.ChunkSize;
            var localPosition = ChunkUtils.IndexToPosition(blockIndex, chunkSize);

            if (!ChunkUtils.TryApplyOffset(chunkPositionXZ, localPosition, offset, chunkSize, out var offsetResult))
                return false;

            neighborXZ = offsetResult.ChunkXZ;
            if (!TryGetBlockBuffer(args, neighborXZ, out neighborBlocks))
                return false;
            if (!TryGetLightmapValues(args, neighborXZ, out neighborLightmapValues))
                return false;

            neighborBlockIndex = ChunkUtils.PositionToIndex(offsetResult.LocalPosition, chunkSize);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetBlockBuffer(in FloodFillLightingArgs args, int2 chunkPositionXZ,
            out NativeArray<BlockData> blockBuffer) =>
            args.Blocks.TryGetBuffer(args.CenterChunkXZ, chunkPositionXZ, out blockBuffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetLightmapValues(in FloodFillLightingArgs args, int2 chunkPositionXZ,
            out NativeArray<byte> lightmapValues) =>
            args.Lightmaps.TryGetBuffer(args.CenterChunkXZ, chunkPositionXZ, out lightmapValues);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetBlockBuffer(in LightingRemovalArgs args, int2 chunkPositionXZ,
            out NativeArray<BlockData> blockBuffer) =>
            args.Blocks.TryGetBuffer(args.CenterChunkXZ, chunkPositionXZ, out blockBuffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetLightmapValues(in LightingRemovalArgs args, int2 chunkPositionXZ,
            out NativeArray<byte> lightmapValues) =>
            args.Lightmaps.TryGetBuffer(args.CenterChunkXZ, chunkPositionXZ, out lightmapValues);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTorchlight(NativeArray<byte> lightmapValues, int blockIndex, int value)
        {
            var lightmapValue = lightmapValues[blockIndex];
            LightingUtils.SetTorchlight(ref lightmapValue, value);
            lightmapValues[blockIndex] = lightmapValue;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetSunlight(NativeArray<byte> lightmapValues, int blockIndex, int value)
        {
            var lightmapValue = lightmapValues[blockIndex];
            LightingUtils.SetSunlight(ref lightmapValue, value);
            lightmapValues[blockIndex] = lightmapValue;
        }
    }
}