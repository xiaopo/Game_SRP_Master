#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED
#define MAX_DIRECTIONAL_LIGHT_COUNT 4
CBUFFER_START(_CustomLight)
    //float3 _DirectionLightColor;
    //float3 _DirectionLightDrection;
    int _DirectionLightCount;//��Чƽ�й����
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


//��ȡ����������
int GetDirectionLightCount()
{
    return _DirectionLightCount;
}

DirectionalShadowData GetDirectionalShadowData(int lightIndex)
{
    DirectionalShadowData data;
    data.strength = _DirectionalLightShadowData[lightIndex].x;
    data.tileIndex = _DirectionalLightShadowData[lightIndex].y;
    return data;

}

//��ȡָ�������ķ��������
Light GetDirectionLight(int index,Surface surfaceWS)
{
    
    Light light;
    light.color = _DirectionLightColors[index].rgb;
    light.direction = _DirectionLightDrections[index].xyz;
    
    DirectionalShadowData shadowData = GetDirectionalShadowData(index);
    light.attenuation = GetDirectionalShadowAttenuation(shadowData, surfaceWS);
    return light;

}

#endif