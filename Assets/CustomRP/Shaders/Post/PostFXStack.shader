Shader"Hidden/Custom RP/Post FX Stack" 
{
	
	SubShader 
	{
		Cull Off
		ZTest Always
		ZWrite Off
		
		HLSLINCLUDE
			#include "../../ShaderLibrary/Common.hlsl"
			#include "PostFXStackPasses.hlsl"
		ENDHLSL

		Pass {
			Name "Bloom Horizontal"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomHorizontalPassFragment
			ENDHLSL
		}

		Pass 
		{
			Name"Copy"
			
			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment CopyPassFragment
			ENDHLSL
		}

		Pass{
				Name "Final"

				HLSLPROGRAM
					#pragma target 3.5
					#pragma vertex DefaultPassVertex
					#pragma fragment FinalPassFragment 
				ENDHLSL
		}

		Pass {
			Name "Bloom Vertical"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomVerticalPassFragment
			ENDHLSL
		}

		Pass {
			Name "Bloom Combine"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomCombinePassFragment
			ENDHLSL
		}

		Pass {
			Name "Bloom Prefilter"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomPrefilterPassFragment
			ENDHLSL
		}
		
		Pass {
			Name "Bloom PrefilterFireflies"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomPrefilterFirefliesPassFragment
			ENDHLSL
		}

		Pass {
			Name "Bloom Scatter"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomScatterPassFragment 
			ENDHLSL
		}

		Pass {
			Name "Bloom ScatterFinal"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment BloomScatterFinalPassFragment 
			ENDHLSL
		}
	
		Pass {
			Name "Bloom ColorGradingNone"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ColorGradingNonePassFragment
			ENDHLSL
		}

		Pass {
			Name "Bloom MappingACES"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ColorGradingACESPassFragment
			ENDHLSL
		}

		Pass {
			Name "Bloom MappingNeutral"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ColorGradingNeutralPassFragment 
			ENDHLSL
		}

		Pass {
			Name "Bloom MappingReinhard"

			HLSLPROGRAM
				#pragma target 3.5
				#pragma vertex DefaultPassVertex
				#pragma fragment ColorGradingReinhardPassFragment 
			ENDHLSL
		}
		
	}
}