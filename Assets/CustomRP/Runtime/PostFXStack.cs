﻿using UnityEngine;
using UnityEngine.Rendering;
using static CustomSR.PostFXSettings;

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
            BloomHorizontal,
            Copy,
            Final,
            BloomVertical,
            BloomCombine,
            BloomPrefilter,
            BloomPrefilterFireflies,
            BloomScatter,
            BloomScatterFinal,
            ColorGradingNone,
            ToneMappingACES,
            ToneMappingNeutral,
            ToneMappingReinhard,
        }

        ScriptableRenderContext context;
        Camera camera;
        PostFXSettings settings;
        public bool IsActive => settings != null;
        int fxSourceId = Shader.PropertyToID("_PostFXSource");
        int fxSource2Id = Shader.PropertyToID("_PostFXSource2");
        int bloomBucibicUpsamplingId = Shader.PropertyToID("_BloomBicubicUpsampling");
        int bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter");
        int bloomThresholdId = Shader.PropertyToID("_BloomThreshold");
        int bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
        int bloomResultId = Shader.PropertyToID("_BloomResult");
        int bloomPyramidId;
        int colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments");
        int colorFilterId = Shader.PropertyToID("_ColorFilter");
        int whiteBalanceId = Shader.PropertyToID("_WhiteBalance");
        int splitToningShadowsId = Shader.PropertyToID("_SplitToningShadows");
        int splitToningHighlightsId = Shader.PropertyToID("_SplitToningHighlights");
        int channelMixerRedId = Shader.PropertyToID("_ChannelMixerRed");
        int channelMixerGreenId = Shader.PropertyToID("_ChannelMixerGreen");
        int channelMixerBlueId = Shader.PropertyToID("_ChannelMixerBlue");
        int smhShadowsId = Shader.PropertyToID("_SMHShadows");
        int smhMidtonesId = Shader.PropertyToID("_SMHMidtones");
        int smhHighlightsId = Shader.PropertyToID("_SMHHighlights");
        int smhRangeId = Shader.PropertyToID("_SMHRange");
        int colorGradingLUTId = Shader.PropertyToID("_ColorGradingLUT");
        int colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters");
        int colorGradingLUTInLogId = Shader.PropertyToID("_ColorGradingLUTInLogC");
        int colorLUTResolution;
        bool useHDR;
        public PostFXStack()
        {
            bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
            for (int i = 1; i < maxBloomPyramidLevels * 2; i++)
            {
                Shader.PropertyToID("_BloomPyramid" + i);
            }
        }

        public void Setup(ScriptableRenderContext context, Camera camera, PostFXSettings settings,bool useHDR,int colorLUTResolution)
        {
            this.context = context;
            this.camera = camera;
            this.settings = settings;
            this.useHDR = useHDR;
            this.colorLUTResolution = colorLUTResolution;
            this.settings = camera.cameraType <= CameraType.SceneView ? settings : null;

            ApplySceneViewState();
        }

        public void Render(int sourceId)
        {
            //buffer.Blit(sourceId, BuiltinRenderTextureType.CameraTarget);
            if (DoBloom(sourceId))
            {
                DoColorGradingAndToneMapping(bloomResultId);
                buffer.ReleaseTemporaryRT(bloomResultId);
            }
            else{
                DoColorGradingAndToneMapping(sourceId);
            }

            context.ExecuteCommandBuffer(buffer);
            buffer.Clear();
        }

        void Draw(RenderTargetIdentifier from, RenderTargetIdentifier to, Pass pass)
        {
            //set to GPU while the shader will uses it 
            buffer.SetGlobalTexture(fxSourceId, from);

            //设置渲染目标，准备渲染
            buffer.SetRenderTarget(to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

            //draw triangles on screen with a material to render
            buffer.DrawProcedural(Matrix4x4.identity, settings.Material, (int)pass,MeshTopology.Triangles, 3);
        }

        void DrawFinal(RenderTargetIdentifier from)
        {
            buffer.SetGlobalTexture(fxSourceId, from);
            buffer.SetRenderTarget(BuiltinRenderTextureType.CameraTarget,RenderBufferLoadAction.Load, RenderBufferStoreAction.Store );
            buffer.SetViewport(camera.pixelRect);
            buffer.DrawProcedural(Matrix4x4.identity, settings.Material, (int)Pass.Final, MeshTopology.Triangles, 3);
        }

        bool DoBloom(int sourceId)
        {
            //buffer.BeginSample("Bloom");

            PostFXSettings.BloomSettings bloom = settings.Bloom;
            buffer.SetGlobalFloat(bloomBucibicUpsamplingId, bloom.bicubicUpsampling ? 1f : 0f);

            int width = camera.pixelWidth >> 1;
            int height = camera.pixelHeight >> 1;

            if (bloom.maxIterations == 0 || bloom.intensity <= 0 || height < bloom.downscaleLimit * 2 || width < bloom.downscaleLimit * 2)
            {
                //Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
               // buffer.EndSample("Bloom");
                return false;
            }

            buffer.BeginSample("Bloom");

            Vector4 threshold;
            threshold.x = Mathf.GammaToLinearSpace(bloom.threshold);
            threshold.y = threshold.x * bloom.thresholdKnee;
            threshold.z = 2f * threshold.y;
            threshold.w = 0.25f / (threshold.y + 0.00001f);
            threshold.y -= threshold.x;
            buffer.SetGlobalVector(bloomThresholdId, threshold);

            RenderTextureFormat format = useHDR ? RenderTextureFormat.DefaultHDR:RenderTextureFormat.Default;

            buffer.GetTemporaryRT(bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format);
            Draw(sourceId, bloomPrefilterId, this.settings.Bloom.fadeFireflies ?Pass.BloomPrefilterFireflies:Pass.BloomPrefilter);
            width = width >> 1;
            height = height >> 1;

            int fromId = bloomPrefilterId;
            int toId = bloomPyramidId + 1;

            int i;
            for (i = 0; i < bloom.maxIterations; i++)
            {
                if (height < bloom.downscaleLimit || width < bloom.downscaleLimit)
                {
                    break;
                }

                int midId = toId - 1;
                buffer.GetTemporaryRT( midId, width, height, 0, FilterMode.Bilinear, format);
                buffer.GetTemporaryRT( toId, width, height, 0, FilterMode.Bilinear, format);
                Draw(fromId, midId, Pass.BloomHorizontal);
                Draw(midId, toId, Pass.BloomVertical);

                fromId = toId;
                toId += 2;
                width = width >> 1;
                height = height >> 1;
            }

            buffer.ReleaseTemporaryRT(bloomPrefilterId);

            //Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.Copy);
            buffer.ReleaseTemporaryRT(fromId - 1);
            float finalIntensity;
            Pass combinePass,finalPass;
            if (bloom.mode == PostFXSettings.BloomSettings.Mode.Additive)
            {
                combinePass = finalPass = Pass.BloomCombine;
                buffer.SetGlobalFloat(bloomIntensityId, 1f);
                finalIntensity = bloom.intensity;
            }
            else
            {
                combinePass = Pass.BloomScatter;
                finalPass = Pass.BloomScatterFinal;
                buffer.SetGlobalFloat(bloomIntensityId, bloom.scatter);
                finalIntensity = Mathf.Min(bloom.intensity, 0.95f);
            }

            if ( i > 1)
            {
                toId -= 5;
                for (i -= 1; i > 0; i--)
                {
                    buffer.SetGlobalTexture(fxSource2Id, toId + 1);
                    Draw(fromId, toId, combinePass);

                    buffer.ReleaseTemporaryRT(fromId);
                    buffer.ReleaseTemporaryRT(fromId + 1);
                    fromId = toId;
                    toId -= 2;
                }
            }
            else
                buffer.ReleaseTemporaryRT(bloomPyramidId);


            buffer.SetGlobalFloat(bloomIntensityId, finalIntensity);
            buffer.SetGlobalTexture(fxSource2Id, sourceId);
            buffer.GetTemporaryRT(bloomResultId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, format);

            Draw(fromId, bloomResultId, finalPass);
            buffer.ReleaseTemporaryRT(fromId);
            buffer.EndSample("Bloom");

            return true;
        }


       
        void ConfigureColorAdjustments()
        {
            ColorAdjustmentsSettings colorAdjustments = settings.ColorAdjustments;

            buffer.SetGlobalVector(colorAdjustmentsId, new Vector4(
                                                                    Mathf.Pow(2f, colorAdjustments.postExposure),
                                                                    colorAdjustments.contrast * 0.01f + 1f,// 1 - 2
                                                                    colorAdjustments.hueShift * (1f / 360f),
                                                                    colorAdjustments.saturation * 0.01f + 1f// 1 - 2
                                                                     ));

            buffer.SetGlobalColor(colorFilterId, colorAdjustments.colorFilter.linear);
        }

        void ConfigureShadowsMidtonesHighlights()
        {
            ShadowsMidtonesHighlightsSettings smh = settings.ShadowsMidtonesHighlights;
            buffer.SetGlobalColor(smhShadowsId, smh.shadows.linear);
            buffer.SetGlobalColor(smhMidtonesId, smh.midtones.linear);
            buffer.SetGlobalColor(smhHighlightsId, smh.highlights.linear);
            buffer.SetGlobalVector(smhRangeId, new Vector4(
                smh.shadowsStart, smh.shadowsEnd, smh.highlightsStart, smh.highLightsEnd
            ));
        }


        void ConfigureChannelMixer()
        {
            ChannelMixerSettings channelMixer = settings.ChannelMixer;
            buffer.SetGlobalVector(channelMixerRedId, channelMixer.red);
            buffer.SetGlobalVector(channelMixerGreenId, channelMixer.green);
            buffer.SetGlobalVector(channelMixerBlueId, channelMixer.blue);
        }

        void ConfigureSplitToning()
        {
            SplitToningSettings splitToning = settings.SplitToning;
            Color splitColor = splitToning.shadows;
            splitColor.a = splitToning.balance * 0.01f;
            buffer.SetGlobalColor(splitToningShadowsId, splitColor);
            buffer.SetGlobalColor(splitToningHighlightsId, splitToning.highlights);
        }
        void ConfigureWhiteBalance()
        {
            WhiteBalanceSettings whiteBalance = settings.WhiteBalance;
            buffer.SetGlobalVector(whiteBalanceId, ColorUtils.ColorBalanceToLMSCoeffs(whiteBalance.temperature, whiteBalance.tint));
        }

        void DoColorGradingAndToneMapping(int sourceId)
        {
            ConfigureColorAdjustments();
            ConfigureWhiteBalance();
            ConfigureSplitToning();
            ConfigureChannelMixer();
            ConfigureShadowsMidtonesHighlights();

            int lutHeight = colorLUTResolution;
            int lutWidth = lutHeight * lutHeight;
            buffer.GetTemporaryRT(colorGradingLUTId, lutWidth, lutHeight, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);

            buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f)));

            ToneMappingSettings.Mode mode = settings.ToneMapping.mode;
            Pass pass = Pass.ColorGradingNone + (int)mode;
            buffer.SetGlobalFloat(colorGradingLUTInLogId, useHDR && pass != Pass.ColorGradingNone ? 1f : 0f);
            Draw(sourceId, colorGradingLUTId, pass);

            buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f));
           
            //Draw(sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Final);
            DrawFinal(sourceId);
            buffer.ReleaseTemporaryRT(colorGradingLUTId);
        }
    }
}
