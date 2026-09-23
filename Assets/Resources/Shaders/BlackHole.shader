// A black hole in the style of the Interstellar renders, on one camera-facing quad:
//   - the shadow: a black disc that hides everything behind it
//   - the accretion disc, seen nearly edge-on: a thin flattened band whose far half passes behind
//     the shadow and whose near half crosses in front of it. Gas orbits faster further in and the
//     side turning toward the viewer is brighter (Doppler beaming)
//   - the lensed image of the far side of the disc, bent up over the top of the shadow and down
//     under it: a ring hugging the shadow, strongest on top
//   - the photon ring: a thin bright line right at the shadow's edge
// Output is premultiplied: the shadow is opaque black, all the light is additive. No fog.
Shader "TubityX/BlackHole"
{
    Properties
    {
        _HotColor ("Inner Disc", Color) = (2.0, 1.55, 1.0, 1)
        _CoolColor ("Outer Disc", Color) = (1.4, 0.55, 0.16, 1)
        _ShadowRadius ("Shadow Radius", Float) = 0.14
        _DiscIn ("Disc Inner Edge", Float) = 0.21
        _DiscOut ("Disc Outer Edge", Float) = 0.95
        _Squash ("Disc Flattening", Float) = 0.1
        _Spin ("Orbit Speed", Float) = 0.35
        _Intensity ("Intensity", Float) = 1.3
        _Fade ("Fade", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-200" "IgnoreProjector" = "True" }
        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "WorldNoise.cginc"

            half4 _HotColor, _CoolColor;
            float _ShadowRadius, _DiscIn, _DiscOut, _Squash, _Spin, _Intensity, _Fade;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 p : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.p = v.uv * 2.0 - 1.0;
                return o;
            }

            // Turbulent gas at disc radius rd and orbital angle a (already advanced by the orbit).
            // Sampled on a circle in 3D so the angle has no seam.
            float gas(float a, float rd)
            {
                float n = wn_fbm3(float3(cos(a) * 2.5, sin(a) * 2.5, rd * 7.0));
                float s = wn_fbm3(float3(cos(a) * 7.0, sin(a) * 7.0, rd * 22.0));
                return 0.35 + 0.9 * n + 0.4 * s;
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 p = i.p;
                float r = length(p);
                float rs = _ShadowRadius;
                float lr = r / rs;
                float t = _Time.y;

                // ---- The disc, un-flattened back into its own plane ----
                float2 q = float2(p.x, p.y / _Squash);
                float rq = length(q);
                float discMask = smoothstep(_DiscIn, _DiscIn + 0.05, rq) * (1.0 - smoothstep(_DiscOut * 0.65, _DiscOut, rq));
                float omega = _Spin / pow(max(rq, 0.08), 1.5);           // Keplerian: inner gas laps the outer
                float a = atan2(q.y, q.x) - t * omega;
                float radial = pow(saturate(1.0 - (rq - _DiscIn) / (_DiscOut - _DiscIn)), 1.6);
                float doppler = 1.0 + 0.6 * (-q.x / max(rq, 1e-3));      // the approaching side, left, is brighter
                float3 disc = lerp(_CoolColor.rgb, _HotColor.rgb, radial) * gas(a, rq) * radial * doppler * discMask;

                // ---- The lensed far side: a ring hugging the shadow, brightest over the top ----
                float ring = smoothstep(1.02, 1.12, lr) * (1.0 - smoothstep(1.3, 2.4, lr));
                float ap = atan2(p.y, p.x) - t * _Spin * 2.5;
                float over = lerp(0.45, 1.0, saturate(p.y / max(r, 1e-3) * 0.5 + 0.5));
                float dop2 = 1.0 + 0.4 * (-p.x / max(r, 1e-3));
                float3 lens = lerp(_CoolColor.rgb, _HotColor.rgb, 0.65) * ring * gas(ap, lr * 0.3) * over * dop2 * 0.8;

                // ---- Photon ring ----
                float photon = exp(-pow((lr - 1.04) / 0.025, 2.0));
                float3 ringLight = _HotColor.rgb * photon * 1.6;

                // A soft glow off the whole system.
                float3 haze = _CoolColor.rgb * exp(-r * 4.0) * 0.12;

                // ---- Compose: the shadow hides the far half and the lensed light; the near half
                // of the disc crosses in front of it. ----
                float shadow = 1.0 - smoothstep(0.985, 1.0, lr);
                float nearHalf = step(p.y, 0.0);
                float3 behind = lens + ringLight + haze + disc * (1.0 - nearHalf);
                float3 col = behind * (1.0 - shadow) + disc * nearHalf;

                col *= saturate((1.0 - r) * 3.0);
                return half4(col * _Intensity * _Fade, shadow * _Fade);
            }
            ENDCG
        }
    }
}
