#ifndef CUSTOM_UNLIT_PASS_Instance_INCLUDED
#define CUSTOM_UNLIT_PASS_Instance_INCLUDED

#include "../../ShaderLibrary/Common.hlsl"

TEXTURE2D(_MainTex);//定义一张2D文理
SAMPLER(sampler_MainTex);//指定一个采样器

//纹理和采样器是全局资源，不能放入缓冲区中

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(float4,_MainColor)
UNITY_DEFINE_INSTANCED_PROP(float4,_MainTex_ST)
UNITY_DEFINE_INSTANCED_PROP(float,_Cutoff)

UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Attributes
{
    float3 psotion : POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    
};

struct Varyings
{
    float4 position : SV_Position;
    float2 baseUV : VAR_BASE_UV;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

//vertex function
Varyings UnlitPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    float3 positionWS = TransformObjectToWorld(input.psotion);
    output.position = TransformWorldToHClip(positionWS);
    
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
    output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    
    return output;

}

//framgment function
float4 UnlitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    float4 baseMap = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.baseUV);

    float4 baseColor =  UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainColor);
    
    float4 color = baseMap * baseColor;
#if defined(_CLIPPING)
    clip(color.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
 #endif
    return color;

}


#endif