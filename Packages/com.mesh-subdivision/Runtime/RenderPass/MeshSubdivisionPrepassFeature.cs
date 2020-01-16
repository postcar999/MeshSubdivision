//#define DEBUG_PINGPONG

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.LWRP;

namespace MeshSubdivision
{
    public class MeshSubdivisionPrepassFeature : ScriptableRendererFeature
    {
        class MeshSubdivisionPrepass : ScriptableRenderPass
        {
            public MeshSubdivisionPrepass()
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
                Camera camera = renderingData.cameraData.camera;

                MeshSubdivisionManager.Instance.Update(camera);

                CommandBuffer cmd;

#if DEBUG_PINGPONG
                MeshSubdivisionManager.Instance.ExecuteInitial();
#endif

                cmd = CommandBufferPool.Get("KernelLod");
                MeshSubdivisionManager.Instance.ExecuteLod(cmd, camera);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

                cmd = CommandBufferPool.Get("KernelIndirectBatcher");
                MeshSubdivisionManager.Instance.ExecuteIndirectBatcher(cmd);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);

#if DEBUG_PINGPONG
                for (int i = 0; i < 2; i++)
                {
                    cmd = CommandBufferPool.Get("Swap SubdivisionBuffers");
                    MeshSubdivisionManager.Instance.SwapBuffers(cmd);
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);

                    cmd = CommandBufferPool.Get("KernelLod");
                    MeshSubdivisionManager.Instance.ExecuteLod(cmd, camera);
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);

                    cmd = CommandBufferPool.Get("KernelIndirectBatcher");
                    MeshSubdivisionManager.Instance.ExecuteIndirectBatcher(cmd);
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }
#endif
            }

            /// Cleanup any allocated resources that were created during the execution of this render pass.
            public override void FrameCleanup(CommandBuffer cmd)
            {

            }
        }

        MeshSubdivisionPrepass _meshSubdivisionPrepass;

        public override void Create()
        {
            _meshSubdivisionPrepass = new MeshSubdivisionPrepass()
            {
                renderPassEvent = RenderPassEvent.BeforeRendering,
            };
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(_meshSubdivisionPrepass);
        }
    }
}


