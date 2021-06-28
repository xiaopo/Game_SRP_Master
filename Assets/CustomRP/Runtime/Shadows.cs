﻿
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomSR
{
    public class Shadows
    {
        const int maxShadowdDirectionalLightCount = 4;
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


        static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
        static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");

        static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowdDirectionalLightCount];

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

            // all shadowed lights are rendered send the matrices to the GPU 
            buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
            buffer.BeginSample(bufferName);
            ExecuteBuffer();

            //分割图块
            int split = ShadowedirectionLightCount <= 1 ? 1 : 2;
            int tileSize = atlasSize / split;

            //遍历所有方向光渲染阴影
            for(int i = 0;i<ShadowedirectionLightCount;i++)
            {
                RenderDirectionalShadows(i, split, tileSize);
            }
            buffer.EndSample(bufferName);
            
            ExecuteBuffer();
        }
        Vector2 SetTileViewPoint(int index, int split,int tileSize)
        {
            //计算列 行
            Vector2 offset = new Vector2(index % split, index / split);
            //设置视图块
            buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));

            return offset;
        }

        //渲染定向光影
        void RenderDirectionalShadows(int index, int split, int tileSize)
        {
            ShadowedDirectionLight light = ShadowedDirectionalLights[index];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

            cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(light.visibleLightIndex, 0, 1, Vector3.zero, tileSize, 0f,
                out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);

            shadowSettings.splitData = splitData;

            //is a conversion matrix from world space to light space
            dirShadowMatrices[index] = ConverToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewPoint(index, split, tileSize), split);
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
                //检查可见光是否有阴影或阴影是不是 beyond the maxshadowsdistance 
                && cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b)
             )
            {
                ShadowedDirectionalLights[ShadowedirectionLightCount++] = new ShadowedDirectionLight { visibleLightIndex = visibleLightIndex };
            }
        }

        Matrix4x4 ConverToAtlasMatrix(Matrix4x4 m,Vector2 offset,int split)
        {
            if(SystemInfo.usesReversedZBuffer)
            {
                m.m20 = -m.m20;
                m.m21 = -m.m21;
                m.m22 = -m.m22;
                m.m23 = -m.m23;
            }

            float scale = 1f / split;
            m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
            m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
            m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
            m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
            m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
            m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
            m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
            m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
            m.m20 = 0.5f * (m.m20 + m.m30) ;
            m.m21 = 0.5f * (m.m21 + m.m31) ;
            m.m22 = 0.5f * (m.m22 + m.m32) ;
            m.m23 = 0.5f * (m.m23 + m.m33) ;

            return m;
        }
  
        public void Cleanup()
        {
            if(ShadowedirectionLightCount > 0)
            {
                buffer.ReleaseTemporaryRT(dirShadowAtlasId);
                ExecuteBuffer();
            }
          
        }
    }
}