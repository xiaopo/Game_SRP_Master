

using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
namespace CustomSR
{
    public class Lighting
    {
        const string bufferName = "Lighting";

        CommandBuffer buffer = new CommandBuffer
        {
            name = bufferName
        };

        //static int dirLightColorId = Shader.PropertyToID("_DirectionalLightColor");
        //static int dirLightDirectionId = Shader.PropertyToID("_DirectionlLightDirection");

        //定义最大可见直接光
        const int maxDirLightCount = 4;
        const int maxOtherLightCount = 64;
        static int dirLightCountId = Shader.PropertyToID("_DirectionLightCount");
        static int dirLightColorsId = Shader.PropertyToID("_DirectionLightColors");
        static int dirLightDirectionsAndMasksId = Shader.PropertyToID("_DirectionalLightDirectionsAndMasks");
        static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");


        static int otherLightCountId = Shader.PropertyToID("_OtherLightCount");
        static int otherLightColorsId = Shader.PropertyToID("_OtherLightColors");
        static int otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions");
        static int otherLightDirectionsAndMasksId = Shader.PropertyToID("_OtherLightDirectionsAndMasks");
        static int otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles");
        static int otherLightShadowDataId = Shader.PropertyToID("_OtherLightShadowData");

        //储存可见光的颜色和方向
        static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
        static Vector4[] dirLightDirectionsAndMasks = new Vector4[maxDirLightCount];
        static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];

        static Vector4[] otherLightColors = new Vector4[maxOtherLightCount];
        static Vector4[] otherLightPositions = new Vector4[maxOtherLightCount];
        static Vector4[] otherLightDirectionsAndMasks = new Vector4[maxOtherLightCount];
        static Vector4[] otherLightSpotAngles = new Vector4[maxOtherLightCount];
        static Vector4[] otherLightShadowData = new Vector4[maxOtherLightCount];

        static string lightsPerObjectKeyword = "_LIGHTS_PER_OBJECT";

        //裁剪信息 
        CullingResults cullingResults;

        Shadows shadows = new Shadows();
        public void Setup(ScriptableRenderContext context, CullingResults cullingResults, 
            ShadowSettings shadowSettings, bool useLightsPerObject, int renderingLayerMask)
        {
            this.cullingResults = cullingResults;

            buffer.BeginSample(bufferName);
            //传递阴影数据
            shadows.Setup(context, cullingResults, shadowSettings);
            //发送光源数据
            SetupLights(useLightsPerObject, renderingLayerMask);
            //渲染阴影
            shadows.Render();
            buffer.EndSample(bufferName);

            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        void SetupLights(bool useLightsPerObject, int renderingLayerMask)
        {
            //Unity 会在剔除阶段计算哪些光源会影响相机的可见性
            //得到所有可见光
            NativeArray<int> indexMap = useLightsPerObject  ? cullingResults.GetLightIndexMap(Allocator.Temp) :default;

            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            if (visibleLights == null) return;

            int dirLightCount = 0, otherLightCount = 0;
            int i;
            for (i = 0; i < visibleLights.Length; i++)
            {
                VisibleLight visibleLight = visibleLights[i];
                Light light = visibleLight.light;
                int newIndex = -1;
                if ((light.renderingLayerMask & renderingLayerMask) != 0)
                {
                    switch (visibleLight.lightType)
                    {
                        case LightType.Directional:
                            if (dirLightCount < maxDirLightCount)
                            {
                                SetupDirectionalLight(dirLightCount++, i, ref visibleLight, light);
                            }
                            break;
                        case LightType.Point:
                            if (otherLightCount < maxOtherLightCount)
                            {
                                newIndex = otherLightCount;
                                SetupPointLight(otherLightCount++, i, ref visibleLight, light);
                            }
                            break;
                        case LightType.Spot:
                            if (otherLightCount < maxOtherLightCount)
                            {
                                newIndex = otherLightCount;
                                SetupSpotLight(otherLightCount++, i, ref visibleLight, light);
                            }
                            break;
                    }
                }
                if (useLightsPerObject) {
                    indexMap[i] = newIndex;  
                }
            }

            if (useLightsPerObject)
            {
                for (; i < indexMap.Length; i++){
                    indexMap[i] = -1;
                }
                //把剔除后的可见光设置回CullingResults.
                if(indexMap.Length > 0)cullingResults.SetLightIndexMap(indexMap);
                indexMap.Dispose();
                Shader.EnableKeyword(lightsPerObjectKeyword);
            }
            else{
                Shader.DisableKeyword(lightsPerObjectKeyword);
            }

            buffer.SetGlobalInt(dirLightCountId, dirLightCount);
            if (dirLightCount > 0)
            {
                //方向光
                buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
                buffer.SetGlobalVectorArray(dirLightDirectionsAndMasksId, dirLightDirectionsAndMasks);
                buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
            }

           
            buffer.SetGlobalInt(otherLightCountId, otherLightCount);
            if(otherLightCount > 0)
            {
                //point and spot light
                buffer.SetGlobalVectorArray(otherLightColorsId, otherLightColors);
                buffer.SetGlobalVectorArray(otherLightPositionsId, otherLightPositions); 
                buffer.SetGlobalVectorArray(otherLightDirectionsAndMasksId, otherLightDirectionsAndMasks);
                buffer.SetGlobalVectorArray(otherLightSpotAnglesId, otherLightSpotAngles);

                buffer.SetGlobalVectorArray(otherLightShadowDataId, otherLightShadowData);

            }
        }

        void SetupDirectionalLight (int index, int visibleIndex, ref VisibleLight visibleLight, Light light)
        {
            if (index >= maxDirLightCount) return;

            dirLightColors[index] = visibleLight.finalColor;
            //第三列取反得到light direction
            Vector4 dirAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);
            dirAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();
            dirLightDirectionsAndMasks[index] = dirAndMask;
            dirLightShadowData[index] = shadows.ReserveDirectionalShadows(light, visibleIndex);
        }

