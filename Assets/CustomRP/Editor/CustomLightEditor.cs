using UnityEngine;
using UnityEditor;
using CustomSR;

[CanEditMultipleObjects]
[CustomEditorForRenderPipeline(typeof(Light), typeof(CustomRendePineAsset))]
public class CustomLightEditor : LightEditor 
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if ( !settings.lightType.hasMultipleDifferentValues &&
            (LightType)settings.lightType.enumValueIndex == LightType.Spot)
        {
            settings.DrawInnerAndOuterSpotAngle();
            settings.ApplyModifiedProperties();
        }
    }
}