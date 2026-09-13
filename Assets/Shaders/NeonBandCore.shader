// The dark ball the neon ribbons are wound onto.
//
// Its job is mostly to be opaque: it writes depth, which is what hides the
// bands on the far side and turns a cage of rings into a solid sphere. Visually
// it is near-black with a cool fresnel rim so the silhouette still reads
// against the tunnel behind it.
Shader "TubityX/NeonBandCore"
{
    Properties
    {
        _Colour ("Body Colour", Color) = (0.020, 0.022, 0.055, 1)
        _RimColour ("Rim Colour", Color) = (0.10, 0.35, 0.75, 1)
        _RimPower ("Rim Tightness", Range(0.5, 8)) = 3.2
        _RimStrength ("Rim Strength", Range(0, 3)) = 0.85
    }

    SubShader
    {
        Tags { "Queue" = "Geometry" "RenderType" = "Opaque" }

        Pass
        {
            Name "CORE"
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                float3 normal : TEXCOORD0;
                float3 view   : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Colour, _RimColour;
            float _RimPower, _RimStrength;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.view = normalize(_WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float ndv = saturate(dot(normalize(i.normal), normalize(i.view)));
                float rim = pow(1.0 - ndv, _RimPower) * _RimStrength;
                return half4(_Colour.rgb + _RimColour.rgb * rim, 1.0);
            }
        ENDCG
        }
    }
}
