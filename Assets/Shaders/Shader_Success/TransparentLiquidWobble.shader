Shader "Custom/TransparentLiquidWobble"
{
    Properties
    {
        [Header(Mesh Axis Alignment)]
        [KeywordEnum(Y_UP, Z_UP, X_UP)] _UpAxis ("Mesh Up Axis", Float) = 1 

        [Header(Liquid Aesthetics)]
        _LiquidColor ("Liquid Deep Color", Color) = (0.0, 0.5, 0.8, 0.8)
        _ShallowColor ("Liquid Shallow Color", Color) = (0.2, 0.8, 1.0, 0.3)
        _SurfaceColor ("Top Surface Color", Color) = (0.3, 0.9, 1.0, 1.0)
        _LiquidDensity ("Liquid Density (Depth Tint)", Range(0.1, 10.0)) = 2.0
        _RefractionStrength ("Refraction Strength", Range(0.0, 0.1)) = 0.02
        
        [Header(Fill and Wobble Controls)]
        _FillLevel ("Fill Level (0 = Empty, 1 = Full)", Range(0.0, 1.0)) = 0.5
        _LocalMin ("Local Mesh Bottom Bound", Float) = -0.5
        _LocalMax ("Local Mesh Top Bound", Float) = 0.5
        
        [Header(Precipitate Sinking Controls)]
        _PrecipitateColor ("Precipitate Color", Color) = (1.0, 0.816, 0.0, 0.0)
        _CloudTop ("Cloud Top (1=Surface, 0=Bottom)", Range(0.0, 1.0)) = 1.0
        _CloudBottom ("Cloud Bottom (1=Surface, 0=Bottom)", Range(0.0, 1.0)) = 1.0

        [Header(Automatic Idle Wobble)]
        _WobbleSpeed ("Idle Anim Speed", Float) = 2.5
        _WobbleAmplitude ("Idle Wave Height", Range(0.0, 0.1)) = 0.02

        [Header(C# Physics Wobble (Do Not Edit))]
        _WobbleX ("Script Wobble X", Range(-1.0, 1.0)) = 0.0
        _WobbleZ ("Script Wobble Z", Range(-1.0, 1.0)) = 0.0
    }

    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent" 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "LiquidVolume"
            Tags { "LightMode" = "UniversalForward" }
            
            Cull Off
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma shader_feature_local _UPAXIS_Y_UP _UPAXIS_Z_UP _UPAXIS_X_UP

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 positionWY   : TEXCOORD0; 
                float3 positionLY   : TEXCOORD3; 
                float4 screenPos    : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float _UpAxis;
                float4 _LiquidColor;
                float4 _ShallowColor;
                float4 _SurfaceColor;
                float _LiquidDensity;
                float _RefractionStrength;
                float _FillLevel;
                float _LocalMin;
                float _LocalMax;
                float4 _PrecipitateColor;
                float _CloudTop;
                float _CloudBottom;
                float _WobbleX;
                float _WobbleZ;
                float _WobbleSpeed;
                float _WobbleAmplitude;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWY = float4(vertexInput.positionWS, 1.0);
                output.positionLY = input.positionOS.xyz; 
                output.screenPos = ComputeScreenPos(vertexInput.positionCS);
                return output;
            }

            float4 frag(Varyings input, bool facing : SV_IsFrontFace) : SV_Target
            {
                float localUp = input.positionLY.y;
                float localLeft = input.positionLY.x;
                float localForward = input.positionLY.z;

                #if defined(_UPAXIS_Z_UP)
                    localUp = input.positionLY.z;
                    localLeft = input.positionLY.x;
                    localForward = input.positionLY.y;
                #elif defined(_UPAXIS_X_UP)
                    localUp = input.positionLY.x;
                    localLeft = input.positionLY.y;
                    localForward = input.positionLY.z;
                #endif

                float targetLocalSurface = lerp(_LocalMin, _LocalMax, _FillLevel);
                float edgeDampening = saturate(_FillLevel * 10.0) * saturate((1.0 - _FillLevel) * 10.0);
                
                float idleWaveX = sin(localLeft * 6.0 + _Time.y * _WobbleSpeed) * _WobbleAmplitude;
                float idleWaveZ = cos(localForward * 6.0 + _Time.y * _WobbleSpeed) * _WobbleAmplitude;
                float wobbleXOffset = (_WobbleX * localLeft + idleWaveX) * edgeDampening;
                float wobbleZOffset = (_WobbleZ * localForward + idleWaveZ) * edgeDampening;
                
                float distToSurface = localUp - (targetLocalSurface + wobbleXOffset + wobbleZOffset);
                clip(-distToSurface);

                // --- NEW: SINKING CLOUD MASK ---
                // Calculate exactly where this pixel is relative to the liquid's height (0 = bottom, 1 = surface)
                float actualLiquidHeight = targetLocalSurface - _LocalMin;
                float distFromBottom = localUp - _LocalMin;
                float percentHeight = saturate(distFromBottom / max(0.001, actualLiquidHeight));

                // Creates a band of color perfectly between CloudBottom and CloudTop
                float cloudMask = smoothstep(_CloudBottom - 0.02, _CloudBottom + 0.02, percentHeight) 
                                * smoothstep(_CloudTop + 0.02, _CloudTop - 0.02, percentHeight);

                float2 uv = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(uv);
                float sceneDepthWS = ComputeWorldSpacePosition(uv, rawDepth, GetWorldToHClipMatrix()).y;
                float liquidThickness = max(0.0, input.positionWY.y - sceneDepthWS);

                float2 distortedUV = uv + sin(input.positionWY.xz * 15.0 + _Time.y) * _RefractionStrength;
                float3 opaqueSceneColor = SampleSceneColor(distortedUV);

                float absorption = exp(-liquidThickness * _LiquidDensity);
                float3 liquidTint = lerp(_LiquidColor.rgb, _ShallowColor.rgb, absorption);
                float combinedAlpha = lerp(_LiquidColor.a, _ShallowColor.a, absorption);

                if (!facing)
                {
                    // Only color the meniscus if the cloud is currently at the surface
                    float topPrecipitateMask = smoothstep(0.95, 1.0, _CloudTop); 
                    float3 topColor = lerp(_SurfaceColor.rgb, _PrecipitateColor.rgb, topPrecipitateMask * _PrecipitateColor.a);
                    return float4(topColor, _SurfaceColor.a);
                }

                float3 finalColor = lerp(liquidTint, opaqueSceneColor, absorption);
                
                // Overlay the sinking cloud color
                finalColor = lerp(finalColor, _PrecipitateColor.rgb, cloudMask * _PrecipitateColor.a);
                combinedAlpha = lerp(combinedAlpha, max(combinedAlpha, _PrecipitateColor.a), cloudMask * _PrecipitateColor.a);

                return float4(finalColor, combinedAlpha);
            }
            ENDHLSL
        }
    }
}