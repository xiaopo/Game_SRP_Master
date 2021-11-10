
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomSR
{
    public class Shadows
    {
        //定义最大支持的shadow的light数量
        const int maxShadowedDirLightCount  = 4, maxShadowedOtherLightCount = 16;
        //定义最大阴影级联的数量
        const int maxCascades = 4;
        //light 中影响shadow的属性
        struct ShadowedDirectionLight
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float nearPlaneOffset;
        }

        //根据级联和light数量定义数据长度
        ShadowedDirectionLight[] ShadowedDirectionalLights = new ShadowedDirectionLight[maxShadowedDirLightCount  * maxCascades];

        int shadowedDirLightCount, shadowedOtherLightCount;

        const string bufferName = "Shadows";

        CommandBuffer buffer = new CommandBuffer
        {
            name = bufferName
        };

        
        static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
        static int dirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
        static int otherShadowAtlasId = Shader.PropertyToID("_OtherShadowAtlas");
        static int otherShadowMatricesId = Shader.PropertyToID("_OtherShadowMatrices");
        static int otherShadowTilesId = Shader.PropertyToID("_OtherShadowTiles");

        static int cascadeCountId = Shader.PropertyToID("_CascadeCount");
        static int cascadeCullingSpheresId = Shader.PropertyToID("_CascadeCullingSpheres");
        static int cascadeDataId = Shader.PropertyToID("_CascadeData");
        static int shadowAtlasSizeId = Shader.PropertyToID("_ShadowAtlasSize");
        static int shadowDistanceFadeId = Shader.PropertyToID("_ShadowDistanceFade");
        static int shadowPancakingId = Shader.PropertyToID("_ShadowPancaking");
        static string[] directionalFilterKeywords =
        {
            "_DIRECTIONAL_PCF3",
            "_DIRECTIONAL_PCF5",
            "_DIRECTIONAL_PCF7",
        };

        static string[] cascadeBlendKeywords = {
            "_CASCADE_BLEND_SOFT",
            "_CASCADE_BLEND_DITHER"
        };

        static string[] shadowMaskKeywords = {
            "_SHADOW_MASK_ALWAYS",
            "_SHADOW_MASK_DISTANCE"
        };

        static string[] otherFilterKeywords = {
            "_OTHER_PCF3",
            "_OTHER_PCF5",
            "_OTHER_PCF7",
        };

        static Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxShadowedDirLightCount  * maxCascades];
        static Matrix4x4[] otherShadowMatrices = new Matrix4x4[maxShadowedOtherLightCount];
        static Vector4[] cascadeCullingSpheres = new Vector4[maxCascades];
        static Vector4[] cascadeData = new Vector4[maxCascades];
        static Vector4[] otherShadowTiles = new Vector4[maxShadowedOtherLightCount];
        struct ShadowedOtherLight
        {
            public int visibleLightIndex;
            public float slopeScaleBias;
            public float normalBias;
        }

        ShadowedOtherLight[] shadowedOtherLights = new ShadowedOtherLight[maxShadowedOtherLightCount];

        ScriptableRenderContext context;
        CullingResults cullingResults;
        ShadowSettings settings;
        Vector4 atlasSizes;
        bool useShadowMask;
        public void Setup(ScriptableRenderContext context,CullingResults cullingResults,ShadowSettings settings)
        {
            this.context = context;
            this.cullingResults = cullingResults;
            this.settings = settings;

            shadowedDirLightCount = 0;
            shadowedOtherLightCount = 0;

            useShadowMask = false;
        }

        void ExecuteBuffer()
        {
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }
        //阴影渲染
        public void Render()
        {
 
            if(shadowedDirLightCount > 0){
                RenderDirectionalShadows();
            }
            else{
                buffer.GetTemporaryRT(dirShadowAtlasId, 1, 1,32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            }

            if (shadowedOtherLightCount > 0){
                RenderOtherShadows();
            }
            else{
                buffer.SetGlobalTexture(otherShadowAtlasId, dirShadowAtlasId);
            }

            buffer.BeginSample(bufferName);
            SetKeywords(shadowMaskKeywords, useShadowMask ? QualitySettings.shadowmaskMode == ShadowmaskMode.Shadowmask ? 0:1 : -1);

            //没有direction light 的时候不需要 cascades count ，但是依然需要 cascade fade 数据
            buffer.SetGlobalInt(cascadeCountId, shadowedDirLightCount > 0 ? settings.directional.cascadeCount : 0);
            float f = 1f - settings.directional.cascadeFade;
            buffer.SetGlobalVector( shadowDistanceFadeId, new Vector4(1f / settings.maxDistance, 1f / settings.distanceFade, 1f / (1f - f * f)));

            buffer.SetGlobalVector(shadowAtlasSizeId, atlasSizes);

            buffer.EndSample(bufferName);
            ExecuteBuffer();
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
          
           
            //float f = 1f - settings.directional.cascadeFade;
            //buffer.SetGlobalVector(shadowDistanceFadeId, new Vector4(1.0f / settings.maxDistance, 1.0f / settings.distanceFade,1.0f/(1.0f - f * f)));
            buffer.EndSample(bufferName);
            ExecuteBuffer();
            buffer.BeginSample(bufferName);
            //在shaowmap上渲染的小块数
            int tiles = shadowedDirLightCount * settings.directional.cascadeCount;
            //每行分几份
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            //每块的宽高
            int tileSize = atlasSize / split;

            //遍历所有方向光渲染阴影
            for(int i = 0;i<shadowedDirLightCount;i++)
            {
                RenderDirectionalShadows(i, split, tileSize);
            }

            buffer.SetGlobalFloat(shadowPancakingId, 1f);
            //buffer.SetGlobalInt(cascadeCountId, settings.directional.cascadeCount);
            buffer.SetGlobalVectorArray(cascadeCullingSpheresId, cascadeCullingSpheres);
            buffer.SetGlobalVectorArray(cascadeDataId, cascadeData);
            // all shadowed lights are rendered send the matrices to the GPU 
            buffer.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);

            SetKeywords(directionalFilterKeywords,(int)settings.directional.filter - 1);
            SetKeywords(cascadeBlendKeywords, (int)settings.directional.cascadeBlend - 1);
            atlasSizes.x = atlasSize;
            atlasSizes.y = 1f / atlasSize;
            buffer.EndSample(bufferName);
            
            ExecuteBuffer();
        }

        void RenderOtherShadows()
        {
            int atlasSize = (int)settings.other.atlasSize;
            atlasSizes.z = atlasSize;
            atlasSizes.w = 1f / atlasSize;

            buffer.GetTemporaryRT(otherShadowAtlasId, atlasSize, atlasSize,32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap );
            buffer.SetRenderTarget(otherShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store );
            buffer.ClearRenderTarget(true, false, Color.clear);
          
            buffer.BeginSample(bufferName);
            ExecuteBuffer();

            int tiles = shadowedOtherLightCount;
            int split = tiles <= 1 ? 1 : tiles <= 4 ? 2 : 4;
            int tileSize = atlasSize / split;

            for (int i = 0; i < shadowedOtherLightCount; i++)
            {
                RenderSpotShadows(i, split, tileSize);
            }

            buffer.SetGlobalMatrixArray(otherShadowMatricesId, otherShadowMatrices);
            buffer.SetGlobalVectorArray(otherShadowTilesId, otherShadowTiles);
            buffer.SetGlobalFloat(shadowPancakingId, 0f);
            SetKeywords(otherFilterKeywords, (int)settings.other.filter - 1);

            buffer.EndSample(bufferName);
            ExecuteBuffer();
        }

        void SetOtherTileData(int index, Vector2 offset, float scale, float bias)
        {
            float border = atlasSizes.w * 0.5f;
            Vector4 data;
            data.x = offset.x * scale + border;
            data.y = offset.y * scale + border;
            data.z = scale - border - border;
            data.w = bias;
            otherShadowTiles[index] = data;
        }

        void RenderSpotShadows(int index, int split, int tileSize)
        {
            ShadowedOtherLight light = shadowedOtherLights[index];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex);

            cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(light.visibleLightIndex,
                                                                        out Matrix4x4 viewMatrix,
                                                                        out Matrix4x4 projectionMatrix,
                                                                        out ShadowSplitData splitData);
            shadowSettings.splitData = splitData;

            float texelSize = 2f / (tileSize * projectionMatrix.m00);
            float filterSize = texelSize * ((float)settings.other.filter + 1f);
            float bias = light.normalBias * filterSize * 1.4142136f;
            Vector2 offset = SetTileViewport(index, split, tileSize);
            float tileScale = 1f / split;
            SetOtherTileData(index, offset, tileScale, bias);

            otherShadowMatrices[index] = ConvertToAtlasMatrix( projectionMatrix * viewMatrix, SetTileViewport(index, split, tileSize), tileScale);

            buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
            buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);

            ExecuteBuffer();
            context.DrawShadows(ref shadowSettings);

            buffer.SetGlobalDepthBias(0f, 0f);
        }

        void SetKeywords(string[] keywords,int enabledIndex)
        {
            //int enabledIndex = (int)settings.directional.filter - 1;
            for(int i = 0;i< keywords.Length;i++)
            {
                if(i == enabledIndex){
                    buffer.EnableShaderKeyword(keywords[i]);
                }
                else{
                    buffer.DisableShaderKeyword(keywords[i]);
                }
            }
        }

        public Vector4 ReserveOtherShadows(Light light, int visibleLightIndex, int visibleIndex)
        {
            if (light.shadows == LightShadows.None || light.shadowStrength <= 0f)
            {
                return new Vector4(0f, 0f, 0f, -1f);
            }

            float maskChannel = -1f;
            LightBakingOutput lightBaking = light.bakingOutput;
            if (lightBaking.lightmapBakeType == LightmapBakeType.Mixed && lightBaking.mixedLightingMode == MixedLightingMode.Shadowmask)
            {
                useShadowMask = true;
                maskChannel = lightBaking.occlusionMaskChannel;
            }

            //out of range and amount of limit
            if ( shadowedOtherLightCount >= maxShadowedOtherLightCount || !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
            {
                return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
            }

            shadowedOtherLights[shadowedOtherLightCount] = new ShadowedOtherLight{
                visibleLightIndex = visibleLightIndex,
                slopeScaleBias = light.shadowBias,
                normalBias = light.shadowNormalBias
            };

            return new Vector4(light.shadowStrength, shadowedOtherLightCount++, 0f, maskChannel);
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
            float cullingFactor = Mathf.Max(0f, 0.8f - settings.directional.cascadeFade);
            float tileScale = 1f / split;
            for (int i = 0;i< cascadeCount;i++)
            {
                cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                light.visibleLightIndex, i, cascadeCount, ratios, tileSize,light.nearPlaneOffset,
                out Matrix4x4 viewMatrix, 
                out Matrix4x4 projectionMatrix, 
                out ShadowSplitData splitData);

                splitData.shadowCascadeBlendCullingFactor = cullingFactor;
                shadowSettings.splitData = splitData;

                if(lightIndex == 0){
                    //as the cascades of all lights are equivalent
                    SetCascadeData(i, splitData.cullingSphere, tileSize);
                }

                int tileIndex = tileOffset + i;
                //is a conversion matrix from world space to light space
                dirShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, SetTileViewport(tileIndex, split, tileSize), tileScale);
                buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
 
                ExecuteBuffer();
                context.DrawShadows(ref shadowSettings);
            }

        }
        
        void SetCascadeData(int index,Vector4 cullingSphere,float titleSize)
        {

            //radius square
            cullingSphere.w *= cullingSphere.w;
            cascadeCullingSpheres[index] = cullingSphere;

            float texelSize = 2f * cullingSphere.w / titleSize;
            float filterSize = texelSize * ((float)settings.directional.filter + 1f);
            float bias = filterSize * 1.4142136f;
            cascadeData[index] = new Vector4( 1.0f / cullingSphere.w, bias);
        }

        public Vector4 ReserveDirectionalShadows(Light light,int visibleLightIndex, int visibleIndex)
        {
            //储存可见光的索引，前提是光源开启了阴影投射并且阴影强度不能为0
            if(shadowedDirLightCount < maxShadowedDirLightCount  
                && light.shadows != LightShadows.None 
                && light.shadowStrength > 0f
             )
            {
                float maskChannel = -1;
                LightBakingOutput lightBakeing = light.bakingOutput;
                if(lightBakeing.lightmapBakeType == LightmapBakeType.Mixed && lightBakeing.mixedLightingMode == MixedLightingMode.Shadowmask) 
                { 
                    useShadowMask = true;
                    maskChannel = lightBakeing.occlusionMaskChannel;
                }

                if (!cullingResults.GetShadowCasterBounds( visibleLightIndex, out Bounds b ))
                {
                    return new Vector4(-light.shadowStrength, 0f, 0f,maskChannel);
                }

                ShadowedDirectionalLights[shadowedDirLightCount] = new ShadowedDirectionLight { visibleLightIndex = visibleLightIndex,
                                                                                                     slopeScaleBias = light.shadowBias,
                                                                                                     nearPlaneOffset = light.shadowNearPlane};

                // x strength  y tileIndex
                //each directional light will now claim multiple successive tiles
                int tileIndex = settings.directional.cascadeCount * shadowedDirLightCount++;
                return new Vector4(
                                    light.shadowStrength, 
                                    tileIndex,
                                    light.shadowNormalBias,
                                    maskChannel
                                    );
            }

            return new Vector4(0f,0f,0f,-1f);
        }

        Matrix4x4 ConvertToAtlasMatrix(Matrix4x4 m,Vector2 offset, float scale)
        {
            if(SystemInfo.usesReversedZBuffer)
            {
                m.m20 = -m.m20;
                m.m21 = -m.m21;
                m.m22 = -m.m22;
                m.m23 = -m.m23;
            }

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
            buffer.ReleaseTemporaryRT(dirShadowAtlasId);

            if (shadowedOtherLightCount > 0)
            {
                buffer.ReleaseTemporaryRT(otherShadowAtlasId);
            }

            ExecuteBuffer();
        }
    }
}