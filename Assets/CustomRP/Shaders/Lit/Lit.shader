Shader "CustomRP/Lit"
{
   
    Properties
    {
        _MainColor("Color",Color)=(1.0,1.0,1.0,1.0)
        _MainMap("Texture",2D) = "white"{}
    }

    SubShader
    {
        Tags
        { 
            "RenderType" = "Opaque"
            "LightMode" = "CustomLit"
        }

        LOD 100

        Pass
        {
            Name "Lit"
            HLSLPROGRAM
            
            #pragma target 3.5//�ü���Խ�ߣ�����ʹ�õ��ִ�GPU����Խ�࣬���������UnityĬ��Ϊ 2.5
            #pragma vertex  LitPassVertex
            #pragma fragment LitPassFragment
            #pragma multi_compile_instancing
            #include "LitPass.hlsl"


            ENDHLSL
        }
    }

}
