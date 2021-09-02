#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED


//�����������
float3 IncomingLight(Surface surface,Light light)
{
    //�����ع���
    return saturate(dot(surface.normal, light.direction) * light.attenuation) * light.color;

}

float3 GetLighting(Surface surface,BRDF brdf,Light light)
{
    return IncomingLight(surface, light) * DirectBRDF(surface,brdf,light);
}


//��������ı�����Ϣ��ȡ���չ��ս��
float3 GetLighting(Surface surface,BRDF brdf,GI gi)
{

    //�ɼ�������������������ۼӵõ������������
    ShadowData shadowData = GetShadowData(surface);
    float3 color = gi.diffuse * brdf.diffuse;
    for (int i = 0; i < GetDirectionLightCount();i++)
    {
        //Get Color and direction by light index 
        Light light = GetDirectionLight(i, surface, shadowData);
        color += GetLighting(surface, brdf, light);
    }
    
    return color;

}

#endif