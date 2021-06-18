#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED


//计算入射光照
float3 IncomingLight(Surface surface,Light light)
{
    //兰伯特光照
    return saturate(dot(surface.normal, light.direction)) * light.color;
}

float3 GetLighting(Surface surface, Light light)
{
    return IncomingLight(surface, light) * surface.color;
}


//根据物体的表面信息获取最终光照结果
float3 GetLighting(Surface surface)
{
    //return GetLighting(surface, GetDirectionLight());
    //可见方向光的照明结果进行累加得到最终照明结果
    float3 color = 0.0;
    for (int i = 0; i < GetDirectionLightCount();i++)
    {
        color += GetLighting(surface, GetDirectionLight(i));
    }
    
    return color;

}

#endif