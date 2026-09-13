// A glowing transparent shell around a skinned sphere - the Solar Flare corona,
// the Ghost's glass body, and the little orbiting motes of Orbitals.
//
// Additive and unlit like the ribbons. _CentreWeight picks the look: 0 lights
// only the limb (a fresnel glass rim), 1 lights the middle and fades to the
// limb (a soft ball of light). Both come from the same view angle term.
Shader "TubityX/NeonShell"
{
    Properties
    {
        _Colour ("Colour", Color) = (0.2, 0.7, 1, 1)
        _Intensity ("Intensity", Range(0, 6)) = 1
        _RimPower ("Rim Tightness", Range(0.5, 8)) = 2.5
        _CentrePower ("Centre Tightness", Range(0.5, 8)) = 1.5
        _CentreWeight ("Centre vs Rim", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "SHELL"
            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend One One

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

            fixed4 _Colour;
            float _Intensity, _RimPower, _CentrePower, _CentreWeight;

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
                float rim = pow(1.0 - ndv, _RimPower);
                float centre = pow(ndv, _CentrePower);
                float glow = lerp(rim, centre, _CentreWeight);
                return half4(_Colour.rgb * glow * _Intensity, 1.0);
            }
        ENDCG
        }
    }
}
