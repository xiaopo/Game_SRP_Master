Shader "CustomRP/Unlit"
{
   
    Properties
    {
        _MainColor("Color",Color)=(1.0,1.0,1.0,1.0)
    }

    SubShader
    {
        Tags{ "RenderType" = "Opaque"}
        LOD 100
        Pass
        {
            Name "Unlit"
            HLSLPROGRAM

  
            #pragma vertex  UnlitPassVertex
            #pragma fragment UnlitPassFragment

            #include "UnlitPass.hlsl"


            ENDHLSL
        }
    }

}
