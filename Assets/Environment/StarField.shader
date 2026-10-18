Shader "Custom/URP_CosmicStarfield"
{
    Properties
    {
        [Header(Deep Space Base)]
        _SpaceBgColor ("Deep Space Void Color", Color) = (0.015, 0.012, 0.025, 1.0)

        [Header(Procedural Nebula Clouds)]
        [HDR] _NebulaColorA ("Nebula Primary Color (HDR)", Color) = (0.8, 0.25, 0.55, 1.0)
        [HDR] _NebulaColorB ("Nebula Secondary Color (HDR)", Color) = (0.15, 0.45, 0.95, 1.0)
        [HDR] _NebulaCoreGlow ("Center Accretion Mist Color", Color) = (1.4, 0.65, 0.15, 1.0)
        _NebulaScale ("Nebula Cloud Scale", Float) = 3.2
        _NebulaIntensity ("Nebula Brightness", Range(0.0, 2.0)) = 0.55
        _NebulaDriftSpeed ("Nebula Swirl Speed", Float) = 0.04

        [Header(Twinkling Stars)]
        [HDR] _StarTint ("Bright Star HDR Multiplier", Color) = (2.2, 2.0, 1.8, 1.0)
        _StarDensity ("Starfield Grid Density", Float) = 28.0
        _TwinkleSpeed ("Star Twinkle Speed", Float) = 3.2
        _StarSize ("Star Size Multiplier", Range(0.2, 2.5)) = 1.0
        _CrossFlareStrength ("Diamond Star Flare", Range(0.0, 2.0)) = 0.85

        [Header(Style)]
        _PixelDensity ("Pixel Grid (Match Eye or High-Res)", Float) = 512.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "Queue" = "Background" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        ZWrite Off
        Cull Off

        Pass
        {
            Name "CosmicStarfieldPass"
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
                float4 _SpaceBgColor;
                float4 _NebulaColorA;
                float4 _NebulaColorB;
                float4 _NebulaCoreGlow;
                float _NebulaScale;
                float _NebulaIntensity;
                float _NebulaDriftSpeed;
                float4 _StarTint;
                float _StarDensity;
                float _TwinkleSpeed;
                float _StarSize;
                float _CrossFlareStrength;
                float _PixelDensity;
            CBUFFER_END

            float Hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float2 Hash22(float2 p)
            {
                float n = Hash21(p);
                return float2(n, Hash21(p + n));
            }

            float Noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float FBM(float2 p)
            {
                float value = 0.0;
                float amp = 0.5;
                float2 shift = float2(100.0, 100.0);
                float2x2 rot = float2x2(cos(0.5), sin(0.5), -sin(0.5), cos(0.5));

                for (int i = 0; i < 4; i++)
                {
                    value += amp * Noise(p);
                    p = mul(rot, p) * 2.02 + shift;
                    amp *= 0.5;
                }
                return value;
            }

            float3 StarLayer(float2 uv, float density, float seedOffset, float sizeMult)
            {
                float2 gv = frac(uv * density) - 0.5;
                float2 id = floor(uv * density) + seedOffset;

                float2 randOffset = (Hash22(id) - 0.5) * 0.72;
                float2 dVec = gv - randOffset;
                float dist = length(dVec);

                float starSeed = Hash21(id * 1.37);

                if (starSeed < 0.45) return float3(0, 0, 0);

                float twinkleFreq = _TwinkleSpeed * (0.6 + starSeed * 1.4);
                float twinklePhase = starSeed * 62.83;
                float twinkle = 0.55 + 0.45 * sin(_Time.y * twinkleFreq + twinklePhase);
                twinkle = pow(twinkle, 1.8) * 1.35;

                float radius = (0.035 * _StarSize * sizeMult) * (0.4 + 0.6 * frac(starSeed * 17.3));
                float core = smoothstep(radius, 0.0, dist);

                float flare = 0.0;
                if (starSeed > 0.82)
                {
                    float2 absD = abs(dVec);
                    float fourPoint = max(
                        smoothstep(radius * 3.5, 0.0, absD.x) * smoothstep(radius * 0.45, 0.0, absD.y),
                        smoothstep(radius * 3.5, 0.0, absD.y) * smoothstep(radius * 0.45, 0.0, absD.x)
                    );
                    flare = fourPoint * _CrossFlareStrength * twinkle;
                }

                float colorPick = frac(starSeed * 43.19);
                float3 starCol = lerp(float3(0.65, 0.82, 1.0), float3(1.0, 0.78, 0.55), colorPick);

                return starCol * (core + flare) * twinkle;
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
                float centerDist = length(centered);

                float angle = atan2(centered.y, centered.x);
                float swirlAngle = angle + (_Time.y * _NebulaDriftSpeed) + (0.28 / (centerDist + 0.15));
                float2 warpedUV = float2(cos(swirlAngle), sin(swirlAngle)) * centerDist;

                float2 nebCoord = uv * _NebulaScale + float2(_Time.y * _NebulaDriftSpeed * 0.3, -_Time.y * _NebulaDriftSpeed * 0.2);
                float2 q = float2(
                    FBM(nebCoord),
                    FBM(nebCoord + float2(5.2, 1.3))
                );
                float2 r = float2(
                    FBM(nebCoord + 3.5 * q + float2(1.7, 9.2) + 0.05 * _Time.y),
                    FBM(nebCoord + 3.5 * q + float2(8.3, 2.8) + 0.04 * _Time.y)
                );

                float nebulaDensity = FBM(nebCoord + 2.5 * r + warpedUV * 0.6);
                float nebulaMask = smoothstep(0.32, 0.85, nebulaDensity);

                float3 nebulaColor = lerp(_NebulaColorA.rgb, _NebulaColorB.rgb, saturate(length(q)));
                nebulaColor *= nebulaMask * _NebulaIntensity;

                float coreMist = smoothstep(0.45, 0.08, centerDist) * nebulaDensity * 0.65;
                nebulaColor += _NebulaCoreGlow.rgb * coreMist;

                float3 starsDeep = StarLayer(uv, _StarDensity * 1.9, 11.4, 0.6) * 0.55;
                float3 starsMid = StarLayer(uv + float2(0.13, 0.27), _StarDensity, 37.8, 1.0) * 0.9;
                float3 starsBright = StarLayer(uv + float2(0.51, 0.73), _StarDensity * 0.45, 93.2, 1.45) * 1.5;

                float nurseryBoost = 1.0 + nebulaMask * 0.65;
                float3 totalStars = (starsDeep + starsMid + starsBright) * _StarTint.rgb * nurseryBoost;

                float3 finalColor = _SpaceBgColor.rgb + nebulaColor + totalStars;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}