
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomSR
{
    /**
     * Normal Bias
     * Incorrect self-shadowing happens because a shadow caster depth texel covers more than one fragment, 
     * which causes the caster's volume to poke out of its surface. 
     * 
     * So if we shrink the caster enough this should no longer happen. 
     * However, shrinking shadows caster will make shadows smaller than they should be and can introduce holes 
     * that shouldn't exist.
     * 
     * We can also do the opposite: inflate the surface while sampling shadows. 
     * Then we're sampling a bit away from the surface, just far enough to avoid incorrect self-shadowing. 
     * This will adjust the positions of shadows a bit, potentially causing misalignment along edges and adding false shadows, 
     * but these artifacts tend to be far less obvious than Peter-Panning.
     * 
     * **/

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
            public bool isPoint;
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

            for (int i = 0; i < shadowedOtherLightCount;)
            { 
                if (shadowedOtherLights[i].isPoint)
                {
                    RenderPointShadows(i, split, tileSize);
                    i += 6;
                }
                else
                {
                    RenderSpotShadows(i, split, tileSize);
                    i += 1;
                }
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
            //Note:
            //A border value by the half (1/atlasSize),that actually as the 1 texel half
            float border = atlasSizes.w * 0.5f;
            Vector4 data;
            //Note:
            //The tile's minimum texture coordinates are the scaled offset
            data.x = offset.x * scale + border;
            data.y = offset.y * scale + border;
            //as a size of square but deducted borders
            data.z = scale - border - border;
            //light normal bias
            data.w = bias;

            otherShadowTiles[index] = data;
        }

        void RenderSpotShadows(int index, int split, int tileSize)
        {
            ShadowedOtherLight light = shadowedOtherLights[index];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex) { useRenderingLayerMaskTest = true };

            cullingResults.ComputeSpotShadowMatricesAndCullingPrimitives(light.visibleLightIndex,
                                                                        out Matrix4x4 viewMatrix,
                                                                        out Matrix4x4 projectionMatrix,
                                                                        out ShadowSplitData splitData);
            shadowSettings.splitData = splitData;

            //世界空间文素对应的Size
            float texelSize = 2f / (tileSize * projectionMatrix.m00);

            float filterSize = texelSize * ((float)settings.other.filter + 1f);

            //最坏得情况是需要偏移对角线
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

        void RenderPointShadows(int index, int split, int tileSize)
        {
            ShadowedOtherLight light = shadowedOtherLights[index];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, 
                light.visibleLightIndex) { useRenderingLayerMaskTest = true };


            //The field of view for cubemap faces is always 90°, thus the world-space tile size at distance 1 is always 2
            // a = tan(fov/2) * distance;
            // worldsize = tan(90° / 2) * 1 * 2;
            float texelSize = 2f / tileSize;
            float filterSize = texelSize * ((float)settings.other.filter + 1f);
            float bias = light.normalBias * filterSize * 1.4142136f;
            float tileScale = 1f / split;

            /**
             * //Note:filed of view bias
             * There's always a discontinuity between faces of a cube map, 
             * because the orientation of the texture plane suddenly changes 90°.
             * Regular cubemap sampling can hide this somewhat because it can interpolate between faces, 
             * but we're sampling from a single tile per fragment. 
             * 
             * We get the same issues that exist at the edge of spot shadow tiles, 
             * but now they aren't hidden because there's no spot attenuation.
             * 
             * We can reduce these artifacts by increasing the field of view—FOV for short—a little 
             * when rendering the shadows, so we never sample beyond the edge of a tile. That's what 
             * the bias argument of ComputePointShadowMatricesAndCullingPrimitives is for. 
             * 
             * We do that by making our tile size a bit larger than 2 at distance 1 from the light. 
             * Specifically, we add the normal bias plus the filter size on each side. 
             * 
             * The tangent of the half corresponding FOV angle is then equal to 1 plus the bias and filter size. 
             * Double that, convert it to degrees, subtract 90°, and use it for the FOV bias in RenderPointShadows.
             * **/
            float fovBias = Mathf.Atan(1f + bias + filterSize) * Mathf.Rad2Deg * 2f - 90f;
            for (int i = 0; i < 6; i++)
            {
                cullingResults.ComputePointShadowMatricesAndCullingPrimitives(light.visibleLightIndex,(CubemapFace)i, fovBias,
                out Matrix4x4 viewMatrix,
                out Matrix4x4 projectionMatrix,
                out ShadowSplitData splitData
                );

                /*
                 * We can now see realtime shadows for point lights. They don't appear to suffer from shadow acne, 
                 * even with zero bias. Unfortunately, light now leaks through objects to surfaces very close to them on the opposite side. 
                 * Increasing the shadow bias makes this worse and also appears to cut holes in the shadows of objects close to other surfaces.
                 * 
                 * This happens because of the way Unity renders shadows for point lights. 
                 * It draws them upside down, which reverses the winding order of triangles. 
                 * Normally the front faces—from the point of view of the light—are drawn, 
                 * but now the back faces get rendered. 
                 * This prevents most acne but introduces light leaking. 
                 * 
                 * 
                 * We cannot stop the flipping,but we can undo it by negating a row of the view matrix 
                 * that we get from ComputePointShadowMatricesAndCullingPrimitives. 
                 * Let's negate its second row. This flips everything upside down in the atlas at second time, 
                 * which turns everything back to normal. 
                 * Because the first component of that row is always zero we can suffice with only negating the other three components.
                 * */
                viewMatrix.m11 = -viewMatrix.m11;
                viewMatrix.m12 = -viewMatrix.m12;
                viewMatrix.m13 = -viewMatrix.m13;
                shadowSettings.splitData = splitData;

                int tileIndex = index + i;

                Vector2 offset = SetTileViewport(tileIndex, split, tileSize);
 
                SetOtherTileData(tileIndex, offset, tileScale, bias);

                otherShadowMatrices[tileIndex] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix, offset, tileScale);

                buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);

                buffer.SetGlobalDepthBias(0f, light.slopeScaleBias);
                ExecuteBuffer();
                context.DrawShadows(ref shadowSettings);
                buffer.SetGlobalDepthBias(0f, 0f);
            }
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

        public Vector4 ReserveOtherShadows(Light light, int visibleLightIndex)
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

            bool isPoint = light.type == LightType.Point;
            int newLightCount = shadowedOtherLightCount + (isPoint ? 6 : 1);

            //out of range and amount of limit
            if (newLightCount >= maxShadowedOtherLightCount || !cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
            {
                return new Vector4(-light.shadowStrength, 0f, 0f, maskChannel);
            }

            shadowedOtherLights[shadowedOtherLightCount] = new ShadowedOtherLight{
                visibleLightIndex = visibleLightIndex,
                slopeScaleBias = light.shadowBias,
                normalBias = light.shadowNormalBias,
                isPoint = isPoint
            };

            Vector4 data =  new Vector4(light.shadowStrength, shadowedOtherLightCount, isPoint ? 1f : 0f, maskChannel);

            shadowedOtherLightCount = newLightCount;

            return data;
        }

        Vector2 SetTileViewport(int index, int split,int tileSize)
        {
            //计算列 行
            Vector2 offset = new Vector2(index % split, index / split);
            //设置视图块
            buffer.SetViewport(new Rect(offset.x * tileSize, offset.y * tileSize, tileSize, tileSize));

            return offset;
        }
        
        void RenderDirectionalShadows(int lightIndex, int split, int tileSize)
        {
            ShadowedDirectionLight light = ShadowedDirectionalLights[lightIndex];
            var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex) { useRenderingLayerMaskTest = true };

            int cascadeCount = settings.directional.cascadeCount;
            //转换到对应的起点
            int tileOffset = lightIndex * cascadeCount;
            Vector3 ratios = settings.directional.CascadeRatios;

            /**
             * One downside of using cascaded shadow maps is that we end up rendering 
             * the same shadow casters more than once per light.
             * It makes sense to try and cull some shadow casters from larger cascades 
             * if it can be guaranteed that their results will always be covered by a smaller cascade. 
             * Unity makes this possible by setting the shadowCascadeBlendCullingFactor of the split data to one.
             * 
             * shadowCascadeBlendCullingFactor = 1;
             * 
             * The value is a factor that modulates the radius of the previous cascade 
             * used to perform the culling. Unity is fairly conservative when culling, 
             * but we should decrease it by the cascade fade ratio and a little extra 
             * to make sure that shadow casters in the transition region never get culled. 
             * So let's use 0.8 minus the fade range, with a minimum of zero. 
             * If you see holes appear in shadows around cascade transitions then it must be reduced even further.
             * **/
            float cullingFactor = Mathf.Max(0f, 0.8f - settings.directional.cascadeFade);
            float tileScale = 1f / split;

            for (int i = 0;i< cascadeCount;i++)
            {
                cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
                    light.visibleLightIndex, i, cascadeCount, ratios, tileSize, light.nearPlaneOffset,
                    out Matrix4x4 viewMatrix,
                    out Matrix4x4 projectionMatrix,
                    out ShadowSplitData splitData);

                /** cull some shadow casters from larger cascades
                **  guaranteed that their results will always be covered by a smaller cascade
                **/
                splitData.shadowCascadeBlendCullingFactor = cullingFactor;
                shadowSettings.splitData = splitData;

                if(lightIndex == 0){
                    /*
                     * Unity determines the region covered by each cascade by creating a culling sphere for it. 
                     * As the shadow projections are orthographic and square they end up 
                     * closely fitting their culling sphere but also cover some space around them. 
                     * 
                     * That's why some shadows can be seen outside the culling regions.
                     * Also, the light direction doesn't matter to the sphere, 
                     * so all directional lights end up using the same culling spheres.
                     **/
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
            
            //dividing the diameter of the culling sphere by the tile size
            //How many meters in the world space corresponding to a texel
            float texelSize = 2f * cullingSphere.w / titleSize;
            /**
             * Increasing the filter size makes shadows smoother,
             * but also causes acne to appear again. 
             * We have to increase the normal bias to match the filter size. 
             * We can do this automatically by multiplying the texel size by one plus the filter mode in SetCascadeData.
             * **/
            float filterSize = texelSize * ((float)settings.directional.filter + 1f);
            //In the worst case we end up having to offset along the square's diagonal, so let's scale it by √2.
            float bias = filterSize * 1.4142136f;

            /*
             * Besides that, increasing the sample region also means that 
             * we can end up sampling outside of the cascade's culling sphere.
             * We can avoid that by reducing the sphere's radius by the filter size before squaring it.
             */
            cullingSphere.w -= filterSize;


            cullingSphere.w *= cullingSphere.w;
            cascadeCullingSpheres[index] = cullingSphere;
            cascadeData[index] = new Vector4( 1.0f / cullingSphere.w, bias);
        }

        public Vector4 ReserveDirectionalShadows(Light light,int visibleLightIndex)
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
                    /*
                     * to examine the bakeingOutput of the            
                     */
                    useShadowMask = true;
                    maskChannel = lightBakeing.occlusionMaskChannel;
                }

                if (!cullingResults.GetShadowCasterBounds( visibleLightIndex, out Bounds b ))
                {
                    /*
                     * Besides that, it's possible that a visible light ends up not affecting any objects that cast shadows, 
                     * either because they're configured not to or because the light only affects objects beyond the max shadow distance
                     */
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

            //To build a matrix that converts the value range from [-1,1] to [0,1]
            Matrix4x4 convert01 = Matrix4x4.identity;
            // x 0.5f
            convert01.m00 = 0.5f;
            convert01.m11 = 0.5f;
            convert01.m22 = 0.5f;
            // + 0.5f
            convert01.m03 = 0.5f;
            convert01.m13 = 0.5f;
            convert01.m23 = 0.5f;

            //To apply the offset and scale to the original matrix
            Matrix4x4 sliceos = Matrix4x4.identity;
            sliceos.m00 = scale;
            sliceos.m11 = scale;

            sliceos.m03 = offset.x * scale;
            sliceos.m13 = offset.y * scale;

            return convert01 * sliceos * m;
            //x
   //         m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
   //         m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
   //         m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
   //         m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
			////y
   //         m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
   //         m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
   //         m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
   //         m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
			////z
   //         m.m20 = 0.5f * (m.m20 + m.m30);
   //         m.m21 = 0.5f * (m.m21 + m.m31);
   //         m.m22 = 0.5f * (m.m22 + m.m32);
   //         m.m23 = 0.5f * (m.m23 + m.m33);

            //return m;
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