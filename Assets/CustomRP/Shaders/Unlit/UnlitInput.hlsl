#ifndef CUSTOM_UNLIT_INPUT_INCLUDED
#define CUSTOM_UNLIT_INPUT_INCLUDED

TEXTURE2D(_MainTex);//����һ��2D����
SAMPLER(sampler_MainTex);//ָ��һ��������

//����Ͳ�������ȫ����Դ�����ܷ��뻺������
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(float4,_MainColor)
UNITY_DEFINE_INSTANCED_PROP(float4,_MainTex_ST)
UNITY_DEFINE_INSTANCED_PROP(float,_Cutoff)

UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)


float2 TransformBaseUV(float2 baseUV)
{
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainTex_ST);
    return baseUV * baseST.xy + baseST.zw;
}

float4 GetBase(float2 baseUV)
{
    float4 map = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, baseUV);
    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MainColor);
    return map * color;
}

float3 GetEmission(float2 baseUV)
{
    return GetBase(baseUV).rgb;
}

float GetCutoff(float2 baseUV)
{
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
}

float GetMetallic(float2 baseUV)
{
    return 0.0;
}

float GetSmoothness(float2 baseUV)
{
    return 0.0;
}



#endif