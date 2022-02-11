#ifndef FRAGMENT_INCLUDED
#define FRAGMENT_INCLUDED

TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_point_clamp);

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
*SV_Position ”Ô“Â
*vertex function: clip-space position ,as 4D homogeneous coordinates
*fragment function: screen-space as known as window-space
*/

/*
*The fragment depth is stored in the last component of the screen-space position vector. 
*It's the value that was used to perform the perspective division to project 3D positions onto the screen. 
*This is the view-space depth, so it's the distance from the camera XY plane, not its near plane.
*/

/*
*Orthographic 
*/
Fragment GetFragment(float4 positionSS) 
{
	Fragment f;
	f.positionSS = positionSS.xy;
	f.screenUV = f.positionSS / _ScreenParams.xy;
	f.depth = IsOrthographicCamera() ? OrthographicDepthBufferToLinear(positionSS.z) : positionSS.w;
	f.bufferDepth = SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, sampler_point_clamp, f.screenUV, 0);
	//f.bufferDepth = LOAD_TEXTURE2D(_CameraDepthTexture, f.positionSS).r;
	f.bufferDepth = IsOrthographicCamera() ? OrthographicDepthBufferToLinear(f.bufferDepth) :LinearEyeDepth(f.bufferDepth, _ZBufferParams);

	return f;
}

#endif