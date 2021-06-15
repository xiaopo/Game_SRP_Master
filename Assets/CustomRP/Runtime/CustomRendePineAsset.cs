using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName ="Rendering/CreateCustomRenderPipeline")]
public class CustomRendePineAsset : RenderPipelineAsset
{
    // Start is called before the first frame update

    public bool useDynamicBatching = true;
    public bool useGPUInstancing = true;
    public bool useSRPBatcher = true;

    
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(this);
    }
}
