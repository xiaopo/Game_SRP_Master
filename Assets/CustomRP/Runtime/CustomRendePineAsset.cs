using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace CustomSR
{
    [CreateAssetMenu(menuName = "Rendering/CreateCustomRenderPipeline")]
    public class CustomRendePineAsset : RenderPipelineAsset
    {
        // Start is called before the first frame update

        public bool useDynamicBatching = true;
        public bool useGPUInstancing = true;
        public bool useSRPBatcher = true;
        public bool useLightsPerObject = false;
        
        [SerializeField]
        public ShadowSettings shadows = default;

        protected override RenderPipeline CreatePipeline()
        {
            return new CustomRenderPipeline(this);
        }
    }
}