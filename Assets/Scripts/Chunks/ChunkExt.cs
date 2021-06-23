using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

namespace Chunks
{
    public static class ChunkExt
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsBusyAt(this Chunk chunk, int3 localPosition) =>
            chunk.IsBusyAt(localPosition.x, localPosition.y, localPosition.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBlockAt(this Chunk chunk, int3 localPosition, BlockData block)
        {
            chunk.SetBlockAt(localPosition.x, localPosition.y, localPosition.z, block);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BlockData GetBlockAt(this Chunk chunk, int3 localPosition) =>
            chunk.GetBlockAt(localPosition.x, localPosition.y, localPosition.z);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 GetBlockWorldCenter(this Chunk chunk, Vector3Int localPosition)
        {
            var worldPosition = chunk.Origin + localPosition;
            return worldPosition + Vector3.one * 0.5f;
        }
    }
}