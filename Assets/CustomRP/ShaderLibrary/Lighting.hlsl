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
    return GetLighting(surface, GetDirectionLight());
}

#endif