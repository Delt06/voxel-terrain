using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace Simulation.Liquids
{
    [BurstCompile]
    public static class LiquidUtils
    {
        private const byte LevelMask = 0b00001111;
        private const byte DecayMask = 0b00110000;
        private const byte SourceFlagMask = 0b01000000;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLiquidSource(in this BlockData blockData) =>
            (blockData.Metadata & SourceFlagMask) >> 6 != 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLiquidDecay(in this BlockData blockData) => ((blockData.Metadata & DecayMask) >> 4) + 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetLiquidLevel(in this BlockData blockData) => blockData.Metadata & LevelMask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetIsLiquidSource(ref this BlockData blockData, bool isSource)
        {
            blockData.Metadata &= ~SourceFlagMask & 0xFF;
            var flagBit = isSource ? 1 : 0;
            blockData.Metadata |= (byte) (flagBit << 6);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetLiquidDecay(ref this BlockData blockData, int decay)
        {
            decay = math.clamp(decay, 1, MaxLiquidDecay);
            decay--;
            blockData.Metadata &= ~DecayMask & 0xFF;
            blockData.Metadata |= (byte) (decay << 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetLiquidLevel(ref this BlockData blockData, int level)
        {
            level = math.clamp(level, 0, MaxLiquidLevel);
            blockData.Metadata &= ~LevelMask & 0xFF;
            blockData.Metadata |= (byte) level;
        }

        public const int MaxLiquidLevel = 15;
        public const int MaxLiquidDecay = 4;
    }
}