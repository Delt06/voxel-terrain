using System;

[Flags]
public enum BlockFlags : byte
{
    Transparent = 1 << 1,
    Liquid = 1 << 2,
    CanPlaceOver = 1 << 3,
}