namespace Blocks
{
    public interface IActiveBlockConsumer
    {
        bool TryGetBlock(out BlockData block);
        void Consume();
    }
}