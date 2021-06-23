using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Simulation.Liquids.Graphics
{
    public static class CameraExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SupportsTransparentWater(this in RenderingData renderingData)
        {
#if UNITY_EDITOR
            // Workaround to avoid proper water rendering if camera belongs to one of Scene View windows or material preview
            // TODO: add support for the listed cameras
            var camera = renderingData.cameraData.camera;
            return camera.gameObject.hideFlags != HideFlags.HideAndDontSave;
#else
            return true;
#endif
        }
    }
}