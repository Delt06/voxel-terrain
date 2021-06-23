namespace Chunks.Lighting
{
    public static class BlockDataLightingExtensions
    {
        public static bool EmitsLight(this in BlockData blockData) => blockData.Exists && blockData.Emission > 0;

        public static bool PassesLight(this in BlockData blockData) => !blockData.Exists || blockData.IsTransparent();
    }
}