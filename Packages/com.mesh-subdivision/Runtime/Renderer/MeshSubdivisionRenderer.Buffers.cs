using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace MeshSubdivision
{
    public partial class MeshSubdivisionRenderer
    {
        public void SwapSubdivisionBuffers(CommandBuffer cmd)
        {
            if (IsInvalidBuffers())
                return;

            if (_ping)
            {
                _subdivisionBuffer    = _subdivision1Buffer;
                _outSubdivisionBuffer = _subdivision0Buffer;

                _ping = false;
                _pong = true;
            }
            else
            {
                _subdivisionBuffer    = _subdivision0Buffer;
                _outSubdivisionBuffer = _subdivision1Buffer;

                _ping = true;
                _pong = false;
            }

            cmd.SetRandomWriteTarget(0, _subdivisionBuffer, false);
            cmd.SetRandomWriteTarget(1, _culledIndexBuffer, false);

            cmd.SetRandomWriteTarget(0, _null0Buffer, false);
            cmd.SetRandomWriteTarget(1, _null1Buffer, false);
        }

        private bool IsValidBuffers()
        {
            return
                _indirectArgumentBuffer != null &&
                _vertexBuffer != null &&
                _indexBuffer != null &&
                _subdivision0Buffer != null &&
                _subdivision1Buffer != null;
        }
        private bool IsInvalidBuffers()
        {
            return IsValidBuffers() == false;
        }

        private void FormInitialMesh()
        {
            int numVertices = Mesh.vertices.Length;
            int vertexStride = Marshal.SizeOf(typeof(Vertex));

            var vertexList = new Vertex[numVertices];
            for (int i = 0; i < numVertices; i++)
            {
                vertexList[i].position = Mesh.vertices[i];
                vertexList[i].normal = Mesh.normals[i];
            }

            _vertexBuffer = new ComputeBuffer(Mesh.vertices.Length, vertexStride);
            _vertexBuffer.SetData(vertexList);

            int numIndices = Mesh.GetIndices(0).Length;
            int indexStride = sizeof(uint);

            _indexBuffer = new ComputeBuffer(numIndices, indexStride);
            _indexBuffer.SetData(Mesh.GetIndices(0));
        }
        private void ReserveNullBuffers()
        {
            int num = 1;
            int stride = 4;
            var type = ComputeBufferType.Structured;

            _null0Buffer = new ComputeBuffer(num, stride, type);
            _null1Buffer = new ComputeBuffer(num, stride, type);
        }
        private void ReserveSubdivisionBuffers()
        {
            // todo : estimate the maximum of subdivision count
            int numFaces = _indexBuffer.count / 3;
            int numReserved = MaxSubdivisions;// numFaces * (2 << 12);
            int subdivisionStride = Marshal.SizeOf(typeof(Subdivision));
            var type = ComputeBufferType.Counter;

            _subdivision0Buffer = new ComputeBuffer(numReserved, subdivisionStride, type);
            _subdivision1Buffer = new ComputeBuffer(numReserved, subdivisionStride, type);

            int numCulling = MaxSubdivisions / 2;
            int cullingStride = sizeof(uint);

            _culledIndexBuffer = new ComputeBuffer(numCulling, cullingStride, type);

            InitializeSubdivisionBuffers();
        }
        private void ReserveIndirectBuffer()
        {
            int numDispatchArguments = 4;
            int numProceduralArguments = 4;

            int numArguments = 3 * (numDispatchArguments + numProceduralArguments);
            int argumentStride = 4;
            var type = ComputeBufferType.IndirectArguments;

            _indirectArgumentBuffer = new ComputeBuffer(numArguments, argumentStride, type);

            InitializeIndirectArgumentBuffer();
        }

        private void InitializeSubdivisionBuffers()
        {
            int numFaces = _indexBuffer.count / 3;

            var subdivisions = new Subdivision[numFaces];
            for (int i = 0; i < subdivisions.Length; ++i)
            {
                subdivisions[i].key = 1;
                subdivisions[i].primId = (uint)i;
            }

            _subdivision0Buffer.SetData(subdivisions);
            _subdivision0Buffer.SetCounterValue((uint)numFaces);
            _subdivision1Buffer.SetCounterValue(0);

            _culledIndexBuffer.SetCounterValue(0);

            _subdivisionBuffer = _subdivision0Buffer;
            _outSubdivisionBuffer = _subdivision1Buffer;

            _ping = true;
            _pong = false;
        }
        private void InitializeIndirectArgumentBuffer()
        {
            uint numFaces = (uint)_indexBuffer.count / 3;
            uint x0 = (numFaces + NumThreads - 1) / NumThreads;
            uint x1 = 0;
            uint x2 = 0;

            var arguments = new IndirectArg[]
            {
                new IndirectArg() // for KernelLod
                {
                    arg0 = x0,
                    arg1 = 1,
                    arg2 = 1,
                    arg3 = numFaces,
                },
                new IndirectArg() // for KernelLod
                {
                    arg0 = x1,
                    arg1 = 1,
                    arg2 = 1,
                    arg3 = 0,
                },
                new IndirectArg() // for DrawProcedural
                {
                    arg0 = 3,
                    arg1 = numFaces,
                    arg2 = 0,
                    arg3 = 0,
                },
            };

            _indirectArgumentBuffer.SetData(arguments);
        }

        private void ReleaseMeshBuffers()
        {
            SafeRelease(ref _vertexBuffer);
            SafeRelease(ref _indexBuffer);
        }
        private void ReleaseNullBuffers()
        {
            SafeRelease(ref _null0Buffer);
            SafeRelease(ref _null1Buffer);
        }
        private void ReleaseSubdivisionBuffers()
        {
            SafeRelease(ref _subdivision0Buffer);
            SafeRelease(ref _subdivision1Buffer);

            SafeRelease(ref _culledIndexBuffer);

            _subdivisionBuffer = null;
            _outSubdivisionBuffer = null;
        }
        private void ReleaseIndirectBuffer()
        {
            SafeRelease(ref _indirectArgumentBuffer);
        }

        public void ReserveBuffers()
        {
            FormInitialMesh();
            ReserveNullBuffers();
            ReserveSubdivisionBuffers();
            ReserveIndirectBuffer();
        }
        public void ReleaseBuffers()
        {
            ReleaseMeshBuffers();
            ReleaseNullBuffers();
            ReleaseSubdivisionBuffers();
            ReleaseIndirectBuffer();
        }

        private void SafeRelease(ref ComputeBuffer buffer)
        {
            if (buffer != null)
            {
                buffer.Release();
                buffer = null;
            }
        }
    }
}
