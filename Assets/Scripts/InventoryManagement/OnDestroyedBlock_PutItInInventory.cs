using System;
using Blocks;
using UnityEngine;

namespace InventoryManagement
{
    public sealed class OnDestroyedBlock_PutItInInventory : MonoBehaviour
    {
        public void Construct(IBlockDestruction blockDestruction, IInventory inventory, IBlockResolver blockResolver)
        {
            _blockDestruction = blockDestruction;
            _inventory = inventory;
            _blockResolver = blockResolver;
        }

        private void OnEnable()
        {
            _blockDestruction.DestroyedBlock += _onDestroyed;
        }

        private void OnDisable()
        {
            _blockDestruction.DestroyedBlock -= _onDestroyed;
        }

        private void Awake()
        {
            _onDestroyed = (sender, block) =>
            {
                if (!_blockResolver.TryGetConfig(block, out var config))
                {
                    Debug.LogWarning($"Block with ID={block.ID} was not resolved.");
                    return;
                }

                _inventory.TryCollect(config, 1);
            };
        }

        private EventHandler<BlockData> _onDestroyed;
        private IBlockDestruction _blockDestruction;
        private IInventory _inventory;
        private IBlockResolver _blockResolver;
    }
}