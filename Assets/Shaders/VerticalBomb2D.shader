Shader "Custom/VerticalBomb2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _GlowColor ("Glow Color", Color) = (0.1, 0.4, 1.0, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 3.0

        _StreakSpeed ("Streak Speed", Range(0, 10)) = 3.0
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 3.0
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 0.6
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
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _GlowColor;
            float _GlowIntensity;
            float _StreakSpeed;
            float _PulseSpeed;
            float _PulseStrength;

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _Color;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = tex2D(_MainTex, input.uv) * input.color;
                if (col.a < 0.01) discard;

                float2 uv = input.uv;
                float2 centered = uv * 2.0 - 1.0;
                float time = _Time.y;

                // --- BLUE TINT OVERLAY ---
                // Blend the entire piece strongly toward the glow color
                half3 tinted = lerp(col.rgb, _GlowColor.rgb, 0.55);

                // --- VERTICAL SWEEP LINE ---
                // A bright line that sweeps bottom-to-top continuously
                float sweepY = frac(time * _StreakSpeed * 0.25) * 2.4 - 1.2;
                float sweepGlow = exp(-abs(centered.y - sweepY) * 5.0);
                // The sweep line is brighter near the vertical center
                sweepGlow *= (1.0 - abs(centered.x) * 0.5);

                // --- SECOND SWEEP (opposite direction for ping-pong feel) ---
                float sweep2Y = -(frac(time * _StreakSpeed * 0.18 + 0.5) * 2.4 - 1.2);
                float sweep2Glow = exp(-abs(centered.y - sweep2Y) * 4.0) * 0.6;
                sweep2Glow *= (1.0 - abs(centered.x) * 0.5);

                // --- VERTICAL BAND GLOW ---
                // A persistent soft vertical stripe through the middle of the piece
                float bandGlow = exp(-centered.x * centered.x * 6.0) * 0.5;

                // --- EDGE HIGHLIGHTS on top/bottom ---
                float topEdge = exp(-(centered.y - 1.0) * (centered.y - 1.0) * 8.0) * 0.4;
                float bottomEdge = exp(-(centered.y + 1.0) * (centered.y + 1.0) * 8.0) * 0.4;
                float edgeGlow = topEdge + bottomEdge;

                // --- GLOBAL PULSE ---
                float pulse = 1.0 + _PulseStrength * sin(time * _PulseSpeed);

                // Combine all effects
                float totalGlow = (sweepGlow + sweep2Glow + bandGlow + edgeGlow) * _GlowIntensity * pulse;
                
                half3 finalRGB = tinted * (1.0 + totalGlow * 0.5) + _GlowColor.rgb * totalGlow;

                return half4(finalRGB, col.a);
            }
            ENDHLSL
        }
    }
}
