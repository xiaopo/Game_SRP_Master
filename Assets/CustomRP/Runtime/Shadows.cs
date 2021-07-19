﻿
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomSR
{
    public class Shadows
    {
        const int maxShadowdDirectionalLightCount = 4;
        const int maxCascades = 4;
        struct ShadowedDirectionLight
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float nearPlaneOffset;
        }

        ShadowedDirectionLight[] ShadowedDirectionalLights = new ShadowedDirectionLight[maxShadowdDirectionalLightCount * maxCascades];
        int ShadowedirectionLightCount;

        const string bufferName = "Shadows";

        CommandBuffer buffer = new CommandBuffer
        {
            name = bufferName
        };

        static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
        static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
        static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
        static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
        static int cascadeDataId = Shader.PropertyToID("_CascadeData");
        static int shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");
        static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
        static string[] directionalFilterKeywords =
        {
            "_DIRECTIONAL_PCF3",
            "_DIRECTIONAL_PCF5",
            "_DIRECTIONAL_PCF7",
        };

        static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowdDirectionalLightCount * maxCascades];
        static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];
        static Vector4[] cascadeData = new Vector4[maxCascades];

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
            buffer.BeginSample(bufferName);
            buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);

            //指定渲染数据储存到RT中
            buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            //清除深度缓冲区
            buffer.ClearRenderTarget(true, false, Color.clear);

            buffer.SetGlobalInt(cascadeCountId, settings.directional.cascadeCount);
            buffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
            buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
            // all shadowed lights are rendered send the matrices to the GPU 
            buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
           
            float f = 1f - settings.directional.cascadeFade;
            buffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(1.0f / settings.maxDistance, 1.0f / settings.distanceFade,1.0f/(1.0f - f * f)));
            buffer.EndSample(bufferName);
            ExecuteBuffer();
            buffer.BeginSample(bufferName);
            //在shaowmap上渲染的小块数
            int tiles = ShadowedirectionLightCount * settings.directional.cascadeCount;
            //每行分几份
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            //每块的宽高
            int tileSize = atlasSize / split;

            //遍历所有方向光渲染阴影
            for(int i = 0;i<ShadowedirectionLightCount;i++)
            {
                RenderDirectionalShadows(i, split, tileSize);
            }

            SetKeywords();
            buffer.SetGlobalVector(shadowAtlasSizeId, new Vector4(atlasSize, 1f / atlasSize));
            buffer.EndSample(bufferName);
            
            ExecuteBuffer();
        }

        void SetKeywords()
        {
            int enabledIndex = (int)settings.directional.filter - 1;
            for(int i = 0;i<directionalFilterKeywords.Length;i++)
            {
                if(i == enabledIndex){
                    buffer.EnableShaderKeyword(directionalFilterKeywords[i]);
                }
                else{
                    buffer.DisableShaderKeyword(directionalFilterKeywords[i]);
                }
            }
        }

        Vector2 SetTileViewport(int index, int split,int tileSize)
        {
            //计算列 行
            Vector2 offset = new Vector2(index % split, index / split);
            //设置视图块
            buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));

            return offset;
        }
        //渲染定向光影
        void RenderDirectionalShadows(int lightIndex, int split, int tileSize)
        {
            ShadowedDirectionLight light = ShadowedDirectionalLights[lightIndex];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

            int cascadeCount = settings.directional.cascadeCount;
            //转换到对应的行
            int tileOffset = lightIndex * cascadeCount;
            Vector3 ratios = settings.directional.CascadeRatios;

            for(int i = 0;i< cascadeCount;i++)
            {
                cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.visibleLightIndex, i, cascadeCount, ratios, tileSize,light.nearPlaneOffset,
                out Matrix4x4 viewMatrix, 
                out Matrix4x4 projectionMatrix, 
                out ShadowSplitData splitData);

                shadowSettings.splitData = splitData;

                if(lightIndex == 0){
                    //as the cascades of all lights are equivalent
                    SetCascadeData(i, splitData.cullingSphere, tileSize);
                }

                int tileIndex = tileOffset + i;
                //is a conversion matrix from world space to light space
                dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), split);
                buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

                buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
                ExecuteBuffer();
                context.DrawShadows(ref shadowSettings);
                buffer.SetGlobalDepthBias(0f, 0f);
            }

        }
        
        void SetCascadeData(int index,Vector4 cullingSphere,float titleSize)
        {

            //radius square
            //cullingSphere.w -= filterSize;
            cullingSphere.w *= cullingSphere.w;
            cascadeCullingSpheres[index] = cullingSphere;

            float texelSize = 2f * cullingSphere.w / titleSize;
            float filterSize = texelSize * ((float)settings.directional.filter + 1f);
            cascadeData[index] = new Vector4(
                                             1.0f / cullingSphere.w,
                                             filterSize * 1.4142136f
                                             );
        }

        public Vector3 ReserveDirectionalShadows(Light light,int visibleLightIndex)
        {
            //储存可见光的索引，前提是光源开启了阴影投射并且阴影强度不能为0
            if(ShadowedirectionLightCount < maxShadowdDirectionalLightCount 
                && light.shadows != LightShadows.None 
                && light.shadowStrength > 0f
                //检查可见光是否有阴影或阴影是不是 beyond the maxshadowsdistance 
                && cullingResults.GetShadowCasterBounds(visibleLightIndex,out Bounds b)
             )
            {
                ShadowedDirectionalLights[ShadowedirectionLightCount] = new ShadowedDirectionLight { visibleLightIndex = visibleLightIndex,
                                                                                                     slopeScaleBias = light.shadowBias,
                                                                                                     nearPlaneOffset = light.shadowNearPlane
                };

                // x strength  y tileIndex
                //each directional light will now claim multiple successive tiles
                int tileIndex = settings.directional.cascadeCount * ShadowedirectionLightCount++;
                return new Vector3(
                                    light.shadowStrength, 
                                    tileIndex,
                                    light.shadowNormalBias
                                    );
            }

            return Vector3.zero;
        }

        Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m,Vector2 offset,int split)
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
            m.m20 = 0.5f * (m.m20 + m.m30);
            m.m21 = 0.5f * (m.m21 + m.m31);
            m.m22 = 0.5f * (m.m22 + m.m32);
            m.m23 = 0.5f * (m.m23 + m.m33);

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