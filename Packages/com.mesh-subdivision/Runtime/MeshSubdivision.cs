#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

namespace MeshSubdivision
{
    public enum MeshType
    {
        Quad = 0,
        Cube,
        Sphere,

        Max,
    }

    public enum MeshShading
    {
        Lit,
        Visualization,
    }

    public enum SubdInterp
    {
        None,
        PnTriangle,
        PhongTessellation,
    }

    [DisallowMultipleComponent, ExecuteAlways]
    public partial class MeshSubdivision : MonoBehaviour
    {
#if UNITY_EDITOR
        private static string PackagePath => MeshSubdivisionFeature.PackagePath;

        [SerializeField] private Camera _debugCamera = null;
#endif

        [SerializeField] private Mesh[] _baseMeshes;
        [SerializeField] private Mesh _mesh;
        [SerializeField] private MeshType _meshType = MeshType.Quad;
        [SerializeField] private float _primScale;
        [SerializeField] private MeshShading _meshShading = MeshShading.Lit;
        [SerializeField] private SubdInterp _subdInterp = SubdInterp.PnTriangle;

        public Mesh Mesh => _mesh;
        public MeshShading MeshShading => _meshShading;
        public SubdInterp SubdInterp => _subdInterp;

        [SerializeField] private bool _castShadows = true;
        [SerializeField] private bool _enableCulling = true;
        [SerializeField] private int _targetPixelSize = 25;

        public bool CastShadows => _castShadows;
        public bool EnableCulling => _enableCulling;

        private MeshSubdivisionBuffers[] _buffersList = new MeshSubdivisionBuffers[5];
        public MeshSubdivisionBuffers[] BuffersList => _buffersList;

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (_baseMeshes == null)
            {
                _baseMeshes = LoadBaseMeshes((int)MeshType.Max);
                SetMesh(_meshType);
            }
#endif

            MeshSubdivisionFeature.Register(this);
        }
        private void OnDisable()
        {
            MeshSubdivisionFeature.Unregister(this);

            for (int i = 0; i < _buffersList.Length; ++i)
            {
                _buffersList[i]?.DestructMesh();
                _buffersList[i]?.Release();
            }
        }

        public void SetMesh(MeshType meshType)
        {
            var meshTypeIndex = (int)meshType;
            _mesh = _baseMeshes[meshTypeIndex];
            _primScale = LoadPrimScale(meshType);

            for (int i = 0; i < _buffersList.Length; ++i)
            {
                _buffersList[i]?.DestructMesh();
                _buffersList[i]?.Release();
            }
        }

        public void ReserveBuffers(int bufferIndex)
        {
            if (bufferIndex == -1)
            {
                Debug.Log("Try unknown buffer index in Mesh Subdivision");
                return;
            }

            if (_buffersList[bufferIndex] == null)
            {
                _buffersList[bufferIndex] = new MeshSubdivisionBuffers();
            }
            if (_buffersList[bufferIndex].IsInvalid())
            {
                var vertexList = MeshToVertexList();
                var indexList = MeshToIndexList();

                _buffersList[bufferIndex].ConstructMesh(vertexList, indexList);
                _buffersList[bufferIndex].Reserve();
            }
        }

        public void SwapBuffers(int bufferIndex)
        {
            _buffersList[bufferIndex]?.SwapSubdivisionBuffers();
        }

        public Camera GetOverriddenCamera(Camera camera)
        {
#if UNITY_EDITOR
            if (OverrideCamera && camera.cameraType == CameraType.SceneView)
                camera = _debugCamera;
#endif

            return camera;
        }

        public Vector4 BuildLodParameters(Camera camera)
        {
            float fov = (camera.fieldOfView * 0.5f) * Mathf.Deg2Rad;
            float screenResolution = camera.pixelWidth;
            float l = 2.0f * Mathf.Tan(fov) * _targetPixelSize / screenResolution;

            float meshScale = transform.localToWorldMatrix.lossyScale.magnitude;
            float averageLength = _primScale * meshScale;
            float lodFactor = l / averageLength;

            return new Vector4(
                0.0f,
                0.0f,
                0.0f,
                lodFactor);
        }

        public Vector4[] BuildFrustumPlanes(Camera camera)
        {
            var planes = GeometryUtility.CalculateFrustumPlanes(camera);
            var frustumPlanes = new Vector4[6]
            {
                new Vector4(planes[0].normal.x, planes[0].normal.y, planes[0].normal.z, planes[0].distance),
                new Vector4(planes[1].normal.x, planes[1].normal.y, planes[1].normal.z, planes[1].distance),
                new Vector4(planes[2].normal.x, planes[2].normal.y, planes[2].normal.z, planes[2].distance),
                new Vector4(planes[3].normal.x, planes[3].normal.y, planes[3].normal.z, planes[3].distance),
                new Vector4(planes[4].normal.x, planes[4].normal.y, planes[4].normal.z, planes[4].distance),
                new Vector4(planes[5].normal.x, planes[5].normal.y, planes[5].normal.z, planes[5].distance),
            };

            return frustumPlanes;
        }

        private float LoadPrimScale(MeshType meshType)
        {
            var primScale = 1.0f;

            if (meshType == MeshType.Quad)
            {
                primScale = 1.0f;
            }
            else if (meshType == MeshType.Cube)
            {
                primScale = 1.0f;
            }
            else if (meshType == MeshType.Sphere)
            {
                primScale = 1.0f / 8.0f;
            }

            return primScale;
        }

        private Vertex[] MeshToVertexList()
        {
            int numVertices = _mesh.vertices.Length;
            var vertexList = new Vertex[numVertices];
            for (int i = 0; i < numVertices; ++i)
            {
                vertexList[i].position = _mesh.vertices[i];
                vertexList[i].normal   = _mesh.normals[i];
            }

            return vertexList;
        }

        private int[] MeshToIndexList()
        {
            return _mesh.GetIndices(0);
        }

#if UNITY_EDITOR
        private Mesh LoadMesh(MeshType meshType)
        {
            string meshName = meshType.ToString() + "Mesh.mesh";
            string pathMesh = PackagePath + "Meshes/" + meshName;

            return AssetDatabase.LoadAssetAtPath<Mesh>(pathMesh);
        }

        private Mesh[] LoadBaseMeshes(int number)
        {
            var meshes = new Mesh[number];

            for (int i = 0; i < number; ++i)
                meshes[i] = LoadMesh((MeshType)i);

            return meshes;
        }

        private bool OverrideCamera
        {
            get
            {
                return
                    _debugCamera != null &&
                    _debugCamera.enabled &&
                    _debugCamera.isActiveAndEnabled;
            }
        }
#endif
    }
}
