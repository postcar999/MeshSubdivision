using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MeshSubdivision
{
    public partial class MeshSubdivisionFeature : ScriptableRendererFeature
    {
        internal class MeshSubdivisionDepthPrepass : ScriptableRenderPass
        {
            private Material _meshSubdMat = null;
            private RenderTargetHandle _depthTexture;

            internal MeshSubdivisionDepthPrepass(Material meshSubdMat)
            {
                _meshSubdMat = meshSubdMat;
                _depthTexture.Init("_CameraDepthTexture");
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                ConfigureTarget(_depthTexture.Identifier());
                ConfigureClear(ClearFlag.None, Color.black);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var camera = renderingData.cameraData.camera;
                var bufferIndex = MeshSubdivisionFeature.ToBufferIndex(camera.cameraType);
                var meshSubdCompList = MeshSubdivisionFeature.ComponentList;

                var cmd = CommandBufferPool.Get("Depth Prepass MeshSubdivision");
                cmd.name = "Depth Prepass MeshSubdivision";

                for (int i = 0; i < meshSubdCompList.Count; ++i)
                {
                    var comp = meshSubdCompList[i];
                    var buffers = comp.BuffersList[bufferIndex];

                    var mat = comp.transform.localToWorldMatrix;
                    var pass = _meshSubdMat.FindPass("DepthOnly");
                    var topology = MeshTopology.Triangles;

                    var lodParameters = comp.BuildLodParameters(camera);

                    var vertexBuffer = buffers.VertexBuffer;
                    var indexBuffer = buffers.IndexBuffer;
                    var subdBuffer = buffers.InSubdivisionBuffer;
                    var cullBuffer = buffers.CulledIndexBuffer;
                    var argsBuffer = buffers.IndirectArgumentBuffer;

                    var argsOffset = comp.EnableCulling ? 48 : 32;

                    var interpPnTriangle = comp.SubdInterp == SubdInterp.PnTriangle;
                    var interpPhongTess = comp.SubdInterp == SubdInterp.PhongTessellation;

                    CoreUtils.SetKeyword(cmd, "_CULLING", comp.EnableCulling);
                    CoreUtils.SetKeyword(cmd, "_VISUALIZATION", false);
                    CoreUtils.SetKeyword(cmd, "_PN_TRIANGLE", interpPnTriangle);
                    CoreUtils.SetKeyword(cmd, "_PHONG_TESSELLATION", interpPhongTess);

                    cmd.SetGlobalVector(ShaderIDs._LodParameters, lodParameters);

                    cmd.SetGlobalBuffer(ShaderIDs._VertexBuffer, vertexBuffer);
                    cmd.SetGlobalBuffer(ShaderIDs._IndexBuffer, indexBuffer);
                    cmd.SetGlobalBuffer(ShaderIDs._SubdivisionBuffer, subdBuffer);
                    if (comp.EnableCulling)
                        cmd.SetGlobalBuffer(ShaderIDs._CulledIndexBuffer, cullBuffer);

                    cmd.DrawProceduralIndirect(mat, _meshSubdMat, pass, topology, argsBuffer, argsOffset);

                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }

                CommandBufferPool.Release(cmd);
            }
        }
    }
}
