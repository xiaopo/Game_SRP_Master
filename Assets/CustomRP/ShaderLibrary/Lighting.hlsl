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
float3 GetLighting(Surface surface,BRDF brdf,GI gi)
{

    //可见方向光的照明结果进行累加得到最终照明结果
    ShadowData shadowData = GetShadowData(surface);
    shadowData.shadowMask = gi.shadowMask;

    //float3 color = gi.diffuse * brdf.diffuse;
    float3 color = IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
    for (int i = 0; i < GetDirectionLightCount();i++)
    {
        //Get Color and direction by light index 
        Light light = GetDirectionLight(i, surface, shadowData);
        color += GetLighting(surface, brdf, light);
    }
    
    return color;
}

#endif