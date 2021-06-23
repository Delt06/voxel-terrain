using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Chunks.Lighting
{
    public static class BlockNeighborhoodUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Neighborhood<BlockData> Create(World world,
            int2 positionXZ, NativeArray<BlockData> defaultBlockBuffer) =>
            new Neighborhood<BlockData>
            {
                Center = GetBlockBufferOrDefault(world, positionXZ, defaultBlockBuffer),
                East = GetBlockBufferOrDefault(world, positionXZ + new int2(1, 0), defaultBlockBuffer),
                West = GetBlockBufferOrDefault(world, positionXZ + new int2(-1, 0), defaultBlockBuffer),
                North = GetBlockBufferOrDefault(world, positionXZ + new int2(0, 1), defaultBlockBuffer),
                South = GetBlockBufferOrDefault(world, positionXZ + new int2(0, -1), defaultBlockBuffer),
                NorthEast = GetBlockBufferOrDefault(world, positionXZ + new int2(1, 1), defaultBlockBuffer),
                NorthWest = GetBlockBufferOrDefault(world, positionXZ + new int2(-1, 1), defaultBlockBuffer),
                SouthEast = GetBlockBufferOrDefault(world, positionXZ + new int2(1, -1), defaultBlockBuffer),
                SouthWest = GetBlockBufferOrDefault(world, positionXZ + new int2(-1, -1), defaultBlockBuffer),
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeArray<BlockData> GetBlockBufferOrDefault(World world, int2 positionXZ,
            NativeArray<BlockData> defaultBlockBuffer)
        {
            if (!world.TryGetChunkAt(positionXZ, out var chunk)) return defaultBlockBuffer;
            if (!chunk.TryGetValidBlocks(out var blocks)) return defaultBlockBuffer;
            return blocks;
        }
    }
}