Shader "Voxel Terrain/Block"
{
    Properties
    {
        [MainTexture] [NoScaleOffset] _BaseMap ("Texture", 2D) = "white" {}
        _UVStep("UV Step (1 Over Block Number)", Vector) = (0.125, 0.125, 0, 0)
        [Toggle(TRANSPARENT)] _Transparent("Transparent", Float) = 0.0
        _AlphaClipThreshold("Alpha Clip Threshold", Range(0.0, 1.0)) = 0.9
        [Toggle(UV_SCROLLING)] _UVScrolling("UV Scrolling", Float) = 0
        _UVScrollingSpeed("UV Scrolling Speed", Vector) = (1.0, 0.0, 0.0, 0.0)
        [HideInInspector] [PerRendererData] [NoScaleOffset] _LightMap ("Light Map", 3D) = "black" {}
        [HideInInspector] [PerRendererData] _ExtraLightmapAttenuation ("Extra Light Map Attenuation", Float) = 0.0
    }
    SubShader
    {
        Tags {"RenderType" = "Opaque" "IgnoreProjector" = "True" "RenderPipeline" = "UniversalPipeline" }
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

            #pragma multi_compile _ VOXEL_GI
            #pragma multi_compile_instancing
            
            #pragma shader_feature_local TRANSPARENT
            #pragma shader_feature_local UV_SCROLLING
            
            #include "./BlockInput.hlsl"
            #include "./BlockForwardPass.hlsl"

            ENDHLSL
        }
        
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #pragma multi_compile_instancing

            #include "./BlockInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"

            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #pragma multi_compile_instancing

            #include "./BlockInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthNormals"
            Tags{"LightMode" = "DepthNormals"}

            ZWrite On
            Cull Back

            HLSLPROGRAM

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            #pragma multi_compile_instancing

            #include "./BlockInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
}
