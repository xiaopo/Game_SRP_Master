#ifndef CUSTOM_BRDF_INCLUDED
#define CUSTOM_BRDF_INCLUDED

struct BRDF
{
    float3 diffuse;//漫反射颜色
    float3 specular;//镜面反射颜色
    float roughness;//粗糙度
};

//电介质的反射率平均约0.04
#define MIN_REFLECTIVITY 0.04

float OneMinuseReflectivity(float metallic)
{
    float range = 1.0 - MIN_REFLECTIVITY;
    return range - metallic * range;
}

//根据公式得到镜面反射强度
float SpecularStrength(Surface surface,BRDF brdf,Light light)
{
    float3 h = SafeNormalize(light.direction + surface.viewDirection);//半角向量
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

//获取给定表面的BRDF数据
BRDF GetBRDF(Surface surface,bool applyAlphaToDiffuse = false)
{
    BRDF brdf;
    float oneMinuseReflectivity = OneMinuseReflectivity(surface.metallic); //1.0 - surface.metallic;
    brdf.diffuse = surface.color * oneMinuseReflectivity;
    if (applyAlphaToDiffuse)
    {
        brdf.diffuse *= surface.alpha;
    }
    
    //表面颜色减去慢反射颜色
    //brdf.specular = surface.color - brdf.diffuse;
    //非金属不影响镜面反射颜色
        brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);
    
    //粗糙度和光滑度相反，只需要使用1减去光滑度即可
    //使用CommonMaterial内置方法
        float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
        brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    
        return brdf;
    
    }
#endif