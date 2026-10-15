Shader "Custom/TransparentLiquidHLSL"
{
    Properties
    {
        [Header(Colors)]
        _TopColor("Top Color", Color) = (0.0, 0.7, 0.8, 1.0)
        _BottomColor("Bottom Color", Color) = (0.0, 0.3, 0.6, 1.0)
        _FoamColor("Foam/Edge Color", Color) = (1.0, 1.0, 1.0, 1.0)
        
        [Header(Liquid Settings)]
        _FoamWidth("Foam Width", Range(0.0, 0.2)) = 0.04
        _ColorGradient("Gradient Spread", Range(0.1, 10.0)) = 2.0
        _Alpha("Global Transparency", Range(0.0, 1.0)) = 0.6

        // Hidden properties driven exactly by Liquid_1.cs
        [HideInInspector] _FillAmount("Fill Amount", Vector) = (0,0,0,0)
        [HideInInspector] _WobbleX("Wobble X", Float) = 0.0
        [HideInInspector] _WobbleZ("Wobble Z", Float) = 0.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
        }
        LOD 100

        // Disables depth writing and culling to allow true glass/liquid transparency
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off 

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0; // Now passing World Space down to the fragment
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _TopColor;
                half4 _BottomColor;
                half4 _FoamColor;
                float _FoamWidth;
                float _ColorGradient;
                float _Alpha;
                
                // Script-driven variables
                float4 _FillAmount;
                float _WobbleX;
                float _WobbleZ;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            // SV_IsFrontFace correctly implemented as a parameter
            half4 frag(Varyings input, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                // 1. Get the object's center point in World Space
                float3 objectPos = UNITY_MATRIX_M._m03_m13_m23;
                
                // 2. Match the exact C# math: WorldPos - ObjectPos - Script Offset
                float3 fillPosition = input.positionWS - objectPos - _FillAmount.xyz;

                // 3. Add wobble based on X and Z world offsets
                float wobbleOffset = (fillPosition.x * _WobbleX) + (fillPosition.z * _WobbleZ);
                
                // 4. Determine liquid surface boundary (0 is exactly the surface line)
                float wobblyFillY = fillPosition.y + wobbleOffset;

                // 5. Discard pixels above the surface to "empty" the top of the flask
                clip(-wobblyFillY);

                // 6. Calculate depth from the surface for coloring logic
                float depthFromSurface = -wobblyFillY;

                half4 finalColor;

                // 7. Backface coloring (Fakes the 3D surface plane of the liquid)
                if (!isFrontFace) 
                {
                    finalColor = _TopColor;
                    if (depthFromSurface < _FoamWidth) finalColor = _FoamColor;
                }
                // 8. Frontface coloring (The side and bottom walls of the liquid)
                else 
                {
                    if (depthFromSurface < _FoamWidth)
                    {
                        finalColor = _FoamColor;
                    }
                    else
                    {
                        float blend = saturate((depthFromSurface - _FoamWidth) * _ColorGradient);
                        finalColor = lerp(_TopColor, _BottomColor, blend);
                    }
                }

                // 9. Apply global transparency
                finalColor.a = _Alpha;

                return finalColor;
            }
            ENDHLSL
        }
    }
}