using System;
using Chunks.Lighting;
using Simulation.Liquids;
using UnityEngine;

namespace Blocks
{
    [CreateAssetMenu]
    public sealed class BlockConfig : ItemConfig
    {
        [Header("Mesh"), SerializeField, Min(0)]
        private int _meshIndex = 0;
        [SerializeField, Min(0)]
        private int _subMeshIndex = 0;

        [Header("Metadata"), SerializeField]
        private BlockFlags _flags = default;
        [SerializeField, Range(0, LightingUtils.MaxLightValue)]
        private byte _emission = 0;

        [SerializeField, Range(1, LiquidUtils.MaxLiquidDecay)]
        private int _liquidDecay = 1;

        private bool IsLiquid => _flags.Include(BlockFlags.Liquid);

        #region Sprite Fields

        [Header("Textures"), SerializeField]
        private bool _singleSprite = true;

        [SerializeField]
        private Sprite _sprite = default;

        [SerializeField]
        private Sprite _northSprite = default;

        [SerializeField]
        private Sprite _southSprite = default;

        [SerializeField]
        private Sprite _westSprite = default;

        [SerializeField]
        private Sprite _eastSprite = default;

        [SerializeField]
        private Sprite _topSprite = default;

        [SerializeField]
        private Sprite _bottomSprite = default;

        #endregion

        public override Sprite MainSprite => GetSprite(Side.South);

        public Sprite GetSprite(Side side)
        {
            if (_singleSprite) return _sprite;

            return side switch
            {
                Side.North => _northSprite,
                Side.South => _southSprite,
                Side.East => _eastSprite,
                Side.West => _westSprite,
                Side.Up => _topSprite,
                Side.Down => _bottomSprite,
                _ => throw new ArgumentException($"Invalid side: {side}."),
            };
        }

        public static implicit operator BlockData(BlockConfig blockConfig)
        {
            var blockData = new BlockData(blockConfig.ID, blockConfig._meshIndex, blockConfig._subMeshIndex,
                blockConfig._emission,
                flags: blockConfig._flags
            );

            if (blockConfig.IsLiquid)
            {
                blockData.SetLiquidLevel(LiquidUtils.MaxLiquidLevel);
                blockData.SetLiquidDecay(blockConfig._liquidDecay);
                blockData.SetIsLiquidSource(true);
            }

            return blockData;
        }
    }
}