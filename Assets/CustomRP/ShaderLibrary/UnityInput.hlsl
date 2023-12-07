#ifndef CUSTOM_UNITY_INPUT_INCLUDED
#define CUSTOM_UNITY_INPUT_INCLUDED

CBUFFER_START(UnityPerDraw)
    float4x4 unity_ObjectToWorld;
    float4x4 unity_WorldToObject;
    float4x4 UNITY_PREV_MATRIX_M;
    float4x4 UNITY_PREV_MATRIX_I_M;
    
    float4 unity_LODFade;
    real4 unity_WorldTransformParams;
    float4 unity_RenderingLayer;

    float4 unity_ProbesOcclusion;
    float4 unity_SpecCube0_HDR;

    float4 unity_LightmapST;
    float4 unity_DynamicLightmapST;
    
    float4 unity_SHAr;
    float4 unity_SHAg;
    float4 unity_SHAb;
    float4 unity_SHBr;
    float4 unity_SHBg;
    float4 unity_SHBb;
    float4 unity_SHC;

  

    real4 unity_LightData;
    real4 unity_LightIndices[2];

    float4 unity_ProbeVolumeParams;
    float4x4 unity_ProbeVolumeWorldToObject;
    float4 unity_ProbeVolumeSizeInv;
    float4 unity_ProbeVolumeMin;
 
CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;

//World space position of the camera.
float3 _WorldSpaceCameraPos;
//x is 1.0 (or –1.0 if currently rendering with a flipped projection matrix), y is the camera’s near plane, z is the camera’s far plane and w is 1/FarPlane.
float4 _ProjectionParams;
//x is orthographic camera’s width, y is orthographic camera’s height, z is unused and w is 1.0 when camera is orthographic, 0.0 when perspective.
float4 unity_OrthoParams;
//x is the width of the camera’s target texture in pixels, y is the height of the camera’s target texture in pixels, z is 1.0 + 1.0 / width and w is 1.0 + 1.0 / height.
float4 _ScreenParams;
//Used to linearize Z buffer values. x is (1-far/near), y is (far/near), z is (x/far) and w is (y/far).
float4 _ZBufferParams;

#endif