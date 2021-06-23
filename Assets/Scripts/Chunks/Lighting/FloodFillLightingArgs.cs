using Unity.Collections;
using Unity.Mathematics;

namespace Chunks.Lighting
{
    public struct FloodFillLightingArgs
    {
        public NativeHashSet<int2> ModifiedChunkPositions;
        public NativeQueue<FloodFillNode> BfsQueue;
        public Neighborhood<byte> Lightmaps;
        public Neighborhood<BlockData> Blocks;
        public int2 CenterChunkXZ;
        public int3 ChunkSize;
    }

    public struct FloodFillNode
    {
        public int2 ChunkXZ;
        public int BlockIndex;
    }
}