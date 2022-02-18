
using UnityEngine;
using UnityEngine.Rendering;
namespace CustomSR
{
    [CreateAssetMenu(menuName = "Rendering/CreateCustomRenderPipeline")]
    public partial  class CustomRendePineAsset : RenderPipelineAsset
    {

        [System.Serializable]
        public struct CameraBufferSettings
        {

            public bool allowHDR;

            public bool copyColor;
            public bool copyColorReflection;

            public bool copyDepth;
            public bool copyDepthReflections;

            [Range(0.1f, 2f)]
            public float renderScale;
        }

        //Start is called before the first frame update
        //public bool allowHDR = true;
        [SerializeField]
        public CameraBufferSettings cameraBuffer = new CameraBufferSettings
        {
            allowHDR = true,
            renderScale = 1f
        };

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

        [SerializeField]
        public Shader cameraRendererShader = default;
        protected override RenderPipeline CreatePipeline()
        {
            return new CustomRenderPipeline(this);
        }


    }
}