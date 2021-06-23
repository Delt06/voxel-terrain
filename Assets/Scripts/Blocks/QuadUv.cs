using Unity.Mathematics;
using UnityEngine;

namespace Blocks
{
    public readonly struct QuadUv
    {
        public readonly float2 Min;
        public readonly float2 Max;

        public QuadUv(float2 min, float2 max)
        {
            Min = min;
            Max = max;
        }

        public static QuadUv FromSprite(Sprite sprite)
        {
            var texture = sprite.texture;
            var rect = sprite.rect;
            var size = new float2(texture.width, texture.height);
            var uvMin = (float2) rect.min / size;
            var uvMax = (float2) rect.max / size;
            return new QuadUv(uvMin, uvMax);
        }
    }
}