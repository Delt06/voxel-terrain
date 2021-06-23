using System.Collections.Generic;
using UnityEngine;

namespace Blocks
{
    public sealed class BlockResolver : MonoBehaviour, IBlockResolver
    {
        public void Construct(IBlockDataProvider blockDataProvider)
        {
            _blockDataProvider = blockDataProvider;
        }

        public bool TryGetConfig(BlockData blockData, out BlockConfig blockConfig)
        {
            EnsureInitialized();
            return _blocksById.TryGetValue(blockData.ID, out blockConfig);
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;

            _initialized = true;

            foreach (var block in _blockDataProvider.Blocks)
            {
                _blocksById[block.ID] = block;
            }
        }

        private bool _initialized;
        private IBlockDataProvider _blockDataProvider;
        private readonly IDictionary<int, BlockConfig> _blocksById = new Dictionary<int, BlockConfig>();
    }
}