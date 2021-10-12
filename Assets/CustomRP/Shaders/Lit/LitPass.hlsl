#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

#include "../../ShaderLibrary/Surface.hlsl"
#include "../../ShaderLibrary/Shadows.hlsl"

#include "../../ShaderLibrary/Light.hlsl"
#include "../../ShaderLibrary/BRDF.hlsl"
#include "../../ShaderLibrary/GI.hlsl"

#include "../../ShaderLibrary/Lighting.hlsl"

struct Attributes
{
    float3 position : POSITION;
    float2 uv : TEXCOORD0;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;

    GI_ATTRIBUTE_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
    
};

struct Varyings
{
    float4 position : SV_Position;
    float2 baseuv : VAR_BASE_UV;
    
#if defined(_DETAIL_MAP)
    float2 detailuv:VAR_DETAIL_UV;
#endif
    
    float3 worldNormal:VAR_NORMAL;
    
#if defined(_NORMAL_MAP)
    float4 tangentWs : VAR_TANGENT;
#endif
    
    float3 worldPos : VAR_POSITION;
    
    GI_VARYINGS_DATA    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


Varyings LitPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    TRANSFER_GI_DATA(input, output);
            
    output.worldPos = TransformObjectToWorld(input.position);
    output.position = TransformWorldToHClip(output.worldPos);
    output.worldNormal = TransformObjectToWorldNormal(input.normal);
    
#if defined(_NORMAL_MAP)
    output.tangentWs = float4(TransformObjectToWorldDir(input.tangent.xyz), input.tangent.w);
#endif
    
    output.baseuv = TransformBaseUV(input.uv);
    
#if defined(_DETAIL_MAP)
    output.detailuv = TransformDetailUV(input.uv);
#endif
    
    return output;
}

float4 LitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    ClipLOD(input.position.xy, unity_LODFade.x);
    
    InputConfig config = GetInputConfig(input.baseuv);
    #if defined(_MASK_MAP)
		config.useMask = true;
	#endif
    
    #if defined(_DETAIL_MAP)
		config.detailUV = input.detailuv;
		config.useDetail = true;
	#endif
    
    float4 albedo = GetBase(config);
    
    #if defined(_CLIPPING)
        clip(albedo.a - GetCutoff(config));
    #endif
    
    //Create a surface struct by those infomation
    Surface surface;
    surface.position = input.worldPos;

#if defined(_NORMAL_MAP)
    surface.normal = NormalTangentToWorld(GetNormalTS(config), input.worldNormal, input.tangentWs);
    surface.interpolatedNormal = input.worldNormal;
#else
    surface.normal = normalize(input.worldNormal);
    surface.interpolatedNormal = surface.normal;
#endif
    
    surface.viewDirection = normalize(_WorldSpaceCameraPos - input.worldPos);
    surface.color = albedo.rgb;
    surface.alpha = albedo.a;
    
    //with 1 indicating that it is fully metallic. The default is fully dielectric
    surface.metallic = GetMetallic(config);
    surface.occlusion = GetOcclusion(config);
    surface.smoothness = GetSmoothness(config); 
    surface.fresnelStrength = GetFresnel(config);
    surface.depth = -TransformWorldToView(input.worldPos).z;
    surface.dither = InterleavedGradientNoise(input.position.xy, 0);
    

#if defined(_PREMULTIPLY_ALPHA)
    BRDF brdf = GetBRDF(surface,true);
#else
    BRDF brdf = GetBRDF(surface);
#endif
    
    GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
    float3 color = GetLighting(surface,brdf,gi);
    color += GetEmission(config);
    
    return float4(color, surface.alpha);

}

#endif