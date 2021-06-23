using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Chunks.Lighting
{
    [BurstCompile]
    public struct CalculateAttenuationJob : IJob
    {
        [ReadOnly]
        public Neighborhood<byte> Lightmaps;

        [ReadOnly]
        public int3 ChunkSize;

        [WriteOnly]
        public NativeArray<float2> LightmapAttenuationValues;

        private static readonly int3 Padding = new int3(1, 0, 1);

        public void Execute()
        {
            var lightmapSize = ChunkSize + Padding * 2;

            for (int i = 0, valuesCount = LightmapAttenuationValues.Length; i < valuesCount; i++)
            {
                var position = ChunkUtils.IndexToPosition(i, lightmapSize);
                position -= Padding;
                byte lightmapValue;

                var clampedPosition = ChunkUtils.ClampToBounds(position, ChunkSize);
                var outOfBounds = !clampedPosition.Equals(position);
                if (outOfBounds)
                {
                    if (Lightmaps.TryGetNeighbor(position, ChunkSize, out var neighborLightmap,
                        out var lightmapLocalPosition
                    ))
                    {
                        var blockIndex = ChunkUtils.PositionToIndex(lightmapLocalPosition, ChunkSize);
                        lightmapValue = neighborLightmap[blockIndex];
                    }
                    else
                    {
                        lightmapValue = 0;
                    }
                }
                else
                {
                    var blockIndex = ChunkUtils.PositionToIndex(position, ChunkSize);
                    lightmapValue = Lightmaps.Center[blockIndex];
                }

                var attenuation = LightingUtils.GetLightAttenuation(lightmapValue);
                LightmapAttenuationValues[i] = attenuation;
            }
        }
    }
}