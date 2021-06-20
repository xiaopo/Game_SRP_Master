#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

#include "../../ShaderLibrary/Common.hlsl"
#include "../../ShaderLibrary/Surface.hlsl"
#include "../../ShaderLibrary/Light.hlsl"
#include "../../ShaderLibrary/BRDF.hlsl"
#include "../../ShaderLibrary/Lighting.hlsl"


TEXTURE2D(_MainMap);//定义一张2D文理
SAMPLER(sampler_MainMap);//指定一个采样器

//纹理和采样器是全局资源，不能放入缓冲区中

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(float4,_MainColor)
UNITY_DEFINE_INSTANCED_PROP(float4,_MainMap_ST)
UNITY_DEFINE_INSTANCED_PROP(float,_Metallic)
UNITY_DEFINE_INSTANCED_PROP(float,_Smoothness)

UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Attributes
{
    float3 position : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    
};

struct Varyings
{
    float4 position : SV_Position;
    float2 uv : VAR_BASE_UV;
    float3 worldNormal : VAR_NORMAL;
    float3 worldPos : VAR_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


Varyings LitPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    output.worldPos = TransformObjectToWorld(input.position);
    output.position = TransformWorldToHClip(output.worldPos);
    
    output.worldNormal = TransformObjectToWorldNormal(input.normal);
    
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainMap_ST);
    output.uv = input.uv * baseST.xy + baseST.zw;
    
    return output;

}


float4 LitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    float4 baseMap = SAMPLE_TEXTURE2D(_MainMap, sampler_MainMap, input.uv);

    float4 baseColor =  UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainColor);
    
    float4 albedo = baseMap * baseColor;
    
    Surface surface;
    surface.normal = normalize(input.worldNormal);
    surface.viewDirection = normalize(_WorldSpaceCameraPos - input.worldPos);
    surface.color = albedo.rgb;
    surface.alpha = albedo.a;
    surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
    surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
    
    //通过表面属性计算最终光照结果
    BRDF brdf = GetBRDF(surface);
    float3 color = GetLighting(surface,brdf);
    
    return float4(color, surface.alpha);

}

#endif