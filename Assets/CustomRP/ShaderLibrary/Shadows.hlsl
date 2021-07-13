#ifndef CUSTOM_SHADOWS_INCLUDED
#define CUSTOM_SHADOWS_INCLUDED

#define MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_CASCADE_COUNT 4
//TEXTURE2D(_DirectionalShadowAtlas);
//SAMPLER(sampler_DirectionalShadowAtlas);

//as the atlas isn't a regular texture let's define it via the TEXTURE2D_SHADOW
TEXTURE2D_SHADOW(_DirectionalShadowAtlas);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(SHADOW_SAMPLER);

CBUFFER_START(_CustomShadows)
    int _CascadeCount;
    float4 _CascadeCullingShperes[MAX_CASCADE_COUNT];
    float4x4 _DirectionalShadowMatrices[MAX_SHADOWED_DIRECTIONAL_LIGHT_COUNT * MAX_CASCADE_COUNT];
    float4 _CascadeData[MAX_CASCADE_COUNT];
    float4 _ShadowDistanceFade;
CBUFFER_END

struct DirectionalShadowData
{
    float strength;
    int tileIndex;
    float normalBias;
};

//The cascade index is determined per fragment,not per light.
struct ShadowData
{
    int cascadeIndex;
    float strength;
 
};

// (1 - d/m)/f
float FadedShadowStrength(float d, float mx, float fx)
{
    return saturate((1.0 - d * mx) * fx);
}


ShadowData GetShadowData(Surface surfaceWS)
{
    //Loop through all cascade culling spheres in GetShadowData until we find one that contains the surface position. 
    //Break out of the loop once it's found and then use the current loop iterator as the cascade index.
    //This means we end up with an invalid index if the fragment lies outside all spheres, but we'll ignore that for now.
    ShadowData data;
    data.strength = FadedShadowStrength(surfaceWS.depth, _ShadowDistanceFade.x, _ShadowDistanceFade.z);
    int i;
    for (i = 0; i < _CascadeCount; i++)
    {
        float4 sphere = _CascadeCullingShperes[i];
        float distanceSqr = DistanceSquared(surfaceWS.position, sphere.xyz);
        if (distanceSqr < sphere.w)
        {
            if( i == _CascadeCount - 1)
                data.strength *= FadedShadowStrength(distanceSqr, _CascadeData[i].x, _ShadowDistanceFade.z);

            break;
        }
    }
    
    if (i == _CascadeCount)
        data.strength = 0.0;
        
    data.cascadeIndex = i;
    return data;
}

float SampleDirectionalShadowAtlas(float3 positionSTS)
{
    return SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
}

float GetDirectionalShadowAttenuation(DirectionalShadowData directional,ShadowData global,Surface surfaceWS)
{
    if (directional.strength <= 0.0)
    {
        return 1.0;
    }
    
    float3 normalBias = surfaceWS.normal * (directional.normalBias * _CascadeData[global.cascadeIndex].y);
    
    float3 positionSTS = mul(_DirectionalShadowMatrices[directional.tileIndex], float4(surfaceWS.position + normalBias, 1.0)).xyz;
    float shadow = SampleDirectionalShadowAtlas(positionSTS);

    return lerp(1.0, shadow, directional.strength);
}

#endif