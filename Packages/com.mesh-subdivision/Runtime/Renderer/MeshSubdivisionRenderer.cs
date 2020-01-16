using UnityEngine;
using UnityEngine.Rendering;

namespace MeshSubdivision
{
    public struct Vertex
    {
        public Vector3 position;
        public Vector3 normal;
    }

    public struct Subdivision
    {
        public uint key;
        public uint primId;
    }

    public enum SubdInterp
    {
        None,
        PnTriangle,
        PhongTessellation,
    }

    public struct IndirectArg
    {
        // arguments        [KernelLod]  [DrawProcedural]
        public uint arg0; // dispatchX    indexCount
        public uint arg1; // dispatchY    numInstances
        public uint arg2; // dispatchZ    startIndex
        public uint arg3; // numSubd      startInstance
    }

    [ExecuteAlways]
    public partial class MeshSubdivisionRenderer : MonoBehaviour
    {
        // todo : need to find the sufficient maximum of subdivisions
        private const int MaxSubdivisions = 6000000;
        private const int NumThreads = 64;
        private class ShaderIDs
        {
            public static readonly int _ArgsOffsets          = Shader.PropertyToID("_ArgsOffsets");
            public static readonly int _VertexOrder          = Shader.PropertyToID("_VertexOrder");
            public static readonly int _LodParameters        = Shader.PropertyToID("_LodParameters");
            public static readonly int _Transform            = Shader.PropertyToID("_Transform");
            public static readonly int _CameraPosition       = Shader.PropertyToID("_CameraPosition");
            public static readonly int _CameraDirection      = Shader.PropertyToID("_CameraDirection");
            public static readonly int _FrustumPlanes        = Shader.PropertyToID("_FrustumPlanes");

            public static readonly int _VertexBuffer         = Shader.PropertyToID("_VertexBuffer");
            public static readonly int _IndexBuffer          = Shader.PropertyToID("_IndexBuffer");
            public static readonly int _SubdivisionBuffer    = Shader.PropertyToID("_SubdivisionBuffer");
            public static readonly int _CulledIndexBuffer    = Shader.PropertyToID("_CulledIndexBuffer");

            public static readonly int _RWIndirectArgsBuffer = Shader.PropertyToID("_RWIndirectArgsBuffer");
            public static readonly int _RWSubdivision0Buffer = Shader.PropertyToID("_RWSubdivision0Buffer");
            public static readonly int _RWSubdivision1Buffer = Shader.PropertyToID("_RWSubdivision1Buffer");
            public static readonly int _RWCulledIndexBuffer  = Shader.PropertyToID("_RWCulledIndexBuffer");

            public static readonly int _ViewMatrix           = Shader.PropertyToID("_ViewMatrix");
            public static readonly int _InvViewMatrix        = Shader.PropertyToID("_InvViewMatrix");
            public static readonly int _ProjMatrix           = Shader.PropertyToID("_ProjMatrix");
            public static readonly int _InvProjMatrix        = Shader.PropertyToID("_InvProjMatrix");
            public static readonly int _ViewProjMatrix       = Shader.PropertyToID("_ViewProjMatrix");
            public static readonly int _InvViewProjMatrix    = Shader.PropertyToID("_InvViewProjMatrix");
        };

        public Mesh Mesh;
        public SubdInterp Interpolation;
        public ComputeShader CS;
        public Material Material;
#if UNITY_EDITOR
        public Camera DebugCamera;
#endif

        public float MeshScale = 1.0f;
        public float PrimScale = 1.0f;
        public bool DoCulling = true;
        public int TargetPixelSize = 25;
        public int VertexOrder = 0;

        private bool _ping = true;
        private bool _pong = false;

        private ComputeBuffer _indirectArgumentBuffer = null;

        private ComputeBuffer _vertexBuffer = null;
        private ComputeBuffer _indexBuffer = null;

        // if SetCounterValue was used, it didn't work with 'DEBUG_PINGPONG'
        // these are used in SetRandomWriteTarget() for using them in DrawProceduralIndirect()
        private ComputeBuffer _null0Buffer = null;
        private ComputeBuffer _null1Buffer = null;

        private ComputeBuffer _subdivision0Buffer = null;
        private ComputeBuffer _subdivision1Buffer = null;
        private ComputeBuffer _culledIndexBuffer = null;

        private ComputeBuffer _subdivisionBuffer = null;
        private ComputeBuffer _outSubdivisionBuffer = null;

        private void OnEnable()
        {
            MeshSubdivisionManager.Instance.RegisterRenderer(this);

            if (Mesh == null)
                return;

            ReserveBuffers();
        }
        private void OnDisable()
        {
            MeshSubdivisionManager.Instance.UnregisterRenderer(this);

            ReleaseBuffers();
        }

        private void Update()
        {
            TargetPixelSize = Mathf.Clamp(TargetPixelSize, 1, 5000);
            VertexOrder = Mathf.Clamp(VertexOrder, 0, 2);
            MeshScale = Mathf.Max(1.0f, MeshScale);
        }

