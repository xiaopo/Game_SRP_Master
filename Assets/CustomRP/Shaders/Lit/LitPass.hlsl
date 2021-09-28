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
    
    GI_ATTRIBUTE_DATA
    UNITY_VERTEX_INPUT_INSTANCE_ID
    
};

struct Varyings
{
    float4 position : SV_Position;
    float2 uv : VAR_BASE_UV;
    float3 worldNormal : VAR_NORMAL;
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

    output.uv = TransformBaseUV(input.uv);
    
    return output;

}


float4 LitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    ClipLOD(input.position.xy, unity_LODFade.x);
    
    float4 albedo = GetBase(input.uv);
    
    #if defined(_CLIPPING)
        clip(albedo.a - GetCutoff(input.uv));
    #endif
    
    //Create a surface struct by those infomation
    Surface surface;
    surface.position = input.worldPos;
    surface.normal = normalize(input.worldNormal);
   
    surface.viewDirection = normalize(_WorldSpaceCameraPos - input.worldPos);
    surface.color = albedo.rgb;
    surface.alpha = albedo.a;
    
    //with 1 indicating that it is fully metallic. The default is fully dielectric
    surface.metallic = GetMetallic(input.uv); //UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
    //with 0 being perfectly rough and 1 being perfectly smooth.
    surface.smoothness = GetSmoothness(input.uv); //UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
    surface.fresnelStrength = GetFresnel(input.uv);
    surface.depth = -TransformWorldToView(input.worldPos).z;
    surface.dither = InterleavedGradientNoise(input.position.xy, 0);
    

#if defined(_PREMULTIPLY_ALPHA)
    BRDF brdf = GetBRDF(surface,true);
#else
    BRDF brdf = GetBRDF(surface);
#endif
    
    GI gi = GetGI(GI_FRAGMENT_DATA(input), surface, brdf);
    float3 color = GetLighting(surface,brdf,gi);
    color += GetEmission(input.uv);
    
    return float4(color, surface.alpha);

}

#endif