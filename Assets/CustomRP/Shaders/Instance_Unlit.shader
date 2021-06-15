Shader "CustomRP/Instance Unlit"
{
   
    Properties
    {
        _MainColor("Color",Color)=(1.0,1.0,1.0,1.0)
        _MainTex("Main Tex",2D) = "white"{}
        [Enum(Off,0,On,1)] _ZWrite("Z Write",Float)= 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend",Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend",Float) = 0

        _Cutoff("Alpha Cutoff",Range(0.0,1.0)) = 0.5
        [Toggle(_CLIPPING)]_Clippling("Alpha Clipping",Float) = 0
    }

    SubShader
    {
        Tags{ "RenderType" = "Opaque" "Queue"="Transparent"}
        LOD 100
        Pass
        {
            Name "Instance Unlit"

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            HLSLPROGRAM
            
            #pragma vertex  UnlitPassVertex
            #pragma fragment UnlitPassFragment

            #pragma shader_feature _CLIPPING
            #pragma multi_compile_instancing

            #include "Instance_UnlitPass.hlsl"

            ENDHLSL
        }
    }

}
