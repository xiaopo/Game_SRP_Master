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
        _StencilRef("Stencil Ref Value",Float) = 0
        //[Enum(Never,0,Less,1,Equal,3,LEqual,4,Greater,5,NotEqual,6,GEqual,7,Always,8)] _StencilComparison("Stencil Comparison",Float) = 3
        //[Enum(Keep,0,Zero,1,Replace,3,IncrSat,4,DecrSat,5,Invert,6,IncrWrap,7,DecrWrap,8)] _StencilOperation("Stencil Operation",Float) = 2
        [Enum(Never,1,Less,2,Equal,3,LEqual,4,Greater,5,Always,8)] _StencilComparison("Stencil Comparison",Float) = 3
        [Enum(Keep,0,Zero,1,Replace,2)] _StencilOperation("Stencil Operation",Float) = 2

    }

    SubShader
    {
        HLSLINCLUDE
        #include "../../ShaderLibrary/Common.hlsl"
        #include "UnlitInput.hlsl"
        ENDHLSL


        Tags{ "RenderType" = "Opaque" "Queue"="Transparent"}    

        LOD 100
        Pass
        {
            Name "ParticleUnlit"

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]

             Stencil
             {
                 Ref[_StencilRef]
                 Comp[_StencilComparison]
                 Pass[_StencilOperation]
             }

            HLSLPROGRAM

            #pragma target 3.5
            #pragma vertex  UnlitPassVertex
            #pragma fragment UnlitPassFragment

            #pragma shader_feature _CLIPPING
            #pragma multi_compile_instancing

            #include "UnlitPass.hlsl"

            ENDHLSL
        }
    }
    
   CustomEditor "CustomShaderGUI"
}