        void SetupOtherLightPosition(int index, ref VisibleLight visibleLight)
        {
            otherLightColors[index] = visibleLight.finalColor * visibleLight.light.intensity;

            //最后一列平移量
            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);

            otherLightPositions[index] = position;
        }

        // point light
        void SetupPointLight(int index, int visibleIndex, ref VisibleLight visibleLight,Light light)
        {
            SetupOtherLightPosition(index, ref visibleLight);
            otherLightSpotAngles[index] = new Vector4(0f, 1f);

            Vector4 dirAndmask = Vector4.zero;
            dirAndmask.w = light.renderingLayerMask.ReinterpretAsFloat();
            otherLightDirectionsAndMasks[index] = dirAndmask;
           // Light light = visibleLight.light;
            otherLightShadowData[index] = shadows.ReserveOtherShadows(light, index);

        }

        //spot light
        void SetupSpotLight(int index, int visibleIndex, ref VisibleLight visibleLight, Light light)
        {
            SetupOtherLightPosition(index, ref visibleLight);

            //第三列取反可得光照方向

            Vector4 dirAndMask = -visibleLight.localToWorldMatrix.GetColumn(2);
            dirAndMask.w = light.renderingLayerMask.ReinterpretAsFloat();
            otherLightDirectionsAndMasks[index] = dirAndMask;


            //Light light = visibleLight.light;

            /**
             * formula
             * R1:inner angle ,R0 outer angle
             * attenuation =  saturate(d * a + b)^2 
             * 
             * d]: is the dot product
             * a]: 1 / cos(R1 * 0.5) - cos(R0 * 0.5)
             * b]: -cos(R0 * 0.5) * a
             * **/

            //内角，光线开始渐变的地方
            float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
            //外角，光线强度值变为0
            float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);

            //parameter "a"
            float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
            //parameter "b"
            float paramB = -outerCos * angleRangeInv;

            //reserve to x,y component
            otherLightSpotAngles[index] = new Vector4(angleRangeInv, paramB);

            //shadow data
            otherLightShadowData[index] = shadows.ReserveOtherShadows(light, index);
        }


        public void Cleanup()
        {
            shadows.Cleanup();
        }
    }
}