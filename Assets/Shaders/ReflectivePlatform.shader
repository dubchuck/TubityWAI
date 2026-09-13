// The polished pad the hero sphere hovers over.
//
// It is the mirror, so the important thing it does is write the stencil: every
// pixel it covers gets ref 1, and TubityX/NeonBandMirror only draws where that
// matches. That is what bounds the reflection to the pad instead of letting it
// spill across the road.
//
// The surface itself is dark anisotropic metal with a rounded-rect neon edge -
// the same distance-field trick the menu buttons use, in world units here.

Shader "TubityX/ReflectivePlatform"
{
    Properties
    {
        _Colour ("Surface Colour", Color) = (0.012, 0.016, 0.038, 1)
        _SheenColour ("Sheen Colour", Color) = (0.18, 0.30, 0.62, 1)
        _SheenStrength ("Sheen Strength", Range(0, 2)) = 0.55
        _Fresnel ("Grazing Sheen", Range(0, 4)) = 1.8

        [Header(Edge)]
        _EdgeColour ("Edge Colour", Color) = (0.25, 0.85, 1.6, 1)
        _EdgeWidth ("Edge Width (uv)", Range(0.001, 0.2)) = 0.018
        _EdgeSoft ("Edge Softness (uv)", Range(0.001, 0.2)) = 0.012
        _EdgeStrength ("Edge Strength", Range(0, 6)) = 2.2
        _CornerRadius ("Corner Radius (uv)", Range(0, 0.5)) = 0.14

        _StencilRef ("Stencil Ref", Float) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Geometry+10" "RenderType" = "Opaque" }

        Pass
        {
            Name "PLATFORM"
            // Stamp the stencil so the reflection knows exactly where the pad is.
            Stencil
            {
                Ref [_StencilRef]
                Comp Always
                Pass Replace
            }
            Cull Back
            ZWrite On
            ZTest LEqual

        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float3 view   : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Colour, _SheenColour, _EdgeColour;
            float _SheenStrength, _Fresnel;
            float _EdgeWidth, _EdgeSoft, _EdgeStrength, _CornerRadius;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.view = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // rounded-rect distance field over the pad's UV
                float2 p = abs(i.uv - 0.5) - (0.5 - _CornerRadius);
                float d = length(max(p, 0.0)) + min(max(p.x, p.y), 0.0) - _CornerRadius;

                float ndv = saturate(dot(normalize(i.normal), normalize(i.view)));
                float grazing = pow(1.0 - ndv, _Fresnel);

                half3 col = _Colour.rgb + _SheenColour.rgb * grazing * _SheenStrength;

                float edge = 1.0 - smoothstep(_EdgeWidth, _EdgeWidth + _EdgeSoft, abs(d));
                col += _EdgeColour.rgb * edge * _EdgeStrength;

                return half4(col, 1.0);
            }
        ENDCG
        }
    }
}
