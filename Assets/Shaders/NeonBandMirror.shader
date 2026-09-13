// The reflection of the hero sphere in the platform.
//
// This is the mirrored-geometry trick: the same ribbon mesh is drawn a second
// time through a matrix that flips it about the platform plane. No reflection
// camera, no render texture, no extra scene pass - one more draw of a mesh that
// is already in memory.
//
// Two things keep it honest:
//   * the stencil test bounds it to pixels the platform actually covered, so the
//     reflection cannot spill onto the road or the tunnel;
//   * ZTest Always, because the mirrored geometry is physically below an opaque
//     floor and would otherwise fail the depth test outright.
// _MirrorPlaneY drives the fade with distance from the surface.

Shader "TubityX/NeonBandMirror"
{
    Properties
    {
        _Intensity ("Intensity", Range(0, 6)) = 0.75
        _CoreWhite ("Core Whiteness", Range(0, 1)) = 0.30
        _Profile ("Edge Falloff", Range(0.5, 6)) = 2.2
        _MirrorPlaneY ("Mirror Plane Y (world)", Float) = 0
        _MirrorFade ("Fade Distance", Range(0.1, 12)) = 2.8
        _FadePower ("Fade Curve", Range(0.5, 4)) = 1.3
        _StencilRef ("Stencil Ref", Float) = 1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent+10" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

        Pass
        {
            Name "BAND_MIRROR"
            Stencil
            {
                Ref [_StencilRef]
                Comp Equal
                Pass Keep
            }
            Cull Off                 // the mirror matrix flips winding
            ZWrite Off
            ZTest Always
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
                float  depth  : TEXCOORD1;   // how far below the mirror plane
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _Intensity, _CoreWhite, _Profile;
            float _MirrorPlaneY, _MirrorFade, _FadePower;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.colour = v.color;
                o.uv = v.uv;
                float worldY = mul(unity_ObjectToWorld, v.vertex).y;
                o.depth = saturate((_MirrorPlaneY - worldY) / _MirrorFade);
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float across = abs(i.uv.y * 2.0 - 1.0);
                float profile = pow(saturate(1.0 - across), _Profile);
                float fade = pow(saturate(1.0 - i.depth), _FadePower);

                half3 c = i.colour.rgb;
                half3 hot = lerp(c, half3(1, 1, 1), _CoreWhite * profile * profile);
                return half4(hot * profile * _Intensity * fade, 1.0);
            }
        ENDCG
        }
    }
}
