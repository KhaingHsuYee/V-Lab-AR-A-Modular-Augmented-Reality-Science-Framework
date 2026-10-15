Shader "Custom/ChemistryLiquidShader"
{
    Properties
    {
        [Header(Colors)]
        _BaseColor("Base Color (Analyte)", Color) = (0.9, 0.9, 0.9, 0.2)
        _IndicatorColor("Indicator Color", Color) = (1, 0, 0.5, 0.8)
        _TitrationProgress("Titration Progress", Range(0,1)) = 0.0

        [Header(Fill Level Control)]
        _FillLevel("Fill Level (0 to 1)", Range(0,1)) = 0.5
        _BottomOffset("Mesh Bottom Offset", Float) = -0.5
        _TopOffset("Mesh Top Offset", Float) = 0.5

        [Header(Wobble Dynamics)]
        _WobbleIntensity("Wobble Intensity", Float) = 0.0

        [Header(Liquid Appearance)]
        _SurfaceColor("Surface Highlight Color", Color) = (1, 1, 1, 0.8)
        _LineThickness("Surface Line Thickness", Range(0.001, 0.05)) = 0.005
        _RimPower("Edge Rim Power", Range(0.5, 5.0)) = 2.0
        _RimIntensity("Edge Rim Brightness", Range(0.0, 3.0)) = 1.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline"
            "DisableBatching"="True"
        }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off 

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 objectOriginWS : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _IndicatorColor;
                float _TitrationProgress;
                float _FillLevel;
                float _BottomOffset;
                float _TopOffset;
                float _WobbleIntensity;
                float4 _SurfaceColor;
                float _LineThickness;
                float _RimPower;
                float _RimIntensity;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.objectOriginWS = TransformObjectToWorld(float3(0,0,0));
                return OUT;
            }

            half4 frag(Varyings IN, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                // 1. GPU WOBBLE TILT MATH (Foolproof AR Scaling)
                float distX = IN.positionWS.x - IN.objectOriginWS.x;
                float distZ = IN.positionWS.z - IN.objectOriginWS.z;
                
                // _Time.y naturally spins the liquid. We multiply by _WobbleIntensity (controlled by C#)
                float tilt = (distX * sin(_Time.y * 25.0) + distZ * cos(_Time.y * 25.0)) * _WobbleIntensity;

                // 2. CLIPPING
                float targetWorldY = lerp(IN.objectOriginWS.y + _BottomOffset, IN.objectOriginWS.y + _TopOffset, _FillLevel);
                targetWorldY += tilt;
                
                float distToTop = targetWorldY - IN.positionWS.y;
                clip(distToTop);

                // 3. CHEMISTRY COLOR BLEND
                half4 liquidColor = lerp(_BaseColor, _IndicatorColor, _TitrationProgress);

                // 4. FAKE VOLUME / RIM LIGHTING
                float3 viewDirWS = SafeNormalize(GetCameraPositionWS() - IN.positionWS);
                float rim = 1.0 - saturate(abs(dot(viewDirWS, normalize(IN.normalWS))));
                rim = pow(rim, _RimPower) * _RimIntensity;

                half4 finalColor = liquidColor;
                
                if (isFrontFace) 
                {
                    finalColor.rgb += _SurfaceColor.rgb * rim * _SurfaceColor.a;
                }
                else
                {
                    finalColor.rgb *= 0.7; 
                }

                // 5. TOP SURFACE MENISCUS
                if (distToTop < _LineThickness) 
                {
                    finalColor.rgb += _SurfaceColor.rgb;
                    finalColor.a = saturate(finalColor.a + 0.3);
                }

                return finalColor;
            }
            ENDHLSL
        }
    }
}