        public void ReloadMesh()
        {
            ReleaseMeshBuffers();
            FormInitialMesh();

            ReleaseSubdivisionBuffers();
            ReserveSubdivisionBuffers();

            InitializeIndirectArgumentBuffer();
        }

        public void ExecuteInitial()
        {
            if (IsInvalidBuffers())
                return;

            InitializeSubdivisionBuffers();
            InitializeIndirectArgumentBuffer();
        }

        public void ExecuteLod(CommandBuffer cmd, Camera camera)
        {
            string kernelName = "KernelLod";

            if (Interpolation == SubdInterp.PnTriangle)
                kernelName += "PnTriangle";
            else if (Interpolation == SubdInterp.PhongTessellation)
                kernelName += "PhongTess";

            if (DoCulling)
                kernelName += "Culling";

            int kernel = GetKernel(kernelName);
            if (kernel == -1)
                return;

#if UNITY_EDITOR
            bool overrideCamera =
                DebugCamera != null &&
                DebugCamera.enabled &&
                DebugCamera.isActiveAndEnabled;

            if (overrideCamera)
                camera = DebugCamera;
#endif

            float fov = (camera.fieldOfView * 0.5f) * Mathf.Deg2Rad;
            float targetPixelSize = TargetPixelSize;
            float screenResolution = camera.pixelWidth;
            float targetPixelRate = targetPixelSize / screenResolution;

            // is it scale in centermeter?
            float lodScale = 1.0f / (PrimScale * MeshScale * MeshScale * 100.0f);

            var position = gameObject.transform.position;

            var argsOffsets = new int[] { _ping ? 0 : 1, _pong ? 0 : 1, 2, };
            var vertexOrder = new int[] { (VertexOrder) % 3, (VertexOrder + 1) % 3, (VertexOrder + 2) % 3, };
            var lodParameters = new Vector4(fov, targetPixelRate, lodScale, 0.0f);
            var transform = new Vector4(position.x, position.y, position.z, MeshScale);
            var cameraPosition = camera.transform.position;
            var cameraDirection = camera.transform.forward;
            var frustumPlanes = MakeFrustumPlanes(camera);

            var viewMatrix = MeshSubdivisionManager.Instance.ViewMatrix;
            var projMatrix = MeshSubdivisionManager.Instance.ProjMatrix;
            var viewProjMatrix = MeshSubdivisionManager.Instance.ViewProjMatrix;

#if UNITY_EDITOR
            if (overrideCamera)
            {
                bool relativeCamera = false;

                viewMatrix = camera.worldToCameraMatrix;
                if (relativeCamera) viewMatrix.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

                projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
                viewProjMatrix = projMatrix * viewMatrix;
            }
#endif

            cmd.SetComputeIntParams(CS, ShaderIDs._ArgsOffsets, argsOffsets);

            cmd.SetComputeIntParams(CS, ShaderIDs._VertexOrder, vertexOrder);
            cmd.SetComputeVectorParam(CS, ShaderIDs._LodParameters, lodParameters);
            cmd.SetComputeVectorParam(CS, ShaderIDs._Transform, transform);
            cmd.SetComputeVectorParam(CS, ShaderIDs._CameraPosition, cameraPosition);
            cmd.SetComputeVectorParam(CS, ShaderIDs._CameraDirection, cameraDirection);
            cmd.SetComputeVectorArrayParam(CS, ShaderIDs._FrustumPlanes, frustumPlanes);

            cmd.SetComputeMatrixParam(CS, ShaderIDs._ViewMatrix, viewMatrix);
            cmd.SetComputeMatrixParam(CS, ShaderIDs._InvViewMatrix, viewMatrix.inverse);
            cmd.SetComputeMatrixParam(CS, ShaderIDs._ProjMatrix, projMatrix);
            cmd.SetComputeMatrixParam(CS, ShaderIDs._InvProjMatrix, projMatrix.inverse);
            cmd.SetComputeMatrixParam(CS, ShaderIDs._ViewProjMatrix, viewProjMatrix);
            cmd.SetComputeMatrixParam(CS, ShaderIDs._InvViewProjMatrix, viewProjMatrix.inverse);

            cmd.SetComputeBufferParam(CS, kernel, ShaderIDs._VertexBuffer, _vertexBuffer);
            cmd.SetComputeBufferParam(CS, kernel, ShaderIDs._IndexBuffer, _indexBuffer);

            cmd.SetComputeBufferParam(CS, kernel, ShaderIDs._RWIndirectArgsBuffer, _indirectArgumentBuffer);
            cmd.SetComputeBufferParam(CS, kernel, ShaderIDs._RWSubdivision0Buffer, _subdivisionBuffer);
            cmd.SetComputeBufferParam(CS, kernel, ShaderIDs._RWSubdivision1Buffer, _outSubdivisionBuffer);
            cmd.SetComputeBufferParam(CS, kernel, ShaderIDs._RWCulledIndexBuffer, _culledIndexBuffer);

            cmd.DispatchCompute(CS, kernel, _indirectArgumentBuffer, (uint)argsOffsets[0] * 16);
        }
        public void ExecuteIndirectBatcher(CommandBuffer cmd)
        {
            int kernel = GetKernel("KernelIndirectBatcher");
            if (kernel == -1)
                return;

            var argsOffsets = new int[] { _ping ? 0 : 1, _pong ? 0 : 1, 2 };

            cmd.SetComputeIntParams(CS, ShaderIDs._ArgsOffsets, argsOffsets);

            cmd.SetComputeBufferParam(CS, kernel, ShaderIDs._RWIndirectArgsBuffer, _indirectArgumentBuffer);
            cmd.SetComputeBufferParam(CS, kernel, ShaderIDs._RWSubdivision1Buffer, _outSubdivisionBuffer);
            cmd.SetComputeBufferParam(CS, kernel, ShaderIDs._RWCulledIndexBuffer, _culledIndexBuffer);

            cmd.DispatchCompute(CS, kernel, 1, 1, 1);
        }
        public void Render(CommandBuffer cmd, Camera camera)
        {
            if (IsInvalidBuffers())
                return;
            if (Material == null)
                return;

            var mat = Matrix4x4.identity;
            var pass = Material.FindPass("Lit");
            var topology = MeshTopology.Triangles;

            float fov = (camera.fieldOfView * 0.5f) * Mathf.Deg2Rad;
            float targetPixelSize = TargetPixelSize;
            float screenResolution = camera.pixelWidth;
            float targetPixelRate = targetPixelSize / screenResolution;

            // is it scale in centermeter?
            float lodScale = 1.0f / (PrimScale * MeshScale * MeshScale * 100.0f);

            var position = gameObject.transform.position;

            var vertexOrder = new Vector4((VertexOrder) % 3, (VertexOrder + 1) % 3, (VertexOrder + 2) % 3);
            var lodParameters = new Vector4(fov, targetPixelRate, lodScale, 0.0f);
            var transform = new Vector4(position.x, position.y, position.z, MeshScale);

            var viewMatrix = MeshSubdivisionManager.Instance.ViewMatrix;
            var projMatrix = MeshSubdivisionManager.Instance.ProjMatrix;
            var viewProjMatrix = MeshSubdivisionManager.Instance.ViewProjMatrix;

            if (Interpolation == SubdInterp.PnTriangle)
            {
                cmd.EnableShaderKeyword("_PN_TRIANGLE");
            }
            else if (Interpolation == SubdInterp.PhongTessellation)
            {
                cmd.EnableShaderKeyword("_PHONG_TESSELLATION");
            }
            else
            {
                cmd.DisableShaderKeyword("_PN_TRIANGLE");
                cmd.DisableShaderKeyword("_PHONG_TESSELLATION");
            }

            cmd.SetGlobalMatrix(ShaderIDs._ViewMatrix, viewMatrix);
            cmd.SetGlobalMatrix(ShaderIDs._InvViewMatrix, viewMatrix.inverse);
            cmd.SetGlobalMatrix(ShaderIDs._ProjMatrix, projMatrix);
            cmd.SetGlobalMatrix(ShaderIDs._InvProjMatrix, projMatrix.inverse);
            cmd.SetGlobalMatrix(ShaderIDs._ViewProjMatrix, viewProjMatrix);
            cmd.SetGlobalMatrix(ShaderIDs._InvViewProjMatrix, viewProjMatrix.inverse);

            cmd.SetGlobalVector(ShaderIDs._VertexOrder, vertexOrder);
            cmd.SetGlobalVector(ShaderIDs._LodParameters, lodParameters);
            cmd.SetGlobalVector(ShaderIDs._Transform, transform);

            cmd.SetGlobalBuffer(ShaderIDs._VertexBuffer, _vertexBuffer);
            cmd.SetGlobalBuffer(ShaderIDs._IndexBuffer, _indexBuffer);
            cmd.SetGlobalBuffer(ShaderIDs._SubdivisionBuffer, _subdivisionBuffer);
            cmd.SetGlobalBuffer(ShaderIDs._CulledIndexBuffer, _culledIndexBuffer);

            cmd.DrawProceduralIndirect(mat, Material, pass, topology, _indirectArgumentBuffer, 32);
        }

        private int GetKernel(string kernelName)
        {
            if (CS == null)
                return -1;
            if (IsInvalidBuffers())
                return -1;

            return CS.FindKernel(kernelName);
        }

        private Vector4[]MakeFrustumPlanes(Camera camera)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var frustumPlanes = new Vector4[6]
            {
                new Vector4(-planes[0].normal.x, -planes[0].normal.y, -planes[0].normal.z, planes[0].distance),
                new Vector4(-planes[1].normal.x, -planes[1].normal.y, -planes[1].normal.z, planes[1].distance),
                new Vector4(-planes[2].normal.x, -planes[2].normal.y, -planes[2].normal.z, planes[2].distance),
                new Vector4(-planes[3].normal.x, -planes[3].normal.y, -planes[3].normal.z, planes[3].distance),
                new Vector4(-planes[4].normal.x, -planes[4].normal.y, -planes[4].normal.z, planes[4].distance),
                new Vector4(-planes[5].normal.x, -planes[5].normal.y, -planes[5].normal.z, planes[5].distance),
            };

            return frustumPlanes;
        }
    }
}
