// Procedural skybox for the themed test levels (space, jungle, underwater,
// volcano, crystal). One full-screen pass, two texture reads and a handful
// of ALU ops per pixel so it stays cheap on mobile:
//   * three-stop vertical gradient (bottom / horizon / top)
//   * a tinted, scrolling noise layer for nebulae, clouds, caustics or aurora
//   * optional hashed star field with a gentle twinkle
//   * a sun / moon disc with a soft halo (HDR so it blooms)
// RenderSettings.fog does not touch the skybox; EnvironmentManager sets the
// fog colour to the horizon colour so scenery fades into the sky.
Shader "TubityX/EnvironmentSky"
{
    Properties
    {
        _TopColor ("Top Colour", Color) = (0.1, 0.2, 0.5, 1)
        _HorizonColor ("Horizon Colour", Color) = (0.5, 0.3, 0.6, 1)
        _BottomColor ("Bottom Colour", Color) = (0.02, 0.02, 0.05, 1)
        _HorizonPower ("Horizon Sharpness", Range(0.5, 8)) = 3

        _DetailTex ("Detail Noise", 2D) = "black" {}
        _DetailColor ("Detail Colour", Color) = (0.4, 0.1, 0.6, 1)
        _DetailScale ("Detail Scale", Float) = 2
        _DetailStrength ("Detail Strength", Range(0, 2)) = 0.8
        _DetailScroll ("Detail Scroll (xy)", Vector) = (0.01, 0.003, 0, 0)
        _DetailStretch ("Detail Stretch (xy)", Vector) = (1, 1, 0, 0)

        _StarDensity ("Star Density", Range(0, 1)) = 0
        _StarColor ("Star Colour", Color) = (1, 1, 1, 1)

        _SunDir ("Sun Direction", Vector) = (0.3, 0.5, 0.8, 0)
        _SunColor ("Sun Colour", Color) = (1, 0.9, 0.7, 1)
        _SunSize ("Sun Size", Range(0.001, 0.5)) = 0.03
        _SunHalo ("Sun Halo", Range(0, 4)) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            Name "SKY"

        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 dir : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            half4 _TopColor, _HorizonColor, _BottomColor;
            float _HorizonPower;

            sampler2D _DetailTex;
            half4 _DetailColor;
            float _DetailScale, _DetailStrength;
            float4 _DetailScroll, _DetailStretch;

            float _StarDensity;
            half4 _StarColor;

            float4 _SunDir;
            half4 _SunColor;
            float _SunSize, _SunHalo;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;   // skybox mesh vertices double as view directions
                return o;
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // One layer of the star field. The sphere is cut into cells of a given angular size,
            // each holding at most one star at a hashed position inside it. `threshold` decides how
            // many cells are occupied and `sharp` how tight the point is, so layering a few of these
            // at different scales gives a field with real magnitude variation instead of one uniform
            // spray of identical dots. `rnd` comes back out so the caller can drive twinkle and
            // colour from the same hash rather than paying for another one.
            float starLayer(float2 sph, float scale, float threshold, float sharp, float seed, out float rnd)
            {
                float2 suv = sph * scale + seed;
                float2 cell = floor(suv);
                float2 f = frac(suv);
                rnd = hash21(cell + seed);
                float2 c = float2(hash21(cell + 7.1 + seed), hash21(cell + 3.7 + seed));
                float s = step(threshold, rnd) * saturate(1.0 - length(f - c) * sharp);
                return s * s;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.dir);
                float h = d.y;

                // --- Gradient ---
                float3 col = lerp(_BottomColor.rgb, _TopColor.rgb, smoothstep(-0.35, 0.65, h));
                float horizon = pow(saturate(1.0 - abs(h)), _HorizonPower);
                col = lerp(col, _HorizonColor.rgb, horizon);

                // --- Detail layer (equirect projection of the view direction) ---
                float lon = atan2(d.x, d.z) / (2.0 * UNITY_PI) + 0.5;
                float2 uv = float2(lon, h * 0.5 + 0.5) * _DetailScale * _DetailStretch.xy;
                float n1 = tex2D(_DetailTex, uv + _DetailScroll.xy * _Time.y).r;
                float n2 = tex2D(_DetailTex, uv * 2.3 + _DetailScroll.yx * _Time.y * 1.7).r;
                float detail = smoothstep(0.35, 0.9, n1 * 0.65 + n2 * 0.35);
                col += _DetailColor.rgb * detail * _DetailStrength;

                // --- Stars ---
                // Uniform branch: themes with no stars skip the whole block, so the cost only lands
                // on the ones that want it.
                if (_StarDensity > 0.0)
                {
                    float2 sph = float2(atan2(d.x, d.z), asin(clamp(h, -1.0, 1.0)));

                    float r0, r1, r2;
                    // Coarse: the handful of genuinely bright stars, wide and slow to twinkle.
                    float s0 = starLayer(sph, 26.0,  1.0 - _StarDensity * 0.16,  7.0,  0.0, r0);
                    // Main: the body of the field.
                    float s1 = starLayer(sph, 52.0,  1.0 - _StarDensity * 0.30, 11.0, 17.3, r1);
                    // Fine: dim grains, too small to resolve individually, which is what makes the
                    // field read as depth rather than as a flat scatter of dots.
                    float s2 = starLayer(sph, 104.0, 1.0 - _StarDensity * 0.42, 16.0, 41.7, r2);

                    float tw0 = 0.70 + 0.30 * sin(_Time.y * 1.7 + r0 * 60.0);
                    float tw1 = 0.72 + 0.28 * sin(_Time.y * 2.9 + r1 * 60.0);

                    // Only the bright layer gets a colour cast - warm or cold by hash. Real fields
                    // read as white until a star is bright enough to show its temperature.
                    float3 tint0 = lerp(float3(0.80, 0.90, 1.22), float3(1.14, 0.95, 0.78), frac(r0 * 7.3));

                    // The detail layer doubles as the Milky Way band, so the fine grains thicken
                    // inside it instead of being dimmed by it - the band is made of stars.
                    float3 stars = _StarColor.rgb * (s0 * tw0 * 1.55 * tint0
                                                   + s1 * tw1 * 0.85
                                                   + s2 * (0.40 + detail * 0.85));
                    col += stars;
                }

                // --- Sun / moon ---
                float sd = dot(d, normalize(_SunDir.xyz));
                float disc = smoothstep(1.0 - _SunSize, 1.0 - _SunSize * 0.4, sd);
                // Tie the glow's spread to the disc. With a fixed exponent a sun small enough to read
                // as a distant star still carried the same ~30-degree wash as a near one, which both
                // gave away the distance and flattened everything near it in the sky.
                float haloPow = lerp(70.0, 11.0, saturate(_SunSize / 0.07));
                float halo = pow(saturate(sd), haloPow) * _SunHalo * 0.30;
                col += _SunColor.rgb * (disc * 1.5 + halo);

                return half4(col, 1.0);
            }
        ENDCG
        }
    }

    Fallback Off
}
