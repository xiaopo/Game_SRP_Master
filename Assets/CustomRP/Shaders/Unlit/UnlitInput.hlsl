#ifndef CUSTOM_UNLIT_INPUT_INCLUDED
#define CUSTOM_UNLIT_INPUT_INCLUDED

TEXTURE2D(_MainTex);//定义一张2D文理
SAMPLER(sampler_MainTex);//指定一个采样器

//纹理和采样器是全局资源，不能放入缓冲区中
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)

UNITY_DEFINE_INSTANCED_PROP(float4,_MainColor)
UNITY_DEFINE_INSTANCED_PROP(float4,_MainTex_ST)
UNITY_DEFINE_INSTANCED_PROP(float,_Cutoff)

UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

struct InputConfig
{
    float4 color;
    float2 baseUV;
    float3 flipbookUVB;
    bool flipbookBlending;
    Fragment fragment;
};

InputConfig GetInputConfig(float4 positionSS,float2 baseUV)
{
    InputConfig c;
    c.fragment = GetFragment(positionSS);
    c.color = 1.0;
    c.baseUV = baseUV;
    c.flipbookUVB = 0.0;
    c.flipbookBlending = false;
    return c;
}

float2 TransformBaseUV(float2 baseUV)
{
    float4 baseST = INPUT_PROP(_MainTex_ST);
    return baseUV * baseST.xy + baseST.zw;
}

float4 GetMask(InputConfig c)
{
    return float4(1, 1, 1, 1);
}

float4 GetBase(InputConfig c)
{
    float4 mainMap = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, c.baseUV);

    if (c.flipbookBlending)
    {
        mainMap = lerp(mainMap, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, c.flipbookUVB.xy),c.flipbookUVB.z);
    }

    float4 color = INPUT_PROP(_MainColor);
    return mainMap * color * c.color;
}

float3 GetEmission(InputConfig c)
{
    return GetBase(c).rgb;
}

float GetFresnel(InputConfig c)
{
    return 0.0;
}

float GetCutoff(InputConfig c)
{
    return INPUT_PROP(_Cutoff);
}

float GetMetallic(InputConfig c)
{
    return 0.0;
}

float GetSmoothness(InputConfig c)
{
    return 0.0;
}



#endif