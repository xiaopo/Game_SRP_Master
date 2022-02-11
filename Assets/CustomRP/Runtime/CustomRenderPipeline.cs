
using UnityEngine;
using UnityEngine.Rendering;
namespace CustomSR
{
    public partial class CustomRenderPipeline : RenderPipeline
    {
        CameraRenderer renderer;
        public CustomRendePineAsset asset;
        public CustomRenderPipeline(CustomRendePineAsset asset)
        {
            renderer = new CameraRenderer(asset.cameraRendererShader);
            GraphicsSettings.useScriptableRenderPipelineBatching = asset.useSRPBatcher;
            //灯光使用线性强度
            GraphicsSettings.lightsUseLinearIntensity = true;

            this.asset = asset;

            InitializeForEditor();
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            foreach (Camera cam in cameras)
            {
                this.renderer.Render(context, cam, asset);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            DisposeForEditor();
            renderer.Dispose();
        }

    }
}