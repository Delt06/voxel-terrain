using System;

namespace Blocks
{
    public interface IBlockDestruction
    {
        event EventHandler<BlockData> DestroyedBlock;
    }
}