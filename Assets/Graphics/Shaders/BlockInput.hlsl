#ifndef BLOCK_INPUT_INCLUDED
#define BLOCK_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

float3 _ChunkSize = float3(16, 50, 16);
half _MinAmbientLighting = 0;
half3 _SunlightColor = half3(1, 1, 1);

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
float4 _LightMap_TexelSize;
float2 _UVStep;
half _AlphaClipThreshold;
half2 _ExtraLightmapAttenuation;

half2 _UVScrollingSpeed;

// for other passes only
half4 _BaseColor;
half _Cutoff;
CBUFFER_END

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

TEXTURE3D(_LightMap);
SAMPLER(sampler_LightMap);

half Alpha(half albedoAlpha, half4 color, half cutoff)
{
    half alpha = color.a * albedoAlpha;
    return alpha;
}

half4 SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
{
    return SAMPLE_TEXTURE2D(albedoAlphaMap, sampler_albedoAlphaMap, uv);
}

#endif
