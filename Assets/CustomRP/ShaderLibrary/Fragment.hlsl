#ifndef FRAGMENT_INCLUDED
#define FRAGMENT_INCLUDED

TEXTURE2D(_CameraColorTexture);
SAMPLER(sampler_CameraColorTexture);

TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_point_clamp);

float4 _CameraBufferSize;

struct Fragment 
{
	float2 positionSS;
	float2 screenUV;
	float depth;
	float bufferDepth;
};

/*
*Perspective matrix
* 
*SV_Position 语义
*vertex function: clip-space position ,as 4D homogeneous coordinates
*fragment function: screen-space as known as window-space
*/

/*
*The fragment depth is stored in the last component of the screen-space position vector. 
*It's the value that was used to perform the perspective division to project 3D positions onto the screen. 
*This is the view-space depth, so it's the distance from the camera XY plane, not its near plane.
*/

Fragment GetFragment(float4 positionSS) 
{
	Fragment f;
	f.positionSS = positionSS.xy;
	//_ScreenParams
    f.screenUV = f.positionSS * _CameraBufferSize.xy;
	
	f.depth = IsOrthographicCamera() ? OrthographicDepthBufferToLinear(positionSS.z) : positionSS.w;
	f.bufferDepth = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, sampler_point_clamp, f.screenUV, 0);
	//f.bufferDepth = LOAD_TEXTURE2D(_CameraDepthTexture, f.positionSS).r;
	f.bufferDepth = IsOrthographicCamera() ? OrthographicDepthBufferToLinear(f.bufferDepth) :LinearEyeDepth(f.bufferDepth, _ZBufferParams);

	return f;
}

float4 GetBufferColor(Fragment fragment, float2 uvOffset = float2(0.0, 0.0))
{
	float2 uv = fragment.screenUV + uvOffset;

	return SAMPLE_TEXTURE2D_LOD(_CameraColorTexture, sampler_CameraColorTexture, uv,0);
}

#endif