Shader "Custom/ColorBomb2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _RainbowSpeed ("Rainbow Cycle Speed", Range(0, 5)) = 1.0
        _RainbowIntensity ("Rainbow Intensity", Range(0, 2)) = 0.6
        _RainbowSaturation ("Rainbow Saturation", Range(0, 1)) = 0.7

        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 3.0
        _PulseStrength ("Pulse Strength", Range(0, 1)) = 0.4

        _RadialSpeed ("Radial Wave Speed", Range(0, 10)) = 2.0
        _RadialCount ("Radial Wave Count", Range(1, 8)) = 3.0
        _RadialIntensity ("Radial Wave Intensity", Range(0, 2)) = 0.8

        _SparkleSpeed ("Sparkle Speed", Range(0, 20)) = 8.0
        _SparkleIntensity ("Sparkle Intensity", Range(0, 2)) = 1.0
        _SparkleDensity ("Sparkle Density", Range(1, 30)) = 12.0

        _GlowRadius ("Center Glow Radius", Range(0, 2)) = 0.8
        _GlowIntensity ("Center Glow Intensity", Range(0, 3)) = 1.2
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

            float _RainbowSpeed;
            float _RainbowIntensity;
            float _RainbowSaturation;
            float _PulseSpeed;
            float _PulseStrength;
            float _RadialSpeed;
            float _RadialCount;
            float _RadialIntensity;
            float _SparkleSpeed;
            float _SparkleIntensity;
            float _SparkleDensity;
            float _GlowRadius;
            float _GlowIntensity;

            // HSV to RGB conversion
            half3 HSVtoRGB(float h, float s, float v)
            {
                h = frac(h) * 6.0;
                float c = v * s;
                float x = c * (1.0 - abs(fmod(h, 2.0) - 1.0));
                float m = v - c;

                half3 rgb;
                if (h < 1.0)      rgb = half3(c, x, 0);
                else if (h < 2.0) rgb = half3(x, c, 0);
                else if (h < 3.0) rgb = half3(0, c, x);
                else if (h < 4.0) rgb = half3(0, x, c);
                else if (h < 5.0) rgb = half3(x, 0, c);
                else              rgb = half3(c, 0, x);

                return rgb + m;
            }

            // Simple pseudo-random hash for sparkle grid
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

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

                float2 centered = input.uv * 2.0 - 1.0;
                float dist = length(centered);
                float angle = atan2(centered.y, centered.x);
                float time = _Time.y;

                // --- RAINBOW COLOR OVERLAY ---
                // Hue shifts based on radial distance + angle + time for a swirling rainbow
                float hue = frac(time * _RainbowSpeed * 0.3 + dist * 0.5 + angle * 0.159);
                half3 rainbow = HSVtoRGB(hue, _RainbowSaturation, 1.0);
                
                // Blend rainbow onto base color
                half3 rainbowBlend = lerp(col.rgb, rainbow, _RainbowIntensity);

                // --- RADIAL PULSE WAVES ---
                // Concentric rings expanding outward from center
                float radialWave = sin((dist * _RadialCount * 6.283) - time * _RadialSpeed) * 0.5 + 0.5;
                radialWave = pow(radialWave, 3.0); // Sharpen the rings
                float radialEffect = radialWave * _RadialIntensity * (1.0 - dist);

                // --- SPARKLE EFFECT ---
                // Grid-based sparkles that twinkle at random intervals
                float2 sparkleUV = floor(input.uv * _SparkleDensity);
                float sparkleRand = hash21(sparkleUV);
                float sparkleTime = sin(time * _SparkleSpeed + sparkleRand * 6.283);
                float sparkle = step(0.92, sparkleRand) * max(0, sparkleTime) * _SparkleIntensity;

                // --- CENTER GLOW ---
                float centerGlow = exp(-dist * dist / (_GlowRadius * 0.3)) * _GlowIntensity;
                // Animate center glow color through the rainbow
                half3 glowColor = HSVtoRGB(frac(time * _RainbowSpeed * 0.5), 0.4, 1.0);

                // --- GLOBAL PULSE ---
                float pulse = 1.0 + _PulseStrength * sin(time * _PulseSpeed);

                // --- STAR BURST (subtle rotating rays emanating from center) ---
                float rays = 0;
                {
                    float rotAngle = angle + time * 0.5;
                    rays = pow(max(0, cos(rotAngle * 4.0)), 8.0) * 0.3 * (1.0 - dist);
                }

                // Composite all effects together
                half3 finalRGB = rainbowBlend * pulse;
                finalRGB += glowColor * centerGlow;
                finalRGB += radialEffect * rainbow;
                finalRGB += sparkle;
                finalRGB += rays * rainbow;

                return half4(finalRGB, col.a);
            }
            ENDHLSL
        }
    }
}
