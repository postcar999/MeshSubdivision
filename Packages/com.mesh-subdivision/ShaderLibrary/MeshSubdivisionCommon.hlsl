#ifndef _MESH_SUBDIVISION_COMMON_INCLUDED_
#define _MESH_SUBDIVISION_COMMON_INCLUDED_

#define ROW_MAJOR 1
#define REORDER_VERTICES 1
#define MAX_SUBDIVISIONS 6000000

struct Vertex
{
    float3 position;
    float3 normal;
};

struct Subdivision
{
    uint key;
    uint primId;
};

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

float3x3 BitToXform(uint bit)
{
    float s = float(bit) - 0.5;

#if ROW_MAJOR
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
#if ROW_MAJOR
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
#if ROW_MAJOR
        xf = mul(xf, BitToXform(key & 1u));
#else
        xf = mul(BitToXform(key & 1u), xf);
#endif
        key = key >> 1;
    }

    parent = xf;

#if ROW_MAJOR
    target = mul(BitToXform(last), parent);
#else
    target = mul(parent, BitToXform(last));
#endif
}

#endif