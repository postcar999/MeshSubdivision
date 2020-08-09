using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MeshSubdivision
{
    public partial class MeshSubdivisionFeature : ScriptableRendererFeature
    {
        internal class MeshSubdivisionShadowCasterPass : ScriptableRenderPass
        {
            private Material _meshSubdMat = null;
            private RenderTargetHandle _mainLightShadowmapTexture;

            internal MeshSubdivisionShadowCasterPass(Material meshSubdMat)
            {
                _meshSubdMat = meshSubdMat;
                _mainLightShadowmapTexture.Init("_MainLightShadowmapTexture");
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                // todo : can't find '_MainLightShadowmapTexture' to set render target
                // because MainLightShadowCasterPass get temporary name from ShadowUtils, not '_MainLightShadowmapTexture'.
                // so this pass would be failed to set render target and render target has to be followed from MainLightShadowCasterPass
                ConfigureTarget(_mainLightShadowmapTexture.Identifier());
                ConfigureClear(ClearFlag.None, Color.black);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var camera = renderingData.cameraData.camera;
                var bufferIndex = MeshSubdivisionFeature.ToBufferIndex(camera.cameraType);
                var meshSubdCompList = MeshSubdivisionFeature.ComponentList;

                var cmd = CommandBufferPool.Get("Render Main Shadowmap MeshSubdivision");
                cmd.name = "Render Main Shadowmap MeshSubdivision";

                for (int i = 0; i < meshSubdCompList.Count; ++i)
                {
                    var comp = meshSubdCompList[i];
                    if (comp.CastShadows == false)
                        continue;

                    var buffers = comp.BuffersList[bufferIndex];

                    var mat = comp.transform.localToWorldMatrix;
                    var pass = _meshSubdMat.FindPass("ShadowCaster");
                    var topology = MeshTopology.Triangles;

                    var lodParameters = comp.BuildLodParameters(camera);

                    var vertexBuffer = buffers.VertexBuffer;
                    var indexBuffer = buffers.IndexBuffer;
                    var subdBuffer = buffers.InSubdivisionBuffer;
                    var argsBuffer = buffers.IndirectArgumentBuffer;

                    var argsOffset = 32; // position without culling

                    var interpPnTriangle = comp.SubdInterp == SubdInterp.PnTriangle;
                    var interpPhongTess = comp.SubdInterp == SubdInterp.PhongTessellation;

                    CoreUtils.SetKeyword(cmd, "_CULLING", false);
                    CoreUtils.SetKeyword(cmd, "_VISUALIZATION", false);
                    CoreUtils.SetKeyword(cmd, "_PN_TRIANGLE", interpPnTriangle);
                    CoreUtils.SetKeyword(cmd, "_PHONG_TESSELLATION", interpPhongTess);

                    cmd.SetGlobalVector(ShaderIDs._LodParameters, lodParameters);

                    cmd.SetGlobalBuffer(ShaderIDs._VertexBuffer, vertexBuffer);
                    cmd.SetGlobalBuffer(ShaderIDs._IndexBuffer, indexBuffer);
                    cmd.SetGlobalBuffer(ShaderIDs._SubdivisionBuffer, subdBuffer);

                    cmd.DrawProceduralIndirect(mat, _meshSubdMat, pass, topology, argsBuffer, argsOffset);

                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }

                CommandBufferPool.Release(cmd);
            }
        }
    }
}
