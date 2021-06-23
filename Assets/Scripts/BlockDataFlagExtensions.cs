using System.Runtime.CompilerServices;
using Unity.Burst;

[BurstCompile]
public static class BlockDataFlagExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsTransparent(in this BlockData blockData) => Include(blockData.Flags, BlockFlags.Transparent);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLiquid(in this BlockData blockData) => blockData.Flags.Include(BlockFlags.Liquid);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Include(this BlockFlags flags, BlockFlags otherFlags) => (flags & otherFlags) == otherFlags;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CanPlaceOver(in this BlockData blockData) =>
        !blockData.Exists || blockData.Flags.Include(BlockFlags.CanPlaceOver);
}