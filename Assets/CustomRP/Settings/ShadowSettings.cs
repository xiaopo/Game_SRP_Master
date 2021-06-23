
using UnityEngine;

namespace CustomSR
{
    [System.Serializable]
    public class ShadowSettings
    {
        //��Ӱ������
        [Min(0f)]
        public float maxDistance = 100f;

        //��Ӱ��ͼ��С
        public enum TextureSize
        {
            _256 = 256, _512 = 512, _1024 = 1024,
            _2048 = 2048, _4096 = 4096, _8192 = 8192
        }
        //��������Ӱ����
        [System.Serializable]
        public struct Directional
        {
            public TextureSize atlasSize;
        }
        //������Դ����Ӱ����

        //Ĭ�ϳߴ�Ϊ1024
        public Directional directional = new Directional
        {
            atlasSize = TextureSize._1024
        };


    }
}