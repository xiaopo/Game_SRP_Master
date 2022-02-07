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
        public bool allowHDR = true;
        public bool useDynamicBatching = true;
        public bool useGPUInstancing = true;
        public bool useSRPBatcher = true;
        public bool useLightsPerObject = false;
        

        [SerializeField]
        public ShadowSettings shadows = default;

        [SerializeField]
        public PostFXSettings postFXSettings = default;

        public enum ColorLUTResolution:int { _16 = 16, _32 = 32, _64 = 64 }

        [SerializeField]
        public ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;
        protected override RenderPipeline CreatePipeline()
        {
            return new CustomRenderPipeline(this);
        }
    }
}