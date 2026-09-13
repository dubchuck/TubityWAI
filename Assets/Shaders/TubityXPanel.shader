// TubityX button / panel chrome - no texture at all.
//
// The rounded rectangle is evaluated analytically per fragment, so one quad
// gives dark glass fill, top specular, neon rim, chromatic ghost rim and an
// outer halo with zero texture fetches. The mesh carries the geometry:
//
//     TEXCOORD0 = pixels from the quad centre
//     TEXCOORD1 = (halfWidth, halfHeight, cornerRadius, highlight)
//
// which is why the same material can serve buttons of different sizes, and why
// hover / focus (highlight) costs a vertex rewrite instead of a material copy.
// The border pulse is driven by _Time - no script touches the material.
//
// The canvas must expose TexCoord1; TubityXPanel.cs turns it on.

Shader "UI/TubityXPanel"
{
    Properties
    {
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Rim)]
        _RimWidth ("Rim Width (px)", Range(0.5, 8)) = 2.2
        _RimCoreWhite ("Rim Core Whiteness", Range(0,1)) = 0.45

        [Header(Pulse)]
        _PulseColor ("Pulse Colour", Color) = (0.45, 0.98, 1.0, 1)
        _PulsePeriod ("Pulse Period (sec)", Range(0.2, 10)) = 1.6
        _PulseBoost ("Pulse Brightness Boost", Range(0,2)) = 0.55

        [Header(Glass)]
        _GlassColor ("Glass Colour", Color) = (0.022, 0.040, 0.098, 1)
        _GlassAlpha ("Glass Alpha", Range(0,1)) = 0.90
        _SpecColor ("Specular Colour", Color) = (0.72, 0.86, 1.0, 1)
        _SpecAlpha ("Specular Alpha", Range(0,1)) = 0.10

        [Header(Glow)]
        _HaloRange ("Halo Range (px)", Range(1, 64)) = 24
        _HaloPower ("Halo Falloff Curve", Range(1,8)) = 2.6
        _HaloStrength ("Halo Strength", Range(0,2)) = 0.55
        _BleedFalloff ("Neon Bleed Falloff (px)", Range(0.5,10)) = 2.6
        _BleedStrength ("Neon Bleed Strength", Range(0,2)) = 0.22

        [Header(Chromatic ghost rim)]
        _GhostOffset ("Ghost Offset (px)", Vector) = (-1.6, 1.6, 0, 0)
        _GhostStrength ("Ghost Strength", Range(0,1)) = 0.55

        [Header(Interaction)]
        _HighlightBoost ("Highlight Boost", Range(0,3)) = 0.70

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
            Name "TUBITYX_PANEL"
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
                float4 texcoord : TEXCOORD0;   // pixels from the quad centre
                float4 texcoord1: TEXCOORD1;   // halfW, halfH, radius, highlight
                float4 texcoord2: TEXCOORD2;   // rim colour rgb, pulse amount
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 local         : TEXCOORD0;
                float4 shape         : TEXCOORD1;
                float4 tint          : TEXCOORD2;
                float4 worldPosition : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            float4 _ClipRect;
            fixed4 _PulseColor, _GlassColor, _SpecColor;
            float  _RimWidth, _RimCoreWhite;
            float  _PulsePeriod, _PulseBoost;
            float  _GlassAlpha, _SpecAlpha;
            float  _HaloRange, _HaloPower, _HaloStrength, _BleedFalloff, _BleedStrength;
            float4 _GhostOffset;
            float  _GhostStrength, _HighlightBoost;

            inline float band(float d, float lo, float hi, float aa)
            {
                return saturate((d - lo) / aa) * saturate((hi - d) / aa);
            }

            // signed distance to a rounded rectangle, positive outside
            inline float roundBox(float2 p, float2 halfExtent, float r)
            {
                float2 q = abs(p) - (halfExtent - r);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.local = v.texcoord.xy;
                OUT.shape = v.texcoord1;
                OUT.tint = v.texcoord2;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 halfSize = IN.shape.xy;
                float radius = min(IN.shape.z, min(halfSize.x, halfSize.y));
                float highlight = IN.shape.w;

                float d = roundBox(IN.local, halfSize, radius);
                float d2 = roundBox(IN.local - _GhostOffset.xy, halfSize, radius);
                float aa = max(fwidth(d), 0.4);

                // one full cycle of the border pulse per _PulsePeriod seconds
                float pulse = 0.5 - 0.5 * cos(_Time.y * 6.2831853 / _PulsePeriod);
                pulse *= IN.tint.a;                       // per-panel pulse amount
                half3 rim = lerp(IN.tint.rgb, _PulseColor.rgb, pulse);
                float boost = 1.0 + _PulseBoost * pulse + _HighlightBoost * highlight;

                float fillM  = saturate((-_RimWidth - d) / aa);
                float rimM   = band(d, -_RimWidth, 0.0, aa);
                float ghostM = band(d2, -_RimWidth, 0.0, aa) * (1.0 - rimM) * _GhostStrength;
                // gate the halo to the outside, smoothly, or it floods the glass
                float halo   = pow(saturate(1.0 - max(d, 0.0) / _HaloRange), _HaloPower)
                               * _HaloStrength * saturate(d / aa);
                float bleed  = exp(-abs(d) / _BleedFalloff) * _BleedStrength;

                float top = saturate(0.5 + IN.local.y / max(2.0 * halfSize.y, 1e-4));

                half3 rgb = 0;
                half  a = 0;
                #define OVER(SRC, ALPHA) { half _s = saturate(ALPHA); rgb = lerp(rgb, SRC, _s); a = lerp(a, 1.0, _s); }

                OVER(rim * boost, halo * boost)                                   // outer halo
                OVER(_GlassColor.rgb, fillM * _GlassAlpha)                        // dark glass
                OVER(_SpecColor.rgb, fillM * _SpecAlpha * saturate(top * 1.6 - 0.5))
                OVER(rim * boost, bleed * boost)                                  // neon bleed
                OVER(half3(0.9, 0.9, 0.9), ghostM * 0.35)                         // ghost rim
                float core = saturate(1.0 - abs(d + _RimWidth * 0.5) / max(_RimWidth * 0.6, 1e-4));
                core = core * core * _RimCoreWhite;
                OVER(saturate(rim * boost) + (1.0 - rim) * core, rimM)            // neon rim
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
