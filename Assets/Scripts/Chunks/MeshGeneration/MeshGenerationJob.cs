using System;
using Blocks;
using Simulation.Liquids;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Chunks.MeshGeneration
{
    [BurstCompile]
    public struct MeshGenerationJob : IJob
    {
        [ReadOnly] public NativeArray<BlockData> Blocks;
        [ReadOnly] public int3 ChunkSize;
        [ReadOnly] public NativeArray<BlockUv> UVs;
        [ReadOnly] public int SubMeshTriangleCount;
        [ReadOnly] public byte MeshIndex;

        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<float3> VertexBuffer;

        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<int> TriangleBuffer;

        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<float2> UvBuffer;

        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<float3> NormalBuffer;

        [NativeDisableParallelForRestriction] public NativeArray<int> VertexCounts;
        [NativeDisableParallelForRestriction] public NativeArray<int> TriangleCounts;
        [NativeDisableParallelForRestriction] public NativeArray<bool> Checked;

        public void Execute()
        {
            for (var i = 0; i < VertexCounts.Length; i++)
            {
                VertexCounts[i] = 0;
            }

            for (var i = 0; i < TriangleCounts.Length; i++)
            {
                TriangleCounts[i] = 0;
            }

            for (var index = 0; index < Blocks.Length; index++)
            {
                Checked[index] = false;
            }

            for (var index = 0; index < Blocks.Length; index++)
            {
                if (Checked[index]) continue;

                var block = Blocks[index];
                if (!block.Exists) continue;

                if (block.MeshIndex != MeshIndex) continue;

                var min = IndexToPosition(index);
                var max = min;

                if (!block.IsTransparent()) Expand(ref min, ref max, block);

                MarkChecked(min, max);
                DrawCubeAt(block, min, max - min + 1);
            }
        }

        private int3 IndexToPosition(int index) => ChunkUtils.IndexToPosition(index, ChunkSize);

        private int PositionToIndex(int3 position) => ChunkUtils.PositionToIndex(position, ChunkSize);

        private void Expand(ref int3 min, ref int3 max, in BlockData block)
        {
            while (TryExpandRight(ref min, ref max, block) ||
                   TryExpandLeft(ref min, ref max, block) ||
                   TryExpandForward(ref min, ref max, block) ||
                   TryExpandBack(ref min, ref max, block)) { }

            while (TryExpandUp(ref min, ref max, block) ||
                   TryExpandDown(ref min, ref max, block)) { }
        }

        private bool TryExpandRight(ref int3 min, ref int3 max, in BlockData block)
        {
            if (max.x >= ChunkSize.x - 1) return false;

            for (var y = min.y; y <= max.y; y++)
            {
                for (var z = min.z; z <= max.z; z++)
                {
                    var position = new int3(max.x + 1, y, z);
                    var index = PositionToIndex(position);
                    if (Checked[index]) return false;
                    if (Blocks[index].ID != block.ID) return false;
                }
            }

            max.x += 1;
            return true;
        }

        private bool TryExpandLeft(ref int3 min, ref int3 max, in BlockData block)
        {
            if (min.x <= 0) return false;

            for (var y = min.y; y <= max.y; y++)
            {
                for (var z = min.z; z <= max.z; z++)
                {
                    var position = new int3(min.x - 1, y, z);
                    var index = PositionToIndex(position);
                    if (Checked[index]) return false;
                    if (Blocks[index].ID != block.ID) return false;
                }
            }

            min.x -= 1;
            return true;
        }

        private bool TryExpandForward(ref int3 min, ref int3 max, in BlockData block)
        {
            if (max.z >= ChunkSize.z - 1) return false;

            for (var y = min.y; y <= max.y; y++)
            {
                for (var x = min.x; x <= max.x; x++)
                {
                    var position = new int3(x, y, max.z + 1);
                    var index = PositionToIndex(position);
                    if (Checked[index]) return false;
                    if (Blocks[index].ID != block.ID) return false;
                }
            }

            max.z += 1;
            return true;
        }

        private bool TryExpandBack(ref int3 min, ref int3 max, in BlockData block)
        {
            if (min.z <= 0) return false;

            for (var y = min.y; y <= max.y; y++)
            {
                for (var x = min.x; x <= max.x; x++)
                {
                    var position = new int3(x, y, min.z - 1);
                    var index = PositionToIndex(position);
                    if (Checked[index]) return false;
                    if (Blocks[index].ID != block.ID) return false;
                }
            }

            min.z -= 1;
            return true;
        }

        private bool TryExpandUp(ref int3 min, ref int3 max, in BlockData block)
        {
            if (max.y >= ChunkSize.y - 1) return false;

            for (var z = min.z; z <= max.z; z++)
            {
                for (var x = min.x; x <= max.x; x++)
                {
                    var position = new int3(x, max.y + 1, z);
                    var index = PositionToIndex(position);
                    if (Checked[index]) return false;
                    if (Blocks[index].ID != block.ID) return false;
                }
            }

            max.y += 1;
            return true;
        }

        private bool TryExpandDown(ref int3 min, ref int3 max, in BlockData block)
        {
            if (min.y <= 0) return false;

            for (var z = min.z; z <= max.z; z++)
            {
                for (var x = min.x; x <= max.x; x++)
                {
                    var position = new int3(x, min.y - 1, z);
                    var index = PositionToIndex(position);
                    if (Checked[index]) return false;

                    var otherBlock = Blocks[index];
                    if (otherBlock.ID != block.ID) return false;
                }
            }

            min.y -= 1;
            return true;
        }


        private void MarkChecked(int3 min, int3 max)
        {
            for (var x = min.x; x <= max.x; x++)
            {
                for (var y = min.y; y <= max.y; y++)
                {
                    for (var z = min.z; z <= max.z; z++)
                    {
                        var position = new int3(x, y, z);
                        var index = PositionToIndex(position);
                        Checked[index] = true;
                    }
                }
            }
        }

        private void DrawCubeAt(BlockData blockData, int3 position, int3 size)
        {
            DrawSideIfFree(blockData, position, size, Side.North);
            DrawSideIfFree(blockData, position, size, Side.South);
            DrawSideIfFree(blockData, position, size, Side.East);
            DrawSideIfFree(blockData, position, size, Side.West);
            DrawSideIfFree(blockData, position, size, Side.Up);
            DrawSideIfFree(blockData, position, size, Side.Down);
        }

        private void DrawSideIfFree(BlockData blockData, int3 position, int3 size, Side side)
        {
            GetNeighborMinMax(position, size, side, out var min, out var max);

            var skipOcclusionCheck = blockData.IsLiquid() && side == Side.Up;
            if (!skipOcclusionCheck && IsOccluded(blockData, min, max)) return;

            float3 sizeAsFloat = size;

            if (blockData.IsLiquid())
            {
                var relativeLiquidLevel = (float) blockData.GetLiquidLevel() / LiquidUtils.MaxLiquidLevel;
                sizeAsFloat.y *= relativeLiquidLevel;
            }

            DrawSide(blockData, side, sizeAsFloat, position);
        }

        private bool IsOccluded(in BlockData blockData, int3 min, int3 max)
        {
            for (var x = min.x; x <= max.x; x++)
            {
                for (var y = min.y; y <= max.y; y++)
                {
                    for (var z = min.z; z <= max.z; z++)
                    {
                        var neighbor = new int3(x, y, z);

                        if (TryGetBlockAt(neighbor, out var neighborBlock))
                        {
                            if (blockData.IsLiquid())
                            {
                                if (IsNotOccludedAsLiquid(blockData, neighborBlock))
                                    return false;
                            }
                            else
                            {
                                if (IsNotOccluded(neighborBlock))
                                    return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static bool IsNotOccluded(in BlockData neighborBlock) => neighborBlock.IsTransparent();

        private static bool IsNotOccludedAsLiquid(in BlockData blockData, in BlockData neighborBlock)
        {
            if (!neighborBlock.IsTransparent()) return false;
            if (!neighborBlock.Equals(blockData)) return true;
            return neighborBlock.Metadata != blockData.Metadata;
        }

        private static void GetNeighborMinMax(int3 origin, int3 size, Side side, out int3 min, out int3 max)
        {
            var vector = ToVector(side);
            var regionMin = origin;
            var regionMax = origin + size - 1;

            if (IsNegative(side))
            {
                min = regionMin + vector;
                max = regionMax + size * vector;
            }
            else
            {
                min = regionMin + size * vector;
                max = regionMax + vector;
            }
        }

        private static bool IsNegative(Side side) => side switch
        {
            Side.North => false,
            Side.South => true,
            Side.East => false,
            Side.West => true,
            Side.Up => false,
            Side.Down => true,
            _ => throw new ArgumentOutOfRangeException(nameof(side)),
        };

        private static int3 ToVector(Side side) => side switch
        {
            Side.North => new int3(0, 0, 1),
            Side.South => new int3(0, 0, -1),
            Side.East => new int3(1, 0, 0),
            Side.West => new int3(-1, 0, 0),
            Side.Up => new int3(0, 1, 0),
            Side.Down => new int3(0, -1, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(side)),
        };

        private bool TryGetBlockAt(int3 position, out BlockData block)
        {
            block = BlockData.Empty;
            if (position.x < 0 || position.x >= ChunkSize.x) return false;
            if (position.y < 0 || position.y >= ChunkSize.y) return false;
            if (position.z < 0 || position.z >= ChunkSize.z) return false;
            block = Blocks[PositionToIndex(position)];
            return block.Exists;
        }

        private void DrawSide(BlockData blockData, Side side, float3 size, float3 origin)
        {
            GetVertices(side, out var v1, out var v2, out var v3, out var v4);
            var normal = side.GetNormal();
            var uv = UVs[blockData.ID].GetAt(side).Min;
            DrawQuad(v1, v2, v3, v4, size, normal, uv, origin, blockData.SubMeshIndex);
        }

        private static void GetVertices(Side side, out float3 v1, out float3 v2, out float3 v3, out float3 v4)
        {
            switch (side)
            {
                case Side.North:
                    v1 = BottomRightFar;
                    v2 = TopRightFar;
                    v3 = TopLeftFar;
                    v4 = BottomLeftFar;
                    return;

                case Side.South:
                    v1 = BottomLeftNear;
                    v2 = TopLeftNear;
                    v3 = TopRightNear;
                    v4 = BottomRightNear;
                    return;

                case Side.East:
                    v1 = BottomRightNear;
                    v2 = TopRightNear;
                    v3 = TopRightFar;
                    v4 = BottomRightFar;
                    return;

                case Side.West:
                    v1 = BottomLeftFar;
                    v2 = TopLeftFar;
                    v3 = TopLeftNear;
                    v4 = BottomLeftNear;
                    return;

                case Side.Up:
                    v1 = TopLeftNear;
                    v2 = TopLeftFar;
                    v3 = TopRightFar;
                    v4 = TopRightNear;
                    return;

                case Side.Down:
                    v1 = BottomLeftFar;
                    v2 = BottomLeftNear;
                    v3 = BottomRightNear;
                    v4 = BottomRightFar;
                    return;

                default:
                    v1 = float3.zero;
                    v2 = float3.zero;
                    v3 = float3.zero;
                    v4 = float3.zero;
                    return;
            }
        }


        // Left is x=0, Right is x=1
        // Bottom is y=0, Top is y=1
        // Near is z=0, Far is z=1
        private static readonly float3 BottomLeftNear = new float3(0, 0, 0);
        private static readonly float3 BottomRightNear = new float3(1, 0, 0);
        private static readonly float3 BottomLeftFar = new float3(0, 0, 1);
        private static readonly float3 BottomRightFar = new float3(1, 0, 1);
        private static readonly float3 TopLeftNear = new float3(0, 1, 0);
        private static readonly float3 TopRightNear = new float3(1, 1, 0);
        private static readonly float3 TopLeftFar = new float3(0, 1, 1);
        private static readonly float3 TopRightFar = new float3(1, 1, 1);

        private void DrawQuad(float3 v1, float3 v2, float3 v3, float3 v4, float3 size, float3 normal, in Vector2 uv,
            float3 offset, int subMeshIndex)
        {
            var vertexIndexOffset = VertexCounts[0];
            VertexBuffer[vertexIndexOffset + 0] = v1 * size + offset;
            VertexBuffer[vertexIndexOffset + 1] = v2 * size + offset;
            VertexBuffer[vertexIndexOffset + 2] = v3 * size + offset;
            VertexBuffer[vertexIndexOffset + 3] = v4 * size + offset;

            UvBuffer[vertexIndexOffset + 0] = uv;
            UvBuffer[vertexIndexOffset + 1] = uv;
            UvBuffer[vertexIndexOffset + 2] = uv;
            UvBuffer[vertexIndexOffset + 3] = uv;

            NormalBuffer[vertexIndexOffset + 0] = normal;
            NormalBuffer[vertexIndexOffset + 1] = normal;
            NormalBuffer[vertexIndexOffset + 2] = normal;
            NormalBuffer[vertexIndexOffset + 3] = normal;

            VertexCounts[0] += 4;

            AddTriangle(0, 1, 2, vertexIndexOffset, subMeshIndex);
            AddTriangle(0, 2, 3, vertexIndexOffset, subMeshIndex);
        }

        private void AddTriangle(int index0, int index1, int index2, int offset, int subMeshIndex)
        {
            var trianglesIndexOffset = SubMeshTriangleCount * subMeshIndex + TriangleCounts[subMeshIndex];
            TriangleBuffer[trianglesIndexOffset + 0] = offset + index0;
            TriangleBuffer[trianglesIndexOffset + 1] = offset + index1;
            TriangleBuffer[trianglesIndexOffset + 2] = offset + index2;
            TriangleCounts[subMeshIndex] += 3;
        }
    }
}