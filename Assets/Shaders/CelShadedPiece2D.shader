Shader "Custom/CelShadedPiece2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0, 5)) = 1.0
        
        // Posterization limits the smooth gradients to make it look "comic-book" flat
        _ColorSteps ("Color Steps (Posterization)", Range(2, 10)) = 4.0
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
            float4 _MainTex_TexelSize; // Unity automatically sets this (e.g. 1/width, 1/height)
            float4 _MainTex_ST;
            
            float4 _Color;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _ColorSteps;

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
                half4 col = tex2D(_MainTex, input.uv);
                
                // --- OUTLINE GENERATION ---
                // We check the transparency (alpha) of the pixel we are currently drawing.
                // If the pixel is transparent, we look at the pixels next to it!
                float alpha = col.a;
                
                // Calculate physical pixel width in UV space
                float2 offset = _MainTex_TexelSize.xy * _OutlineWidth;
                
                // Sample exactly North, South, East, West of this pixel
                float alphaUp    = tex2D(_MainTex, input.uv + float2(0, offset.y)).a;
                float alphaDown  = tex2D(_MainTex, input.uv - float2(0, offset.y)).a;
                float alphaRight = tex2D(_MainTex, input.uv + float2(offset.x, 0)).a;
                float alphaLeft  = tex2D(_MainTex, input.uv - float2(offset.x, 0)).a;
                
                // Are we physically outside the shape, but very close to the edge of the shape?
                // If yes, we are the Outline!
                float outlineAlpha = max(max(alphaUp, alphaDown), max(alphaRight, alphaLeft));
                
                if (alpha < 0.01 && outlineAlpha > 0.01)
                {
                    // Draw solid outline ink!
                    return half4(_OutlineColor.rgb, _OutlineColor.a * outlineAlpha * input.color.a);
                }
                
                // Discard empty space
                if (alpha < 0.01) discard;

                // Apply Sprite Tint
                col *= input.color;

                // --- POSTERIZATION (Comic-Book Flatness) ---
                // Math trick: We multiply the color up, round the decimal part away, and divide it back down.
                // This violently flattens smooth gradients into hard chunks!
                half3 posterizedRGB = round(col.rgb * _ColorSteps) / _ColorSteps;

                return half4(posterizedRGB, col.a);
            }
            ENDHLSL
        }
    }
}
