#ifndef _MESH_SUBDIVISION_LIT_PASS_INPUT_INCLUDED_
#define _MESH_SUBDIVISION_LIT_PASS_INPUT_INCLUDED_

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.mesh-subdivision/ShaderLibrary/MeshSubdivisionCommon.hlsl"
#include "Packages/com.mesh-subdivision/ShaderLibrary/MeshSubdivisionInterpolation.hlsl"

struct SurfaceData
{
    float3 albedo;
    float  metallic;
    float3 specular;
    float  smoothness;
    float3 normalTS;
    float3 emission;
    float  occlusion;
    float  alpha;
};

inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    // todo : gamma correct here, but it has to be moved outside later
    float baseColor = pow(0.5, 2.2);

    outSurfaceData.albedo = baseColor;
    outSurfaceData.metallic = 0.0;
    outSurfaceData.specular = float3(0.0, 0.0, 0.0);
    outSurfaceData.smoothness = 0.5;
    outSurfaceData.normalTS = float3(0.0, 0.0, 1.0);
    outSurfaceData.emission = float3(0.0, 0.0, 0.0);
    outSurfaceData.occlusion = 0.0;
    outSurfaceData.alpha = 1.0;
}

#endif // _MESH_SUBDIVISION_INPUT_INCLUDED_
