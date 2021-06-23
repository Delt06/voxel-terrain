namespace Blocks
{
    public interface IBlockResolver
    {
        bool TryGetConfig(BlockData blockData, out BlockConfig blockConfig);
    }
}