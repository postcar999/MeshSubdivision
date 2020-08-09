using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MeshSubdivision
{
    public partial class MeshSubdivisionFeature : ScriptableRendererFeature
    {
        internal class MeshSubdivisionPrepass : ScriptableRenderPass
        {
            private ComputeShader _meshSubdCS;

            internal MeshSubdivisionPrepass(ComputeShader mehsSubdCS)
            {
                _meshSubdCS = mehsSubdCS;
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var meshSubdList = MeshSubdivisionFeature.ComponentList;

                for (int i = 0; i < meshSubdList.Count; ++i)
                {
                    var meshSubd = meshSubdList[i];
                    var camera = renderingData.cameraData.camera;

                    var bufferIndex = MeshSubdivisionFeature.ToBufferIndex(camera.cameraType);

                    meshSubd.ReserveBuffers(bufferIndex);
                }

                var cmd = CommandBufferPool.Get("Kernel Lod");

                for (int i = 0; i < meshSubdList.Count; ++i)
                {
                    var meshSubd = meshSubdList[i];
                    var camera = meshSubd.GetOverriddenCamera(renderingData.cameraData.camera);

                    ExecuteKernelLod(cmd, camera, meshSubd);

                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }
                CommandBufferPool.Release(cmd);

                cmd = CommandBufferPool.Get("Kernel IndirectBatcher");

                for (int i = 0; i < meshSubdList.Count; ++i)
                {
                    var meshSubd = meshSubdList[i];
                    var camera = renderingData.cameraData.camera;

                    ExecuteKernelIndirectBatcher(cmd, camera, meshSubd);

                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                }
                CommandBufferPool.Release(cmd);
            }

            private void ExecuteKernelLod(CommandBuffer cmd, Camera camera, MeshSubdivision meshSubd)
            {
                int kernel = GetKernelLod(meshSubd);
                if (kernel == -1)
                    return;

                var bufferIndex = MeshSubdivisionFeature.ToBufferIndex(camera.cameraType);
                var buffers = meshSubd.BuffersList[bufferIndex];

                bool ping = buffers.Ping;
                bool pong = buffers.Pong;

                var argsOffsets = new int[] { ping ? 0 : 1, pong ? 0 : 1, 2, 3, };
                var lodParameters = meshSubd.BuildLodParameters(camera);
                var cameraPosition = camera.transform.position;
                var frustumPlanes = meshSubd.BuildFrustumPlanes(camera);

                var viewProjMatrix = CalculateViewProjMatrix(camera);
                var objectToWorldMatrix = meshSubd.transform.localToWorldMatrix;

                var vertexBuffer = buffers.VertexBuffer;
                var indexBuffer = buffers.IndexBuffer;
                var argsBuffer = buffers.IndirectArgumentBuffer;
                var inSubdBuffer = buffers.InSubdivisionBuffer;
                var outSubdBuffer = buffers.OutSubdivisionBuffer;
                var cullBuffer = buffers.CulledIndexBuffer;

                cmd.SetComputeIntParams(_meshSubdCS, ShaderIDs._ArgsOffsets, argsOffsets);
                cmd.SetComputeVectorParam(_meshSubdCS, ShaderIDs._LodParameters, lodParameters);

                cmd.SetComputeVectorParam(_meshSubdCS, ShaderIDs._WorldSpaceCameraPos, cameraPosition);
                cmd.SetComputeVectorArrayParam(_meshSubdCS, ShaderIDs._FrustumPlanes, frustumPlanes);

                cmd.SetComputeMatrixParam(_meshSubdCS, ShaderIDs._ViewProjMatrix, viewProjMatrix);
                cmd.SetComputeMatrixParam(_meshSubdCS, ShaderIDs._ObjectToWorldMatrix, objectToWorldMatrix);

                cmd.SetComputeBufferParam(_meshSubdCS, kernel, ShaderIDs._VertexBuffer, vertexBuffer);
                cmd.SetComputeBufferParam(_meshSubdCS, kernel, ShaderIDs._IndexBuffer, indexBuffer);

                cmd.SetComputeBufferParam(_meshSubdCS, kernel, ShaderIDs._RWIndirectArgsBuffer, argsBuffer);
                cmd.SetComputeBufferParam(_meshSubdCS, kernel, ShaderIDs._RWSubdivision0Buffer, inSubdBuffer);
                cmd.SetComputeBufferParam(_meshSubdCS, kernel, ShaderIDs._RWSubdivision1Buffer, outSubdBuffer);
                cmd.SetComputeBufferParam(_meshSubdCS, kernel, ShaderIDs._RWCulledIndexBuffer, cullBuffer);

                cmd.DispatchCompute(_meshSubdCS, kernel, argsBuffer, (uint)argsOffsets[0] * 16);
            }

            private void ExecuteKernelIndirectBatcher(CommandBuffer cmd, Camera camera, MeshSubdivision meshSubd)
            {
                int kernel = GetKernelIndirectBatcher(meshSubd);
                if (kernel == -1)
                    return;

                var bufferIndex = MeshSubdivisionFeature.ToBufferIndex(camera.cameraType);
                var buffers = meshSubd.BuffersList[bufferIndex];

                bool ping = buffers.Ping;
                bool pong = buffers.Pong;

                var argsOffsets = new int[] { ping ? 0 : 1, pong ? 0 : 1, 2, 3 };

                var argsBuffer = buffers.IndirectArgumentBuffer;
                var inSubdBuffer = buffers.InSubdivisionBuffer;
                var outSubdBuffer = buffers.OutSubdivisionBuffer;
                var cullBuffer = buffers.CulledIndexBuffer;

                cmd.SetComputeIntParams(_meshSubdCS, ShaderIDs._ArgsOffsets, argsOffsets);

                cmd.SetComputeBufferParam(_meshSubdCS, kernel, ShaderIDs._RWIndirectArgsBuffer, argsBuffer);
                cmd.SetComputeBufferParam(_meshSubdCS, kernel, ShaderIDs._RWSubdivision0Buffer, inSubdBuffer);
                cmd.SetComputeBufferParam(_meshSubdCS, kernel, ShaderIDs._RWSubdivision1Buffer, outSubdBuffer);
                cmd.SetComputeBufferParam(_meshSubdCS, kernel, ShaderIDs._RWCulledIndexBuffer, cullBuffer);

                cmd.DispatchCompute(_meshSubdCS, kernel, 1, 1, 1);
            }

            private Matrix4x4 CalculateViewProjMatrix(Camera camera)
            {
                bool relativeCamera = false;

                var gpuView = camera.worldToCameraMatrix;
                if (relativeCamera) gpuView.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

                var gpuProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);

                var viewMatrix = gpuView;
                var projMatrix = gpuProj;
                var viewProjMatrix = gpuProj * gpuView;

                return viewProjMatrix;
            }

            private int GetKernelLod(MeshSubdivision meshSubd)
            {
                if (_meshSubdCS == null)
                    return -1;

                string kernelName = "KernelLod";

                if (meshSubd.SubdInterp == SubdInterp.PnTriangle)
                    kernelName += "PnTriangle";
                else if (meshSubd.SubdInterp == SubdInterp.PhongTessellation)
                    kernelName += "PhongTess";

                if (meshSubd.EnableCulling)
                    kernelName += "Culling";

                return _meshSubdCS.FindKernel(kernelName);
            }

            private int GetKernelIndirectBatcher(MeshSubdivision meshSubd)
            {
                if (_meshSubdCS == null)
                    return -1;

                string kernelName = "KernelIndirectBatcher";

                if (meshSubd.EnableCulling)
                    kernelName += "Culling";

                return _meshSubdCS.FindKernel(kernelName);
            }

            private Light GetMainLight(LightData lightData)
            {
                var mainLightIndex = lightData.mainLightIndex;
                if (mainLightIndex == -1)
                    return null;

                var mainLight = lightData.visibleLights[mainLightIndex].light;
                if (mainLight.type != LightType.Directional)
                    return null;

                return mainLight;
            }
        }
    }
}
