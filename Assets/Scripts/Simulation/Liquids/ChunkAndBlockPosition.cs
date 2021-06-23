using System;
using Unity.Burst;
using Unity.Mathematics;

namespace Simulation.Liquids
{
    [BurstCompile]
    public readonly struct ChunkAndBlockPosition : IEquatable<ChunkAndBlockPosition>
    {
        public readonly int2 ChunkXZ;
        public readonly int BlockIndex;

        public ChunkAndBlockPosition(int2 chunkXZ, int blockIndex)
        {
            ChunkXZ = chunkXZ;
            BlockIndex = blockIndex;
        }

        public bool Equals(ChunkAndBlockPosition other) =>
            ChunkXZ.Equals(other.ChunkXZ) && BlockIndex.Equals(other.BlockIndex);

        public override bool Equals(object obj) => obj is ChunkAndBlockPosition other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (ChunkXZ.GetHashCode() * 397) ^ BlockIndex.GetHashCode();
            }
        }
    }
}