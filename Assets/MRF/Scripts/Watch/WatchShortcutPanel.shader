Shader "MRF/WatchShortcutPanel"
{
    Properties
    {
        _PanelColor ("Panel Color", Color) = (0.018,0.025,0.035,0.94)
        [HDR] _BorderColor ("Border Color", Color) = (0.08,1,0.28,1)
        _BorderWidth ("Border Width", Range(0.002,0.08)) = 0.014
        _CornerRadius ("Corner Radius", Range(0.02,0.45)) = 0.12
        _GlowStrength ("Glow Strength", Range(0,3)) = 0.65
        [HideInInspector] _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "RenderPipeline"="UniversalPipeline" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _PanelColor;
            float4 _BorderColor;
            float _BorderWidth;
            float _CornerRadius;
            float _GlowStrength;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 p = abs(input.uv - 0.5) * 2;
                float2 q = p - (1 - _CornerRadius);
                float distanceToEdge = length(max(q, 0)) + min(max(q.x, q.y), 0) - _CornerRadius;
                float aa = max(fwidth(distanceToEdge), 0.0015);
                float inside = 1 - smoothstep(0, aa, distanceToEdge);
                float border = inside * smoothstep(-_BorderWidth - aa, -_BorderWidth + aa, distanceToEdge);
                float glow = inside * exp(-abs(distanceToEdge) * 42) * _GlowStrength;
                float3 rgb = _PanelColor.rgb * _PanelColor.a;
                rgb += _BorderColor.rgb * (border + glow * 0.32);
                float alpha = saturate(_PanelColor.a * inside + _BorderColor.a * border + glow * 0.18);
                return half4(rgb * input.color.rgb, alpha * input.color.a);
            }
            ENDHLSL
        }
    }
}