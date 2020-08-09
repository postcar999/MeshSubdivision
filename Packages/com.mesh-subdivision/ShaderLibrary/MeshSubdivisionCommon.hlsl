#ifndef _MESH_SUBDIVISION_COMMON_INCLUDED_
#define _MESH_SUBDIVISION_COMMON_INCLUDED_

#define ROW_MAJOR
#define REORDER_VERTICES
#define MAX_SUBDIVISIONS 10000000

struct Vertex
{
    float3 position;
    float3 normal;
};

struct SubdivisionPacked
{
    uint data0;
    uint data1;
};

struct Subdivision
{
    uint key;
    uint index;
    uint msb;

    bool isRoot;
    bool isLeaf;
    bool isChildZeroKey;

    bool isSubdividable;
    bool isUnmergeable;
};

StructuredBuffer<Vertex>            _VertexBuffer;
StructuredBuffer<uint>              _IndexBuffer;
StructuredBuffer<SubdivisionPacked> _SubdivisionBuffer;
StructuredBuffer<uint>              _CulledIndexBuffer;

struct IndirectArg
{
    // arguments   [KernelLod]  [DrawProcedural]
    uint arg0;   // dispatchX    indexCount
    uint arg1;   // dispatchY    numInstances
    uint arg2;   // dispatchZ    startIndex
    uint arg3;   // numSubd      startInstance
};

int FindMSB(uint n)
{
    // Suppose n is 273 (binary is 100010001)
    // It does following 100010001 | 010001000 = 110011001
    n |= n >> 1;

    // This makes sure 4 bits are set. It does following
    // 110011001 | 001100110 = 111111111
    n |= n >> 2;

    n |= n >> 4;
    n |= n >> 8;
    n |= n >> 16;

    // Increment n by 1 so that there is only one set bit
    // which is just before original MSG. n now becomes 1000000000
    n = n + 1;

    // Return original MSB after shifting.
    // n now becomes 1000000000
    return n >> 1;
}

bool IsRootKey(uint key)
{
    return key == 1;
}

bool IsLeafKey(uint key)
{
    return (key & 0x4000000) != 0;
}

bool IsChildZeroKey(uint key)
{
    return (key & 0x00000001) == 0;
}

bool IsChildNonZeroKey(uint key)
{
    return (key & 0x00000001) == 1;
}

float3x3 BitToXform(uint bit)
{
    float s = float(bit) - 0.5;

#ifdef ROW_MAJOR
    float3 c1 = float3(   s, -0.5,  0.0);
    float3 c2 = float3(-0.5,   -s,  0.0);
    float3 c3 = float3( 0.5,  0.5,  1.0);
#else
    float3 c1 = float3(   s, -0.5,  0.5);
    float3 c2 = float3(-0.5,   -s,  0.5);
    float3 c3 = float3( 0.0,  0.0,  1.0);
#endif

    return float3x3(c1, c2, c3);
}

void KeyToXform(uint key, out float3x3 target)
{
    float3x3 xf = float3x3(
        1.0, 0.0, 0.0,
        0.0, 1.0, 0.0,
        0.0, 0.0, 1.0);

    while (key > 1)
    {
#ifdef ROW_MAJOR
        xf = mul(xf, BitToXform(key & 1u));
#else
        xf = mul(BitToXform(key & 1u), xf);
#endif
        key = key >> 1;
    }

    target = xf;
}

void KeyToXform(uint key, out float3x3 target, out float3x3 parent)
{
    float3x3 xf = float3x3(
        1.0, 0.0, 0.0,
        0.0, 1.0, 0.0,
        0.0, 0.0, 1.0);

    target = xf;
    parent = xf;

    if (key == 1)
        return;

    uint last = key & 1u;
    key = key >> 1;

    while (key > 1)
    {
#ifdef ROW_MAJOR
        xf = mul(xf, BitToXform(key & 1u));
#else
        xf = mul(BitToXform(key & 1u), xf);
#endif
        key = key >> 1;
    }

    parent = xf;

#ifdef ROW_MAJOR
    target = mul(BitToXform(last), parent);
#else
    target = mul(parent, BitToXform(last));
#endif
}

void FetchSubdivision(int index, out SubdivisionPacked packed)
{
#ifdef _CULLING
    uint subdIndex = _CulledIndexBuffer[index];
    packed = _SubdivisionBuffer[subdIndex];
#else
    packed = _SubdivisionBuffer[index];
#endif
}

void UnpackSubdivision(SubdivisionPacked packed, out Subdivision subd)
{
    subd.key   = packed.data0;
    subd.index = packed.data1 & 0x0FFFFFFF;
    subd.msb   = FindMSB(subd.key);

#ifdef FULLY_UNPACK
    subd.isRoot = IsRootKey(subd.key);
    subd.isLeaf = IsLeafKey(subd.key);
    subd.isChildZeroKey = IsChildZeroKey(subd.key);
    subd.isSubdividable = subd.isLeaf == false;
    subd.isUnmergeable = false;
#else
    subd.isRoot = false;
    subd.isLeaf = false;
    subd.isChildZeroKey = false;
    subd.isSubdividable = false;
    subd.isUnmergeable = false;
#endif
}

void FetchVertex(uint primIndex, out Vertex vertexList[3])
{
    int primStart = primIndex * 3;

    vertexList[0] = _VertexBuffer[_IndexBuffer[primStart + 0]];
    vertexList[1] = _VertexBuffer[_IndexBuffer[primStart + 1]];
    vertexList[2] = _VertexBuffer[_IndexBuffer[primStart + 2]];

    vertexList[0].position = TransformObjectToWorld(vertexList[0].position);
    vertexList[1].position = TransformObjectToWorld(vertexList[1].position);
    vertexList[2].position = TransformObjectToWorld(vertexList[2].position);

    vertexList[0].normal = TransformObjectToWorldDir(vertexList[0].normal);
    vertexList[1].normal = TransformObjectToWorldDir(vertexList[1].normal);
    vertexList[2].normal = TransformObjectToWorldDir(vertexList[2].normal);

    vertexList[0].normal = normalize(vertexList[0].normal);
    vertexList[1].normal = normalize(vertexList[1].normal);
    vertexList[2].normal = normalize(vertexList[2].normal);
}

float2 ComputeBerpWeight(uint vertexId, Subdivision subd)
{
    float3x3 xf;
    KeyToXform(subd.key, xf);

    float3 uv[3] =
    {
        float3(0.0, 0.0, 1.0),
        float3(1.0, 0.0, 1.0),
        float3(0.0, 1.0, 1.0),
    };

#ifdef REORDER_VERTICES
    // the order of vertices is CCW at the odd level
    // but at the even level it is not, so this will be changed to CCW.

    int powLevel = subd.msb; // this is the same as (2 ^ level)
    int order = 0xAAAAAAAA & powLevel ? 2 : 1;

    // odd  order: 0 1 2
    // even order: 0 2 1
    vertexId = vertexId * order;
    vertexId = vertexId % 3;
#endif

#ifdef ROW_MAJOR
    float2 u = mul(uv[vertexId], xf).xy;
#else
    float2 u = mul(xf, uv[vertexId]).xy;
#endif

    return u;
}

#endif
