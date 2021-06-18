#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED


//�����������
float3 IncomingLight(Surface surface,Light light)
{
    //�����ع���
    return saturate(dot(surface.normal, light.direction)) * light.color;
}

float3 GetLighting(Surface surface, Light light)
{
    return IncomingLight(surface, light) * surface.color;
}


//��������ı�����Ϣ��ȡ���չ��ս��
float3 GetLighting(Surface surface)
{
    //return GetLighting(surface, GetDirectionLight());
    //�ɼ�������������������ۼӵõ������������
    float3 color = 0.0;
    for (int i = 0; i < GetDirectionLightCount();i++)
    {
        color += GetLighting(surface, GetDirectionLight(i));
    }
    
    return color;

}

#endif