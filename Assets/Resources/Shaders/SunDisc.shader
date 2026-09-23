// A star seen from close range, drawn on one camera-facing quad: a boiling disc (granulation over
// slow convection cells, sunspots, limb darkening) inside a streaming corona. The disc is opaque,
// so it hides the stars behind it; the corona is additive. No fog: it is far past the fog's end
// and must still blaze. _Fade lets a blended environment dissolve it.
Shader "TubityX/SunDisc"
{
    Properties
    {
        _CoreColor ("Core", Color) = (1.7, 1.25, 0.6, 1)
        _EdgeColor ("Limb", Color) = (1.2, 0.32, 0.04, 1)
        _CoronaColor ("Corona", Color) = (1.0, 0.42, 0.10, 1)
        _DiscRadius ("Disc Radius", Float) = 0.42
        _Intensity ("Intensity", Float) = 1.6
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

            half4 _CoreColor, _EdgeColor, _CoronaColor;
            float _DiscRadius, _Intensity, _Fade;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 p : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.p = v.uv * 2.0 - 1.0;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 p = i.p;
                float r = length(p);
                float R = _DiscRadius;
                float t = _Time.y;

                float3 col = 0;
                float alpha = 0;

                if (r < R)
                {
                    // A sphere's normal from the disc position, turning slowly.
                    float2 q = p / R;
                    float z = sqrt(saturate(1.0 - dot(q, q)));
                    float3 n = float3(q, z);
                    float3 sp = n + float3(t * 0.010, 0.0, t * 0.006);

                    float cells = wn_fbm3(sp * 4.5);                   // convection cells
                    float grain = wn_fbm3(sp * 16.0 + t * 0.06);       // granulation boiling on top
                    float heat = saturate(cells * 0.75 + grain * 0.45);
                    float limb = pow(z, 0.45);

                    float3 c = lerp(_EdgeColor.rgb, _CoreColor.rgb, heat * limb);
                    float spots = smoothstep(0.30, 0.20, wn_fbm3(sp * 2.0 + 11.0));
                    c *= 1.0 - spots * 0.65;

                    col = c * _Intensity * (0.5 + 0.5 * limb);
                    alpha = 1.0;
                }

                // Corona: a fast falloff with streamers, over a wide faint haze.
                float d = max(r - R, 0.0);
                float ang = atan2(p.y, p.x);
                float stream = wn_fbm3(float3(cos(ang) * 3.0, sin(ang) * 3.0, d * 5.0 - t * 0.12));
                float corona = exp(-d * 10.0) * (0.45 + 1.0 * stream) + exp(-d * 3.2) * 0.2;
                float3 cc = _CoronaColor.rgb * corona * _Intensity;
                col += (r < R) ? cc * 0.1 : cc;

                // Never let the square edge of the quad show.
                col *= saturate((1.0 - r) * 4.0);
                return half4(col * _Fade, alpha * _Fade);
            }
            ENDCG
        }
    }
}
