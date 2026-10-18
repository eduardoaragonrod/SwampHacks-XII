Shader "Custom/URP_SunEaterEye"
{
    Properties
    {
        [Header(Colors)]
        [HDR] _MainColor ("Corona Iris Color", Color) = (2.5, 1.4, 0.4, 1.0)
        _OuterColor ("Outer Dust Color", Color) = (0.55, 0.32, 0.08, 1.0)
        [HDR] _SunColor ("Regurgitated Sun Core HDR", Color) = (5.0, 3.6, 1.4, 1.0)

        [Header(Pixel Resolution)]
        _PixelDensity ("Pixel Grid Density", Float) = 1174.0

        [Header(Pupil And Corona)]
        _PupilRadius ("Pupil Void Radius", Range(0.01, 0.65)) = 0.09
        _CoronaWidth ("Corona Thickness", Range(0.01, 0.25)) = 0.042
        _DilationSpeed ("Eye Breath Speed", Float) = 1.2
        _DilationScale ("Eye Breath Scale", Range(0.0, 0.03)) = 0.008

        [Header(Cutscene And Boss Finale Controls)]
        _BlinkAmount ("Eyelid Blink (0=Open, 1=Closed)", Range(0.0, 1.0)) = 0.0
        _ShockwaveRadius ("Halo Shockwave Radius", Range(0.0, 1.0)) = 0.0
        _SunRebirth ("Sun Rebirth (0=Abyss, 1=Blazing Sun)", Range(0.0, 1.0)) = 0.0

        [Header(Orbital Rings)]
        _RingDensity ("Ring Frequency", Float) = 40.4
        _RotationSpeed ("Base Accretion Speed", Float) = 0.07
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
                float4 _SunColor;
                float _PixelDensity;
                float _PupilRadius;
                float _CoronaWidth;
                float _DilationSpeed;
                float _DilationScale;
                float _BlinkAmount;
                float _ShockwaveRadius;
                float _SunRebirth;
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
                float2 uv = (floor(input.uv * _PixelDensity) + 0.5) / _PixelDensity;
                float2 centered = uv - 0.5;
                float dist = length(centered);
                float angle = atan2(centered.y, centered.x);

                if (dist > 0.495) return half4(0.0, 0.0, 0.0, 0.0);

                float eyeCurve = cos(centered.x * 3.14159);
                float lidHalfHeight = lerp(0.55, 0.0, _BlinkAmount) * eyeCurve;
                float distToLid = abs(centered.y) - lidHalfHeight;

                if (distToLid > 0.0)
                {
                    float seamGlow = smoothstep(0.015, 0.0, distToLid) * _BlinkAmount * eyeCurve;
                    return half4(_MainColor.rgb * seamGlow * 2.0, saturate(seamGlow));
                }

                float lidRim = smoothstep(0.025, 0.0, abs(distToLid)) * step(0.02, _BlinkAmount);

                float dilation = sin(_Time.y * _DilationSpeed) * _DilationScale;
                float pupilEdge = _PupilRadius + dilation;

                float solarRays = (0.5 + 0.5 * sin(angle * 8.0 + _Time.y * 4.5)) * (0.5 + 0.5 * cos(angle * 13.0 - _Time.y * 3.0));
                float sunRadius = pupilEdge * lerp(0.0, 1.35, _SunRebirth);

                if (dist < pupilEdge)
                {
                    float sunCoreMask = smoothstep(sunRadius + 0.03, max(0.0, sunRadius - 0.04), dist) * step(0.001, _SunRebirth);
                    float3 pupilColor = (_MainColor.rgb * lidRim) + (_SunColor.rgb * sunCoreMask * (1.2 + solarRays * 0.8));
                    return half4(pupilColor, 1.0);
                }

                float coronaDist = dist - pupilEdge;
                float coronaMask = saturate(1.0 - (coronaDist / (_CoronaWidth * lerp(1.0, 3.2, _SunRebirth))));
                float lensing = 1.0 + 0.38 * sin(angle + 1.25);
                float coronaIntensity = pow(coronaMask, 1.9) * 2.5 * lensing;

                float godRayFade = smoothstep(0.49, pupilEdge, dist);
                float godRayIntensity = solarRays * godRayFade * _SunRebirth * 2.8;

                float radialIndex = floor(dist * _RingDensity);
                float ringFrac = frac(dist * _RingDensity);
                float ringRand = Hash(radialIndex * 89.17);

                float orbitalVelocity = (_RotationSpeed * (1.1 + ringRand * 0.9)) / max(dist * 3.0, 0.25);
                float rotatedAngle = angle + (_Time.y * orbitalVelocity);

                float ringBorder = step(0.68, ringFrac);
                float streakFrequency = 5.0 + radialIndex * 3.5;
                float streakPattern = sin(rotatedAngle * streakFrequency + ringRand * 6.2831);
                float streakMask = step(0.40 - (ringRand * 0.25), streakPattern);

                float edgeFade = smoothstep(0.49, pupilEdge, dist);
                float dustIntensity = ringBorder * streakMask * edgeFade * 1.6;

                float waveRing = smoothstep(0.025, 0.0, abs(dist - _ShockwaveRadius)) * step(0.01, _ShockwaveRadius);

                float3 activeCoreColor = lerp(_MainColor.rgb, _SunColor.rgb, _SunRebirth);
                float3 finalColor = (activeCoreColor * (coronaIntensity + lidRim + waveRing * 3.0 + godRayIntensity)) 
                                  + (lerp(_OuterColor.rgb, _SunColor.rgb * 0.6, _SunRebirth) * dustIntensity);

                float innerVoidShadow = smoothstep(pupilEdge + 0.14, pupilEdge, dist) * 0.85 * (1.0 - _SunRebirth);
                float finalAlpha = saturate(coronaIntensity + (dustIntensity * 0.95) + waveRing + lidRim + innerVoidShadow + (godRayIntensity * 0.5));

                return half4(finalColor, finalAlpha);
            }
            ENDHLSL
        }
    }
}