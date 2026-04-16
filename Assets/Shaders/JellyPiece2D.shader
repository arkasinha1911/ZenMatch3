Shader "Custom/JellyPiece2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _WobbleSpeed ("Wobble Speed", Range(0, 20)) = 5.0
        _WobbleStrength ("Wobble Strength", Range(0, 0.5)) = 0.05
        _WobbleDensity ("Wobble Density", Range(0, 20)) = 5.0
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
            
            float _WobbleSpeed;
            float _WobbleStrength;
            float _WobbleDensity;

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                // Calculate wobble based on Game Time
                float time = _Time.y * _WobbleSpeed;
                
                // Displace the vertex horizontally based on its vertical position (and vice versa)
                // This creates a squishy stretching effect across the bounds of the sprite
                float2 offset;
                offset.x = sin(input.positionOS.y * _WobbleDensity + time) * _WobbleStrength;
                offset.y = cos(input.positionOS.x * _WobbleDensity + time) * _WobbleStrength;

                // Apply displacement
                float3 displacedPos = input.positionOS.xyz + float3(offset.x, offset.y, 0);

                // Convert from local 3D space to the 2D Viewport Screen space
                output.positionCS = TransformObjectToHClip(displacedPos);
                
                output.color = input.color * _Color;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Grab the exact pixel color from the Sprite graphic
                half4 col = tex2D(_MainTex, input.uv) * input.color;
                
                // Instantly discard pure transparent pixels for memory optimization
                if (col.a < 0.01) discard;

                return col;
            }
            ENDHLSL
        }
    }
}
