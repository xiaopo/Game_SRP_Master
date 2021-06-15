#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

#include "../ShaderLibrary/Common.hlsl"


CBUFFER_START(UnityPerMaterial)
    float4 _MainColor;
CBUFFER_END



struct Attributes
{
    float3 psotion : POSITION;


    
};

struct Varyings
{
    float4 position : SV_Position;
    float2 baseUV : VAR_BASE_UV;
    

};

//vertex function
Varyings UnlitPassVertex(Attributes input)
{
    Varyings output;

    
    float3 positionWS = TransformObjectToWorld(input.psotion);
    output.position = TransformWorldToHClip(positionWS);
    
    return output;

}

//framgment function
float4 UnlitPassFragment(Varyings input) : SV_TARGET
{
    return _MainColor;

}


#endif