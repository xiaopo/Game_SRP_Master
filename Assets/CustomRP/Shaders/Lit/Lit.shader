Shader "CustomRP/Lit"
{

    Properties
    {
        [NoScaleOffset] _MaskMap("Mask (MODS)",2D) = "white"{}
        _Metallic("Metallic",Range(0,1)) = 0//金属度
        _Smoothness("Smoothness",Range(0,1)) = 0.5//光滑度
        _Fresnel("Fresnel", Range(0, 1)) = 1

        _BaseColor("Color",Color)=(1.0,1.0,1.0,1.0)
        _BaseMap("Texture",2D) = "white"{}
        
        [HideInInspector] _MainTex("Texture for Lightmap", 2D) = "white" {}
        [HideInInspector] _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)

        [NoScaleOffset] _EmissionMap("Emission", 2D) = "white" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0.0, 0.0, 0.0, 0.0)

        [Enum(Off,0,On,1)] _ZWrite("Z Write",Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend",Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend",Float) = 0

        _Cutoff("Alpha Cutoff",Range(0.0,1.0)) = 0.5
        [Toggle(_CLIPPING)]_Clipping("Alpha Clipping",Float) = 0
        [Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha("Premultiply Alpha",Float) = 0
        [KeywordEnum(On,Clip,Dither,Off)]_Shadows("Shadows",Float) = 0
        [Toggle(_RECEIVE_SHADOWS)] _ReceiveShadows("Receive Shadows",Float) = 1
    }

    SubShader
    {
         
        HLSLINCLUDE
        #include "../../ShaderLibrary/Common.hlsl"
        #include "LitInput.hlsl"
        ENDHLSL

        Pass
        {
           
            Tags { "LightMode" = "CustomLit"}

            LOD 100

            //Name "Lit"
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]

            HLSLPROGRAM
            
            #pragma target 3.5//该级别越高，允许使用的现代GPU功能越多，如果不设置Unity默认为 2.5
            #pragma vertex  LitPassVertex
            #pragma fragment LitPassFragment
            
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma shader_feature _RECEIVE_SHADOWS
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile _ _CASCADE_BLEND_SOFT _CASCADE_BLEND_DITHER
            #pragma multi_compile _ _SHADOW_MASK_ALWAYS _SHADOW_MASK_DISTANCE
            #pragma multi_compile _ LOD_FADE_CROSSFADE

            #pragma multi_compile_instancing
            #pragma multi_compile _ LIGHTMAP_ON 
            #include "LitPass.hlsl"

            ENDHLSL
        }

        Pass
        {
            Tags{  "LightMode" = "ShadowCaster" }

            Name "ShadowCaster"
            ColorMask 0

            HLSLPROGRAM

            #pragma target 3.5//该级别越高，允许使用的现代GPU功能越多，如果不设置Unity默认为 2.5

            #pragma shader_feature _CLIPPING
            #pragma shader_feature _ _SHADOWS_CLIP _SHADOWS_DITHER
            #pragma multi_compile_instancing
            #pragma vertex  ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment

            #include "ShadowCasterPass.hlsl"

            ENDHLSL
        }

        Pass 
        {
            Tags { "LightMode" = "Meta"}

            Cull Off

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex MetaPassVertex
            #pragma fragment MetaPassFragment
            #include "MetaPass.hlsl"
            ENDHLSL
        }
    }
   CustomEditor "CustomShaderGUI"
}
