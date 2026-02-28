Shader "Custom/PixelOutline"
{
    Properties
    {
        [HideInInspector] [PerRendererData] [NoScaleOffset] _MainTex ("Sprite Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineThickness ("Outline Thickness", Float) = 1
        _PixelSize ("Pixel Size", Vector) = (0.03125, 0.03125, 0, 0)
        [Toggle(CORNERS_ON)] CORNERS ("Corners", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ CORNERS_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineThickness;
                float4 _PixelSize;
                float CORNERS;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                col *= input.color;

                if (col.a > 0.01)
                    return col;

                float2 texelSize = _PixelSize.xy * _OutlineThickness;

                // Cardinal directions
                half upAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(0, texelSize.y)).a;
                half downAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(0, -texelSize.y)).a;
                half leftAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(-texelSize.x, 0)).a;
                half rightAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(texelSize.x, 0)).a;

                half outline = max(max(upAlpha, downAlpha), max(leftAlpha, rightAlpha));

                #ifdef CORNERS_ON
                    half tlAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(-texelSize.x, texelSize.y)).a;
                    half trAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(texelSize.x, texelSize.y)).a;
                    half blAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(-texelSize.x, -texelSize.y)).a;
                    half brAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(texelSize.x, -texelSize.y)).a;
                    outline = max(outline, max(max(tlAlpha, trAlpha), max(blAlpha, brAlpha)));
                #endif

                half4 outlineCol = _OutlineColor;
                outlineCol.a *= outline;
                return outlineCol;
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ CORNERS_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineThickness;
                float4 _PixelSize;
                float CORNERS;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                col *= input.color;

                if (col.a > 0.01)
                    return col;

                float2 texelSize = _PixelSize.xy * _OutlineThickness;

                half upAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(0, texelSize.y)).a;
                half downAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(0, -texelSize.y)).a;
                half leftAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(-texelSize.x, 0)).a;
                half rightAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(texelSize.x, 0)).a;

                half outline = max(max(upAlpha, downAlpha), max(leftAlpha, rightAlpha));

                #ifdef CORNERS_ON
                    half tlAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(-texelSize.x, texelSize.y)).a;
                    half trAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(texelSize.x, texelSize.y)).a;
                    half blAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(-texelSize.x, -texelSize.y)).a;
                    half brAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv + float2(texelSize.x, -texelSize.y)).a;
                    outline = max(outline, max(max(tlAlpha, trAlpha), max(blAlpha, brAlpha)));
                #endif

                half4 outlineCol = _OutlineColor;
                outlineCol.a *= outline;
                return outlineCol;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
