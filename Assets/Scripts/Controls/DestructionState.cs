using Unity.Mathematics;

namespace Controls
{
    public struct DestructionState
    {
        public int2 ChunkPositionXZ;
        public int3 LocalBlockPosition;
        public BlockData Block;
        public float Progress;
        public float LastUpdateTime;
    }
}