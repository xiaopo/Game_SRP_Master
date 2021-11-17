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
        static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");
        public void Render(ScriptableRenderContext contenxt,Camera camera, CustomRendePineAsset asset)
        {
            this.contenxt = contenxt;
            this.camera = camera;
            this.asset = asset;

            //设置命令缓冲区名字
            PrepareBuffer();

            //因为此操作可能给Scene场景中添加一些几何体，所以我们在Render()方法中进行几何体剔除之前用这个方法
            PrepareForSceneWindow();

            if (!Cull(asset.shadows.maxDistance)) return;///被剔除

            buffer.BeginSample(SampleName);
            ExcuteBuffer();
            //渲染灯光
            lighting.Setup(contenxt, culingResouts, asset.shadows, asset.useLightsPerObject);
            //后处理
            postFXStack.Setup(contenxt, camera, asset.postFXSettings);
            buffer.EndSample(SampleName);

            SetUp();
            //绘制几何体
            DrawVisibleGeometry();
            //绘制SRP不支持的着色器类型
            DrawUnsupportedShaders();
            //绘制辅助线
            DrawGizmosBeforeFX();
            //后处理
            if (postFXStack.IsActive){
                postFXStack.Render(frameBufferId);
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

            //后处理部分
            if (postFXStack.IsActive)
            {
                if (flags > CameraClearFlags.Color) flags = CameraClearFlags.Color;

                //intermediate frame buffer for the camera
                buffer.GetTemporaryRT(frameBufferId, camera.pixelWidth, camera.pixelHeight,32, FilterMode.Bilinear, RenderTextureFormat.Default);
                buffer.SetRenderTarget(frameBufferId,RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            }

            buffer.ClearRenderTarget(
                flags <= CameraClearFlags.Depth, 
                flags == CameraClearFlags.Color, 
                flags == CameraClearFlags.Color ? camera.backgroundColor.linear:Color.clear);

            buffer.BeginSample(SampleName);

            ExcuteBuffer();


        }
        void Submit()
        {
            buffer.EndSample(SampleName);
            ExcuteBuffer();
            contenxt.Submit();
        }
        void ExcuteBuffer()
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

        void Cleanup()
        {
            lighting.Cleanup();
            if (postFXStack.IsActive)
            {
                buffer.ReleaseTemporaryRT(frameBufferId);
            }
        }

    }
}