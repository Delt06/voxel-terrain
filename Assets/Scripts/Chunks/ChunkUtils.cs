using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace Chunks
{
    [BurstCompile]
    public static class ChunkUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 IndexToPosition(int index, in int3 chunkSize)
        {
            var area = chunkSize.x * chunkSize.y;
            var z = index / area;
            var x = index % area % chunkSize.x;
            var y = index % area / chunkSize.x;
            return new int3(x, y, z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int PositionToIndex(in int3 position, in int3 chunkSize) =>
            position.x +
            position.y * chunkSize.x +
            position.z * chunkSize.x * chunkSize.y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool OutOfBounds(in int3 position, in int3 chunkSize) =>
            position.x < 0 || position.y < 0 || position.z < 0 ||
            position.x >= chunkSize.x || position.y >= chunkSize.y || position.z >= chunkSize.z;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 ClampToBounds(in int3 position, in int3 chunkSize)
        {
            var one = new int3(1, 1, 1);
            return math.clamp(position, int3.zero, chunkSize - one);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 GetNormal(this Side side)
        {
            return side switch
            {
                Side.North => NorthNormal,
                Side.South => SouthNormal,
                Side.East => EastNormal,
                Side.West => WestNormal,
                Side.Up => UpNormal,
                Side.Down => DownNormal,
                _ => throw new ArgumentOutOfRangeException(nameof(side)),
            };
        }

        public struct OffsetResult
        {
            public int2 ChunkXZ;
            public int3 LocalPosition;

            public OffsetResult(int2 chunkXZ, int3 localPosition)
            {
                ChunkXZ = chunkXZ;
                LocalPosition = localPosition;
            }
        }

        public static bool TryApplyOffset(int2 chunkXZ, in int3 localPosition, in int3 offset, in int3 chunkSize,
            out OffsetResult offsetResult)
        {
            var neighborXZ = chunkXZ;
            var neighborLocalPosition = localPosition + offset;
            if (neighborLocalPosition.y < 0 || neighborLocalPosition.y >= chunkSize.y)
            {
                offsetResult = default;
                return false;
            }

            if (neighborLocalPosition.x < 0)
            {
                neighborXZ.x--;
                neighborLocalPosition.x += chunkSize.x;
            }

            if (neighborLocalPosition.x >= chunkSize.x)
            {
                neighborXZ.x++;
                neighborLocalPosition.x -= chunkSize.x;
            }

            if (neighborLocalPosition.z < 0)
            {
                neighborXZ.y--;
                neighborLocalPosition.z += chunkSize.z;
            }

            if (neighborLocalPosition.z >= chunkSize.z)
            {
                neighborXZ.y++;
                neighborLocalPosition.z -= chunkSize.z;
            }

            offsetResult = new OffsetResult(neighborXZ, neighborLocalPosition);
            return true;
        }

        private static readonly float3 NorthNormal = (Vector3) Side.North.ToVector();
        private static readonly float3 SouthNormal = (Vector3) Side.South.ToVector();
        private static readonly float3 WestNormal = (Vector3) Side.West.ToVector();
        private static readonly float3 EastNormal = (Vector3) Side.East.ToVector();
        private static readonly float3 UpNormal = (Vector3) Side.Up.ToVector();
        private static readonly float3 DownNormal = (Vector3) Side.Down.ToVector();
    }
}