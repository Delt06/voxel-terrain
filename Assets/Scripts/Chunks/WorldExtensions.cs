using Unity.Mathematics;

namespace Chunks
{
    public static class WorldExtensions
    {
        public static void RequestLocksInNeighborhood<T>(this World world, Neighborhood<T> neighborhood, int2 centerXZ,
            object source) where T : struct
        {
            for (var xi = -1; xi <= 1; xi++)
            {
                for (var zi = -1; zi <= 1; zi++)
                {
                    var offset = new int2(xi, zi);
                    var neighborXZ = centerXZ + offset;
                    if (!neighborhood.TryGetBuffer(centerXZ, neighborXZ, out _)) continue;
                    if (!world.TryGetChunkAt(neighborXZ, out var chunk)) continue;

                    chunk.RequestLock(source);
                }
            }
        }

        public static void ReleaseLocksInNeighborhood<T>(this World world, Neighborhood<T> neighborhood, int2 centerXZ,
            object source) where T : struct
        {
            for (var xi = -1; xi <= 1; xi++)
            {
                for (var zi = -1; zi <= 1; zi++)
                {
                    var offset = new int2(xi, zi);
                    var neighborXZ = centerXZ + offset;
                    if (!neighborhood.TryGetBuffer(centerXZ, neighborXZ, out _)) continue;
                    if (!world.TryGetChunkAt(neighborXZ, out var chunk)) continue;

                    chunk.ReleaseLock(source);
                }
            }
        }
    }
}