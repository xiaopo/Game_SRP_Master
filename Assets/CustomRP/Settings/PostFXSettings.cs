﻿using System;
using UnityEngine;
namespace CustomSR
{
    [CreateAssetMenu(menuName = "Rendering/Custom Post FX Settings")]
    public class PostFXSettings : ScriptableObject 
    {
        [SerializeField]
        Shader shader = default;

        [System.NonSerialized]
        Material material;

        [System.Serializable]
        public struct BloomSettings
        {

            [Range(0f, 16f)]
            public int maxIterations;

            [Min(1f)]
            public int downscaleLimit;

            public bool bicubicUpsampling;

            [Min(0f)]
            public float threshold;

            [Range(0f, 1f)]
            public float thresholdKnee;

            [Min(0f)]
            public float intensity;

            public bool fadeFireflies;

            public enum Mode { Additive, Scattering }

            public Mode mode;

            [Range(0.05f, 0.95f)]
            public float scatter;
        }

        [SerializeField]
        BloomSettings bloom = new BloomSettings
        {
            scatter = 0.7f
        };
        public BloomSettings Bloom => bloom;

        [System.Serializable]
        public struct ToneMappingSettings
        {
            public enum Mode { None, ACES, Neutral, Reinhard }
            public Mode mode;
        }

        [SerializeField]
        ToneMappingSettings toneMapping = default;

        public ToneMappingSettings ToneMapping => toneMapping;

        [Serializable]
        public struct ColorAdjustmentsSettings
        {

            public float postExposure;//曝光度

            [Range(-100f, 100f)]
            public float contrast;//对比度

            [ColorUsage(false, true)]
            public Color colorFilter;//颜色滤镜

            [Range(-180f, 180f)]
            public float hueShift;//色调偏移

            [Range(-100f, 100f)]
            public float saturation;//饱和度
        }

        [SerializeField]
        ColorAdjustmentsSettings colorAdjustments = new ColorAdjustmentsSettings
        {
            colorFilter = Color.white
        };

        [Serializable]
        public struct WhiteBalanceSettings
        {

            [Range(-100f, 100f)]
            public float temperature, tint;
        }

        [SerializeField]
        WhiteBalanceSettings whiteBalance = default;

        public WhiteBalanceSettings WhiteBalance => whiteBalance;

        public ColorAdjustmentsSettings ColorAdjustments => colorAdjustments;

        [Serializable]
        public struct SplitToningSettings
        {

            [ColorUsage(false)]
            public Color shadows, highlights;

            [Range(-100f, 100f)]
            public float balance;
        }

        [SerializeField]
        SplitToningSettings splitToning = new SplitToningSettings
        {
            shadows = Color.gray,
            highlights = Color.gray
        };

        public SplitToningSettings SplitToning => splitToning;

        [Serializable]
        public struct ChannelMixerSettings
        {

            public Vector3 red, green, blue;
        }

        [SerializeField]
        ChannelMixerSettings channelMixer = new ChannelMixerSettings
        {
            red = Vector3.right,
            green = Vector3.up,
            blue = Vector3.forward
        };

        public ChannelMixerSettings ChannelMixer => channelMixer;

        [Serializable]
        public struct ShadowsMidtonesHighlightsSettings
        {

            [ColorUsage(false, true)]
            public Color shadows, midtones, highlights;

            [Range(0f, 2f)]
            public float shadowsStart, shadowsEnd, highlightsStart, highLightsEnd;
        }

        [SerializeField]
        ShadowsMidtonesHighlightsSettings shadowsMidtonesHighlights = new ShadowsMidtonesHighlightsSettings
                                                                            {
                                                                                shadows = Color.white,
                                                                                midtones = Color.white,
                                                                                highlights = Color.white,
                                                                                shadowsEnd = 0.3f,
                                                                                highlightsStart = 0.55f,
                                                                                highLightsEnd = 1f
                                                                            };

        public ShadowsMidtonesHighlightsSettings ShadowsMidtonesHighlights => shadowsMidtonesHighlights;
        public Material Material
        {
            get
            {
                if (material == null && shader != null)
                {
                    material = new Material(shader);
                    material.hideFlags = HideFlags.HideAndDontSave;
                }
                return material;
            }
        }
    }
}