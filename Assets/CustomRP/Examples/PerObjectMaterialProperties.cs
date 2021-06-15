using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PerObjectMaterialProperties : MonoBehaviour
{
    static int mainColorId = Shader.PropertyToID("_MainColor");
    static int cutoffId = Shader.PropertyToID("_Cutoff");
    [SerializeField]
    Color mainColor = Color.white;
    [SerializeField]
    float cutoff = 0.5f;

    static MaterialPropertyBlock block;

    private void OnValidate()
    {
        if (block == null)
            block = new MaterialPropertyBlock();


        //…Ë÷√≤ƒ÷  Ù–‘
        block.SetColor(mainColorId, mainColor);
        block.SetFloat(cutoffId, cutoff);
        GetComponent<Renderer>().SetPropertyBlock(block);
    }

    private void Awake()
    {
        OnValidate();
    }
}
