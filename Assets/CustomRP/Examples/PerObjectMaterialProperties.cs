using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace CustomSR
{

    [DisallowMultipleComponent]
    public class PerObjectMaterialProperties : MonoBehaviour
    {
        static int mainColorId = Shader.PropertyToID("_MainColor");
        static int cutoffId = Shader.PropertyToID("_Cutoff");
        static int metallicId = Shader.PropertyToID("_Metallic");
        static int smoothnessId = Shader.PropertyToID("_Smoothness");
        static int emissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField]
        Color mainColor = Color.white;
        [SerializeField]
        float cutoff = 0.5f;

        [SerializeField,Range(0f,1f)]
        float metallic = 0f;
        [SerializeField, Range(0f, 1f)]
        float smoothness = 0.5f;

        [SerializeField, ColorUsage(false, true)]
        Color emissionColor = Color.black;


        static MaterialPropertyBlock block;

        private void OnValidate()
        {
            if (block == null)
                block = new MaterialPropertyBlock();


            //…Ë÷√≤ƒ÷  Ù–‘
            block.SetColor(mainColorId, mainColor);
            block.SetFloat(cutoffId, cutoff);
            block.SetFloat(metallicId, metallic);
            block.SetFloat(smoothnessId, smoothness);
            block.SetColor(emissionColorId, emissionColor);
            GetComponent<Renderer>().SetPropertyBlock(block);
        }

        private void Awake()
        {
            OnValidate();
        }
    }
}