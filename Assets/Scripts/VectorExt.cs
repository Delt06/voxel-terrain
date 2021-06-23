using Unity.Mathematics;
using UnityEngine;

public static class VectorExt
{
    public static int3 FloorToInt(this Vector3 vector) => FloorToInt((float3) vector);

    public static int3 FloorToInt(this float3 vector) => (int3) math.floor(vector);
}