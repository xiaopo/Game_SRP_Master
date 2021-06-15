
using UnityEngine;
using UnityEngine.Rendering;

public class CustomRenderPipeline :RenderPipeline
{
    CameraRenderer render = new CameraRenderer();
    public static CustomRendePineAsset asset;
    public CustomRenderPipeline(CustomRendePineAsset ast )
    {
        GraphicsSettings.useScriptableRenderPipelineBatching = ast.useSRPBatcher;

        asset = ast;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach(Camera cam in cameras)
        {
            this.render.Render(context, cam);
        }
    }

}
