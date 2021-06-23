using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace Chunks
{
    public static class NeighborhoodExt
    {
        [BurstCompile]
        public static bool TryGetBuffer<T>(this in Neighborhood<T> blocks, int2 centerXZ, int2 neighborXZ,
            out NativeArray<T> neighbor) where T : struct
        {
            var offset = neighborXZ - centerXZ;
            neighbor = offset switch
            {
                { x: -1, y: -1 } => blocks.SouthWest,
                { x: -1, y: 0 } => blocks.West,
                { x: -1, y: 1 } => blocks.NorthWest,
                { x: 0, y: -1 } => blocks.South,
                { x: 0, y: 0 } => blocks.Center,
                { x: 0, y: 1 } => blocks.North,
                { x: 1, y: -1 } => blocks.SouthEast,
                { x: 1, y: 0 } => blocks.East,
                { x: 1, y: 1 } => blocks.NorthEast,
                _ => default,
            };
            return neighbor.IsCreated && neighbor.Length > 0;
        }

        [BurstCompile]
        public static bool TryGetCenterBuffer<T>(this in Neighborhood<T> blocks,
            out NativeArray<T> neighbor) where T : struct
        {
            neighbor = blocks.Center;
            return neighbor.IsCreated && neighbor.Length > 0;
        }
    }
}