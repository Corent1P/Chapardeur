Shader "Custom/RevealLight"
{
    Properties
    {
        _BaseColor("Color", Color) = (1,1,1,1)
        _BaseMap("Albedo", 2D) = "white" {}
        _Smoothness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        
        _LightPos("Light Position", Vector) = (0,0,0,0)
        _LightDir("Light Direction", Vector) = (0,0,1,0)
        _LightAngle("Light Angle", Float) = 45
        _RevealPower("Reveal Power", Float) = 5
        _RevealSoftness("Reveal Softness", Float) = 0.2
        _DistanceAttenuation("Distance Attenuation", Float) = 1.0
        _LightEnabled("Light Enabled", Float) = 1.0
    }
    
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "UnityStandardUtils.cginc"
            
            sampler2D _BaseMap;
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float _Smoothness;
            float _Metallic;
            
            float3 _LightPos;
            float3 _LightDir;
            float _LightAngle;
            float _RevealPower;
            float _RevealSoftness;
            float _DistanceAttenuation;
            float _LightEnabled;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                // Si la lumière n'est pas activée, retourner transparent
                if (_LightEnabled < 0.5)
                {
                    return float4(0, 0, 0, 0);
                }
                
                // Texture
                float4 texColor = tex2D(_BaseMap, i.uv);
                float4 finalColor = texColor * _BaseColor;
                
                // === CALCUL DE LA RÉVÉLATION ===
                
                // 1. Direction vers la lumière et direction du spotlight
                float3 toLight = normalize(_LightPos - i.worldPos);
                float3 lightDir = normalize(_LightDir);
                
                // 2. Produit scalaire pour vérifier l'alignement
                float spotFalloff = dot(toLight, lightDir);
                
                // 3. Angle du cone (en degrés -> radians)
                float coneAngle = radians(_LightAngle * 0.5);
                float threshold = cos(coneAngle);
                
                // 4. Transition lisse aux bords du cone
                float reveal = smoothstep(threshold - _RevealSoftness, threshold + _RevealSoftness, spotFalloff);
                
                // 5. Appliquer la puissance de révélation
                reveal = pow(reveal, _RevealPower);
                
                // 6. Atténuation par distance
                float dist = distance(_LightPos, i.worldPos);
                float distFalloff = 1.0 / (1.0 + dist * dist * _DistanceAttenuation);
                reveal *= distFalloff;
                
                // 7. Appliquer la révélation
                finalColor.rgb *= reveal;
                finalColor.a = texColor.a * reveal;
                
                // 8. Emission légère pour voir l'objet même dans le noir
                finalColor.rgb += finalColor.rgb * reveal * 0.3;
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "Standard"
}