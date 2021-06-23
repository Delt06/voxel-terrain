using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace Chunks.Lighting
{
    public static class LightmapNeighborhoodUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Neighborhood<byte> Create(World world,
            int2 positionXZ, NativeArray<byte> defaultLightmap) =>
            new Neighborhood<byte>
            {
                Center = GetLightmapValuesOrDefault(world, positionXZ, defaultLightmap),
                East = GetLightmapValuesOrDefault(world, positionXZ + new int2(1, 0), defaultLightmap),
                West = GetLightmapValuesOrDefault(world, positionXZ + new int2(-1, 0), defaultLightmap),
                North = GetLightmapValuesOrDefault(world, positionXZ + new int2(0, 1), defaultLightmap),
                South = GetLightmapValuesOrDefault(world, positionXZ + new int2(0, -1), defaultLightmap),
                NorthEast = GetLightmapValuesOrDefault(world, positionXZ + new int2(1, 1), defaultLightmap),
                NorthWest = GetLightmapValuesOrDefault(world, positionXZ + new int2(-1, 1), defaultLightmap),
                SouthEast = GetLightmapValuesOrDefault(world, positionXZ + new int2(1, -1), defaultLightmap),
                SouthWest = GetLightmapValuesOrDefault(world, positionXZ + new int2(-1, -1), defaultLightmap),
            };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static NativeArray<byte> GetLightmapValuesOrDefault(World world, int2 positionXZ,
            NativeArray<byte> defaultLightmap)
        {
            if (!world.TryGetChunkAt(positionXZ, out var chunk)) return defaultLightmap;
            var chunkLighting = chunk.GetComponent<ChunkLighting>();
            return chunkLighting.LightmapValues;
        }

        [BurstCompile]
        public static bool TryGetNeighbor(this in Neighborhood<byte> lightmaps, in int3 localPosition,
            in int3 chunkSize,
            out NativeArray<byte> lightmap, out int3 lightmapLocalPosition)
        {
            var resolved =
                lightmaps.TryResolveNeighbor(localPosition, chunkSize, out lightmap, out lightmapLocalPosition);
            if (!resolved) return false;

            if (lightmapLocalPosition.x < 0 || lightmapLocalPosition.x >= chunkSize.x) return false;
            if (lightmapLocalPosition.z < 0 || lightmapLocalPosition.z >= chunkSize.z) return false;

            return lightmap.IsCreated &&
                   lightmap.Length > 0;
        }

        [BurstCompile]
        private static bool TryResolveNeighbor(in this Neighborhood<byte> lightmaps, in int3 localPosition,
            in int3 chunkSize, out NativeArray<byte> lightmap,
            out int3 lightmapLocalPosition)
        {
            if (localPosition.y < 0 || localPosition.y >= chunkSize.y)
            {
                lightmap = default;
                lightmapLocalPosition = default;
                return false;
            }

            lightmapLocalPosition = localPosition;

            if (localPosition.x < 0)
            {
                lightmapLocalPosition.x += chunkSize.x;

                if (localPosition.z < 0)
                {
                    lightmap = lightmaps.SouthWest;
                    lightmapLocalPosition.z += chunkSize.z;
                    return true;
                }

                if (localPosition.z >= chunkSize.z)
                {
                    lightmap = lightmaps.NorthWest;
                    lightmapLocalPosition.z -= chunkSize.z;
                    return true;
                }

                lightmap = lightmaps.West;
                return true;
            }

            if (localPosition.x >= chunkSize.x)
            {
                lightmapLocalPosition.x -= chunkSize.x;

                if (localPosition.z < 0)
                {
                    lightmap = lightmaps.SouthEast;
                    lightmapLocalPosition.z += chunkSize.z;
                    return true;
                }

                if (localPosition.z >= chunkSize.z)
                {
                    lightmap = lightmaps.NorthEast;
                    lightmapLocalPosition.z -= chunkSize.z;
                    return true;
                }

                lightmap = lightmaps.East;
                return true;
            }

            if (localPosition.z < 0)
            {
                lightmap = lightmaps.South;
                lightmapLocalPosition.z += chunkSize.z;
                return true;
            }

            if (localPosition.z >= chunkSize.z)
            {
                lightmap = lightmaps.North;
                lightmapLocalPosition.z -= chunkSize.z;
                return true;
            }

            lightmap = default;
            lightmapLocalPosition = default;
            return false;
        }
    }
}