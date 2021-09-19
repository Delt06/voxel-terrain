using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Simulation.Liquids.Graphics
{
    public static class CameraExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SupportsTransparentWater(this in RenderingData renderingData) =>
            renderingData.cameraData.cameraType == CameraType.Game;
    }
}