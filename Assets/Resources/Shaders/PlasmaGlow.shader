// Additive glow for solid meshes: solar prominence loops, light beams, lightning bolts, lava drips.
// Brightest where the surface turns edge-on (a glowing tube reads as a tube, not a flat ribbon),
// with a flow pattern running along uv.x and a flicker. It fades into the fog rather than
// greying out, and _Fade lets a blended environment dissolve it.
Shader "TubityX/PlasmaGlow"
{
    Properties
    {
        _Color ("Colour", Color) = (1, 0.6, 0.2, 1)
        _Intensity ("Intensity", Float) = 2.5
        _RimPower ("Edge Power", Float) = 1.5
        _Flow ("Flow Speed", Float) = 2
        _Flicker ("Flicker Speed", Float) = 3
        _Fade ("Fade", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" }
        Blend One One
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "WorldNoise.cginc"

            half4 _Color;
            float _Intensity, _RimPower, _Flow, _Flicker, _Fade;

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float3 wpos : TEXCOORD0; float3 wn : TEXCOORD1; float2 uv : TEXCOORD2; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.wpos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.wn = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float3 V = normalize(_WorldSpaceCameraPos - i.wpos);
                float ndv = abs(dot(normalize(i.wn), V));
                float edge = pow(1.0 - ndv, _RimPower);
                float core = ndv * ndv;

                float t = _Time.y;
                float flow = wn_noise3(float3(i.uv.x * 12.0 - t * _Flow, i.uv.y * 3.0, t * 0.3));
                float flicker = 0.8 + 0.2 * sin(t * _Flicker + i.wpos.x * 0.3 + i.wpos.y * 0.2);

                float glow = (edge * 1.1 + core * 0.55) * (0.6 + 0.8 * flow) * flicker;
                float3 col = _Color.rgb * glow * _Intensity * _Fade * wn_fogFactor(i.wpos);
                return half4(col, 1);
            }
            ENDCG
        }
    }
}
