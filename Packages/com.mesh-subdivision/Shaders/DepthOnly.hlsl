#ifndef _MESH_SUBDIVISION_DEPTH_ONLY_INCLUDED_
#define _MESH_SUBDIVISION_DEPTH_ONLY_INCLUDED_

struct Varyings
{
    float4 positionCS : SV_POSITION;

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings DepthOnlyVertex(uint vertexId : SV_VertexID, uint instId : SV_InstanceID)
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

    float3 posWS = vertexInterpolated.position;
    output.positionCS = TransformWorldToHClip(posWS);

    return output;
}

half4 DepthOnlyFragment(Varyings input) : SV_TARGET
{
    return 0;
}

#endif
