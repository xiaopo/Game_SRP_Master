Shader "CustomRP/UnlitSRPBatcher"
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
            Name "UnlitSRPBatcher"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex  UnlitPassVertex
            #pragma fragment UnlitPassFragment

            #include "UnlitSRPBatcher.hlsl"

            ENDHLSL
        }
    }

}
