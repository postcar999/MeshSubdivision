#ifndef _MESH_SUBDIVISION_LOD_INCLUDED_
#define _MESH_SUBDIVISION_LOD_INCLUDED_

#include "Packages/com.mesh-subdivision/ShaderLibrary/MeshSubdivisionCommon.hlsl"
#include "Packages/com.mesh-subdivision/ShaderLibrary/MeshSubdivisionInterpolation.hlsl"
#include "Packages/com.mesh-subdivision/ShaderLibrary/MeshSubdivisionTransforms.hlsl"

float ComputeDistanceSq(float3 p0, float3 p1)
{
    float3 p = p1 - p0;
    float distanceSq = dot(p, p);

    return distanceSq;
}

float ComputeLod(float distanceSq, float scale)
{
    float sz = 2.0 * (distanceSq) * tan(_Fov);
    float tmp = sz * _TargetPixelRate * scale;

    return -log2(clamp(tmp, 0.0, 1.0));
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

bool IsRootKey(uint key)
{
    return key == 1;
}

bool IsLeafKey(uint key)
{
    return FindMSB(key) == (1 << 30);
}

bool IsChildZeroKey(uint key)
{
    return (key & 0x00000001) == 0;
}

void WriteKey(uint key, uint primId)
{
    uint index = _RWSubdivision1Buffer.IncrementCounter();

    Subdivision subd;
    subd.key = key;
    subd.primId = primId;

    if (index < MAX_SUBDIVISIONS)
        _RWSubdivision1Buffer[index] = subd;
}

void WriteIndex(uint subdIndex)
{
    uint index = _RWCulledIndexBuffer.IncrementCounter();
    _RWCulledIndexBuffer[index] = subdIndex;
}

void SubdivideTriangle(uint key, float3x3 xf, Vertex vertex[3], out float3 output[3])
{
    float3 uv0 = float3(0.0, 0.0, 1.0);
    float3 uv1 = float3(1.0, 0.0, 1.0);
    float3 uv2 = float3(0.0, 1.0, 1.0);

#if REORDER_VERTICES
    // the order of vertices is CCW at the odd level
    // but at the even level it is not, so this will be changed to CCW.

    int powLevel = FindMSB(key); // this is the same as (2 ^ level)
    bool even = 0xAAAAAAAA & powLevel;

    if (even)
    {
        uv1 = float3(0.0, 1.0, 1.0);
        uv2 = float3(1.0, 0.0, 1.0);
    }
#endif

#if ROW_MAJOR
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

void SubdivideTriangle(uint key, float2 u, float3x3 xf, Vertex vertex[3], out float3 interpolated)
{
#if ROW_MAJOR
    u = mul(float3(u, 1.0), xf).xy;
#else
    u = mul(xf, float3(u, 1.0)).xy;
#endif

    InterpolateVertex(u, vertex, interpolated);
}

void SubdivideTriangleNormal(uint key, float2 u, float3x3 xf, Vertex vertex[3], out float3 normal)
{
#if ROW_MAJOR
    u = mul(float3(u, 1.0), xf).xy;
#else
    u = mul(xf, float3(u, 1.0)).xy;
#endif

    float3 uvw = float3(u.xy, 1.0 - u.x - u.y);

    normal = BerpNormal(vertex, uvw);
}

bool IsFrontFace(float3 v[3])
{
    // todo : is there any way to effeciently compute thi?

    float4 V0 = TransformWorldToHClip(v[0]);
    float4 V1 = TransformWorldToHClip(v[1]);
    float4 V2 = TransformWorldToHClip(v[2]);

    V0.xyz /= V0.w;
    V1.xyz /= V1.w;
    V2.xyz /= V2.w;

    float3 C0 = V1.xyz - V0.xyz;
    float3 C1 = V2.xyz - V0.xyz;

    float3 D = cross(C0, C1);

    return D.z >= 0.0 && V0.z >= 0.0 && V1.z >= 0.0 && V2.z >= 0.0;
}

bool IsInsideFrustum(float3 hypotenuse, float3 edge)
{
    // todo : is there any way to effeciently compute thi?

    float r = distance(hypotenuse, edge);

    float s0 = dot(_FrustumPlanes[0].xyz, hypotenuse) - _FrustumPlanes[0].w;
    float s1 = dot(_FrustumPlanes[1].xyz, hypotenuse) - _FrustumPlanes[1].w;
    float s2 = dot(_FrustumPlanes[2].xyz, hypotenuse) - _FrustumPlanes[2].w;
    float s3 = dot(_FrustumPlanes[3].xyz, hypotenuse) - _FrustumPlanes[3].w;
    float s4 = dot(_FrustumPlanes[4].xyz, hypotenuse) - _FrustumPlanes[4].w;
    float s5 = dot(_FrustumPlanes[5].xyz, hypotenuse) - _FrustumPlanes[5].w;

    return s0 <= r && s1 <= r && s2 <= r && s3 <= r && s4 <= r && s5 <= r;
}

void UpdateSubdivisionBuffer(uint key, uint primId, int targetLod, int parentLod)
{
    int keyLod = log2(FindMSB(key));

    bool isLeafKey = IsLeafKey(key);

    bool isSubdividable = isLeafKey == false;

    if (keyLod < targetLod && isSubdividable)
    {
        // subdivide

        uint2 childrenKeys = GetChildrenKeys(key);

        WriteKey(childrenKeys.x, primId);
        WriteKey(childrenKeys.y, primId);
    }
    else if (keyLod < (parentLod + 1))
    {
        // keep

        WriteKey(key, primId);
    }
    else
    {
        // merge

        if (IsRootKey(key))
        {
            WriteKey(key, primId);
        }
        else if (IsChildZeroKey(key))
        {
            WriteKey(GetParentKey(key), primId);
        }
    }
}

void UpdateCulledIndexBuffer(uint subdIndex, bool isFrontFace, bool isInsideFrustum)
{
#if _CULLING
    if (isFrontFace && isInsideFrustum)
        WriteIndex(subdIndex);
#else
    WriteIndex(subdIndex);
#endif
}

#endif