Shader "Custom/ChemicalSwirl"
{
    Properties
    {
        _Color ("Chemical Color", Color) = (1.0, 0.816, 0.0, 1.0) // Canary Yellow Default
        _SwirlSpeed ("Swirl Speed", Range(0, 5)) = 1.5
        _Distortion ("Distortion Amount", Range(0, 3)) = 1.2
        _Scale ("Noise Scale", Range(1, 20)) = 8.0
        _Density ("Cloud Density", Range(0, 5)) = 2.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        
        // Setup for soft transparency
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // Particle System color support
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float4 particleColor : COLOR;
            };

            float4 _Color;
            float _SwirlSpeed;
            float _Distortion;
            float _Scale;
            float _Density;

            // Pseudo-random generator
            float random (float2 st) {
                return frac(sin(dot(st.xy, float2(12.9898,78.233))) * 43758.5453123);
            }

            // 2D Value Noise
            float noise(float2 st) {
                float2 i = floor(st);
                float2 f = frac(st);
                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a)* u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.particleColor = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Use local UVs for the mask, but world position for the noise 
                // so the fluid looks like it's moving through a 3D space
                float2 noiseUV = i.worldPos.xy * _Scale; 
                float time = _Time.y * _SwirlSpeed;

                // Layer 1: Base fluid flow
                float flow1 = noise(noiseUV + float2(time, time * 0.5));
                
                // Layer 2: Counter-flow to create the "swirling" distortion
                float flow2 = noise(noiseUV * 2.0 - float2(time * 0.8, -time));
                
                // Combine and distort to get that organic, stringy liquid look
                float finalFluid = noise(noiseUV + flow1 * _Distortion + flow2);

                // Create a soft radial mask so it looks like a puff, not a hard square
                float distFromCenter = distance(i.uv, float2(0.5, 0.5));
                float radialMask = smoothstep(0.5, 0.1, distFromCenter);

                // Combine Shader Color with Particle System Color (allows fading out)
                fixed4 baseColor = _Color * i.particleColor;
                
                // Apply the fluid noise to the opacity (alpha)
                baseColor.a *= finalFluid * radialMask * _Density;
                baseColor.a = saturate(baseColor.a);

                return baseColor;
            }
            ENDCG
        }
    }
}