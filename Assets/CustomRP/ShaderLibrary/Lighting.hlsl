#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

//计算入射光照
float3 IncomingLight(Surface surface,Light light)
{

    return saturate(dot(surface.normal, light.direction) * light.attenuation) * light.color;
}

float3 GetLighting(Surface surface,BRDF brdf,Light light)
{
    //Li * Fbrdf 
    return IncomingLight(surface, light) * DirectBRDF(surface,brdf,light);
}

//根据物体的表面信息获取最终光照结果
float3 GetLighting(Surface surface,BRDF brdf,GI gi)
{

    //可见方向光的照明结果进行累加得到最终照明结果
    ShadowData shadowData = GetShadowData(surface);
    shadowData.shadowMask = gi.shadowMask;

    float3 color = IndirectBRDF(surface, brdf, gi.diffuse, gi.specular);
    for (int i = 0; i < GetDirectionLightCount();i++)
    {
        //Get Color and direction by light index 
        Light light = GetDirectionLight(i, surface, shadowData);
        if (RenderingLayersOverlap(surface, light)) {
            color += GetLighting(surface, brdf, light);
        }

    }
    
    //point light and spot light
    #if defined(_LIGHTS_PER_OBJECT)
    /**
     * Using the Indices
     * 
     *  real4 unity_LightData; //which contains the amount of lights in its Y component.  
     * 
     *  real4 unity_LightIndices[2];
     * 
     * unity_LightIndices ,which is an array of length two.
     * Each channel of the two vectors contains a light index,
     * so up to eight are supported per object.
     * 
     * **/
        for (int j = 0; j < min(unity_LightData.y, 8); j++)
        {
            /**
             * In this case the amount of lights is found via unity_LightData.y 
             * and the light index has to be retrieved from the appropriate element and component of unity_LightIndices. 
             * We can get the correct vector by dividing the iterator by 4 and the correct component via modulo 4.
             * **/
            int lightIndex = unity_LightIndices[(uint)j / 4][(uint) j % 4];
            
			Light light = GetOtherLight(lightIndex, surface, shadowData);
            if (RenderingLayersOverlap(surface, light)) {
                color += GetLighting(surface, brdf, light);
            }
		}

    /**
     * Note that with lights-per-object enabled GPU instancing is less efficient, 
     * because only objects whose light counts and index lists match are grouped. 
     * The SRP batcher isn't affected, because each object still gets its own optimized draw call.
     * **/
	#else
        for (int j = 0; j < GetOtherLightCount(); j++)
        {
            Light light = GetOtherLight(j, surface, shadowData);
            if (RenderingLayersOverlap(surface, light)) {
                color += GetLighting(surface, brdf, light);
            }
        }
    #endif
    
     return color;
}
#endif