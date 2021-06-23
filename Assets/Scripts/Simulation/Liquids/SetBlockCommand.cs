using Unity.Burst;
using Unity.Mathematics;

namespace Simulation.Liquids
{
    [BurstCompile]
    public struct SetBlockCommand
    {
        public int2 ChunkXZ;
        public BlockData BlockData;
        public int3 LocalPosition;
        public int Timestamp;
    }
}