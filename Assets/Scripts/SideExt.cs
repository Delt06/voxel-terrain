using System;
using UnityEngine;

public static class SideExt
{
    public static Vector3Int ToVector(this Side side)
    {
        return side switch
        {
            Side.North => Vector3Int.forward,
            Side.South => Vector3Int.back,
            Side.East => Vector3Int.right,
            Side.West => Vector3Int.left,
            Side.Up => Vector3Int.up,
            Side.Down => Vector3Int.down,
            _ => throw new ArgumentOutOfRangeException(nameof(side)),
        };
    }
}