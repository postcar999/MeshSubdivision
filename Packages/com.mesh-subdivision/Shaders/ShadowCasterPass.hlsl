#ifndef _MESH_SUBDIVISION_SHADOW_CASTER_PASS_INCLUDED_
#define _MESH_SUBDIVISION_SHADOW_CASTER_PASS_INCLUDED_

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

float3 _LightDirection;

struct Varyings
{
    float4 positionCS : SV_POSITION;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float4 GetShadowPositionHClip(Vertex vertex)
{
    float3 posWS = vertex.position;
    float3 norWS = vertex.normal;

    float4 posCS = TransformWorldToHClip(ApplyShadowBias(posWS, norWS, _LightDirection));

#if UNITY_REVERSED_Z
    posCS.z = min(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
#else
    posCS.z = max(posCS.z, posCS.w * UNITY_NEAR_CLIP_VALUE);
#endif

    return posCS;
}

Varyings ShadowPassVertex(uint vertexId : SV_VertexID, uint instId : SV_InstanceID)
{
    Varyings output = (Varyings)0;

    SubdivisionPacked packed;
    FetchSubdivision(instId, packed);

    Subdivision subd;
    UnpackSubdivision(packed, subd);

    Vertex vertexList[3];
    FetchVertex(subd.index, vertexList);

    float2 u = ComputeBerpWeight(vertexId, subd);

    Vertex vertexInterpolated;
    InterpolateVertex(u, vertexList, vertexInterpolated);

    output.positionCS = GetShadowPositionHClip(vertexInterpolated);

    return output;
}

half4 ShadowPassFragment(Varyings input) : SV_TARGET
{
    return 0;
}

#endif
