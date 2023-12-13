#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED
#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_OTHER_LIGHT_COUNT 64

CBUFFER_START(_CustomLight)
    int _DirectionLightCount;//有效平行光个数
    float4 _DirectionLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirectionsAndMasks[MAX_DIRECTIONAL_LIGHT_COUNT];

    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _OtherLightShadowData[MAX_OTHER_LIGHT_COUNT];

    int _OtherLightCount;
    float4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightPositions[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightDirectionsAndMasks[MAX_OTHER_LIGHT_COUNT];
    float4 _OtherLightSpotAngles[MAX_OTHER_LIGHT_COUNT];
CBUFFER_END

struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
    uint renderingLayerMask;
};

//----------- direction light

int GetDirectionLightCount()
{
    return _DirectionLightCount;
}

int GetOtherLightCount()
{
    return _OtherLightCount;
}

bool RenderingLayersOverlap(Surface surface, Light light) {
    return (surface.renderingLayerMask & light.renderingLayerMask) != 0;
}

DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowData)
{
    DirectionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
    data.normalBias = _DirectionalLightShadowData[lightIndex].z;
    data.shadowMaskChannel = _DirectionalLightShadowData[lightIndex].w;
    return data;
}


OtherShadowData GetOtherShadowData(int lightIndex)
{
    OtherShadowData data;
    data.strength = _OtherLightShadowData[lightIndex].x;
    data.tileIndex = _OtherLightShadowData[lightIndex].y;
    data.shadowMaskChannel = _OtherLightShadowData[lightIndex].w;
    data.isPoint = _OtherLightShadowData[lightIndex].z == 1.0;
    data.lightPositionWS = 0.0;
    data.lightDirectionWS = 0.0;
    data.spotDirectionWS = 0.0;
    
    return data;
}

/**The shadows of point and spot lights can also be baked into the shadow mask, 
 * by setting their Mode to Mixed. 
 * Each lights gets a channel,  just like directional lights. 
 * 
 * But because their range is limited it is possible  for multiple lights to use the same channel, 
 * as long as they don't overlap. Thus the shadow mask can support an arbitrary amount of lights, 
 * but only up to four per texel. 
 * 
 * If multiple lights end up overlapping while trying to claim the same channel then 
 * the least important lights will be forced to Baked mode until there is no longer a conflict.
 * **/


//获取指定索引的方向的数据
Light GetDirectionLight(int index, Surface surfaceWS, ShadowData shadowData)
{
    Light light;
    light.color = _DirectionLightColors[index].rgb;
    light.direction = _DirectionalLightDirectionsAndMasks[index].xyz;
    light.renderingLayerMask = asuint(_DirectionalLightDirectionsAndMasks[index].w);

    DirectionalShadowData dirshadowData = GetDirectionalShadowData(index, shadowData);
    light.attenuation = GetDirectionalShadowAttenuation(dirshadowData, shadowData,surfaceWS);

    return light;
}

//point light and spot light
Light GetOtherLight(int index, Surface surfaceWS, ShadowData shadowData)
{
    Light light;
    light.color = _OtherLightColors[index].rgb;
    float3 position = _OtherLightPositions[index].xyz;
    
    float3 ray = position - surfaceWS.position;
    light.direction = normalize(ray);
    float distanceSqr = max(dot(ray, ray), 0.00001);

    //max(0,1 - (d^2 / r^2)^2)^2;
    float rangeAttenuation = Square(saturate(1.0 - Square(distanceSqr * _OtherLightPositions[index].w)));
    

    //saturate(d * a + b)^2 
    //The "spotangles" values are calculated on the CPU side, with the formula separated into x and y components respectively
    float4 spotAngles = _OtherLightSpotAngles[index];
    float3 spotDirection = _OtherLightDirectionsAndMasks[index].xyz;
    light.renderingLayerMask = asuint(_OtherLightDirectionsAndMasks[index].w);
    float spotAttenuation = Square(saturate(dot(spotDirection, light.direction) * spotAngles.x + spotAngles.y));
    
    //other light with shadowmask
    OtherShadowData otherShadowData = GetOtherShadowData(index);
    otherShadowData.lightPositionWS = position;
    otherShadowData.lightDirectionWS = light.direction;
    otherShadowData.spotDirectionWS = spotDirection;
    
    float otherShadowVal = GetOtherShadowAttenuation(otherShadowData, shadowData, surfaceWS);

    //To calculate the attenuation that consists of shadow, light, and range.
    light.attenuation = otherShadowVal * spotAttenuation * rangeAttenuation / distanceSqr;

    return light;  
}

#endif