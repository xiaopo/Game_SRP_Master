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
    shadowData.shadowMask = gi.shadowMask;

    float3 color = IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
    for (int i = 0; i < GetDirectionLightCount();i++)
    {
        //Get Color and direction by light index 
        Light light = GetDirectionLight(i, surface, shadowData);
        if (RenderingLayersOverlap(surface, light)) {
            color += GetLighting(surface, brdf, light);
        }

    }
    
    //point light and spot light
    #if defined(_LIGHTS_PER_OBJECT)
        for (int j = 0; j < min(unity_LightData.y, 8); j++)
        {
            int lightIndex = unity_LightIndices[(uint)j / 4][(uint) j % 4];
			Light light = GetOtherLight(lightIndex, surface, shadowData);
            if (RenderingLayersOverlap(surface, light)) {
                color += GetLighting(surface, brdf, light);
            }
		}
	#else
        for (int j = 0; j < GetOtherLightCount(); j++)
        {
            Light light = GetOtherLight(j, surface, shadowData);
            if (RenderingLayersOverlap(surface, light)) {
                color += GetLighting(surface, brdf, light);
            }
        }
    #endif
    
     return color;
}
#endif