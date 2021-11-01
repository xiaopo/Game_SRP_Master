

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
        static int dirLightDirectionsId = Shader.PropertyToID("_DirectionLightDrections");
        static int dirLightShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");


        static int otherLightCountId = Shader.PropertyToID("_OtherLightCount");
        static int otherLightColorsId = Shader.PropertyToID("_OtherLightColors");
        static int otherLightPositionsId = Shader.PropertyToID("_OtherLightPositions");
        static int otherLightDirectionsId = Shader.PropertyToID("_OtherLightDirections");
        static int otherLightSpotAnglesId = Shader.PropertyToID("_OtherLightSpotAngles");
        static int otherLightShadowDataId = Shader.PropertyToID("_OtherLightShadowData");

        //储存可见光的颜色和方向
        static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
        static Vector4[] dirLightDirectioins = new Vector4[maxDirLightCount];
        static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];

        static Vector4[] otherLightColors = new Vector4[maxOtherLightCount];
        static Vector4[] otherLightPositions = new Vector4[maxOtherLightCount];
        static Vector4[] otherLightDirections = new Vector4[maxOtherLightCount];
        static Vector4[] otherLightSpotAngles = new Vector4[maxOtherLightCount];
        static Vector4[] otherLightShadowData = new Vector4[maxOtherLightCount];

        static string lightsPerObjectKeyword = "_LIGHTS_PER_OBJECT";

        //裁剪信息 
        CullingResults cullingResults;

        Shadows shadows = new Shadows();
        public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings, bool useLightsPerObject)
        {
            this.cullingResults = cullingResults;

            buffer.BeginSample(bufferName);
            //传递阴影数据
            shadows.Setup(context, cullingResults, shadowSettings);
            //发送光源数据
            SetupLights(useLightsPerObject);
            //渲染阴影
            shadows.Render();
            buffer.EndSample(bufferName);

            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        void SetupLights(bool useLightsPerObject)
        {
            //Unity 会在剔除阶段计算哪些光源会影响相机的可见性
            //得到所有可见光
            NativeArray<int> indexMap = useLightsPerObject  ? cullingResults.GetLightIndexMap(Allocator.Temp) :default;

            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            if (visibleLights == null) return;

            int dirLightCount = 0, otherLightCount = 0;
            int i = 0;
            for (i = 0; i < visibleLights.Length; i++)
            {
                VisibleLight visibleLight = visibleLights[i];
                int newIndex = -1;
                switch (visibleLight.lightType)
                {
                    case LightType.Directional:
                        if (dirLightCount < maxDirLightCount){
                            SetupDirectionalLight(dirLightCount++, ref visibleLight);
                        }
                        break;
                    case LightType.Point:
                        if (otherLightCount < maxOtherLightCount){
                            newIndex = otherLightCount;
                            SetupPointLight(otherLightCount++, ref visibleLight);
                        }
                        break;
                    case LightType.Spot:
                        if (otherLightCount < maxOtherLightCount){
                            newIndex = otherLightCount;
                            SetupSpotLight(otherLightCount++, ref visibleLight);
                        }
                        break;
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

                cullingResults.SetLightIndexMap(indexMap);
                indexMap.Dispose();
                Shader.EnableKeyword(lightsPerObjectKeyword);
            }
            else{
                Shader.DisableKeyword(lightsPerObjectKeyword);
            }


            if (dirLightCount > 0)
            {
                //方向光
                buffer.SetGlobalInt(dirLightCountId, dirLightCount);
                buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
                buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirectioins);
                buffer.SetGlobalVectorArray(dirLightShadowDataId, dirLightShadowData);
            }

           
            buffer.SetGlobalInt(otherLightCountId, otherLightCount);
            if(otherLightCount > 0)
            {
                //点光
                buffer.SetGlobalVectorArray(otherLightColorsId, otherLightColors);
                buffer.SetGlobalVectorArray(otherLightPositionsId, otherLightPositions);
                //聚光
                buffer.SetGlobalVectorArray(otherLightDirectionsId, otherLightDirections);
                buffer.SetGlobalVectorArray(otherLightSpotAnglesId, otherLightSpotAngles);

                buffer.SetGlobalVectorArray(otherLightShadowDataId, otherLightShadowData);

            }
        }

        void SetupDirectionalLight (int index, ref VisibleLight visibleLight)
        {
            if (index >= maxDirLightCount) return;
            dirLightColors[index] = visibleLight.finalColor;
            //第三列取反得到light direction
            dirLightDirectioins[index] = -visibleLight.localToWorldMatrix.GetColumn(2);

            dirLightShadowData[index] = shadows.ReserveDirectionalShadows(visibleLight.light, index);
        }


        void SetupOtherLightPosition(int index, ref VisibleLight visibleLight)
        {
            otherLightColors[index] = visibleLight.finalColor;

            //最后一列平移量
            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);

            otherLightPositions[index] = position;
        }

        // point light
        void SetupPointLight(int index, ref VisibleLight visibleLight)
        {
            SetupOtherLightPosition(index, ref visibleLight);
            otherLightSpotAngles[index] = new Vector4(0f, 1f);

            Light light = visibleLight.light;
            otherLightShadowData[index] = shadows.ReserveOtherShadows(light, index);
        }

        //spot light
        void SetupSpotLight(int index, ref VisibleLight visibleLight)
        {
            SetupOtherLightPosition(index, ref visibleLight);

            Light light = visibleLight.light;
            //第三列取反可得光照方向
            otherLightDirections[index] =  -visibleLight.localToWorldMatrix.GetColumn(2);

            float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
            float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * visibleLight.spotAngle);
            float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
            otherLightSpotAngles[index] = new Vector4(angleRangeInv, -outerCos * angleRangeInv);

            otherLightShadowData[index] = shadows.ReserveOtherShadows(light, index);
        }

        public void Cleanup()
        {
            shadows.Cleanup();
        }
    }
}