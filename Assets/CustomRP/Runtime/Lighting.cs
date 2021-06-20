

using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    const string bufferName = "Lighting";

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    //static int dirLightColorId = Shader.PropertyToID("_DirectionalLightColor");
    //static int dirLightDirectionId = Shader.PropertyToID("_DirectionlLightDirection");

    //定义最大可见直接光
    const int maxDirLightCount = 4;

    static int dirLightCountId = Shader.PropertyToID("_DirectionLightCount");
    static int dirLightColorsId = Shader.PropertyToID("_DirectionLightColors");
    static int dirLightDirectionsId = Shader.PropertyToID("_DirectionLightDrections");


    //储存可见光的颜色和方向
    static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    static Vector4[] dirLightDirectioins = new Vector4[maxDirLightCount];

    //裁剪信息
    CullingResults cullingResults;
    public void Setup(ScriptableRenderContext context,CullingResults cullingResults,Camera camera)
    {
        this.cullingResults = cullingResults;

        buffer.BeginSample(bufferName);

        //发送光源数据
        SetupLights(camera);

        //SetupDirectionLight();
        buffer.EndSample(bufferName);

        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void SetupLights(Camera camera)
    {
        //Unity 会在剔除阶段计算哪些光源会影响相机的可见性
        //得到所有可见光
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        if (visibleLights == null) return;

        int dirLightCount = 0;
        for(int i = 0;i< visibleLights.Length;i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            //如果是方向光，我们才进行数据储存
            if(visibleLight.lightType == LightType.Directional)
            {
                //Visible 结构很大，我们改为传递引用不是传递值，这样不会生成副本
                SetupDirectionLight(dirLightCount++,ref visibleLight);

                if (dirLightCount > maxDirLightCount) break;
            }
        }

        //设置数据到Shader中
        buffer.SetGlobalInt(dirLightCountId, dirLightCount);
        buffer.SetGlobalVectorArray(dirLightColorsId,dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirectioins);
    }

    void SetupDirectionLight(int index, ref VisibleLight visibleLight)
    {
        //Light light = RenderSettings.sun;
        //if (light == null) return;
        //buffer.SetGlobalVector(dirLightColorId,light.color.linear * light.intensity);
        //buffer.SetGlobalVector(dirLightDirectionId, -light.transform.forward);

        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirectioins[index] = -visibleLight.localToWorldMatrix.GetColumn(2);

    }
}