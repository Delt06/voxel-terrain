using Unity.Collections;

namespace Chunks.Lighting
{
    public struct BlockNeighborhood
    {
        [ReadOnly]
        public NativeArray<BlockData> Center;
        [ReadOnly]
        public NativeArray<BlockData> North;
        [ReadOnly]
        public NativeArray<BlockData> South;
        [ReadOnly]
        public NativeArray<BlockData> West;
        [ReadOnly]
        public NativeArray<BlockData> East;

        [ReadOnly]
        public NativeArray<BlockData> NorthWest;
        [ReadOnly]
        public NativeArray<BlockData> NorthEast;
        [ReadOnly]
        public NativeArray<BlockData> SouthWest;
        [ReadOnly]
        public NativeArray<BlockData> SouthEast;
    }
}