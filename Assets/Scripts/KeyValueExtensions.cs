using System;
using Unity.Collections.LowLevel.Unsafe;

public static class KeyValueExtensions
{
    public static void Deconstruct<TKey, TValue>(in this KeyValue<TKey, TValue> keyValue, out TKey key,
        out TValue value)
        where TKey : struct, IEquatable<TKey> where TValue : struct
    {
        key = keyValue.Key;
        value = keyValue.Value;
    }
}