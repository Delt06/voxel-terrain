using System;
using Unity.Burst;

[BurstCompile]
public struct BlockData : IEquatable<BlockData>
{
    public readonly short ID;
    public readonly byte MeshIndex;
    public readonly byte SubMeshIndex;
    public readonly byte Emission;
    public readonly BlockFlags Flags;
    public byte Metadata;

    public BlockData(int id, int meshIndex, int subMeshIndex, byte emission = 0, byte metadata = 0,
        BlockFlags flags = default)
    {
        ID = (short) id;
        MeshIndex = (byte) meshIndex;
        SubMeshIndex = (byte) subMeshIndex;
        Emission = emission;
        Metadata = metadata;
        Flags = flags;
    }

    public bool Exists => ID >= 0;

    public static BlockData Empty => new BlockData(-1, 0, 0);

    public bool Equals(BlockData other)
    {
        if (Exists && other.Exists)
            return ID == other.ID;

        return !Exists && !other.Exists;
    }

    public override bool Equals(object obj)
    {
        if (obj is BlockData other)
            return Equals(other);
        return false;
    }

    public override int GetHashCode() => Exists ? ID : -1;
}