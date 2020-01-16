using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace MeshSubdivision
{
    public class MeshSubdivisionPostpassFeature : ScriptableRendererFeature
    {
        class MeshSubdivisionPostpass : ScriptableRenderPass
        {
            public MeshSubdivisionPostpass()
            {

            }

            // This method is called before executing the render pass. 
            // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
            // When empty this render pass will render to the active camera render target.
            // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
            // The render pipeline will ensure target setup and clearing happens in an performance manner.
            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {

            }

            // Here you can implement the rendering logic.
            // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
            // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
            // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var cmd = CommandBufferPool.Get("Swap SubdivisionBuffers");
                MeshSubdivisionManager.Instance.SwapBuffers(cmd);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            /// Cleanup any allocated resources that were created during the execution of this render pass.
            public override void FrameCleanup(CommandBuffer cmd)
            {

            }
        }

        MeshSubdivisionPostpass _meshSubdivisionPostpass;

        public override void Create()
        {
            _meshSubdivisionPostpass = new MeshSubdivisionPostpass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques,
            };
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_meshSubdivisionPostpass);
        }
    }
}


