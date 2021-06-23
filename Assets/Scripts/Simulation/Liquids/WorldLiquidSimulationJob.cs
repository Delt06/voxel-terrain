using Chunks;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Simulation.Liquids
{
    [BurstCompile]
    public struct WorldLiquidSimulationJob : IJob
    {
        public NativeList<ChunkAndBlockPosition> ModifiedBlocks;
        public int3 ChunkSize;
        public int2 CenterChunkXZ;
        public Neighborhood<BlockData> BlocksNeighborhood;

        [NativeDisableContainerSafetyRestriction]
        public NativeHashMap<ChunkAndBlockPosition, BlockData>.ParallelWriter ResultingBlockChanges;

        private static readonly int3 Right = new int3(1, 0, 0);
        private static readonly int3 Left = new int3(-1, 0, 0);
        private static readonly int3 Up = new int3(0, 1, 0);
        private static readonly int3 Down = new int3(0, -1, 0);
        private static readonly int3 Forward = new int3(0, 0, 1);
        private static readonly int3 Back = new int3(0, 0, -1);

        public void Execute()
        {
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int index = 0, initialModifiedBlocksCount = ModifiedBlocks.Length;
                index < initialModifiedBlocksCount;
                index++)
            {
                var modifiedPosition = ModifiedBlocks[index];
                TryAddToModifiedBlocks(modifiedPosition, Right);
                TryAddToModifiedBlocks(modifiedPosition, Left);
                TryAddToModifiedBlocks(modifiedPosition, Down);
                TryAddToModifiedBlocks(modifiedPosition, Forward);
                TryAddToModifiedBlocks(modifiedPosition, Back);
                TryAddToModifiedBlocks(modifiedPosition, Up);
            }

            // ReSharper disable once ForCanBeConvertedToForeach
            for (int index = 0, blocksCount = ModifiedBlocks.Length; index < blocksCount; index++)
            {
                var modifiedPosition = ModifiedBlocks[index];
                TryProcessModifiedBlock(modifiedPosition);
            }
        }

        private void TryAddToModifiedBlocks(in ChunkAndBlockPosition position, in int3 offset)
        {
            var origin = ChunkUtils.IndexToPosition(position.BlockIndex, ChunkSize);
            if (!ChunkUtils.TryApplyOffset(position.ChunkXZ, origin, offset, ChunkSize,
                out var offsetResult
            )) return;

            var neighborXZ = offsetResult.ChunkXZ;
            var neighborLocalPosition = offsetResult.LocalPosition;
            var neighborBlockIndex = ChunkUtils.PositionToIndex(neighborLocalPosition, ChunkSize);
            var neighborChunkAndBlockPosition = new ChunkAndBlockPosition(neighborXZ, neighborBlockIndex);

            // TODO: maybe refactor to set?
            if (ModifiedBlocks.IndexOf(neighborChunkAndBlockPosition) != -1) return;

            ModifiedBlocks.Add(neighborChunkAndBlockPosition);
        }

        private void TryProcessModifiedBlock(ChunkAndBlockPosition position)
        {
            if (!BlocksNeighborhood.TryGetBuffer(CenterChunkXZ, position.ChunkXZ, out var blocks)) return;

            var blockIndex = position.BlockIndex;
            var currentBlock = blocks[blockIndex];
            if (currentBlock.Exists && !currentBlock.IsLiquid()) return;

            if (!TryGetResultingBlock(currentBlock, position, out var resultingBlock)) return;

            ResultingBlockChanges.TryAdd(position, resultingBlock);
        }

        private bool TryGetResultingBlock(in BlockData currentBlock, in ChunkAndBlockPosition position,
            out BlockData resultingBlock)
        {
            if (currentBlock.Exists && currentBlock.IsLiquid() && currentBlock.IsLiquidSource())
            {
                resultingBlock = default;
                return false;
            }

            if (TryCheckLiquidAbove(position, out resultingBlock)) return true;
            if (TryGetNeighborWithMaxLevel(position, out var maxNeighbor))
            {
                var maxNeighborLevel = maxNeighbor.GetLiquidLevel();
                var newLiquidLevel = maxNeighborLevel - maxNeighbor.GetLiquidDecay();
                if (newLiquidLevel > 0)
                {
                    resultingBlock = maxNeighbor;
                    resultingBlock.SetIsLiquidSource(false);
                    resultingBlock.SetLiquidLevel(newLiquidLevel);
                }
                else
                {
                    resultingBlock = BlockData.Empty;
                }

                return true;
            }


            resultingBlock = BlockData.Empty;
            return true;
        }

        private bool TryCheckLiquidAbove(in ChunkAndBlockPosition position, out BlockData resultingBlock)
        {
            if (!TryGetNeighborBlock(position, Up, out var blockAbove) || !blockAbove.IsLiquid())
            {
                resultingBlock = default;
                return false;
            }

            resultingBlock = blockAbove;
            resultingBlock.SetIsLiquidSource(false);
            resultingBlock.SetLiquidLevel(LiquidUtils.MaxLiquidLevel);
            return true;
        }

        private bool TryGetNeighborWithMaxLevel(in ChunkAndBlockPosition position, out BlockData maxNeighbor)
        {
            const int notFoundLevel = -1;
            var maxNeighborLevel = notFoundLevel;
            maxNeighbor = default;

            TryGetNeighborLevelAndFindMax(position, Right, ref maxNeighbor, ref maxNeighborLevel);
            TryGetNeighborLevelAndFindMax(position, Left, ref maxNeighbor, ref maxNeighborLevel);
            TryGetNeighborLevelAndFindMax(position, Forward, ref maxNeighbor, ref maxNeighborLevel);
            TryGetNeighborLevelAndFindMax(position, Back, ref maxNeighbor, ref maxNeighborLevel);

            return maxNeighborLevel != notFoundLevel;
        }

        private void TryGetNeighborLevelAndFindMax(in ChunkAndBlockPosition position, in int3 offset,
            ref BlockData maxNeighbor,
            ref int maxLiquidLevel)
        {
            if (!TryGetNeighborBlock(position, offset, out var neighborBlock)) return;
            if (!neighborBlock.IsLiquid()) return;

            if (!TryGetNeighborBlock(position, offset + Down, out var blockBelow)) return;
            if (!blockBelow.Exists || blockBelow.IsLiquid()) return;

            var neighborLiquidLevel = neighborBlock.GetLiquidLevel();
            if (neighborLiquidLevel <= maxLiquidLevel) return;

            maxLiquidLevel = neighborLiquidLevel;
            maxNeighbor = neighborBlock;
        }

        private bool TryGetNeighborBlock(in ChunkAndBlockPosition position, in int3 offset, out BlockData block)
        {
            block = default;
            var localPosition = ChunkUtils.IndexToPosition(position.BlockIndex, ChunkSize);
            if (!ChunkUtils.TryApplyOffset(position.ChunkXZ, localPosition, offset, ChunkSize, out var offsetResults))
                return false;

            var neighborXZ = offsetResults.ChunkXZ;
            if (!BlocksNeighborhood.TryGetBuffer(CenterChunkXZ, neighborXZ, out var neighborBlocks))
                return false;

            var neighborBlockIndex = ChunkUtils.PositionToIndex(offsetResults.LocalPosition, ChunkSize);
            block = neighborBlocks[neighborBlockIndex];
            return true;
        }
    }
}