Shader "UI/NeonGradientSheen"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Gradient Settings)]
        _LeftColor ("Left Color (Blue)", Color) = (0, 0.5, 1, 1)
        _RightColor ("Right Color (Purple)", Color) = (0.8, 0, 1, 1)
        
        [Header(Sheen Settings)]
        _SheenColor ("Sheen Color", Color) = (1, 1, 1, 1)
        _SheenWidth ("Sheen Width", Range(0, 1)) = 0.1
        _SheenSoftness ("Sheen Softness", Range(0.001, 1)) = 0.05
        _SheenAngle ("Sheen Angle", Range(-2, 2)) = -0.5
        _SheenSpeed ("Sheen Speed", Range(0, 5)) = 0.5
        _SheenInterval ("Sheen Interval (Delay)", Range(0, 5)) = 2.0
        
        [Header(UI Masks)]
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            
            fixed4 _LeftColor;
            fixed4 _RightColor;
            
            fixed4 _SheenColor;
            float _SheenWidth;
            float _SheenSoftness;
            float _SheenAngle;
            float _SheenSpeed;
            float _SheenInterval;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = v.texcoord;
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // Base texture color
                half4 color = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // Horizontal Gradient Blend (from Left to Right based on X UV)
                half4 gradientColor = lerp(_LeftColor, _RightColor, IN.texcoord.x);
                
                // Colorize the sprite
                color *= gradientColor;

                // Sheen Calculation
                float timeCycle = _Time.y * _SheenSpeed;
                float cycleLength = 1.0 + _SheenInterval;
                float currentCycle = fmod(timeCycle, cycleLength);
                
                // Only draw sheen if within the active part of the cycle
                if (currentCycle <= 1.0)
                {
                    // Calculate UV position modified by angle
                    float uvPos = IN.texcoord.x + (IN.texcoord.y - 0.5) * _SheenAngle;
                    
                    // Remap cycle from 0->1 to start outside the left edge and end outside right edge
                    float sheenPos = lerp(-0.5, 1.5, currentCycle);
                    
                    // Distance from current sheen center
                    float dist = abs(uvPos - sheenPos);
                    
                    // Smoothstep for soft edges
                    float sheen = 1.0 - smoothstep(_SheenWidth, _SheenWidth + _SheenSoftness, dist);
                    
                    // Add sheen on top, multiplied by alpha to only affect opaque parts of the sprite
                    color.rgb += _SheenColor.rgb * sheen * color.a * _SheenColor.a;
                }

                return color;
            }
            ENDCG
        }
    }
}
