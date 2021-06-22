#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED

struct BRDF
{
    float3 diffuse;//��������ɫ
    float3 specular;//���淴����ɫ
    float roughness;//�ֲڶ�
};

//����ʵķ�����ƽ��Լ0.04
#define MIN_REFLECTIVITY 0.04

float OneMinuseReflectivity(float metallic)
{
    float range = 1.0 - MIN_REFLECTIVITY;
    return range - metallic * range;
}

//���ݹ�ʽ�õ����淴��ǿ��
float SpecularStrength(Surface surface,BRDF brdf,Light light)
{
    float3 h = SafeNormalize(light.direction + surface.viewDirection);//�������
    float nh2 = Square(saturate(dot(surface.normal,h)));
    float lh2 = Square(saturate(dot(light.direction, h)));
    float r2 = Square(brdf.roughness);
    float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
    float normalization = brdf.roughness * 4.0 + 2.0;
    
    return r2 / (d2 * max(0.1, lh2) * normalization);

}

float3 DirectBRDF(Surface surface,BRDF brdf,Light light)
{
    return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

//��ȡ���������BRDF����
BRDF GetBRDF(Surface surface,bool applyAlphaToDiffuse = false)
{
    BRDF brdf;
    float oneMinuseReflectivity = OneMinuseReflectivity(surface.metallic); //1.0 - surface.metallic;
    brdf.diffuse = surface.color * oneMinuseReflectivity;
    if (applyAlphaToDiffuse)
    {
        brdf.diffuse *= surface.alpha;
    }
    
    //������ɫ��ȥ��������ɫ
    //brdf.specular = surface.color - brdf.diffuse;
    //�ǽ�����Ӱ�쾵�淴����ɫ
        brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
    
    //�ֲڶȺ͹⻬���෴��ֻ��Ҫʹ��1��ȥ�⻬�ȼ���
    //ʹ��CommonMaterial���÷���
        float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
        brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    
        return brdf;
    
    }
#endif