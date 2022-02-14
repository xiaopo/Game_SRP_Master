
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
namespace CustomSR
{ 
    public partial class CameraRenderer
    {
        partial void DrawUnsupportedShaders();

    #if UNITY_EDITOR
        static ShaderTagId[] legacyShaderTagIds =
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM"),
        
        };

        static Material errorMaterial;
        /// <summary>
        /// 绘制SRP不支持的着色器类型
        /// </summary>
        partial void DrawUnsupportedShaders()
        {
            if (errorMaterial == null)
            {
                errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
            }


            var drawingSettings = new DrawingSettings();
            drawingSettings.sortingSettings = new SortingSettings(camera);
            drawingSettings.overrideMaterial = errorMaterial;
            for (int i = 0;i< legacyShaderTagIds.Length;i++)
            {
                drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
            }

            var filteringSettings = FilteringSettings.defaultValue;

            //绘制不支持的ShaderTag类型的物体
            contenxt.DrawRenderers(culingResouts, ref drawingSettings, ref filteringSettings);
        }

    #endif

        partial void DrawGizmosBeforeFX();
        partial void DrawGizmosAfterFX();
#if UNITY_EDITOR
        //绘制DrawGizmos
        partial void DrawGizmosBeforeFX()
        {
            if (Handles.ShouldRenderGizmos())
            {
                if (useIntermediateBuffer)
                {
                    Draw(depthAttachmentId, BuiltinRenderTextureType.CameraTarget, true);
                    ExecuteBuffer();
                }

                contenxt.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                //context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
        }

        partial void DrawGizmosAfterFX()
        {
            if (Handles.ShouldRenderGizmos())
            {
                contenxt.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
        }
#endif




        partial void PrepareForSceneWindow();
    #if UNITY_EDITOR
        /// <summary>
        /// 在Game视图绘制的几何体也绘制到Scene视图中
        /// </summary>
        partial void PrepareForSceneWindow()
        {
            if(camera.cameraType == CameraType.SceneView)
            {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
        }
    #endif

        partial void PrepareBuffer();
    #if UNITY_EDITOR
        string SampleName { get; set; }
        partial void PrepareBuffer()
        {
            Profiler.BeginSample("Editor Only");
            buffer.name = SampleName = camera.name;
            Profiler.EndSample();
        }
    #else
        const string SmapleName = bufferName;
    #endif

    }
}