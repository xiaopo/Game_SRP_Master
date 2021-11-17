#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace CustomSR
{
    public partial class PostFXStack
    {

        partial void ApplySceneViewState();

#if UNITY_EDITOR

        partial void ApplySceneViewState()
        {
            if ( camera.cameraType == CameraType.SceneView &&!SceneView.currentDrawingSceneView.sceneViewState.showImageEffects)
            {
                settings = null;
            }
        }

#endif

    }
}
