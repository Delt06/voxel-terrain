using System;
using Unity.Collections;

namespace Chunks
{
    public static class NativeCollectionsExt
    {
        public static void EnsureCreated<T>(this ref NativeArray<T> buffer, int length, Allocator allocator,
            NativeArrayOptions arrayOptions = NativeArrayOptions.ClearMemory) where T : struct
        {
            if (!buffer.IsCreated)
                buffer = new NativeArray<T>(length, allocator, arrayOptions);
        }

        public static void EnsureCreated<TKey, TValue>(this ref NativeHashMap<TKey, TValue> map, int capacity,
            Allocator allocator) where TKey : struct, IEquatable<TKey> where TValue : struct
        {
            if (!map.IsCreated)
                map = new NativeHashMap<TKey, TValue>(capacity, allocator);
        }

        public static void DisposeIfCreated<T>(this ref NativeArray<T> array) where T : struct
        {
            if (array.IsCreated)
                array.Dispose();
            array = default;
        }

        public static void DisposeIfCreated<T>(this ref NativeList<T> array) where T : struct
        {
            if (array.IsCreated)
                array.Dispose();
            array = default;
        }

        public static void DisposeIfCreated<T>(this ref NativeQueue<T> array) where T : struct
        {
            if (array.IsCreated)
                array.Dispose();
            array = default;
        }

        public static void DisposeIfCreated<T>(this ref NativeHashSet<T> set) where T : unmanaged, IEquatable<T>
        {
            if (set.IsCreated)
                set.Dispose();
            set = default;
        }

        public static void DisposeIfCreated<TKey, TValue>(this ref NativeHashMap<TKey, TValue> map)
            where TKey : unmanaged, IEquatable<TKey> where TValue : struct
        {
            if (map.IsCreated)
                map.Dispose();
            map = default;
        }
    }
}