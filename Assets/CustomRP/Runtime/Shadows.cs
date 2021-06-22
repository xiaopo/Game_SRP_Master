using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        //阴影渲染
        public void Render()
        {
            if(ShadowedirectionLightCount > 0)
            {
                RenderDirectionalShadows();
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
            ExcuteBuffer();

        }

        public void ReserveDirectionalShadows(Light light,int visibleLightIndex)
        {
            if(ShadowedirectionLightCount < maxShadowdDirectionalLightCount 
                && light.shadows != LightShadows.None 
                && light.shadowStrength > 0f
                && cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b)
             )
            {
                ShadowedDirectionalLights[ShadowedirectionLightCount++] = new ShadowedDirectionLight { visibleLightIndex = visibleLightIndex };
            }
        }

        void ExcuteBuffer()
        {
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        public void Cleanup()
        {
            buffer.ReleaseTemporaryRT(dirShadowAtlasId);
            ExcuteBuffer();
        }
    }
}