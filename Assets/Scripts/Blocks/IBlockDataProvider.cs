using System.Collections.Generic;
using Unity.Collections;

namespace Blocks
{
    public interface IBlockDataProvider
    {
        NativeArray<BlockUv> UVs { get; }
        IReadOnlyList<BlockConfig> Blocks { get; }
    }
}