Shader "Custom/URP_SunEaterEye"
{
    Properties
    {
        [Header(Colors)]
        [HDR] _MainColor ("Corona Iris Color", Color) = (1.0, 0.72, 0.22, 1.0)
        _OuterColor ("Outer Dust Color", Color) = (0.55, 0.32, 0.08, 1.0)

        [Header(Pixel Resolution)]
        _PixelDensity ("Pixel Grid Density", Float) = 140.0

        [Header(Pupil And Corona)]
        _PupilRadius ("Pupil Void Radius", Range(0.05, 0.35)) = 0.165
        _CoronaWidth ("Corona Thickness", Range(0.01, 0.12)) = 0.038
        _DilationSpeed ("Eye Breath Speed", Float) = 1.2
        _DilationScale ("Eye Breath Scale", Range(0.0, 0.03)) = 0.008

        [Header(Orbital Rings)]
        _RingDensity ("Ring Frequency", Float) = 26.0
        _RotationSpeed ("Base Accretion Speed", Float) = 0.75
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "SunEaterPass"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _OuterColor;
                float _PixelDensity;
                float _PupilRadius;
                float _CoronaWidth;
                float _DilationSpeed;
                float _DilationScale;
                float _RingDensity;
                float _RotationSpeed;
            CBUFFER_END

            float Hash(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Snap UVs to discrete pixel grid steps
                float2 uv = (floor(input.uv * _PixelDensity) + 0.5) / _PixelDensity;
                float2 centered = uv - 0.5;
                float dist = length(centered);
                float angle = atan2(centered.y, centered.x);

                // Breathing pupil expansion/contraction
                float dilation = sin(_Time.y * _DilationSpeed) * _DilationScale;
                float pupilEdge = _PupilRadius + dilation;

                // 1. Central Ominous Void (The Pupil)
                if (dist < pupilEdge)
                {
                    return half4(0.0, 0.0, 0.0, 1.0);
                }

                // 2. Inner Corona Rim (Lensed Event Horizon)
                float coronaDist = dist - pupilEdge;
                float coronaMask = saturate(1.0 - (coronaDist / _CoronaWidth));
                // Asymmetric Doppler beaming (brighter crescent arc)
                float lensing = 1.0 + 0.38 * sin(angle + 1.25);
                float coronaIntensity = pow(coronaMask, 1.9) * 2.5 * lensing;

                // 3. Differential Orbiting Accretion Dust (Concentric Horizons)
                float radialIndex = floor(dist * _RingDensity);
                float ringFrac = frac(dist * _RingDensity);
                float ringRand = Hash(radialIndex * 89.17);

                // Keplerian differential rotation: inner rings spin faster than outer rings
                float orbitalVelocity = (_RotationSpeed * (1.1 + ringRand * 0.9)) / max(dist * 3.0, 0.25);
                float rotatedAngle = angle + (_Time.y * orbitalVelocity);

                // Concentric circular ring boundary
                float ringBorder = step(0.68, ringFrac);

                // Angular dashed streak breakup
                float streakFrequency = 5.0 + radialIndex * 3.5;
                float streakPattern = sin(rotatedAngle * streakFrequency + ringRand * 6.2831);
                float streakMask = step(0.40 - (ringRand * 0.25), streakPattern);

                // Radial falloff: outer rings fade into the darkness of the cathedral
                float edgeFade = smoothstep(0.49, pupilEdge, dist);
                float dustIntensity = ringBorder * streakMask * edgeFade * 1.6;

                // Combine corona flare and particle dust
                float3 finalColor = (_MainColor.rgb * coronaIntensity) + (_OuterColor.rgb * dustIntensity);
                float finalAlpha = saturate(coronaIntensity + dustIntensity + (dist < 0.48 ? 0.9 : 0.0));

                // Hard clamp outside unit circle
                if (dist > 0.495)
                {
                    return half4(0.0, 0.0, 0.0, 0.0);
                }

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}