#ifndef _MESH_SUBDIVISION_INTERPOLATION_INCLUDED_
#define _MESH_SUBDIVISION_INTERPOLATION_INCLUDED_

float3 Berp(float3 v[3], float3 uvw)
{
    return uvw.z * v[0] + uvw.x * v[1] + uvw.y * v[2];
}

float3 BerpPosition(Vertex v[3], float3 uvw)
{
    return uvw.z * v[0].position + uvw.x * v[1].position + uvw.y * v[2].position;
}

float3 BerpNormal(Vertex v[3], float3 uvw)
{
    return uvw.z * v[0].normal + uvw.x * v[1].normal + uvw.y * v[2].normal;
}

Vertex BerpVertex(Vertex v[3], float3 uvw)
{
    Vertex vertex;
    vertex.position = uvw.z * v[0].position + uvw.x * v[1].position + uvw.y * v[2].position;
    vertex.normal   = uvw.z * v[0].normal   + uvw.x * v[1].normal   + uvw.y * v[2].normal;

    return vertex;
}

float3 SmoothPnTriangle(Vertex vertex[3], float3 uvw)
{
    float3 P1 = vertex[0].position;
    float3 P2 = vertex[1].position;
    float3 P3 = vertex[2].position;

    float3 N1 = vertex[0].normal;
    float3 N2 = vertex[1].normal;
    float3 N3 = vertex[2].normal;

    float3 B300 = P1;
    float3 B030 = P2;
    float3 B003 = P3;

    const float weight = 1.0 / 3.0;

    float w32 = dot((P2 - P3), N3);
    float w23 = dot((P3 - P2), N2);
    float w31 = dot((P1 - P3), N3);
    float w13 = dot((P3 - P1), N1);
    float w21 = dot((P1 - P2), N2);
    float w12 = dot((P2 - P1), N1);

    float3 B012 = weight * (2.0 * P3 + P2 - w32 * N3);
    float3 B021 = weight * (2.0 * P2 + P3 - w23 * N2);
    float3 B102 = weight * (2.0 * P3 + P1 - w31 * N3);
    float3 B201 = weight * (2.0 * P1 + P3 - w13 * N1);
    float3 B120 = weight * (2.0 * P2 + P1 - w21 * N2);
    float3 B210 = weight * (2.0 * P1 + P2 - w12 * N1);

    float3 E = weight * 0.5 * (B210 + B120 + B021 + B012 + B102 + B201);
    float3 V = weight * (P1 + P2 + P3);

    float3 B111 = E + (E - V) * 0.5;

    float u = uvw.x;
    float v = uvw.y;
    float w = uvw.z;

    float u2 = u * u;
    float v2 = v * v;
    float w2 = w * w;

    float u3 = u2 * u;
    float v3 = v2 * v;
    float w3 = w2 * w;

    return
        B300 * w3 +
        B030 * u3 +
        B003 * v3 +
        B210 * 3.0 * w2 * u  +
        B120 * 3.0 * w  * u2 +
        B201 * 3.0 * w2 * v  +
        B021 * 3.0 * u2 * v  +
        B102 * 3.0 * w  * v2 +
        B012 * 3.0 * u  * v2 +
        B111 * 6.0 * w * u * v;
}

float3 PIi(float3 pi, float3 ni, float3 q)
{
    float3 qpi = q - pi;

    return q - dot(qpi, ni) * ni;
}

float3 SmoothPhongTessellation(Vertex vertex[3], float3 uvw)
{
    float3 Pi = vertex[0].position;
    float3 Pj = vertex[1].position;
    float3 Pk = vertex[2].position;

    float3 Ni = vertex[0].normal;
    float3 Nj = vertex[1].normal;
    float3 Nk = vertex[2].normal;

    float3 PIiPj = PIi(Pi, Ni, Pj);
    float3 PIjPi = PIi(Pj, Nj, Pi);
    float3 PIjPk = PIi(Pj, Nj, Pk);
    float3 PIkPj = PIi(Pk, Nk, Pj);
    float3 PIkPi = PIi(Pk, Nk, Pi);
    float3 PIiPk = PIi(Pi, Ni, Pk);

    float uu = uvw.x * uvw.x;
    float vv = uvw.y * uvw.y;
    float ww = uvw.z * uvw.z;

    float uv = uvw.x * uvw.y;
    float vw = uvw.y * uvw.z;
    float wu = uvw.z * uvw.x;

    float3 berp = BerpPosition(vertex, uvw);
    float3 phong =
        ww * Pi +
        uu * Pj +
        vv * Pk +
        wu * (PIiPj + PIjPi) +
        uv * (PIjPk + PIkPj) +
        vw * (PIkPi + PIiPk);

    return lerp(berp, phong, 0.5);
}

void InterpolateVertex(float2 u, Vertex vertex[3], out float3 interpolated)
{
    float3 uvw = float3(u.xy, 1.0 - u.x - u.y);

#if _PN_TRIANGLE
    interpolated = SmoothPnTriangle(vertex, uvw);
#elif _PHONG_TESSELLATION
    interpolated = SmoothPhongTessellation(vertex, uvw);
#else
    interpolated = BerpPosition(vertex, uvw);
#endif
}

void InterpolateVertex(float2 u, Vertex vertex[3], out Vertex interpolated)
{
    float3 uvw = float3(u.xy, 1.0 - u.x - u.y);

#if _PN_TRIANGLE
    interpolated.position = SmoothPnTriangle(vertex, uvw);
    interpolated.normal = BerpNormal(vertex, uvw);
#elif _PHONG_TESSELLATION
    interpolated.position = SmoothPhongTessellation(vertex, uvw);
    interpolated.normal = BerpNormal(vertex, uvw);
#else
    interpolated = BerpVertex(vertex, uvw);
#endif
}

#endif