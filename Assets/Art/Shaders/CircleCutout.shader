Shader "UI/CircleCutout"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Radius ("Radius", Float) = 1.0
        _Softness ("Edge Softness", Float) = 0.02
        _Color ("Tint", Color) = (0, 0, 0, 1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Cull Off
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _Center;
            float _Radius;
            float _Softness;
            fixed4 _Color;
            float4 _ClipRect;

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 center = _Center.xy;
                float2 uv = i.uv;

                // Correct for aspect ratio so the circle isn't stretched
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 diff = uv - center;
                diff.x *= aspect;

                float dist = length(diff);
                float alpha = smoothstep(_Radius, _Radius + _Softness, dist);

                fixed4 col = fixed4(_Color.rgb, alpha * _Color.a);
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                return col;
            }
            ENDCG
        }
    }
}
