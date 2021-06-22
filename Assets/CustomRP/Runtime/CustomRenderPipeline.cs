
using UnityEngine;
using UnityEngine.Rendering;
namespace CustomSR
{
    public class CustomRenderPipeline : RenderPipeline
    {
        CameraRenderer render = new CameraRenderer();
        public CustomRendePineAsset asset;
        public CustomRenderPipeline(CustomRendePineAsset asset)
        {
            GraphicsSettings.useScriptableRenderPipelineBatching = asset.useSRPBatcher;
            //�ƹ�ʹ������ǿ��
            GraphicsSettings.lightsUseLinearIntensity = true;

            this.asset = asset;
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