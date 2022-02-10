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
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Packing.hlsl"

//orthographic: 1 ,otherwise: 0
bool IsOrthographicCamera() {
    return unity_OrthoParams.w;
}

/*
* For an orthographic camera the best we can do is rely on the Z component of the screen-space position vector, 
* which contains the converted clip-space depth of the fragment. 
* This is the raw value that is used for depth comparisons and is written to the depth buffer if depth writing is enabled. 
* It's a value in the 0¨C1 range and is linear for orthographic projections. 
* To convert it to view-space depth we have to scale it by the camera's near¨Cfar range and then add the near plane distance. 
* The near and far distances are stored in the Y and Z components of _ProjectionParams 
*/

float OrthographicDepthBufferToLinear(float rawDepth) 
{
#if UNITY_REVERSED_Z//We also need to reverse the raw depth if a reversed depth buffer is used
    rawDepth = 1.0 - rawDepth;
#endif
    return (_ProjectionParams.z - _ProjectionParams.y) * rawDepth + _ProjectionParams.y;
}
#include "Fragment.hlsl"

float Square(float v)
{
    return v * v;
}

float3 DecodeNormal(float4 sample, float scale)
{
#if defined(UNITY_NO_DXT5nm)
	    return UnpackNormalRGB(sample, scale);
#else
    return UnpackNormalmapRGorAG(sample, scale);
#endif
}

float3 NormalTangentToWorld(float3 normalTs, float3 normalWS, float4 tangentWS)
{
    float3x3 tangentToWorld = CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
    
    //real sgn = tangentWS * GetOddNegativeScale();
    //real3 bitangent = cross(normalTS, tangentWS.xyz) * sgn;
    //float3x3 tangentToWorld = real3x3(tangent, bitangent, normal);
    
    return mul(normalTs, tangentToWorld);
}

float DistanceSquared(float3 pA, float3 pB)
{
    float3 D = pA - pB;
    return dot(D, D);
}

void ClipLOD(Fragment fragment,float fade)
{
#if defined(LOD_FADE_CROSSFADE)
	//float dither = (fragment.positionCS.y % 32) / 32;
    float dither = InterleavedGradientNoise(fragment.positionSS, 0);
    clip(fade + (fade < 0.0 ? dither : -dither));
#endif
}



#endif