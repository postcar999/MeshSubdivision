#ifndef _MESH_SUBDIVISION_PASSES_INCLUDED_
#define _MESH_SUBDIVISION_PASSES_INCLUDED_

#include "Packages/com.mesh-subdivision/ShaderLibrary/MeshSubdivisionCommon.hlsl"
#include "Packages/com.mesh-subdivision/ShaderLibrary/MeshSubdivisionInterpolation.hlsl"
#include "Packages/com.mesh-subdivision/ShaderLibrary/MeshSubdivisionTransforms.hlsl"

CBUFFER_START(_MeshSubdivision)
    float4 _VertexOrder;
    float4 _LodParameters;
    float4 _Transform;
CBUFFER_END

#define _Fov              _LodParameters.x
#define _TargetPixelRate  _LodParameters.y // _TargetPixelSize / _ScreenResolution

#define _Translation      _Transform.xyz
#define _Scale            _Transform.w

StructuredBuffer<Vertex>      _VertexBuffer;
StructuredBuffer<uint>        _IndexBuffer;
StructuredBuffer<Subdivision> _SubdivisionBuffer;
StructuredBuffer<uint>        _CulledIndexBuffer;

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 normalWS : COLOR0;
    float4 color : COLOR1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

float2 ComputePuv(uint vertexId, uint key)
{
    float2 uv = float2(0.0, 0.0);

#if REORDER_VERTICES
    // the order of vertices is CCW at the odd level
    // but at the even level it is not, so this will be changed to CCW.

    int powLevel = FindMSB(key); // this is the same as (2 ^ level)
    bool even = 0xAAAAAAAA & powLevel;

    int offsetId = vertexId;

    if (vertexId == 1 && even) offsetId = 2;
    if (vertexId == 2 && even) offsetId = 1;

    if (offsetId == 1) uv.x = 1.0;
    if (offsetId == 2) uv.y = 1.0;
#else
    if (vertexId == 1) uv.x = 1.0;
    if (vertexId == 2) uv.y = 1.0;
#endif

    return uv;
}

float4 VisualizeLevel(uint key, uint vertexId)
{
    float4 color = float4(0.0, 0.0, 0.0, 1.0);

    int level = log2(FindMSB(key));

    if (level ==  0) { color.r   = 0.15; }
    if (level ==  1) { color.g   = 0.15; }
    if (level ==  2) { color.b   = 0.15; }
    if (level ==  3) { color.rg  = 0.15; }
    if (level ==  4) { color.gb  = 0.15; }
    if (level ==  5) { color.br  = 0.15; }
    if (level ==  6) { color.rgb = 0.15; }

    if (level ==  7) { color.r   = 0.30; }
    if (level ==  8) { color.g   = 0.30; }
    if (level ==  9) { color.b   = 0.30; }
    if (level == 10) { color.rg  = 0.30; }
    if (level == 11) { color.gb  = 0.30; }
    if (level == 12) { color.br  = 0.30; }
    if (level == 13) { color.rgb = 0.30; }

    if (level == 14) { color.r   = 0.45; }
    if (level == 15) { color.g   = 0.45; }
    if (level == 16) { color.b   = 0.45; }
    if (level == 17) { color.rg  = 0.45; }
    if (level == 18) { color.gb  = 0.45; }
    if (level == 19) { color.br  = 0.45; }
    if (level == 20) { color.rgb = 0.45; }

    if (level == 21) { color.r   = 0.60; }
    if (level == 22) { color.g   = 0.60; }
    if (level == 23) { color.b   = 0.60; }
    if (level == 24) { color.rg  = 0.60; }
    if (level == 25) { color.gb  = 0.60; }
    if (level == 26) { color.br  = 0.60; }
    if (level == 27) { color.rgb = 0.60; }

    if (level == 28) { color.r   = 0.75; }
    if (level == 29) { color.g   = 0.75; }
    if (level == 30) { color.b   = 0.75; }

    if (vertexId == 0) color.r += 0.15;
    if (vertexId == 1) color.g += 0.15;
    if (vertexId == 2) color.b += 0.15;

    return color;
}

