using UnityEngine;

namespace MeshSubdivision
{
    public class ShaderIDs
    {
        public static readonly int _ArgsOffsets          = Shader.PropertyToID("_ArgsOffsets");
        public static readonly int _LodParameters        = Shader.PropertyToID("_LodParameters");

        public static readonly int _WorldSpaceCameraPos  = Shader.PropertyToID("_WorldSpaceCameraPos");
        public static readonly int _FrustumPlanes        = Shader.PropertyToID("_FrustumPlanes");

        public static readonly int _ViewProjMatrix       = Shader.PropertyToID("unity_MatrixVP");
        public static readonly int _ObjectToWorldMatrix  = Shader.PropertyToID("unity_ObjectToWorld");

        public static readonly int _VertexBuffer         = Shader.PropertyToID("_VertexBuffer");
        public static readonly int _IndexBuffer          = Shader.PropertyToID("_IndexBuffer");
        public static readonly int _SubdivisionBuffer    = Shader.PropertyToID("_SubdivisionBuffer");
        public static readonly int _CulledIndexBuffer    = Shader.PropertyToID("_CulledIndexBuffer");

        public static readonly int _RWIndirectArgsBuffer = Shader.PropertyToID("_RWIndirectArgsBuffer");
        public static readonly int _RWSubdivision0Buffer = Shader.PropertyToID("_RWSubdivision0Buffer");
        public static readonly int _RWSubdivision1Buffer = Shader.PropertyToID("_RWSubdivision1Buffer");
        public static readonly int _RWCulledIndexBuffer  = Shader.PropertyToID("_RWCulledIndexBuffer");
    };
}
