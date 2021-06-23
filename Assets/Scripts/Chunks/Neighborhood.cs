using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Chunks
{
    public struct Neighborhood<T> where T : struct
    {
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<T> Center;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<T> North;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<T> South;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<T> West;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<T> East;

        [NativeDisableContainerSafetyRestriction]
        public NativeArray<T> NorthWest;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<T> NorthEast;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<T> SouthWest;
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<T> SouthEast;
    }
}