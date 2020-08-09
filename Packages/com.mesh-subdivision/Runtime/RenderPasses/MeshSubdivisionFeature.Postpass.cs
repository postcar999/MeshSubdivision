using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MeshSubdivision
{
    public partial class MeshSubdivisionFeature : ScriptableRendererFeature
    {
        internal class MeshSubdivisionPostpass : ScriptableRenderPass
        {
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var camera = renderingData.cameraData.camera;
                var bufferIndex = MeshSubdivisionFeature.ToBufferIndex(camera.cameraType);
                var meshSubdCompList = MeshSubdivisionFeature.ComponentList;

                for (int i = 0; i < meshSubdCompList.Count; ++i)
                {
                    var comp = meshSubdCompList[i];
                    comp.SwapBuffers(bufferIndex);
                }
            }
        }
    }
}
