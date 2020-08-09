#ifndef _MESH_SUBDIVISION_LIT_PASS_INCLUDED_
#define _MESH_SUBDIVISION_LIT_PASS_INCLUDED_

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct Varyings
{
    float2 uv              : TEXCOORD0;
    float3 color           : TEXCOORD1;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    float3 positionWS      : TEXCOORD2;
#endif

#ifdef _NORMALMAP
    float4 normalWS        : TEXCOORD3;    // xyz: normal, w: viewDir.x
    float4 tangentWS       : TEXCOORD4;    // xyz: tangent, w: viewDir.y
    float4 bitangentWS     : TEXCOORD5;    // xyz: bitangent, w: viewDir.z
#else
    float3 normalWS        : TEXCOORD3;
    float3 viewDirWS       : TEXCOORD4;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord     : TEXCOORD7;
#endif

    float4 positionCS      : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

float4 VisualizeLevel(Subdivision subd, uint vertexId)
{
    float4 color = float4(0.0, 0.0, 0.0, 1.0);

    int level = log2(subd.msb);

    if (level ==  0) { color.r   = 0.1; }
    if (level ==  1) { color.g   = 0.1; }
    if (level ==  2) { color.b   = 0.1; }
    if (level ==  3) { color.rg  = 0.1; }
    if (level ==  4) { color.gb  = 0.1; }
    if (level ==  5) { color.br  = 0.1; }
    if (level ==  6) { color.rgb = 0.1; }

    if (level ==  7) { color.r   = 0.2; }
    if (level ==  8) { color.g   = 0.2; }
    if (level ==  9) { color.b   = 0.2; }
    if (level == 10) { color.rg  = 0.2; }
    if (level == 11) { color.gb  = 0.2; }
    if (level == 12) { color.br  = 0.2; }
    if (level == 13) { color.rgb = 0.2; }

    if (level == 14) { color.r   = 0.3; }
    if (level == 15) { color.g   = 0.3; }
    if (level == 16) { color.b   = 0.3; }
    if (level == 17) { color.rg  = 0.3; }
    if (level == 18) { color.gb  = 0.3; }
    if (level == 19) { color.br  = 0.3; }
    if (level == 20) { color.rgb = 0.3; }

    if (level == 21) { color.r   = 0.4; }
    if (level == 22) { color.g   = 0.4; }
    if (level == 23) { color.b   = 0.4; }
    if (level == 24) { color.rg  = 0.4; }
    if (level == 25) { color.gb  = 0.4; }
    if (level == 26) { color.br  = 0.4; }
    if (level == 27) { color.rgb = 0.4; }

    if (level == 28) { color.r   = 0.5; }
    if (level == 29) { color.g   = 0.5; }
    if (level == 30) { color.b   = 0.5; }

    if (vertexId == 0) color.r += 0.5;
    if (vertexId == 1) color.g += 0.5;
    if (vertexId == 2) color.b += 0.5;

    //if (subd.isUnmergeable) { color.rgb = float3(1.0, 0.0, 0.0); }
    //else { color.rgb = 0.0; }

    return color;
}

void SubdivideFlatNormal(uint msb, float3x3 xf, Vertex vertex[3], out float3 normal)
{
    float3 interpolated[3];

    float3 uv0 = float3(0.0, 0.0, 1.0);
    float3 uv1 = float3(1.0, 0.0, 1.0);
    float3 uv2 = float3(0.0, 1.0, 1.0);

#ifdef REORDER_VERTICES
    // the order of vertices is CCW at the odd level,
    // the order of them is CW at the even level

    int powLevel = msb; // this is the same as (2 ^ level)
    bool evenLevel = 0xAAAAAAAA & powLevel;

    if (evenLevel)
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

    InterpolateVertex(u0, vertex, interpolated[0]);
    InterpolateVertex(u1, vertex, interpolated[1]);
    InterpolateVertex(u2, vertex, interpolated[2]);

    float3 P01 = interpolated[1] - interpolated[0];
    float3 P02 = interpolated[2] - interpolated[0];

    normal = normalize(cross(P01, P02));
}

void InitializeInputData(Varyings input, float3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

#ifdef _ADDITIONAL_LIGHTS
    inputData.positionWS = input.positionWS;
#endif

#ifdef _NORMALMAP
    float3 viewDirWS = float3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
    inputData.normalWS = TransformTangentToWorld(normalTS,
        float3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz));
#else
    float3 viewDirWS = input.viewDirWS;
    inputData.normalWS = input.normalWS;
#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    viewDirWS = SafeNormalize(viewDirWS);
    inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

    inputData.fogCoord = 0.0; // ignore
    inputData.vertexLighting = 0.0; // ignore
    inputData.bakedGI = 0.0; // ignore
}

VertexPositionInputs GetVertexPositionInputsMeshSubd(float3 positionWS)
{
    VertexPositionInputs input;
    input.positionWS = positionWS;
    input.positionVS = TransformWorldToView(input.positionWS);
    input.positionCS = TransformWorldToHClip(input.positionWS);

    float4 ndc = input.positionCS * 0.5f;
    input.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
    input.positionNDC.zw = input.positionCS.zw;

    return input;
}

Varyings LitPassVertex(uint vertexId : SV_VertexID, uint instId : SV_InstanceID)
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
    float3 norWS = vertexInterpolated.normal;

    VertexPositionInputs vertexInput = GetVertexPositionInputsMeshSubd(posWS);
    float3 viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;

#if defined(_VISUALIZATION)
    float4 color = VisualizeLevel(subd, vertexId);
#else
    float4 color = float4(0.5, 0.5, 0.5, 1.0);
#endif

    output.uv = 0.0; //
    output.color = color.xyz;

#if defined(_NORMALMAP)
    output.normalWS = float4(norWS, viewDirWS.x); // viewDirWS.x
    output.tangetnWS = 0.0; //
    output.bitangentWS = 0.0; //
#else
    output.normalWS = norWS;
    output.viewDirWS = viewDirWS;
#endif

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    output.positionWS = 0.0; //
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    output.positionCS = vertexInput.positionCS;

    return output;
}

float4 LitPassFragment(Varyings input) : SV_Target
{
    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);

#ifndef _VISUALIZATION
    float4 color = UniversalFragmentPBR(
        inputData,
        surfaceData.albedo,
        surfaceData.metallic,
        surfaceData.specular,
        surfaceData.smoothness,
        surfaceData.occlusion,
        surfaceData.emission,
        surfaceData.alpha);
#else
    float NdotL = 1.0;
    float3 intensity = 1.0;

    float3 diffuse = 1.0 / 3.141592653;
    float3 albedo = input.color;
    float3 ambient = 0.2;

    float3 radiance = diffuse * NdotL * intensity + ambient;

    float4 color = float4(albedo, 1.0);
#endif

    return color;
}

#endif