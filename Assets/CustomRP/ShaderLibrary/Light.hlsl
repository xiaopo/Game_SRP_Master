#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED
#define MAX_DIRECTIONAL_LIGHT_COUNT 4
CBUFFER_START(_CustomLight)
    //float3 _DirectionLightColor;
    //float3 _DirectionLightDrection;
    int _DirectionLightCount;//有效平行光个数
    float4 _DirectionLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionLightDrections[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct Light
{
    float3 color;
    float3 direction;
    float attenuation;
};


//获取方向光的数量
int GetDirectionLightCount()
{
    return _DirectionLightCount;
}

DirectionalShadowData GetDirectionalShadowData(int lightIndex, ShadowData shadowData)
{
    DirectionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x * shadowData.strength;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
    data.normalBias = _DirectionalLightShadowData[lightIndex].z;
    return data;

}

//获取指定索引的方向的数据
Light GetDirectionLight(int index, Surface surfaceWS, ShadowData shadowData)
{
    Light light;
    light.color = _DirectionLightColors[index].rgb;
    light.direction = _DirectionLightDrections[index].xyz;
    
    DirectionalShadowData dirshadowData = GetDirectionalShadowData(index, shadowData);
    light.attenuation = GetDirectionalShadowAttenuation(dirshadowData, shadowData,surfaceWS);

    return light;

}

#endif