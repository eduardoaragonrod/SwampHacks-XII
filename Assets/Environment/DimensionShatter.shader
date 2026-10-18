Shader "Custom/URP_DimensionShatter"
{
    Properties
    {
        [Header(Colors)]
        [HDR] _CrackColor ("Crack Edge HDR Color", Color) = (4.0, 2.2, 0.6, 1.0)
        [HDR] _PrismaticColor ("Chromatic Split Color", Color) = (0.2, 1.8, 3.5, 1.0)
        _VoidColor ("Inner Dimension Void Color", Color) = (0.0, 0.0, 0.0, 1.0)

        [Header(Fracture Controls)]
        _ShatterProgress ("Shatter Progress (0=Intact, 1=Broken)", Range(0.0, 1.0)) = 0.0
        _CrackIntensity ("Crack Line Thickness", Range(0.0, 0.25)) = 0.06
        _ShardSeparation ("Shard Gap Separation", Range(0.0, 0.5)) = 0.0
        _CellDensity ("Shard Count / Density", Float) = 9.0
        _PixelDensity ("Pixelation Grid", Float) = 128.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent+10" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "DimensionShatterPass"
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
                float4 _CrackColor;
                float4 _PrismaticColor;
                float4 _VoidColor;
                float _ShatterProgress;
                float _CrackIntensity;
                float _ShardSeparation;
                float _CellDensity;
                float _PixelDensity;
            CBUFFER_END

            float2 Hash22(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            // Returns (x: closest cell dist, y: edge distance between shards)
            float2 VoronoiCracks(float2 uv, float density)
            {
                float2 n = floor(uv * density);
                float2 f = frac(uv * density);

                float2 mg, mr;
                float md = 8.0;

                // First pass: find closest shard seed
                for (int j = -1; j <= 1; j++)
                {
                    for (int i = -1; i <= 1; i++)
                    {
                        float2 g = float2((float)i, (float)j);
                        float2 o = Hash22(n + g);
                        float2 r = g + o - f;
                        float d = dot(r, r);

                        if (d < md)
                        {
                            md = d;
                            mr = r;
                            mg = g;
                        }
                    }
                }

                // Second pass: compute exact distance to shard border lines
                md = 8.0;
                for (int j2 = -2; j2 <= 2; j2++)
                {
                    for (int i2 = -2; i2 <= 2; i2++)
                    {
                        float2 g = mg + float2((float)i2, (float)j2);
                        float2 o = Hash22(n + g);
                        float2 r = g + o - f;

                        if (dot(mr - r, mr - r) > 0.00001)
                        {
                            md = min(md, dot(0.5 * (mr + r), normalize(r - mr)));
                        }
                    }
                }

                return float2(sqrt(dot(mr, mr)), md);
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
                if (_ShatterProgress <= 0.001) return half4(0, 0, 0, 0);

                // Pixelate coordinates for crisp retro-anime look
                float2 uv = (floor(input.uv * _PixelDensity) + 0.5) / _PixelDensity;
                float2 centered = uv - 0.5;

                // Elliptical mask so the rift tapers naturally at the edges of the doorway
                float radialDist = length(float2(centered.x * 0.85, centered.y * 1.6));
                float reachMask = step(radialDist, _ShatterProgress * 0.65);

                if (reachMask <= 0.0) return half4(0, 0, 0, 0);

                // Compute primary & secondary chromatic offset Voronoi cracks
                float2 vMain = VoronoiCracks(uv, _CellDensity);
                float2 vChroma = VoronoiCracks(uv + float2(0.008, -0.005) * _ShatterProgress, _CellDensity);

                float edgeDist = vMain.y;
                float chromaDist = vChroma.y;

                // 1. Pitch-black dimensional void where shards pull apart
                float voidGap = step(edgeDist, _ShardSeparation * _ShatterProgress);

                // 2. Blazing anime fracture lines
                float crackLine = smoothstep(_CrackIntensity * _ShatterProgress, 0.0, edgeDist);
                float prismaticLine = smoothstep(_CrackIntensity * 1.3 * _ShatterProgress, 0.0, chromaDist);

                // Combine prismatic split, glowing crack edges, and dark void core
                float3 col = (_PrismaticColor.rgb * prismaticLine * 0.8) + (_CrackColor.rgb * crackLine);
                col = lerp(col, _VoidColor.rgb, voidGap * 0.85);

                // Add a bright white-hot rim right on the border of the void gap
                float voidRim = smoothstep(0.02, 0.0, abs(edgeDist - (_ShardSeparation * _ShatterProgress))) * step(0.01, _ShardSeparation);
                col += _CrackColor.rgb * voidRim * 2.5;

                float alpha = saturate((crackLine + prismaticLine * 0.5 + voidGap) * reachMask);

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}