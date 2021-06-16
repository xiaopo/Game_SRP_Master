

using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    const string bufferName = "Lighting";

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    static int dirLightColorId = Shader.PropertyToID("_DirectionLightColor");
    static int dirLightDirectionId = Shader.PropertyToID("_DirectionLightDrection");

    public void Setup(ScriptableRenderContext context)
    {
        buffer.BeginSample(bufferName);

        //发送光源数据
        SetupDirectionLight();
        buffer.EndSample(bufferName);

        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void SetupDirectionLight()
    {
        Light light = RenderSettings.sun;

        //灯光的颜色我们在乘上光强作为最终颜色
        buffer.SetGlobalVector(dirLightColorId,light.color.linear * light.intensity);

        buffer.SetGlobalVector(dirLightDirectionId, -light.transform.forward);
    }
}