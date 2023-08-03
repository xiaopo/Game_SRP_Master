#ifndef CUSTOM_SHADOWCASTER_PASS_INCLUDED
#define CUSTOM_SHADOWCASTER_PASS_INCLUDED

struct Attributes
{
    float3 position : POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 position : SV_Position;
    float2 uv : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
bool _ShadowPancaking;
Varyings ShadowCasterPassVertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    float3 worldPos = TransformObjectToWorld(input.position);
    output.position = TransformWorldToHClip(worldPos);
    
    if (_ShadowPancaking)
    {
        //Shadow Pancaking
        #if UNITY_REVERSED_Z
            output.position.z = min(output.position.z, output.position.w * UNITY_NEAR_CLIP_VALUE);
        #else
            output.position.z = max(output.position.z, output.position.w * UNITY_NEAR_CLIP_VALUE);
        #endif
    }
    output.uv = TransformBaseUV(input.uv);
    return output;
}


void ShadowCasterPassFragment(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    

    InputConfig config = GetInputConfig(input.position,input.uv);

    ClipLOD(config.fragment, unity_LODFade.x);
   
    float4 albedo = GetBase(config);
    
#if defined(_SHADOWS_CLIP)
        clip(albedo.a - GetCutoff(config));
#elif defined(_SHADOWS_DITHER)
		float dither = InterleavedGradientNoise(config.fragment.positionSS, 0);
		clip(baseMap.a - dither);
#endif
    
}

#endif