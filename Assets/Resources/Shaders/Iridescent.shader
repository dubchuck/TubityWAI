// Mirror and prism surfaces for the Hall of Mirrors world. Real reflections are off the table on
// mobile, so the "mirror" is faked: a dark silvered base, a rainbow sheen that follows the
// viewing angle (thin-film style), and banded light keyed off the reflected direction, all
// drifting slowly so the hall shimmers as you fall. Opaque, fogged like the rest of the scene.
Shader "TubityX/Iridescent"
{
    Properties
    {
        _BaseColor ("Base (silvering)", Color) = (0.05, 0.05, 0.08, 1)
        _Intensity ("Sheen Intensity", Float) = 1.1
        _HueSpeed ("Hue Drift", Float) = 0.03
        _BandFreq ("Reflection Bands", Float) = 16
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "WorldNoise.cginc"

            half4 _BaseColor;
            float _Intensity, _HueSpeed, _BandFreq;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; float3 wn : TEXCOORD1; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.wn = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 V = normalize(_WorldSpaceCameraPos - i.wpos);
                float3 N = normalize(i.wn);
                float ndv = abs(dot(N, V));
                float fres = pow(1.0 - ndv, 2.0);
                float3 R = reflect(-V, N);

                float t = _Time.y;
                float hue = frac(fres * 0.8 + i.wpos.z * 0.015 + R.y * 0.25 + t * _HueSpeed);
                float3 rainbow = wn_hsv2rgb(float3(hue, 0.65, 1.0));

                // Stripes of "reflected" light, as if the hall's own edges were mirrored in it.
                float bands = 0.5 + 0.5 * sin(R.y * _BandFreq + R.x * _BandFreq * 0.5 + t * 0.6);
                bands = pow(bands, 6.0);

                float3 col = _BaseColor.rgb
                           + rainbow * (0.12 + 0.9 * fres) * _Intensity
                           + lerp(rainbow, float3(1, 1, 1), 0.5) * bands * 0.35 * _Intensity * (1.0 - fres);

                col = lerp(unity_FogColor.rgb, col, wn_fogFactor(i.wpos));
                return half4(col, 1);
            }
            ENDCG
        }
    }
}
