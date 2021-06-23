using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Chunks.Lighting
{
    public static class LightingUtils
    {
        private const byte BitsForTorchlight = 4;
        private const byte TorchlightMask = 0xF;
        private const byte SunlightMask = unchecked((byte) ~TorchlightMask);

        public const byte MaxLightValue = 15;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetSunlight(byte lightmapValue) =>
            (lightmapValue >> BitsForTorchlight) & TorchlightMask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetSunlight(ref byte lightmapValue, int sunlight)
        {
            lightmapValue = (byte) ((lightmapValue & TorchlightMask) | (sunlight << BitsForTorchlight));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetTorchlight(byte lightmapValue) => lightmapValue & TorchlightMask;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetTorchlight(ref byte lightmapValue, int torchlight)
        {
            lightmapValue = (byte) ((lightmapValue & SunlightMask) | torchlight);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 GetLightAttenuation(byte lightmapValue)
        {
            var sunlight = GetSunlight(lightmapValue);
            var torchlight = GetTorchlight(lightmapValue);
            return new float2(sunlight, torchlight) / new float2(MaxLightValue, MaxLightValue);
        }
    }
}