// TubityX display-face label - SDF text from Resources/UI/Tex_TubityXFont.
//
// The atlas holds a signed distance field per glyph (0.5 = the glyph edge), so
// one small texture stays crisp at any size and the neon rim and glow are
// distance bands rather than baked pixels. The mesh carries the per-label
// accent colour so every label in the menu shares one material:
//
//     TEXCOORD0 = atlas uv
//     TEXCOORD1 = (rim.r, rim.g, rim.b, glowStrength)
//
// The canvas must expose TexCoord1; TubityXLabel.cs turns it on.

Shader "UI/TubityXLabel"
{
    Properties
    {
        [PerRendererData] _MainTex ("Glyph SDF Atlas", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Spread ("SDF Spread (px baked)", Float) = 10

        _FaceColor ("Face Colour", Color) = (0.95, 0.98, 1.0, 1)
        _FaceAlpha ("Face Alpha", Range(0,1)) = 1
        _RimWidth ("Rim Width (px)", Range(0,6)) = 1.5
        _RimAlpha ("Rim Alpha", Range(0,1)) = 0.90
        _GlowRange ("Glow Range (px)", Range(0.5, 10)) = 6
        _GlowPower ("Glow Falloff Curve", Range(1,8)) = 2.4
        _Softness ("Edge Softness", Range(0.5,3)) = 1

        [Header(UI plumbing)]
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
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
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

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "TUBITYX_LABEL"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float4 texcoord : TEXCOORD0;
                float4 texcoord1: TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 accent        : TEXCOORD1;
                float4 worldPosition : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color, _FaceColor;
            float4 _ClipRect;
            float _Spread, _FaceAlpha, _RimWidth, _RimAlpha;
            float _GlowRange, _GlowPower, _Softness;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.uv = v.texcoord.xy;
                OUT.accent = v.texcoord1;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float d = (tex2D(_MainTex, IN.uv).r - 0.5) * _Spread * 2.0;   // atlas px
                float aa = max(max(fwidth(IN.uv.x) * _MainTex_TexelSize.z,
                                   fwidth(IN.uv.y) * _MainTex_TexelSize.w), 0.55) * _Softness;

                half3 rim = IN.accent.rgb;
                float glowStrength = IN.accent.a;

                float faceM  = saturate((-_RimWidth - d) / aa);   // inside the rim
                float solidM = saturate((0.0 - d) / aa);          // the glyph, rim included
                float rimM   = saturate(solidM - faceM);
                float glow = pow(saturate(1.0 - max(d, 0.0) / _GlowRange), _GlowPower) * glowStrength;

                half3 rgb = 0;
                half  a = 0;
                #define OVER(SRC, ALPHA) { half _s = saturate(ALPHA); rgb = lerp(rgb, SRC, _s); a = lerp(a, 1.0, _s); }
                OVER(rim, glow)
                OVER(rim, rimM * _RimAlpha)
                OVER(_FaceColor.rgb, faceM * _FaceAlpha)
                #undef OVER

                half4 col = half4(rgb, a) * IN.color;
                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                return col;
            }
        ENDCG
        }
    }
    Fallback "UI/Default"
}
