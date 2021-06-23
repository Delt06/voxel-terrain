using System;

namespace Blocks
{
    public readonly struct BlockUv
    {
        public readonly QuadUv North;
        public readonly QuadUv South;

        public readonly QuadUv West;
        public readonly QuadUv East;

        public readonly QuadUv Top;
        public readonly QuadUv Bottom;

        public BlockUv(QuadUv north, QuadUv south, QuadUv west, QuadUv east, QuadUv top, QuadUv bottom)
        {
            North = north;
            South = south;
            West = west;
            East = east;
            Top = top;
            Bottom = bottom;
        }

        public QuadUv GetAt(Side side) =>
            side switch
            {
                Side.North => North,
                Side.South => South,
                Side.East => East,
                Side.West => West,
                Side.Up => Top,
                Side.Down => Bottom,
                _ => throw new ArgumentException($"Invalid side: {side}."),
            };

        public static BlockUv FromConfig(BlockConfig blockConfig) =>
            new BlockUv(
                QuadUv.FromSprite(blockConfig.GetSprite(Side.North)),
                QuadUv.FromSprite(blockConfig.GetSprite(Side.South)),
                QuadUv.FromSprite(blockConfig.GetSprite(Side.West)),
                QuadUv.FromSprite(blockConfig.GetSprite(Side.East)),
                QuadUv.FromSprite(blockConfig.GetSprite(Side.Up)),
                QuadUv.FromSprite(blockConfig.GetSprite(Side.Down))
            );
    }
}