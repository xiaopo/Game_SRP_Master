Shader "CustomRP/Lit"
{

    Properties
    {
        _Metallic("Metallic",Range(0,1)) = 0//金属度
        _Smoothness("Smoothness",Range(0,1)) = 0.5//光滑度

        _BaseColor("Color",Color)=(1.0,1.0,1.0,1.0)
        _BaseMap("Texture",2D) = "white"{}
        
        [Enum(Off,0,On,1)] _ZWrite("Z Write",Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend",Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend",Float) = 0

        _Cutoff("Alpha Cutoff",Range(0.0,1.0)) = 0.5
        [Toggle(_CLIPPING)]_Clipping("Alpha Clipping",Float) = 0
        [Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha("Premultiply Alpha",Float) = 0
    }

    SubShader
    {
         

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
            #pragma multi_compile_instancing
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #include "LitPass.hlsl"

            ENDHLSL
        }

         Pass
        {
            Tags{  "LightMode" = "ShadowCaster" }

            //Name "ShadowCaster"
            ColorMask 0

            HLSLPROGRAM

            #pragma target 3.5//该级别越高，允许使用的现代GPU功能越多，如果不设置Unity默认为 2.5

            #pragma shader_feature _CLIPPING
            #pragma multi_compile_instancing

            #pragma vertex  ShadowCasterPassVertex
            #pragma fragment ShadowCasterPassFragment

            #include "ShadowCasterPass.hlsl"

            ENDHLSL
        }
    }
   CustomEditor "CustomShaderGUI"
}
