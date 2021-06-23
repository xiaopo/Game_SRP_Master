
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomSR
{
    public class Shadows
    {
        const int maxShadowdDirectionalLightCount = 1;
        struct ShadowedDirectionLight
        {
            public int visibleLightIndex;
        }

        ShadowedDirectionLight[] ShadowedDirectionalLights = new ShadowedDirectionLight[maxShadowdDirectionalLightCount];
        int ShadowedirectionLightCount;

        const string bufferName = "Shadows";

        CommandBuffer buffer = new CommandBuffer
        {
            name = bufferName
        };

        ScriptableRenderContext context;
        CullingResults cullingResults;
        ShadowSettings settings;

        public void Setup(ScriptableRenderContext context,CullingResults cullingResults,ShadowSettings settings)
        {
            this.context = context;
            this.cullingResults = cullingResults;
            this.settings = settings;

            ShadowedirectionLightCount = 0;
        }
        void ExecuteBuffer()
        {
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        //阴影渲染
        public void Render()
        {
 
            if(ShadowedirectionLightCount > 0)
            {
                RenderDirectionalShadows();
            }
            else
            {
                buffer.GetTemporaryRT(dirShadowAtlasId, 1, 1,32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            }

        }

        static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
        //渲染定向光影
        void RenderDirectionalShadows()
        {
            //创建renderTexture，并指定该类型是阴影贴图
            int atlasSize = (int)settings.directional.atlasSize;

            buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);

            //指定渲染数据储存到RT中
            buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            //清除深度缓冲区
            buffer.ClearRenderTarget(true, false, Color.clear);

            buffer.BeginSample(bufferName);
            ExecuteBuffer();

            //遍历所有方向光渲染阴影
            for(int i = 0;i<ShadowedirectionLightCount;i++)
            {
                RenderDirectionalShadows(i, atlasSize);
            }
            buffer.EndSample(bufferName);
            
            ExecuteBuffer();
        }

        //渲染定向光影
        void RenderDirectionalShadows(int index,int tileSize)
        {
            ShadowedDirectionLight light = ShadowedDirectionalLights[index];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, 0, 1, Vector3.zero, tileSize, 0f,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

            shadowSettings.splitData = splitData;

            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            ExecuteBuffer();

            context.DrawShadows(ref shadowSettings);

        }

        public void ReserveDirectionalShadows(Light light,int visibleLightIndex)
        {
            //储存可见光的索引，前提是光源开启了阴影投射并且阴影强度不能为0
            if(ShadowedirectionLightCount < maxShadowdDirectionalLightCount 
                && light.shadows != LightShadows.None 
                && light.shadowStrength > 0f
                && cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b)
             )
            {
                ShadowedDirectionalLights[ShadowedirectionLightCount++] = new ShadowedDirectionLight { visibleLightIndex = visibleLightIndex };
            }
        }

  
        public void Cleanup()
        {
            buffer.ReleaseTemporaryRT(dirShadowAtlasId);
            ExecuteBuffer();
        }
    }
}