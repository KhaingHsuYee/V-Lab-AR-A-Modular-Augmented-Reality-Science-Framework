Shader "Custom/ChemistryLiquidWobble" {
    Properties {
        [Header(Colors)]
        _TopColor ("Top Surface Color", Color) = (0.4, 0.8, 1.0, 0.9)
        _BaseColor ("Side Liquid Color", Color) = (0.1, 0.4, 0.9, 0.7)
        _EdgeColor ("Deep Edge Color", Color) = (0.05, 0.1, 0.5, 0.9)
        
        [Header(Fill Settings)]
        _FillLevel ("Fill Level (0 to 1)", Range(0.0, 1.0)) = 0.5
        _MeshMin ("Mesh Bottom Bound", Float) = 0.0
        _MeshMax ("Mesh Top Bound", Float) = 4.71
        _FillDirection ("Fill Direction", Vector) = (0, 0, 1, 0)
        
        [Header(Wave Settings)]
        _WaveSpeed ("Wave Speed", Float) = 2.0
        _WaveAmplitude ("Wave Amplitude", Float) = 0.05
        _WaveFrequency ("Wave Frequency", Float) = 2.0

        // --- PHYSICAL MOVEMENT (Driven by C# script) ---
        [HideInInspector] _WobbleX ("Wobble X", Float) = 0.0
        [HideInInspector] _WobbleZ ("Wobble Z", Float) = 0.0
    }
    
    SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 100
        Cull Off Blend SrcAlpha OneMinusSrcAlpha ZWrite Off 

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
            struct v2f { float4 pos : SV_POSITION; float3 normal : NORMAL; float3 viewDir : TEXCOORD0; float isTopSurface : TEXCOORD1; };

            float4 _TopColor, _BaseColor, _EdgeColor;
            float _FillLevel, _MeshMin, _MeshMax, _WaveSpeed, _WaveAmplitude, _WaveFrequency;
            float4 _FillDirection;
            
            // Wobble variables received from C# script
            float _WobbleX;
            float _WobbleZ;

            v2f vert (appdata v) {
                v2f o;
                float3 fillDir = normalize(_FillDirection.xyz);
                float targetHeight = lerp(_MeshMin, _MeshMax, _FillLevel);
                
                // 1. PHYSICAL MOVEMENT TILT (Inertia from C# script)
                // Automatically maps movement to the correct axes based on your fill direction
                float wobbleOffset = 0.0;
                if (abs(_FillDirection.z) > 0.5) {
                    wobbleOffset = (v.vertex.x * _WobbleX) + (v.vertex.y * _WobbleZ);
                } else if (abs(_FillDirection.x) > 0.5) {
                    wobbleOffset = (v.vertex.y * _WobbleX) + (v.vertex.z * _WobbleZ);
                } else {
                    wobbleOffset = (v.vertex.x * _WobbleX) + (v.vertex.z * _WobbleZ);
                }
                targetHeight += wobbleOffset;

                // 2. AUTOMATIC RIPPLE/WAVE ANIMATION
                float wave = sin((v.vertex.x + v.vertex.z) * _WaveFrequency + (_Time.y * _WaveSpeed)) * _WaveAmplitude;
                targetHeight += wave;

                // 3. VERTEX SQUASHING (The Cap)
                float currentHeight = dot(v.vertex.xyz, fillDir);
                
                if (currentHeight > targetHeight) {
                    v.vertex.xyz -= fillDir * (currentHeight - targetHeight);
                    o.isTopSurface = 1.0;
                } else {
                    o.isTopSurface = 0.0;
                }

                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                if (_FillLevel <= 0.005) clip(-1.0);

                // --- FRESNEL EDGE EFFECT ---
                float dotProduct = dot(normalize(i.normal), normalize(i.viewDir));
                float fresnel = pow(saturate(1.0 - abs(dotProduct)), 2.0);
                
                float4 sideColor = lerp(_BaseColor, _EdgeColor, fresnel);
                
                return i.isTopSurface > 0.5 ? _TopColor : sideColor;
            }
            ENDCG
        }
    }
}