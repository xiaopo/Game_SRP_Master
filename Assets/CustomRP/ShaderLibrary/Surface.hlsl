#ifndef CUSTOM_SURFACE_INCLUDED
#define CUSTOM_SURFACE_INCLUDED
//表面信息
struct Surface
{
    float3 position;//at world space
    float3 normal;//世界空间
    float3 color;//漫反射颜色
    float alpha;//漫反射alpha
    float metallic;//金属度
    float smoothness;//光滑度
    float3 viewDirection;//视角
    float depth;
    float dither;
};

#endif