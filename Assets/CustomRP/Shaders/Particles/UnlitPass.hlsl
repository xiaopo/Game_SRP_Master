#ifndef CUSTOM_UNLIT_PASS_Instance_INCLUDED
#define CUSTOM_UNLIT_PASS_Instance_INCLUDED


struct Attributes
{
    float3 psotion : POSITION;
    float4 color : COLOR;

#if defined(_FLIPBOOK_BLENDING)
    float4 baseUV : TEXCOORD0;//If flipbook blending is active both UV pairs are provided via TEXCOORD0
    float flipbookBlend : TEXCOORD1;
#else
    float2 baseUV : TEXCOORD0;
#endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 position : SV_Position;
#if defined(_VERTEX_COLORS)
    float4 color : VAR_COLOR;
#endif
    float2 baseUV : VAR_BASE_UV;
#if defined(_FLIPBOOK_BLENDING)
    float3 flipbookUVB : VAR_FLIPBOOK;
#endif

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
    
#if defined(_VERTEX_COLORS)
    output.color = input.color;
#endif

    output.baseUV.xy = TransformBaseUV(input.baseUV.xy);
#if defined(_FLIPBOOK_BLENDING)
    output.flipbookUVB.xy = TransformBaseUV(input.baseUV.zw);
    output.flipbookUVB.z = input.flipbookBlend;
#endif

    return output;
}

//framgment function
float4 UnlitPassFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    InputConfig c = GetInputConfig(input.baseUV);
    
#if defined(_VERTEX_COLORS)
    c.color = input.color;
#endif
#if defined(_FLIPBOOK_BLENDING)
    c.flipbookUVB = input.flipbookUVB;
    c.flipbookBlending = true;
#endif

    float4 color = GetBase(c);
    
#if defined(_CLIPPING)
    clip(color.a - GetCutoff(c));
 #endif
    return color;

}


#endif