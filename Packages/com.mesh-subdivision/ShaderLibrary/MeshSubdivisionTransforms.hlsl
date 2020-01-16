#ifndef _MESH_SUBDIVISION_TRANSFORMS_INCLUDED_
#define _MESH_SUBDIVISION_TRANSFORMS_INCLUDED_

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

CBUFFER_START(UnityPerCamera)
float4x4 _ViewMatrix;
float4x4 _InvViewMatrix;
float4x4 _ProjMatrix;
float4x4 _InvProjMatrix;
float4x4 _ViewProjMatrix;
float4x4 _InvViewProjMatrix;
CBUFFER_END

CBUFFER_START(UnityPerDraw)
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
CBUFFER_END

CBUFFER_START(_DirectionalLightBuffer)
float4 _LightDirection;
CBUFFER_END

CBUFFER_START(_CaptureCameraBuffer)
float4 _clipPlane;
CBUFFER_END

float4x4 GetRawUnityObjectToWorld() { return unity_ObjectToWorld; }
float4x4 GetRawUnityWorldToObject() { return unity_WorldToObject; }

float4x4 ApplyCameraTranslationToMatrix(float4x4 modelMatrix)
{
    return modelMatrix;
}

float4x4 ApplyCameraTranslationToInverseMatrix(float4x4 inverseModelMatrix)
{
    return inverseModelMatrix;
}

#ifdef UNITY_MATRIX_M
#undef UNITY_MATRIX_M
#endif

#ifdef UNITY_MATRIX_I_M
#undef UNITY_MATRIX_I_M
#endif

#ifdef UNITY_MATRIX_V
#undef UNITY_MATRIX_V
#endif

#ifdef UNITY_MATRIX_I_V
#undef UNITY_MATRIX_I_V
#endif

#ifdef UNITY_MATRIX_P
#undef UNITY_MATRIX_P
#endif

#ifdef UNITY_MATRIX_I_P
#undef UNITY_MATRIX_I_P
#endif

#ifdef UNITY_MATRIX_VP
#undef UNITY_MATRIX_VP
#endif

#ifdef UNITY_MATRIX_I_VP
#undef UNITY_MATRIX_I_VP
#endif

#define UNITY_MATRIX_M   ApplyCameraTranslationToMatrix(GetRawUnityObjectToWorld())
#define UNITY_MATRIX_I_M ApplyCameraTranslationToInverseMatrix(GetRawUnityWorldToObject())

#define UNITY_MATRIX_V    _ViewMatrix
#define UNITY_MATRIX_I_V  _InvViewMatrix
#define UNITY_MATRIX_P    _ProjMatrix
#define UNITY_MATRIX_I_P  _InvProjMatrix
#define UNITY_MATRIX_VP   _ViewProjMatrix
#define UNITY_MATRIX_I_VP _InvViewProjMatrix

float4x4 GetObjectToWorldMatrix()
{
    return UNITY_MATRIX_M;
}

float4x4 GetWorldToObjectMatrix()
{
    return UNITY_MATRIX_I_M;
}

float4x4 GetWorldToViewMatrix()
{
    return UNITY_MATRIX_V;
}

// Transform to homogenous clip space
float4x4 GetWorldToHClipMatrix()
{
    return UNITY_MATRIX_VP;
}

// Transform to homogenous clip space
float4x4 GetViewToHClipMatrix()
{
    return UNITY_MATRIX_P;
}

float3 TransformObjectToWorld(float3 positionOS, float w = 1.0)
{
    return mul(GetObjectToWorldMatrix(), float4(positionOS, w)).xyz;
}

float3 TransformWorldToObject(float3 positionWS, float w = 1.0)
{
    return mul(GetWorldToObjectMatrix(), float4(positionWS, w)).xyz;
}

float3 TransformWorldToView(float3 positionWS, float w = 1.0)
{
    return mul(GetWorldToViewMatrix(), float4(positionWS, w)).xyz;
}

// Transforms position from object space to homogenous space
float4 TransformObjectToHClip(float3 positionOS, float w = 1.0)
{
    // More efficient than computing M*VP matrix product
    return mul(GetWorldToHClipMatrix(), mul(GetObjectToWorldMatrix(), float4(positionOS, w)));
}

// Tranforms position from world space to homogenous space
float4 TransformWorldToHClip(float3 positionWS, float w = 1.0)
{
    return mul(GetWorldToHClipMatrix(), float4(positionWS, w));
}

// Tranforms position from view space to homogenous space
float4 TransformWViewToHClip(float3 positionVS, float w = 1.0)
{
    return mul(GetViewToHClipMatrix(), float4(positionVS, w));
}

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

#endif