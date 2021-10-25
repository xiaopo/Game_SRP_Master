

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

        //储存可见光的颜色和方向
        static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
        static Vector4[] dirLightDirectioins = new Vector4[maxDirLightCount];
        static Vector4[] dirLightShadowData = new Vector4[maxDirLightCount];

        static Vector4[] otherLightColors = new Vector4[maxOtherLightCount];
        static Vector4[] otherLightPositions = new Vector4[maxOtherLightCount];
        //裁剪信息
        CullingResults cullingResults;

        Shadows shadows = new Shadows();
        public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
        {
            this.cullingResults = cullingResults;

            buffer.BeginSample(bufferName);
            //传递阴影数据
            shadows.Setup(context, cullingResults, shadowSettings);
            //发送光源数据
            SetupLights();
            //渲染阴影
            shadows.Render();
            buffer.EndSample(bufferName);

            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        void SetupLights()
        {
            //Unity 会在剔除阶段计算哪些光源会影响相机的可见性
            //得到所有可见光
            NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
            if (visibleLights == null) return;

            int dirLightCount = 0, otherLightCount = 0;
            for (int i = 0; i < visibleLights.Length; i++)
            {
                VisibleLight visibleLight = visibleLights[i];

                switch (visibleLight.lightType)
                {
                    case LightType.Directional:
                        if (dirLightCount < maxDirLightCount)
                        {
                            SetupDirectionalLight(dirLightCount++, ref visibleLight);
                        }
                        break;
                    case LightType.Point:
                        if (otherLightCount < maxOtherLightCount)
                        {
                            SetupPointLight(otherLightCount++, ref visibleLight);
                        }
                        break;
                }
            }

            
            if(dirLightCount > 0)
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
                //点光和聚光
                buffer.SetGlobalVectorArray(otherLightColorsId, otherLightColors);
                buffer.SetGlobalVectorArray(otherLightPositionsId, otherLightPositions);
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

        // point and spot light
        void SetupPointLight(int index, ref VisibleLight visibleLight)
        {
            otherLightColors[index] = visibleLight.finalColor;

            Vector4 position = visibleLight.localToWorldMatrix.GetColumn(3);
            position.w = 1f / Mathf.Max(visibleLight.range * visibleLight.range, 0.00001f);
            //最后一列平移量
            otherLightPositions[index] = position;
        }
        public void Cleanup()
        {
            shadows.Cleanup();
        }
    }
}