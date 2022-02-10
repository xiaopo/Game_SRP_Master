#ifndef CUSTOM_UNLIT_PASS_Instance_INCLUDED
#define CUSTOM_UNLIT_PASS_Instance_INCLUDED


struct Attributes
{
    float3 psotion : POSITION;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    
};

struct Varyings
{
    float4 position : SV_Position;
    float2 baseUV : VAR_BASE_UV;
    
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

//vertex function
Varyings UnlitPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    float3 positionWS = TransformObjectToWorld(input.psotion);
    output.position = TransformWorldToHClip(positionWS);
    

    output.baseUV = TransformBaseUV(input.baseUV);
    return output;

}

//framgment function
float4 UnlitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    InputConfig config = GetInputConfig(input.position,input.baseUV);
    float4 color = GetBase(config);
    
#if defined(_CLIPPING)
    clip(color.a - GetCutoff(config));
 #endif
    return color;

}


#endif