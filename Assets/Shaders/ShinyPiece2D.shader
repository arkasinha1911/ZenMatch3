Shader "Custom/ShinyPiece2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _HighLightColor ("Highlight Color", Color) = (1,1,1,0.5)
        _LightDirection ("Fake Light Direction (XY)", Vector) = (-0.5, 0.5, 1, 0)
        _Shininess ("Shininess Power", Range(4, 128)) = 32
        _BevelStrength ("Bevel / 3D Depth", Range(0, 1)) = 0.6
        _SweepSpeed ("Sweep Speed", Range(0, 5)) = 1.0
        _EnableSweep ("Enable Shine Sweep", Float) = 1.0
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
        Blend SrcAlpha OneMinusSrcAlpha // Standard Alpha Blending for Unity Sprites

        Pass
        {
            Name "Forward"
            // We specifically MUST use Universal2D so the URP 2D Renderer draws this pass!
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
            float4 _HighLightColor;
            float4 _LightDirection;
            float _Shininess;
            float _BevelStrength;
            float _SweepSpeed;
            float _EnableSweep;

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

                // 1. Calculate a fake normal based on UV distance from center
                float2 centeredUV = input.uv * 2.0 - 1.0;
                float dist2 = dot(centeredUV, centeredUV);
                
                // Z depth for normal map fake sphere
                float z = sqrt(max(0.001, 1.0 - dist2));
                float3 normal = normalize(float3(centeredUV.x, centeredUV.y, z));
                
                // Flatten normal based on BevelStrength
                normal.z += (1.0 - _BevelStrength) * 3.0;
                normal = normalize(normal);

                // 2. Setup Lighting vectors
                float3 lightDir = normalize(float3(_LightDirection.xy, 1.0));
                float3 viewDir = float3(0, 0, 1);
                
                // Diffuse
                float ndotl = max(0.0, dot(normal, lightDir));
                
                // Specular (Blinn-Phong)
                float3 halfVector = normalize(lightDir + viewDir);
                float spec = pow(max(0.0, dot(normal, halfVector)), _Shininess);
                
                // 3. Animated shine sweep
                float sweep = 0;
                if (_EnableSweep > 0.5) {
                    float sweepMove = frac(_Time.y * _SweepSpeed * 0.3) * 3.0 - 1.0; 
                    float sweepDist = abs((centeredUV.x + centeredUV.y) * 0.707 - sweepMove);
                    sweep = smoothstep(0.15, 0.0, sweepDist) * 0.8;
                }
                
                // Base diffuse color + Specular Highlight + Sweeping Highlight
                half3 finalRGB = col.rgb * (0.6 + 0.4 * ndotl) + (_HighLightColor.rgb * spec) + (_HighLightColor.rgb * sweep);

                return half4(finalRGB, col.a);
            }
            ENDHLSL
        }
    }
}
