using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
namespace CustomSR
{
    public partial class CameraRenderer
    {
    
        ScriptableRenderContext contenxt;

        Camera camera;

        CustomRendePineAsset asset;
        const string bufferName = "Render Camera";

        CommandBuffer buffer = new CommandBuffer
        {
            name = bufferName
        };

        Lighting lighting = new Lighting();//灯光
        PostFXStack postFXStack = new PostFXStack();

        static int colorAttachmentId = Shader.PropertyToID("_CameraColorAttachment");
        static int  depthAttachmentId = Shader.PropertyToID("_CameraDepthAttachment");
        static int colorTextureId = Shader.PropertyToID("_CameraColorTexture");
        static int depthTextureId = Shader.PropertyToID("_CameraDepthTexture");
        static int sourceTextureId = Shader.PropertyToID("_SourceTexture");
        static bool copyTextureSupported = SystemInfo.copyTextureSupport > CopyTextureSupport.None;

        bool useHDR;
        bool useColorTexture, useDepthTexture, useIntermediateBuffer;
        Material material;

        /*
         * As the depth texture is optional it might not exist. When a shader samples it anyway the result will be random. 
         * It could be either an empty texture or an old copy, potentially of another camera. 
         * It's also possible that a shader samples the depth texture too early, during the opaque rendering phase
         * **/
        Texture2D missingTexture;
        public CameraRenderer(Shader shader)
        {
            material = CoreUtils.CreateEngineMaterial(shader);
            missingTexture = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave,
                name = "Missing"
            };
            missingTexture.SetPixel(0, 0, Color.white * 0.5f);
            missingTexture.Apply(true, true);
        }
        public void Render(ScriptableRenderContext contenxt,Camera camera, CustomRendePineAsset asset)
        {
            this.contenxt = contenxt;
            this.camera = camera;
            this.asset = asset;

            this.useDepthTexture = true;
            if (camera.cameraType == CameraType.Reflection)
            {
                useColorTexture = asset.cameraBuffer.copyColorReflection;
                useDepthTexture = asset.cameraBuffer.copyDepthReflections;
            }
            else
            {
                useColorTexture = asset.cameraBuffer.copyColor;
                useDepthTexture = asset.cameraBuffer.copyDepth;
            }

            //设置命令缓冲区名字
            PrepareBuffer();

            //因为此操作可能给Scene场景中添加一些几何体，所以我们在Render()方法中进行几何体剔除之前用这个方法
            PrepareForSceneWindow();

            if (!Cull(asset.shadows.maxDistance)) return;///被剔除

            useHDR = asset.cameraBuffer.allowHDR && camera.allowHDR;

            buffer.BeginSample(SampleName);
            ExecuteBuffer();
            //渲染灯光
            lighting.Setup(contenxt, culingResouts, asset.shadows, asset.useLightsPerObject);
            //后处理
            postFXStack.Setup(contenxt, camera, asset.postFXSettings,useHDR, (int)asset.colorLUTResolution);
            buffer.EndSample(SampleName);

            SetUp();
            //绘制几何体
            DrawVisibleGeometry();
            //绘制SRP不支持的着色器类型
            DrawUnsupportedShaders();
            //绘制辅助线
            DrawGizmosBeforeFX();

            
            if (postFXStack.IsActive){
                //后处理
                postFXStack.Render(colorAttachmentId);
            }
            else if (useIntermediateBuffer){
                Draw(colorAttachmentId, BuiltinRenderTextureType.CameraTarget);
                ExecuteBuffer();
            }

            DrawGizmosAfterFX();
            //释放申请的RT内存空间
            Cleanup();

            Submit();
        }

        ShaderTagId[] _shaderTagIds = {
                                        new ShaderTagId("SRPDefaultUnlit"),
                                        new ShaderTagId("CustomLit")
                                      };
        void DrawVisibleGeometry()
        {

            PerObjectData lightsPerObjectFlags = asset.useLightsPerObject ? PerObjectData.LightData | PerObjectData.LightIndices : PerObjectData.None;
            //设置绘制序列和制定渲染相机
            var sortingSetting = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };

            //设置渲染的 Shader Pass 和排序模式
            var drawingSettings = new DrawingSettings()
            {
                enableDynamicBatching = asset.useDynamicBatching,
                enableInstancing = asset.useGPUInstancing,
                perObjectData = PerObjectData.Lightmaps| 
                                PerObjectData.LightProbe| 
                                PerObjectData.LightProbeProxyVolume| 
                                PerObjectData.ShadowMask|
                                PerObjectData.OcclusionProbe|
                                PerObjectData.OcclusionProbeProxyVolume|
                                PerObjectData.ReflectionProbes|
                                lightsPerObjectFlags
            };

