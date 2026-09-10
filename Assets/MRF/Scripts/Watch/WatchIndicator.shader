Shader "MRF/WatchIndicator"
{
    Properties
    {
        [HDR] _Color ("LED Color", Color) = (0,0.65,1,1)
        _Intensity ("Emission", Float) = 1.8
        _Mode ("Ring / Screen / Tile", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            float _Intensity;
            float _Mode;
            CBUFFER_END
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 p = (input.uv - 0.5) * 2;
                if (_Mode > 1.5)
                {
                    float edge = smoothstep(0.75, 1.0, max(abs(p.x), abs(p.y)));
                    return half4(_Color.rgb * _Intensity * (0.75 + edge * 0.25), 1);
                }
                if (_Mode > 0.5)
                {
                    float radius = length(p);
                    float mask = 1 - smoothstep(0.94, 1.0, radius);
                    float innerLight = 0.24 + 0.4 * exp(-radius * radius * 2.5);
                    float rim = exp(-pow((radius - 0.9) * 25, 2)) * 0.45;
                    return half4(_Color.rgb * _Intensity, mask * (innerLight + rim));
                }
                float ring = abs(length(p) - 0.68);
                float aa = max(fwidth(ring), 0.006);
                float ringMask = 1 - smoothstep(0.022, 0.022 + aa, ring);
                float2 tileDistance = abs(abs(p) - float2(0.22, 0.22));
                float tileBox = max(tileDistance.x, tileDistance.y);
                float tilesMask = 1 - smoothstep(0.105, 0.105 + aa, tileBox);
                float glow = exp(-ring * ring / 0.008) * 0.32;
                glow += exp(-max(tileBox - 0.10, 0) * 30) * 0.16;
                float alpha = saturate(max(ringMask, tilesMask) + glow);
                return half4(_Color.rgb * _Intensity, alpha * _Color.a);
            }
            ENDHLSL
        }
    }
}
