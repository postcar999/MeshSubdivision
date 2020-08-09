using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MeshSubdivision
{
    public partial class MeshSubdivisionFeature : ScriptableRendererFeature
    {
        public static readonly string PackagePath = "Packages/com.mesh-subdivision/";

        [SerializeField, HideInInspector] private ComputeShader _meshSubdCS;
        [SerializeField, HideInInspector] private Material _meshSubdMat;

        private MeshSubdivisionPrepass _meshSubdivisionPrepass;
        private MeshSubdivisionShadowCasterPass _meshSubdivisionShadowCasterPass;
        private MeshSubdivisionDepthPrepass _meshSubdivisionDepthPrepass;
        private MeshSubdivisionLit _meshSubdivisionLit;
        private MeshSubdivisionPostpass _meshSubdivisionPostpass;

        public void OnEnable()
        {
#if UNITY_EDITOR
            _meshSubdCS = LoadComputeShader();
            _meshSubdMat = LoadMaterial();
#endif
        }

        public override void Create()
        {
            _meshSubdivisionPrepass = new MeshSubdivisionPrepass(_meshSubdCS)
            {
                renderPassEvent = RenderPassEvent.BeforeRendering,
            };
            _meshSubdivisionShadowCasterPass = new MeshSubdivisionShadowCasterPass(_meshSubdMat)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingShadows,
            };
            _meshSubdivisionDepthPrepass = new MeshSubdivisionDepthPrepass(_meshSubdMat)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPrePasses - 25,
            };
            _meshSubdivisionLit = new MeshSubdivisionLit(_meshSubdMat)
            {
                renderPassEvent = RenderPassEvent.BeforeRenderingOpaques + 25,
            };
            _meshSubdivisionPostpass = new MeshSubdivisionPostpass()
            {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques,
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;
            var camera = cameraData.camera;

            if (camera.cameraType == CameraType.Preview)
                return;

            renderer.EnqueuePass(_meshSubdivisionPrepass);

            var mainLightIndex = renderingData.lightData.mainLightIndex;
            if (mainLightIndex != -1)
            {
                var mainLight = renderingData.lightData.visibleLights[mainLightIndex];
                if (mainLight.lightType == LightType.Directional)
                    renderer.EnqueuePass(_meshSubdivisionShadowCasterPass);
            }

            // determine whether this pass is needed or not by 'requiresScreenSpaceShadowResolve'
            if (renderingData.shadowData.requiresScreenSpaceShadowResolve)
                renderer.EnqueuePass(_meshSubdivisionDepthPrepass);

            renderer.EnqueuePass(_meshSubdivisionLit);
            renderer.EnqueuePass(_meshSubdivisionPostpass);
        }

#if UNITY_EDITOR
        private ComputeShader LoadComputeShader()
        {
            string pathCS = PackagePath + "Shaders/MeshSubdivision.compute";
            var CS = AssetDatabase.LoadAssetAtPath<ComputeShader>(pathCS);

            return CS;
        }

        private Material LoadMaterial()
        {
            string pathMaterial = PackagePath + "Materials/MeshSubdivision.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(pathMaterial);

            return material;
        }
#endif

        private static List<MeshSubdivision> sComponentList = new List<MeshSubdivision>();
        public static List<MeshSubdivision> ComponentList => sComponentList;

        public static void Register(MeshSubdivision comp)
        {
            sComponentList.Add(comp);
        }
        public static void Unregister(MeshSubdivision comp)
        {
            sComponentList.Remove(comp);
        }

        public static int ToBufferIndex(CameraType cameraType)
        {
            switch (cameraType)
            {
                case CameraType.Game: return 0;
                case CameraType.SceneView: return 1;
                case CameraType.Preview: return 2;
                case CameraType.VR: return 3;
                case CameraType.Reflection: return 4;
            }

            return -1;
        }
    }
}
