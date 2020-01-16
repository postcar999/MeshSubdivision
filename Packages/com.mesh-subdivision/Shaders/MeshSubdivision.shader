Shader "MeshSubdivision/MeshSubdivision"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        Pass
        {
            Name "Lit"
            Tags { "LightMode" = "MeshSubdivisionForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma multi_compile _ _PN_TRIANGLE _PHONG_TESSELLATION

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "MeshSubdivisionPasses.hlsl"
            ENDHLSL
        }

        //Pass
        //{
        //    Name "ShadowCaster"
        //    Tags { "LightMode" = "ShadowCaster" }
        //
        //    ZWrite On
        //    ZTest LEqual
        //
        //    HLSLPROGRAM
        //    #pragma target 4.5
        //
        //    #pragma vertex ShadowPassVertex
        //    #pragma fragment ShadowPassFragment
        //    ENDHLSL
        //}
    }

    //FallBack
}
