#ifndef _MESH_SUBDIVISION_LOD_INCLUDED_
#define _MESH_SUBDIVISION_LOD_INCLUDED_

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GeometricTools.hlsl"
#include "Packages/com.mesh-subdivision/ShaderLibrary/MeshSubdivisionInterpolation.hlsl"

CBUFFER_START(_MeshSubdivisionCommon)
    float4 _LodParameters;
CBUFFER_END

#define _LodFactor _LodParameters.w

float ComputeLod(float distance)
{
    float lod = distance * _LodFactor;
    lod = clamp(lod, 0.0, 1.0);

    return -2.0f * log2(lod);
}

uint GetParentKey(uint key)
{
    return key >> 1;
}

uint2 GetChildrenKeys(uint key)
{
    // children0 : key << 1 + 0;
    // children1 : key << 1 + 1;

    uint2 childrenKey;
    childrenKey.x = (key << 1) + 0;
    childrenKey.y = (key << 1) + 1;

    return childrenKey;
}

void WriteKey(uint key, uint primIndex, bool isUnmergeable = false)
{
    uint index = _RWSubdivision1Buffer.IncrementCounter();

    uint data0 = key;
    uint data1 = primIndex & 0x0FFFFFFF;
    if (isUnmergeable) data1 |= 0x80000000;

    SubdivisionPacked packed;
    packed.data0 = data0;
    packed.data1 = data1;

    if (index < MAX_SUBDIVISIONS)
        _RWSubdivision1Buffer[index] = packed;
}

void WriteIndex(uint primIndex)
{
    uint index = _RWCulledIndexBuffer.IncrementCounter();
    _RWCulledIndexBuffer[index] = primIndex;
}

void SubdivideTriangle(uint msb, float3x3 xf, Vertex vertex[3], out float3 output[3])
{
    float3 uv0 = float3(0.0, 0.0, 1.0);
    float3 uv1 = float3(1.0, 0.0, 1.0);
    float3 uv2 = float3(0.0, 1.0, 1.0);

#ifdef REORDER_VERTICES
    // the order of vertices is CCW at the odd level
    // but at the even level it is not, so this will be changed to CCW.

    int powLevel = msb; // this is the same as (2 ^ level)
    bool even = 0xAAAAAAAA & powLevel;

    if (even)
    {
        uv1 = float3(0.0, 1.0, 1.0);
        uv2 = float3(1.0, 0.0, 1.0);
    }
#endif

#ifdef ROW_MAJOR
    float2 u0 = mul(uv0, xf).xy;
    float2 u1 = mul(uv1, xf).xy;
    float2 u2 = mul(uv2, xf).xy;
#else
    float2 u0 = mul(xf, uv0).xy;
    float2 u1 = mul(xf, uv1).xy;
    float2 u2 = mul(xf, uv2).xy;
#endif

    InterpolateVertex(u0, vertex, output[0]);
    InterpolateVertex(u1, vertex, output[1]);
    InterpolateVertex(u2, vertex, output[2]);
}

void SubdivideTriangle(float2 u, float3x3 xf, Vertex vertex[3], out float3 interpolated)
{
#ifdef ROW_MAJOR
    u = mul(float3(u, 1.0), xf).xy;
#else
    u = mul(xf, float3(u, 1.0)).xy;
#endif

    InterpolateVertex(u, vertex, interpolated);
}

bool CullSubdivisionBackFace(float3 v[3], float epsilon)
{
    return CullTriangleBackFace(v[0], v[1], v[2], epsilon, _WorldSpaceCameraPos, 1.0);
}

bool CullSubdivisionFrustum(float3 v[3], float epsilon)
{
    bool outside = false;

    float3 center = (v[0] + v[1] + v[2]) * 0.333;
    float  radius = max(max(
        distance(center, v[0]), distance(center, v[1])),
        distance(center, v[2]))- epsilon;

    for (int i = 0; i < 6; i++)
    {
        outside = outside || (DistanceFromPlane(center, _FrustumPlanes[i]) < -radius);
    }

    return outside;
}

void UpdateSubdivisionBuffer(Subdivision subd, int targetLod, int parentLod)
{
    int keyLod = log2(subd.msb); // from pow2(level) to level

    if (subd.isSubdividable && keyLod < targetLod)
    {
        // subdivide

        uint2 childrenKeys = GetChildrenKeys(subd.key);

        WriteKey(childrenKeys.x, subd.index);
        WriteKey(childrenKeys.y, subd.index);
    }
    else if (subd.isUnmergeable || keyLod < (parentLod + 1))
    {
        // keep

        WriteKey(subd.key, subd.index);
    }
    else
    {
        // merge

        if (subd.isRoot)
        {
            WriteKey(subd.key, subd.index);
        }
        else if (subd.isChildZeroKey)
        {
            uint parentKey = GetParentKey(subd.key);

            WriteKey(parentKey, subd.index);
        }
    }
}

void UpdateCulledIndexBuffer(uint subdIndex, bool culledBackFace, bool culledFrustum)
{
    if (culledBackFace || culledFrustum)
        return;

    WriteIndex(subdIndex);
}

#endif