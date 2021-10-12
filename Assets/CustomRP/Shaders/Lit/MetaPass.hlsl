#ifndef CUSTOM_META_PASS_INCLUDED
#define CUSTOM_META_PASS_INCLUDED

#include "../../ShaderLibrary/Surface.hlsl"
#include "../../ShaderLibrary/Shadows.hlsl"
#include "../../ShaderLibrary/Light.hlsl"
#include "../../ShaderLibrary/BRDF.hlsl"

struct Attributes
{
    float3 position : POSITION;
    float2 uv : TEXCOORD0; 
    float2 lightMapUV : TEXCOORD1;
};

struct Varyings
{
    float4 position : SV_Position;
    float2 uv : VAR_BASE_UV;
};


bool4 unity_MetaFragmentControl;
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

Varyings MetaPassVertex(Attributes input)
{
    Varyings output;
  
    input.position.xy = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    input.position.z = input.position.z > 0.0 ? FLT_MIN : 0.0;
    output.position = TransformWorldToHClip(input.position);
    output.uv = TransformBaseUV(input.uv);
    return output;
}


float4 MetaPassFragment(Varyings input) : SV_TARGET
{
    InputConfig config = GetInputConfig(input.uv);
    float4 albedo = GetBase(config);

    Surface surface;
    ZERO_INITIALIZE(Surface, surface);

    surface.color = albedo.rgb;
    surface.metallic = GetMetallic(config);
    surface.smoothness = GetSmoothness(config);

    BRDF brdf = GetBRDF(surface);
    float4 meta = 0.0;
    if (unity_MetaFragmentControl.x)
    {
        meta = float4(brdf.diffuse, 1.0);
        meta.rgb += brdf.specular * brdf.roughness * 0.5;
        meta.rgb = min(PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue);
    }
    else if (unity_MetaFragmentControl.y)
    {
        meta = float4(GetEmission(config), 1.0);
    }
    
    return meta;
}

#endif