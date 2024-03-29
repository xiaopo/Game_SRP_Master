﻿#ifndef CUSTOM_META_PASS_INCLUDED
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

/**
 * Because indirect diffuse light bounces off surfaces it should be affected
 * by the diffuse reflectivity of those surfaces. 
 * This currently doesn't happen. Unity treats our surfaces as uniformly white. 
 * 
 * Unity uses a special meta pass to determine the reflected light while baking
 * **/

bool4 unity_MetaFragmentControl;
float unity_OneOverOutputBoost;
float unity_MaxOutputValue;

Varyings MetaPassVertex(Attributes input)
{
    Varyings output;
  
    //利用 xy 保存lightmap uv坐标
    input.position.xy = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    
    //We still need the object-space vertex attribute as input because shaders expect it to exist.
    //In fact, it seems that OpenGL doesn't work unless it explicitly uses the Z coordinate.
    //We'll use the same dummy assignment that Unity's own meta pass uses, which is input.positionOS.z > 0.0 ? FLT_MIN : 0.0.
    input.position.z = input.position.z > 0.0 ? FLT_MIN : 0.0;
    
    //继续转一下
    output.position = TransformWorldToHClip(input.position);
    
    output.uv = TransformBaseUV(input.uv);
    return output;
}


float4 MetaPassFragment(Varyings input) : SV_TARGET
{
    InputConfig config = GetInputConfig(input.position,input.uv);
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
        // If the X flag is set then diffuse reflectivity is requested
        meta = float4(brdf.diffuse, 1.0);
        meta.rgb += brdf.specular * brdf.roughness * 0.5;
        
        //the result is modified by raising it to a power provided via unity_OneOverOutputBoost with the PositivePow method, 
        //and then limited it to unity_MaxOutputValue.
        meta.rgb = min(PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue);
    }
    else if (unity_MetaFragmentControl.y)
    {
        //Emissive light is baked via a separate pass. When the Y flag of unity_MetaFragmentControl is set 
        meta = float4(GetEmission(config), 1.0);
    }
    
    return meta;
}

#endif


/**
 * Baked Transparency
 * Hard-Coded Properties
 * Unfortunately Unity's lightmapper has a hard-coded approach for transparency. 
 * It looks at the material's queue to determine whether it's opaque, clipped, or transparent.
 * It then determines transparency by multiplying the alpha components of a _MainTex and _Color property, 
 * using the _Cutoff property for alpha clipping. Our shaders have the third but lack first two. 
 * The only way to currently make this work is by adding the expected properties to our shaders, 
 * giving them the HideInInspector attribute so they don't show up in the inspector. 
 * Unity's SRP shaders have to deal with the same problem.
 * 
 * [HideInInspector] _MainTex("Texture for Lightmap", 2D) = "white" {}
 * [HideInInspector] _Color("Color for Lightmap", Color) = (0.5, 0.5, 0.5, 1.0)
 * **/

/**
 * Copying Properties
 * We have to make sure that the _MainTex property points to the same texture as _BaseMap and 
 * uses the same UV transformation. Both color properties must also be identical. 
 * We can do this in a new CopyLightMappingProperties method that we invoke at the end of CustomShaderGUI.OnGUI 
 * if a change has been made. If the relevant properties exist copy their values.
 * **/
        