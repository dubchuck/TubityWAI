// The glow around each neon ribbon on the attract-mode hero sphere.
//
// A real neon tube reads as a thin white-hot line inside a wide, soft cloud of
// its own colour. The ribbon (TubityX/NeonBand) is the line; this is the cloud.
// It is a second, much wider ribbon under each band - the halo submesh built by
// NeonBandSphere - with a gaussian cross-section so it fades to nothing with no
// visible edge, tinted purely by the band's vertex colour so the hue never
// drifts. It also gives URP bloom something wide enough to pick up: a two-pixel
// line loses almost all its energy in the bloom downsample, a twenty-pixel
// halo does not.
Shader "TubityX/NeonBandHalo"
{
    Properties
    {
        _Intensity ("Intensity", Range(0, 3)) = 0.55
        _Softness ("Softness", Range(1, 4)) = 2.1
        _Fresnel ("Grazing Boost", Range(0, 3)) = 1.2
        _HueShift ("Hue Shift (radians)", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "HALO"
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend One One            // additive, hidden on the far side by the opaque core

        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                fixed4 colour : COLOR;
                float2 uv     : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 view   : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _Intensity, _Softness, _Fresnel, _HueShift;

            // rotate the colour about the grey axis - the Spectrum skin cycles
            // its hues with this instead of rebuilding the mesh every frame
            half3 HueRotate(half3 c, float a)
            {
                half3 k = half3(0.57735, 0.57735, 0.57735);
                float ca = cos(a), sa = sin(a);
                return c * ca + cross(k, c) * sa + k * dot(k, c) * (1.0 - ca);
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.colour = v.color;
                o.uv = v.uv;
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(normalize(v.vertex.xyz));
                o.view = normalize(_WorldSpaceCameraPos - world);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // gaussian across the ribbon: 1 at the centre, ~0 at the edges
                float across = abs(i.uv.y * 2.0 - 1.0);
                float x = across * _Softness;
                float glow = exp(-x * x);

                // the halo lies flat on the sphere, so at the limb it is seen
                // edge-on and would vanish - boost it there so the silhouette
                // glows the way the reference art does
                float ndv = saturate(dot(normalize(i.normal), normalize(i.view)));
                float grazing = 1.0 + _Fresnel * (1.0 - ndv) * (1.0 - ndv);

                half3 c = _HueShift != 0.0 ? HueRotate(i.colour.rgb, _HueShift) : i.colour.rgb;
                return half4(c * glow * _Intensity * grazing, 1.0);
            }
        ENDCG
        }
    }
}
