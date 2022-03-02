
using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace CustomSR
{
    [CreateAssetMenu(menuName = "Rendering/CreateCustomRenderPipeline")]
    public partial  class CustomRendePineAsset : RenderPipelineAsset
    {
       
        [Serializable]
        public struct CameraBufferSettings
        {
            public enum BicubicRescalingMode { Off, UpOnly, UpAndDown }

            public bool allowHDR;

            public bool copyColor;
            public bool copyColorReflection;

            public bool copyDepth;
            public bool copyDepthReflections;

            [Range(0.1f, 2f)]
            public float renderScale;

            public BicubicRescalingMode bicubicRescaling;

            [Serializable]
            public struct FXAA
            {
                public bool enabled;

                // Trims the algorithm from processing darks.
                //   0.0833 - upper limit (default, the start of visible unfiltered edges)
                //   0.0625 - high quality (faster)
                //   0.0312 - visible limit (slower)
                [Range(0.0312f, 0.0833f)]
                public float fixedThreshold;

                // The minimum amount of local contrast required to apply algorithm.
                //   0.333 - too little (faster)
                //   0.250 - low quality
                //   0.166 - default
                //   0.125 - high quality 
                //   0.063 - overkill (slower)
                [Range(0.063f, 0.333f)]
                public float relativeThreshold;
            }

            public FXAA fxaa;
        }


        [SerializeField]
        public CameraBufferSettings cameraBuffer = new CameraBufferSettings
        {
            allowHDR = true,
            renderScale = 1f,
            fxaa = new CameraBufferSettings.FXAA
            {
                fixedThreshold = 0.0833f,
                relativeThreshold = 0.166f
            }
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