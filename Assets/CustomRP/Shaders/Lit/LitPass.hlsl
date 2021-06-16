#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

#include "../../ShaderLibrary/Common.hlsl"
#include "../../ShaderLibrary/Surface.hlsl"
#include "../../ShaderLibrary/Light.hlsl"
#include "../../ShaderLibrary/Lighting.hlsl"


TEXTURE2D(_MainMap);//定义一张2D文理
SAMPLER(sampler_MainMap);//指定一个采样器

//纹理和采样器是全局资源，不能放入缓冲区中

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(float4,_MainColor)
UNITY_DEFINE_INSTANCED_PROP(float4,_MainMap_ST)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Attributes
{
    float3 psotion : POSITION;
    float2 baseUV : TEXCOORD0;
    
    float3 normal : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    
};

struct Varyings
{
    float4 position : SV_Position;
    float2 baseUV : VAR_BASE_UV;
    float3 worldNormal : VAR_NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


Varyings LitPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    float3 positionWS = TransformObjectToWorld(input.psotion);
    output.position = TransformWorldToHClip(positionWS);
    
    output.worldNormal = TransformObjectToWorldNormal(input.normal);
    
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainMap_ST);
    output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    
    return output;

}


float4 LitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    float4 baseMap = SAMPLE_TEXTURE2D(_MainMap, sampler_MainMap, input.baseUV);

    float4 baseColor =  UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainColor);
    
    float4 albedo = baseMap * baseColor;
    
    Surface surface;
    surface.normal = normalize(input.worldNormal);
    surface.color = albedo.rgb;
    surface.alpha = albedo.a;
    
    //通过表面属性计算最终光照结果
    float3 color = GetLighting(surface);
    
    return float4(color, surface.alpha);

}

#endif