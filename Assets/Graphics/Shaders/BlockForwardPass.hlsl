#ifndef BLOCK_FORWARD_PASS_INCLUDED
#define BLOCK_FORWARD_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex_os : TEXCOORD1;
    float4 vertex_cs : SV_POSITION;
    float4 normal_coefficients : TEXCOORD2; // x, y, z - coefficients for normal components, w - normal sign
    float3 normal_ws : TEXCOORD3;

    #ifdef VOXEL_GI
    float3 lightmap_uv : TEXCOORD4;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

v2f vert(appdata v)
{
    v2f o;
    const VertexPositionInputs vertex_position_inputs = GetVertexPositionInputs(v.vertex.xyz);
    o.vertex_cs = vertex_position_inputs.positionCS;
    o.vertex_os = v.vertex;
    o.uv = v.uv;

    const float3 normal_ws = GetVertexNormalInputs(v.normal).normalWS;
    float4 normal_coefficients = 0.f;
    const float3 normal_sign = sign(normal_ws);
    normal_coefficients.xyz = abs(normal_ws);
    normal_coefficients.w = normal_sign.x + normal_sign.y + normal_sign.z;
    o.normal_coefficients = normal_coefficients;
    o.normal_ws = normal_ws;

    #ifdef VOXEL_GI
    static const half3 padding = half3(1, 0, 1);
    const half3 lightmap_size = _ChunkSize + padding * 2;
    o.lightmap_uv = max(o.vertex_os.xyz + v.normal * 0.5 + padding, 0) / lightmap_size;
    #endif

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);

    return o;
}

inline float2 get_offset(v2f i)
{
    const float sign = i.normal_coefficients.w;
    const float2x3 mtx = {
        sign * i.vertex_os.z, i.vertex_os.x, -sign * i.vertex_os.x,
        i.vertex_os.y, sign * i.vertex_os.z, i.vertex_os.y
    };
    float2 offset = mul(mtx, i.normal_coefficients.xyz);
    offset -= floor(offset);
    return offset;
}

inline float2 get_effective_uv(v2f i)
{
    const float2 offset = get_offset(i);
    float2 effective_uv = i.uv + offset * _UVStep;

    #ifdef UV_SCROLLING
    const float time = _Time.y;
    half2 scrolling_offset = _UVScrollingSpeed * time;
    scrolling_offset = round(scrolling_offset) * _UVStep;
    effective_uv += scrolling_offset;
    effective_uv = frac(effective_uv);
    #endif

    return effective_uv;
}

half4 frag(v2f i) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(i);

    const half2 effective_uv = get_effective_uv(i);
    half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, effective_uv);

    #ifdef TRANSPARENT
    clip(color.a - _AlphaClipThreshold);
    #endif

    #ifdef VOXEL_GI
    const float2 light_value = SAMPLE_TEXTURE3D(_LightMap, sampler_LightMap, i.lightmap_uv).rg;
    const half3 sunlight_color = (light_value.x + _ExtraLightmapAttenuation.x) * _SunlightColor;
    const half3 torchlight_color = (light_value.y + _ExtraLightmapAttenuation.y) * half3(1, 1, 1);
    const half3 min_ambient_lighting = half3(1, 1, 1) * _MinAmbientLighting;
    const half3 light_color = max(sunlight_color + torchlight_color, min_ambient_lighting);

    color.xyz *= light_color;
    return saturate(color);

    #else

    return color;

    #endif
}

#endif
