Shader "Voxel Terrain/Skybox"
{
    Properties
    {
        _SunColor ("Sun Color", Color) = (1, 1, 1, 1)
        _SunSize ("Sun Size", Range(0, 1)) = 0.1
        _SunSmoothness ("Sun Smoothnesss", Range(0, 1)) = 0.1
        _MoonColor ("Moon Color", Color) = (1, 1, 1, 1)
        _MoonSize ("Moon Size", Range(0, 1)) = 0.1
        _MoonSmoothness ("Moon Smoothnesss", Range(0, 1)) = 0.1
    }
    SubShader
    {
        Tags {"RenderType" = "Opaque"}
        LOD 100
        
        Cull Back
        Blend One Zero
        ZTest LEqual
        ZWrite On

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            half3 _SkyColor = half3(1, 1, 1);
            half _NormalizedTimeOfDay;

            half4 _SunColor;
            half _SunSize;
            half _SunSmoothness;
            
            half4 _MoonColor;
            half _MoonSize;
            half _MoonSmoothness;

            CBUFFER_START(UnityPerMaterial)
            CBUFFER_END

            struct appdata
            {
                float4 vertex : POSITION;
                half3 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                half3 uv : TEXCOORD0;
                float4 vertex_cs : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                const VertexPositionInputs vertex_position_inputs = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex_cs = vertex_position_inputs.positionCS;
                o.uv = v.uv;
                
                return o;
            }

            inline half3 get_sun_position(const half time)
            {
                const half angle = time * 2 * PI;
                const half3 sun_position = half3(cos(angle), sin(angle), 0);
                return sun_position;
            }

            inline half get_sun_value(const in float3 uv, const in float3 sun_position, const half size, const half smoothness)
            {
                half sun_value = 1 - saturate(length(uv - sun_position));
                const half edge0 = 1 - size;
                sun_value = smoothstep(edge0, edge0 + smoothness, sun_value);
                return sun_value;
            }

            float3 frag (v2f i) : SV_Target
            {
                const half3 sun_position = get_sun_position(_NormalizedTimeOfDay);
                const half sun_value = get_sun_value(i.uv, sun_position, _SunSize, _SunSmoothness);
                const half3 moon_position = get_sun_position(_NormalizedTimeOfDay - 0.5);
                const half moon_value = get_sun_value(i.uv, moon_position, _MoonSize, _MoonSmoothness);
                return sun_value * _SunColor.xyz * _SunColor.a +
                       moon_value * _MoonColor.xyz * _MoonColor.a +
                       _SkyColor;
            }
            ENDHLSL
        }
    }
}