            drawingSettings.sortingSettings = sortingSetting;
            for(int i = 0;i< _shaderTagIds.Length;i++)
            {
                drawingSettings.SetShaderPassName(i, _shaderTagIds[i]);
            }


            //设置哪些类型的渲染队列可以被绘制
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

            //1.绘制 opaque
            contenxt.DrawRenderers(culingResouts, ref drawingSettings, ref filteringSettings);
            //2.draw sky box
            contenxt.DrawSkybox(camera);

            if (useColorTexture || useDepthTexture) { 
                CopyAttachments();
            }

            //3.draw transparent
            sortingSetting.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSetting;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;

            contenxt.DrawRenderers(culingResouts, ref drawingSettings, ref filteringSettings);
        }
       

        void SetUp()
        {
            //设置相机的属性和矩阵
            //不设置我们将无法控制摄像机，设置它的Transform毫无作用
            contenxt.SetupCameraProperties(camera);

            //得到相机的清除状态
            CameraClearFlags flags = camera.clearFlags;

            useIntermediateBuffer = useColorTexture || useDepthTexture || postFXStack.IsActive;
            if (useIntermediateBuffer)
            {
                if (flags > CameraClearFlags.Color) flags = CameraClearFlags.Color;

                //frame buffer for the camera

                buffer.GetTemporaryRT(colorAttachmentId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, 
                                        useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);

                buffer.GetTemporaryRT(depthAttachmentId, camera.pixelWidth, camera.pixelHeight, 32, FilterMode.Point, RenderTextureFormat.Depth);

                buffer.SetRenderTarget(colorAttachmentId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,//color buffer
                                        depthAttachmentId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);//depth buffer


            }

            buffer.ClearRenderTarget(
                flags <= CameraClearFlags.Depth, 
                flags == CameraClearFlags.Color, 
                flags == CameraClearFlags.Color ? camera.backgroundColor.linear:Color.clear);

            buffer.BeginSample(SampleName);

            buffer.SetGlobalTexture(colorTextureId, missingTexture);
            buffer.SetGlobalTexture(depthTextureId, missingTexture);

            ExecuteBuffer();
        }
        void Submit()
        {
            buffer.EndSample(SampleName);
            ExecuteBuffer();
            contenxt.Submit();
        }
        void ExecuteBuffer()
        {
            contenxt.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        #region Culling
        //储存剔除后的结果数据
        CullingResults culingResouts;
        bool Cull(float maxShadowDistance)
        {
            ScriptableCullingParameters p;

            if(camera.TryGetCullingParameters(out p))
            {
                //得到最大阴影距离，和相机远截面作比较，取最小的那个作为阴影距离
                p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);

                culingResouts = contenxt.Cull(ref p);

                return true;
            }

            return false;
        }

        #endregion

        #region attachments of buffer
        void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, bool isDepth = false)
        {
            buffer.SetGlobalTexture(sourceTextureId, from);
            buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            buffer.DrawProcedural(Matrix4x4.identity, material, isDepth ? 1 : 0, MeshTopology.Triangles, 3);
        }

        void CopyAttachments()
        {
            if (useColorTexture)
            {
                buffer.GetTemporaryRT(colorTextureId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default );
                if (copyTextureSupported)
                {
                    buffer.CopyTexture(colorAttachmentId, colorTextureId);
                }
                else
                {
                    Draw(colorAttachmentId, colorTextureId);
                }
            }

            if (useDepthTexture)
            {
                buffer.GetTemporaryRT( depthTextureId, camera.pixelWidth, camera.pixelHeight,32, FilterMode.Point, RenderTextureFormat.Depth);
                if (copyTextureSupported)
                {
                    buffer.CopyTexture(depthAttachmentId, depthTextureId);
                }
                else
                {
                    Draw(depthAttachmentId, depthTextureId, true);
                    //buffer.SetRenderTarget(…);
                }
                //ExecuteBuffer();
            }

            if (!copyTextureSupported)
            {
                buffer.SetRenderTarget(
                    colorAttachmentId,RenderBufferLoadAction.Load, RenderBufferStoreAction.Store,
                    depthAttachmentId,RenderBufferLoadAction.Load, RenderBufferStoreAction.Store
                );
            }

            ExecuteBuffer();
        }

        #endregion
        void Cleanup()
        {
            lighting.Cleanup();
            if (useIntermediateBuffer)
            {
                buffer.ReleaseTemporaryRT(colorAttachmentId);
                buffer.ReleaseTemporaryRT(depthAttachmentId);

                if (useColorTexture)
                    buffer.ReleaseTemporaryRT(colorTextureId);

                if (useDepthTexture)
                    buffer.ReleaseTemporaryRT(depthTextureId);
            }
        }

        public void Dispose()
        {
            CoreUtils.Destroy(material);
            CoreUtils.Destroy(missingTexture);
        }

    }
}