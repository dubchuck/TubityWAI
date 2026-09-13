// Neon ribbon bands wound around the attract-mode hero sphere.
//
// Unlit and additive: there is no lighting model here, only emission, which is
// what lets URP's bloom do the heavy lifting. The ribbon is three vertices
// across and the cross-section profile - hot core, soft shoulders - is shaped
// from UV.y, so the geometry stays cheap.
//
// _MirrorPlaneY / _MirrorFade turn this same shader into the reflection: see
// UI/NeonBandMirror below, which shares the code and adds the stencil test.

Shader "TubityX/NeonBand"
{
    Properties
    {
        _Intensity ("Intensity", Range(0, 6)) = 1.6
        _CoreWhite ("Core Whiteness", Range(0, 1)) = 0.45
        _Profile ("Edge Falloff", Range(0.5, 6)) = 2.2
        _Fresnel ("Grazing Boost", Range(0, 2)) = 0.55
        _HueShift ("Hue Shift (radians)", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "BAND"
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend One One            // additive; the dark core still occludes via depth

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

            float _Intensity, _CoreWhite, _Profile, _Fresnel, _HueShift;

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
                // the ribbon lies on the sphere, so the vertex direction is the normal
                o.normal = UnityObjectToWorldNormal(normalize(v.vertex.xyz));
                o.view = normalize(_WorldSpaceCameraPos - world);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // cross-section: 1 at the ribbon centre, 0 at its edges
                float across = abs(i.uv.y * 2.0 - 1.0);
                float profile = pow(saturate(1.0 - across), _Profile);

                // ribbons seen edge-on catch a little extra, like a real tube
                float ndv = saturate(dot(normalize(i.normal), normalize(i.view)));
                float grazing = 1.0 + _Fresnel * (1.0 - ndv);

                half3 c = _HueShift != 0.0 ? max(HueRotate(i.colour.rgb, _HueShift), 0.0) : i.colour.rgb;
                half3 hot = lerp(c, half3(1, 1, 1), _CoreWhite * profile * profile);
                return half4(hot * profile * _Intensity * grazing, 1.0);
            }
        ENDCG
        }
    }
}
