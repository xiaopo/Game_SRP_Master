using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    
    
    ScriptableRenderContext contenxt;

    Camera camera;

    const string bufferName = "Render Camera";

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    Lighting lighting = new Lighting();

    public void Render(ScriptableRenderContext contenxt,Camera camera)
    {
        this.contenxt = contenxt;
        this.camera = camera;

        //设置命令缓冲区名字
        PrepareBuffer();
        //因为此操作可能给Scene场景中添加一些几何体，所以我们在Render()方法中进行几何体剔除之前用这个方法
        PrepareForSceneWindow();

        if (!Cull()) return;///被剔除

        SetUp();

        lighting.Setup(contenxt,culingResouts);
        //绘制几何体
        DrawVisibleGeometry();

        //绘制SRP不支持的着色器类型
        DrawUnsupportedShaders();
        //绘制辅助线
        DrawGizmos();

        Submit();
    }

    ShaderTagId[] _shaderTagIds = new ShaderTagId[2] { 
                                                        new ShaderTagId("SRPDefaultUnlit"),
                                                        new ShaderTagId("CustomLit") 
                                                      };
    void DrawVisibleGeometry()
    {

        //设置绘制序列和制定渲染相机
        var sortingSetting = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };

        //设置渲染的 Shader Pass 和排序模式
        var drawingSettings = new DrawingSettings()
        {
            enableDynamicBatching = CustomRenderPipeline.asset.useDynamicBatching,
            enableInstancing = CustomRenderPipeline.asset.useGPUInstancing,
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

        contenxt.SetupCameraProperties(camera);

        CameraClearFlags flags = camera.clearFlags;

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
    bool Cull()
    {
        ScriptableCullingParameters p;

        if(camera.TryGetCullingParameters(out p))
        {
            culingResouts = contenxt.Cull(ref p);

            return true;
        }

        return false;
    }

    #endregion

}
