#ifndef CUSTOM_COMMON_INCLUDED
#define CUSTOM_COMMON_INCLUDED


#define UNITY_MATRIX_M   unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

#include "UnityInput.hlsl"

#if defined(_SHADOW_MASK_ALWAYS) || defined(_SHADOW_MASK_DISTANCE)
	#define SHADOWS_SHADOWMASK
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"


float Square(float v)
{
    return v * v;
}

float DistanceSquared(float3 pA, float3 pB)
{
    float3 D = pA - pB;
    return dot(D, D);
}
void ClipLOD(float2 positionCS, float fade)
{
#if defined(LOD_FADE_CROSSFADE)
	//float dither = (positionCS.y % 32) / 32;
    float dither = InterleavedGradientNoise(positionCS.xy, 0);
    clip(fade + (fade < 0.0 ? dither : -dither));
#endif
}

#endif