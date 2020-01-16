using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MeshSubdivision
{
    public class MeshSubdivisionManager
    {
        private static MeshSubdivisionManager sInstance = null;
        public static MeshSubdivisionManager Instance
        {
            get
            {
                if (sInstance == null)
                    sInstance = new MeshSubdivisionManager();

                return sInstance;
            }
        }

        private Matrix4x4 _viewMatrix;
        private Matrix4x4 _projMatrix;
        private Matrix4x4 _viewProjMatrix;

        public Matrix4x4 ViewMatrix => _viewMatrix;
        public Matrix4x4 ProjMatrix => _projMatrix;
        public Matrix4x4 ViewProjMatrix => _viewProjMatrix;

        private List<MeshSubdivisionRenderer> _rendererList = new List<MeshSubdivisionRenderer>();

        public void RegisterRenderer(MeshSubdivisionRenderer renderer)
        {
            _rendererList.Add(renderer);
        }
        public void UnregisterRenderer(MeshSubdivisionRenderer renderer)
        {
            _rendererList.Remove(renderer);
        }

        public void Update(Camera camera)
        {
            bool relativeCamera = false;

            var gpuView = camera.worldToCameraMatrix;
            if (relativeCamera) gpuView.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

            var gpuProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);

            _viewMatrix = gpuView;
            _projMatrix = gpuProj;
            _viewProjMatrix = gpuProj * gpuView;
        }

        public void ExecuteInitial()
        {
            foreach (var renderer in _rendererList)
                renderer.ExecuteInitial();
        }

        public void ExecuteLod(CommandBuffer cmd, Camera camera)
        {
            cmd.name = "MeshSubdivision Lod";
            foreach (var renderer in _rendererList)
            {
                renderer.ExecuteLod(cmd, camera);
            }
        }
        public void ExecuteIndirectBatcher(CommandBuffer cmd)
        {
            cmd.name = "MeshSubdivision IndirectBatcher";
            foreach (var renderer in _rendererList)
            {
                renderer.ExecuteIndirectBatcher(cmd);
            }
        }
        public void Render(CommandBuffer cmd, Camera camera)
        {
            cmd.name = "MeshSubdivision Render";
            foreach (var renderer in _rendererList)
            {
                renderer.Render(cmd, camera);
            }
        }

        public void SwapBuffers(CommandBuffer cmd)
        {
            foreach (var renderer in _rendererList)
            {
                renderer.SwapSubdivisionBuffers(cmd);
            }
        }
    }
}
