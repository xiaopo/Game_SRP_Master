#ifndef CUSTOM_SURFACE_INCLUDED
#define CUSTOM_SURFACE_INCLUDED
//������Ϣ
struct Surface
{
    float3 position;//at world space
    float3 normal;//����ռ�
    float3 color;//��������ɫ
    float alpha;//������alpha
    float metallic;//������
    float smoothness;//�⻬��
    float3 viewDirection;//�ӽ�
    float depth;
    float dither;
};

#endif