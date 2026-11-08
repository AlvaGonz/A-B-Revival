Shader "ABRevival/InstancedSprite"
{
    Properties
    {
        _MainTex ("Unified Atlas", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ABRevivalSprite"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
            UNITY_DOTS_INSTANCING_START(UserPropertyMetadata)
                UNITY_DOTS_INSTANCED_PROP(float4, _UVRect)
            UNITY_DOTS_INSTANCING_END(UserPropertyMetadata)
            #endif

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                #ifdef UNITY_DOTS_INSTANCING_ENABLED
                    float4 uvRect = UNITY_ACCESS_DOTS_INSTANCED_PROP(float4, _UVRect);
                    uint tint = unity_RendererUserValue;
                #else
                    float4 uvRect = float4(0.0, 0.0, 1.0, 1.0);
                    uint tint = 0xFFFFFFFFu;
                #endif

                output.uv = uvRect.xy + input.uv * uvRect.zw;
                output.color = half4(
                    (half)(tint & 0xFFu) / 255.0h,
                    (half)((tint >> 8) & 0xFFu) / 255.0h,
                    (half)((tint >> 16) & 0xFFu) / 255.0h,
                    (half)((tint >> 24) & 0xFFu) / 255.0h);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return tex * input.color;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
