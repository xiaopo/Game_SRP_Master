
using UnityEngine;
using UnityEngine.Rendering;
namespace CustomSR
{
    public partial class CustomRenderPipeline : RenderPipeline
    {
        CameraRenderer render = new CameraRenderer();
        public CustomRendePineAsset asset;
        public CustomRenderPipeline(CustomRendePineAsset asset)
        {
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
                this.render.Render(context, cam, asset);
            }
        }

    }
}