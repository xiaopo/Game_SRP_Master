#ifndef CUSTOM_UNLIT_INPUT_INCLUDED
#define CUSTOM_UNLIT_INPUT_INCLUDED

TEXTURE2D(_DistortionMap);
SAMPLER(sampler_DistortionMap);

TEXTURE2D(_MainTex);//定义一张2D文理
SAMPLER(sampler_MainTex);//指定一个采样器

//纹理和采样器是全局资源，不能放入缓冲区中
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)

UNITY_DEFINE_INSTANCED_PROP(float4,_MainColor)
UNITY_DEFINE_INSTANCED_PROP(float4,_MainTex_ST)
UNITY_DEFINE_INSTANCED_PROP(float,_Cutoff)
UNITY_DEFINE_INSTANCED_PROP(float, _ZWrite)
UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeDistance)
UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeRange)
UNITY_DEFINE_INSTANCED_PROP(float, _SoftParticlesDistance)
UNITY_DEFINE_INSTANCED_PROP(float, _SoftParticlesRange)
UNITY_DEFINE_INSTANCED_PROP(float, _DistortionStrength)
UNITY_DEFINE_INSTANCED_PROP(float, _DistortionBlend)

UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

#define INPUT_PROP(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

struct InputConfig
{
    float4 color;
    float2 baseUV;
    float3 flipbookUVB;
    bool flipbookBlending;
    Fragment fragment;
    bool nearFade;
    bool softParticles;
};

InputConfig GetInputConfig(float4 positionSS,float2 baseUV)
{
    InputConfig c;
    c.fragment = GetFragment(positionSS);
    c.color = 1.0;
    c.baseUV = baseUV;
    c.flipbookUVB = 0.0;
    c.flipbookBlending = false;
    c.nearFade = false;
    c.softParticles = false;
    return c;
}

float GetFinalAlpha(float alpha) {
    return INPUT_PROP(_ZWrite) ? 1.0 : alpha;
}

float GetDistortionBlend(InputConfig c) {
    return INPUT_PROP(_DistortionBlend);
}

float2 GetDistortion(InputConfig c) 
{
    float4 rawMap = SAMPLE_TEXTURE2D(_DistortionMap, sampler_DistortionMap, c.baseUV);

    if (c.flipbookBlending) {
        rawMap = lerp(rawMap, SAMPLE_TEXTURE2D(_DistortionMap, sampler_DistortionMap, c.flipbookUVB.xy),c.flipbookUVB.z);
    }

    return DecodeNormal(rawMap, INPUT_PROP(_DistortionStrength)).xy;
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
        //
        mainMap = lerp(mainMap, SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, c.flipbookUVB.xy),c.flipbookUVB.z);
    }

    if (c.nearFade) 
    {
        float nearAttenuation = (c.fragment.depth - INPUT_PROP(_NearFadeDistance)) / INPUT_PROP(_NearFadeRange);
        mainMap.a *= saturate(nearAttenuation);
    }

    if (c.softParticles) 
    {
        float depthDelta = c.fragment.bufferDepth - c.fragment.depth;
        float nearAttenuation = (depthDelta - INPUT_PROP(_SoftParticlesDistance)) / INPUT_PROP(_SoftParticlesRange);
        mainMap.a *= saturate(nearAttenuation);
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