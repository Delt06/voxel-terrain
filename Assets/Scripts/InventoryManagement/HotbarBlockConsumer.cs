using Blocks;
using UnityEngine;

namespace InventoryManagement
{
    public sealed class HotbarBlockConsumer : MonoBehaviour, IActiveBlockConsumer
    {
        public void Construct(IHotbar hotbar)
        {
            _hotbar = hotbar;
        }

        public bool TryGetBlock(out BlockData block)
        {
            var activeCell = _hotbar.GetActiveCell();
            block = default;
            if (activeCell.IsEmpty) return false;
            if (!(activeCell.Item is BlockConfig blockConfig)) return false;
            block = blockConfig;
            return true;
        }

        public void Consume()
        {
            var activeCell = _hotbar.GetActiveCell();
            if (activeCell.IsEmpty) return;

            activeCell.Count--;
            _hotbar.SetActiveCell(activeCell);
        }

        private IHotbar _hotbar;
    }
}