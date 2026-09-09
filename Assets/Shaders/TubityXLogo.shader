// TubityX title logo - single-pass uGUI shader.
//
// The texture it reads is DATA, not artwork:
//     R = signed distance field of the wordmark (0.5 = outline)
//     G = framing-line / marker mask
//     B = soft glow of the framing lines
// Every visual band - neon rim, chromatic ghost rim, inline contour, frosted
// glass body, bloom halo and the animated sheen - is generated here, so the
// whole logo is one quad, one texture and one draw call. Nothing animates on
// the CPU: the sheen is driven by _Time, so the menu costs zero scripts.
//
// Colours are authored as a left-to-right ramp (_ColorA -> _ColorB) which is
// what turns "TUBITY" cyan and the "X" magenta; the ghost rim uses the
// opposite end of the ramp, which is what puts a magenta edge on the cyan
// letters and a cyan edge on the X.
//
// The defaults are tuned for a LINEAR colour-space project (this one is) on a
// Screen Space - Overlay canvas, i.e. with no URP bloom reaching the logo - all
// of the glow you see is generated here.

Shader "UI/TubityXLogo"
{
    Properties
    {
        [PerRendererData] _MainTex ("Packed SDF (R=word G=lines B=lineGlow)", 2D) = "black" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Spread ("SDF Spread (px baked)", Float) = 28
        _Softness ("Edge Softness", Range(0.5, 3)) = 1

        [Header(Palette)]
        _ColorA ("Colour A (left)", Color) = (0.42, 0.93, 1.0, 1)
        _ColorB ("Colour B (right)", Color) = (1.0, 0.30, 0.86, 1)
        _GradStart ("Ramp Start (u)", Range(0,1)) = 0.66
        _GradEnd ("Ramp End (u)", Range(0,1)) = 0.75

        [Header(Neon rim)]
        _StrokeOut ("Rim Outside (px)", Range(0,6)) = 1.05
        _StrokeIn ("Rim Inside (px)", Range(0,6)) = 2.15
        _CoreWhite ("Rim Core Whiteness", Range(0,1)) = 0.62

        [Header(Inline contour)]
        _InlinePos ("Inline Offset (px)", Range(0,12)) = 3.25
        _InlineW ("Inline Width (px)", Range(0,4)) = 1.00
        _InlineAlpha ("Inline Alpha", Range(0,1)) = 0.85
        _InlineWhite ("Inline Whiteness", Range(0,1)) = 0.12

        [Header(Glass body)]
        _BodyColor ("Body Colour", Color) = (0.030, 0.035, 0.075, 1)
        _BodyAlpha ("Body Alpha", Range(0,1)) = 0.92
        _BodyTint ("Body Neon Tint", Range(0,1)) = 0.012
        _GlassColor ("Frosted Panel Colour", Color) = (0.62, 0.76, 0.95, 1)
        _GlassAlpha ("Frosted Panel Alpha", Range(0,1)) = 0.09
        _GlassTopBoost ("Frosted Top Light", Range(0,2)) = 0.55

        [Header(Glow)]
        _GlowRange ("Halo Range (px)", Range(1,28)) = 26
        _GlowPower ("Halo Falloff Curve", Range(1,8)) = 2.6
        _GlowStrength ("Halo Strength", Range(0,2)) = 0.70
        _HotFalloff ("Neon Bleed Falloff (px)", Range(0.5,10)) = 2.6
        _HotStrength ("Neon Bleed Strength", Range(0,2)) = 0.50

        [Header(Chromatic ghost rim)]
        _RimOffset ("Ghost Offset (px)", Vector) = (-1.9, 1.9, 0, 0)
        _RimStrength ("Ghost Strength", Range(0,1)) = 0.85

        [Header(Framing lines)]
        _FrameAlpha ("Line Alpha", Range(0,1)) = 1
        _FrameWhite ("Line Whiteness", Range(0,1)) = 0.35
        _FrameGlow ("Line Glow", Range(0,2)) = 0.80

        [Header(Sheen sweep)]
        _SheenColor ("Sheen Colour", Color) = (1,1,1,1)
        _SheenPeriod ("Sweep Period (sec)", Range(0.5, 30)) = 6
        _SheenDuty ("Sweep Fraction Of Period", Range(0.02, 1)) = 0.22
        _SheenWidth ("Sheen Width (uv)", Range(0, 0.4)) = 0.055
        _SheenSoft ("Sheen Softness (uv)", Range(0.001, 0.4)) = 0.075
        _SheenSkew ("Sheen Skew", Range(-2, 2)) = -0.45
        _SheenStrength ("Sheen Strength", Range(0, 2)) = 0.45
        _SheenGlassBoost ("Sheen Boost On Glass", Range(0, 2)) = 0.70

        [Header(UI plumbing)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            Name "TUBITYX_LOGO"
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            float  _Spread, _Softness;
            fixed4 _ColorA, _ColorB;
            float  _GradStart, _GradEnd;
            float  _StrokeOut, _StrokeIn, _CoreWhite;
            float  _InlinePos, _InlineW, _InlineAlpha, _InlineWhite;
            fixed4 _BodyColor, _GlassColor;
            float  _BodyAlpha, _BodyTint, _GlassAlpha, _GlassTopBoost;
            float  _GlowRange, _GlowPower, _GlowStrength, _HotFalloff, _HotStrength;
            float4 _RimOffset;
            float  _RimStrength;
            float  _FrameAlpha, _FrameWhite, _FrameGlow;
            fixed4 _SheenColor;
            float  _SheenPeriod, _SheenDuty, _SheenWidth, _SheenSoft;
            float  _SheenSkew, _SheenStrength, _SheenGlassBoost;

            // 1 inside [lo,hi], antialiased by aa
            inline float band(float d, float lo, float hi, float aa)
            {
                return saturate((d - lo) / aa) * saturate((hi - d) / aa);
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                // --- decode the packed field ------------------------------
                half4 t = tex2D(_MainTex, uv);
                float spread2 = _Spread * 2.0;
                float d = (t.r - 0.5) * spread2;                      // texture px, + outside
                // _RimOffset is authored in texture pixels with +y pointing DOWN,
                // so flip y against Unity's bottom-left UV origin.
                float2 ghostUV = uv + float2(_RimOffset.x, -_RimOffset.y) * _MainTex_TexelSize.xy;
                float d2 = (tex2D(_MainTex, ghostUV).r - 0.5) * spread2;
                half frameM = t.g;
                half frameG = t.b;

                // one screen pixel expressed in texture pixels
                float aa = max(max(fwidth(uv.x) * _MainTex_TexelSize.z,
                                   fwidth(uv.y) * _MainTex_TexelSize.w), 0.55) * _Softness;

                // --- palette ramp -----------------------------------------
                float g = saturate((uv.x - _GradStart) / max(_GradEnd - _GradStart, 1e-4));
                g = g * g * (3.0 - 2.0 * g);
                half3 main = lerp(_ColorA.rgb, _ColorB.rgb, g);
                half3 comp = lerp(_ColorB.rgb, _ColorA.rgb, g);       // opposite end of the ramp

                // --- masks -------------------------------------------------
                float inlineLo = _InlinePos + _InlineW;
                float strokeM = band(d, -_StrokeIn, _StrokeOut, aa);
                float inlineM = band(d, -inlineLo, -_InlinePos, aa);
                float fillM   = saturate((-inlineLo - d) / aa);
                float bodyM   = saturate((_StrokeOut - d) / aa);
                float ghostM  = band(d2, -_StrokeIn, _StrokeOut, aa) * (1.0 - strokeM) * _RimStrength;

                // Windowed falloff rather than exp(): it reaches exactly zero at
                // _GlowRange, so the halo cannot leave a faint rectangle across the
                // whole sprite where the baked field hits its clamp.
                float hw = saturate(1.0 - max(d, 0.0) / _GlowRange);
                float halo = pow(hw, _GlowPower) * _GlowStrength;
                float bleed = exp(-abs(d) / _HotFalloff) * _HotStrength;

                // rim burns toward white in its middle
                float halfW = 0.5 * (_StrokeIn + _StrokeOut);
                float core = saturate(1.0 - abs(d + (_StrokeIn - _StrokeOut) * 0.5) / max(halfW, 1e-4));
                core = core * core * _CoreWhite;
                half3 strokeCol = main + (1.0 - main) * core;

                // --- composite back to front -------------------------------
                half3 rgb = 0;
                half  a = 0;
                #define OVER(SRC, ALPHA) { half _s = saturate(ALPHA); rgb = lerp(rgb, SRC, _s); a = lerp(a, 1.0, _s); }

                float top = uv.y;   // uv.y == 1 is the top of the sprite
                OVER(main, halo)                                                  // bloom halo
                OVER(_BodyColor.rgb + main * _BodyTint, bodyM * _BodyAlpha)       // dark glass body
                OVER(_GlassColor.rgb * (0.55 + _GlassTopBoost * top),
                     fillM * _GlassAlpha * (0.6 + 0.8 * top))                     // frosted panel
                OVER(main, bleed)                                                 // neon bleed
                OVER(comp, ghostM)                                                // chromatic ghost rim
                OVER(main + (1.0 - main) * _InlineWhite, inlineM * _InlineAlpha)   // inline contour
                OVER(strokeCol, strokeM)                                          // neon rim
                OVER(main, frameG * _FrameGlow)                                   // framing-line glow
                OVER(main + (1.0 - main) * _FrameWhite, frameM * _FrameAlpha)      // framing lines
                #undef OVER

                // --- sheen sweep -------------------------------------------
                // frac() gives one cycle per _SheenPeriod seconds; the band only
                // travels during the first _SheenDuty of it and then parks
                // off-screen, so the pause between sweeps costs no branch.
                float cyc = frac(_Time.y / _SheenPeriod);
                float travel = saturate(cyc / _SheenDuty);
                float pos = lerp(-0.35, 1.35, travel);
                float sd = abs(uv.x + (0.5 - uv.y) * _SheenSkew - pos);
                float s = 1.0 - saturate((sd - _SheenWidth) / _SheenSoft);
                s = s * s * (3.0 - 2.0 * s) * _SheenStrength;
                rgb += _SheenColor.rgb * (s * a * (1.0 + _SheenGlassBoost * fillM));

                half4 col = half4(rgb, a) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif
                return col;
            }
        ENDCG
        }
    }
    Fallback "UI/Default"
}
