Shader "Voxel Terrain/Water Blit"
{
    Properties
    {
        _MainTex ("Source", 2D) = "white" {}
        _Tint ("Tint", Color) = (0, 0.5, 1, 0.5)
    }
    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType"="Transparent"  "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZTest LEqual
        ZWrite Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Tint;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex_cs : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                const VertexPositionInputs vertex_position_inputs = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex_cs = vertex_position_inputs.positionCS;
                o.uv = v.uv;

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                color *= _Tint;
                return color;
            }


            ENDHLSL
        }
    }
}
