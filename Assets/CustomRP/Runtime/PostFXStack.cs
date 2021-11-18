using UnityEngine;
using UnityEngine.Rendering;

namespace CustomSR
{
    public partial class PostFXStack
    {
        const int maxBloomPyramidLevels = 16;
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

        int bloomPyramidId;
        public PostFXStack()
        {
            bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
            for (int i = 1; i < maxBloomPyramidLevels; i++)
            {
                Shader.PropertyToID("_BloomPyramid" + i);
            }
        }

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
    
            DoBloom(sourceId);

            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
        {
            //set to GPU while the shader will uses it 
            buffer.SetGlobalTexture(fxSourceId, from);

            //设置渲染目标，准备渲染
            buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

            //draw triangles on screen
            buffer.DrawProcedural(Matrix4x4.identity, settings.Material, (int)pass,MeshTopology.Triangles, 3);
        }

        void DoBloom(int sourceId)
        {
            buffer.BeginSample("Bloom");
            int width = camera.pixelWidth / 2;
            int height = camera.pixelHeight / 2;
            RenderTextureFormat format = RenderTextureFormat.Default;
            int fromId = sourceId;
            int toId = bloomPyramidId;
            PostFXSettings.BloomSettings bloom = settings.Bloom;
            int i;
            for (i = 0; i < bloom.maxIterations; i++)
            {
                if (height < bloom.downscaleLimit || width < bloom.downscaleLimit)
                {
                    break;
                }
                buffer.GetTemporaryRT( toId, width, height, 0, FilterMode.Bilinear, format);
                Draw(fromId, toId, Pass.Copy);
                fromId = toId;
                toId += 1;
                width = width >> 1;
                height = height >> 1;
            }

            Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);

            for (i -= 1; i >= 0; i--)
            {
                buffer.ReleaseTemporaryRT(bloomPyramidId + i);
            }

            buffer.EndSample("Bloom");
        }
    }
}
