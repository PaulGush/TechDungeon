Shader "Custom/SpriteHitFlash"
{
    Properties
    {
        [HideInInspector] [PerRendererData] [NoScaleOffset] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HideInInspector] [PerRendererData] _RendererColor ("RendererColor", Color) = (1, 1, 1, 1)
        _FlashColor ("Flash Color", Color) = (1, 1, 1, 1)
        _FlashAmount ("Flash Amount", Range(0, 1)) = 0

        [Header(Shield Shimmer)]
        _ShimmerColor ("Shimmer Color", Color) = (0.3, 0.7, 1.4, 1)
        _ShimmerAmount ("Shimmer Amount", Range(0, 1)) = 0
        _ShimmerScanSpeed ("Shimmer Scan Speed", Float) = 1.8
        _ShimmerScanFreq ("Shimmer Scan Frequency", Float) = 3.5
        _ShimmerPulseSpeed ("Shimmer Pulse Speed", Float) = 4.0
        _ShimmerPulseMin ("Shimmer Pulse Min", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        HLSLINCLUDE
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

        // Per-renderer (populated by SpriteRenderer via MaterialPropertyBlock).
        // Declared outside UnityPerMaterial so Unity can override it per-sprite.
        float4 _RendererColor;

        CBUFFER_START(UnityPerMaterial)
            float4 _FlashColor;
            float _FlashAmount;
            float4 _ShimmerColor;
            float _ShimmerAmount;
            float _ShimmerScanSpeed;
            float _ShimmerScanFreq;
            float _ShimmerPulseSpeed;
            float _ShimmerPulseMin;
        CBUFFER_END

        Varyings ShimmerVert(Attributes input)
        {
            Varyings output;
            output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
            output.uv = input.uv;
            output.color = input.color;
            return output;
        }

        half4 ShimmerFrag(Varyings input) : SV_Target
        {
            half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
            col *= input.color * _RendererColor;
            col.rgb = lerp(col.rgb, _FlashColor.rgb, _FlashAmount);

            // Phase Shield: animated vertical scanline blended with a slow whole-sprite pulse.
            // Scan provides the moving "energy" line; pulse keeps a baseline blue tint visible
            // even between scans so the player reads as shielded at all times. _ShimmerAmount
            // is the on/off lever the buff controller flips.
            if (_ShimmerAmount > 0)
            {
                float scan = sin((input.uv.y * _ShimmerScanFreq * 6.28318) - (_Time.y * _ShimmerScanSpeed * 6.28318));
                scan = saturate(scan * 0.5 + 0.5);
                float pulse = (sin(_Time.y * _ShimmerPulseSpeed) * 0.5 + 0.5);
                pulse = lerp(_ShimmerPulseMin, 1.0, pulse);
                float k = saturate(_ShimmerAmount * _ShimmerColor.a * scan * pulse);
                col.rgb = lerp(col.rgb, _ShimmerColor.rgb, k);
            }

            return col;
        }
        ENDHLSL

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex ShimmerVert
            #pragma fragment ShimmerFrag
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex ShimmerVert
            #pragma fragment ShimmerFrag
            ENDHLSL
        }
    }

    FallBack Off
}
