using UnityEngine;
using UnityEngine.Rendering;

namespace CustomSR
{
    public partial class PostFXStack
    {

        const string bufferName = "Post FX";
        CommandBuffer buffer = new CommandBuffer
        {
            name = bufferName
        };

        enum Pass
        {
            Copy
        }

        ScriptableRenderContext context;
        Camera camera;
        PostFXSettings settings;
        public bool IsActive => settings != null;
        int fxSourceId = Shader.PropertyToID("_PostFXSource");

        public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings settings)
        {
            this.context = context;
            this.camera = camera;
            this.settings = settings;

            this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;

            ApplySceneViewState();
        }

        public void Render(int sourceId)
        {
            //buffer.Blit(sourceId, BuiltinRenderTextureType.CameraTarget);
            Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
        {
            buffer.SetGlobalTexture(fxSourceId, from);
            buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            //draw triangles on screen
            buffer.DrawProcedural(Matrix4x4.identity, settings.Material, (int)pass,MeshTopology.Triangles, 3);
        }
    }
}
