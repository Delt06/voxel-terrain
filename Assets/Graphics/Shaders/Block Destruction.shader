Shader "Voxel Terrain/Block Destruction"
{
    Properties
    {
        [MainTexture] [NoScaleOffset] _BaseMap ("Texture", 2D) = "white" {}
        _AlphaClipThreshold("Alpha Clip Threshold", Range(0.0, 1.0)) = 0.9
        _Stages ("Stages", Int) = 10
        _Progress("Progress", Range(0.0, 1.0)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100
        
        Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            half _AlphaClipThreshold;
            half _Stages;
            float _Progress;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 tiled_uv = i.uv;
                tiled_uv.x += floor((_Stages - 1) * _Progress);
                tiled_uv.x /= _Stages;
                fixed4 color = tex2D(_BaseMap, tiled_uv);
                clip(color.a - _AlphaClipThreshold);
                return color;
            }
            ENDCG
        }
    }
}
