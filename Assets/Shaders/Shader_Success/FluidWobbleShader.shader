Shader "Custom/FluidWobbleShader"
{
    Properties
    {
        [Header(Fluid Aesthetics)]
        _FluidColor ("Fluid Deep Color", Color) = (0.0, 0.5, 0.8, 0.8)
        _ShallowColor ("Fluid Shallow Color", Color) = (0.2, 0.8, 1.0, 0.3)
        _SurfaceColor ("Top Surface Color", Color) = (0.3, 0.9, 1.0, 1.0)
        _FluidDensity ("Fluid Density (Depth Tint)", Range(0.1, 10.0)) = 2.0
        _RefractionStrength ("Refraction Strength", Range(0.0, 0.05)) = 0.01
        
        [Header(Fill and Wobble Controls)]
        _FillLevel ("Fill Level (0 = Empty, 1 = Full)", Range(0.0, 1.0)) = 0.5
        _BeakerHeight ("Beaker Total Height (World Units)", Float) = 0.5
        _BottomOffset ("Beaker Bottom World Offset", Float) = 0.0
        
        [Header(Automatic Idle Wobble)]
        _WobbleSpeed ("Idle Anim Speed", Float) = 2.5
        _WobbleAmplitude ("Idle Wave Height", Range(0.0, 0.05)) = 0.005

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

        // ----------------------------------------------------------------
        // PASS 1: RENDER THE INNER BACK WALLS AND TOP CAP BOUNDARY
        // ----------------------------------------------------------------
        Pass
        {
            Name "FluidTopSurface"
            Tags { "LightMode" = "UniversalForward" }
            
            Cull Front
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            CBUFFER_START(UnityPerMaterial)
                float4 _SurfaceColor; float _FillLevel; float _BeakerHeight; float _BottomOffset;
                float _WobbleX; float _WobbleZ; float _WobbleSpeed; float _WobbleAmplitude;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 objectOriginWS = TransformObjectToWorld(float3(0, 0, 0));
                float worldBottom = objectOriginWS.y + _BottomOffset;
                float targetWorldSurface = worldBottom + (_FillLevel * _BeakerHeight);
                
                float edgeDampening = saturate(_FillLevel * 10.0) * saturate((1.0 - _FillLevel) * 10.0);
                float3 relWS = input.positionWS - objectOriginWS;
                
                float idleWave = (sin(relWS.x * 20.0 + _Time.y * _WobbleSpeed) + cos(relWS.z * 20.0 + _Time.y * _WobbleSpeed)) * _WobbleAmplitude;
                float wobbleEffect = (_WobbleX * relWS.x + _WobbleZ * relWS.z + idleWave) * edgeDampening;
                
                float cutPlaneHeight = targetWorldSurface + wobbleEffect;
                
                clip(cutPlaneHeight - input.positionWS.y);
                return _SurfaceColor;
            }
            ENDHLSL
        }

        // ----------------------------------------------------------------
        // PASS 2: RENDER THE LIQUID BODY WALLS WITH DEPTH TRANSPARENCY
        // ----------------------------------------------------------------
        Pass
        {
            Name "FluidBody"
            Tags { "LightMode" = "UniversalForward" }
            
            Cull Back
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings 
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0; 
                float4 screenPos    : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _LiquidColor; float4 _ShallowColor; float _FluidDensity; float _RefractionStrength;
                float _FillLevel; float _BeakerHeight; float _BottomOffset;
                float _WobbleX; float _WobbleZ; float _WobbleSpeed; float _WobbleAmplitude;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.screenPos = ComputeScreenPos(vertexInput.positionCS);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float3 objectOriginWS = TransformObjectToWorld(float3(0, 0, 0));
                float worldBottom = objectOriginWS.y + _BottomOffset;
                float targetWorldSurface = worldBottom + (_FillLevel * _BeakerHeight);
                
                float edgeDampening = saturate(_FillLevel * 10.0) * saturate((1.0 - _FillLevel) * 10.0);
                float3 relWS = input.positionWS - objectOriginWS;
                
                float idleWave = (sin(relWS.x * 20.0 + _Time.y * _WobbleSpeed) + cos(relWS.z * 20.0 + _Time.y * _WobbleSpeed)) * _WobbleAmplitude;
                float wobbleEffect = (_WobbleX * relWS.x + _WobbleZ * relWS.z + idleWave) * edgeDampening;
                
                float cutPlaneHeight = targetWorldSurface + wobbleEffect;
                
                clip(cutPlaneHeight - input.positionWS.y);

                // Transparency sorting calculations
                float2 uv = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(uv);
                float sceneDepthWS = ComputeWorldSpacePosition(uv, rawDepth, GetWorldToHClipMatrix()).y;
                float liquidThickness = max(0.0, input.positionWS.y - sceneDepthWS);

                float2 distortedUV = uv + sin(input.positionWS.xz * 30.0 + _Time.y) * _RefractionStrength;
                float3 opaqueSceneColor = SampleSceneColor(distortedUV);

                float absorption = exp(-liquidThickness * _FluidDensity);
                float3 liquidTint = lerp(_LiquidColor.rgb, _ShallowColor.rgb, absorption);
                float combinedAlpha = lerp(_LiquidColor.a, _ShallowColor.a, absorption);

                float3 finalColor = lerp(liquidTint, opaqueSceneColor, absorption);
                return float4(finalColor, combinedAlpha);
            }
            ENDHLSL
        }
    }
}