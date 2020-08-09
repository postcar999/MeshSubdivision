using System.Runtime.InteropServices;
using UnityEngine;

namespace MeshSubdivision
{
    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
    }

    public struct SubdivisionPacked
    {
        public uint data0;
        public uint data1;
    }

    public struct IndirectArg
    {
        // arguments        [KernelLod]  [DrawProcedural]
        public uint arg0; // dispatchX    indexCount
        public uint arg1; // dispatchY    numInstances
        public uint arg2; // dispatchZ    startIndex
        public uint arg3; // numSubd      startInstance
    }

    public class MeshSubdivisionBuffers
    {
        // todo : need to find the sufficient maximum of subdivisions
        private const int MaxSubdivisions = 10000000;
        private const int NumThreads = 64;

        private bool _ping = true;
        private bool _pong = false;

        private uint _frameCount = 0;

        private ComputeBuffer _indirectArgumentBuffer = null;

        private ComputeBuffer _vertexBuffer = null;
        private ComputeBuffer _indexBuffer = null;

        private ComputeBuffer _subdivision0Buffer = null;
        private ComputeBuffer _subdivision1Buffer = null;
        private ComputeBuffer _culledIndexBuffer = null;

        public bool Ping => _ping;
        public bool Pong => _pong;

        public ComputeBuffer IndirectArgumentBuffer => _indirectArgumentBuffer;

        public ComputeBuffer VertexBuffer => _vertexBuffer;
        public ComputeBuffer IndexBuffer => _indexBuffer;

        public ComputeBuffer InSubdivisionBuffer  { get { return _ping ? _subdivision0Buffer : _subdivision1Buffer; } }
        public ComputeBuffer OutSubdivisionBuffer { get { return _pong ? _subdivision0Buffer : _subdivision1Buffer; } }
        public ComputeBuffer CulledIndexBuffer => _culledIndexBuffer;

        public void ConstructMesh(Vertex[] vertexList, int[] indexList)
        {
            int numVertices = vertexList.Length;
            int vertexStride = Marshal.SizeOf(typeof(Vertex));

            _vertexBuffer = new ComputeBuffer(numVertices, vertexStride);
            _vertexBuffer.SetData(vertexList);

            int numIndices = indexList.Length;
            int indexStride = sizeof(uint);

            _indexBuffer = new ComputeBuffer(numIndices, indexStride);
            _indexBuffer.SetData(indexList);
        }
        public void DestructMesh()
        {
            SafeRelease(ref _vertexBuffer);
            SafeRelease(ref _indexBuffer);
        }

        public void Reserve()
        {
            ReserveSubdivisionBuffers();
            ReserveIndirectArgumentBuffer();
        }
        public void Release()
        {
            ReleaseSubdivisionBuffers();
            ReleaseIndirectArgumentBuffer();
        }

        public void MakeSubdivisionInitialized()
        {
            InitializeSubdivisionBuffers();
            InitializeIndirectArgumentBuffer();
        }
        public void SwapSubdivisionBuffers()
        {
            if (IsInvalid())
                return;

            _frameCount++;
            if (_frameCount > 1)
            {
                _frameCount = 0;

                _ping = !_ping;
                _pong = !_ping;

                if (_ping) _subdivision0Buffer.SetCounterValue(0);
                if (_pong) _subdivision1Buffer.SetCounterValue(0);
            }

            _culledIndexBuffer.SetCounterValue(0);
        }

        public bool IsValid()
        {
            return
                _indirectArgumentBuffer != null &&
                _vertexBuffer           != null &&
                _indexBuffer            != null &&
                _subdivision0Buffer     != null &&
                _subdivision1Buffer     != null;
        }
        public bool IsInvalid()
        {
            return IsValid() == false;
        }

        private void SafeRelease(ref ComputeBuffer buffer)
        {
            if (buffer != null)
            {
                buffer.Release();
                buffer = null;
            }
        }

        private void ReserveSubdivisionBuffers()
        {
            // todo : estimate the maximum of subdivision count
            int numReserved = MaxSubdivisions;
            int subdivisionStride = Marshal.SizeOf(typeof(SubdivisionPacked));
            var type = ComputeBufferType.Counter;

            _subdivision0Buffer = new ComputeBuffer(numReserved, subdivisionStride, type);
            _subdivision1Buffer = new ComputeBuffer(numReserved, subdivisionStride, type);

            int numCulling = MaxSubdivisions / 2;
            int cullingStride = sizeof(uint);

            _culledIndexBuffer = new ComputeBuffer(numCulling, cullingStride, type);

            InitializeSubdivisionBuffers();
        }
        private void ReserveIndirectArgumentBuffer()
        {
            int numDispatchArguments = 4;
            int numProceduralArguments = 4;

            int numArguments = 2 * (numDispatchArguments + numProceduralArguments);
            int argumentStride = 4;
            var type = ComputeBufferType.IndirectArguments;

            _indirectArgumentBuffer = new ComputeBuffer(numArguments, argumentStride, type);

            InitializeIndirectArgumentBuffer();
        }

        private void ReleaseSubdivisionBuffers()
        {
            SafeRelease(ref _subdivision0Buffer);
            SafeRelease(ref _subdivision1Buffer);

            SafeRelease(ref _culledIndexBuffer);
        }
        private void ReleaseIndirectArgumentBuffer()
        {
            SafeRelease(ref _indirectArgumentBuffer);
        }

        private void InitializeSubdivisionBuffers()
        {
            int numFaces = _indexBuffer.count / 3;

            var subdivisions = new SubdivisionPacked[numFaces];
            for (int i = 0; i < subdivisions.Length; ++i)
            {
                subdivisions[i].data0 = 0x00000001;
                subdivisions[i].data1 = (uint)i;
            }

            _subdivision0Buffer.SetData(subdivisions);
            _subdivision0Buffer.SetCounterValue((uint)numFaces);
            _subdivision1Buffer.SetCounterValue(0);

            _culledIndexBuffer.SetCounterValue(0);

            _ping = true;
            _pong = false;
        }
        private void InitializeIndirectArgumentBuffer()
        {
            uint numFaces = (uint)_indexBuffer.count / 3;
            uint x0 = (numFaces + NumThreads - 1) / NumThreads;
            uint x1 = 0;

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
    }
}
