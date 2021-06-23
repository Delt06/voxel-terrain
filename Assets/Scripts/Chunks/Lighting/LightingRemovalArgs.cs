using Unity.Collections;
using Unity.Mathematics;

namespace Chunks.Lighting
{
    public struct LightingRemovalArgs
    {
        public NativeHashSet<int2> ModifiedChunkPositions;
        public NativeQueue<LightingRemovalNode> RemovalBfsQueue;
        public NativeQueue<FloodFillNode> LightBfsQueue;
        public Neighborhood<byte> Lightmaps;
        public int3 ChunkSize;
        public Neighborhood<BlockData> Blocks;
        public int2 CenterChunkXZ;
    }

    public struct LightingRemovalNode
    {
        public int2 ChunkXZ;
        public int BlockIndex;
        public int LightLevel;
    }
}