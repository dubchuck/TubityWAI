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

            fixed4 _TopColor, _HorizonColor, _BottomColor;
            float _HorizonPower;

            sampler2D _DetailTex;
            fixed4 _DetailColor;
            float _DetailScale, _DetailStrength;
            float4 _DetailScroll, _DetailStretch;

            float _StarDensity;
            fixed4 _StarColor;

            float4 _SunDir;
            fixed4 _SunColor;
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

            fixed4 frag(v2f i) : SV_Target
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
                if (_StarDensity > 0.0)
                {
                    float2 suv = float2(atan2(d.x, d.z), asin(clamp(h, -1.0, 1.0))) * 45.0;
                    float2 cell = floor(suv);
                    float2 f = frac(suv);
                    float r = hash21(cell);
                    float2 c = float2(hash21(cell + 7.1), hash21(cell + 3.7));
                    float dist = length(f - c);
                    float star = step(1.0 - _StarDensity * 0.3, r) * saturate(1.0 - dist * 10.0);
                    star *= star;
                    float twinkle = 0.65 + 0.35 * sin(_Time.y * 2.5 + r * 60.0);
                    col += _StarColor.rgb * star * twinkle * (1.0 - detail * 0.6);
                }

                // --- Sun / moon ---
                float sd = dot(d, normalize(_SunDir.xyz));
                float disc = smoothstep(1.0 - _SunSize, 1.0 - _SunSize * 0.4, sd);
                float halo = pow(saturate(sd), 10.0) * _SunHalo * 0.35;
                col += _SunColor.rgb * (disc * 1.5 + halo);

                return fixed4(col, 1.0);
            }
        ENDCG
        }
    }

    Fallback Off
}
