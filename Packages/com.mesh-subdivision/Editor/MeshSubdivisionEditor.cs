using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MeshSubdivision
{
    [CustomEditor(typeof(MeshSubdivision))]
    public partial class MeshSubdivisionEditor : Editor
    {
        private static string PackagePath => MeshSubdivisionFeature.PackagePath;

        private enum MeshTypeInEditor
        {
            Quad = MeshType.Quad,
            Cube = MeshType.Cube,
            Sphere = MeshType.Sphere,
        };

        private SerializedProperty _debugCameraProp;

        private SerializedProperty _meshProp;
        private SerializedProperty _meshShadingProp;
        private SerializedProperty _subdInterpProp;

        private SerializedProperty _castShadowsProp;
        private SerializedProperty _enableCullingProp;
        private SerializedProperty _targetPixelSizeProp;

        private EnumField _meshTypeEnum;
        private EnumField _meshShadingEnum;
        private EnumField _subdInterpEnum;
        private SliderInt _targetPixelSlider;
        private TextField _targetPixelText;
        private Toggle _castShadowsToggle;
        private Toggle _cullingToggle;
        private ObjectField _debugCameraObject;

        private void OnEnable()
        {
            _debugCameraProp = serializedObject.FindProperty("_debugCamera");

            _meshProp = serializedObject.FindProperty("_mesh");
            _meshShadingProp = serializedObject.FindProperty("_meshShading");
            _subdInterpProp = serializedObject.FindProperty("_subdInterp");

            _castShadowsProp = serializedObject.FindProperty("_castShadows");
            _enableCullingProp = serializedObject.FindProperty("_enableCulling");
            _targetPixelSizeProp = serializedObject.FindProperty("_targetPixelSize");

            serializedObject.ApplyModifiedProperties();
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
            _meshTypeEnum.Init(MeshTypeInEditor.Quad);
            _meshTypeEnum.value = GetMeshType((Mesh)_meshProp.objectReferenceValue);
            _meshTypeEnum.RegisterCallback<ChangeEvent<Enum>>(
                (evt) => OnChangeMesh(evt.newValue));

            _meshShadingEnum = rootView.Q<EnumField>("enumMeshShading");
            _meshShadingEnum.Init(MeshShading.Lit);
            _meshShadingEnum.value = GetMeshShading();
            _meshShadingEnum.RegisterCallback<ChangeEvent<Enum>>(
                (evt) => OnChangeMeshShading(evt.newValue));

            _subdInterpEnum = rootView.Q<EnumField>("enumSubdInterp");
            _subdInterpEnum.Init(SubdInterp.None);
            _subdInterpEnum.value = GetSubdInterp();
            _subdInterpEnum.RegisterCallback<ChangeEvent<Enum>>(
                (evt) => OnChangeSubdInterp(evt.newValue));

            _castShadowsToggle = rootView.Q<Toggle>("toggleCastShadows");
            _castShadowsToggle.value = _castShadowsProp.boolValue;
            _castShadowsToggle.RegisterCallback<ChangeEvent<bool>>(
                (evt) => OnChangeCastShadows(evt.newValue));

            _cullingToggle = rootView.Q<Toggle>("toggleCulling");
            _cullingToggle.value = _enableCullingProp.boolValue;
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

        private MeshTypeInEditor GetMeshType(Mesh mesh)
        {
            var prefixLength = mesh.name.Length - 4;
            var prefix = mesh.name.Substring(0, prefixLength);

            string[] meshTypeStrings = System.Enum.GetNames(typeof(MeshType));
            for (int i = 0; i < meshTypeStrings.Length; ++i)
            {
                if (prefix == meshTypeStrings[i])
                    return (MeshTypeInEditor)i;
            }

            return MeshTypeInEditor.Quad;
        }

        private MeshShading GetMeshShading()
        {
            switch (_meshShadingProp.enumValueIndex)
            {
                case 0: return MeshShading.Lit;
                case 1: return MeshShading.Visualization;
            }

            return MeshShading.Lit;
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
            var meshType = (MeshType)value;

            var comp = target as MeshSubdivision;
            if (comp != null)
                comp.SetMesh(meshType);
        }
        private void OnChangeMeshShading(Enum value)
        {
            _meshShadingProp.enumValueIndex = (int)(MeshShading)value;
            serializedObject.ApplyModifiedProperties();
        }
        private void OnChangeSubdInterp(Enum value)
        {
            _subdInterpProp.enumValueIndex = (int)(SubdInterp)value;
            serializedObject.ApplyModifiedProperties();
        }
        private void OnChangeCastShadows(bool castShadows)
        {
            _castShadowsProp.boolValue = castShadows;
            serializedObject.ApplyModifiedProperties();
        }
        private void OnChangeDoCulling(bool doCulling)
        {
            _enableCullingProp.boolValue = doCulling;
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
            const int MinValue = 1;
            const int MaxValue = 5000;

            int value = MinValue;

            try
            {
                value = int.Parse(text);
                value = Mathf.Clamp(value, MinValue, MaxValue);
            }
            catch (Exception e)
            {
                if (e is FormatException || e is OverflowException)
                {
                    value = _targetPixelSizeProp.intValue;
                }
            }

            _targetPixelSlider.value = value;

            _targetPixelSizeProp.intValue = value;
            serializedObject.ApplyModifiedProperties();
        }
        private void OnChangeDebugCamera(UnityEngine.Object cameraObject)
        {
            _debugCameraProp.objectReferenceValue = cameraObject;
            serializedObject.ApplyModifiedProperties();
        }

        //[MenuItem("MeshSubdivision/Create QuadMesh")]
        public static void GenerateQuadMesh(MenuCommand menuCommand)
        {
            var quadMesh = CreateQuadMesh(1.0f);
            AssetDatabase.CreateAsset(quadMesh, PackagePath + "Meshes/QuadMesh.mesh");
        }

        //[MenuItem("MeshSubdivision/Create CubeMesh")]
        public static void GenerateCubeMesh(MenuCommand menuCommand)
        {
            var cubeMesh = CreateCubeMesh(1.0f);
            AssetDatabase.CreateAsset(cubeMesh, PackagePath + "Meshes/CubeMesh.mesh");
        }

        //[MenuItem("MeshSubdivision/Create SphereMesh")]
        public static void GenerateSphereMesh(MenuCommand menuCommand)
        {
            var sphereMesh = CreateSphereMesh(0.5f, 8);
            AssetDatabase.CreateAsset(sphereMesh, PackagePath + "Meshes/SphereMesh.mesh");
        }

        [MenuItem("GameObject/Mesh Subdivision/MeshSubdivision")]
        public static void CreateMeshSubdivision(MenuCommand menuCommand)
        {
            var gameObject = new GameObject("Mesh Subdivision");
            var meshSubd = gameObject.AddComponent<MeshSubdivision>();
        }
    }
}
