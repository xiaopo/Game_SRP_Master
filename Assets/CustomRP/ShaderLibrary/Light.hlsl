#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED
#define MAX_DIRECTIONAL_LIGHT_COUNT 4
CBUFFER_START(_CustomLight)
    //float3 _DirectionLightColor;
    //float3 _DirectionLightDrection;
    int _DirectionLightCount;
    float4 _DirectionLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionLightDrections[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct Light
{
    float3 color;
    float3 direction;
};

//��ȡ����������
int GetDirectionLightCount()
{
    return _DirectionLightCount;
}

//��ȡָ�������ķ��������
Light GetDirectionLight(int index)
{
    Light light;
    light.color = _DirectionLightColors[index].rgb;
    light.direction = _DirectionLightDrections[index].xyz;
    
    return light;

}

#endif