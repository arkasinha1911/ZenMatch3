Shader "Custom/URP_ToonShader"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        
        [Header(Cel Shading Settings)]
        _ColorBands ("Lighting Bands", Range(1, 10)) = 3
        _ShadowColor ("Shadow Color", Color) = (0.3, 0.3, 0.4, 1)
    }
    
    SubShader
    {
        // These tags tell URP how and when to render this material
        Tags { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // URP specific includes for lighting and rendering logic
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // Data passed from the mesh into the vertex shader
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            // Data passed from the vertex shader into the fragment shader
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            // Declare our properties
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _ColorBands;
                float4 _ShadowColor;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                // Convert position to clip space for rendering
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                // Convert normals to world space for lighting calculations
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 1. Sample the texture and base color
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half3 albedo = texColor.rgb * _BaseColor.rgb;

                // 2. Get the URP Main Light (usually your Directional Light)
                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                half3 normal = normalize(IN.normalWS);

                // 3. Calculate basic lighting (Dot product of normal and light direction)
                // This gives a smooth value between 0 (shadow) and 1 (fully lit)
                half NdotL = saturate(dot(normal, lightDir));

                // 4. Apply the Cel-Shading Math (Stepping the light into bands)
                // We multiply by the bands, round down, and divide back to get stepped increments
                half toonIntensity = floor(NdotL * _ColorBands) / _ColorBands;

                // 5. Mix the lit color and the shadow color based on the toon intensity
                half3 finalColor = lerp(_ShadowColor.rgb, albedo * mainLight.color, toonIntensity);

                return half4(finalColor, texColor.a);
            }
            ENDHLSL
        }
    }
    
    // Fallback if the shader fails
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}