#ifndef CUSTOM_FXAA_PASS_INCLUDED
#define CUSTOM_FXAA_PASS_INCLUDED

float GetLuma(float2 uv)
{
    //gamama-adjusted 2.2,this case get a approximate value is 2
    //return sqrt(Luminance(GetSource(uv)));
#if defined(FXAA_ALPHA_CONTAINS_LUMA)
		return GetSource(uv).a;
#else
    return GetSource(uv).g;
#endif
}

float4 FXAAPassFragment(Varyings input) : SV_TARGET
{
    return GetLuma(input.screenUV);
}

#endif