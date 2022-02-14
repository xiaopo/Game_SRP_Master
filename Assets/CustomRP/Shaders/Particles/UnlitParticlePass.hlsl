#ifndef CUSTOM_UNLIT_PASS_PARTICLE_INCLUDED
#define CUSTOM_UNLIT_PASS_PARTICLE_INCLUDED

/*
Now the distorted color texture samples fade as well, 
which makes the undistorted background and other particles partially visible again. 
The result is a smooth mess that doesn't make physical sense but is enough to provide the illusion of atmospheric refraction.
This can be improved further by tweaking the distortion strength along with smoothly fading particles in and out by adjusting their color during their lifetime.
Also, the offset vectors are aligned with the screen and aren't affected by the orientation of the particle. 
So if the particles are set to rotate during their lifetime their individual distortion patterns will appear to twist.
*/

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
    float4 positionCS_SS : SV_Position;
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
    output.positionCS_SS = TransformWorldToHClip(positionWS);
    
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
    InputConfig config = GetInputConfig(input.positionCS_SS,input.baseUV);

    //return GetBufferColor(config.fragment, 0.05);
    //return float4(config.fragment.bufferDepth.xxx / 20.0, 1.0);

#if defined(_VERTEX_COLORS)
    config.color = input.color;
#endif
#if defined(_FLIPBOOK_BLENDING)
    config.flipbookUVB = input.flipbookUVB;
    config.flipbookBlending = true;
#endif

#if defined(_NEAR_FADE)
    config.nearFade = true;
#endif
#if defined(_SOFT_PARTICLES)
    config.softParticles = true;
#endif
    float4 color = GetBase(config);
    
#if defined(_CLIPPING)
    clip(color.a - GetCutoff(config));
 #endif

#if defined(_DISTORTION)

    float2 distortion = GetDistortion(config) * color.a;
    color.rgb = lerp(GetBufferColor(config.fragment, distortion).rgb, color.rgb, saturate(color.a - GetDistortionBlend(config)));
   
#endif

    return color;

}

#endif