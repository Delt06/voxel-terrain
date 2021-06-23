using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Chunks.TerrainGeneration
{
    [BurstCompile]
    public struct TerrainGenerationJob : IJobParallelFor
    {
        [ReadOnly] public int BatchCount;
        [ReadOnly] public int3 ChunkSize;

        [ReadOnly] public float Scale;
        [ReadOnly] public float MinTerrainHeight;
        [ReadOnly] public float MaxTerrainHeight;

        [ReadOnly] public float StoneScale;
        [ReadOnly] public float MinStoneHeight;
        [ReadOnly] public float MaxStoneHeight;

        [ReadOnly] public float RelativeWaterLevel;

        [ReadOnly] public BlockData Grass;
        [ReadOnly] public BlockData Dirt;
        [ReadOnly] public BlockData Stone;
        [ReadOnly] public BlockData WaterSource;

        [ReadOnly] public float3 Origin;

        [WriteOnly, NativeDisableParallelForRestriction]
        public NativeArray<BlockData> Blocks;

        public void Execute(int batchIndex)
        {
            var batchSize = ChunkSize.x * ChunkSize.z / BatchCount;
            var from = batchIndex * batchSize;
            var to = (batchIndex + 1) * batchSize;
            var waterLevel = (int) math.round(RelativeWaterLevel * ChunkSize.y);

            for (var blockIndex = from; blockIndex < to; blockIndex++)
            {
                var x = blockIndex % ChunkSize.x;
                var z = blockIndex / ChunkSize.x;

                var noiseValue = GetNoiseAt(x, z, Scale);
                var maxY = LerpRounded(MinTerrainHeight, MaxTerrainHeight, noiseValue);
                maxY = math.min(maxY, ChunkSize.y);

                var stoneNoiseValue = GetNoiseAt(x, z, StoneScale);
                var stoneMaxY = LerpRounded(MinStoneHeight, MaxStoneHeight, stoneNoiseValue);
                stoneMaxY = math.min(stoneMaxY, maxY);

                var y = 0;
                for (; y <= maxY; y++)
                {
                    var blockData = GetBlockData(y, maxY, stoneMaxY);
                    Blocks[ChunkUtils.PositionToIndex(new int3(x, y, z), ChunkSize)] = blockData;
                }

                for (; y < ChunkSize.y; y++)
                {
                    var block = BlockData.Empty;
                    if (y <= waterLevel)
                        block = WaterSource;
                    Blocks[ChunkUtils.PositionToIndex(new int3(x, y, z), ChunkSize)] = block;
                }
            }
        }

        private static int LerpRounded(float min, float max, float t) => (int) math.ceil(math.lerp(min, max, t));

        private BlockData GetBlockData(int y, int maxY, int stoneMaxY)
        {
            if (y <= stoneMaxY)
                return Stone;

            if (y < maxY)
                return Dirt;
            return Grass;
        }

        private float GetNoiseAt(float localX, float localZ, float scale)
        {
            var x = (localX + Origin.x) * scale;
            var z = (localZ + Origin.z) * scale;
            return noise.cnoise(new float2(x, z));
        }
    }
}