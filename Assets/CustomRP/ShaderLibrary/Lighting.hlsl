#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED


//计算入射光照
float3 IncomingLight(Surface surface,Light light)
{
    //兰伯特光照
    return saturate(dot(surface.normal, light.direction) * light.attenuation) * light.color;

}

float3 GetLighting(Surface surface,BRDF brdf,Light light)
{
    return IncomingLight(surface, light) * DirectBRDF(surface,brdf,light);
}


//根据物体的表面信息获取最终光照结果
float3 GetLighting(Surface surface,BRDF brdf)
{
    //return GetLighting(surface, GetDirectionLight());
    //可见方向光的照明结果进行累加得到最终照明结果
    ShadowData shadowData = GetShadowData(surface);
    float3 color = 0.0;
    for (int i = 0; i < GetDirectionLightCount();i++)
    {
        Light light = GetDirectionLight(i, surface, shadowData);
        color += GetLighting(surface, brdf, light);
    }
    
    return color;

}

#endif