float3 ComputeFlatNormal(uint key, Vertex vList[3])
{
    float3x3 xf;
    KeyToXform(key, xf);

    float2 uv0 = ComputePuv(0, key);
    float2 uv1 = ComputePuv(1, key);
    float2 uv2 = ComputePuv(2, key);

#if ROW_MAJOR
    float2 u0 = mul(float3(uv0, 1.0), xf).xy;
    float2 u1 = mul(float3(uv1, 1.0), xf).xy;
    float2 u2 = mul(float3(uv2, 1.0), xf).xy;
#else
    float2 u0 = mul(xf, float3(uv0, 1.0)).xy;
    float2 u1 = mul(xf, float3(uv1, 1.0)).xy;
    float2 u2 = mul(xf, float3(uv2, 1.0)).xy;
#endif

    Vertex vListInterpolated0;
    Vertex vListInterpolated1;
    Vertex vListInterpolated2;

    InterpolateVertex(u0, vList, vListInterpolated0);
    InterpolateVertex(u1, vList, vListInterpolated1);
    InterpolateVertex(u2, vList, vListInterpolated2);

    float3 P01 = vListInterpolated1.position - vListInterpolated0.position;
    float3 P02 = vListInterpolated2.position - vListInterpolated0.position;

    return normalize(cross(P01, P02));
}

Varyings LitPassVertex(uint vertexId : SV_VertexID, uint instId : SV_InstanceID)
{
    Varyings output = (Varyings)0;

    uint subdIndex = _CulledIndexBuffer[instId];
    Subdivision subd = _SubdivisionBuffer[subdIndex];
    int primStart = subd.primId * 3;

    Vertex vertexList[3] =
    {
        _VertexBuffer[_IndexBuffer[primStart + _VertexOrder.x]],
        _VertexBuffer[_IndexBuffer[primStart + _VertexOrder.y]],
        _VertexBuffer[_IndexBuffer[primStart + _VertexOrder.z]],
    };

    vertexList[0].position.xyz *= _Scale;
    vertexList[1].position.xyz *= _Scale;
    vertexList[2].position.xyz *= _Scale;

    vertexList[0].position.xyz += _Translation;
    vertexList[1].position.xyz += _Translation;
    vertexList[2].position.xyz += _Translation;

    float3 posList[3] =
    {
        vertexList[0].position,
        vertexList[1].position,
        vertexList[2].position,
    };
    float3 norList[3] =
    {
        vertexList[0].normal,
        vertexList[1].normal,
        vertexList[2].normal,
    };

    float3x3 xf;
    KeyToXform(subd.key, xf);

    float2 uv = ComputePuv(vertexId, subd.key);

#if ROW_MAJOR
    float2 u = mul(float3(uv, 1.0), xf).xy;
#else
    float2 u = mul(xf, float3(uv, 1.0)).xy;
#endif

    Vertex vertexInterpolated;
    InterpolateVertex(u, vertexList, vertexInterpolated);

    float3 posWS = vertexInterpolated.position;
    float4 posCS = TransformWorldToHClip(posWS);

    //float3 norWS = ComputeFlatNormal(subd.key, vertexList);
    float3 norWS = vertexInterpolated.normal;

    float4 color = VisualizeLevel(subd.key, vertexId);

    output.positionCS = posCS;
    output.normalWS = norWS;
    output.color = color;

    return output;
}

float4 LitPassFragment(Varyings input) : SV_Target
{
    float3 L = float3(0.0, 1.0, 0.0);
    float3 N = input.normalWS;

    float intensity = 2.0;

    float NdotL = saturate(dot(N, L));

    float4 albedo = input.color;
    float3 ambient = 0.3;

    float3 radiance = NdotL * intensity + ambient;

    float4 color = float4(albedo.xyz * radiance, 1.0);

    return color;
}

#endif