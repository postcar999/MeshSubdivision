Shader "MeshSubdivision/Lit"
{
    Properties
    {

    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 300

        Pass
        {
            Name "Lit"
            Tags { "LightMode" = "MeshSubdivisionForward" }

            ZWrite On
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5

            // -------------------------------------
            // Material Keywords

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            // -------------------------------------
            // Mesh Subdivision keywords
            #pragma multi_compile _ _CULLING
            #pragma multi_compile _ _PN_TRIANGLE _PHONG_TESSELLATION
            #pragma multi_compile _ _VISUALIZATION

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            #include "LitPassInput.hlsl"
            #include "LitPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
        
            ZWrite On
            ZTest LEqual
            Cull Back
        
            HLSLPROGRAM
            #pragma target 4.5

            // -------------------------------------
            // Mesh Subdivision keywords
            #pragma multi_compile _ _CULLING
            #pragma multi_compile _ _PN_TRIANGLE _PHONG_TESSELLATION

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            #include "DepthOnlyInput.hlsl"
            #include "DepthOnly.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
        
            ZWrite On
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5

            // -------------------------------------
            // Mesh Subdivision keywords
            #pragma multi_compile _ _PN_TRIANGLE _PHONG_TESSELLATION

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "ShadowCasterPassInput.hlsl"
            #include "ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}
