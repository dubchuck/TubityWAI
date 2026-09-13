// Additive billboard for the skins' sparks, embers and wisps, and for the
// Void Lattice arcs (LineRenderer). A soft disc shaped from the quad UV,
// tinted by the particle's vertex colour, so one material serves every skin.
Shader "TubityX/NeonParticle"
{
    Properties
    {
        _Intensity ("Intensity", Range(0, 6)) = 1.5
        _Softness ("Edge Softness", Range(0.5, 4)) = 1.6
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+20" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }

        Pass
        {
            Name "PARTICLE"
            Cull Off
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
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                fixed4 colour : COLOR;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _Intensity, _Softness;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.colour = v.color;
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // a soft disc on a billboard; a LineRenderer stretches U along
                // its length, so its V alone shapes the strip the same way
                float2 d = i.uv - 0.5;
                float r = length(d) * 2.0;
                float glow = pow(saturate(1.0 - r), _Softness);
                return half4(i.colour.rgb * glow * _Intensity * i.colour.a, 1.0);
            }
        ENDCG
        }
    }
}
