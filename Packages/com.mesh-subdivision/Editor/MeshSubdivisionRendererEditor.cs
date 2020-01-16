using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MeshSubdivision
{
    [CustomEditor(typeof(MeshSubdivisionRenderer))]
    public class MeshSubdivisionRendererEditor : Editor
    {
        private static readonly string PackagePath = "Packages/com.mesh-subdivision/";

        private enum MeshType
        {
            Quad,
            Cube,
            Sphere,
        };

        private SerializedProperty _meshProp;
        private SerializedProperty _subdInterpProp;
        private SerializedProperty _computeShaderProp;
        private SerializedProperty _materialProp;
        private SerializedProperty _debugCameraProp;

        private SerializedProperty _meshScaleProp;
        private SerializedProperty _primScaleProp;
        private SerializedProperty _doCullingProp;
        private SerializedProperty _targetPixelSizeProp;

        private EnumField _meshTypeEnum;
        private TextField _meshScaleText;
        private EnumField _subdInterpEnum;
        private SliderInt _targetPixelSlider;
        private TextField _targetPixelText;
        private Toggle _cullingToggle;
        private ObjectField _debugCameraObject;

        private void OnEnable()
        {
            _meshProp = serializedObject.FindProperty("Mesh");
            _subdInterpProp = serializedObject.FindProperty("Interpolation");
            _computeShaderProp = serializedObject.FindProperty("CS");
            _materialProp = serializedObject.FindProperty("Material");
            _debugCameraProp = serializedObject.FindProperty("DebugCamera");

            bool needToInitializeMesh = _meshProp.objectReferenceValue == null;
            bool needToInitializeCS = _computeShaderProp.objectReferenceValue == null;
            bool needToInitializeMaterial = _materialProp.objectReferenceValue == null;

            if (needToInitializeMesh)
            {
                string pathMesh = PackagePath + "Meshes/QuadMesh.mesh";
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(pathMesh);
                _meshProp.objectReferenceValue = mesh;
            }
            if (needToInitializeCS)
            {
                string pathCS = PackagePath + "Shaders/MeshSubdivision.compute";
                var CS = AssetDatabase.LoadAssetAtPath<ComputeShader>(pathCS);
                _computeShaderProp.objectReferenceValue = CS;
            }
            if (needToInitializeMaterial)
            {
                string pathMaterial = PackagePath + "Materials/MeshSubdivision.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(pathMaterial);
                _materialProp.objectReferenceValue = material;
            }

            _meshScaleProp = serializedObject.FindProperty("MeshScale");
            _primScaleProp = serializedObject.FindProperty("PrimScale");
            _doCullingProp = serializedObject.FindProperty("DoCulling");
            _targetPixelSizeProp = serializedObject.FindProperty("TargetPixelSize");

            serializedObject.ApplyModifiedProperties();

            if (needToInitializeMesh)
            {
                var renderer = target as MeshSubdivisionRenderer;
                if (renderer != null)
                    renderer.ReserveBuffers();
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var rootView = new VisualElement();

            string pathUxml = PackagePath + "Editor/MeshSubdivisionRenderer.uxml";
            string pathUss  = PackagePath + "Editor/MeshSubdivisionRenderer.uss";

            var treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(pathUxml);
            var styleAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>(pathUss);

            treeAsset.CloneTree(rootView);
            rootView.styleSheets.Add(styleAsset);

            _meshTypeEnum = rootView.Q<EnumField>("enumMeshType");
            _meshTypeEnum.Init(MeshType.Quad);
            _meshTypeEnum.value = GetMeshType((Mesh)_meshProp.objectReferenceValue);
            _meshTypeEnum.RegisterCallback<ChangeEvent<Enum>>(
                (evt) => OnChangeMesh(evt.newValue));

            _meshScaleText = rootView.Q<TextField>("textMeshScale");
            _meshScaleText.value = _meshScaleProp.floatValue.ToString();
            _meshScaleText.RegisterCallback<ChangeEvent<string>>(
                (evt) => OnChangeMeshScale(evt.newValue));

            _subdInterpEnum = rootView.Q<EnumField>("enumSubdInterp");
            _subdInterpEnum.Init(SubdInterp.None);
            _subdInterpEnum.value = GetSubdInterp();
            _subdInterpEnum.RegisterCallback<ChangeEvent<Enum>>(
                (evt) => OnChangeSubdInterp(evt.newValue));

            _cullingToggle = rootView.Q<Toggle>("toggleCulling");
            _cullingToggle.value = _doCullingProp.boolValue;
            _cullingToggle.RegisterCallback<ChangeEvent<bool>>(
                (evt) => OnChangeDoCulling(evt.newValue));

            _targetPixelSlider = rootView.Q<SliderInt>("sliderTargetPixel");
            _targetPixelSlider.value = _targetPixelSizeProp.intValue;
            _targetPixelSlider.RegisterCallback<ChangeEvent<int>>(
                (evt) => OnChangeTargetPixelSize(evt.newValue));

            _targetPixelText = rootView.Q<TextField>("textTargetPixel");
            _targetPixelText.value = _targetPixelSizeProp.intValue.ToString();
            _targetPixelText.RegisterCallback<ChangeEvent<string>>(
                (evt) => OnChangeTargetPixelSize(evt.newValue));

            _debugCameraObject = rootView.Q<ObjectField>("objectDebugCamera");
            _debugCameraObject.objectType = typeof(Camera);
            _debugCameraObject.value = _debugCameraProp.objectReferenceValue;
            _debugCameraObject.RegisterCallback<ChangeEvent<UnityEngine.Object>>(
                (evt) => OnChangeDebugCamera(evt.newValue));

            return rootView;
        }

        private MeshType GetMeshType(Mesh mesh)
        {
            if (mesh.name == "QuadMesh")
                return MeshType.Quad;
            else if (mesh.name == "CubeMesh")
                return MeshType.Cube;
            else if (mesh.name == "SphereMesh")
                return MeshType.Sphere;

            return MeshType.Quad;
        }

        private SubdInterp GetSubdInterp()
        {
            switch (_subdInterpProp.enumValueIndex)
            {
                case 1: return SubdInterp.PnTriangle;
                case 2: return SubdInterp.PhongTessellation;
            }

            return SubdInterp.None;
        }

        private void OnChangeMesh(Enum value)
        {
            string pathMesh = PackagePath + "Meshes/";

            var meshType = (MeshType)value;
            var primScale = 1.0f;

            if (meshType == MeshType.Quad)
            {
                pathMesh += "QuadMesh.mesh";
                primScale = 128.0f * 128.0f;
            }
            else if (meshType == MeshType.Cube)
            {
                pathMesh += "CubeMesh.mesh";
                primScale = 1.0f;
            }
            else if (meshType == MeshType.Sphere)
            {
                pathMesh += "SphereMesh.mesh";
                primScale = 1.0f / (8.0f * 8.0f);
            }

            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(pathMesh);
            _meshProp.objectReferenceValue = mesh;
            _primScaleProp.floatValue = primScale;

            serializedObject.ApplyModifiedProperties();

            var renderer = target as MeshSubdivisionRenderer;
            if (renderer != null)
                renderer.ReloadMesh();
        }
        private void OnChangeMeshScale(string text)
        {
            float value = float.Parse(text);
            value = Mathf.Max(1.0f, value);

            _meshScaleProp.floatValue = value;
            serializedObject.ApplyModifiedProperties();
        }
        private void OnChangeSubdInterp(Enum value)
        {
            _subdInterpProp.enumValueIndex = (int)(SubdInterp)value;
            serializedObject.ApplyModifiedProperties();
        }
        private void OnChangeDoCulling(bool doCulling)
        {
            _doCullingProp.boolValue = doCulling;
            serializedObject.ApplyModifiedProperties();
        }
        private void OnChangeTargetPixelSize(int value)
        {
            _targetPixelText.value = value.ToString();

            _targetPixelSizeProp.intValue = value;
            serializedObject.ApplyModifiedProperties();
        }
        private void OnChangeTargetPixelSize(string text)
        {
            int value = int.Parse(text);
            _targetPixelSlider.value = value;

            _targetPixelSizeProp.intValue = value;
            serializedObject.ApplyModifiedProperties();
        }
        private void OnChangeDebugCamera(UnityEngine.Object cameraObject)
        {
            _debugCameraProp.objectReferenceValue = cameraObject;
            serializedObject.ApplyModifiedProperties();
        }

        public static Mesh CreateQuadMesh(float scale)
        {
            var vertices = new List<Vector3>()
            {
                new Vector3(-1.0f, 0.0f, -1.0f) * 0.5f * scale,
                new Vector3( 1.0f, 0.0f, -1.0f) * 0.5f * scale,
                new Vector3(-1.0f, 0.0f,  1.0f) * 0.5f * scale,
                new Vector3( 1.0f, 0.0f,  1.0f) * 0.5f * scale,
            };

            var normals = new List<Vector3>()
            {
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
            };

            var indices = new int[6]
            {
                0, 2, 1,
                3, 1, 2,
            };

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            return mesh;
        }
        public static Mesh CreateCubeMesh(float scale)
        {
            var vertices = new List<Vector3>()
            {
                new Vector3(-1.0f, -1.0f, -1.0f) * 0.5f * scale,
                new Vector3( 1.0f, -1.0f, -1.0f) * 0.5f * scale,
                new Vector3(-1.0f,  1.0f, -1.0f) * 0.5f * scale,
                new Vector3( 1.0f,  1.0f, -1.0f) * 0.5f * scale,

                new Vector3(-1.0f, -1.0f,  1.0f) * 0.5f * scale,
                new Vector3( 1.0f, -1.0f,  1.0f) * 0.5f * scale,
                new Vector3(-1.0f,  1.0f,  1.0f) * 0.5f * scale,
                new Vector3( 1.0f,  1.0f,  1.0f) * 0.5f * scale,
            };

            var normals = new List<Vector3>()
            {
                vertices[0].normalized,
                vertices[1].normalized,
                vertices[2].normalized,
                vertices[3].normalized,

                vertices[4].normalized,
                vertices[5].normalized,
                vertices[6].normalized,
                vertices[7].normalized,
            };

            var indices = new int[36]
            {
                0, 2, 1, 3, 1, 2,
                1, 3, 5, 7, 5, 3,
                5, 7, 4, 6, 4, 7,
                4, 6, 0, 2, 0, 6,
                2, 6, 3, 7, 3, 6,
                4, 0, 5, 1, 5, 0,
            };

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);

            return mesh;
        }
        public static Mesh CreateSphereMesh(float radius, int numDivisions)
        {
            var origins = new List<Vector3>()
            {
                new Vector3(-1.0f, -1.0f, -1.0f),
                new Vector3( 1.0f, -1.0f, -1.0f),
                new Vector3( 1.0f, -1.0f,  1.0f),
                new Vector3(-1.0f, -1.0f,  1.0f),
                new Vector3(-1.0f,  1.0f, -1.0f),
                new Vector3(-1.0f, -1.0f,  1.0f),
            };
            var rights = new List<Vector3>()
            {
                new Vector3( 2.0f, 0.0f,  0.0f),
                new Vector3( 0.0f, 0.0f,  2.0f),
                new Vector3(-2.0f, 0.0f,  0.0f),
                new Vector3( 0.0f, 0.0f, -2.0f),
                new Vector3( 2.0f, 0.0f,  0.0f),
                new Vector3( 2.0f, 0.0f,  0.0f)
            };
            var ups = new List<Vector3>()
            {
                new Vector3(0.0f, 2.0f,  0.0f),
                new Vector3(0.0f, 2.0f,  0.0f),
                new Vector3(0.0f, 2.0f,  0.0f),
                new Vector3(0.0f, 2.0f,  0.0f),
                new Vector3(0.0f, 0.0f,  2.0f),
                new Vector3(0.0f, 0.0f, -2.0f),
            };

            float step = 1.0f / numDivisions;

            var s3 = new Vector3(step, step, step);

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var indices = new List<int>();

            for (int f = 0; f < 6; f++)
            {
                var origin = origins[f];
                var right = rights[f];
                var up = ups[f];

                for (int j = 0; j < numDivisions + 1; j++)
                {
                    for (int i = 0; i < numDivisions + 1; i++)
                    {
                        var p = new Vector3(
                            origin.x + step * (i * right.x + j * up.x),
                            origin.y + step * (i * right.y + j * up.y),
                            origin.z + step * (i * right.z + j * up.z));

                        vertices.Add(p.normalized * radius);
                        normals.Add(p.normalized);
                    }
                }
            }

            int k = numDivisions + 1;

            for (int f = 0; f < 6; f++)
            {
                for (int j = 0; j < numDivisions; j++)
                {
                    bool bottom = j < (numDivisions / 2);
                    for (int i = 0; i < numDivisions; i++)
                    {
                        bool left = i < (numDivisions / 2);
                        int a = ((f * k + j) * k + i);
                        int b = ((f * k + j) * k + i + 1);
                        int c = ((f * k + j + 1) * k + i);
                        int d = ((f * k + j + 1) * k + i + 1);

                        indices.AddRange(new int[] { a, c, b, d, b, c, });
                    }
                }
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetIndices(indices.ToArray(), MeshTopology.Triangles, 0);

            return mesh;
        }

        [MenuItem("MeshSubdivision/Create QuadMesh")]
        public static void GenerateQuadMesh(MenuCommand menuCommand)
        {
            var quadMesh = CreateQuadMesh(128.0f);
            AssetDatabase.CreateAsset(quadMesh, PackagePath + "Meshes/QuadMesh.mesh");
        }

        [MenuItem("MeshSubdivision/Create CubeMesh")]
        public static void GenerateCubeMesh(MenuCommand menuCommand)
        {
            var cubeMesh = CreateCubeMesh(1.0f);
            AssetDatabase.CreateAsset(cubeMesh, PackagePath + "Meshes/CubeMesh.mesh");
        }

        [MenuItem("MeshSubdivision/Create SphereMesh")]
        public static void GenerateSphereMesh(MenuCommand menuCommand)
        {
            var sphereMesh = CreateSphereMesh(0.5f, 8);
            AssetDatabase.CreateAsset(sphereMesh, PackagePath + "Meshes/SphereMesh.mesh");
        }
    }
}
