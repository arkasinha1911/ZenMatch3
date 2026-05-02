Shader "Custom/Sprite3DLook"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(3D Effect Settings)]
        _BevelDepth ("Bevel Depth", Range(0, 10)) = 3.0
        _BevelWidth ("Bevel Width (Pixels)", Range(1, 5)) = 1.0
        
        [Header(Lighting)]
        _LightAngle ("Light Angle", Range(0, 360)) = 135.0
        _LightElevation ("Light Elevation", Range(0, 90)) = 45.0
        _Ambient ("Ambient Intensity", Range(0, 1)) = 0.5
        _Diffuse ("Diffuse Intensity", Range(0, 2)) = 1.0
        _Specular ("Specular Highlights", Range(0, 3)) = 1.5
        _Shininess ("Shininess", Range(1, 100)) = 30.0
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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;

            float _BevelDepth;
            float _BevelWidth;
            
            float _LightAngle;
            float _LightElevation;
            float _Ambient;
            float _Diffuse;
            float _Specular;
            float _Shininess;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            // Simple Sobel filter to extract normals from the alpha channel or luma
            float3 CalculateNormal(float2 uv)
            {
                float2 texel = _MainTex_TexelSize.xy * _BevelWidth;
                
                // We use alpha channel to determine shape edges
                // If the sprite is fully opaque rectangle, this will only find edges at the very border
                float tl = tex2D(_MainTex, uv + float2(-texel.x,  texel.y)).a;
                float l  = tex2D(_MainTex, uv + float2(-texel.x,        0)).a;
                float bl = tex2D(_MainTex, uv + float2(-texel.x, -texel.y)).a;
                float t  = tex2D(_MainTex, uv + float2(       0,  texel.y)).a;
                float b  = tex2D(_MainTex, uv + float2(       0, -texel.y)).a;
                float tr = tex2D(_MainTex, uv + float2( texel.x,  texel.y)).a;
                float r  = tex2D(_MainTex, uv + float2( texel.x,        0)).a;
                float br = tex2D(_MainTex, uv + float2( texel.x, -texel.y)).a;

                // Sobel operator
                float dX = tr + 2.0 * r + br - (tl + 2.0 * l + bl);
                float dY = tl + 2.0 * t + tr - (bl + 2.0 * b + br);
                
                // Construct normal
                float3 normal = normalize(float3(-dX * _BevelDepth, -dY * _BevelDepth, 1.0));
                return normal;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
                
                // Skip fully transparent pixels
                if (c.a <= 0.01) return c;

                // 1. Calculate Fake Normal from Alpha
                float3 normal = CalculateNormal(IN.texcoord);

                // 2. Calculate Light Direction
                float angleRad = _LightAngle * UNITY_PI / 180.0;
                float elevRad = _LightElevation * UNITY_PI / 180.0;
                
                float3 lightDir = normalize(float3(
                    cos(angleRad) * cos(elevRad),
                    sin(angleRad) * cos(elevRad),
                    sin(elevRad)
                ));

                // 3. Diffuse Lighting
                float nDotL = dot(normal, lightDir);
                float diffuse = max(0.0, nDotL) * _Diffuse;

                // 4. Specular Lighting (Blinn-Phong)
                float3 viewDir = float3(0, 0, 1);
                float3 halfDir = normalize(lightDir + viewDir);
                float nDotH = max(0.0, dot(normal, halfDir));
                
                // Add specular mainly on the edges where normal faces the half-vector
                float specular = pow(nDotH, _Shininess) * _Specular;

                // 5. Combine Lighting
                float lightIntensity = _Ambient + diffuse;
                
                // Apply lighting to RGB, keep Alpha intact
                fixed4 finalColor;
                finalColor.rgb = c.rgb * lightIntensity + (specular * c.a);
                finalColor.a = c.a;
                
                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}
