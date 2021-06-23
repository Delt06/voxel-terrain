using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace Blocks
{
    public class ResourcesBlockDataProvider : MonoBehaviour, IBlockDataProvider
    {
        [SerializeField] private string _path = "Blocks/";

        public IReadOnlyList<BlockConfig> Blocks
        {
            get
            {
                EnsureInitialized();
                return _blocks;
            }
        }

        public NativeArray<BlockUv> UVs
        {
            get
            {
                EnsureInitialized();
                return _uvs;
            }
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;

            _initialized = true;
            FindAllBlocks();
            CreateUvs();
        }

        private void FindAllBlocks()
        {
            _blocks = Resources.LoadAll<BlockConfig>(_path);
            Array.Sort(_blocks, (b1, b2) => b1.ID.CompareTo(b2.ID));
        }

        private void CreateUvs()
        {
            _uvs = new NativeArray<BlockUv>(_blocks.Length, Allocator.Persistent);

            for (var blockIndex = 0; blockIndex < _blocks.Length; blockIndex++)
            {
                _uvs[blockIndex] = BlockUv.FromConfig(_blocks[blockIndex]);
            }
        }

        private void OnDestroy()
        {
            if (_uvs.IsCreated)
                _uvs.Dispose();
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private bool _initialized;
        private BlockConfig[] _blocks;
        private NativeArray<BlockUv> _uvs;
    }
}