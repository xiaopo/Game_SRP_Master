#ifndef CUSTOM_SHADOWCASTER_PASS_INCLUDED
#define CUSTOM_SHADOWCASTER_PASS_INCLUDED

#include "../../ShaderLibrary/Common.hlsl"


TEXTURE2D(_BaseMap);//定义一张2D文理
SAMPLER(sampler_BaseMap);//指定一个采样器

//纹理和采样器是全局资源，不能放入缓冲区中

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)

UNITY_DEFINE_INSTANCED_PROP(float4,_BaseColor)
UNITY_DEFINE_INSTANCED_PROP(float4,_BaseMap_ST)
UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)

UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Attributes
{
    float3 position : POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    
};

struct Varyings
{
    float4 position : SV_Position;
    float2 uv : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


Varyings ShadowCasterPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    float3 worldPos = TransformObjectToWorld(input.position);
    output.position = TransformWorldToHClip(worldPos);
    
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    output.uv = input.uv * baseST.xy + baseST.zw;
    
    return output;

}


void ShadowCasterPassFragment(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 albedo = baseMap * baseColor;
    
    #if defined(_CLIPPING)
        clip(albedo.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #endif
    
}

#endif