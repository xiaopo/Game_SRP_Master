Shader "Hidden/CustomRP/CameraRenderer" 
{
	Properties
	{
		[Enum(UnityEngine.Rendering.BlendMode)] _CameraSrcBlend("Src Blend",Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _CameraDstBlend("Dst Blend",Float) = 0
	}

	SubShader
	{
		Cull Off
		ZTest Always
		ZWrite Off

		HLSLINCLUDE
		#include "../ShaderLibrary/Common.hlsl"
		#include "CameraRendererPasses.hlsl"
		ENDHLSL

		Pass 
		{
			Name "Copy"
			Blend[_CameraSrcBlend][_CameraDstBlend]
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment CopyPassFragment
			ENDHLSL
		}

		Pass {
			Name "Copy Depth"

			ColorMask 0
			ZWrite On

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment CopyDepthPassFragment
			ENDHLSL
		}
	}
}