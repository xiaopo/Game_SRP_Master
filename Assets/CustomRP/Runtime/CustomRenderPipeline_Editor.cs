﻿using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using LightType = UnityEngine.LightType;

namespace CustomSR
{
    public partial class CustomRenderPipeline
    {
        partial void InitializeForEditor();

#if UNITY_EDITOR

        partial void InitializeForEditor()
        {
            Lightmapping.SetDelegate(lightsDelegate);
        }
        /**
         * We can tell Unity to use a different falloff, 
         * by providing a delegate to a method that should get invoked before Unity performs lightmapping in the editor. 
         **/
        // lambda expression
        static Lightmapping.RequestLightsDelegate lightsDelegate = (Light[] lights, NativeArray<LightDataGI> output) =>
        //static void lightsDelegateFuns(Light[] lights, NativeArray<LightDataGI> output)
        {
            var lightData = new LightDataGI();
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                switch (light.type)
                {
                    case LightType.Directional:
                        var directionalLight = new DirectionalLight();
                        LightmapperUtils.Extract(light, ref directionalLight);
                        lightData.Init(ref directionalLight);
                        break;
                    case LightType.Point:
                        var pointLight = new PointLight();
                        LightmapperUtils.Extract(light, ref pointLight);
                        lightData.Init(ref pointLight);
                        break;
                    case LightType.Spot:
                        var spotLight = new SpotLight();
                        LightmapperUtils.Extract(light, ref spotLight);
                        spotLight.innerConeAngle = light.innerSpotAngle * Mathf.Deg2Rad;
                        spotLight.angularFalloff = AngularFalloffType.AnalyticAndInnerAngle;
                        lightData.Init(ref spotLight);
                        break;
                    case LightType.Area:
                        var rectangleLight = new RectangleLight();
                        LightmapperUtils.Extract(light, ref rectangleLight);
                        rectangleLight.mode = LightMode.Baked;
                        lightData.Init(ref rectangleLight);
                        break;
                    default:
                        lightData.InitNoBake(light.GetInstanceID());
                        break;
                }

                lightData.falloff = FalloffType.InverseSquared;
                output[i] = lightData;
                
            }
        };

        //protected override void Dispose(bool disposing)
        //{
        //    base.Dispose(disposing);
        //    Lightmapping.ResetDelegate();
        //}
        partial void DisposeForEditor()
        {
            //base.Dispose(disposing);
            Lightmapping.ResetDelegate();
        }
#endif

        partial void DisposeForEditor();
    }
}
