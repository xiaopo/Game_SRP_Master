Shader "CustomRP/Particles/Unlit"
{
   
    Properties
    {
        [HDR]_MainColor("Color",Color)=(1.0,1.0,1.0,1.0)
        _MainTex("Main Tex",2D) = "white"{}
        [Enum(Off,0,On,1)] _ZWrite("Z Write",Float)= 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend",Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend",Float) = 0

        _Cutoff("Alpha Cutoff",Range(0.0,1.0)) = 0.5
        [Toggle(_CLIPPING)]_Clippling("Alpha Clipping",Float) = 0
        [Toggle(_VERTEX_COLORS)] _VertexColors("Vertex Colors", Float) = 0
        [Toggle(_FLIPBOOK_BLENDING)] _FlipbookBlending("Flipbook Blending", Float) = 0
        [Toggle(_NEAR_FADE)] _NearFade("Near Fade", Float) = 0
        _NearFadeDistance("Near Fade Distance", Range(0.0, 10.0)) = 1
        _NearFadeRange("Near Fade Range", Range(0.01, 10.0)) = 1
    }

    SubShader
    {
        HLSLINCLUDE
        #include "../../ShaderLibrary/Common.hlsl"
        #include "../Unlit/UnlitInput.hlsl"
        ENDHLSL

        Tags{ "RenderType" = "Opaque" "Queue"="Transparent"}    

        LOD 100
        Pass
        {
            Name "ParticleUnlit"

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]

            HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex  UnlitPassVertex
            #pragma fragment UnlitPassFragment

            #pragma shader_feature _CLIPPING
            #pragma shader_feature _VERTEX_COLORS
            #pragma shader_feature _FLIPBOOK_BLENDING
            #pragma shader_feature _NEAR_FADE

            #include "UnlitPass.hlsl"

            ENDHLSL
        }
    }
    
}
