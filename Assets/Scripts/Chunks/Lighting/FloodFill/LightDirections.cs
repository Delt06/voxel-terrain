using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace Chunks.Lighting.FloodFill
{
    [BurstCompile]
    internal static class LightDirections
    {
        public const int Count = 6;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 GetAt(int index) =>
            index switch
            {
                0 => new int3(1, 0, 0),
                1 => new int3(-1, 0, 0),
                2 => new int3(0, 1, 0),
                3 => new int3(0, -1, 0),
                4 => new int3(0, 0, 1),
                5 => new int3(0, 0, -1),
                _ => int3.zero,
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SunlightDecays(in int3 direction) => !direction.Equals(new int3(0, -1, 0));
    }
}